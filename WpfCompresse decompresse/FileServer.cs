using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class FileServer
{
    private readonly int _port;
    private readonly string _rootFolder;

    public FileServer(string rootFolder, int port = 5000)
    {
        _rootFolder = rootFolder;
        _port = port;
    }

    public async Task StartAsync()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"Serveur démarré sur le port {_port}.");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        string command = await reader.ReadLineAsync();
        if (command == "LIST")
        {
            foreach (var dir in Directory.GetDirectories(_rootFolder))
                await writer.WriteLineAsync(Path.GetFileName(dir));
        }
        else if (command.StartsWith("DOWNLOAD "))
        {
            string folderName = command.Substring(9);
            string folderPath = Path.Combine(_rootFolder, folderName);

            await SendFolderAsync(folderPath, stream);
        }

        client.Close();
    }

    private async Task SendFolderAsync(string folderPath, NetworkStream stream)
    {
        foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(folderPath, file);
            await new StreamWriter(stream) { AutoFlush = true }.WriteLineAsync(relative);
            byte[] data = File.ReadAllBytes(file);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }
}
