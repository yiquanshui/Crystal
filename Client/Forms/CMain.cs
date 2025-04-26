using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Security;
using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirScenes;
using Client.MirSounds;
using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Windows;
using Font = System.Drawing.Font;

namespace Client
{
    public partial class CMain : RenderForm
    {
        public static MirControl? DebugBaseLabel { get; private set; }
        public static MirControl? HintBaseLabel { get; private set; }
        private static MirLabel? DebugTextLabel { get; set; }
        private static MirLabel? HintTextLabel { get; set; }
        // public static MirControl? ScreenshotTextLabel { get; private set; }
        public static Graphics Graphics { get; private set; } = null!;
        public static Point MPoint { get; private set;}

        private static Stopwatch Timer { get;} = Stopwatch.StartNew();
        private static DateTime StartTime { get; } = DateTime.UtcNow;
        public static long Time { get; private set; }
        public static DateTime Now => StartTime.AddMilliseconds(Time);
        public static readonly Random Random = new Random();

        public static string DebugText { get; set; } = "";

        private static long _fpsTime;
        private static int _fps;
        private static long _cleanTime;
        private static long _drawTime;
        private int FPS;
        public static int DPS;
        public static int DPSCounter;

        public static long PingTime;
        public static long NextPing = 10000;

        public static bool Shift, Alt, Ctrl, Tilde, SpellTargetLock;
        public static double BytesSent, BytesReceived;

        public static readonly KeyBindSettings InputKeys = new KeyBindSettings();

