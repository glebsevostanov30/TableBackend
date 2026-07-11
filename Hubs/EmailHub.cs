using Microsoft.AspNetCore.SignalR;
using TableBackend.Dto.email;

namespace TableBackend.Hubs;

public class EmailHub : Hub
{
    private readonly ILogger<EmailHub> _logger;
    private static readonly Dictionary<string, string> _userConnections = new();

    public EmailHub(ILogger<EmailHub> logger)
    {
        _logger = logger;
    }

    // Подписка на получение писем
    public async Task SubscribeToEmails(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User ID not found");
        }

        // Добавляем в группу пользователя
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Сохраняем связь connectionId -> userId
        _userConnections[Context.ConnectionId] = userId;

        _logger.LogInformation($"User {userId} subscribed to emails. ConnectionId: {Context.ConnectionId}");

        // Отправляем подтверждение
        await Clients.Caller.SendAsync("SubscriptionConfirmed", new
        {
            Status = "Success",
            Message = "Subscribed to email updates",
            UserId = userId
        });
    }

    // Отписка
    public async Task UnsubscribeFromEmails()
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _userConnections.Remove(Context.ConnectionId);

            _logger.LogInformation($"User {userId} unsubscribed from emails");
        }
    }

    // Метод для отправки письма конкретному пользователю (вызывается из вашего почтового сервиса)
    public async Task SendEmailToUser(string userId, EmailDto email)
    {
        try
        {
            await Clients.Group($"user_{userId}").SendAsync("ReceiveEmail", email);
            _logger.LogInformation($"Email sent to user {userId}: {email.Subject}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to user {userId}");
            throw;
        }
    }

    // Метод для массовой рассылки
    public async Task BroadcastEmail(EmailDto email)
    {
        await Clients.All.SendAsync("BroadcastEmail", email);
        _logger.LogInformation($"Email broadcasted: {email.Subject}");
    }

    // Отметка о прочтении
    public async Task MarkAsRead(string emailId)
    {
        var userId = GetUserId();
        // Здесь логика обновления в БД
        _logger.LogInformation($"User {userId} marked email {emailId} as read");

        // Оповещаем других клиентов этого пользователя
        await Clients.Group($"user_{userId}").SendAsync("EmailRead", new { EmailId = emailId, UserId = userId });
    }

    // Переопределяем методы жизненного цикла
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation($"Client connected: {Context.ConnectionId}, User: {userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _userConnections.Remove(Context.ConnectionId);
        }

        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}, User: {userId}");
        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserId()
    {
        return "user-123";
    }
}