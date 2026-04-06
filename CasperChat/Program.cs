using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;


internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ChatForm());
    }
}

public class ChatForm : Form
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    private HubConnection? connection;
    private string username = "";
    private string selectedUser = "";

    private readonly HttpClient httpClient = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5064")
    };

    private string BuildServerUrl(string relativeOrAbsoluteUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
            return "http://localhost:5064";

        if (relativeOrAbsoluteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeOrAbsoluteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return relativeOrAbsoluteUrl;

        return "http://localhost:5064" + relativeOrAbsoluteUrl;
    }

    private void ApplyCaptureProtectionToWindow(Form form)
    {
        if (form == null || form.IsDisposed)
            return;

        SetWindowDisplayAffinity(form.Handle, WDA_EXCLUDEFROMCAPTURE);
    }
    private bool IsImageFile(string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext == ".jpg" ||
               ext == ".jpeg" ||
               ext == ".png" ||
               ext == ".bmp" ||
               ext == ".gif" ||
               ext == ".webp";
    }

    private async Task PickAndSendDocument()
    {
        if (connection == null || connection.State != HubConnectionState.Connected)
        {
            AddSystemCard("Нет подключения к серверу");
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedUser))
        {
            AddSystemCard("Сначала выбери пользователя слева");
            return;
        }

        using OpenFileDialog dialog = new OpenFileDialog();
        dialog.Title = "Выберите файл";
        dialog.Filter = "Все файлы|*.*";

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            await using var fileStream = File.OpenRead(dialog.FileName);

            using var form = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);

            form.Add(streamContent, "file", Path.GetFileName(dialog.FileName));

            var uploadResponse = await httpClient.PostAsync("/upload", form);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                string errorText = await uploadResponse.Content.ReadAsStringAsync();
                AddSystemCard("Ошибка загрузки файла: " + errorText);
                return;
            }

            var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();

            if (uploadResult == null || !uploadResult.Success)
            {
                AddSystemCard("Ошибка загрузки файла");
                return;
            }

            await connection.InvokeAsync(
                "SendFileMessage",
                selectedUser,
                uploadResult.FileName,
                uploadResult.FileUrl
            );
        }
        catch (Exception ex)
        {
            AddSystemCard("Ошибка отправки файла: " + ex.Message);
        }
    }


    // ===== Цвета Gen X Soft Club =====
    private readonly Color AppBackColor = Color.FromArgb(28, 32, 38);
    private readonly Color SidebarColor = Color.FromArgb(36, 41, 48);
    private readonly Color SurfaceColor = Color.FromArgb(42, 47, 55);
    private readonly Color SurfaceAltColor = Color.FromArgb(48, 54, 63);
    private readonly Color SeparatorColor = Color.FromArgb(66, 72, 82);
    private readonly Color TextPrimary = Color.FromArgb(230, 232, 235);
    private readonly Color TextSecondary = Color.FromArgb(167, 173, 181);
    private readonly Color AccentSoft = Color.FromArgb(110, 140, 160);
    private readonly Color SelfMessageColor = Color.FromArgb(56, 64, 76);
    private readonly Color OtherMessageColor = Color.FromArgb(43, 48, 56);
    private readonly Color InputColor = Color.FromArgb(34, 39, 46);

    // ===== UI =====
    private readonly Panel topBar = new Panel();
    private readonly Panel sidebar = new Panel();
    private readonly Panel mainPanel = new Panel();
    private readonly Panel bottomBar = new Panel();
    private readonly Panel chatHeader = new Panel();

    private readonly Label appTitleLabel = new Label();
    private readonly Label currentUserLabel = new Label();
    private readonly Label usersTitleLabel = new Label();
    private readonly Label chatTitleLabel = new Label();
    private readonly Label chatSubtitleLabel = new Label();

    private readonly ListBox usersList = new ListBox();
    private readonly FlowLayoutPanel messagesPanel = new FlowLayoutPanel();

    private readonly TextBox inputBox = new TextBox();
    private readonly Button sendButton = new Button();
    private readonly Button attachButton = new Button();

    public ChatForm()
    {
        Text = "CasperChat";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 620);
        Size = new Size(1180, 760);
        BackColor = AppBackColor;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10f, FontStyle.Regular);

        BuildUi();
        WireEvents();
    }

    private void BuildUi()
    {
        // ===== Верхняя панель =====
        topBar.Dock = DockStyle.Top;
        topBar.Height = 56;
        topBar.BackColor = SidebarColor;
        topBar.Padding = new Padding(18, 10, 18, 10);

        appTitleLabel.Text = "CasperChat";
        appTitleLabel.ForeColor = TextPrimary;
        appTitleLabel.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
        appTitleLabel.AutoSize = true;
        appTitleLabel.Location = new Point(18, 16);

        currentUserLabel.Text = "offline";
        currentUserLabel.ForeColor = TextSecondary;
        currentUserLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        currentUserLabel.AutoSize = true;
        currentUserLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        currentUserLabel.Location = new Point(1040, 18);

        topBar.Controls.Add(appTitleLabel);
        topBar.Controls.Add(currentUserLabel);

        // ===== Боковая панель =====
        sidebar.Dock = DockStyle.Left;
        sidebar.Width = 280;
        sidebar.BackColor = SidebarColor;
        sidebar.Padding = new Padding(18, 18, 18, 18);

        usersTitleLabel.Text = "Пользователи";
        usersTitleLabel.ForeColor = TextPrimary;
        usersTitleLabel.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
        usersTitleLabel.Dock = DockStyle.Top;
        usersTitleLabel.Height = 28;

        usersList.Dock = DockStyle.Fill;
        usersList.BackColor = SurfaceColor;
        usersList.ForeColor = TextPrimary;
        usersList.BorderStyle = BorderStyle.None;
        usersList.Font = new Font("Segoe UI", 10f);
        usersList.ItemHeight = 28;
        usersList.IntegralHeight = false;

        sidebar.Controls.Add(usersList);
        sidebar.Controls.Add(usersTitleLabel);

        // ===== Правая часть =====
        mainPanel.Dock = DockStyle.Fill;
        mainPanel.BackColor = AppBackColor;
        mainPanel.Padding = new Padding(18, 18, 18, 18);

        // ===== Заголовок чата =====
        chatHeader.Dock = DockStyle.Top;
        chatHeader.Height = 72;
        chatHeader.BackColor = SurfaceColor;
        chatHeader.Padding = new Padding(18, 12, 18, 12);

        chatTitleLabel.Text = "Никто не выбран";
        chatTitleLabel.ForeColor = TextPrimary;
        chatTitleLabel.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
        chatTitleLabel.AutoSize = true;
        chatTitleLabel.Location = new Point(18, 12);

        chatSubtitleLabel.Text = "Спокойный защищённый чат";
        chatSubtitleLabel.ForeColor = TextSecondary;
        chatSubtitleLabel.Font = new Font("Segoe UI", 9f);
        chatSubtitleLabel.AutoSize = true;
        chatSubtitleLabel.Location = new Point(18, 40);

        chatHeader.Controls.Add(chatTitleLabel);
        chatHeader.Controls.Add(chatSubtitleLabel);

        // ===== Сообщения =====
        messagesPanel.Dock = DockStyle.Fill;
        messagesPanel.FlowDirection = FlowDirection.TopDown;
        messagesPanel.WrapContents = false;
        messagesPanel.AutoScroll = true;
        messagesPanel.BackColor = AppBackColor;
        messagesPanel.Padding = new Padding(0, 14, 0, 14);

        // ===== Нижняя панель =====
        bottomBar.Dock = DockStyle.Bottom;
        bottomBar.Height = 78;
        bottomBar.BackColor = AppBackColor;
        bottomBar.Padding = new Padding(0, 12, 0, 0);

        attachButton.Text = "＋";
        attachButton.Width = 54;
        attachButton.Dock = DockStyle.Left;
        attachButton.FlatStyle = FlatStyle.Flat;
        attachButton.FlatAppearance.BorderSize = 0;
        attachButton.BackColor = SurfaceColor;
        attachButton.ForeColor = TextPrimary;
        attachButton.Font = new Font("Segoe UI", 14f, FontStyle.Regular);
        attachButton.Cursor = Cursors.Hand;

        sendButton.Text = "Отправить";
        sendButton.Width = 130;
        sendButton.Dock = DockStyle.Right;
        sendButton.FlatStyle = FlatStyle.Flat;
        sendButton.FlatAppearance.BorderSize = 0;
        sendButton.BackColor = AccentSoft;
        sendButton.ForeColor = TextPrimary;
        sendButton.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
        sendButton.Cursor = Cursors.Hand;

        inputBox.Dock = DockStyle.Fill;
        inputBox.BorderStyle = BorderStyle.None;
        inputBox.BackColor = InputColor;
        inputBox.ForeColor = TextPrimary;
        inputBox.Font = new Font("Segoe UI", 11f);
        inputBox.Margin = new Padding(10);
        inputBox.PlaceholderText = "Введите сообщение...";

        var inputContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = InputColor,
            Padding = new Padding(16, 16, 16, 16)
        };

        inputContainer.Controls.Add(inputBox);

        bottomBar.Controls.Add(inputContainer);
        bottomBar.Controls.Add(sendButton);
        bottomBar.Controls.Add(attachButton);

        mainPanel.Controls.Add(messagesPanel);
        mainPanel.Controls.Add(bottomBar);
        mainPanel.Controls.Add(chatHeader);

        Controls.Add(mainPanel);
        Controls.Add(sidebar);
        Controls.Add(topBar);

        AddSystemCard("Клиент запущен");
    }

    private void WireEvents()
    {
        Shown += async (_, _) =>
        {
            ApplyCaptureProtection();
            await ConnectToServer();
        };

        sendButton.Click += async (_, _) => await SendTextMessage();

        inputBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendTextMessage();
            }
        };

        usersList.SelectedIndexChanged += async (_, _) =>
        {
            if (usersList.SelectedItem != null)
            {
                selectedUser = usersList.SelectedItem.ToString() ?? "";
                chatTitleLabel.Text = selectedUser;
                chatSubtitleLabel.Text = "Личная переписка";
                await LoadHistory();
            }
        };

        attachButton.Click += async (_, _) => await PickAndSendDocument();
        Resize += (_, _) =>
        {
            UpdateMessageWidths();
            UpdateTopBarUserLabel();
        };
    }

    private void UpdateTopBarUserLabel()
    {
        currentUserLabel.Text = string.IsNullOrWhiteSpace(username) ? "offline" : username;
        currentUserLabel.Left = topBar.Width - currentUserLabel.Width - 18;
    }

    private void ApplyCaptureProtection()
    {
        bool ok = SetWindowDisplayAffinity(this.Handle, WDA_EXCLUDEFROMCAPTURE);

        if (!ok)
        {
            int error = Marshal.GetLastWin32Error();
            AddSystemCard($"Не удалось включить защиту. Win32 Error = {error}");
        }
    }

    private async Task ConnectToServer()
    {
        connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5064/chat")
            .WithAutomaticReconnect()
            .Build();

        connection.On<IEnumerable<string>>("UsersList", users =>
        {
            Invoke(() =>
            {
                string? currentSelection = usersList.SelectedItem?.ToString();
                usersList.Items.Clear();

                foreach (var user in users)
                {
                    if (!string.Equals(user, username, StringComparison.OrdinalIgnoreCase))
                    {
                        usersList.Items.Add(user);
                    }
                }

                if (!string.IsNullOrWhiteSpace(currentSelection) && usersList.Items.Contains(currentSelection))
                {
                    usersList.SelectedItem = currentSelection;
                }
                else if (usersList.Items.Count > 0 && string.IsNullOrWhiteSpace(selectedUser))
                {
                    usersList.SelectedIndex = 0;
                }
            });
        });


        connection.On<ChatMessage>("ReceiveMessage", msg =>
        {
            Invoke(() =>
            {
                bool isCurrentChat =
                    (!string.IsNullOrWhiteSpace(selectedUser)) &&
                    (
                        (msg.FromUser == selectedUser && msg.ToUser == username) ||
                        (msg.FromUser == username && msg.ToUser == selectedUser)
                    );

                if (!isCurrentChat)
                    return;

                bool isMine = msg.FromUser == username;

                if (msg.MessageType == "file")
                {
                    if (IsImageFile(msg.FileName))
                    {
                        AddImageCard(
                            isMine ? "Вы" : msg.FromUser,
                            msg.FileName,
                            msg.FileUrl,
                            msg.CreatedAtUtc,
                            isMine
                        );
                    }
                    else
                    {
                        AddFileCard(
                            isMine ? "Вы" : msg.FromUser,
                            msg.FileName,
                            msg.FileUrl,
                            msg.CreatedAtUtc,
                            isMine
                        );
                    }
                }
                else
                {
                    AddMessageCard(
                        isMine ? "Вы" : msg.FromUser,
                        msg.Text,
                        msg.CreatedAtUtc,
                        isMine
                    );
                }
            });
        });

        try
        {
            await connection.StartAsync();
        }
        catch (Exception ex)
        {
            AddSystemCard("Ошибка подключения к серверу: " + ex.Message);
            Close();
            return;
        }

        var loginResult = await LoginPrompt.ShowDialogAsync(connection);

        if (loginResult == null)
        {
            Close();
            return;
        }

        username = loginResult.Username;
        currentUserLabel.Text = username;
        UpdateTopBarUserLabel();
        AddSystemCard($"Вы вошли как {username}");
    }

    private async Task SendTextMessage()
    {
        string text = inputBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (connection == null || connection.State != HubConnectionState.Connected)
        {
            AddSystemCard("Нет подключения к серверу");
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedUser))
        {
            AddSystemCard("Сначала выбери пользователя слева");
            return;
        }

        try
        {
            await connection.InvokeAsync("SendMessage", selectedUser, text);
            inputBox.Clear();
        }
        catch (Exception ex)
        {
            AddSystemCard("Ошибка отправки: " + ex.Message);
        }
    }

    private async Task LoadHistory()
    {
        if (connection == null || connection.State != HubConnectionState.Connected)
            return;

        if (string.IsNullOrWhiteSpace(selectedUser))
            return;

        try
        {
            var history = await connection.InvokeAsync<List<ChatMessage>>("GetHistory", selectedUser);

            messagesPanel.Controls.Clear();

            foreach (var msg in history)
            {
                bool isMine = msg.FromUser == username;

                if (msg.MessageType == "file")
                {
                    if (IsImageFile(msg.FileName))
                    {
                        AddImageCard(
                            isMine ? "Вы" : msg.FromUser,
                            msg.FileName,
                            msg.FileUrl,
                            msg.CreatedAtUtc,
                            isMine
                        );
                    }
                    else
                    {
                        AddFileCard(
                            isMine ? "Вы" : msg.FromUser,
                            msg.FileName,
                            msg.FileUrl,
                            msg.CreatedAtUtc,
                            isMine
                        );
                    }
                }
                else
                {
                    AddMessageCard(
                        isMine ? "Вы" : msg.FromUser,
                        msg.Text,
                        msg.CreatedAtUtc,
                        isMine
                    );
                }
            }
        }
        catch (Exception ex)
        {
            AddSystemCard("Ошибка загрузки истории: " + ex.Message);
        }
    }




    private void AddSystemCard(string text)
    {
        var wrapper = new Panel
        {
            Width = GetBubbleWidth(),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.Transparent
        };

        var label = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(GetBubbleWidth() - 40, 0),
            Text = text,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            BackColor = Color.Transparent,
            Padding = new Padding(12, 8, 12, 8)
        };

        wrapper.Controls.Add(label);
        messagesPanel.Controls.Add(wrapper);
        ScrollToBottom();
    }

    private void AddMessageCard(string sender, string text, string createdAtUtc, bool isMine)
    {
        var row = new Panel
        {
            Width = GetBubbleWidth(),
            Height = 10,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Color.Transparent
        };

        var bubble = new Panel
        {
            Width = Math.Min(640, Math.Max(280, GetBubbleWidth() - 120)),
            AutoSize = true,
            BackColor = isMine ? SelfMessageColor : OtherMessageColor,
            Padding = new Padding(14, 12, 14, 12)
        };

        var senderLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = sender,
            ForeColor = isMine ? TextPrimary : AccentSoft,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent
        };

        var timeLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = FormatTime(createdAtUtc),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8f),
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.Transparent
        };

        var textLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = Color.Transparent
        };

        bubble.Controls.Add(textLabel);
        bubble.Controls.Add(timeLabel);
        bubble.Controls.Add(senderLabel);

        senderLabel.Dock = DockStyle.Top;
        timeLabel.Dock = DockStyle.Top;
        textLabel.Dock = DockStyle.Top;

        if (isMine)
        {
            bubble.Left = row.Width - bubble.Width - 8;
        }
        else
        {
            bubble.Left = 8;
        }

        row.Controls.Add(bubble);
        messagesPanel.Controls.Add(row);
        ScrollToBottom();
    }

    private void AddFileCard(string sender, string fileName, string fileUrl, string createdAtUtc, bool isMine)
    {
        var row = new Panel
        {
            Width = GetBubbleWidth(),
            Height = 10,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Color.Transparent
        };

        var bubble = new Panel
        {
            Width = Math.Min(640, Math.Max(280, GetBubbleWidth() - 120)),
            AutoSize = true,
            BackColor = isMine ? SelfMessageColor : OtherMessageColor,
            Padding = new Padding(14, 12, 14, 12),
            Cursor = Cursors.Hand
        };

        var senderLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = sender,
            ForeColor = isMine ? TextPrimary : AccentSoft,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent
        };

        var timeLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = FormatTime(createdAtUtc),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8f),
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.Transparent
        };

        var fileLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = "📄 " + fileName,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        void OpenFile()
        {
            try
            {
                string url = BuildServerUrl(fileUrl);
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddSystemCard("Не удалось открыть файл: " + ex.Message);
            }
        }

        bubble.Click += (_, _) => OpenFile();
        fileLabel.Click += (_, _) => OpenFile();
        senderLabel.Click += (_, _) => OpenFile();
        timeLabel.Click += (_, _) => OpenFile();

        bubble.Controls.Add(fileLabel);
        bubble.Controls.Add(timeLabel);
        bubble.Controls.Add(senderLabel);

        senderLabel.Dock = DockStyle.Top;
        timeLabel.Dock = DockStyle.Top;
        fileLabel.Dock = DockStyle.Top;

        if (isMine)
            bubble.Left = row.Width - bubble.Width - 8;
        else
            bubble.Left = 8;

        row.Controls.Add(bubble);
        messagesPanel.Controls.Add(row);
        ScrollToBottom();
    }


    private void AddImageCard(string sender, string fileName, string fileUrl, string createdAtUtc, bool isMine)
    {
        var row = new Panel
        {
            Width = GetBubbleWidth(),
            Height = 10,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Color.Transparent
        };

        var bubble = new Panel
        {
            Width = Math.Min(520, Math.Max(280, GetBubbleWidth() - 180)),
            AutoSize = true,
            BackColor = isMine ? SelfMessageColor : OtherMessageColor,
            Padding = new Padding(14, 12, 14, 12),
            Cursor = Cursors.Hand
        };

        var senderLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = sender,
            ForeColor = isMine ? TextPrimary : AccentSoft,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var timeLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = FormatTime(createdAtUtc),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8f),
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var nameLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(bubble.Width - 24, 0),
            Text = fileName,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8.5f),
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var pictureBox = new PictureBox
        {
            Width = bubble.Width - 28,
            Height = 220,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 34, 40),
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };

        string imageUrl = BuildServerUrl(fileUrl);

        try
        {
            pictureBox.LoadAsync(imageUrl);
        }
        catch
        {
            // если не загрузилось, просто оставим пустое поле
        }

        void OpenViewer()
        {
            OpenImageViewer(fileName, imageUrl);
        }

        bubble.Click += (_, _) => OpenViewer();
        senderLabel.Click += (_, _) => OpenViewer();
        timeLabel.Click += (_, _) => OpenViewer();
        nameLabel.Click += (_, _) => OpenViewer();
        pictureBox.Click += (_, _) => OpenViewer();

        bubble.Controls.Add(pictureBox);
        bubble.Controls.Add(nameLabel);
        bubble.Controls.Add(timeLabel);
        bubble.Controls.Add(senderLabel);

        senderLabel.Dock = DockStyle.Top;
        timeLabel.Dock = DockStyle.Top;
        nameLabel.Dock = DockStyle.Top;
        pictureBox.Dock = DockStyle.Top;

        if (isMine)
            bubble.Left = row.Width - bubble.Width - 8;
        else
            bubble.Left = 8;

        row.Controls.Add(bubble);
        messagesPanel.Controls.Add(row);
        ScrollToBottom();
    }

    private void OpenImageViewer(string fileName, string imageUrl)
    {
        Form viewer = new Form
        {
            Text = fileName,
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(1000, 700),
            MinimumSize = new Size(700, 500),
            BackColor = AppBackColor,
            ForeColor = TextPrimary
        };

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = SidebarColor,
            Padding = new Padding(16, 12, 16, 12)
        };

        var titleLabel = new Label
        {
            Text = fileName,
            Dock = DockStyle.Fill,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
            AutoEllipsis = true
        };

        var pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = AppBackColor
        };

        try
        {
            pictureBox.LoadAsync(imageUrl);
        }
        catch
        {
        }

        topPanel.Controls.Add(titleLabel);
        viewer.Controls.Add(pictureBox);
        viewer.Controls.Add(topPanel);

        // Очень важно:
        // защита ставится после создания и показа окна,
        // когда у него уже есть валидный Handle.
        viewer.Shown += (_, _) => ApplyCaptureProtectionToWindow(viewer);

        viewer.ShowDialog(this);
    }
    private string FormatTime(string utcTime)
    {
        if (DateTime.TryParse(utcTime, out var dt))
            return dt.ToLocalTime().ToString("dd.MM.yyyy  HH:mm");

        return utcTime;
    }

    private int GetBubbleWidth()
    {
        return Math.Max(320, messagesPanel.ClientSize.Width - 35);
    }

    private void UpdateMessageWidths()
    {
        foreach (Control row in messagesPanel.Controls)
        {
            row.Width = GetBubbleWidth();

            foreach (Control child in row.Controls)
            {
                if (child is Panel bubble)
                {
                    bubble.Width = Math.Min(640, Math.Max(280, GetBubbleWidth() - 120));

                    foreach (Control inner in bubble.Controls)
                    {
                        if (inner is Label lbl)
                        {
                            lbl.MaximumSize = new Size(bubble.Width - 24, 0);
                        }
                    }

                    bool isMine = bubble.BackColor == SelfMessageColor;
                    bubble.Left = isMine ? row.Width - bubble.Width - 8 : 8;
                }
            }
        }
    }

    private void ScrollToBottom()
    {
        if (messagesPanel.Controls.Count == 0)
            return;

        messagesPanel.ScrollControlIntoView(messagesPanel.Controls[messagesPanel.Controls.Count - 1]);
    }
}

