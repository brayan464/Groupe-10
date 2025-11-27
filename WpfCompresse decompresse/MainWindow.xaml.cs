using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;



namespace WpfCompresse_decompresse
{
    public partial class MainWindow : Window
    {
        private ClipboardNetworkService networkService;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            networkService = new ClipboardNetworkService();
            networkService.DirectoryReceived += OnNetworkDirectoryReceived;

            DispatcherTimer clipboardWatcher = new DispatcherTimer();
            clipboardWatcher.Interval = TimeSpan.FromMilliseconds(500);
            clipboardWatcher.Tick += ClipboardWatcher_Tick;
            clipboardWatcher.Start();

            ClipboardWatcher.Start(this);
            ClipboardWatcher.ClipboardChanged += OnClipboardChanged;

            LoadNetworkClipboard();

            InitTreeView(); // si tu veux initialiser le TreeView dès le départ
        }


        private void OnClipboardChanged()
        {
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files.Count == 1 && Directory.Exists(files[0]))
                {
                    lastClipboardValue = files[0];
                    FilePathBox.Text = files[0];
                }
            }
        }


        public static class TcpFileClient
{
    public static async Task DownloadFolder(string ip, int port, string destinationFolder)
    {
        if (!Directory.Exists(destinationFolder))
            Directory.CreateDirectory(destinationFolder);

        using var client = new TcpClient();
        await client.ConnectAsync(ip, port);

        using var ns = client.GetStream();
        using var br = new BinaryReader(ns);

        while (true)
        {
            int pathLen = br.ReadInt32();

            // Fin de transmission
            if (pathLen == 0)
                break;

            // Lire chemin relatif
            byte[] pathBytes = br.ReadBytes(pathLen);
            string relativePath = System.Text.Encoding.UTF8.GetString(pathBytes);

            // Lire contenu
            int contentLength = br.ReadInt32();
            byte[] content = br.ReadBytes(contentLength);

            // Construire le chemin complet
            string fullPath = Path.Combine(destinationFolder, relativePath);

            // Créer dossier si nécessaire
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            // Écrire fichier
            File.WriteAllBytes(fullPath, content);
        }
    }
}

        private void SaveToNetworkClipboard(string folderPath)
        {

            string machine = Environment.MachineName;
            string ip = networkService.GetLocalIPAddress();
            string entry = $"{machine}|{ip}|{folderPath}";

            if (!NetworkClipboardList.Items.Contains(entry))
                NetworkClipboardList.Items.Add(entry);

            var list = NetworkClipboardList.Items.Cast<string>().ToList();
            File.WriteAllText("network_clipboard.json", JsonConvert.SerializeObject(list));
        }

