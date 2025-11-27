using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

public class FileClient
{
    private readonly string _serverIp;
    private readonly int _port;

    public FileClient(string serverIp, int port = 5000)
    {
        _serverIp = serverIp;
        _port = port;
    }

    public async Task<string[]> GetFoldersAsync()
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_serverIp, _port);

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        await writer.WriteLineAsync("LIST");

        var folders = new System.Collections.Generic.List<string>();
        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            folders.Add(line);
        }

        return folders.ToArray();
    }

    public async Task DownloadFolderAsync(string folderName, string localPath)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_serverIp, _port);

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        await writer.WriteLineAsync($"DOWNLOAD {folderName}");

        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            string localFile = Path.Combine(localPath, line);
            Directory.CreateDirectory(Path.GetDirectoryName(localFile)!);
            // Lecture du fichier binaire
            byte[] buffer = new byte[4096];
            int read;
            using var fs = File.Create(localFile);
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, read);
                if (read < buffer.Length) break;
            }
        }
    }
}