public class UploadResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string Error { get; set; } = "";
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
}

public class LoginSuccessData
{
    public string Username { get; set; } = "";
}

public static class LoginPrompt
{
    public static async Task<LoginSuccessData?> ShowDialogAsync(HubConnection connection)
    {
        LoginSuccessData? result = null;

        Color bgColor = Color.FromArgb(28, 32, 38);
        Color panelColor = Color.FromArgb(42, 47, 55);
        Color inputColor = Color.FromArgb(34, 39, 46);
        Color textPrimary = Color.FromArgb(230, 232, 235);
        Color textSecondary = Color.FromArgb(167, 173, 181);
        Color accentSoft = Color.FromArgb(110, 140, 160);
        Color errorColor = Color.FromArgb(198, 146, 146);

        Form prompt = new Form
        {
            Text = "Вход / регистрация",
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            BackColor = bgColor,
            ForeColor = textPrimary,
            ClientSize = new Size(560, 420),
            AutoScaleMode = AutoScaleMode.Dpi
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 8,
            BackColor = bgColor
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // title
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // subtitle
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // user label
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // user box
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // pass label
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // pass box
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // error filler
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // buttons

        var titleLabel = new Label
        {
            Text = "CasperChat",
            ForeColor = textPrimary,
            Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var subtitleLabel = new Label
        {
            Text = "Вход для существующего аккаунта или регистрация нового пользователя",
            ForeColor = textSecondary,
            Font = new Font("Segoe UI", 10f),
            Dock = DockStyle.Fill
        };

        var userLabel = new Label
        {
            Text = "Имя пользователя",
            ForeColor = textSecondary,
            Font = new Font("Segoe UI", 10f),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };

        var userPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = inputColor,
            Padding = new Padding(14, 12, 14, 8),
            Margin = new Padding(0, 0, 0, 8)
        };

        var userBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = inputColor,
            ForeColor = textPrimary,
            Font = new Font("Segoe UI", 11f)
        };
        userPanel.Controls.Add(userBox);

        var passLabel = new Label
        {
            Text = "Пароль",
            ForeColor = textSecondary,
            Font = new Font("Segoe UI", 10f),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };

        var passPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = inputColor,
            Padding = new Padding(14, 12, 14, 8),
            Margin = new Padding(0, 0, 0, 8)
        };

