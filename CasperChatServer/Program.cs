using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileProviders;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

builder.Services.AddSignalR();

var app = builder.Build();

FileStorage.Initialize();
Database.Initialize();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(FileStorage.UploadsPath),
    RequestPath = "/uploads"
});

app.MapGet("/", () => "Casper Chat Server Running");

app.MapPost("/upload", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { error = "Ожидался multipart/form-data" });

    var form = await request.ReadFormAsync();
    var file = form.Files["file"];

    if (file == null || file.Length == 0)
        return Results.BadRequest(new { error = "Файл не найден" });

    if (file.Length > 20 * 1024 * 1024)
        return Results.BadRequest(new { error = "Файл слишком большой. Максимум 20 MB." });

    string originalFileName = Path.GetFileName(file.FileName);

    await using var stream = file.OpenReadStream();
    string storedFileName = await FileStorage.SaveFileAsync(originalFileName, stream);
    string fileUrl = $"/uploads/{storedFileName}";

    return Results.Ok(new UploadResult
    {
        Success = true,
        FileName = originalFileName,
        FileUrl = fileUrl
    });
});

app.MapHub<ChatHub>("/chat");

app.Run("http://0.0.0.0:5064");

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionUsers = new();

    public async Task<AuthResult> Register(string username, string password)
    {
        username = (username ?? "").Trim();
        password = (password ?? "").Trim();

        var result = Database.Register(username, password);
        if (!result.Success)
            return result;

        OnlineUsers[username] = Context.ConnectionId;
        ConnectionUsers[Context.ConnectionId] = username;

        await BroadcastUsersList();
        return new AuthResult(true, "Регистрация выполнена");
    }

    public async Task<AuthResult> Login(string username, string password)
    {
        username = (username ?? "").Trim();
        password = (password ?? "").Trim();

        var result = Database.Login(username, password);
        if (!result.Success)
            return result;

        OnlineUsers[username] = Context.ConnectionId;
        ConnectionUsers[Context.ConnectionId] = username;

        await BroadcastUsersList();
        return new AuthResult(true, "Вход выполнен");
    }

    public async Task SendMessage(string toUser, string text)
    {
        if (!ConnectionUsers.TryGetValue(Context.ConnectionId, out var fromUser))
            return;

        toUser = (toUser ?? "").Trim();
        text = (text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(toUser) || string.IsNullOrWhiteSpace(text))
            return;

        var savedMessage = Database.SaveTextMessage(fromUser, toUser, text);
        await DeliverMessage(savedMessage);
    }

    public async Task SendFileMessage(string toUser, string fileName, string fileUrl)
    {
        if (!ConnectionUsers.TryGetValue(Context.ConnectionId, out var fromUser))
            return;

        toUser = (toUser ?? "").Trim();
        fileName = Path.GetFileName((fileName ?? "").Trim());
        fileUrl = (fileUrl ?? "").Trim();

        if (string.IsNullOrWhiteSpace(toUser) ||
            string.IsNullOrWhiteSpace(fileName) ||
            string.IsNullOrWhiteSpace(fileUrl))
            return;

        var savedMessage = Database.SaveFileMessage(fromUser, toUser, fileName, fileUrl);
        await DeliverMessage(savedMessage);
    }

    public Task<List<ChatMessage>> GetHistory(string otherUser)
    {
        if (!ConnectionUsers.TryGetValue(Context.ConnectionId, out var currentUser))
            return Task.FromResult(new List<ChatMessage>());

        otherUser = (otherUser ?? "").Trim();

        if (string.IsNullOrWhiteSpace(otherUser))
            return Task.FromResult(new List<ChatMessage>());

        return Task.FromResult(Database.GetHistory(currentUser, otherUser));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionUsers.TryRemove(Context.ConnectionId, out var username))
        {
            OnlineUsers.TryRemove(username, out _);
            await BroadcastUsersList();
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task DeliverMessage(ChatMessage message)
    {
        if (OnlineUsers.TryGetValue(message.ToUser, out var toConnectionId))
        {
            await Clients.Client(toConnectionId).SendAsync("ReceiveMessage", message);
        }

        await Clients.Caller.SendAsync("ReceiveMessage", message);
    }

    private async Task BroadcastUsersList()
    {
        var users = OnlineUsers.Keys.OrderBy(x => x).ToArray();
        await Clients.All.SendAsync("UsersList", users);
    }
}

public static class FileStorage
{
    public static readonly string UploadsPath = Path.Combine(AppContext.BaseDirectory, "uploads");

    public static void Initialize()
    {
        Directory.CreateDirectory(UploadsPath);
    }

    public static async Task<string> SaveFileAsync(string originalFileName, Stream source)
    {
        string ext = Path.GetExtension(originalFileName);
        string storedFileName = $"{Guid.NewGuid():N}{ext}";
        string fullPath = Path.Combine(UploadsPath, storedFileName);

        await using var fileStream = File.Create(fullPath);
        await source.CopyToAsync(fileStream);

        return storedFileName;
    }
}

