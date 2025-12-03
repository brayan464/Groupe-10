using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace EditeurWpf
{
    public class AutoCompleteManager
    {
        private RichTextBox editor;
        private Popup? autoCompletePopup;
        private ListBox? autoCompleteList;
        private string[] commonWords = new string[]
        {
            "bonjour", "merci", "document", "rapport", "lettre", "article",
            "paragraphe", "section", "introduction", "conclusion", "développement",
            "entreprise", "société", "organisation", "département", "service",
            "monsieur", "madame", "cher", "cordialement", "sincèrement",
            "important", "urgent", "nécessaire", "essentiel", "primordial",
            "analyse", "synthèse", "résumé", "présentation", "projet",
            "objectif", "stratégie", "résultat", "performance", "qualité"
        };

        public AutoCompleteManager(RichTextBox editorControl)
        {
            editor = editorControl;
            InitializePopup();
        }

        private void InitializePopup()
        {
            autoCompletePopup = new Popup
            {
                PlacementTarget = editor,
                Placement = PlacementMode.Relative,
                IsOpen = false,
                StaysOpen = false,
                Width = 200,
                MaxHeight = 150
            };

            autoCompleteList = new ListBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
                BorderThickness = new Thickness(1)
            };

            autoCompleteList.MouseDoubleClick += (s, e) =>
            {
                if (autoCompleteList.SelectedItem != null)
                {
                    string? word = autoCompleteList.SelectedItem.ToString();
                    if (!string.IsNullOrEmpty(word))
                    {
                        InsertAutoComplete(word);
                    }
                }
            };

            autoCompletePopup.Child = autoCompleteList;
        }

        public void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (autoCompletePopup?.IsOpen == true && autoCompleteList != null)
            {
                if (e.Key == Key.Down)
                {
                    autoCompleteList.SelectedIndex = Math.Min(
                        autoCompleteList.SelectedIndex + 1,
                        autoCompleteList.Items.Count - 1);
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    autoCompleteList.SelectedIndex = Math.Max(
                        autoCompleteList.SelectedIndex - 1, 0);
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter || e.Key == Key.Tab)
                {
                    if (autoCompleteList.SelectedItem != null)
                    {
                        string? word = autoCompleteList.SelectedItem.ToString();
                        if (!string.IsNullOrEmpty(word))
                        {
                            InsertAutoComplete(word);
                        }
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    autoCompletePopup.IsOpen = false;
                    e.Handled = true;
                }
            }
        }

        public void HandleTextChanged()
        {
            if (autoCompletePopup == null || autoCompleteList == null) return;

            string currentWord = GetCurrentWord();
            if (currentWord.Length >= 2)
            {
                var suggestions = commonWords
                    .Where(w => w.StartsWith(currentWord.ToLower()) &&
                                w != currentWord.ToLower())
                    .ToArray();

                if (suggestions.Length > 0)
                {
                    autoCompleteList.Items.Clear();
                    foreach (var suggestion in suggestions.Take(5))
                    {
                        autoCompleteList.Items.Add(suggestion);
                    }
                    autoCompleteList.SelectedIndex = 0;

                    Rect caretRect = editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
                    autoCompletePopup.HorizontalOffset = caretRect.Left;
                    autoCompletePopup.VerticalOffset = caretRect.Bottom;
                    autoCompletePopup.IsOpen = true;
                }
                else
                {
                    autoCompletePopup.IsOpen = false;
                }
            }
            else
            {
                autoCompletePopup.IsOpen = false;
            }
        }

        private void InsertAutoComplete(string word)
        {
            if (autoCompletePopup == null) return;

            TextPointer caretPos = editor.CaretPosition;
            string currentWord = GetCurrentWord();
            TextPointer? wordStart = caretPos.GetPositionAtOffset(-currentWord.Length);

            if (wordStart != null)
            {
                TextRange range = new TextRange(wordStart, caretPos);
                range.Text = word;
                editor.CaretPosition = caretPos.GetPositionAtOffset(
                    word.Length - currentWord.Length) ?? caretPos;
            }

            autoCompletePopup.IsOpen = false;
        }

        private string GetCurrentWord()
        {
            TextPointer caretPos = editor.CaretPosition;
            TextPointer? wordStart = caretPos;

            while (wordStart != null &&
                   wordStart.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text)
            {
                string text = wordStart.GetTextInRun(LogicalDirection.Backward);
                if (string.IsNullOrWhiteSpace(text) || text.EndsWith(" "))
                    break;
                wordStart = wordStart.GetNextContextPosition(LogicalDirection.Backward);
            }

            if (wordStart != null)
            {
                TextRange range = new TextRange(wordStart, caretPos);
                return range.Text.Trim();
            }

            return "";
        }
    }
}