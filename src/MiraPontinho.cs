using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MiraPontinho
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main(string[] args)
        {
            try { SetProcessDPIAware(); }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Settings s = Settings.Load();
            ApplyArgs(s, args);

            bool silent = HasFlag(args, "--silent");

            if (HasFlag(args, "--help-manual"))
            {
                Application.Run(new HelpForm(s));
                return;
            }

            if (HasFlag(args, "--hide-cs") || HasFlag(args, "--show-cs"))
            {
                bool hide = HasFlag(args, "--hide-cs");
                string dir = CsConfig.FindCstrike(s);
                if (dir == null)
                {
                    MessageBox.Show("Nao achei a pasta do CS. Abra o CS uma vez e tente de novo.",
                        "Mira Pontinho", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                s.CstrikePath = dir;
                s.HideCsCrosshair = hide;
                s.Save();
                string msg;
                CsConfig.Apply(dir, hide, s, out msg);
                MessageBox.Show(msg + "\n\n" + dir, "Mira Pontinho",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.Run(new TrayContext(s, !silent));
        }

        private static bool HasFlag(string[] a, string flag)
        {
            foreach (string x in a)
                if (string.Equals(x, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void ApplyArgs(Settings s, string[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                string k = a[i].ToLowerInvariant();
                try
                {
                    switch (k)
                    {
                        case "--r": s.R = int.Parse(a[++i]); break;
                        case "--g": s.G = int.Parse(a[++i]); break;
                        case "--b": s.B = int.Parse(a[++i]); break;
                        case "--size": s.Size = int.Parse(a[++i]); break;
                        case "--outline": s.Outline = int.Parse(a[++i]); break;
                        case "--always": s.OnlyInGame = false; break;
                        case "--screen": s.ForceScreenCenter = true; break;
                        case "--borderless": s.Borderless = true; break;
                        case "--no-borderless": s.Borderless = false; break;
                        case "--hide-scope": s.HideOnScope = true; break;
                        case "--no-hide-scope": s.HideOnScope = false; break;
                        case "--scope-threshold": s.ScopeThreshold = int.Parse(a[++i]); break;
                    }
                }
                catch { }
            }
            s.Normalize();
        }
    }

    internal sealed class Settings
    {
        public string Shape = "dot";      // dot, cross, t, circle, x
        public int R = 0;
        public int G = 255;
        public int B = 0;
        public int Size = 6;              // diametro do ponto / comprimento do braco
        public int Thickness = 1;
        public int Gap = 3;
        public int Outline = 1;
        public bool OnlyInGame = true;    // mostra so quando o CS esta em foco
        public bool Borderless = true;    // deixa a janela do CS em tela cheia sem borda
        public bool ForceScreenCenter = false;
        public bool HideCsCrosshair = false; // oculta a mira nativa do CS (via autoexec.cfg)
        public bool HideOnScope = true;      // esconde o ponto quando da zoom (AWP/Scout)
        public int ScopeThreshold = 55;      // % de tela preta pra considerar "scoped"
        public string CstrikePath = "";
        public string OrigCrosshairColor = "50 250 50";
        public string OrigCrosshairTranslucent = "0";

        public Color Color { get { return Color.FromArgb(255, R, G, B); } }

        public static string IniPath()
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(dir, "MiraPontinho.ini");
        }

        public static Settings Load()
        {
            Settings s = new Settings();
            try
            {
                if (!File.Exists(IniPath())) return s;
                foreach (string line in File.ReadAllLines(IniPath()))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string v = line.Substring(eq + 1).Trim();
                    switch (k)
                    {
                        case "shape": s.Shape = v; break;
                        case "r": s.R = ParseInt(v, s.R); break;
                        case "g": s.G = ParseInt(v, s.G); break;
                        case "b": s.B = ParseInt(v, s.B); break;
                        case "size": s.Size = ParseInt(v, s.Size); break;
                        case "thickness": s.Thickness = ParseInt(v, s.Thickness); break;
                        case "gap": s.Gap = ParseInt(v, s.Gap); break;
                        case "outline": s.Outline = ParseInt(v, s.Outline); break;
                        case "onlyingame": s.OnlyInGame = v == "1"; break;
                        case "borderless": s.Borderless = v == "1"; break;
                        case "hidecs": s.HideCsCrosshair = v == "1"; break;
                        case "hideosscope": s.HideOnScope = v == "1"; break;
                        case "scopethreshold": s.ScopeThreshold = ParseInt(v, s.ScopeThreshold); break;
                        case "cstrikepath": s.CstrikePath = v; break;
                        case "origcolor": s.OrigCrosshairColor = v; break;
                        case "origtranslucent": s.OrigCrosshairTranslucent = v; break;
                    }
                }
            }
            catch { }
            s.Normalize();
            return s;
        }

        private static int ParseInt(string v, int d) { int r; return int.TryParse(v, out r) ? r : d; }

        public void Normalize()
        {
            R = Clamp(R, 0, 255); G = Clamp(G, 0, 255); B = Clamp(B, 0, 255);
            Size = Clamp(Size, 1, 60);
            Thickness = Clamp(Thickness, 1, 20);
            Gap = Clamp(Gap, 0, 40);
            Outline = Clamp(Outline, 0, 6);
            ScopeThreshold = Clamp(ScopeThreshold, 20, 95);
            if (Shape == null) Shape = "dot";
            Shape = Shape.ToLowerInvariant();
            if (Shape != "dot" && Shape != "cross" && Shape != "t" && Shape != "circle" && Shape != "x")
                Shape = "dot";
        }

        private static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("shape=" + Shape);
                sb.AppendLine("r=" + R);
                sb.AppendLine("g=" + G);
                sb.AppendLine("b=" + B);
                sb.AppendLine("size=" + Size);
                sb.AppendLine("thickness=" + Thickness);
                sb.AppendLine("gap=" + Gap);
                sb.AppendLine("outline=" + Outline);
                sb.AppendLine("onlyingame=" + (OnlyInGame ? "1" : "0"));
                sb.AppendLine("borderless=" + (Borderless ? "1" : "0"));
                sb.AppendLine("hidecs=" + (HideCsCrosshair ? "1" : "0"));
                sb.AppendLine("hideosscope=" + (HideOnScope ? "1" : "0"));
                sb.AppendLine("scopethreshold=" + ScopeThreshold);
                sb.AppendLine("cstrikepath=" + CstrikePath);
                sb.AppendLine("origcolor=" + OrigCrosshairColor);
                sb.AppendLine("origtranslucent=" + OrigCrosshairTranslucent);
                File.WriteAllText(IniPath(), sb.ToString());
            }
            catch { }
        }
    }

    internal static class CrosshairRenderer
    {
        public static int Extent(Settings s)
        {
            switch (s.Shape)
            {
                case "dot": return s.Size;
                case "circle": return s.Size + s.Thickness;
                default: return 2 * (s.Gap + s.Size) + s.Thickness;
            }
        }

        public static void Draw(Graphics g, float cx, float cy, Settings s, float scale)
        {
            g.SmoothingMode = SmoothingMode.None;
            float size = s.Size * scale;
            float thick = s.Thickness * scale;
            float gap = s.Gap * scale;
            float outline = s.Outline * scale;

            if (s.Shape == "dot")
            {
                if (outline > 0)
                    using (SolidBrush b = new SolidBrush(Color.Black))
                        g.FillEllipse(b, cx - size / 2 - outline, cy - size / 2 - outline, size + 2 * outline, size + 2 * outline);
                using (SolidBrush b = new SolidBrush(s.Color))
                    g.FillEllipse(b, cx - size / 2, cy - size / 2, size, size);
                return;
            }

            if (s.Shape == "circle")
            {
                float d = size;
                if (outline > 0)
                    using (Pen p = new Pen(Color.Black, thick + 2 * outline))
                        g.DrawEllipse(p, cx - d / 2, cy - d / 2, d, d);
                using (Pen p = new Pen(s.Color, thick))
                    g.DrawEllipse(p, cx - d / 2, cy - d / 2, d, d);
                return;
            }

            if (s.Shape == "x")
            {
                float a = gap + size;
                DrawLine(g, cx - a, cy - a, cx + a, cy + a, thick, outline, s.Color);
                DrawLine(g, cx - a, cy + a, cx + a, cy - a, thick, outline, s.Color);
                return;
            }

            bool up = s.Shape == "cross";
            float h = thick / 2;
            if (up) FillArm(g, cx - h, cy - gap - size, thick, size, outline, s.Color);
            FillArm(g, cx - h, cy + gap, thick, size, outline, s.Color);
            FillArm(g, cx - gap - size, cy - h, size, thick, outline, s.Color);
            FillArm(g, cx + gap, cy - h, size, thick, outline, s.Color);
        }

        private static void FillArm(Graphics g, float x, float y, float w, float h, float outline, Color color)
        {
            if (outline > 0)
                using (SolidBrush b = new SolidBrush(Color.Black))
                    g.FillRectangle(b, x - outline, y - outline, w + 2 * outline, h + 2 * outline);
            using (SolidBrush b = new SolidBrush(color))
                g.FillRectangle(b, x, y, w, h);
        }

        private static void DrawLine(Graphics g, float x0, float y0, float x1, float y1, float thick, float outline, Color color)
        {
            if (outline > 0)
                using (Pen p = new Pen(Color.Black, thick + 2 * outline))
                {
                    p.StartCap = LineCap.Flat; p.EndCap = LineCap.Flat;
                    g.DrawLine(p, x0, y0, x1, y1);
                }
            using (Pen p = new Pen(color, thick))
            {
                p.StartCap = LineCap.Flat; p.EndCap = LineCap.Flat;
                g.DrawLine(p, x0, y0, x1, y1);
            }
        }
    }

    internal static class CsConfig
    {
        private const string Begin = "// === Mira Pontinho ===";
        private const string End = "// === fim Mira Pontinho ===";

        public static string FindCstrike(Settings s)
        {
            // 1) CS rodando agora
            try
            {
                foreach (Process p in Process.GetProcessesByName("hl"))
                {
                    try
                    {
                        string exe = p.MainModule.FileName;
                        string dir = Path.Combine(Path.GetDirectoryName(exe), "cstrike");
                        if (Directory.Exists(dir)) return dir;
                    }
                    catch { }
                }
            }
            catch { }

            // 2) caminho salvo
            if (!string.IsNullOrEmpty(s.CstrikePath) && Directory.Exists(s.CstrikePath))
                return s.CstrikePath;

            // 3) caminhos comuns
            string[] guesses = {
                @"C:\Program Files (x86)\Steam\steamapps\common\Half-Life\cstrike",
                @"C:\Program Files\Steam\steamapps\common\Half-Life\cstrike",
                @"C:\Games\Counter-Strike WaRzOnE\cstrike",
                @"D:\Steam\steamapps\common\Half-Life\cstrike",
                @"D:\Games\Counter-Strike WaRzOnE\cstrike",
            };
            foreach (string g in guesses)
                if (Directory.Exists(g)) return g;

            return null;
        }

        public static void Apply(string cstrikeDir, bool hide, Settings s, out string message)
        {
            string color, translucent;

            if (hide)
            {
                // guarda a cor original do CS (pra restaurar depois)
                string c = ReadCvar(cstrikeDir, "cl_crosshair_color");
                string t = ReadCvar(cstrikeDir, "cl_crosshair_translucent");
                if (!string.IsNullOrEmpty(c) && c.Trim() != "0 0 0")
                    s.OrigCrosshairColor = c.Trim();
                if (!string.IsNullOrEmpty(t) && t.Trim() != "1")
                    s.OrigCrosshairTranslucent = t.Trim();

                color = "0 0 0";
                translucent = "1";
            }
            else
            {
                color = s.OrigCrosshairColor;
                translucent = s.OrigCrosshairTranslucent;
            }

            // o config.cfg termina com "exec userconfig.cfg": esse arquivo e
            // executado por ultimo e garante que os cvars valham.
            WriteBlock(Path.Combine(cstrikeDir, "userconfig.cfg"), color, translucent);
            WriteBlock(Path.Combine(cstrikeDir, "autoexec.cfg"), color, translucent);

            message = hide
                ? "Mira nativa ocultada. Reinicie o CS para aplicar."
                : "Mira nativa restaurada. Reinicie o CS para aplicar.";
        }

        private static void WriteBlock(string path, string color, string translucent)
        {
            List<string> lines = new List<string>();
            if (File.Exists(path))
                lines.AddRange(File.ReadAllLines(path));

            int b = lines.FindIndex(delegate(string x) { return x.Trim() == Begin; });
            int e = lines.FindIndex(delegate(string x) { return x.Trim() == End; });
            if (b >= 0 && e >= b)
                lines.RemoveRange(b, e - b + 1);

            lines.Add(Begin);
            lines.Add("cl_crosshair_color \"" + color + "\"");
            lines.Add("cl_crosshair_translucent \"" + translucent + "\"");
            lines.Add(End);

            File.WriteAllLines(path, lines.ToArray());
        }

        private static string ReadCvar(string cstrikeDir, string name)
        {
            try
            {
                string path = Path.Combine(cstrikeDir, "config.cfg");
                if (!File.Exists(path)) return null;
                foreach (string line in File.ReadAllLines(path))
                {
                    string s = line.Trim();
                    if (s.StartsWith(name + " ", StringComparison.OrdinalIgnoreCase))
                    {
                        int a = s.IndexOf('"');
                        int b = s.LastIndexOf('"');
                        if (a >= 0 && b > a) return s.Substring(a + 1, b - a - 1);
                        return s.Substring(name.Length).Trim();
                    }
                }
            }
            catch { }
            return null;
        }
    }

    internal sealed class PreviewPanel : Panel
    {
        private readonly Settings _s;
        public PreviewPanel(Settings s)
        {
            _s = s;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(32, 34, 38);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int extent = CrosshairRenderer.Extent(_s);
            if (extent < 1) extent = 1;
            float scale = Math.Min((float)(ClientSize.Width - 24) / extent, (float)(ClientSize.Height - 24) / extent);
            if (scale > 6f) scale = 6f;
            CrosshairRenderer.Draw(e.Graphics, ClientSize.Width / 2f, ClientSize.Height / 2f, _s, scale);
        }
    }

    internal sealed class OverlayForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int VK_D = 0x44;
        private const int HOTKEY_ID = 0x4D50;

        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int SWP_FRAMECHANGED = 0x0020;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_SHOWWINDOW = 0x0040;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint GA_ROOT = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public int dwFlags; }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);
        [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        private static readonly Dictionary<IntPtr, int> _origStyle = new Dictionary<IntPtr, int>();
        private static readonly Dictionary<IntPtr, RECT> _origRect = new Dictionary<IntPtr, RECT>();
        private static readonly List<IntPtr> _touched = new List<IntPtr>();

        private readonly Settings _s;
        private readonly Timer _timer;
        private bool _enabled = true;
        private DateTime _lastActive = DateTime.MinValue;
        private bool _scoped = false;
        private int _scopeTick = 0;

        public OverlayForm(Settings s)
        {
            _s = s;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Fuchsia;
            TransparencyKey = Color.Fuchsia;
            Text = "Mira Pontinho";
            Visible = false;

            ApplySettings();

            _timer = new Timer();
            _timer.Interval = 50;
            _timer.Tick += OnTick;
            _timer.Start();

            IntPtr force = Handle; // cria o handle para o hotkey global
        }

        public void Toggle()
        {
            _enabled = !_enabled;
            if (!_enabled) Visible = false;
        }

        public bool IsOn
        {
            get { return _enabled; }
            set { _enabled = value; if (!value) Visible = false; }
        }

        public void ApplySettings()
        {
            int extent = CrosshairRenderer.Extent(_s) + 2 * _s.Outline + 10;
            if (extent < 40) extent = 40;
            Size = new Size(extent, extent);
            Invalidate();
        }

        public void RestoreGameWindow()
        {
            foreach (IntPtr h in _touched)
            {
                try
                {
                    if (_origStyle.ContainsKey(h))
                    {
                        SetWindowLong(h, GWL_STYLE, _origStyle[h]);
                        RECT r = _origRect[h];
                        SetWindowPos(h, IntPtr.Zero, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top,
                            SWP_FRAMECHANGED | SWP_NOZORDER | SWP_NOACTIVATE);
                    }
                }
                catch { }
            }
            _touched.Clear();
            _origStyle.Clear();
            _origRect.Clear();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT  -> clica atraves
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_D);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            RestoreGameWindow();
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
                Toggle();
            base.WndProc(ref m);
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_enabled)
            {
                if (Visible) Visible = false;
                return;
            }

            IntPtr game = FindGameWindow();
            if (game == IntPtr.Zero)
            {
                if (_s.OnlyInGame) { if (Visible) Visible = false; return; }
                CenterOnScreen();
                ShowOverlay();
                return;
            }

            if (_s.Borderless) EnforceBorderless(game);

            if (_s.OnlyInGame)
            {
                if (IsGameForeground(game))
                    _lastActive = DateTime.Now;

                // tolerancia: nao some na hora se perder o foco por um instante
                if ((DateTime.Now - _lastActive).TotalMilliseconds > 900)
                {
                    if (Visible) Visible = false;
                    return;
                }
            }

            // esconde a mira quando o jogador esta com o zoom (AWP/Scout)
            if (_s.HideOnScope)
            {
                if (++_scopeTick >= 2) { _scopeTick = 0; UpdateScopeState(game); }
                if (_scoped) { if (Visible) Visible = false; return; }
            }

            if (_s.ForceScreenCenter)
            {
                CenterOnScreen();
            }
            else
            {
                RECT r;
                GetClientRect(game, out r);
                POINT p = new POINT();
                p.X = 0; p.Y = 0;
                ClientToScreen(game, ref p);
                int cx = p.X + (r.Right - r.Left) / 2;
                int cy = p.Y + (r.Bottom - r.Top) / 2;
                Location = new Point(cx - Width / 2, cy - Height / 2);
            }
            ShowOverlay();
        }

        private void ShowOverlay()
        {
            if (!Visible) Visible = true;
            // reafirma SEMPRE no topo (o jogo as vezes se coloca na frente,
            // por exemplo ao mostrar o nome dos jogadores no spec)
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        // Detecta o zoom (AWP/Scout) olhando a tela: quando o scope esta ativo,
        // a maior parte da tela fica preta (menos o circulo central e o HUD).
        private void UpdateScopeState(IntPtr game)
        {
            RECT r;
            if (!GetClientRect(game, out r)) return;
            int w = r.Right - r.Left;
            int h = r.Bottom - r.Top;
            if (w < 100 || h < 100) return;

            POINT p = new POINT();
            p.X = 0;
            p.Y = 0;
            ClientToScreen(game, ref p);

            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero) return;

            int total = 0, black = 0;
            try
            {
                const int cols = 9, rows = 7;
                for (int iy = 0; iy < rows; iy++)
                {
                    for (int ix = 0; ix < cols; ix++)
                    {
                        int x = p.X + (int)((ix + 0.5) * w / cols);
                        int y = p.Y + (int)((iy + 0.5) * h / rows);
                        uint c = GetPixel(hdc, x, y);
                        if (c == 0xFFFFFFFF) continue;
                        int rr = (int)(c & 0xFF);
                        int gg = (int)((c >> 8) & 0xFF);
                        int bb = (int)((c >> 16) & 0xFF);
                        total++;
                        if (rr <= 20 && gg <= 20 && bb <= 20) black++;
                    }
                }
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }

            if (total == 0) return;
            int pct = black * 100 / total;
            if (_scoped)
            {
                if (pct < _s.ScopeThreshold - 12) _scoped = false;
            }
            else
            {
                if (pct >= _s.ScopeThreshold) _scoped = true;
            }
        }

        private void CenterOnScreen()
        {
            Rectangle b = Screen.PrimaryScreen.Bounds;
            Location = new Point(b.X + b.Width / 2 - Width / 2, b.Y + b.Height / 2 - Height / 2);
        }

        private static bool IsGameForeground(IntPtr game)
        {
            IntPtr fg = GetForegroundWindow();
            if (fg == game) return true;
            if (fg == IntPtr.Zero) return false;
            return GetAncestor(fg, GA_ROOT) == game;
        }

        private static void EnforceBorderless(IntPtr hwnd)
        {
            try
            {
                int style = GetWindowLong(hwnd, GWL_STYLE);
                int want = (style & ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU)) | WS_POPUP;

                IntPtr mon = MonitorFromWindow(hwnd, 2 /* NEAREST */);
                MONITORINFO mi = new MONITORINFO();
                mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (mon == IntPtr.Zero || !GetMonitorInfo(mon, ref mi)) return;
                int w = mi.rcMonitor.Right - mi.rcMonitor.Left;
                int h = mi.rcMonitor.Bottom - mi.rcMonitor.Top;

                if (!_origStyle.ContainsKey(hwnd))
                {
                    _origStyle[hwnd] = style;
                    RECT r;
                    GetWindowRect(hwnd, out r);
                    _origRect[hwnd] = r;
                    _touched.Add(hwnd);
                }

                if (style != want)
                    SetWindowLong(hwnd, GWL_STYLE, want);

                RECT cur;
                GetWindowRect(hwnd, out cur);
                bool sizeOk = (cur.Left == mi.rcMonitor.Left && cur.Top == mi.rcMonitor.Top &&
                               (cur.Right - cur.Left) == w && (cur.Bottom - cur.Top) == h);
                if (style != want || !sizeOk)
                    SetWindowPos(hwnd, IntPtr.Zero, mi.rcMonitor.Left, mi.rcMonitor.Top, w, h,
                        SWP_FRAMECHANGED | SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            catch { }
        }

        private static IntPtr FindGameWindow()
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd) || IsIconic(hWnd)) return true;

                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (pid == 0) return true;

                string name = null;
                try
                {
                    Process p = Process.GetProcessById((int)pid);
                    name = p.ProcessName;
                }
                catch { return true; }

                if (name != null && name.Equals("hl", StringComparison.OrdinalIgnoreCase))
                {
                    RECT r;
                    if (GetClientRect(hWnd, out r) && (r.Right - r.Left) >= 200 && (r.Bottom - r.Top) >= 200)
                    {
                        found = hWnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            CrosshairRenderer.Draw(e.Graphics, ClientSize.Width / 2f, ClientSize.Height / 2f, _s, 1f);
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly Settings _s;
        private readonly OverlayForm _overlay;

        private ComboBox _shape;
        private Panel _swatch;
        private TrackBar _size, _thick, _gap, _outline;
        private Label _sizeV, _thickV, _gapV, _outlineV;
        private CheckBox _onlyGame, _borderless, _hideCs, _hideScope;
        private TrackBar _scopeSens;
        private Label _csStatus, _scopeSensV;
        private PreviewPanel _preview;
        private bool _suppress;

        private static readonly string[] ShapeNames = { "Ponto", "Cruz (+)", "T", "Circulo", "X" };
        private static readonly string[] ShapeIds = { "dot", "cross", "t", "circle", "x" };

        public SettingsForm(Settings s, OverlayForm overlay)
        {
            _s = s;
            _overlay = overlay;

            Text = "Mira Pontinho - Configuracoes";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 530);
            TopMost = true;
            BackColor = Color.FromArgb(245, 246, 248);

            _preview = new PreviewPanel(_s);
            _preview.SetBounds(16, 16, 200, 200);
            Controls.Add(_preview);

            Label shapeL = MakeLabel("Forma da mira", 232, 14);
            _shape = new ComboBox();
            _shape.DropDownStyle = ComboBoxStyle.DropDownList;
            _shape.SetBounds(232, 34, 260, 24);
            _shape.Items.AddRange(ShapeNames);
            _shape.SelectedIndex = IdToIndex(_s.Shape);
            _shape.SelectedIndexChanged += delegate { _s.Shape = ShapeIds[_shape.SelectedIndex]; Changed(); };
            Controls.Add(_shape);

            MakeLabel("Cor", 232, 70);
            Button colorBtn = new Button();
            colorBtn.Text = "Escolher cor...";
            colorBtn.SetBounds(232, 90, 130, 26);
            colorBtn.Click += delegate
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = _s.Color;
                    cd.FullOpen = true;
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        _s.R = cd.Color.R; _s.G = cd.Color.G; _s.B = cd.Color.B;
                        UpdateSwatch();
                        Changed();
                    }
                }
            };
            Controls.Add(colorBtn);

            _swatch = new Panel();
            _swatch.SetBounds(372, 90, 60, 26);
            _swatch.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(_swatch);
            UpdateSwatch();

            _size = MakeSlider("Tamanho", 232, 130, 1, 60, _s.Size, out _sizeV);
            _thick = MakeSlider("Espessura", 232, 190, 1, 20, _s.Thickness, out _thickV);
            _gap = MakeSlider("Vao (espaco no meio)", 232, 250, 0, 40, _s.Gap, out _gapV);
            _outline = MakeSlider("Contorno preto", 232, 310, 0, 6, _s.Outline, out _outlineV);

            _onlyGame = new CheckBox();
            _onlyGame.Text = "Mostrar o ponto so quando o CS estiver aberto";
            _onlyGame.SetBounds(16, 232, 200, 24);
            _onlyGame.Checked = _s.OnlyInGame;
            _onlyGame.CheckedChanged += delegate { _s.OnlyInGame = _onlyGame.Checked; Changed(); };
            Controls.Add(_onlyGame);

            _borderless = new CheckBox();
            _borderless.Text = "Deixar o CS em tela cheia sem borda (pra mira aparecer)";
            _borderless.SetBounds(16, 258, 200, 44);
            _borderless.Checked = _s.Borderless;
            _borderless.CheckedChanged += delegate
            {
                _s.Borderless = _borderless.Checked;
                if (!_borderless.Checked) _overlay.RestoreGameWindow();
                Changed();
            };
            Controls.Add(_borderless);

            _hideCs = new CheckBox();
            _hideCs.Text = "Ocultar a mira nativa do CS";
            _hideCs.SetBounds(16, 302, 180, 22);
            _hideCs.Checked = _s.HideCsCrosshair;
            _hideCs.CheckedChanged += delegate { ToggleHideCs(); };
            Controls.Add(_hideCs);

            Button help = new Button();
            help.Text = "?";
            help.SetBounds(200, 301, 22, 23);
            help.Click += delegate { using (HelpForm h = new HelpForm(_s)) h.ShowDialog(this); };
            Controls.Add(help);

            _hideScope = new CheckBox();
            _hideScope.Text = "Esconder a mira no zoom (AWP/Scout)";
            _hideScope.SetBounds(16, 328, 206, 22);
            _hideScope.Checked = _s.HideOnScope;
            _hideScope.CheckedChanged += delegate { _s.HideOnScope = _hideScope.Checked; Changed(); };
            Controls.Add(_hideScope);

            Label sensL = new Label();
            sensL.Text = "Sensibilidade do zoom";
            sensL.SetBounds(16, 352, 150, 16);
            Controls.Add(sensL);

            _scopeSensV = new Label();
            _scopeSensV.Text = _s.ScopeThreshold + "%";
            _scopeSensV.TextAlign = ContentAlignment.MiddleRight;
            _scopeSensV.SetBounds(170, 352, 52, 16);
            Controls.Add(_scopeSensV);

            _scopeSens = new TrackBar();
            _scopeSens.Minimum = 25;
            _scopeSens.Maximum = 85;
            _scopeSens.TickFrequency = 10;
            _scopeSens.LargeChange = 5;
            _scopeSens.Value = _s.ScopeThreshold;
            _scopeSens.SetBounds(12, 368, 210, 32);
            _scopeSens.ValueChanged += delegate
            {
                _s.ScopeThreshold = _scopeSens.Value;
                _scopeSensV.Text = _scopeSens.Value + "%";
                Changed();
            };
            Controls.Add(_scopeSens);

            _csStatus = new Label();
            _csStatus.SetBounds(16, 406, 206, 40);
            _csStatus.ForeColor = Color.FromArgb(90, 90, 90);
            Controls.Add(_csStatus);

            Button browse = new Button();
            browse.Text = "Procurar pasta do CS...";
            browse.SetBounds(16, 448, 200, 26);
            browse.Click += delegate { BrowseCstrike(); };
            Controls.Add(browse);

            UpdateCsStatus();

            Button reset = new Button();
            reset.Text = "Restaurar padrao";
            reset.SetBounds(232, 420, 120, 30);
            reset.Click += delegate { ResetDefaults(); };
            Controls.Add(reset);

            Button close = new Button();
            close.Text = "Fechar";
            close.SetBounds(372, 420, 120, 30);
            close.Click += delegate { Hide(); };
            Controls.Add(close);

            Label hint = new Label();
            hint.Text = "Atalho: Ctrl + Alt + D  liga/desliga o ponto";
            hint.ForeColor = Color.FromArgb(90, 90, 90);
            hint.SetBounds(232, 462, 280, 20);
            Controls.Add(hint);
        }

        private static int IdToIndex(string id)
        {
            for (int i = 0; i < ShapeIds.Length; i++)
                if (ShapeIds[i] == id) return i;
            return 0;
        }

        private Label MakeLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.SetBounds(x, y, 260, 18);
            Controls.Add(l);
            return l;
        }

        private TrackBar MakeSlider(string caption, int x, int y, int min, int max, int val, out Label valueLabel)
        {
            MakeLabel(caption, x, y);
            Label v = new Label();
            v.Text = val.ToString();
            v.TextAlign = ContentAlignment.MiddleRight;
            v.SetBounds(x + 200, y, 60, 18);
            Controls.Add(v);
            valueLabel = v;

            TrackBar tb = new TrackBar();
            tb.Minimum = min;
            tb.Maximum = max;
            tb.TickFrequency = Math.Max(1, (max - min) / 10);
            tb.LargeChange = 1;
            tb.SmallChange = 1;
            tb.Value = val;
            tb.SetBounds(x - 4, y + 20, 264, 40);
            tb.ValueChanged += delegate
            {
                v.Text = tb.Value.ToString();
                ApplySlider(tb, caption);
                Changed();
            };
            Controls.Add(tb);
            return tb;
        }

        private void ApplySlider(TrackBar tb, string caption)
        {
            if (tb == _size) _s.Size = tb.Value;
            else if (tb == _thick) _s.Thickness = tb.Value;
            else if (tb == _gap) _s.Gap = tb.Value;
            else if (tb == _outline) _s.Outline = tb.Value;
        }

        private void UpdateSwatch()
        {
            _swatch.BackColor = _s.Color;
        }

        private void Changed()
        {
            _s.Normalize();
            _s.Save();
            _overlay.ApplySettings();
            _preview.Invalidate();
        }

        private void ToggleHideCs()
        {
            if (_suppress) return;
            bool hide = _hideCs.Checked;
            string dir = CsConfig.FindCstrike(_s);

            if (dir == null && hide)
            {
                MessageBox.Show(this,
                    "Nao achei a pasta do CS.\nAbra o CS uma vez ou use \"Procurar pasta do CS...\".",
                    "Mira Pontinho", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _suppress = true;
                _hideCs.Checked = false;
                _suppress = false;
                _s.HideCsCrosshair = false;
                _s.Save();
                UpdateCsStatus();
                return;
            }

            _s.HideCsCrosshair = hide;
            if (dir != null)
            {
                _s.CstrikePath = dir;
                string msg;
                CsConfig.Apply(dir, hide, _s, out msg);
                _csStatus.Text = msg;
            }
            else
            {
                UpdateCsStatus();
            }
            _s.Save();
        }

        private void BrowseCstrike()
        {
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                fb.Description = "Escolha a pasta 'cstrike' do Counter-Strike 1.6";
                if (!string.IsNullOrEmpty(_s.CstrikePath) && Directory.Exists(_s.CstrikePath))
                    fb.SelectedPath = _s.CstrikePath;
                if (fb.ShowDialog(this) == DialogResult.OK)
                {
                    _s.CstrikePath = fb.SelectedPath;
                    _s.Save();
                    UpdateCsStatus();
                }
            }
        }

        private void UpdateCsStatus()
        {
            string dir = CsConfig.FindCstrike(_s);
            if (dir == null)
                _csStatus.Text = "Pasta do CS: nao encontrada.\nAbra o CS ou clique em Procurar.";
            else
                _csStatus.Text = "Pasta do CS:\n" + dir;
        }

        private void ResetDefaults()
        {
            Settings d = new Settings();
            _s.Shape = d.Shape; _s.R = d.R; _s.G = d.G; _s.B = d.B;
            _s.Size = d.Size; _s.Thickness = d.Thickness; _s.Gap = d.Gap; _s.Outline = d.Outline;

            _shape.SelectedIndex = IdToIndex(_s.Shape);
            _size.Value = _s.Size; _thick.Value = _s.Thickness; _gap.Value = _s.Gap; _outline.Value = _s.Outline;
            UpdateSwatch();
            Changed();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // fechar a janela apenas esconde (o programa continua na bandeja)
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }
    }

    internal sealed class HelpForm : Form
    {
        private const string Commands = "cl_crosshair_translucent 1\r\ncl_crosshair_color \"0 0 0\"";

        public HelpForm(Settings s)
        {
            Text = "Como ocultar a mira do CS (manual)";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(490, 330);
            TopMost = true;
            BackColor = Color.FromArgb(245, 246, 248);

            Label info = new Label();
            info.SetBounds(16, 12, 458, 150);
            info.Text =
                "Se a opcao automatica nao funcionar, da pra ocultar a mira do CS na mao.\r\n\r\n" +
                "1) Abra o CS e aperte a tecla  ~  pra abrir o console.\r\n" +
                "2) Cole os comandos abaixo e aperte Enter. A mira some na hora.\r\n\r\n" +
                "Pra ficar salvo (sem digitar toda vez): crie ou edite o arquivo\r\n" +
                "\"userconfig.cfg\" dentro da pasta cstrike do CS e cole essas 2 linhas nele.\r\n" +
                "(Ou use o botao \"Copiar comandos\" e cole no arquivo.)";
            Controls.Add(info);

            string dir = CsConfig.FindCstrike(s);
            Label pathLbl = new Label();
            pathLbl.SetBounds(16, 166, 458, 34);
            pathLbl.ForeColor = Color.FromArgb(90, 90, 90);
            pathLbl.Text = dir == null ? "Pasta do CS: nao encontrada." : ("Pasta do CS:\n" + dir);
            Controls.Add(pathLbl);

            TextBox tb = new TextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.WordWrap = false;
            tb.Font = new Font("Consolas", 10f);
            tb.SetBounds(16, 204, 458, 54);
            tb.Text = Commands;
            Controls.Add(tb);

            Button copy = new Button();
            copy.Text = "Copiar comandos";
            copy.SetBounds(16, 274, 150, 30);
            copy.Click += delegate
            {
                try { Clipboard.SetText(Commands); copy.Text = "Copiado!"; }
                catch { }
            };
            Controls.Add(copy);

            Button open = new Button();
            open.Text = "Abrir pasta do CS";
            open.SetBounds(176, 274, 150, 30);
            open.Enabled = dir != null;
            open.Click += delegate
            {
                try { Process.Start("explorer.exe", "\"" + dir + "\""); }
                catch { }
            };
            Controls.Add(open);

            Button close = new Button();
            close.Text = "Fechar";
            close.SetBounds(354, 274, 120, 30);
            close.Click += delegate { Close(); };
            Controls.Add(close);
        }
    }

    internal sealed class TrayContext : ApplicationContext
    {
        private readonly Settings _s;
        private readonly OverlayForm _overlay;
        private readonly NotifyIcon _tray;
        private SettingsForm _form;

        public TrayContext(Settings s, bool openForm)
        {
            _s = s;
            _overlay = new OverlayForm(s);
            _overlay.Show();
            _overlay.Hide();

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Configurar mira...", null, delegate { OpenSettings(); });
            menu.Items.Add("Ligar/desligar (Ctrl+Alt+D)", null, delegate { _overlay.Toggle(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Sair", null, delegate { Quit(); });

            _tray = new NotifyIcon();
            _tray.Icon = SystemIcons.Application;
            _tray.Text = "Mira Pontinho";
            _tray.Visible = true;
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += delegate { OpenSettings(); };

            if (openForm) OpenSettings();
        }

        private void OpenSettings()
        {
            if (_form == null || _form.IsDisposed)
                _form = new SettingsForm(_s, _overlay);
            _form.Show();
            _form.BringToFront();
            _form.Activate();
        }

        private void Quit()
        {
            _overlay.RestoreGameWindow();
            _tray.Visible = false;
            _tray.Dispose();
            _overlay.Close();
            ExitThread();
        }
    }
}