        var passBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = inputColor,
            ForeColor = textPrimary,
            Font = new Font("Segoe UI", 11f),
            UseSystemPasswordChar = true
        };
        passPanel.Controls.Add(passBox);

        var errorLabel = new Label
        {
            Text = "",
            ForeColor = errorColor,
            Font = new Font("Segoe UI", 9.5f),
            Dock = DockStyle.Fill
        };

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = bgColor
        };

        var registerButton = new Button
        {
            Text = "Регистрация",
            Width = 130,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = accentSoft,
            ForeColor = textPrimary,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Margin = new Padding(10, 6, 0, 0)
        };
        registerButton.FlatAppearance.BorderSize = 0;

        var loginButton = new Button
        {
            Text = "Войти",
            Width = 110,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = panelColor,
            ForeColor = textPrimary,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Margin = new Padding(10, 6, 0, 0)
        };
        loginButton.FlatAppearance.BorderSize = 0;

        async Task TryAuth(bool isRegister)
        {
            string username = userBox.Text.Trim();
            string password = passBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                errorLabel.Text = "Введите имя пользователя";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errorLabel.Text = "Введите пароль";
                return;
            }

            loginButton.Enabled = false;
            registerButton.Enabled = false;
            errorLabel.Text = "Проверка...";

            try
            {
                AuthResult authResult = isRegister
                    ? await connection.InvokeAsync<AuthResult>("Register", username, password)
                    : await connection.InvokeAsync<AuthResult>("Login", username, password);

                if (!authResult.Success)
                {
                    errorLabel.Text = authResult.Message;
                    return;
                }

                result = new LoginSuccessData
                {
                    Username = username
                };

                prompt.DialogResult = DialogResult.OK;
                prompt.Close();
            }
            catch (Exception ex)
            {
                errorLabel.Text = ex.Message;
            }
            finally
            {
                if (!prompt.IsDisposed)
                {
                    loginButton.Enabled = true;
                    registerButton.Enabled = true;
                }
            }
        }

        loginButton.Click += async (_, _) => await TryAuth(false);
        registerButton.Click += async (_, _) => await TryAuth(true);

        passBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await TryAuth(false);
            }
        };

        buttonsPanel.Controls.Add(registerButton);
        buttonsPanel.Controls.Add(loginButton);

        root.Controls.Add(titleLabel, 0, 0);
        root.Controls.Add(subtitleLabel, 0, 1);
        root.Controls.Add(userLabel, 0, 2);
        root.Controls.Add(userPanel, 0, 3);
        root.Controls.Add(passLabel, 0, 4);
        root.Controls.Add(passPanel, 0, 5);
        root.Controls.Add(errorLabel, 0, 6);
        root.Controls.Add(buttonsPanel, 0, 7);

        prompt.Controls.Add(root);
        prompt.ShowDialog();

        return result;
    }
}