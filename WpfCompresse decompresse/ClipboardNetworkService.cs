using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class ClipboardNetworkService
{
    private const int Port = 50505;
    private TcpListener? listener;

    public event Action<string>? DirectoryReceived;

    private readonly Dictionary<string, string> _localFolders = new();

    public void Start()
    {
        StartTcpServer();
        StartAutoDiscovery();
    }

    // --------------------------
    // 1) SERVEUR TCP POUR RECEVOIR LES MESSAGES
    // --------------------------
    private void StartTcpServer()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            ListenLoop();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Erreur lancement serveur TCP : " + ex.Message);
        }
    }

    private async void ListenLoop()
    {
        while (true)
        {
            try
            {
                TcpClient client = await listener!.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client));
            }
            catch { }
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string message = await reader.ReadLineAsync() ?? "";

        if (string.IsNullOrWhiteSpace(message))
            return;

        string senderMachine = message.Split('|')[0];

        if (senderMachine != Environment.MachineName)
            DirectoryReceived?.Invoke(message);
    }

    // --------------------------
    // 2) AUTO-SCAN DU RÉSEAU POUR DÉTECTER LES MACHINES
    // --------------------------
    private async void StartAutoDiscovery()
    {
        await Task.Delay(2000); // laisse le programme démarrer

        string baseIP = GetNetworkBase(); // exemple: "192.168.43."

        for (int i = 1; i < 255; i++)
        {
            string ip = baseIP + i;

            if (ip == GetLocalIPAddress()) continue;

            _ = Task.Run(() => TryConnect(ip));
        }
    }

    private void TryConnect(string ip)
    {
        try
        {
            using TcpClient client = new TcpClient();
            client.SendTimeout = 1000;
            client.ReceiveTimeout = 1000;

            client.Connect(ip, Port);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            writer.WriteLine($"{Environment.MachineName}|{GetLocalIPAddress()}|DISCOVER");
        }
        catch
        {
            // IP ne réponds pas → ignore
        }
    }

    // --------------------------
    // ENVOI DOSSIER À TOUTES LES MACHINES TROUVÉES
    // --------------------------
    public void BroadcastDirectory(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return;

        _ = Task.Run(async () =>
        {
            string baseIP = GetNetworkBase();
            string folderName = Path.GetFileName(folderPath);

            _localFolders[folderName] = folderPath;

            for (int i = 1; i < 255; i++)
            {
                string ip = baseIP + i;

                if (ip == GetLocalIPAddress()) continue;

                try
                {
                    using TcpClient client = new TcpClient();
                    client.Connect(ip, Port);

                    using var stream = client.GetStream();
                    using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                    writer.WriteLine($"{Environment.MachineName}|{GetLocalIPAddress()}|{folderName}");
                }
                catch
                {
                    // ignore si ne répond pas
                }
            }
        });
    }

    // --------------------------
    // HELPERS
    // --------------------------
    public string? GetLocalFolderPath(string folderName)
    {
        return _localFolders.TryGetValue(folderName, out var path) ? path : null;
    }

    public string GetLocalIPAddress()
    {
        foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is System.Net.NetworkInformation.NetworkInterfaceType.Loopback or System.Net.NetworkInformation.NetworkInterfaceType.Tunnel) continue;

            var ipProps = ni.GetIPProperties();
            foreach (var addr in ipProps.UnicastAddresses)
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    return addr.Address.ToString();
        }
        return "127.0.0.1";
    }

    private string GetNetworkBase()
    {
        string ip = GetLocalIPAddress();   // ex: 192.168.43.55
        string[] parts = ip.Split('.');
        return $"{parts[0]}.{parts[1]}.{parts[2]}.";
    }
}
