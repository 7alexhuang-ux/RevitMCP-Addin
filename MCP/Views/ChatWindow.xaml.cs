using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RevitMCP.Services;
using RevitMCP.Tools;
using RevitMCP.Utils;

namespace RevitMCP.Views
{
    public partial class ChatWindow : Window
    {
        private ChatService _chatService;
        private string _currentSessionId;
        private ObservableCollection<ChatSession> _history;

        public ChatWindow(RevitEvent revitEvent, ToolHandler toolHandler)
        {
            InitializeComponent();
            _chatService = new ChatService(revitEvent, toolHandler);
            _history = new ObservableCollection<ChatSession>();
            HistoryList.ItemsSource = _history;

            Loaded += ChatWindow_Loaded;
        }

        private async void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadChatHistory();
                InitializeModelCombo();
            }
            catch (Exception ex)
            {
                AddMessageToChat("error", $"Error loading chat: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadChatHistory()
        {
            try
            {
                var sessions = await _chatService.GetChatHistory();
                
                await this.Dispatcher.InvokeAsync(() =>
                {
                    _history.Clear();
                    foreach (var session in sessions)
                    {
                        _history.Add(session);
                    }
                });
            }
            catch (Exception ex)
            {
                // Log error silently or show toast
            }
        }

        private void InitializeModelCombo()
        {
            // Default to Gemini
            ProviderCombo.SelectedIndex = 0; 
            UpdateModelList("Gemini");
        }

        private async void UpdateModelList(string provider)
        {
            try
            {
                await this.Dispatcher.InvokeAsync(() =>
                {
                    ModelCombo.Items.Clear();
                });

                if (provider == "Gemini")
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        ModelCombo.Items.Add("auto");
                        ModelCombo.Items.Add("gemini-2.5-flash");
                        ModelCombo.Items.Add("gemini-2.5-pro");
                        ModelCombo.SelectedIndex = 0;
                    });
                }
                else if (provider == "Ollama")
                {
                    var models = await _chatService.GetOllamaModels();
                    
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        if (models.Count > 0)
                        {
                            foreach (var m in models) ModelCombo.Items.Add(m);
                            ModelCombo.SelectedIndex = 0;
                        }
                        else
                        {
                            ModelCombo.Items.Add("No models found");
                            ModelCombo.SelectedIndex = 0;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                 AddMessageToChat("error", $"Error updating models: {ex.Message}");
            }
        }

        private void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderCombo.SelectedItem is ComboBoxItem item)
            {
                string content = item.Content.ToString();
                if (content.Contains("Gemini")) UpdateModelList("Gemini");
                else if (content.Contains("Ollama")) UpdateModelList("Ollama");
            }
        }

        private async void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentSessionId = null;
                ChatPanel.Children.Clear();
                ChatTitle.Text = "New Chat";
                
                var session = await _chatService.CreateNewChat();
                _currentSessionId = session.id;
                await LoadChatHistory(); 
            }
            catch (Exception ex)
            {
                AddMessageToChat("error", $"Error creating new chat: {ex.Message}");
            }
        }

        private async void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (HistoryList.SelectedItem is ChatSession session)
                {
                    _currentSessionId = session.id;
                    ChatTitle.Text = string.IsNullOrEmpty(session.title) ? "Chat" : session.title;
                    ChatPanel.Children.Clear();

                    var fullSession = await _chatService.LoadChatSession(session.id);
                    
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        if (fullSession != null && fullSession.messages != null)
                        {
                            foreach (var msg in fullSession.messages)
                            {
                                string text = "";
                                if (msg.parts != null && msg.parts.Count > 0)
                                    text = msg.parts[0].text;
                                
                                AddMessageToChat(msg.role, text);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                AddMessageToChat("error", $"Error loading history: {ex.Message}");
            }
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            try
            {
                string userText = InputBox.Text.Trim();
                if (string.IsNullOrEmpty(userText)) return;

                if (string.IsNullOrEmpty(_currentSessionId))
                {
                    var session = await _chatService.CreateNewChat();
                    _currentSessionId = session.id;
                    await LoadChatHistory();
                }

                InputBox.Text = "";
                AddMessageToChat("user", userText);

                string provider = "gemini";
                if (ProviderCombo.SelectedItem is ComboBoxItem item && item.Content.ToString().Contains("Ollama"))
                    provider = "ollama";
                
                string model = ModelCombo.SelectedItem?.ToString() ?? "auto";

                string response = await _chatService.SendMessageAsync(userText, provider, model, _currentSessionId);
                
                await this.Dispatcher.InvokeAsync(() =>
                {
                    AddMessageToChat("model", response);
                });
                
                if (ChatPanel.Children.Count <= 2) await LoadChatHistory();
            }
            catch (Exception ex)
            {
                await this.Dispatcher.InvokeAsync(() =>
                {
                    AddMessageToChat("error", $"Error: {ex.Message}");
                });
            }
        }

        private void AddMessageToChat(string role, string text)
        {
            // Ensure UI update is on UI thread
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.InvokeAsync(() => AddMessageToChat(role, text));
                return;
            }

            var border = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(5),
                MaxWidth = 600,
                HorizontalAlignment = role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Background = role == "user" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC")) : 
                             role == "error" ? Brushes.Red :
                             new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"))
            };

            var textBox = new TextBox
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Foreground = role == "user" ? Brushes.White : Brushes.Black,
                FontSize = 14,
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.IBeam,
                IsHitTestVisible = true
            };

            border.Child = textBox;
            ChatPanel.Children.Add(border);
            ChatScrollViewer.ScrollToBottom();
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Language switching not implemented yet.");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings not implemented yet.");
        }
    }
}
