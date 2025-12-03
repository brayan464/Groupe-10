using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml.Packaging;
using System.Linq;

// Alias pour éviter les conflits de noms
using WpfParagraph = System.Windows.Documents.Paragraph;
using WpfRun = System.Windows.Documents.Run;
using WpfTable = System.Windows.Documents.Table;
using WpfTableRow = System.Windows.Documents.TableRow;
using WpfTableCell = System.Windows.Documents.TableCell;
using WpfTextAlignment = System.Windows.TextAlignment;
using WpfColor = System.Windows.Media.Color;
using WpfFontFamily = System.Windows.Media.FontFamily;

using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WordTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using WordTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using WordTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using WordBold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using WordItalic = DocumentFormat.OpenXml.Wordprocessing.Italic;
using WordUnderline = DocumentFormat.OpenXml.Wordprocessing.Underline;

namespace EditeurWpf
{
    public class DocumentManager
    {
        private MainWindow mainWindow;
        private RichTextBox editor;
        private WordprocessingDocument? currentWordDocument;

        public DocumentManager(MainWindow window, RichTextBox editorControl)
        {
            mainWindow = window;
            editor = editorControl;
        }

        public void NewFile()
        {
            if (!AskSaveIfNeeded()) return;
            editor.Document.Blocks.Clear();
            mainWindow.CurrentFilePath = null;
            mainWindow.IsModified = false;
            currentWordDocument?.Dispose();
            currentWordDocument = null;
            UpdateTitle();
        }