public static class TcpFileServer
    {
        public static void Start(string folderPath, int port = 6000)
        {
            Task.Run(async () =>
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => SendFolder(client, folderPath));
                }
            });
        }

        private static void SendFolder(TcpClient client, string folderPath)
        {
            using var ns = client.GetStream();
            using var bw = new BinaryWriter(ns);

            // On envoie tous les fichiers avec chemin relatif
            foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(folderPath, file);
                byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(relativePath);
                byte[] content = File.ReadAllBytes(file);

                bw.Write(pathBytes.Length);
                bw.Write(pathBytes);
                bw.Write(content.Length);
                bw.Write(content);
            }

            // Fin de transmission
            bw.Write(0);
            client.Close();
        }
}


        private async void NetworkClipboardList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NetworkClipboardList.SelectedItem == null) return;

            string selected = NetworkClipboardList.SelectedItem.ToString() ?? "";

            // Format attendu : "Machine|IP|Folder"
            string[] parts = selected.Split('|', StringSplitOptions.TrimEntries);

            if (parts.Length < 3)
            {
                MessageBox.Show("Format du message invalide.");
                return;
            }

            string machine = parts[0];
            string ip = parts[1];
            string folderName = parts[2];

            // ⛔ PAS DE LOGIN — DIRECT DOWNLOAD
            try
            {
                await TcpFileClient.DownloadFolder(ip, 6000, folderName);
                MessageBox.Show("Téléchargement terminé !");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur téléchargement : " + ex.Message);
            }
        }



        // Méthode récursive pour copier un dossier complet via SFTP
        private void DownloadSftpDirectory(Renci.SshNet.SftpClient sftp, string remotePath, string localPath)
        {
            if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);

            var files = sftp.ListDirectory(remotePath);
            long totalFiles = files.Count(f => !f.IsDirectory && !f.IsSymbolicLink);
            long copiedFiles = 0;

            foreach (var file in files)
            {
                if (file.Name == "." || file.Name == "..") continue;

                string localFilePath = Path.Combine(localPath, file.Name);
                if (file.IsDirectory)
                {
                    DownloadSftpDirectory(sftp, file.FullName, localFilePath);
                }
                else
                {
                    using var fs = File.OpenWrite(localFilePath);
                    sftp.DownloadFile(file.FullName, fs);
                    copiedFiles++;
                    double percent = (double)copiedFiles / totalFiles * 100;
                    Application.Current.Dispatcher.Invoke(() => ProgressBar.Value = percent);
                }
            }

            Application.Current.Dispatcher.Invoke(() => ProgressBar.Value = 100);
        }






        private void CopyDirectoryWithProgress(string sourceDir, string destinationDir)
        {
            var allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            long totalFiles = allFiles.Length;
            long copiedFiles = 0;

            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string subDir = dirPath.Replace(sourceDir, destinationDir);
                Directory.CreateDirectory(subDir);
            }

            foreach (var filePath in allFiles)
            {
                string destFile = filePath.Replace(sourceDir, destinationDir);
                File.Copy(filePath, destFile, true);
                copiedFiles++;

                double percent = (double)copiedFiles / totalFiles * 100;
                Dispatcher.Invoke(() => ProgressBar.Value = percent);
            }

            Dispatcher.Invoke(() => ProgressBar.Value = 100);
        }




        public class NetworkClipboardEntry
        {
            public string machine { get; set; } = "";
            public string path { get; set; } = "";
            public DateTime timestamp { get; set; }
        }




        private string? lastClipboardValue = null;

        private void ClipboardWatcher_Tick(object? sender, EventArgs e)
        {
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files.Count > 0)
                {
                    string path = files[0];

                    if (path != lastClipboardValue && Directory.Exists(path))
                    {
                        lastClipboardValue = path;
                        networkService.BroadcastDirectory(path);
                    }
                }
            }
        }


        private void OnNetworkDirectoryReceived(string message)
        {
            // message = "MachineName|IP|Chemin"
            Dispatcher.Invoke(() =>
            {
                if (!NetworkClipboardList.Items.Contains(message))
                    NetworkClipboardList.Items.Add(message);
            });
        }

        private void LoadNetworkClipboard()
        {
            NetworkClipboardList.Items.Clear();

            if (!File.Exists("network_clipboard.json"))
                return;

            try
            {
                var entries = JsonConvert.DeserializeObject<List<string>>(
                    File.ReadAllText("network_clipboard.json")
                );

                if (entries != null)
                {
                    foreach (var entry in entries)
                        NetworkClipboardList.Items.Add(entry);
                }
            }
            catch
            {
                // fichier corrompu → on l’ignore
            }
        }




        #region Arborescence
        private void InitTreeView()
        {
            FileTreeView.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var node = new TreeViewItem { Header = drive.Name, Tag = drive.Name };
                node.Items.Add("...");
                node.Expanded += TreeNode_Expanded;
                FileTreeView.Items.Add(node);
            }
        }

        private void TreeNode_Expanded(object sender, RoutedEventArgs e)
        {
            var node = sender as TreeViewItem;
            if (node == null || node.Items.Count != 1 || !(node.Items[0] is string)) return;
            node.Items.Clear();

            string path = node.Tag.ToString();
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirNode = new TreeViewItem { Header = System.IO.Path.GetFileName(dir), Tag = dir };
                    dirNode.Items.Add("...");
                    dirNode.Expanded += TreeNode_Expanded;
                    node.Items.Add(dirNode);
                }
                foreach (var file in Directory.GetFiles(path))
                {
                    node.Items.Add(new TreeViewItem { Header = System.IO.Path.GetFileName(file), Tag = file });
                }
            }
            catch { /* accès refusé */ }
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FileTreeView.SelectedItem is TreeViewItem item)
            {
                FilePathBox.Text = item.Tag.ToString();
            }
        }
        #endregion

        #region Compression / Décompression
        private async void CompressATG_Click(object sender, RoutedEventArgs e)
        {
            string source = FilePathBox.Text;
            if (string.IsNullOrEmpty(source) || !File.Exists(source)) { MessageBox.Show("Sélectionnez un fichier."); return; }
            string dest = Path.ChangeExtension(source, ".ATG");
            ProgressBar.Value = 0;

            try
            {
                await Task.Run(() => HuffmanCompression.Compresser(source, dest, percent =>
                    Dispatcher.Invoke(() => ProgressBar.Value = percent)
                ));
                MessageBox.Show($"Compression Huffman terminée : {dest}");
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private async void CompressZIP_Click(object sender, RoutedEventArgs e)
        {
            string source = FilePathBox.Text;
            if (string.IsNullOrEmpty(source) || !File.Exists(source)) { MessageBox.Show("Sélectionnez un fichier."); return; }
            string dest = Path.ChangeExtension(source, ".zip");
            ProgressBar.Value = 0;

            await Task.Run(() =>
            {
                using (FileStream zipToOpen = new FileStream(dest, FileMode.Create))
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    var entry = archive.CreateEntry(Path.GetFileName(source));
                    using (var entryStream = entry.Open())
                    using (var fs = File.OpenRead(source))
                    {
                        byte[] buffer = new byte[8192];
                        int read;
                        long total = 0;
                        long length = fs.Length;
                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            entryStream.Write(buffer, 0, read);
                            total += read;
                            double percent = (double)total / length * 100;
                            Dispatcher.Invoke(() => ProgressBar.Value = percent);
                        }
                    }
                }
            });

            MessageBox.Show($"Compression ZIP terminée : {dest}");
        }

        private async void Decompress_Click(object sender, RoutedEventArgs e)
        {
            string source = FilePathBox.Text;
            if (string.IsNullOrEmpty(source) || !File.Exists(source)) { MessageBox.Show("Sélectionnez un fichier."); return; }

            string dest;
            ProgressBar.Value = 0;

            if (source.EndsWith(".ATG"))
            {
                dest = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source));
                await Task.Run(() => HuffmanCompression.Decompresser(source, dest, percent =>
                    Dispatcher.Invoke(() => ProgressBar.Value = percent)
                ));
                MessageBox.Show($"Décompression Huffman terminée : {dest}");
            }
            else if (source.EndsWith(".zip"))
            {
                dest = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source));
                Directory.CreateDirectory(dest);

                await Task.Run(() =>
                {
                    using (ZipArchive archive = ZipFile.OpenRead(source))
                    {
                        long total = archive.Entries.Sum(x => x.Length);
                        long processed = 0;

                        foreach (var entry in archive.Entries)
                        {
                            string fileDest = Path.Combine(dest, entry.FullName);
                            Directory.CreateDirectory(Path.GetDirectoryName(fileDest));
                            entry.ExtractToFile(fileDest, true);
                            processed += entry.Length;
                            double percent = (double)processed / total * 100;
                            Dispatcher.Invoke(() => ProgressBar.Value = percent);
                        }
                    }
                });

                MessageBox.Show($"Décompression ZIP terminée : {dest}");
            }
            else
            {
                MessageBox.Show("Format de fichier non supporté pour la décompression.");
            }
        }
        #endregion
    }

    #region Classe Huffman
    public static class HuffmanCompression
    {
        class Node
        {
            public byte? Value;
            public int Frequency;
            public Node Left;
            public Node Right;
            public bool IsLeaf => Value.HasValue;
        }

        public static void Compresser(string sourceFile, string destinationFile, Action<int> progressCallback)
        {
            byte[] data = File.ReadAllBytes(sourceFile);
            Dictionary<byte, int> freqs = new Dictionary<byte, int>();
            for (int i = 0; i < data.Length; i++)
            {
                if (!freqs.ContainsKey(data[i])) freqs[data[i]] = 0;
                freqs[data[i]]++;
                if (i % 1000 == 0) progressCallback?.Invoke((int)((i / (double)data.Length) * 20));
            }

            Node root = BuildTree(freqs);
            Dictionary<byte, string> codes = new Dictionary<byte, string>();
            GenerateCodes(root, "", codes);

            using (var bw = new BinaryWriter(File.Open(destinationFile, FileMode.Create)))
            {
                bw.Write(freqs.Count);
                foreach (var kv in freqs) { bw.Write(kv.Key); bw.Write(kv.Value); }

                byte buffer = 0;
                int bitCount = 0;

                for (int i = 0; i < data.Length; i++)
                {
                    string code = codes[data[i]];
                    foreach (char bit in code)
                    {
                        buffer <<= 1;
                        if (bit == '1') buffer |= 1;
                        bitCount++;
                        if (bitCount == 8)
                        {
                            bw.Write(buffer);
                            buffer = 0;
                            bitCount = 0;
                        }
                    }
                    if (i % 1000 == 0) progressCallback?.Invoke(20 + (int)((i / (double)data.Length) * 70));
                }

                if (bitCount > 0)
                {
                    buffer <<= (8 - bitCount);
                    bw.Write(buffer);
                }

                byte pad = (byte)(bitCount == 0 ? 0 : 8 - bitCount);
                bw.Write(pad);
            }

            progressCallback?.Invoke(100);
        }

        public static void Decompresser(string sourceFile, string destinationFile, Action<int> progressCallback)
        {
            using (var br = new BinaryReader(File.Open(sourceFile, FileMode.Open)))
            {
                int count = br.ReadInt32();
                Dictionary<byte, int> freqs = new Dictionary<byte, int>();
                for (int i = 0; i < count; i++) { byte b = br.ReadByte(); int f = br.ReadInt32(); freqs[b] = f; }

                Node root = BuildTree(freqs);
                byte pad = br.ReadByte();

                List<byte> compressedData = new List<byte>();
                while (br.BaseStream.Position != br.BaseStream.Length)
                    compressedData.Add(br.ReadByte());

                string bitString = "";
                for (int i = 0; i < compressedData.Count; i++)
                {
                    bitString += Convert.ToString(compressedData[i], 2).PadLeft(8, '0');
                    if (i % 1000 == 0) progressCallback?.Invoke((int)((i / (double)compressedData.Count) * 50));
                }
                bitString = bitString.Substring(0, bitString.Length - pad);

                List<byte> result = new List<byte>();
                Node current = root;
                for (int i = 0; i < bitString.Length; i++)
                {
                    current = (bitString[i] == '0') ? current.Left : current.Right;
                    if (current.IsLeaf)
                    {
                        result.Add(current.Value.Value);
                        current = root;
                    }
                    if (i % 1000 == 0) progressCallback?.Invoke(50 + (int)((i / (double)bitString.Length) * 50));
                }

                File.WriteAllBytes(destinationFile, result.ToArray());
                progressCallback?.Invoke(100);
            }
        }

        private static Node BuildTree(Dictionary<byte, int> freqs)
        {
            var queue = new List<Node>();
            foreach (var kv in freqs) queue.Add(new Node { Value = kv.Key, Frequency = kv.Value });

            while (queue.Count > 1)
            {
                queue = queue.OrderBy(n => n.Frequency).ToList();
                Node left = queue[0]; Node right = queue[1];
                queue.RemoveRange(0, 2);
                queue.Add(new Node { Left = left, Right = right, Frequency = left.Frequency + right.Frequency });
            }
            return queue[0];
        }

        private static void GenerateCodes(Node node, string prefix, Dictionary<byte, string> codes)
        {
            if (node.IsLeaf) codes[node.Value.Value] = prefix;
            else
            {
                if (node.Left != null) GenerateCodes(node.Left, prefix + "0", codes);
                if (node.Right != null) GenerateCodes(node.Right, prefix + "1", codes);
            }
        }
    }
    #endregion
}
