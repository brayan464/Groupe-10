using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace EditeurWpf
{
    public class ChatManager
    {
        private StackPanel chatPanel;
        private ScrollViewer chatScrollViewer;

        public ChatManager(StackPanel panel, ScrollViewer scrollViewer)
        {
            chatPanel = panel;
            chatScrollViewer = scrollViewer;
        }

        public void AddMessage(string message, bool isUser)
        {
            Border messageBorder = new Border
            {
                Background = new SolidColorBrush(isUser ?
                    Color.FromRgb(0, 120, 215) :
                    Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(5),
                HorizontalAlignment = isUser ?
                    HorizontalAlignment.Right :
                    HorizontalAlignment.Left,
                MaxWidth = 280
            };

            TextBlock textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            messageBorder.Child = textBlock;
            chatPanel.Children.Add(messageBorder);

            chatScrollViewer.ScrollToBottom();
        }

        public void ClearChat()
        {
            chatPanel.Children.Clear();
        }
    }
}