        public void OpenFile()
        {
            if (!AskSaveIfNeeded()) return;

            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Documents Word (*.docx;*.doc)|*.docx;*.doc|Rich Text Format (*.rtf)|*.rtf|Texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    mainWindow.CurrentFilePath = dlg.FileName;
                    editor.Document.Blocks.Clear();

                    string extension = Path.GetExtension(dlg.FileName).ToLower();

                    switch (extension)
                    {
                        case ".docx":
                            LoadWordDocument(dlg.FileName);
                            break;

                        case ".doc":
                            MessageBox.Show("Les fichiers .doc nécessitent une conversion. Veuillez enregistrer en .docx",
                                "Format non supporté", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;

                        case ".rtf":
                            LoadRtfDocument(dlg.FileName);
                            break;

                        default:
                            LoadTextDocument(dlg.FileName);
                            break;
                    }

                    mainWindow.IsModified = false;
                    UpdateTitle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture: {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadWordDocument(string filePath)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".docx");
            File.Copy(filePath, tempFile, true);

            currentWordDocument?.Dispose();
            currentWordDocument = WordprocessingDocument.Open(tempFile, true);

            if (currentWordDocument.MainDocumentPart == null)
            {
                throw new Exception("Document invalide");
            }

            var body = currentWordDocument.MainDocumentPart.Document.Body;
            if (body == null) return;

            FlowDocument flowDoc = new FlowDocument();

            foreach (var element in body.Elements())
            {
                if (element is WordParagraph wordPara)
                {
                    var wpfPara = ConvertWordParagraph(wordPara);
                    if (wpfPara != null)
                        flowDoc.Blocks.Add(wpfPara);
                }
                else if (element is WordTable wordTable)
                {
                    var wpfTable = ConvertWordTable(wordTable);
                    if (wpfTable != null)
                        flowDoc.Blocks.Add(wpfTable);
                }
            }

            editor.Document = flowDoc;
        }

        private WpfParagraph? ConvertWordParagraph(WordParagraph wordPara)
        {
            var wpfPara = new WpfParagraph();

            if (wordPara.ParagraphProperties != null)
            {
                var props = wordPara.ParagraphProperties;

                // Alignement
                if (props.Justification != null && props.Justification.Val != null)
                {
                    wpfPara.TextAlignment = ConvertAlignment(props.Justification.Val.Value);
                }

                // Interligne
                if (props.SpacingBetweenLines != null && props.SpacingBetweenLines.Line != null)
                {
                    double lineSpacing = double.Parse(props.SpacingBetweenLines.Line.Value ?? "240") / 240.0;
                    wpfPara.LineHeight = lineSpacing;
                }

                // Indentation
                if (props.Indentation != null)
                {
                    double leftMargin = wpfPara.Margin.Left;
                    if (props.Indentation.Left != null)
                    {
                        leftMargin = double.Parse(props.Indentation.Left.Value ?? "0") / 20.0;
                    }

                    wpfPara.Margin = new Thickness(
                        leftMargin,
                        wpfPara.Margin.Top,
                        wpfPara.Margin.Right,
                        wpfPara.Margin.Bottom);

                    if (props.Indentation.FirstLine != null)
                    {
                        wpfPara.TextIndent = double.Parse(props.Indentation.FirstLine.Value ?? "0") / 20.0;
                    }
                }

                // Espacement avant
                if (props.SpacingBetweenLines?.Before != null)
                {
                    wpfPara.Margin = new Thickness(
                        wpfPara.Margin.Left,
                        double.Parse(props.SpacingBetweenLines.Before.Value ?? "0") / 20.0,
                        wpfPara.Margin.Right,
                        wpfPara.Margin.Bottom);
                }
            }

            // Convertir les runs
            foreach (var run in wordPara.Elements<WordRun>())
            {
                var wpfRun = ConvertWordRun(run);
                if (wpfRun != null)
                    wpfPara.Inlines.Add(wpfRun);
            }

            return wpfPara;
        }

        private WpfRun? ConvertWordRun(WordRun wordRun)
        {
            string text = wordRun.InnerText;
            if (string.IsNullOrEmpty(text)) return null;

            var wpfRun = new WpfRun(text);

            if (wordRun.RunProperties != null)
            {
                var props = wordRun.RunProperties;

                // Gras
                if (props.Bold != null)
                {
                    wpfRun.FontWeight = FontWeights.Bold;
                }

                // Italique
                if (props.Italic != null)
                {
                    wpfRun.FontStyle = FontStyles.Italic;
                }

                // Souligné
                if (props.Underline != null)
                {
                    wpfRun.TextDecorations = TextDecorations.Underline;
                }

                // Barré
                if (props.Strike != null)
                {
                    wpfRun.TextDecorations = TextDecorations.Strikethrough;
                }

                // Taille de police
                if (props.FontSize != null && props.FontSize.Val != null)
                {
                    wpfRun.FontSize = double.Parse(props.FontSize.Val.Value ?? "24") / 2.0;
                }

                // Police
                if (props.RunFonts != null && props.RunFonts.Ascii != null)
                {
                    wpfRun.FontFamily = new WpfFontFamily(props.RunFonts.Ascii.Value ?? "Calibri");
                }

                // Couleur du texte
                if (props.Color != null && props.Color.Val != null)
                {
                    wpfRun.Foreground = ConvertColor(props.Color.Val.Value ?? "000000");
                }

                // Surlignage
                if (props.Highlight != null && props.Highlight.Val != null)
                {
                    wpfRun.Background = ConvertHighlight(props.Highlight.Val.Value);
                }
            }

            return wpfRun;
        }

        private WpfTable? ConvertWordTable(WordTable wordTable)
        {
            var wpfTable = new WpfTable
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };

            // Colonnes
            var grid = wordTable.Elements<DocumentFormat.OpenXml.Wordprocessing.TableGrid>().FirstOrDefault();
            if (grid != null)
            {
                foreach (var col in grid.Elements<DocumentFormat.OpenXml.Wordprocessing.GridColumn>())
                {
                    double width = 100;
                    if (col.Width != null && col.Width.Value != null)
                    {
                        width = double.Parse(col.Width.Value ?? "2000") / 20.0;
                    }
                    wpfTable.Columns.Add(new TableColumn { Width = new GridLength(width) });
                }
            }

            // Lignes
            TableRowGroup rowGroup = new TableRowGroup();

            foreach (var wordRow in wordTable.Elements<WordTableRow>())
            {
                var wpfRow = new WpfTableRow();

                foreach (var wordCell in wordRow.Elements<WordTableCell>())
                {
                    var wpfCell = new WpfTableCell
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5)
                    };

                    foreach (var para in wordCell.Elements<WordParagraph>())
                    {
                        var wpfPara = ConvertWordParagraph(para);
                        if (wpfPara != null)
                            wpfCell.Blocks.Add(wpfPara);
                    }

                    wpfRow.Cells.Add(wpfCell);
                }

