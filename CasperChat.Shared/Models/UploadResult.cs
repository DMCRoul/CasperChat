namespace CasperChat.Shared.Models
{
    public class UploadResult
    {
        public bool Success { get; set; }
        public string FileName { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public string Error { get; set; } = "";
    }
}