public static class Database
{
    private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "chat.db");
    private static readonly string ConnectionString = $"Data Source={DbPath}";

    public static void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL,
            created_at_utc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS messages (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            from_user TEXT NOT NULL,
            to_user TEXT NOT NULL,
            text TEXT,
            message_type TEXT NOT NULL,
            file_name TEXT,
            file_url TEXT,
            created_at_utc TEXT NOT NULL
        );
        """;
        command.ExecuteNonQuery();
    }

    public static AuthResult Register(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new AuthResult(false, "Имя пользователя пустое");

        if (string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, "Пароль пустой");

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = """
            SELECT COUNT(*)
            FROM users
            WHERE username = $username
        """;
        checkCommand.Parameters.AddWithValue("$username", username);

        long count = (long)(checkCommand.ExecuteScalar() ?? 0L);

        if (count > 0)
            return new AuthResult(false, "Такой username уже занят");

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO users (username, password_hash, created_at_utc)
            VALUES ($username, $passwordHash, $createdAtUtc)
        """;
        insertCommand.Parameters.AddWithValue("$username", username);
        insertCommand.Parameters.AddWithValue("$passwordHash", HashPassword(password));
        insertCommand.Parameters.AddWithValue("$createdAtUtc", DateTime.UtcNow.ToString("O"));
        insertCommand.ExecuteNonQuery();

        return new AuthResult(true, "Пользователь зарегистрирован");
    }

    public static AuthResult Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new AuthResult(false, "Имя пользователя пустое");

        if (string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, "Пароль пустой");

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT password_hash
            FROM users
            WHERE username = $username
        """;
        command.Parameters.AddWithValue("$username", username);

        var existingHash = command.ExecuteScalar() as string;

        if (existingHash == null)
            return new AuthResult(false, "Пользователь не найден");

        if (!string.Equals(existingHash, HashPassword(password), StringComparison.Ordinal))
            return new AuthResult(false, "Неверный пароль");

        return new AuthResult(true, "Успешный вход");
    }

    public static ChatMessage SaveTextMessage(string fromUser, string toUser, string text)
    {
        var createdAtUtc = DateTime.UtcNow.ToString("O");

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO messages (from_user, to_user, text, message_type, file_name, file_url, created_at_utc)
            VALUES ($fromUser, $toUser, $text, 'text', NULL, NULL, $createdAtUtc)
        """;
        command.Parameters.AddWithValue("$fromUser", fromUser);
        command.Parameters.AddWithValue("$toUser", toUser);
        command.Parameters.AddWithValue("$text", text);
        command.Parameters.AddWithValue("$createdAtUtc", createdAtUtc);
        command.ExecuteNonQuery();

        return new ChatMessage
        {
            FromUser = fromUser,
            ToUser = toUser,
            Text = text,
            MessageType = "text",
            FileName = "",
            FileUrl = "",
            CreatedAtUtc = createdAtUtc
        };
    }

    public static ChatMessage SaveFileMessage(string fromUser, string toUser, string fileName, string fileUrl)
    {
        var createdAtUtc = DateTime.UtcNow.ToString("O");

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO messages (from_user, to_user, text, message_type, file_name, file_url, created_at_utc)
            VALUES ($fromUser, $toUser, NULL, 'file', $fileName, $fileUrl, $createdAtUtc)
        """;
        command.Parameters.AddWithValue("$fromUser", fromUser);
        command.Parameters.AddWithValue("$toUser", toUser);
        command.Parameters.AddWithValue("$fileName", fileName);
        command.Parameters.AddWithValue("$fileUrl", fileUrl);
        command.Parameters.AddWithValue("$createdAtUtc", createdAtUtc);
        command.ExecuteNonQuery();

        return new ChatMessage
        {
            FromUser = fromUser,
            ToUser = toUser,
            Text = "",
            MessageType = "file",
            FileName = fileName,
            FileUrl = fileUrl,
            CreatedAtUtc = createdAtUtc
        };
    }

    public static List<ChatMessage> GetHistory(string userA, string userB)
    {
        var result = new List<ChatMessage>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT from_user, to_user, COALESCE(text, ''), message_type, COALESCE(file_name, ''), COALESCE(file_url, ''), created_at_utc
            FROM messages
            WHERE
                (from_user = $userA AND to_user = $userB)
                OR
                (from_user = $userB AND to_user = $userA)
            ORDER BY id ASC
        """;
        command.Parameters.AddWithValue("$userA", userA);
        command.Parameters.AddWithValue("$userB", userB);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ChatMessage
            {
                FromUser = reader.GetString(0),
                ToUser = reader.GetString(1),
                Text = reader.GetString(2),
                MessageType = reader.GetString(3),
                FileName = reader.GetString(4),
                FileUrl = reader.GetString(5),
                CreatedAtUtc = reader.GetString(6)
            });
        }

        return result;
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

public class ChatMessage
{
    public string FromUser { get; set; } = "";
    public string ToUser { get; set; } = "";
    public string Text { get; set; } = "";
    public string MessageType { get; set; } = "text";
    public string FileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string CreatedAtUtc { get; set; } = "";
}

public class AuthResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";

    public AuthResult()
    {
    }

    public AuthResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}

public class UploadResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string Error { get; set; } = "";
}