        public CMain()
        {
            InitializeComponent();

            Application.Idle += Application_Idle;

            MouseClick += CMain_MouseClick;
            MouseDown += CMain_MouseDown;
            MouseUp += CMain_MouseUp;
            MouseMove += CMain_MouseMove;
            MouseDoubleClick += CMain_MouseDoubleClick;
            KeyPress += CMain_KeyPress;
            KeyDown += CMain_KeyDown;
            KeyUp += CMain_KeyUp;
            Deactivate += CMain_Deactivate;
            MouseWheel += CMain_MouseWheel;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            FormBorderStyle = Settings.FullScreen || Settings.Borderless ? FormBorderStyle.None : FormBorderStyle.FixedDialog;

            Graphics = CreateGraphics();
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            Graphics.CompositingQuality = CompositingQuality.HighQuality;
            Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Graphics.TextContrast = 0;
        }

        
        private void CMain_Load(object sender, EventArgs e) {
            Text = GameLanguage.GameName;
            try {
                ClientSize = new Size(Settings.ScreenWidth, Settings.ScreenHeight);

                LoadMouseCursors();
                SetMouseCursor(MouseCursor.Default);

                SlimDX.Configuration.EnableObjectTracking = true;
                DXManager.Create();
                SoundManager.Create();
                CenterToScreen();
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }

        
        private void Application_Idle(object? sender, EventArgs e) {
            try {
                while (AppStillIdle) {
                    UpdateTime();
                    UpdateEnvironment();

                    if (IsDrawTime()) {
                        RenderEnvironment();
                        UpdateFrameTime();
                    }
                }
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }

        
        private static void CMain_Deactivate(object? sender, EventArgs e) {
            MapControl.MapButtons = MouseButtons.None;
            Shift = false;
            Alt = false;
            Ctrl = false;
            Tilde = false;
            SpellTargetLock = false;
        }

        
        public static void CMain_KeyDown(object? sender, KeyEventArgs e) {
            Shift = e.Shift;
            Alt = e.Alt;
            Ctrl = e.Control;

            if (!string.IsNullOrEmpty(InputKeys.GetKey(KeybindOptions.TargetSpellLockOn))) {
                SpellTargetLock = e.KeyCode == (Keys)Enum.Parse(typeof(Keys), InputKeys.GetKey(KeybindOptions.TargetSpellLockOn), true);
            } else {
                SpellTargetLock = false;
            }

            if (e.KeyCode == Keys.Oem8)
                Tilde = true;

            try {
                if (e is { Alt: true, KeyCode: Keys.Enter }) {
                    ToggleFullScreen();
                    return;
                }

                MirScene.ActiveScene?.OnKeyDown(e);
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }
        
        
        public static void CMain_MouseMove(object? sender, MouseEventArgs e) {
            if (Settings.FullScreen || Settings.MouseClip)
                Cursor.Clip = Program.Form.RectangleToScreen(Program.Form.ClientRectangle);

            MPoint = Program.Form.PointToClient(Cursor.Position);

            try {
                MirScene.ActiveScene?.OnMouseMove(e);
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }
        
        
        public static void CMain_KeyUp(object? sender, KeyEventArgs e) {
            Shift = e.Shift;
            Alt = e.Alt;
            Ctrl = e.Control;

            if (!string.IsNullOrEmpty(InputKeys.GetKey(KeybindOptions.TargetSpellLockOn))) {
                SpellTargetLock = e.KeyCode == (Keys)Enum.Parse(typeof(Keys), InputKeys.GetKey(KeybindOptions.TargetSpellLockOn), true);
            }
            else {
                SpellTargetLock = false;
            }

            if (e.KeyCode == Keys.Oem8)
                Tilde = false;

            foreach (KeyBind KeyCheck in InputKeys.Keylist) {
                if (KeyCheck.function != KeybindOptions.Screenshot) continue;
                
                if (KeyCheck.Key != e.KeyCode)
                    continue;
                
                if ((KeyCheck.RequireAlt != 2) && (KeyCheck.RequireAlt != (Alt ? 1 : 0)))
                    continue;
                
                if ((KeyCheck.RequireShift != 2) && (KeyCheck.RequireShift != (Shift ? 1 : 0)))
                    continue;
                
                if ((KeyCheck.RequireCtrl != 2) && (KeyCheck.RequireCtrl != (Ctrl ? 1 : 0)))
                    continue;
                
                if ((KeyCheck.RequireTilde != 2) && (KeyCheck.RequireTilde != (Tilde ? 1 : 0)))
                    continue;
                
                Program.Form.CreateScreenShot();
                break;
            }
            
            try {
                MirScene.ActiveScene?.OnKeyUp(e);
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }


        private static void CMain_KeyPress(object? sender, KeyPressEventArgs e) {
            try {
                MirScene.ActiveScene?.OnKeyPress(e);
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }


        private static void CMain_MouseDoubleClick(object? sender, MouseEventArgs e) {
            try {
                MirScene.ActiveScene?.OnMouseClick(e);
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }


        private static void CMain_MouseUp(object? sender, MouseEventArgs e) {
            MapControl.MapButtons &= ~e.Button;
            if (e.Button is not MouseButtons.Right || !Settings.NewMove)
                GameScene.CanRun = false;

            try {
                MirScene.ActiveScene?.OnMouseUp(e);
            } catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }


        private static void CMain_MouseDown(object? sender, MouseEventArgs e) {
            if (Program.Form.ActiveControl is TextBox { Tag: MirTextBox { CanLoseFocus: true } })
                Program.Form.ActiveControl = null;

            if (e.Button == MouseButtons.Right && (GameScene.SelectedCell != null || GameScene.PickedUpGold))
            {
                GameScene.SelectedCell = null;
                GameScene.PickedUpGold = false;
                return;
            }

            try {
                MirScene.ActiveScene?.OnMouseDown(e);
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }


        private static void CMain_MouseClick(object? sender, MouseEventArgs e) {
            try {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseClick(e);
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }


        private static void CMain_MouseWheel(object? sender, MouseEventArgs e) {
            try {
                MirScene.ActiveScene?.OnMouseWheel(e);
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
            }
        }

        
        private static void UpdateTime() {
            Time = Timer.ElapsedMilliseconds;
        }

        
        private void UpdateFrameTime() {
            if (Time >= _fpsTime) {
                _fpsTime = Time + 1000;
                FPS = _fps;
                _fps = 0;

                DPS = DPSCounter;
                DPSCounter = 0;
            }
            else {
                _fps++;
            }
        }

        
        private static bool IsDrawTime() {
            const int TargetUpdates = 1000 / 60; // 60 frames per second

            if (Time >= _drawTime) {
                _drawTime = Time + TargetUpdates;
                return true;
            }
            
            return false;
        }
        

        private void UpdateEnvironment() {
            if (Time >= _cleanTime) {
                _cleanTime = Time + 1000;

                DXManager.Clean(); // Clean once a second.
            }

            Network.Process();

            MirScene.ActiveScene?.Process();

            foreach (MirAnimatedControl anim in MirAnimatedControl.Animations)
                anim.UpdateOffSet();

            foreach (MirAnimatedButton anim in MirAnimatedButton.Animations)
                anim.UpdateOffSet();

            CreateHintLabel();

            if (Settings.DebugMode) {
                CreateDebugLabel();
            }
        }

        
        private static void RenderEnvironment() {
            try {
                if (DXManager.DeviceLost) {
                    DXManager.AttemptReset();
                    Thread.Sleep(1);
                    return;
                }

                DXManager.Device.Clear(ClearFlags.Target, Color.Black, 0, 0);
                DXManager.Device.BeginScene();
                DXManager.Sprite.Begin(SpriteFlags.AlphaBlend);
                DXManager.SetSurface(DXManager.MainSurface);

                MirScene.ActiveScene?.Draw();

                DXManager.Sprite.End();
                DXManager.Device.EndScene();
                DXManager.Device.Present();
            }
            catch (Direct3D9Exception ex) {
                DXManager.DeviceLost = true;
                SaveError(ex.ToString());
            }
            catch (Exception ex) {
                SaveError(ex.ToString());
                DXManager.AttemptRecovery();
            }
        }

        
        private void CreateDebugLabel() {
            string text;

            if (MirControl.MouseControl != null) {
                text = $"FPS: {FPS}";

                text += $", DPS: {DPS}";

                text += $", Time: {Now:HH:mm:ss UTC}";

                if (MirControl.MouseControl is MapControl)
                    text += $", Co Ords: {MapControl.MapLocation}";

                if (MirControl.MouseControl is MirImageControl)
                    text += $", Control: {MirControl.MouseControl.GetType().Name}";

                if (MirScene.ActiveScene is GameScene)
                    text += $", Objects: {MapControl.Objects.Count}";

                if (MirScene.ActiveScene is GameScene && !string.IsNullOrEmpty(DebugText))
                    text += $", Debug: {DebugText}";

                if (MirObjects.MapObject.MouseObject != null) {
                    text += $", Target: {MirObjects.MapObject.MouseObject.Name}";
                }
                else {
                    text += string.Format(", Target: none");
                }
            }
            else {
                text = $"FPS: {FPS}";
            }

            text += $", Ping: {PingTime}";

            text += $", Sent: {Functions.ConvertByteSize(BytesSent)}, Received: {Functions.ConvertByteSize(BytesReceived)}";

            text += $", TLC: {DXManager.TextureList.Count(x => x.TextureValid)}";
            text += $", CLC: {DXManager.ControlList.Count(x => x.IsDisposed == false)}";

            if (Settings.FullScreen) {
                if (DebugBaseLabel == null || DebugBaseLabel.IsDisposed) {
                    DebugBaseLabel = new MirControl {
                        BackColour = Color.FromArgb(50, 50, 50),
                        Border = true,
                        BorderColour = Color.Black,
                        DrawControlTexture = true,
                        Location = new Point(5, 5),
                        NotControl = true,
                        Opacity = 0.5F
                    };
                }

                if (DebugTextLabel == null || DebugTextLabel.IsDisposed) {
                    DebugTextLabel = new MirLabel {
                        AutoSize = true,
                        BackColour = Color.Transparent,
                        ForeColour = Color.White,
                        Parent = DebugBaseLabel,
                    };

                    DebugTextLabel.SizeChanged += (_, _) => DebugBaseLabel.Size = DebugTextLabel.Size;
                }

                DebugTextLabel.Text = text;
            }
            else {
                if (DebugBaseLabel is { IsDisposed: false }) {
                    DebugBaseLabel.Dispose();
                    DebugBaseLabel = null;
                }
                
                if (DebugTextLabel is { IsDisposed: false }) {
                    DebugTextLabel.Dispose();
                    DebugTextLabel = null;
                }

                Program.Form.Text = $"{GameLanguage.GameName} - {text}";
            }
        }

        
        private static void CreateHintLabel() {
            if (HintBaseLabel == null || HintBaseLabel.IsDisposed) {
                HintBaseLabel = new MirControl {
                    BackColour = Color.FromArgb(255, 0, 0, 0),
                    Border = true,
                    DrawControlTexture = true,
                    BorderColour = Color.FromArgb(255, 144, 144, 0),
                    ForeColour = Color.Yellow,
                    Parent = MirScene.ActiveScene,
                    NotControl = true,
                    Opacity = 0.5F
                };
            }


            if (HintTextLabel == null || HintTextLabel.IsDisposed) {
                HintTextLabel = new MirLabel {
                    AutoSize = true,
                    BackColour = Color.Transparent,
                    ForeColour = Color.Yellow,
                    Parent = HintBaseLabel,
                };

                HintTextLabel.SizeChanged += (o, e) => HintBaseLabel.Size = HintTextLabel.Size;
            }

            if (MirControl.MouseControl == null || string.IsNullOrEmpty(MirControl.MouseControl.Hint)) {
                HintBaseLabel.Visible = false;
                return;
            }

            HintBaseLabel.Visible = true;
            HintTextLabel.Text = MirControl.MouseControl.Hint;
            Point point = MPoint.Add(-HintTextLabel.Size.Width, 20);

            if (point.X + HintBaseLabel.Size.Width >= Settings.ScreenWidth)
                point.X = Settings.ScreenWidth - HintBaseLabel.Size.Width - 1;
            
            if (point.Y + HintBaseLabel.Size.Height >= Settings.ScreenHeight)
                point.Y = Settings.ScreenHeight - HintBaseLabel.Size.Height - 1;

            if (point.X < 0)
                point.X = 0;
            
            if (point.Y < 0)
                point.Y = 0;

            HintBaseLabel.Location = point;
        }

        
        private static void ToggleFullScreen() {
            Settings.FullScreen = !Settings.FullScreen;
            Program.Form.FormBorderStyle = Settings.FullScreen || Settings.Borderless ? FormBorderStyle.None : FormBorderStyle.FixedDialog;
            Program.Form.TopMost = Settings.FullScreen;
            DXManager.Parameters.Windowed = !Settings.FullScreen;
            Program.Form.ClientSize = new Size(Settings.ScreenWidth, Settings.ScreenHeight);

            DXManager.ResetDevice();

            if (MirScene.ActiveScene == GameScene.Scene) {
                GameScene.Scene.MapControl.FloorValid = false; 
                GameScene.Scene.TextureValid = false;
            }

            Program.Form.CenterToScreen();
        }


        private void CreateScreenShot()
        {
            string text =
                $"[{(Settings.P_ServerName.Length > 0 ? Settings.P_ServerName : "Crystal")} Server {MapControl.User?.Name}] {Now.ToShortDateString()} {Now.TimeOfDay:hh\\:mm\\:ss}";

            Surface backbuffer = DXManager.Device.GetBackBuffer(0, 0);

            using DataStream stream = Surface.ToStream(backbuffer, ImageFileFormat.Png);
            Bitmap image = new Bitmap(stream);

            using Graphics graphics = Graphics.FromImage(image);
            StringFormat sf = new StringFormat {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };

            graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 3, 10), sf);
            graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 4, 9), sf);
            graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 5, 10), sf);
            graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 4, 11), sf);
            graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.White, new Point((Settings.ScreenWidth / 2) + 4, 10), sf);//SandyBrown               

            string path = Path.Combine(Application.StartupPath, @"Screenshots\");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            int count = Directory.GetFiles(path, "*.png").Length;

            image.Save(Path.Combine(path, $"Image {count}.png"), ImageFormat.Png);
        }

        
        public static void SaveError(string ex) {
            try {
                if (Settings.RemainingErrorLogs-- > 0) {
                    File.AppendAllText(@".\Error.txt", $"[{Now}] {ex}{Environment.NewLine}");
                }
            }
            catch {
                // ignored
            }
        }

        
        public static void SetResolution(int width, int height) {
            if (Settings.ScreenWidth == width && Settings.ScreenHeight == height) return;

            Settings.ScreenWidth = width;
            Settings.ScreenHeight = height;
            Program.Form.ClientSize = new Size(width, height);

            DXManager.Device.Clear(ClearFlags.Target, Color.Black, 0, 0);
            DXManager.Device.Present();
            DXManager.ResetDevice();

            if (!Settings.FullScreen)
                Program.Form.CenterToScreen();
        }

        #region ScreenCapture

        //private Bitmap CaptureScreen()
        //{
            
        //}

        #endregion

        #region Idle Check
        private static bool AppStillIdle {
            get {
                PeekMsg msg;
                return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool PeekMessage(out PeekMsg msg, IntPtr hWnd, uint messageFilterMin,
            uint messageFilterMax, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct PeekMsg
        {
            private readonly IntPtr hWnd;
            private readonly Message msg;
            private readonly IntPtr wParam;
            private readonly IntPtr lParam;
            private readonly uint time;
            private readonly Point p;
        }
        #endregion

        
        private void CMain_FormClosing(object sender, FormClosingEventArgs e) {
            if (Time < GameScene.LogTime && !Settings.UseTestConfig && !GameScene.Observing) {
                GameScene.Scene.ChatDialog.ReceiveChat(string.Format(GameLanguage.CannotLeaveGame, (GameScene.LogTime - CMain.Time) / 1000), ChatType.System);
                e.Cancel = true;
            }
            else {
                Settings.Save();

                DXManager.Dispose();
                SoundManager.Dispose();
            }
        }

        
        protected override void WndProc(ref Message m) {
            // WM_SYSCOMMAND
            if (m.Msg == 0x0112) {
                switch (m.WParam.ToInt32()) {
                    // SC_KEYMENU
                    case 0xF100:
                        m.Result = IntPtr.Zero;
                        return;
                    // SC_MAXIMISE
                    case 0xF030:
                        ToggleFullScreen();
                        return;
                }
            }

            base.WndProc(ref m);
        }


        public static Cursor[] Cursors { get; private set; } = null!;
        private static MouseCursor current_cursor = MouseCursor.None;


        public static void SetMouseCursor(MouseCursor cursor) {
            if (!Settings.UseMouseCursors) return;

            if (current_cursor != cursor) {
                current_cursor = cursor;
                Program.Form.Cursor = Cursors[(byte)cursor];
            }
        }

        
        private static void LoadMouseCursors() {
            Cursors = new Cursor[8];

            Cursors[(int)MouseCursor.None] = Program.Form.Cursor;
            
            string[] names = ["Cursor_Default", "Cursor_Normal_Atk", "Cursor_Compulsion_Atk", "Cursor_Npc", 
                "Cursor_TextPrompt", "Cursor_Trash", "Cursor_Upgrade"];

            for (int i = 0; i < names.Length; i++) {
                string path = $"{Settings.MouseCursorPath}{names[i]}.CUR";
                if (File.Exists(path)) {
                    Cursors[i + 1] = LoadCustomCursor(path);
                }
            }
        }


        private static Cursor LoadCustomCursor(string path) {
            IntPtr hCurs = LoadCursorFromFile(path);
            if (hCurs == IntPtr.Zero) throw new Win32Exception();
            var curs = new Cursor(hCurs);
            // Note: force the cursor to own the handle so it gets released properly
            //var fi = typeof(Cursor).GetField("ownHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            //fi.SetValue(curs, true);
            return curs;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadCursorFromFile(string path);
    }
}