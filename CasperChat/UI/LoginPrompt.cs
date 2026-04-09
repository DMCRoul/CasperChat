using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using CasperChat.Shared.Models;
using CasperChat.Client.Services;

namespace CasperChat.Client.UI
{
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

            prompt.Shown += (_, _) =>
            {
                CaptureProtectionService.Apply(prompt);
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 8,
                BackColor = bgColor
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

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
}