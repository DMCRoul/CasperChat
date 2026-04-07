using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CasperChat.Shared.Models;

namespace CasperChat.Client.Services
{
    public class FileUploadService
    {
        private readonly HttpClient _httpClient;

        public FileUploadService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<UploadResult?> UploadFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            await using var fileStream = File.OpenRead(filePath);
            using var form = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);

            form.Add(streamContent, "file", Path.GetFileName(filePath));

            var uploadResponse = await _httpClient.PostAsync("/upload", form);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                string errorText = await uploadResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException("Upload failed: " + errorText);
            }

            return await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();
        }
    }
}