                rowGroup.Rows.Add(wpfRow);
            }

            wpfTable.RowGroups.Add(rowGroup);
            return wpfTable;
        }

        private WpfTextAlignment ConvertAlignment(DocumentFormat.OpenXml.Wordprocessing.JustificationValues alignment)
        {
            // CORRECTION: Utilisation de if-else au lieu de switch expression
            if (alignment == DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center)
                return WpfTextAlignment.Center;
            else if (alignment == DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Right)
                return WpfTextAlignment.Right;
            else if (alignment == DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Both)
                return WpfTextAlignment.Justify;
            else
                return WpfTextAlignment.Left;
        }

        private Brush ConvertColor(string hexColor)
        {
            try
            {
                if (hexColor.Length == 6)
                {
                    byte r = Convert.ToByte(hexColor.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hexColor.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hexColor.Substring(4, 2), 16);
                    return new SolidColorBrush(WpfColor.FromRgb(r, g, b));
                }
            }
            catch { }
            return Brushes.Black;
        }

        private Brush ConvertHighlight(DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues color)
        {
            // CORRECTION: Utilisation de if-else au lieu de switch expression
            if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Yellow)
                return Brushes.Yellow;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Green)
                return Brushes.LightGreen;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Cyan)
                return Brushes.Cyan;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Magenta)
                return Brushes.Magenta;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Blue)
                return Brushes.LightBlue;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Red)
                return Brushes.LightCoral;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.DarkBlue)
                return Brushes.DarkBlue;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.DarkCyan)
                return Brushes.DarkCyan;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.DarkGreen)
                return Brushes.DarkGreen;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.DarkMagenta)
                return Brushes.DarkMagenta;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.DarkRed)
                return Brushes.DarkRed;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.DarkYellow)
                return Brushes.Gold;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.DarkGray)
                return Brushes.DarkGray;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.LightGray)
                return Brushes.LightGray;
            else if (color == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Black)
                return Brushes.Black;
            else
                return Brushes.Transparent;
        }

        private void LoadRtfDocument(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                TextRange range = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
                range.Load(fs, DataFormats.Rtf);
            }
        }

        private void LoadTextDocument(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                TextRange range = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
                range.Load(fs, DataFormats.Text);
            }
        }

        public void SaveFile()
        {
            if (mainWindow.CurrentFilePath == null)
                SaveAsFile();
            else
                SaveToFile(mainWindow.CurrentFilePath);
        }

        public void SaveAsFile()
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Document Word (*.docx)|*.docx|Rich Text Format (*.rtf)|*.rtf|Texte (*.txt)|*.txt"
            };

            if (dlg.ShowDialog() == true)
            {
                mainWindow.CurrentFilePath = dlg.FileName;
                SaveToFile(mainWindow.CurrentFilePath);
            }
        }

        private void SaveToFile(string path)
        {
            try
            {
                string extension = Path.GetExtension(path).ToLower();

                switch (extension)
                {
                    case ".docx":
                        SaveAsWordDocument(path);
                        break;

                    case ".rtf":
                        SaveAsRtf(path);
                        break;

                    default:
                        SaveAsText(path);
                        break;
                }

                mainWindow.IsModified = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsWordDocument(string path)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(path,
                DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                DocumentFormat.OpenXml.Wordprocessing.Body body =
                    mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

                foreach (Block block in editor.Document.Blocks)
                {
                    if (block is WpfParagraph wpfPara)
                    {
                        var wordPara = ConvertToWordParagraph(wpfPara);
                        body.AppendChild(wordPara);
                    }
                    else if (block is WpfTable wpfTable)
                    {
                        var wordTable = ConvertToWordTable(wpfTable);
                        body.AppendChild(wordTable);
                    }
                }

                mainPart.Document.Save();
            }
        }

        private WordParagraph ConvertToWordParagraph(WpfParagraph wpfPara)
        {
            var wordPara = new WordParagraph();
            var paraProps = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();

            // Alignement - CORRECTION: utilisation correcte des EnumValue
            if (wpfPara.TextAlignment != WpfTextAlignment.Left)
            {
                var justification = new DocumentFormat.OpenXml.Wordprocessing.Justification();

                if (wpfPara.TextAlignment == WpfTextAlignment.Center)
                    justification.Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center;
                else if (wpfPara.TextAlignment == WpfTextAlignment.Right)
                    justification.Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Right;
                else if (wpfPara.TextAlignment == WpfTextAlignment.Justify)
                    justification.Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Both;
                else
                    justification.Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Left;

                paraProps.Justification = justification;
            }

            wordPara.AppendChild(paraProps);

            // Convertir les inlines
            foreach (var inline in wpfPara.Inlines)
            {
                if (inline is WpfRun wpfRun)
                {
                    var wordRun = ConvertToWordRun(wpfRun);
                    wordPara.AppendChild(wordRun);
                }
            }

            return wordPara;
        }

        private WordRun ConvertToWordRun(WpfRun wpfRun)
        {
            var wordRun = new WordRun();
            var runProps = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();

            // Gras
            if (wpfRun.FontWeight == FontWeights.Bold)
            {
                runProps.Bold = new WordBold();
            }

            // Italique
            if (wpfRun.FontStyle == FontStyles.Italic)
            {
                runProps.Italic = new WordItalic();
            }

            // Souligné
            if (wpfRun.TextDecorations.Contains(TextDecorations.Underline[0]))
            {
                runProps.Underline = new WordUnderline
                {
                    Val = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues.Single
                };
            }

            // Taille
            if (wpfRun.FontSize > 0)
            {
                runProps.FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize
                {
                    Val = ((int)(wpfRun.FontSize * 2)).ToString()
                };
            }

            // Police
            if (wpfRun.FontFamily != null)
            {
                runProps.RunFonts = new DocumentFormat.OpenXml.Wordprocessing.RunFonts
                {
                    Ascii = wpfRun.FontFamily.Source
                };
            }

            wordRun.AppendChild(runProps);
            wordRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(wpfRun.Text));

            return wordRun;
        }

        private WordTable ConvertToWordTable(WpfTable wpfTable)
        {
            var wordTable = new WordTable();

            foreach (var rowGroup in wpfTable.RowGroups)
            {
                foreach (var wpfRow in rowGroup.Rows)
                {
                    var wordRow = new WordTableRow();

                    foreach (var wpfCell in wpfRow.Cells)
                    {
                        var wordCell = new WordTableCell();

                        foreach (var block in wpfCell.Blocks)
                        {
                            if (block is WpfParagraph para)
                            {
                                wordCell.AppendChild(ConvertToWordParagraph(para));
                            }
                        }

                        wordRow.AppendChild(wordCell);
                    }

                    wordTable.AppendChild(wordRow);
                }
            }

            return wordTable;
        }

        private void SaveAsRtf(string path)
        {
            TextRange range = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                range.Save(fs, DataFormats.Rtf);
            }
        }

        private void SaveAsText(string path)
        {
            TextRange range = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                range.Save(fs, DataFormats.Text);
            }
        }

        public void ExportToPDF()
        {
            MessageBox.Show("Fonction Export PDF: Utilisez iTextSharp ou installez 'Microsoft.Office.Interop.Word' pour la conversion",
                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void Print()
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    FlowDocument doc = editor.Document;
                    IDocumentPaginatorSource idpSource = doc;
                    printDialog.PrintDocument(idpSource.DocumentPaginator, "Impression Document");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'impression: {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Search(string query)
        {
            if (string.IsNullOrEmpty(query)) return;

            TextPointer? start = FindText(query);
            if (start != null)
            {
                TextPointer? end = start.GetPositionAtOffset(query.Length);
                if (end != null)
                {
                    editor.Selection.Select(start, end);
                    editor.Focus();
                }
            }
            else
            {
                MessageBox.Show("Texte non trouvé.", "Recherche",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void Replace(string search, string replace)
        {
            if (string.IsNullOrEmpty(search)) return;

            TextRange document = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
            string content = document.Text.Replace(search, replace, StringComparison.OrdinalIgnoreCase);

            editor.Document.Blocks.Clear();
            editor.Document.Blocks.Add(new WpfParagraph(new WpfRun(content)));
            mainWindow.IsModified = true;
            UpdateTitle();
        }

        public void InsertImage()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Images (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(dlg.FileName));
                    Image image = new Image
                    {
                        Source = bitmap,
                        Width = 300,
                        Stretch = Stretch.Uniform
                    };

                    InlineUIContainer container = new InlineUIContainer(image, editor.CaretPosition);
                    mainWindow.IsModified = true;
                    UpdateTitle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'insertion: {ex.Message}", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void InsertTable()
        {
            var table = new WpfTable
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };

            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });

            TableRowGroup rowGroup = new TableRowGroup();

            for (int i = 0; i < 3; i++)
            {
                WpfTableRow row = new WpfTableRow();
                for (int j = 0; j < 3; j++)
                {
                    WpfTableCell cell = new WpfTableCell(new WpfParagraph(new WpfRun($"Cellule {i + 1}-{j + 1}")))
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5)
                    };
                    row.Cells.Add(cell);
                }
                rowGroup.Rows.Add(row);
            }

            table.RowGroups.Add(rowGroup);
            editor.Document.Blocks.Add(table);
            mainWindow.IsModified = true;
            UpdateTitle();
        }

        public void InsertDate()
        {
            string date = DateTime.Now.ToString("dddd d MMMM yyyy");
            editor.CaretPosition.InsertTextInRun(date);
            mainWindow.IsModified = true;
            UpdateTitle();
        }

        public void UpdateTitle()
        {
            string fileName = mainWindow.CurrentFilePath != null ?
                Path.GetFileName(mainWindow.CurrentFilePath) : "Sans titre";
            mainWindow.Title = fileName + (mainWindow.IsModified ? " *" : "") + " - Word Pro avec IA";
        }

        private bool AskSaveIfNeeded()
        {
            if (!mainWindow.IsModified) return true;

            MessageBoxResult r = MessageBox.Show(
                "Le document a été modifié. Voulez-vous enregistrer ?",
                "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (r == MessageBoxResult.Yes)
            {
                SaveFile();
                return true;
            }
            return r == MessageBoxResult.No;
        }

        private TextPointer? FindText(string text)
        {
            TextRange document = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
            int index = document.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
                return GetTextPositionAtOffset(editor.Document.ContentStart, index);
            return null;
        }

        private TextPointer? GetTextPositionAtOffset(TextPointer start, int offset)
        {
            TextPointer? current = start;
            int cnt = 0;

            while (current != null)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string run = current.GetTextInRun(LogicalDirection.Forward);
                    if (offset <= cnt + run.Length)
                        return current.GetPositionAtOffset(offset - cnt);
                    cnt += run.Length;
                }
                current = current.GetNextContextPosition(LogicalDirection.Forward);
            }
            return null;
        }
    }
}