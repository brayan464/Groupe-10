using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace WpfCompresse_decompresse
{
    public partial class MainWindow : Window
    {
        private bool IsServer = true;
        private int Port = 5000;
        private string RemoteIP = "192.168.1.100";

        private DispatcherTimer clipboardTimer;
        private string lastClipboardPath = "";

        public MainWindow()
        {
            InitializeComponent();
            InitTreeView();
            if (!IsServer)
            {
                clipboardTimer = new DispatcherTimer();
                clipboardTimer.Interval = TimeSpan.FromMilliseconds(500);
                clipboardTimer.Tick += ClipboardTimer_Tick;
                clipboardTimer.Start();
            }
            else
            {
                StartListener();
            }
        }

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
                    var dirNode = new TreeViewItem { Header = Path.GetFileName(dir), Tag = dir };
                    dirNode.Items.Add("...");
                    dirNode.Expanded += TreeNode_Expanded;
                    node.Items.Add(dirNode);
                }
                foreach (var file in Directory.GetFiles(path))
                {
                    node.Items.Add(new TreeViewItem { Header = Path.GetFileName(file), Tag = file });
                }
            }
            catch { }
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FileTreeView.SelectedItem is TreeViewItem item)
                FilePathBox.Text = item.Tag.ToString();
        }

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
                Dispatcher.Invoke(() => InitTreeView());
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
            Dispatcher.Invoke(() => InitTreeView());
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
            Dispatcher.Invoke(() => InitTreeView());
        }

        private async void CompressCustom_Click(object sender, RoutedEventArgs e)
        {
            string source = FilePathBox.Text;
            string ext = CustomExtBox.Text.Trim();
            if (string.IsNullOrEmpty(source) || !File.Exists(source)) { MessageBox.Show("Sélectionnez un fichier."); return; }
            if (string.IsNullOrEmpty(ext) || ext == ".ATG" || !ext.StartsWith(".")) { MessageBox.Show("Entrez une extension valide."); return; }
            string dest = Path.ChangeExtension(source, ext);
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
            MessageBox.Show($"Compression terminée : {dest}");
            Dispatcher.Invoke(() => InitTreeView());
        }

        private async void DecompressCustom_Click(object sender, RoutedEventArgs e)
        {
            string source = FilePathBox.Text;
            string ext = CustomExtBox.Text.Trim();
            if (string.IsNullOrEmpty(source) || !File.Exists(source)) { MessageBox.Show("Sélectionnez un fichier."); return; }
            if (string.IsNullOrEmpty(ext) || !ext.StartsWith(".")) { MessageBox.Show("Entrez une extension valide."); return; }
            string dest = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source));
            Directory.CreateDirectory(dest);
            ProgressBar.Value = 0;
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
            MessageBox.Show($"Décompression terminée : {dest}");
            Dispatcher.Invoke(() => InitTreeView());
        }

        private void CopyPath_Click(object sender, RoutedEventArgs e)
        {
            string path = FilePathBox.Text;
            if (!string.IsNullOrEmpty(path) && !CopiedFilesListBox.Items.Contains(path))
            {
                CopiedFilesListBox.Items.Add(path);
                if (!IsServer)
                    SendPathToServer(path);
            }
        }

        private void CopiedFilesListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CopiedFilesListBox.SelectedItem != null)
                FilePathBox.Text = CopiedFilesListBox.SelectedItem.ToString();
        }

        private void ClipboardTimer_Tick(object sender, EventArgs e)
        {
            if (System.Windows.Clipboard.ContainsFileDropList())
            {
                var files = System.Windows.Clipboard.GetFileDropList();
                if (files.Count > 0 && files[0] != lastClipboardPath)
                {
                    lastClipboardPath = files[0];
                    FilePathBox.Text = lastClipboardPath;
                    SendPathToServer(lastClipboardPath);
                }
            }
        }

        private async void StartListener()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string path = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Dispatcher.Invoke(() =>
                {
                    if (!CopiedFilesListBox.Items.Contains(path))
                        CopiedFilesListBox.Items.Add(path);
                });
            }
        }

        private void SendPathToServer(string path)
        {
            Task.Run(() =>
            {
                try
                {
                    using (TcpClient client = new TcpClient(RemoteIP, Port))
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] data = Encoding.UTF8.GetBytes(path);
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch { }
            });
        }
    }

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
                if (bitCount > 0) buffer <<= (8 - bitCount);
                bw.Write(buffer);
                bw.Write((byte)(bitCount == 0 ? 0 : 8 - bitCount));
            }
            progressCallback?.Invoke(100);
        }

        public static void Decompresser(string sourceFile, string destinationFile, Action<int> progressCallback)
        {
            using (var br = new BinaryReader(File.Open(sourceFile, FileMode.Open)))
            using (var fsOut = new FileStream(destinationFile, FileMode.Create))
            {
                int count = br.ReadInt32();
                Dictionary<byte, int> freqs = new Dictionary<byte, int>();
                for (int i = 0; i < count; i++) { byte b = br.ReadByte(); int f = br.ReadInt32(); freqs[b] = f; }
                Node root = BuildTree(freqs);
                byte pad = br.ReadByte();
                Node current = root;
                long totalBytes = freqs.Values.Sum();
                long written = 0;
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    byte b = br.ReadByte();
                    for (int i = 7; i >= 0; i--)
                    {
                        bool bit = (b & (1 << i)) != 0;
                        current = bit ? current.Right : current.Left;
                        if (current.IsLeaf)
                        {
                            fsOut.WriteByte(current.Value.Value);
                            written++;
                            current = root;
                            if (written % 1000 == 0)
                                progressCallback?.Invoke((int)((written / (double)totalBytes) * 100));
                        }
                    }
                }
                if (pad > 0) fsOut.SetLength(fsOut.Length - 1);
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
                Node left = queue[0], right = queue[1];
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
}
