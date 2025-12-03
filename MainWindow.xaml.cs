using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace EditeurWpf
{
    public partial class MainWindow : Window
    {
        private string? currentFilePath = null;
        private bool isModified = false;
        private string apiKeyOpenRouter = "";
        private string apiKeyOpenAI = "";
        private System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();

        // Modules
        private AudioManager? audioManager;
        private AutoCompleteManager? autoCompleteManager;
        private ApiManager? apiManager;
        private ChatManager? chatManager;
        private DocumentManager? documentManager;
        private PageManager? pageManager;
        private StyleManager? styleManager;
        private WatermarkManager? watermarkManager;

        public MainWindow()
        {
            InitializeComponent();
            InitializeModules();
            LoadApiKeys();
        }

        private void InitializeModules()
        {
            audioManager = new AudioManager(this, httpClient);
            autoCompleteManager = new AutoCompleteManager(Editor);
            apiManager = new ApiManager(httpClient);
            chatManager = new ChatManager(ChatPanel, ChatScrollViewer);
            documentManager = new DocumentManager(this, Editor);
            pageManager = new PageManager(Editor, PageCountLabel, WordCountLabel, CharCountLabel);
            styleManager = new StyleManager(Editor);

            // Initialiser le WatermarkManager
            Grid? editorContainer = FindParentGrid(Editor);
            if (editorContainer != null)
            {
                watermarkManager = new WatermarkManager(Editor, editorContainer);
            }

            Editor.TextChanged += Editor_TextChanged;
            Editor.PreviewKeyDown += autoCompleteManager.Editor_PreviewKeyDown;
            Editor.SizeChanged += pageManager.UpdatePageInfo;

            documentManager.UpdateTitle();
            pageManager.UpdatePageInfo(null, null);
        }

        private Grid? FindParentGrid(DependencyObject child)
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is Grid grid)
                    return grid;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            isModified = true;
            documentManager?.UpdateTitle();
            autoCompleteManager?.HandleTextChanged();
            pageManager?.UpdatePageInfo(null, null);
        }

        // === MENU FICHIER ===
        private void NewFile_Click(object sender, RoutedEventArgs e) => documentManager?.NewFile();
        private void OpenFile_Click(object sender, RoutedEventArgs e) => documentManager?.OpenFile();
        private void SaveFile_Click(object sender, RoutedEventArgs e) => documentManager?.SaveFile();
        private void SaveAsFile_Click(object sender, RoutedEventArgs e) => documentManager?.SaveAsFile();
        private void ExportPDF_Click(object sender, RoutedEventArgs e) => documentManager?.ExportToPDF();
        private void Print_Click(object sender, RoutedEventArgs e) => documentManager?.Print();
        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        // === MENU ÉDITION ===
        private void Undo_Click(object sender, RoutedEventArgs e) => Editor.Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Editor.Redo();
        private void Cut_Click(object sender, RoutedEventArgs e) => Editor.Cut();
        private void Copy_Click(object sender, RoutedEventArgs e) => Editor.Copy();
        private void Paste_Click(object sender, RoutedEventArgs e) => Editor.Paste();
        private void SelectAll_Click(object sender, RoutedEventArgs e) => Editor.SelectAll();

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            documentManager?.Search(SearchBox.Text);
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            documentManager?.Replace(SearchBox.Text, ReplaceBox.Text);
        }

        // === MENU INSERTION ===
        private void InsertImage_Click(object sender, RoutedEventArgs e) => documentManager?.InsertImage();
        private void InsertTable_Click(object sender, RoutedEventArgs e) => documentManager?.InsertTable();
        private void InsertPageBreak_Click(object sender, RoutedEventArgs e) => pageManager?.InsertPageBreak();
        private void InsertDate_Click(object sender, RoutedEventArgs e) => documentManager?.InsertDate();
        private void InsertWatermark_Click(object sender, RoutedEventArgs e) => watermarkManager?.ShowWatermarkDialog();

        // === MENU MISE EN PAGE ===
        private void PageMargins_Click(object sender, RoutedEventArgs e) => pageManager?.ShowMarginsDialog();
        private void PageOrientation_Click(object sender, RoutedEventArgs e) => pageManager?.ToggleOrientation();
        private void PageSize_Click(object sender, RoutedEventArgs e) => pageManager?.ShowPageSizeDialog();
        private void PageBorders_Click(object sender, RoutedEventArgs e) => pageManager?.ShowBordersDialog();

        // NOUVEAU: En-tête et pied de page
        private void HeaderFooter_Click(object sender, RoutedEventArgs e) => pageManager?.ShowHeaderFooterDialog();

        // === FORMATAGE ===
        private void Bold_Click(object sender, RoutedEventArgs e) =>
            EditingCommands.ToggleBold.Execute(null, Editor);
        private void Italic_Click(object sender, RoutedEventArgs e) =>
            EditingCommands.ToggleItalic.Execute(null, Editor);
        private void Underline_Click(object sender, RoutedEventArgs e) =>
            EditingCommands.ToggleUnderline.Execute(null, Editor);
        private void AlignLeft_Click(object sender, RoutedEventArgs e) =>
            EditingCommands.AlignLeft.Execute(null, Editor);
        private void AlignCenter_Click(object sender, RoutedEventArgs e) =>
            EditingCommands.AlignCenter.Execute(null, Editor);
        private void AlignRight_Click(object sender, RoutedEventArgs e) =>
            EditingCommands.AlignRight.Execute(null, Editor);
        private void AlignJustify_Click(object sender, RoutedEventArgs e) =>
            EditingCommands.AlignJustify.Execute(null, Editor);

        private void Color_Click(object sender, RoutedEventArgs e) => styleManager?.ChangeTextColor();
        private void Highlight_Click(object sender, RoutedEventArgs e) => styleManager?.HighlightText();
        private void Font_Click(object sender, RoutedEventArgs e) => styleManager?.ChangeFont();

        private void FontSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizeCombo?.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                if (double.TryParse(item.Content.ToString(), out double size))
                {
                    styleManager?.ChangeFontSize(size);
                }
            }
        }

        // === STYLES ===
        private void Style_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (StyleCombo?.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string? style = item.Tag.ToString();
                if (!string.IsNullOrEmpty(style))
                {
                    styleManager?.ApplyStyle(style);
                }
            }
        }

        private void BulletList_Click(object sender, RoutedEventArgs e) => styleManager?.ToggleBulletList();
        private void NumberedList_Click(object sender, RoutedEventArgs e) => styleManager?.ToggleNumberedList();

        // === API ET IA ===
        // === API ET IA ===
        private void SaveApiKeys_Click(object sender, RoutedEventArgs e)
        {
            // Récupérer les clés des PasswordBox
            apiKeyOpenRouter = ApiKeyOpenRouter.Password;
            apiKeyOpenAI = ApiKeyOpenAI.Password;

            // Validation basique
            if (string.IsNullOrWhiteSpace(apiKeyOpenRouter) && string.IsNullOrWhiteSpace(apiKeyOpenAI))
            {
                MessageBox.Show("Veuillez entrer au moins une clé API.", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validation format OpenRouter
            if (!string.IsNullOrWhiteSpace(apiKeyOpenRouter) && !apiKeyOpenRouter.StartsWith("sk-or-"))
            {
                MessageBox.Show("⚠️ La clé OpenRouter devrait commencer par 'sk-or-'\nVérifiez votre clé sur https://openrouter.ai/keys",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Validation format Groq
            if (!string.IsNullOrWhiteSpace(apiKeyOpenAI) && !apiKeyOpenAI.StartsWith("gsk_"))
            {
                MessageBox.Show("⚠️ La clé Groq devrait commencer par 'gsk_'\nVérifiez votre clé sur https://console.groq.com/keys",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Sauvegarder
            apiManager?.SaveApiKeys(apiKeyOpenRouter, apiKeyOpenAI);

            // Message de succès avec détails
            string details = "";
            if (!string.IsNullOrWhiteSpace(apiKeyOpenRouter))
                details += "✅ OpenRouter (Chat)\n";
            if (!string.IsNullOrWhiteSpace(apiKeyOpenAI))
                details += "✅ Groq (Audio)\n";

            MessageBox.Show($"Clés API sauvegardées avec succès!\n\n{details}\nElles seront chargées automatiquement au prochain démarrage.",
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadApiKeys()
        {
            if (apiManager != null)
            {
                var keys = apiManager.LoadApiKeys();
                apiKeyOpenRouter = keys.openrouter;
                apiKeyOpenAI = keys.openai;
                ApiKeyOpenRouter.Password = apiKeyOpenRouter;
                ApiKeyOpenAI.Password = apiKeyOpenAI;

                // Message de chargement (optionnel, peut être commenté si trop intrusif)
                if (!string.IsNullOrEmpty(apiKeyOpenRouter) || !string.IsNullOrEmpty(apiKeyOpenAI))
                {
                    System.Diagnostics.Debug.WriteLine("✅ Clés API chargées depuis la configuration");
                }
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string prompt = PromptBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(prompt)) return;

            if (string.IsNullOrEmpty(apiKeyOpenRouter))
            {
                MessageBox.Show("Veuillez configurer votre clé API OpenRouter.",
                    "Configuration requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            chatManager?.AddMessage(prompt, true);
            PromptBox.Clear();

            string editorText = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;

            try
            {
                string? response = await apiManager!.CallOpenRouterAPI(prompt, editorText, apiKeyOpenRouter);

                if (response != null)
                {
                    if (response.StartsWith("✅"))
                    {
                        chatManager?.AddMessage(response, false);
                    }
                    else if (response.Contains("\"action\"") && response.Contains("\"write\""))
                    {
                        string? content = apiManager.ExtractWriteContent(response);
                        if (!string.IsNullOrEmpty(content))
                        {
                            Editor.Document.Blocks.Clear();
                            Editor.Document.Blocks.Add(new Paragraph(new Run(content)));
                            isModified = true;
                            documentManager?.UpdateTitle();
                            chatManager?.AddMessage("✅ Texte inséré !", false);
                        }
                    }
                    else
                    {
                        chatManager?.AddMessage(response, false);
                    }
                }
            }
            catch (Exception ex)
            {
                chatManager?.AddMessage($"Erreur: {ex.Message}", false);
            }
        }

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            audioManager?.ToggleRecording(RecordButton, async (transcription) =>
            {
                chatManager?.AddMessage($"🎤 {transcription}", true);

                if (!string.IsNullOrWhiteSpace(transcription))
                {
                    string editorText = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
                    try
                    {
                        string? response = await apiManager!.CallOpenRouterAPI(transcription, editorText, apiKeyOpenRouter);
                        if (response != null)
                        {
                            chatManager?.AddMessage(response, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        chatManager?.AddMessage($"Erreur: {ex.Message}", false);
                    }
                }
            }, apiKeyOpenAI);
        }

        // === ACTIONS RAPIDES ===
        private async void QuickCorrect_Click(object sender, RoutedEventArgs e) =>
            await ProcessQuickAction("Corrige l'orthographe et la grammaire. Format JSON: {\"action\": \"write\", \"content\": \"texte corrigé\"}");

        private async void QuickSummarize_Click(object sender, RoutedEventArgs e) =>
            await ProcessQuickAction("Résume ce texte de manière concise.");

        private async void QuickImprove_Click(object sender, RoutedEventArgs e) =>
            await ProcessQuickAction("Améliore le style. Format JSON: {\"action\": \"write\", \"content\": \"texte amélioré\"}");

        private async void QuickTranslate_Click(object sender, RoutedEventArgs e) =>
            await ProcessQuickAction("Traduis en anglais. Format JSON: {\"action\": \"write\", \"content\": \"traduction\"}");

        private async System.Threading.Tasks.Task ProcessQuickAction(string instruction)
        {
            if (string.IsNullOrEmpty(apiKeyOpenRouter))
            {
                MessageBox.Show("Configurez votre clé API OpenRouter.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string editorText = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;

            if (string.IsNullOrWhiteSpace(editorText) &&
                (instruction.Contains("Corrige") || instruction.Contains("Résume") ||
                 instruction.Contains("Améliore") || instruction.Contains("Traduis")))
            {
                MessageBox.Show("Le document est vide.", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            chatManager?.AddMessage(instruction.Split('.')[0], true);

            try
            {
                string? response = await apiManager!.CallOpenRouterAPI(instruction, editorText, apiKeyOpenRouter);

                if (response != null && response.Contains("\"action\"") && response.Contains("\"write\""))
                {
                    string? content = apiManager.ExtractWriteContent(response);
                    if (!string.IsNullOrEmpty(content))
                    {
                        Editor.Document.Blocks.Clear();
                        Editor.Document.Blocks.Add(new Paragraph(new Run(content)));
                        isModified = true;
                        documentManager?.UpdateTitle();
                        chatManager?.AddMessage("✅ Texte mis à jour !", false);
                    }
                }
                else if (response != null)
                {
                    chatManager?.AddMessage(response, false);
                }
            }
            catch (Exception ex)
            {
                chatManager?.AddMessage($"Erreur: {ex.Message}", false);
            }
        }

        public string? CurrentFilePath
        {
            get => currentFilePath;
            set => currentFilePath = value;
        }

        public bool IsModified
        {
            get => isModified;
            set => isModified = value;
        }
    }
}