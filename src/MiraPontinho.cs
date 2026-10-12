using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
            LocApp.Lang = s.Lang;
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
                    MessageBox.Show(LocApp.T("Nao achei a pasta do CS. Abra o CS uma vez e tente de novo."),
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

    // Traducao simples: a chave e o texto em portugues.
    internal static class LocApp
    {
        public static string Lang = "pt";
        public static bool En { get { return Lang == "en"; } }

        public static string T(string pt)
        {
            if (!En || pt == null) return pt;
            string en;
            return _en.TryGetValue(pt, out en) ? en : pt;
        }

        // Traduz os textos de um formulario (botoes, labels, checkboxes e os
        // itens de ComboBox). Nao mexe em listas de dados (nomes de miras).
        public static void Apply(Control root)
        {
            if (!En || root == null) return;
            root.Text = T(root.Text);
            ApplyTo(root);
        }

        private static void ApplyTo(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                c.Text = T(c.Text);
                ComboBox cb = c as ComboBox;
                if (cb != null)
                    for (int i = 0; i < cb.Items.Count; i++)
                    {
                        string item = cb.Items[i] as string;
                        if (item != null) cb.Items[i] = T(item);
                    }
                ApplyTo(c);
            }
        }

        private static readonly Dictionary<string, string> _en =
            new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ---- geral / bandeja ----
            { "Configurar mira...", "Configure crosshair..." },
            { "Miras / compartilhar...", "Crosshairs / share..." },
            { "Ligar/desligar (Ctrl+Alt+D)", "Toggle on/off (Ctrl+Alt+D)" },
            { "Sair", "Exit" },
            { "Mira nativa ocultada. Reinicie o CS para aplicar.", "Native crosshair hidden. Restart CS to apply." },
            { "Mira nativa restaurada. Reinicie o CS para aplicar.", "Native crosshair restored. Restart CS to apply." },
            { "Nao achei a pasta do CS. Abra o CS uma vez e tente de novo.", "Couldn't find the CS folder. Open CS once and try again." },

            // ---- janela de configuracao ----
            { "Mira Pontinho - Configuracoes", "Mira Pontinho - Settings" },
            { "Forma da mira", "Crosshair shape" },
            { "Cor", "Color" },
            { "Escolher cor...", "Choose color..." },
            { "Tamanho", "Size" },
            { "Espessura", "Thickness" },
            { "Vao (espaco no meio)", "Gap (space in the middle)" },
            { "Contorno preto", "Black outline" },
            { "Ponto", "Dot" },
            { "Cruz (+)", "Cross (+)" },
            { "Circulo", "Circle" },
            { "Mostrar o ponto so quando o CS estiver aberto", "Show the crosshair only when CS is open" },
            { "Deixar o CS em tela cheia sem borda (pra mira aparecer)", "Keep CS in borderless fullscreen (so the crosshair shows)" },
            { "Deixar a mira do CS invisivel", "Make the CS crosshair invisible" },
            { "Grava 2 comandos no userconfig.cfg.\nNada e injetado no jogo.", "Writes 2 commands to userconfig.cfg.\nNothing is injected into the game." },
            { "O Mira Pontinho NAO injeta nada no jogo.\n\n" +
              "Ele vai apenas gravar estas 2 linhas no arquivo \"userconfig.cfg\" da pasta do CS:\n\n" +
              "    cl_crosshair_translucent \"1\"\n" +
              "    cl_crosshair_color \"0 0 0\"\n\n" +
              "Pasta:\n{0}\n\n" +
              "Deseja continuar? (da pra desfazer desmarcando a opcao)",
              "Mira Pontinho does NOT inject anything into the game.\n\n" +
              "It will only write these 2 lines to the \"userconfig.cfg\" file in the CS folder:\n\n" +
              "    cl_crosshair_translucent \"1\"\n" +
              "    cl_crosshair_color \"0 0 0\"\n\n" +
              "Folder:\n{0}\n\n" +
              "Continue? (you can undo it by unchecking the option)" },
            { "Esconder a mira no zoom (AWP/Scout)", "Hide the crosshair when scoped (AWP/Scout)" },
            { "Sensibilidade do zoom", "Scope sensitivity" },
            { "Pasta do CS: nao encontrada.\nAbra o CS ou clique em Procurar.", "CS folder: not found.\nOpen CS or click Browse." },
            { "Pasta do CS:\n", "CS folder:\n" },
            { "Procurar pasta do CS...", "Browse CS folder..." },
            { "Miras / compartilhar por codigo...", "Crosshairs / share by code..." },
            { "Restaurar padrao", "Restore defaults" },
            { "Fechar", "Close" },
            { "Atalho: Ctrl + Alt + D  liga/desliga o ponto", "Hotkey: Ctrl + Alt + D  toggles the crosshair" },
            { "Nao achei a pasta do CS.\nAbra o CS uma vez ou use \"Procurar pasta do CS...\".", "Couldn't find the CS folder.\nOpen CS once or use \"Browse CS folder...\"." },
            { "Escolha a pasta 'cstrike' do Counter-Strike 1.6", "Choose the Counter-Strike 1.6 'cstrike' folder" },

            // ---- ajuda manual ----
            { "Como ocultar a mira do CS (manual)", "How to hide the CS crosshair (manual)" },
            { "O Mira Pontinho NAO injeta nada no jogo. Isso aqui e so configuracao (comandos cl_crosshair).", "Mira Pontinho does NOT inject anything into the game. This is only configuration (cl_crosshair commands)." },
            { "Pasta do CS: nao encontrada.", "CS folder: not found." },
            { "Copiar comandos", "Copy commands" },
            { "Copiado!", "Copied!" },
            { "Abrir pasta do CS", "Open CS folder" },
            { "Se a opcao automatica nao funcionar, da pra ocultar a mira do CS na mao.\r\n\r\n" +
              "1) Abra o CS e aperte a tecla  ~  pra abrir o console.\r\n" +
              "2) Cole os comandos abaixo e aperte Enter. A mira some na hora.\r\n\r\n" +
              "Pra ficar salvo (sem digitar toda vez): crie ou edite o arquivo\r\n" +
              "\"userconfig.cfg\" dentro da pasta cstrike do CS e cole essas 2 linhas nele.\r\n" +
              "(Ou use o botao \"Copiar comandos\" e cole no arquivo.)",
              "If the automatic option doesn't work, you can hide the CS crosshair by hand.\r\n\r\n" +
              "1) Open CS and press the  ~  key to open the console.\r\n" +
              "2) Paste the commands below and press Enter. The crosshair disappears right away.\r\n\r\n" +
              "To keep it saved (so you don't type it every time): create or edit the file\r\n" +
              "\"userconfig.cfg\" inside the CS cstrike folder and paste those 2 lines in it.\r\n" +
              "(Or use the \"Copy commands\" button and paste it into the file.)" },

            // ---- janela Miras ----
            { "Miras - salvar e compartilhar", "Crosshairs - save and share" },
            { "Minhas miras", "My crosshairs" },
            { "Aplicar", "Apply" },
            { "Excluir", "Delete" },
            { "Nome da mira:", "Crosshair name:" },
            { "Salvar mira atual", "Save current crosshair" },
            { "Compartilhar mira (codigo)", "Share crosshair (code)" },
            { "Exportar + copiar", "Export + copy" },
            { "Importar codigo", "Import code" },
            { "Copiar codigo", "Copy code" },
            { "Mira \"{0}\" aplicada!", "Crosshair \"{0}\" applied!" },
            { "Mira {0}", "Crosshair {0}" },
            { "Mira salva como \"{0}\".", "Crosshair saved as \"{0}\"." },
            { "Mira \"{0}\" excluida.", "Crosshair \"{0}\" deleted." },
            { "Codigo copiado pra area de transferencia!", "Code copied to clipboard!" },
            { "Codigo gerado (nao consegui copiar).", "Code generated (couldn't copy)." },
            { "Nao tem codigo pra copiar.", "No code to copy." },
            { "Nao consegui copiar.", "Couldn't copy." },
            { "Codigo invalido. Confira e tente de novo.", "Invalid code. Check it and try again." },
            { "Mira importada!", "Crosshair imported!" },
        };
    }

    // Icone do programa (extraido do proprio .exe, que embute o app.ico).
    internal static class AppIcon
    {
        private static Icon _icon;
        public static Icon Value
        {
            get
            {
                if (_icon == null)
                {
                    try { _icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
                    catch { }
                    if (_icon == null) _icon = SystemIcons.Application;
                }
                return _icon;
            }
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
        public bool HideCsCrosshair = false; // oculta a mira nativa do CS (via config)
        public bool HideCsWarned = false;    // ja mostrou o aviso "so config" uma vez
        public bool HideOnScope = true;      // esconde o ponto quando da zoom (AWP/Scout)
        public int ScopeThreshold = 55;      // % de tela preta pra considerar "scoped"
        public string CstrikePath = "";
        public string OrigCrosshairColor = "50 250 50";
        public string OrigCrosshairTranslucent = "0";
        public string Lang = "pt";            // pt ou en
        public List<UserPreset> UserPresets = new List<UserPreset>(); // biblioteca pessoal

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
                        case "hidecswarned": s.HideCsWarned = v == "1"; break;
                        case "hideosscope": s.HideOnScope = v == "1"; break;
                        case "scopethreshold": s.ScopeThreshold = ParseInt(v, s.ScopeThreshold); break;
                        case "cstrikepath": s.CstrikePath = v; break;
                        case "origcolor": s.OrigCrosshairColor = v; break;
                        case "origtranslucent": s.OrigCrosshairTranslucent = v; break;
                        case "lang": s.Lang = v; break;
                        case "userpreset":
                            {
                                int bar = v.IndexOf('|');
                                if (bar > 0)
                                {
                                    CrosshairPreset cp = CrosshairPreset.FromCode(v.Substring(bar + 1));
                                    if (cp != null)
                                        s.UserPresets.Add(new UserPreset(v.Substring(0, bar).Trim(), cp));
                                }
                                break;
                            }
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
            if (Lang != "pt" && Lang != "en") Lang = "pt";
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
                sb.AppendLine("hidecswarned=" + (HideCsWarned ? "1" : "0"));
                sb.AppendLine("hideosscope=" + (HideOnScope ? "1" : "0"));
                sb.AppendLine("scopethreshold=" + ScopeThreshold);
                sb.AppendLine("cstrikepath=" + CstrikePath);
                sb.AppendLine("origcolor=" + OrigCrosshairColor);
                sb.AppendLine("origtranslucent=" + OrigCrosshairTranslucent);
                sb.AppendLine("lang=" + Lang);
                foreach (UserPreset up in UserPresets)
                    sb.AppendLine("userpreset=" + SanitizeName(up.Name) + "|" + up.Preset.ToCode());
                File.WriteAllText(IniPath(), sb.ToString());
            }
            catch { }
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Mira";
            return name.Replace("|", " ").Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }

    // Uma mira "portatil": so os dados visuais (o que da pra compartilhar).
    internal sealed class CrosshairPreset
    {
        public string Shape = "dot";
        public int R, G, B;
        public int Size = 6, Thickness = 1, Gap = 3, Outline = 1;

        public static CrosshairPreset FromSettings(Settings s)
        {
            CrosshairPreset p = new CrosshairPreset();
            p.Shape = s.Shape; p.R = s.R; p.G = s.G; p.B = s.B;
            p.Size = s.Size; p.Thickness = s.Thickness; p.Gap = s.Gap; p.Outline = s.Outline;
            return p;
        }

        public void ApplyTo(Settings s)
        {
            s.Shape = Shape; s.R = R; s.G = G; s.B = B;
            s.Size = Size; s.Thickness = Thickness; s.Gap = Gap; s.Outline = Outline;
            s.Normalize();
        }

        // codigo de compartilhamento no estilo "share code" do CS:GO
        // ex.: MIRA1-Y3Jvc3MsMTEsMjU1LDAsNCwyLDEsMA
        public string ToCode()
        {
            string payload = string.Join(",",
                new string[] { Shape, R.ToString(), G.ToString(), B.ToString(),
                               Size.ToString(), Thickness.ToString(), Gap.ToString(), Outline.ToString() });
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return "MIRA1." + b64;
        }

        public static CrosshairPreset FromCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            code = code.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\t", "");
            if (code.StartsWith("MIRA1.", StringComparison.OrdinalIgnoreCase))
                code = code.Substring(6);
            code = code.Replace('-', '+').Replace('_', '/');
            while (code.Length % 4 != 0) code += "=";

            string payload;
            try { payload = Encoding.UTF8.GetString(Convert.FromBase64String(code)); }
            catch { return null; }

            string[] f = payload.Split(',');
            if (f.Length != 8) return null;

            try
            {
                CrosshairPreset p = new CrosshairPreset();
                p.Shape = f[0].Trim().ToLowerInvariant();
                p.R = int.Parse(f[1]); p.G = int.Parse(f[2]); p.B = int.Parse(f[3]);
                p.Size = int.Parse(f[4]); p.Thickness = int.Parse(f[5]);
                p.Gap = int.Parse(f[6]); p.Outline = int.Parse(f[7]);

                if (p.Shape != "dot" && p.Shape != "cross" && p.Shape != "t" && p.Shape != "circle" && p.Shape != "x")
                    return null;
                if (p.R < 0 || p.R > 255 || p.G < 0 || p.G > 255 || p.B < 0 || p.B > 255) return null;
                if (p.Size < 1 || p.Size > 60 || p.Thickness < 1 || p.Thickness > 20) return null;
                if (p.Gap < 0 || p.Gap > 40 || p.Outline < 0 || p.Outline > 6) return null;
                return p;
            }
            catch { return null; }
        }
    }

    internal sealed class UserPreset
    {
        public string Name;
        public CrosshairPreset Preset;
        public UserPreset(string name, CrosshairPreset preset) { Name = name; Preset = preset; }
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

            // centraliza no PIXEL exato (evita traco torto por offset de 0.5px)
            int icx = (int)Math.Round(cx);
            int icy = (int)Math.Round(cy);
            int size = Math.Max(1, (int)Math.Round(s.Size * scale));
            int thick = Math.Max(1, (int)Math.Round(s.Thickness * scale));
            int gap = (int)Math.Round(s.Gap * scale);
            int outline = (int)Math.Round(s.Outline * scale);

            if (s.Shape == "dot")
            {
                // O GDI+ FillEllipse rasteriza errado em tamanhos pequenos: em
                // vez de um ponto redondo centralizado, ele desenha um quadrado
                // deslocado (um pixel "vazando" para um lado). Entao desenhamos
                // o ponto pixel a pixel, sempre centrado no pixel do meio.
                double pcx = icx - (size - 1) / 2 + size / 2.0;
                double pcy = icy - (size - 1) / 2 + size / 2.0;
                if (outline > 0)
                    FillCircle(g, pcx, pcy, size / 2.0 + outline, Color.Black);
                FillCircle(g, pcx, pcy, size / 2.0, s.Color);
                return;
            }

            if (s.Shape == "circle")
            {
                int x = icx - (size - 1) / 2;
                int y = icy - (size - 1) / 2;
                if (outline > 0)
                    using (Pen p = new Pen(Color.Black, thick + 2 * outline))
                        g.DrawEllipse(p, x - outline, y - outline, size + 2 * outline, size + 2 * outline);
                using (Pen p = new Pen(s.Color, thick))
                    g.DrawEllipse(p, x, y, size, size);
                return;
            }

            if (s.Shape == "x")
            {
                int a = gap + size;
                DrawLine(g, icx - a, icy - a, icx + a, icy + a, thick, outline, s.Color);
                DrawLine(g, icx - a, icy + a, icx + a, icy - a, thick, outline, s.Color);
                return;
            }

            // Cruz / T: os bracos ficam simetricos em relacao ao pixel central.
            // (antes o vao de cima/do lado tinha 1 pixel a mais que o de baixo,
            // deixando a cruz desalinhada em meio pixel)
            bool up = s.Shape == "cross";
            int h = (thick - 1) / 2;
            int v0 = icx - h;                  // primeira coluna do traco vertical
            int u0 = icy - h;                  // primeira linha do traco horizontal
            int v1 = v0 + thick - 1;           // ultima coluna do traco vertical
            int u1 = u0 + thick - 1;           // ultima linha do traco horizontal

            if (up) FillArm(g, v0, u0 - gap - size, thick, size, outline, s.Color); // cima
            FillArm(g, v0, u1 + 1 + gap, thick, size, outline, s.Color);            // baixo
            FillArm(g, v0 - gap - size, u0, size, thick, outline, s.Color);         // esquerda
            FillArm(g, v1 + 1 + gap, u0, size, thick, outline, s.Color);            // direita
            if (gap == 0) FillArm(g, v0, u0, thick, thick, outline, s.Color);       // sem vao: fecha o meio
        }

        // Ponto redondo desenhado pixel a pixel: garante simetria perfeita e o
        // pixel central certo (o GDI+ FillEllipse erra em tamanhos pequenos).
        private static void FillCircle(Graphics g, double cx, double cy, double radius, Color color)
        {
            if (radius <= 0) return;
            int x0 = (int)Math.Floor(cx - radius);
            int x1 = (int)Math.Ceiling(cx + radius);
            int y0 = (int)Math.Floor(cy - radius);
            int y1 = (int)Math.Ceiling(cy + radius);
            double r2 = radius * radius;
            using (SolidBrush b = new SolidBrush(color))
            {
                for (int y = y0; y < y1; y++)
                {
                    double dy = (y + 0.5) - cy;
                    double inner = r2 - dy * dy;
                    if (inner < 0) continue;
                    double half = Math.Sqrt(inner);
                    int xa = (int)Math.Ceiling(cx - half - 0.5);
                    int xb = (int)Math.Floor(cx + half - 0.5);
                    if (xb >= xa) g.FillRectangle(b, xa, y, xb - xa + 1, 1);
                }
            }
        }

        private static void FillArm(Graphics g, int x, int y, int w, int h, int outline, Color color)
        {
            if (outline > 0)
                using (SolidBrush b = new SolidBrush(Color.Black))
                    g.FillRectangle(b, x - outline, y - outline, w + 2 * outline, h + 2 * outline);
            using (SolidBrush b = new SolidBrush(color))
                g.FillRectangle(b, x, y, w, h);
        }

        private static void DrawLine(Graphics g, int x0, int y0, int x1, int y1, int thick, int outline, Color color)
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

            // So o userconfig.cfg: o config.cfg termina com "exec userconfig.cfg",
            // entao ele e executado por ultimo e garante os cvars. NAO mexemos no
            // autoexec.cfg (menos invasivo). Se uma versao antiga deixou o bloco
            // la, a gente remove.
            WriteBlock(Path.Combine(cstrikeDir, "userconfig.cfg"), color, translucent);
            RemoveBlock(Path.Combine(cstrikeDir, "autoexec.cfg"));

            message = hide
                ? LocApp.T("Mira nativa ocultada. Reinicie o CS para aplicar.")
                : LocApp.T("Mira nativa restaurada. Reinicie o CS para aplicar.");
        }

        private static void RemoveBlock(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                List<string> lines = new List<string>();
                lines.AddRange(File.ReadAllLines(path));

                int b = lines.FindIndex(delegate(string x) { return x.Trim() == Begin; });
                int e = lines.FindIndex(delegate(string x) { return x.Trim() == End; });
                if (b >= 0 && e >= b)
                {
                    lines.RemoveRange(b, e - b + 1);
                    File.WriteAllLines(path, lines.ToArray());
                }
            }
            catch { }
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
            CrosshairRenderer.Draw(e.Graphics, (ClientSize.Width - 1) / 2f, (ClientSize.Height - 1) / 2f, _s, scale);
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
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);

        private static readonly Dictionary<IntPtr, int> _origStyle = new Dictionary<IntPtr, int>();
        private static readonly Dictionary<IntPtr, RECT> _origRect = new Dictionary<IntPtr, RECT>();
        private static readonly List<IntPtr> _touched = new List<IntPtr>();

        private readonly Settings _s;
        private readonly Timer _timer;
        private bool _enabled = true;
        private DateTime _lastActive = DateTime.MinValue;
        private bool _scoped = false;
        private Bitmap _scopeBmp;
        private DateTime _scopeFastUntil = DateTime.MinValue;
        private DateTime _lastScopeCheck = DateTime.MinValue;
        private bool _rPrev = false;
        private DateTime _lastRightClick = DateTime.MinValue;
        private IntPtr _gameHwnd = IntPtr.Zero;
        private DateTime _nextGameSearch = DateTime.MinValue;

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
            Icon = AppIcon.Value;
            Visible = false;

            ApplySettings();

            _timer = new Timer();
            _timer.Interval = 40;
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
            if (extent < 41) extent = 41;
            if ((extent & 1) == 0) extent++; // impar => o centro cai num pixel exato
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
                // quando o botao direito e clicado (tecla do scope), checa rapido
                // por um tempo; no resto, checa devagar pra economizar CPU
                bool rdown = (GetAsyncKeyState(0x02) & 0x8000) != 0;
                if (rdown && !_rPrev)
                {
                    _scopeFastUntil = DateTime.Now.AddMilliseconds(700);
                    _lastRightClick = DateTime.Now;
                }
                _rPrev = rdown;

                double interval = (DateTime.Now < _scopeFastUntil) ? 35 : 200;
                if ((DateTime.Now - _lastScopeCheck).TotalMilliseconds >= interval)
                {
                    _lastScopeCheck = DateTime.Now;
                    UpdateScopeState(game);
                }
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
                int half = (Width - 1) / 2; // o pixel central do overlay fica no centro do CS
                Location = new Point(cx - half, cy - half);
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

        // Detecta o zoom (AWP/Scout) olhando a tela. O scope do CS e preto PURO
        // (0,0,0) nas laterais e mostra a visao no circulo central. Entao so
        // consideramos "scoped" se as laterais forem preto puro E o centro
        // tiver conteudo (o buraco do scope). Cena escura nao passa nisso.
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

            int stripH = h * 12 / 100;
            if (stripH < 8) stripH = 8;
            int sy = p.Y + h * 1 / 100;

            try
            {
                if (_scopeBmp == null || _scopeBmp.Width != w || _scopeBmp.Height != stripH)
                {
                    if (_scopeBmp != null) _scopeBmp.Dispose();
                    _scopeBmp = new Bitmap(w, stripH, PixelFormat.Format32bppArgb);
                }

                using (Graphics g = Graphics.FromImage(_scopeBmp))
                    g.CopyFromScreen(p.X, sy, 0, 0, new Size(w, stripH), CopyPixelOperation.SourceCopy);

                BitmapData d = _scopeBmp.LockBits(new Rectangle(0, 0, w, stripH), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int sideTotal = 0, sideBlack = 0;
                try
                {
                    for (int yy = 0; yy < stripH; yy += 3)
                    {
                        for (int xx = 0; xx < w; xx += 6)
                        {
                            int v = Marshal.ReadInt32(d.Scan0, yy * d.Stride + xx * 4);
                            int rr = v & 0xFF;
                            int gg = (v >> 8) & 0xFF;
                            int bb = (v >> 16) & 0xFF;
                            bool pureBlack = rr <= 10 && gg <= 10 && bb <= 10;
                            double fx = (double)xx / w;

                            // so as laterais (onde o scope e preto puro, sem o circulo)
                            if (fx < 0.30 || fx > 0.70)
                            {
                                sideTotal++;
                                if (pureBlack) sideBlack++;
                            }
                        }
                    }
                }
                finally
                {
                    _scopeBmp.UnlockBits(d);
                }

                if (sideTotal == 0) return;
                int sidePct = sideBlack * 100 / sideTotal;
                bool screenScope = sidePct >= _s.ScopeThreshold;

                if (!_scoped)
                {
                    // so entra em "scoped" se um clique direito (o gatilho do zoom)
                    // aconteceu recentemente -> evita falso positivo em cena escura
                    if (screenScope && (DateTime.Now - _lastRightClick).TotalMilliseconds < 1500)
                        _scoped = true;
                }
                else
                {
                    if (!screenScope)
                        _scoped = false;
                }
            }
            catch { }
        }

        private void CenterOnScreen()
        {
            Rectangle b = Screen.PrimaryScreen.Bounds;
            int half = (Width - 1) / 2;
            Location = new Point(b.X + b.Width / 2 - half, b.Y + b.Height / 2 - half);
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

        private IntPtr FindGameWindow()
        {
            // reutiliza o handle em cache enquanto ainda for valido (barato)
            if (_gameHwnd != IntPtr.Zero && IsWindow(_gameHwnd) && IsWindowVisible(_gameHwnd) && !IsIconic(_gameHwnd))
                return _gameHwnd;

            // so procura de novo a cada 1,5s (evita enumerar tudo a cada tick)
            if (DateTime.Now < _nextGameSearch)
                return IntPtr.Zero;
            _nextGameSearch = DateTime.Now.AddMilliseconds(1500);

            HashSet<uint> pids = new HashSet<uint>();
            try
            {
                foreach (Process p in Process.GetProcessesByName("hl"))
                    pids.Add((uint)p.Id);
            }
            catch { }

            IntPtr found = IntPtr.Zero;
            if (pids.Count > 0)
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (!IsWindowVisible(hWnd) || IsIconic(hWnd)) return true;

                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    if (!pids.Contains(pid)) return true;

                    RECT r;
                    if (GetClientRect(hWnd, out r) && (r.Right - r.Left) >= 200 && (r.Bottom - r.Top) >= 200)
                    {
                        found = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }

            _gameHwnd = found;
            return found;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            CrosshairRenderer.Draw(e.Graphics, (ClientSize.Width - 1) / 2f, (ClientSize.Height - 1) / 2f, _s, 1f);
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly Settings _s;
        private readonly OverlayForm _overlay;
        private readonly Action _onReopen;

        private ComboBox _shape;
        private ComboBox _lang;
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

        public SettingsForm(Settings s, OverlayForm overlay, Action onReopen)
        {
            _s = s;
            _overlay = overlay;
            _onReopen = onReopen;

            Text = "Mira Pontinho - Configuracoes";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 560);
            TopMost = true;
            BackColor = Color.FromArgb(245, 246, 248);
            Icon = AppIcon.Value;

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
            _hideCs.Text = "Deixar a mira do CS invisivel";
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

            Label hideCsNote = new Label();
            hideCsNote.AutoSize = false;
            hideCsNote.Font = new Font(Font.FontFamily, 8f);
            hideCsNote.Text = "Grava 2 comandos no userconfig.cfg.\nNada e injetado no jogo.";
            hideCsNote.ForeColor = Color.FromArgb(120, 120, 120);
            hideCsNote.SetBounds(16, 486, 210, 30);
            Controls.Add(hideCsNote);

            UpdateCsStatus();

            Button reset = new Button();
            reset.Text = "Restaurar padrao";
            reset.SetBounds(232, 420, 120, 30);
            reset.Click += delegate { ResetDefaults(); };
            Controls.Add(reset);

            Button presets = new Button();
            presets.Text = "Miras / compartilhar por codigo...";
            presets.SetBounds(232, 380, 260, 30);
            presets.Click += delegate { OpenPresets(); };
            Controls.Add(presets);

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

            Label langLbl = new Label();
            langLbl.Text = "Idioma / Language";
            langLbl.SetBounds(232, 486, 130, 18);
            Controls.Add(langLbl);

            _lang = new ComboBox();
            _lang.DropDownStyle = ComboBoxStyle.DropDownList;
            _lang.Items.AddRange(new object[] { "Portugues", "English" });
            _lang.SelectedIndex = _s.Lang == "en" ? 1 : 0;
            _lang.SetBounds(232, 504, 130, 24);
            _lang.SelectedIndexChanged += delegate
            {
                if (_suppress) return;
                string want = _lang.SelectedIndex == 1 ? "en" : "pt";
                if (want == _s.Lang) return;
                _s.Lang = want;
                _s.Save();
                LocApp.Lang = want;
                if (_onReopen != null) BeginInvoke((Action)delegate { _onReopen(); });
            };
            Controls.Add(_lang);

            // traduz tudo pro idioma escolhido (a chave e o texto em PT)
            LocApp.Apply(this);
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
                    LocApp.T("Nao achei a pasta do CS.\nAbra o CS uma vez ou use \"Procurar pasta do CS...\"."),
                    "Mira Pontinho", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _suppress = true;
                _hideCs.Checked = false;
                _suppress = false;
                _s.HideCsCrosshair = false;
                _s.Save();
                UpdateCsStatus();
                return;
            }

            // Aviso (na primeira vez) deixando claro que NAO injetamos nada:
            // so gravamos 2 comandos de configuracao no userconfig.cfg.
            if (hide && !_s.HideCsWarned)
            {
                string txt = string.Format(LocApp.T(
                    "O Mira Pontinho NAO injeta nada no jogo.\n\n" +
                    "Ele vai apenas gravar estas 2 linhas no arquivo \"userconfig.cfg\" da pasta do CS:\n\n" +
                    "    cl_crosshair_translucent \"1\"\n" +
                    "    cl_crosshair_color \"0 0 0\"\n\n" +
                    "Pasta:\n{0}\n\n" +
                    "Deseja continuar? (da pra desfazer desmarcando a opcao)"), dir);

                if (MessageBox.Show(this, txt, "Mira Pontinho",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK)
                {
                    _suppress = true;
                    _hideCs.Checked = false;
                    _suppress = false;
                    _s.HideCsCrosshair = false;
                    _s.Save();
                    UpdateCsStatus();
                    return;
                }
                _s.HideCsWarned = true;
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
                fb.Description = LocApp.T("Escolha a pasta 'cstrike' do Counter-Strike 1.6");
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
                _csStatus.Text = LocApp.T("Pasta do CS: nao encontrada.\nAbra o CS ou clique em Procurar.");
            else
                _csStatus.Text = LocApp.T("Pasta do CS:\n") + dir;
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

        public void OpenPresets()
        {
            using (PresetsForm pf = new PresetsForm(_s, this))
                pf.ShowDialog(this);
        }

        // Sincroniza os controles com o objeto de configuracao (usado quando uma
        // mira pronta/importada e aplicada na janela "Miras").
        public void ReloadFromSettings()
        {
            _shape.SelectedIndex = IdToIndex(_s.Shape);
            _size.Value = _s.Size;
            _thick.Value = _s.Thickness;
            _gap.Value = _s.Gap;
            _outline.Value = _s.Outline;
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

    // Janela "Miras": biblioteca pessoal e compartilhar por codigo.
    internal sealed class PresetsForm : Form
    {
        private readonly Settings _s;
        private readonly SettingsForm _owner;
        private readonly Settings _previewSettings = new Settings();

        private readonly ListBox _mineList = new ListBox();
        private readonly TextBox _codeBox = new TextBox();
        private readonly TextBox _nameBox = new TextBox();
        private readonly Label _status = new Label();
        private readonly PreviewPanel _preview;

        public PresetsForm(Settings s, SettingsForm owner)
        {
            _s = s;
            _owner = owner;

            Text = LocApp.T("Miras - salvar e compartilhar");
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(600, 424);
            TopMost = true;
            BackColor = Color.FromArgb(245, 246, 248);
            Icon = AppIcon.Value;

            // ---------- Minhas miras ----------
            GroupBox gbMine = new GroupBox();
            gbMine.Text = LocApp.T("Minhas miras");
            gbMine.SetBounds(10, 10, 580, 190);
            Controls.Add(gbMine);

            _mineList.SetBounds(14, 24, 270, 152);
            _mineList.IntegralHeight = false;
            _mineList.SelectedIndexChanged += delegate { PreviewMine(); };
            _mineList.DoubleClick += delegate { ApplyMine(); };
            gbMine.Controls.Add(_mineList);

            Button applyMine = new Button();
            applyMine.Text = LocApp.T("Aplicar");
            applyMine.SetBounds(296, 26, 130, 28);
            applyMine.Click += delegate { ApplyMine(); };
            gbMine.Controls.Add(applyMine);

            Button delMine = new Button();
            delMine.Text = LocApp.T("Excluir");
            delMine.SetBounds(296, 60, 130, 28);
            delMine.Click += delegate { DeleteMine(); };
            gbMine.Controls.Add(delMine);

            Label nameLbl = new Label();
            nameLbl.Text = LocApp.T("Nome da mira:");
            nameLbl.SetBounds(296, 94, 130, 16);
            gbMine.Controls.Add(nameLbl);

            _nameBox.SetBounds(296, 112, 130, 24);
            gbMine.Controls.Add(_nameBox);

            Button saveMine = new Button();
            saveMine.Text = LocApp.T("Salvar mira atual");
            saveMine.SetBounds(296, 140, 130, 24);
            saveMine.Click += delegate { SaveMine(); };
            gbMine.Controls.Add(saveMine);

            _preview = new PreviewPanel(_previewSettings);
            _preview.SetBounds(434, 24, 134, 152);
            gbMine.Controls.Add(_preview);

            // ---------- Compartilhar ----------
            GroupBox gbShare = new GroupBox();
            gbShare.Text = LocApp.T("Compartilhar mira (codigo)");
            gbShare.SetBounds(10, 206, 580, 152);
            Controls.Add(gbShare);

            _codeBox.Multiline = true;
            _codeBox.WordWrap = true;
            _codeBox.Font = new Font("Consolas", 9f);
            _codeBox.SetBounds(14, 26, 420, 102);
            gbShare.Controls.Add(_codeBox);

            Button exp = new Button();
            exp.Text = LocApp.T("Exportar + copiar");
            exp.SetBounds(446, 26, 124, 30);
            exp.Click += delegate { ExportCode(); };
            gbShare.Controls.Add(exp);

            Button imp = new Button();
            imp.Text = LocApp.T("Importar codigo");
            imp.SetBounds(446, 62, 124, 30);
            imp.Click += delegate { ImportCode(); };
            gbShare.Controls.Add(imp);

            Button cp = new Button();
            cp.Text = LocApp.T("Copiar codigo");
            cp.SetBounds(446, 98, 124, 30);
            cp.Click += delegate { CopyCode(); };
            gbShare.Controls.Add(cp);

            // ---------- Rodape ----------
            _status.SetBounds(10, 364, 480, 18);
            _status.ForeColor = Color.FromArgb(70, 70, 70);
            Controls.Add(_status);

            Button close = new Button();
            close.Text = LocApp.T("Fechar");
            close.SetBounds(488, 382, 102, 26);
            close.Click += delegate { Close(); };
            Controls.Add(close);

            RefreshMineList();
            Preview(_s); // comeca mostrando a mira atual
        }

        private void ApplyMine()
        {
            int i = _mineList.SelectedIndex;
            if (i < 0 || i >= _s.UserPresets.Count) return;
            _s.UserPresets[i].Preset.ApplyTo(_s);
            _owner.ReloadFromSettings();
            _status.Text = string.Format(LocApp.T("Mira \"{0}\" aplicada!"), _s.UserPresets[i].Name);
        }

        private void PreviewMine()
        {
            int i = _mineList.SelectedIndex;
            if (i < 0 || i >= _s.UserPresets.Count) { Preview(_s); return; }
            Preview(_s.UserPresets[i].Preset);
        }

        private void Preview(CrosshairPreset p)
        {
            p.ApplyTo(_previewSettings);
            _previewSettings.Normalize();
            _preview.Invalidate();
        }

        private void Preview(Settings s)
        {
            Preview(CrosshairPreset.FromSettings(s));
        }

        private void SaveMine()
        {
            string name = _nameBox.Text.Trim();
            if (name.Length == 0) name = string.Format(LocApp.T("Mira {0}"), _s.UserPresets.Count + 1);

            CrosshairPreset p = CrosshairPreset.FromSettings(_s);
            int existing = -1;
            for (int i = 0; i < _s.UserPresets.Count; i++)
                if (string.Equals(_s.UserPresets[i].Name, name, StringComparison.OrdinalIgnoreCase))
                { existing = i; break; }

            if (existing >= 0) _s.UserPresets[existing].Preset = p;
            else _s.UserPresets.Add(new UserPreset(name, p));

            _s.Save();
            RefreshMineList();
            _status.Text = string.Format(LocApp.T("Mira salva como \"{0}\"."), name);
        }

        private void DeleteMine()
        {
            int i = _mineList.SelectedIndex;
            if (i < 0 || i >= _s.UserPresets.Count) return;
            string name = _s.UserPresets[i].Name;
            _s.UserPresets.RemoveAt(i);
            _s.Save();
            RefreshMineList();
            _status.Text = string.Format(LocApp.T("Mira \"{0}\" excluida."), name);
        }

        private void RefreshMineList()
        {
            int sel = _mineList.SelectedIndex;
            _mineList.Items.Clear();
            foreach (UserPreset up in _s.UserPresets) _mineList.Items.Add(up.Name);
            if (_mineList.Items.Count > 0)
                _mineList.SelectedIndex = Math.Min(Math.Max(sel, 0), _mineList.Items.Count - 1);
            else
                Preview(_s);
        }

        private void ExportCode()
        {
            string code = CrosshairPreset.FromSettings(_s).ToCode();
            _codeBox.Text = code;
            try { Clipboard.SetText(code); _status.Text = LocApp.T("Codigo copiado pra area de transferencia!"); }
            catch { _status.Text = LocApp.T("Codigo gerado (nao consegui copiar)."); }
        }

        private void CopyCode()
        {
            string code = _codeBox.Text.Trim();
            if (code.Length == 0) { _status.Text = LocApp.T("Nao tem codigo pra copiar."); return; }
            try { Clipboard.SetText(code); _status.Text = LocApp.T("Codigo copiado!"); }
            catch { _status.Text = LocApp.T("Nao consegui copiar."); }
        }

        private void ImportCode()
        {
            CrosshairPreset p = CrosshairPreset.FromCode(_codeBox.Text);
            if (p == null) { _status.Text = LocApp.T("Codigo invalido. Confira e tente de novo."); return; }
            p.ApplyTo(_s);
            _s.Save();
            _owner.ReloadFromSettings();
            Preview(p);
            _status.Text = LocApp.T("Mira importada!");
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
            ClientSize = new Size(490, 366);
            TopMost = true;
            BackColor = Color.FromArgb(245, 246, 248);
            Icon = AppIcon.Value;

            Label note = new Label();
            note.SetBounds(16, 10, 458, 30);
            note.ForeColor = Color.FromArgb(70, 70, 70);
            note.Text = "O Mira Pontinho NAO injeta nada no jogo. Isso aqui e so configuracao (comandos cl_crosshair).";
            Controls.Add(note);

            Label info = new Label();
            info.SetBounds(16, 46, 458, 150);
            info.Text = LocApp.T(
                "Se a opcao automatica nao funcionar, da pra ocultar a mira do CS na mao.\r\n\r\n" +
                "1) Abra o CS e aperte a tecla  ~  pra abrir o console.\r\n" +
                "2) Cole os comandos abaixo e aperte Enter. A mira some na hora.\r\n\r\n" +
                "Pra ficar salvo (sem digitar toda vez): crie ou edite o arquivo\r\n" +
                "\"userconfig.cfg\" dentro da pasta cstrike do CS e cole essas 2 linhas nele.\r\n" +
                "(Ou use o botao \"Copiar comandos\" e cole no arquivo.)");
            Controls.Add(info);

            string dir = CsConfig.FindCstrike(s);
            Label pathLbl = new Label();
            pathLbl.SetBounds(16, 200, 458, 34);
            pathLbl.ForeColor = Color.FromArgb(90, 90, 90);
            pathLbl.Text = dir == null ? LocApp.T("Pasta do CS: nao encontrada.") : (LocApp.T("Pasta do CS:\n") + dir);
            Controls.Add(pathLbl);

            TextBox tb = new TextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.WordWrap = false;
            tb.Font = new Font("Consolas", 10f);
            tb.SetBounds(16, 238, 458, 54);
            tb.Text = Commands;
            Controls.Add(tb);

            Button copy = new Button();
            copy.Text = "Copiar comandos";
            copy.SetBounds(16, 308, 150, 30);
            copy.Click += delegate
            {
                try { Clipboard.SetText(Commands); copy.Text = LocApp.T("Copiado!"); }
                catch { }
            };
            Controls.Add(copy);

            Button open = new Button();
            open.Text = "Abrir pasta do CS";
            open.SetBounds(176, 308, 150, 30);
            open.Enabled = dir != null;
            open.Click += delegate
            {
                try { Process.Start("explorer.exe", "\"" + dir + "\""); }
                catch { }
            };
            Controls.Add(open);

            Button close = new Button();
            close.Text = "Fechar";
            close.SetBounds(354, 308, 120, 30);
            close.Click += delegate { Close(); };
            Controls.Add(close);

            LocApp.Apply(this);
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

            _tray = new NotifyIcon();
            _tray.Icon = AppIcon.Value;
            _tray.Text = "Mira Pontinho";
            _tray.Visible = true;
            _tray.DoubleClick += delegate { OpenSettings(); };

            BuildMenu();

            if (openForm) OpenSettings();
        }

        private void BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(LocApp.T("Configurar mira..."), null, delegate { OpenSettings(); });
            menu.Items.Add(LocApp.T("Miras / compartilhar..."), null, delegate { OpenSettings(); if (_form != null) _form.OpenPresets(); });
            menu.Items.Add(LocApp.T("Ligar/desligar (Ctrl+Alt+D)"), null, delegate { _overlay.Toggle(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(LocApp.T("Sair"), null, delegate { Quit(); });
            _tray.ContextMenuStrip = menu;
        }

        private void OpenSettings()
        {
            if (_form == null || _form.IsDisposed)
                _form = new SettingsForm(_s, _overlay, ReopenSettings);
            _form.Show();
            _form.BringToFront();
            _form.Activate();
        }

        // Recria a janela (usado quando troca o idioma) pra aplicar a traducao.
        private void ReopenSettings()
        {
            SettingsForm old = _form;
            _form = null;
            if (old != null && !old.IsDisposed) old.Dispose();
            BuildMenu();
            OpenSettings();
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
