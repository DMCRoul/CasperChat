using CasperChat.Server.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace CasperChat.Server.Hubs;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> Users = new();
    private static readonly ConcurrentDictionary<string, string> Connections = new();
    private static readonly List<ChatMessage> Messages = new();

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = Connections
            .FirstOrDefault(x => x.Value == Context.ConnectionId).Key;

        if (!string.IsNullOrWhiteSpace(user))
        {
            Connections.TryRemove(user, out _);
            await BroadcastUsers();
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<AuthResult> Register(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, "Имя и пароль обязательны");

        if (!Users.TryAdd(username, password))
            return new AuthResult(false, "Пользователь уже существует");

        Connections[username] = Context.ConnectionId;
        await BroadcastUsers();
        return new AuthResult(true, "OK");
    }

    public async Task<AuthResult> Login(string username, string password)
    {
        if (!Users.TryGetValue(username, out var savedPassword))
            return new AuthResult(false, "Пользователь не найден");

        if (savedPassword != password)
            return new AuthResult(false, "Неверный пароль");

        Connections[username] = Context.ConnectionId;
        await BroadcastUsers();
        return new AuthResult(true, "OK");
    }

    public async Task SendMessage(string selectedUser, string text)
    {
        var fromUser = GetCurrentUser();

        if (string.IsNullOrWhiteSpace(fromUser))
            throw new HubException("Пользователь не авторизован");

        var message = new ChatMessage
        {
            FromUser = fromUser,
            ToUser = selectedUser,
            Text = text,
            MessageType = "text",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        Messages.Add(message);

        await Clients.Caller.SendAsync("ReceiveMessage", message);

        if (Connections.TryGetValue(selectedUser, out var targetConnectionId))
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", message);
    }

    public async Task SendFileMessage(string selectedUser, string fileName, string fileUrl)
    {
        var fromUser = GetCurrentUser();

        if (string.IsNullOrWhiteSpace(fromUser))
            throw new HubException("Пользователь не авторизован");

        var message = new ChatMessage
        {
            FromUser = fromUser,
            ToUser = selectedUser,
            FileName = fileName,
            FileUrl = fileUrl,
            MessageType = "file",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        Messages.Add(message);

        await Clients.Caller.SendAsync("ReceiveMessage", message);

        if (Connections.TryGetValue(selectedUser, out var targetConnectionId))
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", message);
    }

    public Task<List<ChatMessage>> GetHistory(string selectedUser)
    {
        var currentUser = GetCurrentUser();

        if (string.IsNullOrWhiteSpace(currentUser))
            throw new HubException("Пользователь не авторизован");

        var history = Messages
            .Where(m =>
                (m.FromUser == currentUser && m.ToUser == selectedUser) ||
                (m.FromUser == selectedUser && m.ToUser == currentUser))
            .OrderBy(m => m.CreatedAtUtc)
            .ToList();

        return Task.FromResult(history);
    }

    private string? GetCurrentUser()
    {
        return Connections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
    }

    private Task BroadcastUsers()
    {
        var users = Connections.Keys.OrderBy(x => x).ToList();
        return Clients.All.SendAsync("UsersList", users);
    }
}