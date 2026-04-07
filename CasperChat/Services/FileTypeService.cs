using System.IO;

namespace CasperChat.Client.Services
{
    public static class FileTypeService
    {
        public static bool IsImageFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            string ext = Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(ext))
                return false;

            ext = ext.ToLowerInvariant();

            return ext == ".jpg"
                || ext == ".jpeg"
                || ext == ".png"
                || ext == ".bmp"
                || ext == ".gif"
                || ext == ".webp";
        }
    }
}