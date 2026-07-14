using Microsoft.AspNetCore.SignalR;
namespace TableBackend.Hubs;

public class EmailHub : Hub, IEmailHub
{
    private readonly ILogger<EmailHub> _logger;

    public EmailHub(ILogger<EmailHub> logger)
    {
        _logger = logger;
    }

    public Task Subscribe(string userId) 
    {
        throw new NotImplementedException();
    }
}