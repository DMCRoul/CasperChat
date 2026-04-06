namespace CasperChat.Shared.Models
{
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
}