using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NAudio.Wave;

namespace EditeurWpf
{
    public class AudioManager
    {
        private MainWindow mainWindow;
        private HttpClient httpClient;
        private WaveInEvent? waveIn;
        private MemoryStream? audioStream;
        private WaveFileWriter? waveWriter;
        private bool isRecording = false;

        public AudioManager(MainWindow window, HttpClient client)
        {
            mainWindow = window;
            httpClient = client;
        }

        public void ToggleRecording(Button recordButton, Func<string, Task> onTranscription, string apiKey)
        {
            if (!isRecording)
            {
                StartRecording(recordButton);
            }
            else
            {
                StopRecording(recordButton, onTranscription, apiKey);
            }
        }

        private void StartRecording(Button recordButton)
        {
            try
            {
                // Créer un nouveau stream pour l'enregistrement
                audioStream = new MemoryStream();

                waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 1) // 16kHz, mono
                };

                waveWriter = new WaveFileWriter(audioStream, waveIn.WaveFormat);

                waveIn.DataAvailable += (s, e) =>
                {
                    if (waveWriter != null && e.BytesRecorded > 0)
                    {
                        waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                        waveWriter.Flush(); // Forcer l'écriture
                    }
                };

                waveIn.StartRecording();
                isRecording = true;
                recordButton.Content = "⏹️ Stop";
                recordButton.Background = System.Windows.Media.Brushes.Red;

                System.Diagnostics.Debug.WriteLine("🎤 Enregistrement démarré");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du démarrage de l'enregistrement:\n\n{ex.Message}\n\nAssurez-vous qu'un microphone est connecté et autorisé.",
                    "Erreur Audio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StopRecording(Button recordButton, Func<string, Task> onTranscription, string apiKey)
        {
            byte[]? audioData = null;

            try
            {
                System.Diagnostics.Debug.WriteLine("⏹️ Arrêt de l'enregistrement...");

                // Arrêter l'enregistrement
                if (waveIn != null)
                {
                    waveIn.StopRecording();
                    waveIn.DataAvailable -= null; // Retirer les handlers
                }

                // Attendre un peu pour que les dernières données soient écrites
                await Task.Delay(100);

                // Finaliser l'écriture du fichier WAV
                if (waveWriter != null)
                {
                    waveWriter.Flush();
                    // NE PAS DISPOSER waveWriter tout de suite !
                }

                // COPIER les données AVANT de disposer quoi que ce soit
                if (audioStream != null && audioStream.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"📊 Taille audio stream: {audioStream.Length} bytes");

                    // Retourner au début du stream
                    audioStream.Position = 0;

                    // COPIER les données dans un tableau
                    audioData = new byte[audioStream.Length];
                    await audioStream.ReadAsync(audioData, 0, audioData.Length);

                    System.Diagnostics.Debug.WriteLine($"✅ Données audio copiées: {audioData.Length} bytes");
                }

                // Maintenant on peut tout disposer
                if (waveWriter != null)
                {
                    waveWriter.Dispose();
                    waveWriter = null;
                }

                if (waveIn != null)
                {
                    waveIn.Dispose();
                    waveIn = null;
                }

                if (audioStream != null)
                {
                    audioStream.Dispose();
                    audioStream = null;
                }

                // Réinitialiser l'interface
                isRecording = false;
                recordButton.Content = "🎤";
                recordButton.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(45, 45, 48));

                // Vérifier qu'on a des données audio valides
                if (audioData != null && audioData.Length > 44) // Plus que l'en-tête WAV (44 bytes)
                {
                    System.Diagnostics.Debug.WriteLine("🚀 Envoi pour transcription...");

                    // Transcription avec les données copiées
                    string transcription = await TranscribeAudio(audioData, apiKey);

                    if (!string.IsNullOrWhiteSpace(transcription) && !transcription.StartsWith("❌"))
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Transcription: {transcription}");
                        await onTranscription(transcription);
                    }
                    else
                    {
                        MessageBox.Show(transcription, "Transcription",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Enregistrement audio trop court ou vide.\nParlez au moins 1 seconde.",
                        "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'arrêt de l'enregistrement:\n\n{ex.Message}\n\nStack: {ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

                // Nettoyage en cas d'erreur
                try
                {
                    waveWriter?.Dispose();
                    waveIn?.Dispose();
                    audioStream?.Dispose();
                }
                catch { }

                isRecording = false;
                recordButton.Content = "🎤";
                recordButton.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(45, 45, 48));
            }
        }

        private async Task<string> TranscribeAudio(byte[] audioData, string apiKey)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📤 Début transcription (taille: {audioData.Length} bytes)");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return "❌ Clé API Groq manquante. Configurez-la dans les paramètres (⚙️ Configuration API).";
                }

                // Validation de la clé API
                if (!apiKey.StartsWith("gsk_"))
                {
                    return "❌ Clé API Groq invalide. Elle doit commencer par 'gsk_'\n\nObtenez-en une sur: https://console.groq.com/keys";
                }

                // Préparer la requête multipart
                var content = new MultipartFormDataContent();

                // Ajouter le fichier audio
                var audioContent = new ByteArrayContent(audioData);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(audioContent, "file", "recording.wav");

                // Paramètres
                content.Add(new StringContent("whisper-large-v3-turbo"), "model");
                content.Add(new StringContent("fr"), "language");
                content.Add(new StringContent("0.0"), "temperature");

                // Créer la requête
                var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://api.groq.com/openai/v1/audio/transcriptions");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = content;

                System.Diagnostics.Debug.WriteLine("📡 Envoi requête à Groq...");

                // Envoyer la requête
                var response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur Groq: {error}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return "❌ Clé API Groq invalide ou expirée.\n\nVérifiez votre clé sur: https://console.groq.com/keys";
                    }

                    return $"❌ Erreur API Groq ({response.StatusCode}):\n{error}";
                }

                // Lire la réponse
                string jsonResponse = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Réponse Groq reçue: {jsonResponse}");

                using (JsonDocument json = JsonDocument.Parse(jsonResponse))
                {
                    if (json.RootElement.TryGetProperty("text", out var textProp))
                    {
                        string text = textProp.GetString() ?? "";

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            return "❌ Aucun texte détecté. Parlez plus fort ou plus clairement.";
                        }

                        return text.Trim();
                    }

                    return "❌ Format de réponse inattendu de Groq.";
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur réseau: {ex.Message}");
                return $"❌ Erreur réseau:\n{ex.Message}\n\nVérifiez votre connexion internet.";
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur JSON: {ex.Message}");
                return $"❌ Erreur format réponse:\n{ex.Message}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur transcription: {ex.Message}");
                return $"❌ Erreur transcription:\n{ex.Message}";
            }
        }
    }
}