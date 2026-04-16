namespace CasperChat.Server.Models;

public class AuthResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";

    public AuthResult() { }

    public AuthResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}