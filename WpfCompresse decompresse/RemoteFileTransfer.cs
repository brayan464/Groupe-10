using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public static class RemoteFileTransfer
{
    /// <summary>
    /// Télécharge un dossier distant via SFTP de manière asynchrone.
    /// </summary>
    /// <param name="host">Adresse du serveur SFTP</param>
    /// <param name="username">Nom d'utilisateur</param>
    /// <param name="password">Mot de passe</param>
    /// <param name="remotePath">Chemin distant du dossier</param>
    /// <param name="localPath">Chemin local où sauvegarder</param>
    /// <param name="progressCallback">Action pour rapporter la progression (0 à 100)</param>
    /// <returns>Task asynchrone</returns>
    public static async Task DownloadDirectoryAsync(string host, string username, string password,
                                                    string remotePath, string localPath,
                                                    Action<double>? progressCallback = null)
    {
        try
        {
            using var client = new SftpClient(host, username, password);
            client.Connect();

            var allFiles = GetAllFiles(client, remotePath);
            int totalFiles = allFiles.Count;
            int filesCopied = 0;

            foreach (var file in allFiles)
            {
                string relativePath = file.Substring(remotePath.Length).TrimStart('/', '\\');
                string localFilePath = Path.Combine(localPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

                string dir = Path.GetDirectoryName(localFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                await CopyFileWithRetryAsync(client, file, localFilePath, 3);

                filesCopied++;
                progressCallback?.Invoke((double)filesCopied / totalFiles * 100);
            }

            client.Disconnect();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Erreur SFTP : " + ex.Message);
        }
    }

    /// <summary>
    /// Copie un fichier distant avec retry si verrouillé
    /// </summary>
    private static async Task CopyFileWithRetryAsync(SftpClient client, string remoteFile, string localFile, int retryCount)
    {
        int attempts = 0;
        while (attempts < retryCount)
        {
            try
            {
                using var fs = File.Open(localFile, FileMode.Create, FileAccess.Write, FileShare.None);
                client.DownloadFile(remoteFile, fs);
                break; // succès
            }
            catch (IOException)
            {
                attempts++;
                if (attempts >= retryCount)
                    Console.WriteLine($"Impossible de copier : {localFile}");
                else
                    await Task.Delay(200); // attendre un peu
            }
        }
    }

    /// <summary>
    /// Récupère tous les fichiers d'un dossier distant (récursif)
    /// </summary>
    private static List<string> GetAllFiles(SftpClient client, string remotePath)
    {
        var files = new List<string>();

        void Recurse(string path)
        {
            foreach (var entry in client.ListDirectory(path))
            {
                if (entry.Name == "." || entry.Name == "..") continue;

                if (entry.IsDirectory) Recurse(entry.FullName);
                else files.Add(entry.FullName);
            }
        }

        Recurse(remotePath);
        return files;
    }
}
