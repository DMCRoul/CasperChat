using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using CasperChat.Client.Services;
using CasperChat.Client.UI;
using CasperChat.Shared.Models;

namespace CasperChat.Client.Forms
{
    public class ChatForm : Form
    {
        private readonly ChatClientService chatClientService = new ChatClientService();
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private string username = "";
        private string selectedUser = "";

        private readonly HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri(ServerUrlService.GetBaseUrl())
        };

        private readonly FileUploadService fileUploadService;

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
            ShowInTaskbar = false;
            InitializeTray();
            fileUploadService = new FileUploadService(httpClient);

            Text = "CasperChat";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 620);
            Size = new Size(1180, 760);
            BackColor = Theme.AppBackColor;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            BuildUi();
            WireEvents();
        }

        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();

            trayMenu.Items.Add("Открыть", null, (_, _) =>
            {
                Show();
                WindowState = FormWindowState.Normal;
            });

            trayMenu.Items.Add("Выход", null, (_, _) =>
            {
                trayIcon.Visible = false;
                Application.Exit();
            });

            trayIcon = new NotifyIcon
            {
                Text = "CasperChat",
                Icon = SystemIcons.Application, // позже заменим на свою
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += (_, _) =>
            {
                Show();
                WindowState = FormWindowState.Normal;
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }

            base.OnFormClosing(e);
        }

        private async Task PickAndSendDocument()
        {
            if (!chatClientService.IsConnected)
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
                var uploadResult = await fileUploadService.UploadFileAsync(dialog.FileName);

                if (uploadResult == null || !uploadResult.Success)
                {
                    AddSystemCard("Ошибка загрузки файла");
                    return;
                }

                await chatClientService.SendFileMessageAsync(
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

        private void BuildUi()
        {
            topBar.Dock = DockStyle.Top;
            topBar.Height = 56;
            topBar.BackColor = Theme.SidebarColor;
            topBar.Padding = new Padding(18, 10, 18, 10);

            appTitleLabel.Text = "CasperChat";
            appTitleLabel.ForeColor = Theme.TextPrimary;
            appTitleLabel.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
            appTitleLabel.AutoSize = true;
            appTitleLabel.Location = new Point(18, 16);

            currentUserLabel.Text = "offline";
            currentUserLabel.ForeColor = Theme.TextSecondary;
            currentUserLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            currentUserLabel.AutoSize = true;
            currentUserLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            currentUserLabel.Location = new Point(1040, 18);

            topBar.Controls.Add(appTitleLabel);
            topBar.Controls.Add(currentUserLabel);

            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 280;
            sidebar.BackColor = Theme.SidebarColor;
            sidebar.Padding = new Padding(18, 18, 18, 18);

            usersTitleLabel.Text = "Пользователи";
            usersTitleLabel.ForeColor = Theme.TextPrimary;
            usersTitleLabel.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
            usersTitleLabel.Dock = DockStyle.Top;
            usersTitleLabel.Height = 28;

            usersList.Dock = DockStyle.Fill;
            usersList.BackColor = Theme.SurfaceColor;
            usersList.ForeColor = Theme.TextPrimary;
            usersList.BorderStyle = BorderStyle.None;
            usersList.Font = new Font("Segoe UI", 10f);
            usersList.ItemHeight = 28;
            usersList.IntegralHeight = false;

            sidebar.Controls.Add(usersList);
            sidebar.Controls.Add(usersTitleLabel);

            mainPanel.Dock = DockStyle.Fill;
            mainPanel.BackColor = Theme.AppBackColor;
            mainPanel.Padding = new Padding(18, 18, 18, 18);

            chatHeader.Dock = DockStyle.Top;
            chatHeader.Height = 72;
            chatHeader.BackColor = Theme.SurfaceColor;
            chatHeader.Padding = new Padding(18, 12, 18, 12);

            chatTitleLabel.Text = "Никто не выбран";
            chatTitleLabel.ForeColor = Theme.TextPrimary;
            chatTitleLabel.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            chatTitleLabel.AutoSize = true;
            chatTitleLabel.Location = new Point(18, 12);

            chatSubtitleLabel.Text = "Спокойный защищённый чат";
            chatSubtitleLabel.ForeColor = Theme.TextSecondary;
            chatSubtitleLabel.Font = new Font("Segoe UI", 9f);
            chatSubtitleLabel.AutoSize = true;
            chatSubtitleLabel.Location = new Point(18, 40);

            chatHeader.Controls.Add(chatTitleLabel);
            chatHeader.Controls.Add(chatSubtitleLabel);

            messagesPanel.Dock = DockStyle.Fill;
            messagesPanel.FlowDirection = FlowDirection.TopDown;
            messagesPanel.WrapContents = false;
            messagesPanel.AutoScroll = true;
            messagesPanel.BackColor = Theme.AppBackColor;
            messagesPanel.Padding = new Padding(0, 14, 0, 14);

            bottomBar.Dock = DockStyle.Bottom;
            bottomBar.Height = 78;
            bottomBar.BackColor = Theme.AppBackColor;
            bottomBar.Padding = new Padding(0, 12, 0, 0);

            attachButton.Text = "＋";
            attachButton.Width = 54;
            attachButton.Dock = DockStyle.Left;
            attachButton.FlatStyle = FlatStyle.Flat;
            attachButton.FlatAppearance.BorderSize = 0;
            attachButton.BackColor = Theme.SurfaceColor;
            attachButton.ForeColor = Theme.TextPrimary;
            attachButton.Font = new Font("Segoe UI", 14f, FontStyle.Regular);
            attachButton.Cursor = Cursors.Hand;

            sendButton.Text = "Отправить";
            sendButton.Width = 130;
            sendButton.Dock = DockStyle.Right;
            sendButton.FlatStyle = FlatStyle.Flat;
            sendButton.FlatAppearance.BorderSize = 0;
            sendButton.BackColor = Theme.AccentSoft;
            sendButton.ForeColor = Theme.TextPrimary;
            sendButton.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            sendButton.Cursor = Cursors.Hand;

            inputBox.Dock = DockStyle.Fill;
            inputBox.BorderStyle = BorderStyle.None;
            inputBox.BackColor = Theme.InputColor;
            inputBox.ForeColor = Theme.TextPrimary;
            inputBox.Font = new Font("Segoe UI", 11f);
            inputBox.Margin = new Padding(10);
            inputBox.PlaceholderText = "Введите сообщение...";

            var inputContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.InputColor,
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
            bool ok = CaptureProtectionService.Apply(this);

            if (!ok)
            {
                AddSystemCard("Не удалось включить защиту окна");
            }
        }

        private async Task ConnectToServer()
        {
            chatClientService.UsersListReceived += users =>
            {
                Invoke(() =>
                {
                    string currentSelection = usersList.SelectedItem?.ToString();
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
            };

            chatClientService.MessageReceived += msg =>
            {
                Invoke(() =>
                {
                    bool isCurrentChat =
                        !string.IsNullOrWhiteSpace(selectedUser) &&
                        (
                            (msg.FromUser == selectedUser && msg.ToUser == username) ||
                            (msg.FromUser == username && msg.ToUser == selectedUser)
                        );

                    if (!isCurrentChat)
                        return;

                    bool isMine = msg.FromUser == username;

                    if (msg.MessageType == "file")
                    {
                        if (FileTypeService.IsImageFile(msg.FileName))
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
            };

            try
            {
                await chatClientService.ConnectAsync(ServerUrlService.GetHubUrl());
            }
            catch (Exception ex)
            {
                AddSystemCard("Ошибка подключения к серверу: " + ex.Message);
                Close();
                return;
            }

            if (chatClientService.Connection == null)
            {
                AddSystemCard("Не удалось создать подключение");
                Close();
                return;
            }

            var loginResult = await LoginPrompt.ShowDialogAsync(chatClientService.Connection);

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

            if (!chatClientService.IsConnected)
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
                await chatClientService.SendMessageAsync(selectedUser, text);
                inputBox.Clear();
            }
            catch (Exception ex)
            {
                AddSystemCard("Ошибка отправки: " + ex.Message);
            }
        }

        private async Task LoadHistory()
        {
            if (!chatClientService.IsConnected)
                return;

            if (string.IsNullOrWhiteSpace(selectedUser))
                return;

            try
            {
                var history = await chatClientService.GetHistoryAsync(selectedUser);
                messagesPanel.Controls.Clear();

                foreach (var msg in history)
                {
                    bool isMine = msg.FromUser == username;

                    if (msg.MessageType == "file")
                    {
                        if (FileTypeService.IsImageFile(msg.FileName))
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
                ForeColor = Theme.TextSecondary,
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
                BackColor = isMine ? Theme.SelfMessageColor : Theme.OtherMessageColor,
                Padding = new Padding(14, 12, 14, 12)
            };

            var senderLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(bubble.Width - 24, 0),
                Text = sender,
                ForeColor = isMine ? Theme.TextPrimary : Theme.AccentSoft,
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Color.Transparent
            };

            var timeLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(bubble.Width - 24, 0),
                Text = FormatTime(createdAtUtc),
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 8f),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.Transparent
            };

            var textLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(bubble.Width - 24, 0),
                Text = text,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Color.Transparent
            };

            bubble.Controls.Add(textLabel);
            bubble.Controls.Add(timeLabel);
            bubble.Controls.Add(senderLabel);

            senderLabel.Dock = DockStyle.Top;
            timeLabel.Dock = DockStyle.Top;
            textLabel.Dock = DockStyle.Top;

            bubble.Left = isMine ? row.Width - bubble.Width - 8 : 8;

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
                BackColor = isMine ? Theme.SelfMessageColor : Theme.OtherMessageColor,
                Padding = new Padding(14, 12, 14, 12),
                Cursor = Cursors.Hand
            };

            var senderLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(bubble.Width - 24, 0),
                Text = sender,
                ForeColor = isMine ? Theme.TextPrimary : Theme.AccentSoft,
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Color.Transparent
            };

            var timeLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(bubble.Width - 24, 0),
                Text = FormatTime(createdAtUtc),
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 8f),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.Transparent
            };

            var fileLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(bubble.Width - 24, 0),
                Text = "📄 " + fileName,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            void OpenFile()
            {
                try
                {
                    string url = ServerUrlService.GetAbsoluteUrl(fileUrl);
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

            bubble.Left = isMine ? row.Width - bubble.Width - 8 : 8;

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
                BackColor = isMine ? Theme.SelfMessageColor : Theme.OtherMessageColor,
                Padding = new Padding(14, 12, 14, 12),
                Cursor = Cursors.Hand
            };

            var senderLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(bubble.Width - 24, 0),
                Text = sender,
                ForeColor = isMine ? Theme.TextPrimary : Theme.AccentSoft,
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
                ForeColor = Theme.TextSecondary,
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
                ForeColor = Theme.TextSecondary,
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

            string imageUrl = ServerUrlService.GetAbsoluteUrl(fileUrl);

            try
            {
                pictureBox.LoadAsync(imageUrl);
            }
            catch
            {
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

            bubble.Left = isMine ? row.Width - bubble.Width - 8 : 8;

            row.Controls.Add(bubble);
            messagesPanel.Controls.Add(row);
            ScrollToBottom();
        }

        private void OpenImageViewer(string fileName, string imageUrl)
        {
            using var viewer = new ImageViewerForm(fileName, imageUrl);
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
                        bool looksLikeImageBubble = false;

                        foreach (Control inner in bubble.Controls)
                        {
                            if (inner is PictureBox)
                            {
                                looksLikeImageBubble = true;
                                break;
                            }
                        }

                        bubble.Width = looksLikeImageBubble
                            ? Math.Min(520, Math.Max(280, GetBubbleWidth() - 180))
                            : Math.Min(640, Math.Max(280, GetBubbleWidth() - 120));

                        foreach (Control inner in bubble.Controls)
                        {
                            if (inner is Label lbl)
                            {
                                lbl.MaximumSize = new Size(bubble.Width - 24, 0);
                            }

                            if (inner is PictureBox pb)
                            {
                                pb.Width = bubble.Width - 28;
                            }
                        }

                        bool isMine = bubble.BackColor == Theme.SelfMessageColor;
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
}