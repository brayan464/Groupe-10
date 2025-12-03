using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace EditeurWpf
{
    public class PageManager
    {
        private RichTextBox editor;
        private Label pageCountLabel;
        private Label wordCountLabel;
        private Label charCountLabel;

        private double pageWidth = 816; // A4: 21cm = 816px
        private double pageHeight = 1056; // A4: 27.7cm = 1056px
        private Thickness margins = new Thickness(96, 96, 96, 96); // 2.5cm marges

        // En-têtes et pieds de page
        private string headerText = "";
        private string footerText = "";
        private bool showHeader = false;
        private bool showFooter = false;
        private bool showPageNumbers = false;

        public PageManager(RichTextBox editorControl, Label pageLabel, Label wordLabel, Label charLabel)
        {
            editor = editorControl;
            pageCountLabel = pageLabel;
            wordCountLabel = wordLabel;
            charCountLabel = charLabel;

            ConfigureEditor();
        }

        private void ConfigureEditor()
        {
            editor.Document.PageWidth = pageWidth;
            editor.Document.PageHeight = pageHeight;
            editor.Document.PagePadding = margins;
            editor.Document.ColumnWidth = double.PositiveInfinity; // Une seule colonne
        }

        public void UpdatePageInfo(object sender, EventArgs e)
        {
            TextRange textRange = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
            string text = textRange.Text;

            // Compteur de mots
            int wordCount = text.Split(new char[] { ' ', '\r', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;

            // Compteur de caractères
            int charCount = text.Length;

            // Estimation pages (approximative: 500 mots par page)
            int pageCount = Math.Max(1, (int)Math.Ceiling(wordCount / 500.0));

            if (pageCountLabel != null)
                pageCountLabel.Content = $"📄 Page {pageCount}";

            if (wordCountLabel != null)
                wordCountLabel.Content = $"📝 {wordCount} mots";

            if (charCountLabel != null)
                charCountLabel.Content = $"🔤 {charCount} caractères";
        }

        public void ShowMarginsDialog()
        {
            var dialog = new Window
            {
                Title = "Marges de la page",
                Width = 300,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            // Marges
            panel.Children.Add(CreateMarginControl("Haut (cm):", margins.Top / 37.8, out TextBox topBox));
            panel.Children.Add(CreateMarginControl("Bas (cm):", margins.Bottom / 37.8, out TextBox bottomBox));
            panel.Children.Add(CreateMarginControl("Gauche (cm):", margins.Left / 37.8, out TextBox leftBox));
            panel.Children.Add(CreateMarginControl("Droite (cm):", margins.Right / 37.8, out TextBox rightBox));

            Button okButton = new Button
            {
                Content = "OK",
                Margin = new Thickness(0, 20, 0, 0),
                Padding = new Thickness(20, 5, 20, 5)
            };

            okButton.Click += (s, e) =>
            {
                try
                {
                    double top = double.Parse(topBox.Text) * 37.8;
                    double bottom = double.Parse(bottomBox.Text) * 37.8;
                    double left = double.Parse(leftBox.Text) * 37.8;
                    double right = double.Parse(rightBox.Text) * 37.8;

                    margins = new Thickness(left, top, right, bottom);
                    editor.Document.PagePadding = margins;
                    dialog.Close();
                }
                catch
                {
                    MessageBox.Show("Valeurs invalides", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            panel.Children.Add(okButton);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private StackPanel CreateMarginControl(string label, double value, out TextBox textBox)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };
            Label lbl = new Label
            {
                Content = label,
                Foreground = Brushes.White
            };
            textBox = new TextBox
            {
                Text = value.ToString("F1"),
                Width = 100,
                Background = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
                Foreground = Brushes.White
            };
            sp.Children.Add(lbl);
            sp.Children.Add(textBox);
            return sp;
        }

        public void ToggleOrientation()
        {
            double temp = pageWidth;
            pageWidth = pageHeight;
            pageHeight = temp;

            editor.Document.PageWidth = pageWidth;
            editor.Document.PageHeight = pageHeight;

            MessageBox.Show($"Orientation: {(pageWidth > pageHeight ? "Paysage" : "Portrait")}",
                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowPageSizeDialog()
        {
            var dialog = new Window
            {
                Title = "Taille de la page",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            Label titleLabel = new Label
            {
                Content = "Sélectionnez le format:",
                Foreground = Brushes.White
            };

            ComboBox sizeCombo = new ComboBox
            {
                Margin = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
                Foreground = Brushes.White
            };

            sizeCombo.Items.Add("A4 (21 x 29.7 cm)");
            sizeCombo.Items.Add("Letter (21.6 x 27.9 cm)");
            sizeCombo.Items.Add("Legal (21.6 x 35.6 cm)");
            sizeCombo.SelectedIndex = 0;

            Button okButton = new Button
            {
                Content = "OK",
                Padding = new Thickness(20, 5, 20, 5)
            };

            okButton.Click += (s, e) =>
            {
                switch (sizeCombo.SelectedIndex)
                {
                    case 0: // A4
                        pageWidth = 816;
                        pageHeight = 1056;
                        break;
                    case 1: // Letter
                        pageWidth = 816;
                        pageHeight = 1056;
                        break;
                    case 2: // Legal
                        pageWidth = 816;
                        pageHeight = 1344;
                        break;
                }

                editor.Document.PageWidth = pageWidth;
                editor.Document.PageHeight = pageHeight;
                dialog.Close();
            };

            panel.Children.Add(titleLabel);
            panel.Children.Add(sizeCombo);
            panel.Children.Add(okButton);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        public void ShowBordersDialog()
        {
            var dialog = new Window
            {
                Title = "Bordures de page",
                Width = 350,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            // Style de bordure
            Label styleLabel = new Label { Content = "Style de bordure:", Foreground = Brushes.White };
            ComboBox styleCombo = new ComboBox
            {
                Margin = new Thickness(0, 5, 0, 15),
                Background = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
                Foreground = Brushes.White
            };
            styleCombo.Items.Add("Aucune");
            styleCombo.Items.Add("Simple");
            styleCombo.Items.Add("Double");
            styleCombo.Items.Add("Pointillé");
            styleCombo.SelectedIndex = 0;

            // Épaisseur
            Label thicknessLabel = new Label { Content = "Épaisseur:", Foreground = Brushes.White };
            Slider thicknessSlider = new Slider
            {
                Minimum = 1,
                Maximum = 10,
                Value = 1,
                Width = 200,
                Margin = new Thickness(0, 5, 0, 15)
            };

            // Couleur
            Label colorLabel = new Label { Content = "Couleur:", Foreground = Brushes.White };
            Button colorButton = new Button
            {
                Content = "Choisir la couleur",
                Margin = new Thickness(0, 5, 0, 15),
                Padding = new Thickness(10, 5, 10, 5)
            };

            Brush selectedBrush = Brushes.Black;
            colorButton.Click += (s, e) =>
            {
                var colorDialog = new ColorPickerDialog();
                if (colorDialog.ShowDialog() == true)
                {
                    selectedBrush = new SolidColorBrush(colorDialog.SelectedColor);
                    colorButton.Background = selectedBrush;
                }
            };

            Button okButton = new Button
            {
                Content = "Appliquer",
                Padding = new Thickness(20, 5, 20, 5),
                Margin = new Thickness(0, 10, 0, 0)
            };

            okButton.Click += (s, e) =>
            {
                // Appliquer la bordure au RichTextBox (pas au FlowDocument)
                if (styleCombo.SelectedIndex > 0)
                {
                    editor.BorderBrush = selectedBrush;
                    editor.BorderThickness = new Thickness(thicknessSlider.Value);
                }
                else
                {
                    editor.BorderThickness = new Thickness(0);
                }
                MessageBox.Show("Bordures appliquées!", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dialog.Close();
            };

            panel.Children.Add(styleLabel);
            panel.Children.Add(styleCombo);
            panel.Children.Add(thicknessLabel);
            panel.Children.Add(thicknessSlider);
            panel.Children.Add(colorLabel);
            panel.Children.Add(colorButton);
            panel.Children.Add(okButton);

            dialog.Content = new ScrollViewer { Content = panel };
            dialog.ShowDialog();
        }

        // NOUVELLES FONCTIONNALITÉS: En-têtes et pieds de page
        public void ShowHeaderFooterDialog()
        {
            var dialog = new Window
            {
                Title = "En-tête et pied de page",
                Width = 450,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            // EN-TÊTE
            GroupBox headerGroup = new GroupBox
            {
                Header = "📄 En-tête",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            };

            StackPanel headerPanel = new StackPanel { Margin = new Thickness(10) };

            CheckBox headerCheck = new CheckBox
            {
                Content = "Afficher l'en-tête",
                Foreground = Brushes.White,
                IsChecked = showHeader,
                Margin = new Thickness(0, 0, 0, 10)
            };

            Label headerLabel = new Label { Content = "Texte de l'en-tête:", Foreground = Brushes.White };
            TextBox headerInput = new TextBox
            {
                Text = headerText,
                Background = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
                Foreground = Brushes.White,
                Padding = new Thickness(5),
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true
            };

            headerPanel.Children.Add(headerCheck);
            headerPanel.Children.Add(headerLabel);
            headerPanel.Children.Add(headerInput);
            headerGroup.Content = headerPanel;

            // PIED DE PAGE
            GroupBox footerGroup = new GroupBox
            {
                Header = "📄 Pied de page",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            };

            StackPanel footerPanel = new StackPanel { Margin = new Thickness(10) };

            CheckBox footerCheck = new CheckBox
            {
                Content = "Afficher le pied de page",
                Foreground = Brushes.White,
                IsChecked = showFooter,
                Margin = new Thickness(0, 0, 0, 10)
            };

            Label footerLabel = new Label { Content = "Texte du pied de page:", Foreground = Brushes.White };
            TextBox footerInput = new TextBox
            {
                Text = footerText,
                Background = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
                Foreground = Brushes.White,
                Padding = new Thickness(5),
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true
            };

            CheckBox pageNumberCheck = new CheckBox
            {
                Content = "Numéros de page",
                Foreground = Brushes.White,
                IsChecked = showPageNumbers,
                Margin = new Thickness(0, 10, 0, 0)
            };

            footerPanel.Children.Add(footerCheck);
            footerPanel.Children.Add(footerLabel);
            footerPanel.Children.Add(footerInput);
            footerPanel.Children.Add(pageNumberCheck);
            footerGroup.Content = footerPanel;

            // BOUTONS
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button okButton = new Button
            {
                Content = "Appliquer",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 0, 5, 0)
            };

            okButton.Click += (s, e) =>
            {
                showHeader = headerCheck.IsChecked == true;
                showFooter = footerCheck.IsChecked == true;
                showPageNumbers = pageNumberCheck.IsChecked == true;
                headerText = headerInput.Text;
                footerText = footerInput.Text;

                ApplyHeaderFooter();
                dialog.Close();
            };

            Button cancelButton = new Button
            {
                Content = "Annuler",
                Padding = new Thickness(15, 5, 15, 5)
            };

            cancelButton.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(headerGroup);
            panel.Children.Add(footerGroup);
            panel.Children.Add(buttonPanel);

            dialog.Content = new ScrollViewer { Content = panel };
            dialog.ShowDialog();
        }

        private void ApplyHeaderFooter()
        {
            // Retirer les anciens en-têtes/pieds de page s'ils existent
            var blocksToRemove = new System.Collections.Generic.List<Block>();
            foreach (Block block in editor.Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    var text = new TextRange(para.ContentStart, para.ContentEnd).Text;
                    if (text.Contains("───────── Saut de page ─────────") ||
                        text.StartsWith("Page {PAGE}") ||
                        para.BorderThickness.Bottom > 0 ||
                        para.BorderThickness.Top > 0)
                    {
                        // Ne pas supprimer, c'est probablement un en-tête/pied existant
                        continue;
                    }
                }
            }

            // Appliquer l'en-tête
            if (showHeader && !string.IsNullOrWhiteSpace(headerText))
            {
                Paragraph headerPara = new Paragraph(new Run(headerText))
                {
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(0, 0, 0, 5)
                };

                if (editor.Document.Blocks.FirstBlock != null)
                {
                    editor.Document.Blocks.InsertBefore(editor.Document.Blocks.FirstBlock, headerPara);
                }
                else
                {
                    editor.Document.Blocks.Add(headerPara);
                }
            }

            // Appliquer le pied de page
            if (showFooter)
            {
                string footerContent = footerText ?? "";
                if (showPageNumbers)
                {
                    footerContent += (string.IsNullOrWhiteSpace(footerContent) ? "" : " - ") + "Page {PAGE}";
                }

                if (!string.IsNullOrWhiteSpace(footerContent))
                {
                    Paragraph footerPara = new Paragraph(new Run(footerContent))
                    {
                        FontSize = 10,
                        Foreground = Brushes.Gray,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0),
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        Padding = new Thickness(0, 5, 0, 0)
                    };

                    editor.Document.Blocks.Add(footerPara);
                }
            }

            MessageBox.Show("En-tête et pied de page appliqués!", "Succès",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void InsertPageBreak()
        {
            // Insérer un saut de page visuel
            Paragraph pageBreak = new Paragraph(new Run("───────── Saut de page ─────────"))
            {
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.LightGray,
                FontSize = 10,
                Margin = new Thickness(0, 20, 0, 20),
                BreakPageBefore = true
            };

            editor.CaretPosition.Paragraph?.ElementEnd.InsertParagraphBreak();
            editor.Document.Blocks.Add(pageBreak);
        }
    }
}