using MailKit.Search;
using TableBackend.Config.XML;
using TableBackend.Config.XML.Email;

namespace TableBackend.Service.Email;

using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

public static class ImapIdleService
{
    private static ImapClient? _client;
    private static readonly SemaphoreSlim ConnectionLock = new(1, 1);
    private static CancellationTokenSource _idleCts = new();
    private static Task? _idleTask;
    private static bool _isStopping;

    private const int KeepAliveIntervalMinutes = 5;

    private static readonly EmailConfiguration.EmailSettings EmailConfiguration =
        XmlSettingService.LoadSettings(XmlConfigs.Email).Settings;

    public static event EventHandler<MessageEventArgs>? NewMessageArrived;

    /// <summary>
    /// Запускает IDLE сессию (вызовите один раз при старте приложения)
    /// </summary>
    public static async Task StartAsync()
    {
        if (_idleTask is { IsCompleted: false })
        {
            return; // Уже запущено
        }

        _isStopping = false;
        _idleCts = new CancellationTokenSource();
        _idleTask = RunIdleLoopAsync(_idleCts.Token);

        // Не блокируем, возвращаем управление
        await Task.CompletedTask;
    }

    /// <summary>
    /// Останавливает IDLE сессию (вызовите при завершении приложения)
    /// </summary>
    public static async Task StopAsync()
    {
        _isStopping = true;
        await _idleCts.CancelAsync();

        if (_idleTask is { IsCompleted: false })
        {
            try
            {
                await _idleTask.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (TimeoutException)
            {
                // Принудительно разрываем
                await _idleCts.CancelAsync();
            }
        }

        await DisconnectInternalAsync();
    }

    /// <summary>
    /// Основной цикл IDLE с переподключением
    /// </summary>
    private static async Task RunIdleLoopAsync(CancellationToken cancellationToken)
    {
        while (!_isStopping && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Подключаемся и запускаем IDLE
                await ConnectAndIdleAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Логируем ошибку (используйте ILogger)
                Console.WriteLine($"[IMAP IDLE] Ошибка: {ex.Message}");

                // Ждем перед переподключением
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Устанавливает соединение и запускает IDLE
    /// </summary>
    private static async Task ConnectAndIdleAsync(CancellationToken cancellationToken)
    {
        await ConnectionLock.WaitAsync(cancellationToken);
        try
        {
            // Проверяем и пересоздаем клиент
            if (_client is not { IsConnected: true })
            {
                _client?.Dispose();
                _client = new ImapClient();

                // Настройка таймаутов
                _client.Timeout = 60000; // 60 секунд

                // Подключение
                await _client.ConnectAsync(EmailConfiguration.ImapServer, EmailConfiguration.ImapPort,
                    SecureSocketOptions.SslOnConnect,
                    cancellationToken);

                await _client.AuthenticateAsync(EmailConfiguration.EmailAddress, EmailConfiguration.Password,
                    cancellationToken);

                // Открываем папку
                await _client.Inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

                // Подписываемся на события
                _client.Inbox.CountChanged += OnInboxCountChanged;

                Console.WriteLine("[IMAP IDLE] Подключено успешно");
            }
        }
        finally
        {
            ConnectionLock.Release();
        }

        // Запускаем IDLE в бесконечном цикле
        while (!_isStopping && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Создаем токен с таймаутом для Keep-Alive
                using (var idleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    idleCts.CancelAfter(TimeSpan.FromMinutes(KeepAliveIntervalMinutes));
                    
                    // ✅ Правильный вызов - только CancellationToken
                    await _client.IdleAsync(idleCts.Token);
                }
                
                // Если дошли сюда - IDLE завершился по таймауту
                Console.WriteLine($"[IMAP IDLE] Keep-Alive: перезапуск через {KeepAliveIntervalMinutes} минут");
            }
            catch (OperationCanceledException)
            {
                // Если отменено нашим таймаутом - просто перезапускаем
                if (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("[IMAP IDLE] Keep-Alive: перезапуск IDLE");
                    continue;
                }
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAP IDLE] Ошибка IDLE: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Обработчик события изменения количества писем
    /// </summary>
    private static async void OnInboxCountChanged(object? sender, EventArgs e)
    {
        try
        {
            // Защита от повторных входов
            if (!await ConnectionLock.WaitAsync(1000))
                return;

            try
            {
                if (sender is not IMailFolder inbox) return;

                // Получаем последние непрочитанные письма
                var query = SearchQuery.NotSeen;
                var uids = await inbox.SearchAsync(query);

                if (uids.Count <= 0) return;
                
                // Берем последние N писем (например, 5)
                var lastUids = uids.TakeLast(Math.Min(5, uids.Count)).ToList();
                
                var items = await inbox.FetchAsync(lastUids,
                    MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope);

                foreach (var item in items)
                {
                    // Генерируем событие для внешних подписчиков
                    NewMessageArrived?.Invoke(null, new MessageEventArgs
                    {
                        Subject = item.Envelope.Subject,
                        From = item.Envelope.From.ToString(),
                        Date = item.Envelope.Date,
                        Uid = item.UniqueId
                    });

                    Console.WriteLine($"[IMAP] Новое письмо: {item.Envelope.Subject} от {item.Envelope.From}");
                }

                // Опционально: отмечаем как прочитанные
                // await inbox.AddFlagsAsync(lastUids, MessageFlags.Seen, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAP] Ошибка обработки письма: {ex.Message}");
            }
            finally
            {
                ConnectionLock.Release();
            }
        }
        catch (Exception)
        {
            throw; // TODO handle exception
        }
    }

    /// <summary>
    /// Отключение и очистка ресурсов
    /// </summary>
    private static async Task DisconnectInternalAsync()
    {
        await ConnectionLock.WaitAsync();
        try
        {
            if (_client is { IsConnected: true })
            {
                _client.Inbox.CountChanged -= OnInboxCountChanged;
                await _client.DisconnectAsync(true);
            }

            _client?.Dispose();
            _client = null;
        }
        finally
        {
            ConnectionLock.Release();
        }
    }

    /// <summary>
    /// Публичный метод для получения клиента (если нужно вручную)
    /// </summary>
    public static async Task<ImapClient> GetClientAsync()
    {
        await ConnectionLock.WaitAsync();
        try
        {
            return _client is not { IsConnected: true }
                ? throw new InvalidOperationException("IMAP клиент не подключен. Запустите StartAsync()")
                : _client;
        }
        finally
        {
            ConnectionLock.Release();
        }
    }
}