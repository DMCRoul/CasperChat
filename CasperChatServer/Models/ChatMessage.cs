namespace CasperChat.Server.Models;

public class ChatMessage
{
    public string FromUser { get; set; } = "";
    public string ToUser { get; set; } = "";
    public string Text { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string MessageType { get; set; } = ""; // text / file
    public string CreatedAtUtc { get; set; } = "";
}