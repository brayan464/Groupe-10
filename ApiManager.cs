using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EditeurWpf
{
    public class ApiManager
    {
        private HttpClient httpClient;

        public ApiManager(HttpClient client)
        {
            httpClient = client;
        }

        public void SaveApiKeys(string openRouterKey, string groqKey)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EditeurWpf"
                );

                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                string configFile = Path.Combine(appDataPath, "apikeys.json");

                var config = new
                {
                    openrouter = openRouterKey ?? "",
                    groq = groqKey ?? ""
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configFile, json);

                System.Diagnostics.Debug.WriteLine($"✅ Clés sauvegardées dans: {configFile}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ Erreur lors de la sauvegarde des clés API:\n\n{ex.Message}",
                    "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw; // Propager l'erreur
            }
        }

        public (string openrouter, string openai) LoadApiKeys()
        {
            try
            {
                string configFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EditeurWpf",
                    "apikeys.json"
                );

                if (File.Exists(configFile))
                {
                    string json = File.ReadAllText(configFile);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        return (
                            root.TryGetProperty("openrouter", out var or1) ? or1.GetString() ?? "" : "",
                            root.TryGetProperty("groq", out var groq) ? groq.GetString() ?? "" : ""
                        );
                    }
                }
            }
            catch { }

            return ("", "");
        }

        public async Task<string?> CallOpenRouterAPI(string prompt, string editorText, string apiKey)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    return "❌ Clé API OpenRouter manquante";
                }

                string systemPrompt = @"Tu es un assistant d'écriture intelligent. 
Pour modifier le document, réponds UNIQUEMENT avec ce format JSON strict :
{""action"": ""write"", ""content"": ""ton texte ici""}

Exemples:
- Correction: {""action"": ""write"", ""content"": ""texte corrigé""}
- Amélioration: {""action"": ""write"", ""content"": ""texte amélioré""}
- Traduction: {""action"": ""write"", ""content"": ""translated text""}

Pour les questions/discussions, réponds normalement sans JSON.";

                var requestBody = new
                {
                    model = "anthropic/claude-3.5-sonnet",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = $"Document actuel:\n{editorText}\n\nDemande: {prompt}" }
                    },
                    max_tokens = 2000,
                    temperature = 0.7
                };

                string jsonRequest = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = content;

                var response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return $"❌ Erreur API: {response.StatusCode} - {errorContent}";
                }

                // NE PAS FERMER LE STREAM - laisser HttpClient le gérer
                string responseContent = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var message = choices[0].GetProperty("message");
                        string responseText = message.GetProperty("content").GetString() ?? "";
                        return responseText;
                    }
                }

                return "❌ Réponse vide de l'API";
            }
            catch (Exception ex)
            {
                return $"❌ Erreur: {ex.Message}";
            }
        }

        public string? ExtractWriteContent(string jsonResponse)
        {
            try
            {
                // Nettoyer le JSON des balises markdown
                string cleaned = jsonResponse.Trim();
                if (cleaned.StartsWith("```json"))
                {
                    cleaned = cleaned.Substring(7);
                }
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(3);
                }
                if (cleaned.EndsWith("```"))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);
                }
                cleaned = cleaned.Trim();

                using (JsonDocument doc = JsonDocument.Parse(cleaned))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("action", out var action) &&
                        action.GetString() == "write" &&
                        root.TryGetProperty("content", out var content))
                    {
                        return content.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur extraction JSON: {ex.Message}");
            }

            return null;
        }
    }
}