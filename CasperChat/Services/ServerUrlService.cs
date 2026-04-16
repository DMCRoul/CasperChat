using System;

namespace CasperChat.Client.Services
{
    public static class ServerUrlService
    {
        private const string BaseUrl = "http://127.0.0.1:5064";
        private const string ChatHubPath = "/chat";

        public static string GetBaseUrl()
        {
            return BaseUrl;
        }

        public static string GetHubUrl()
        {
            return BaseUrl + ChatHubPath;
        }

        public static string GetAbsoluteUrl(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
                return BaseUrl;

            if (Uri.IsWellFormedUriString(relativeOrAbsolutePath, UriKind.Absolute))
                return relativeOrAbsolutePath;

            if (!relativeOrAbsolutePath.StartsWith("/"))
                relativeOrAbsolutePath = "/" + relativeOrAbsolutePath;

            return BaseUrl + relativeOrAbsolutePath;
        }
    }
}