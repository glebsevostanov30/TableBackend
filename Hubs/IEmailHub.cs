using Microsoft.AspNetCore.SignalR;

namespace TableBackend.Hubs;

public interface IEmailHub
{
    public Task Subscribe(string userId);
}