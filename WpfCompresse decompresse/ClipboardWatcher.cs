using System.Windows.Interop;
using WpfCompresse_decompresse;
using System;
using System.Windows;
using System.Windows.Interop;


namespace WpfCompresse_decompresse
{
    public static class ClipboardWatcher
    {
        private static HwndSource? _source;
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        public static event Action? ClipboardChanged;

        public static void Start(Window window)
        {
            if (!window.IsLoaded)
            {
                window.Loaded += (_, __) => Initialize(window);
            }
            else
            {
                Initialize(window);
            }
        }

        private static void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) throw new InvalidOperationException("Impossible d’obtenir le handle.");

            _source = HwndSource.FromHwnd(hwnd) ?? throw new InvalidOperationException("Impossible de créer HwndSource.");
            _source.AddHook(WndProc);

            NativeMethods.AddClipboardFormatListener(hwnd);
        }

        public static void Stop(Window window)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd != IntPtr.Zero) NativeMethods.RemoveClipboardFormatListener(hwnd);
                if (_source != null)
                {
                    _source.RemoveHook(WndProc);
                    _source = null;
                }
            }
            catch { /* silencieux */ }
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE) ClipboardChanged?.Invoke();
            return IntPtr.Zero;
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    }
}
