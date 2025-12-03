using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace EditeurWpf
{
    public class StyleManager
    {
        private RichTextBox editor;

        public StyleManager(RichTextBox editorControl)
        {
            editor = editorControl;
        }

        public void ApplyStyle(string styleName)
        {
            if (editor.Selection.IsEmpty) return;

            switch (styleName)
            {
                case "Normal":
                    editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
                    editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                    break;

                case "Heading1":
                    editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 24.0);
                    editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                    editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkBlue);
                    break;

                case "Heading2":
                    editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 18.0);
                    editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                    editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkSlateGray);
                    break;

                case "Heading3":
                    editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 14.0);
                    editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                    break;

                case "Title":
                    editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 28.0);
                    editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                    EditingCommands.AlignCenter.Execute(null, editor);
                    break;

                case "Subtitle":
                    editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 16.0);
                    editor.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
                    EditingCommands.AlignCenter.Execute(null, editor);
                    break;

                case "Quote":
                    editor.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
                    editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);
                    editor.Selection.ApplyPropertyValue(Paragraph.TextIndentProperty, 20.0);
                    break;
            }
        }

        public void ChangeFontSize(double size)
        {
            if (!editor.Selection.IsEmpty)
            {
                editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
            }
        }

        public void ChangeFont()
        {
            // Dialog simple pour choisir la police
            var dialog = new Window
            {
                Title = "Choisir une police",
                Width = 300,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var panel = new StackPanel { Margin = new Thickness(10) };
            var listBox = new ListBox { Height = 300 };

            // Ajouter les polices courantes
            string[] fonts = { "Arial", "Calibri", "Times New Roman", "Verdana",
                             "Georgia", "Courier New", "Comic Sans MS", "Impact" };
            foreach (var font in fonts)
            {
                listBox.Items.Add(font);
            }

            var okButton = new Button
            {
                Content = "OK",
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(20, 5, 20, 5)
            };

            okButton.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string fontName = listBox.SelectedItem.ToString() ?? "Calibri";
                    editor.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty,
                        new FontFamily(fontName));
                    dialog.Close();
                }
            };

            panel.Children.Add(new Label { Content = "Sélectionnez une police:" });
            panel.Children.Add(listBox);
            panel.Children.Add(okButton);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        public void ChangeTextColor()
        {
            var dialog = new ColorPickerDialog();
            if (dialog.ShowDialog() == true)
            {
                editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty,
                    new SolidColorBrush(dialog.SelectedColor));
            }
        }

        public void HighlightText()
        {
            var dialog = new ColorPickerDialog();
            if (dialog.ShowDialog() == true)
            {
                editor.Selection.ApplyPropertyValue(TextElement.BackgroundProperty,
                    new SolidColorBrush(dialog.SelectedColor));
            }
        }

        public void ToggleBulletList()
        {
            EditingCommands.ToggleBullets.Execute(null, editor);
        }

        public void ToggleNumberedList()
        {
            EditingCommands.ToggleNumbering.Execute(null, editor);
        }
    }

    // Dialog simple pour choisir une couleur
    public class ColorPickerDialog : Window
    {
        public Color SelectedColor { get; private set; } = Colors.Black;

        public ColorPickerDialog()
        {
            Title = "Choisir une couleur";
            Width = 300;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var panel = new StackPanel { Margin = new Thickness(10) };

            // Couleurs prédéfinies
            Color[] colors = {
                Colors.Black, Colors.Red, Colors.Blue, Colors.Green,
                Colors.Yellow, Colors.Orange, Colors.Purple, Colors.Pink,
                Colors.Gray, Colors.Brown, Colors.Cyan, Colors.Magenta
            };

            var grid = new UniformGrid { Columns = 4, Rows = 3 };

            foreach (var color in colors)
            {
                var button = new Button
                {
                    Background = new SolidColorBrush(color),
                    Width = 60,
                    Height = 40,
                    Margin = new Thickness(5)
                };
                button.Click += (s, e) =>
                {
                    SelectedColor = color;
                    DialogResult = true;
                    Close();
                };
                grid.Children.Add(button);
            }

            panel.Children.Add(new Label { Content = "Sélectionnez une couleur:" });
            panel.Children.Add(grid);
            Content = panel;
        }
    }
}