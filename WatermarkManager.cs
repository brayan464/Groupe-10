using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EditeurWpf
{
    public class WatermarkManager
    {
        private RichTextBox editor;
        private Border? watermarkBorder;
        private Grid? editorGrid;

        // Propriétés du filigrane
        private string watermarkText = "";
        private ImageSource? watermarkImage = null;
        private double watermarkOpacity = 0.3;
        private double watermarkRotation = 315; // 45° en diagonale
        private Color watermarkColor = Colors.Gray;
        private double watermarkFontSize = 48;

        public WatermarkManager(RichTextBox editorControl, Grid container)
        {
            editor = editorControl;
            editorGrid = container;
        }

        public void ShowWatermarkDialog()
        {
            var dialog = new Window
            {
                Title = "Filigrane",
                Width = 450,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            var mainPanel = new StackPanel { Margin = new Thickness(15) };

            // === FILIGRANE TEXTE ===
            var textGroup = new GroupBox
            {
                Header = "📝 Filigrane Texte",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 5, 0, 10),
                Padding = new Thickness(10)
            };

            var textPanel = new StackPanel();

            // Texte
            textPanel.Children.Add(new Label { Content = "Texte:", Foreground = Brushes.White });
            var textInput = new TextBox
            {
                Text = watermarkText,
                Background = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
                Foreground = Brushes.White,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 0, 10)
            };
            textPanel.Children.Add(textInput);

            // Taille de police
            var fontSizePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            fontSizePanel.Children.Add(new Label { Content = "Taille:", Foreground = Brushes.White, Width = 80 });
            var fontSizeSlider = new Slider
            {
                Minimum = 20,
                Maximum = 100,
                Value = watermarkFontSize,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };
            var fontSizeLabel = new Label { Content = watermarkFontSize.ToString("F0"), Foreground = Brushes.White, Width = 40 };
            fontSizeSlider.ValueChanged += (s, e) => fontSizeLabel.Content = e.NewValue.ToString("F0");
            fontSizePanel.Children.Add(fontSizeSlider);
            fontSizePanel.Children.Add(fontSizeLabel);
            textPanel.Children.Add(fontSizePanel);

            // Opacité
            var opacityPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            opacityPanel.Children.Add(new Label { Content = "Opacité:", Foreground = Brushes.White, Width = 80 });
            var opacitySlider = new Slider
            {
                Minimum = 0.1,
                Maximum = 1.0,
                Value = watermarkOpacity,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };
            var opacityLabel = new Label { Content = watermarkOpacity.ToString("P0"), Foreground = Brushes.White, Width = 40 };
            opacitySlider.ValueChanged += (s, e) => opacityLabel.Content = e.NewValue.ToString("P0");
            opacityPanel.Children.Add(opacitySlider);
            opacityPanel.Children.Add(opacityLabel);
            textPanel.Children.Add(opacityPanel);

            // Rotation
            var rotationPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            rotationPanel.Children.Add(new Label { Content = "Rotation:", Foreground = Brushes.White, Width = 80 });
            var rotationSlider = new Slider
            {
                Minimum = 0,
                Maximum = 360,
                Value = watermarkRotation,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };
            var rotationLabel = new Label { Content = watermarkRotation.ToString("F0") + "°", Foreground = Brushes.White, Width = 40 };
            rotationSlider.ValueChanged += (s, e) => rotationLabel.Content = e.NewValue.ToString("F0") + "°";
            rotationPanel.Children.Add(rotationSlider);
            rotationPanel.Children.Add(rotationLabel);
            textPanel.Children.Add(rotationPanel);

            // Couleur
            var colorPanel = new StackPanel { Orientation = Orientation.Horizontal };
            colorPanel.Children.Add(new Label { Content = "Couleur:", Foreground = Brushes.White, Width = 80 });
            var colorButton = new Button
            {
                Content = "Choisir",
                Width = 100,
                Background = new SolidColorBrush(watermarkColor)
            };
            Color selectedColor = watermarkColor;
            colorButton.Click += (s, e) =>
            {
                var colorDialog = new ColorPickerDialog();
                if (colorDialog.ShowDialog() == true)
                {
                    selectedColor = colorDialog.SelectedColor;
                    colorButton.Background = new SolidColorBrush(selectedColor);
                }
            };
            colorPanel.Children.Add(colorButton);
            textPanel.Children.Add(colorPanel);

            textGroup.Content = textPanel;
            mainPanel.Children.Add(textGroup);

            // === FILIGRANE IMAGE ===
            var imageGroup = new GroupBox
            {
                Header = "🖼️ Filigrane Image",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 5, 0, 10),
                Padding = new Thickness(10)
            };

            var imagePanel = new StackPanel();

            var imageButton = new Button
            {
                Content = "📂 Choisir une image",
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(10, 5, 10, 5)
            };

            var imagePreview = new Image
            {
                Width = 100,
                Height = 100,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 5, 0, 0)
            };

            if (watermarkImage != null)
            {
                imagePreview.Source = watermarkImage;
            }

            ImageSource? selectedImage = watermarkImage;

            imageButton.Click += (s, e) =>
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
                };

                if (openDialog.ShowDialog() == true)
                {
                    try
                    {
                        selectedImage = new BitmapImage(new Uri(openDialog.FileName));
                        imagePreview.Source = selectedImage;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur chargement image: {ex.Message}");
                    }
                }
            };

            imagePanel.Children.Add(imageButton);
            imagePanel.Children.Add(imagePreview);

            imageGroup.Content = imagePanel;
            mainPanel.Children.Add(imageGroup);

            // === BOUTONS ===
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };

            var applyButton = new Button
            {
                Content = "✅ Appliquer",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 0, 5, 0)
            };

            applyButton.Click += (s, e) =>
            {
                watermarkText = textInput.Text;
                watermarkFontSize = fontSizeSlider.Value;
                watermarkOpacity = opacitySlider.Value;
                watermarkRotation = rotationSlider.Value;
                watermarkColor = selectedColor;
                watermarkImage = selectedImage;

                ApplyWatermark();
                dialog.Close();
            };

            var removeButton = new Button
            {
                Content = "🗑️ Supprimer",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 0, 5, 0)
            };

            removeButton.Click += (s, e) =>
            {
                RemoveWatermark();
                dialog.Close();
            };

            var cancelButton = new Button
            {
                Content = "❌ Annuler",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 0, 0, 0)
            };

            cancelButton.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(applyButton);
            buttonPanel.Children.Add(removeButton);
            buttonPanel.Children.Add(cancelButton);

            mainPanel.Children.Add(buttonPanel);

            dialog.Content = new ScrollViewer { Content = mainPanel };
            dialog.ShowDialog();
        }

        private void ApplyWatermark()
        {
            // Supprimer l'ancien filigrane
            RemoveWatermark();

            if (editorGrid == null) return;

            // Créer le conteneur du filigrane
            watermarkBorder = new Border
            {
                IsHitTestVisible = false, // Ne bloque pas les clics
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Priorité au texte si les deux sont définis
            if (!string.IsNullOrWhiteSpace(watermarkText))
            {
                var textBlock = new TextBlock
                {
                    Text = watermarkText,
                    FontSize = watermarkFontSize,
                    Foreground = new SolidColorBrush(watermarkColor),
                    Opacity = watermarkOpacity,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(watermarkRotation)
                };

                watermarkBorder.Child = textBlock;
            }
            else if (watermarkImage != null)
            {
                var image = new Image
                {
                    Source = watermarkImage,
                    Opacity = watermarkOpacity,
                    Stretch = Stretch.Uniform,
                    MaxWidth = 300,
                    MaxHeight = 300,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(watermarkRotation)
                };

                watermarkBorder.Child = image;
            }

            // Ajouter au conteneur (Grid qui contient le RichTextBox)
            Grid.SetRow(watermarkBorder, Grid.GetRow(editor));
            Grid.SetColumn(watermarkBorder, Grid.GetColumn(editor));
            editorGrid.Children.Add(watermarkBorder);

            // Mettre l'éditeur au premier plan
            Panel.SetZIndex(editor, 10);
            Panel.SetZIndex(watermarkBorder, 1);
        }

        public void RemoveWatermark()
        {
            if (watermarkBorder != null && editorGrid != null)
            {
                editorGrid.Children.Remove(watermarkBorder);
                watermarkBorder = null;
            }
        }

        // Méthodes rapides pour filigranes prédéfinis
        public void ApplyConfidentialWatermark()
        {
            watermarkText = "CONFIDENTIEL";
            watermarkColor = Colors.Red;
            watermarkFontSize = 72;
            watermarkOpacity = 0.2;
            watermarkRotation = 315;
            watermarkImage = null;
            ApplyWatermark();
        }

        public void ApplyDraftWatermark()
        {
            watermarkText = "BROUILLON";
            watermarkColor = Colors.Gray;
            watermarkFontSize = 60;
            watermarkOpacity = 0.25;
            watermarkRotation = 315;
            watermarkImage = null;
            ApplyWatermark();
        }

        public void ApplyUrgentWatermark()
        {
            watermarkText = "URGENT";
            watermarkColor = Colors.OrangeRed;
            watermarkFontSize = 68;
            watermarkOpacity = 0.2;
            watermarkRotation = 315;
            watermarkImage = null;
            ApplyWatermark();
        }
    }
}