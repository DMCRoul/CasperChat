using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CasperChat.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace CasperChat.Client.Services
{
    public class ChatClientService
    {
        private HubConnection? _connection;

        public HubConnection? Connection => _connection;

        public bool IsConnected =>
        _connection != null &&
            _connection.State == HubConnectionState.Connected;

        public event Action<IEnumerable<string>>? UsersListReceived;
        public event Action<ChatMessage>? MessageReceived;

        public async Task ConnectAsync(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<IEnumerable<string>>("UsersList", users =>
            {
                UsersListReceived?.Invoke(users);
            });

            _connection.On<ChatMessage>("ReceiveMessage", message =>
            {
                MessageReceived?.Invoke(message);
            });

            await _connection.StartAsync();
        }

        public async Task SendMessageAsync(string selectedUser, string text)
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection is not initialized.");

            await _connection.InvokeAsync("SendMessage", selectedUser, text);
        }

        public async Task SendFileMessageAsync(string selectedUser, string fileName, string fileUrl)
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection is not initialized.");

            await _connection.InvokeAsync("SendFileMessage", selectedUser, fileName, fileUrl);
        }

        public async Task<List<ChatMessage>> GetHistoryAsync(string selectedUser)
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection is not initialized.");

            return await _connection.InvokeAsync<List<ChatMessage>>("GetHistory", selectedUser);
        }
    }
}