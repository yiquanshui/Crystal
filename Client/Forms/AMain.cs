using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Client;
using Microsoft.Web.WebView2.Core;
using System.Net.Http.Headers;
using System.Net.Http.Handlers;
using Client.Utils;

namespace Launcher
{
    public partial class AMain : Form
    {
        private long _totalBytes, _completedBytes;
        private int _fileCount, _currentCount;

        public bool Completed, Checked, CleanFiles;
        private bool LabelSwitch;
        private bool ErrorFound;

        private List<FileInformation> OldList;
        private readonly Queue<FileInformation> DownloadList = new();
        private readonly List<Download> ActiveDownloads = [];

        private Stopwatch _stopwatch = Stopwatch.StartNew();

        public Thread _workThread;

        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private readonly Config ConfigForm = new Config();

        private bool Restart = false;

        public AMain()
        {
            InitializeComponent();

            BackColor = Color.FromArgb(1, 0, 0);
            TransparencyKey = Color.FromArgb(1, 0, 0);
        }


        private static void SaveError(string ex) {
            try {
                if (Settings.RemainingErrorLogs-- > 0) {
                    File.AppendAllText(@".\Error.txt", $"[{DateTime.Now}] {ex}{Environment.NewLine}");
                }
            } 
            catch {
                // ignored
            }
        }

        
        public void Start() {
            try {
                GetOldFileList();

                if (OldList.Count == 0) {
                    MessageBox.Show(GameLanguage.PatchErr);
                    Completed = true;
                    return;
                }

                _fileCount = OldList.Count;
                foreach (FileInformation info in OldList)
                    CheckFile(info);

                Checked = true;
                _fileCount = 0;
                _currentCount = 0;

                _fileCount = DownloadList.Count;

                _stopwatch = Stopwatch.StartNew();
                BeginDownload().Wait();
            }
            catch (EndOfStreamException ex) {
                MessageBox.Show("End of stream found. Host is likely using a pre version 1.1.0.0 patch system");
                Completed = true;
                SaveError(ex.ToString());
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Error");
                Completed = true;
                SaveError(ex.ToString());
            }

            _stopwatch.Stop();
        }

        
        private async Task BeginDownload() {
            if (DownloadList.Count == 0) {
                Completed = true;
                CleanUp();
                return;
            }

            ServicePointManager.DefaultConnectionLimit = Settings.P_Concurrency;

            List<Task> tasks = [];
            for (var i = 1; i < Settings.P_Concurrency; i++) {
                if (DownloadList.TryDequeue(out var file_info)) {
                    var download = new Download {
                        Info = file_info
                    };
                    tasks.Add(DownloadFile(download));
                };
            }

            while (tasks.Count > 0) {
                Task finish_task = Task.WhenAny(tasks);
                await finish_task;
                tasks.Remove(finish_task);
                
                if (DownloadList.TryDequeue(out var file_info)) {
                    var download = new Download {
                        Info = file_info
                    };
                    tasks.Add(DownloadFile(download));
                };
            }
        }

        
        private void CleanUp() {
            if (!CleanFiles) return;

            string[] files = Directory.GetFiles(@".\", "*.*", SearchOption.AllDirectories);
            foreach (string path in files) {
                if (path.StartsWith(".\\Screenshots\\")) continue;

                string name = Path.GetFileName(path);

                if (name == "Mir2Config.ini" || name == AppDomain.CurrentDomain.FriendlyName) continue;

                try {
                    if (!NeedFile(path))
                        File.Delete(path);
                } catch {
                    // ignored
                }
            }
        }


        private bool NeedFile(string name) {
            return OldList.Any(info => name.EndsWith(info.FileName));
        }

        
        private void GetOldFileList()
        {
            OldList = [];
            string uri_string = Settings.P_Host + Path.ChangeExtension(Settings.P_PatchFileName, ".gz");
            byte[]? data = DownloadFile(uri_string).Result;
            if (data == null) return;
            
            using MemoryStream stream = new (data);
            using BinaryReader reader = new (stream);
            if (reader.ReadByte() == 60)
            {
                //assume we got a html page back with an error code so it's not a patchlist
                return;
            }
            
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                OldList.Add(new FileInformation(reader));
            }
        }


        public void ParseOld(BinaryReader reader) {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                OldList.Add(new FileInformation(reader));
        }

        
        private void CheckFile(FileInformation old) {
            FileInformation? info = GetFileInformation(Settings.P_Client + old.FileName);
            _currentCount++;
            if (info != null && old.Length == info.Length && old.Creation == info.Creation) return;
            
            DownloadList.Enqueue(old);
            _totalBytes += old.Length;
        }

        private int error_count = 0;

        //TODO : implement true multi download
        private async Task DownloadFile(Download dl) {
            FileInformation info = dl.Info;
            string name = info.FileName.Replace(@"\", "/");
            if (name != "PList.gz" && (info.Compressed != info.Length || info.Compressed == 0)) {
                name += ".gz";
            }

            try {
                using HttpClientHandler http_handler = new();
                http_handler.AllowAutoRedirect = true;
                using ProgressMessageHandler progress_handler = new(http_handler);
                progress_handler.HttpReceiveProgress += (_, args) => {
                    dl.CurrentBytes = args.BytesTransferred;
                };

                using HttpClient client = new(progress_handler);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptCharset.Clear();
                client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

                if (Settings.P_NeedLogin) {
                    string auth_info = Settings.P_Login + ":" + Settings.P_Password;
                    auth_info = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(auth_info));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth_info);
                }

                ActiveDownloads.Add(dl);
                _currentCount++;
                
                Uri file_uri = new($"{Settings.P_Host}{name}");
                HttpResponseMessage response = await client.GetAsync(file_uri, HttpCompletionOption.ResponseHeadersRead);
                byte[] data = await response.Content.ReadAsByteArrayAsync();

                _completedBytes += dl.CurrentBytes;
                dl.CurrentBytes = 0;
                dl.Completed = true;

                if (info.Compressed > 0 && info.Compressed != info.Length) {
                    data = Functions.DecompressBytes(data);
                }

                string output_path = Settings.P_Client + info.FileName;
                string? output_dir = Path.GetDirectoryName(output_path);
                if (output_dir is null) {
                    throw new DirectoryNotFoundException("Directory not found");
                }
                
                if (!Directory.Exists(output_dir))
                    Directory.CreateDirectory(output_dir);

                //first remove the original file if needed
                string[] special_files = [".dll", ".exe", ".pdb"];
                if (File.Exists(output_path) && special_files.Contains(Path.GetExtension(output_path).ToLower())) {
                    string old_filename = Path.Combine(output_dir, "Old__" + Path.GetFileName(output_path));
                    try {
                        //if there's another previous backup: delete it first
                        if (File.Exists(old_filename)) {
                            File.Delete(old_filename);   
                        }
                        
                        File.Move(output_path, old_filename);
                        
                    } catch (UnauthorizedAccessException ex) {
                        SaveError(ex.ToString());
                        error_count++;
                        string error_msg = error_count >= 5 ? "Too many problems occured, no longer displaying future errors" :
                            $"Problem occured saving this file: {output_path}";
                        MessageBox.Show(error_msg);
                        
                    } catch (Exception ex) {
                        SaveError(ex.ToString());
                        error_count++;
                        string error_msg = error_count >= 5 ? "Too many problems occured, no longer displaying future errors" :
                            $"Problem occured saving this file: {output_path}";
                        MessageBox.Show(error_msg);
                        
                    } finally {
                        //Might cause an infinite loop if it can never gain access
                        Restart = true;
                    }
                }

                await File.WriteAllBytesAsync(output_path, data);
                File.SetLastWriteTime(output_path, info.Creation);
                
            } catch (HttpRequestException e) {
                await File.AppendAllTextAsync(@".\Error.txt",
                    $"[{DateTime.Now}] {info.FileName} could not be downloaded. ({e.Message}) {Environment.NewLine}");
                ErrorFound = true;
                
            } catch (Exception ex) {
                SaveError(ex.ToString());
                error_count++;
                error_count++;
                string error_msg = error_count >= 5 ? "Too many problems occured, no longer displaying future errors" :
                    $"Problem occured saving this file: {dl.Info.FileName}";
                MessageBox.Show(error_msg);
                
            } finally {
                if (ErrorFound) {
                    MessageBox.Show($"Failed to download file: {name}");
                }
            }

            // BeginDownload();
        }

        
        private static async Task<byte[]?> DownloadFile(string file_url) {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptCharset.Clear();
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

            if (Settings.P_NeedLogin) {
                string auth_info = Settings.P_Login + ":" + Settings.P_Password;
                auth_info = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(auth_info));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth_info);
            }

            if (Uri.IsWellFormedUriString(file_url, UriKind.Absolute)) {
                HttpResponseMessage response = await client.GetAsync(new Uri(file_url), HttpCompletionOption.ResponseHeadersRead);
                await using Stream sm = await response.Content.ReadAsStreamAsync();
                using MemoryStream ms = new();
                await sm.CopyToAsync(ms);
                byte[] data = ms.ToArray();
                return data;
            }

            MessageBox.Show("Please Check Launcher HOST Setting is formatted correctly\nCan be caused by missing or extra slashes and spelling mistakes.\nThis error can be ignored if patching is not required.", "Bad HOST Format");
            return null;
        }
        
        private static FileInformation? GetFileInformation(string file_path) {
            if (!File.Exists(file_path)) return null;

            FileInfo info = new FileInfo(file_path);
            return new FileInformation {
                FileName = file_path.Remove(0, Settings.P_Client.Length),
                Length = (int)info.Length,
                Creation = info.LastWriteTime
            };
        }

        
        private void AMain_Load(object sender, EventArgs e) {
            var env = CoreWebView2Environment.CreateAsync(null, Settings.ResourcePath).Result;
            Main_browser.EnsureCoreWebView2Async(env);

            if (Settings.P_BrowserAddress != "") {
                if (Uri.IsWellFormedUriString(Settings.P_BrowserAddress, UriKind.Absolute)) {
                    Main_browser.NavigationCompleted += Main_browser_NavigationCompleted;
                    Main_browser.Source = new Uri(Settings.P_BrowserAddress);
                } else {
                    MessageBox.Show("Please Check Launcher BROWSER Setting is formatted correctly.\nCan be caused by missing or extra slashes and spelling mistakes.\nThis error can be ignored.", "Bad BROWSER Format");
                }
            }

            RepairOldFiles();

            Launch_pb.Enabled = false;
            ProgressCurrent_pb.Width = 5;
            TotalProg_pb.Width = 5;
            Version_label.Text = $"Build: {Globals.ProductCodename}.{(Settings.UseTestConfig ? "Debug" : "Release")}.{Application.ProductVersion}";

            if (Settings.P_ServerName != string.Empty) {
                Name_label.Visible = true;
                Name_label.Text = Settings.P_ServerName;
            }

            _workThread = new Thread(Start) { IsBackground = true };
            _workThread.Start();
        }

        
        private void Main_browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (Main_browser.Source.AbsolutePath != "blank") Main_browser.Visible = true;
        }

        
        private void Launch_pb_Click(object sender, EventArgs e) {
            Launch();
        }

        
        private void Launch() {
            if (ConfigForm.Visible) ConfigForm.Visible = false;

            Program.Launch = true;
            Close();
        }

        
        private void Close_pb_Click(object sender, EventArgs e) {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
            
            Close();
        }

        
        private void Movement_panel_MouseClick(object sender, MouseEventArgs e) {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
            
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        
        private void Movement_panel_MouseUp(object sender, MouseEventArgs e) {
            dragging = false;
        }

        
        private void Movement_panel_MouseMove(object sender, MouseEventArgs e) {
            if (!dragging) return;
            
            Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
            Location = Point.Add(dragFormPoint, new Size(dif));
        }

        
        private void Launch_pb_MouseEnter(object sender, EventArgs e) {
            Launch_pb.Image = Client.Resources.Images.Launch_Hover;
        }

        
        private void Launch_pb_MouseLeave(object sender, EventArgs e) {
            Launch_pb.Image = Client.Resources.Images.Launch_Base1;
        }

        
        private void Close_pb_MouseEnter(object sender, EventArgs e) {
            Close_pb.Image = Client.Resources.Images.Cross_Hover;
        }

        
        private void Close_pb_MouseLeave(object sender, EventArgs e) {
            Close_pb.Image = Client.Resources.Images.Cross_Base;
        }

        
        private void Launch_pb_MouseDown(object sender, MouseEventArgs e) {
            Launch_pb.Image = Client.Resources.Images.Launch_Pressed;
        }

        
        private void Launch_pb_MouseUp(object sender, MouseEventArgs e) {
            Launch_pb.Image = Client.Resources.Images.Launch_Base1;
        }

        
        private void Close_pb_MouseDown(object sender, MouseEventArgs e) {
            Close_pb.Image = Client.Resources.Images.Cross_Pressed;
        }

        
        private void Close_pb_MouseUp(object sender, MouseEventArgs e) {
            Close_pb.Image = Client.Resources.Images.Cross_Base;
        }

        
        private void ProgressCurrent_pb_SizeChanged(object sender, EventArgs e) {
            ProgEnd_pb.Location = new Point((ProgressCurrent_pb.Location.X + ProgressCurrent_pb.Width), ProgressCurrent_pb.Location.Y);
            ProgEnd_pb.Visible = ProgressCurrent_pb.Width != 0;
        }

        
        private void Config_pb_MouseDown(object sender, MouseEventArgs e) {
            Config_pb.Image = Client.Resources.Images.Config_Pressed;
        }

        
        private void Config_pb_MouseEnter(object sender, EventArgs e) {
            Config_pb.Image = Client.Resources.Images.Config_Hover;
        }

        
        private void Config_pb_MouseLeave(object sender, EventArgs e) {
            Config_pb.Image = Client.Resources.Images.Config_Base;
        }

        
        private void Config_pb_MouseUp(object sender, MouseEventArgs e) {
            Config_pb.Image = Client.Resources.Images.Config_Base;
        }

        
        private void Config_pb_Click(object sender, EventArgs e) {
            if (ConfigForm.Visible) ConfigForm.Hide();
            else ConfigForm.Show(Program.PForm);
            ConfigForm.Location = new Point(Location.X + Config_pb.Location.X - 183, Location.Y + 36);
        }

        
        private void TotalProg_pb_SizeChanged(object sender, EventArgs e) {
            ProgTotalEnd_pb.Location = new Point((TotalProg_pb.Location.X + TotalProg_pb.Width), TotalProg_pb.Location.Y);
            ProgTotalEnd_pb.Visible = TotalProg_pb.Width != 0;
        }

        
        private void InterfaceTimer_Tick(object sender, EventArgs e) {
            try {
                if (Completed && ActiveDownloads.Count == 0) {
                    ActionLabel.Text = "";
                    CurrentFile_label.Text = "Up to date.";
                    SpeedLabel.Text = "";
                    ProgressCurrent_pb.Width = 550;
                    TotalProg_pb.Width = 550;
                    CurrentFile_label.Visible = true;
                    CurrentPercent_label.Visible = true;
                    TotalPercent_label.Visible = true;
                    CurrentPercent_label.Text = "100%";
                    TotalPercent_label.Text = "100%";
                    InterfaceTimer.Enabled = false;
                    Launch_pb.Enabled = true;
                    if (ErrorFound) MessageBox.Show("One or more files failed to download, check Error.txt for details.", "Failed to Download.");
                    ErrorFound = false;

                    if (CleanFiles) {
                        CleanFiles = false;
                        MessageBox.Show("Your files have been cleaned up.", "Clean Files");
                    }

                    if (Restart) {
                        Program.Restart = true;
                        Close();
                    }

                    if (Settings.P_AutoStart) {
                        Launch();
                    }
                    
                    return;
                }

                var currentBytes = 0L;
                FileInformation? currentFile = null;

                // Remove completed downloads..
                for (int i = ActiveDownloads.Count - 1; i >= 0; i--) {
                    Download dl = ActiveDownloads[i];
                    if (dl.Completed) {
                        ActiveDownloads.RemoveAt(i);
                    }
                }

                for (int i = ActiveDownloads.Count - 1; i >= 0; i--) {
                    Download dl = ActiveDownloads[i];
                    if (!dl.Completed)
                        currentBytes += dl.CurrentBytes;
                }

                if (Settings.P_Concurrency == 1)
                {
                    // Note: Just mimic old behaviour for now until a better UI is done.
                    if (ActiveDownloads.Count > 0)
                        currentFile = ActiveDownloads[0].Info;
                }

                ActionLabel.Visible = true;
                SpeedLabel.Visible = true;
                CurrentFile_label.Visible = true;
                CurrentPercent_label.Visible = true;
                TotalPercent_label.Visible = true;

                ActionLabel.Text = LabelSwitch ? 
                    $"{_fileCount - _currentCount} Files Remaining" : 
                    $"{((_totalBytes) - (_completedBytes + currentBytes)) / 1024 / 1024:#,##0}MB Remaining";

                if (Settings.P_Concurrency > 1) {
                    CurrentFile_label.Text = $"<Concurrent> {ActiveDownloads.Count}";
                    SpeedLabel.Text = ToSize(currentBytes / _stopwatch.Elapsed.TotalSeconds);
                } else {
                    if (currentFile != null) {
                        CurrentFile_label.Text = $"{currentFile.FileName}";
                        SpeedLabel.Text = ToSize(currentBytes / _stopwatch.Elapsed.TotalSeconds);
                        CurrentPercent_label.Text = ((int)(100 * currentBytes / currentFile.Length)) + "%";
                        ProgressCurrent_pb.Width = (int)(5.5f * (100.0f * currentBytes / currentFile.Length));
                    }
                }

                if (!(_completedBytes is 0 && currentBytes is 0 && _totalBytes is 0)) {
                    TotalProg_pb.Width = (int)(5.5f * (100.0f * (_completedBytes + currentBytes) / _totalBytes));
                    TotalPercent_label.Text = $"{(int)(100.0f * (_completedBytes + currentBytes) / _totalBytes)} %";
                }

            } catch {
                //to-do 
            }

        }

        
        private void AMain_Click(object sender, EventArgs e) {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
        }

        
        private void ActionLabel_Click(object sender, EventArgs e) {
            LabelSwitch = !LabelSwitch;
        }

        
        private void Credit_label_Click(object sender, EventArgs e) {
            Credit_label.Text = Credit_label.Text == "Powered by Crystal M2" ? "Designed by Breezer" : "Powered by Crystal M2";
        }

        
        private void AMain_FormClosed(object sender, FormClosedEventArgs e) {
            MoveOldFilesToCurrent();
            Launch_pb?.Dispose();
            Close_pb?.Dispose();
        }

        
        private static readonly string[] suffixes = [" B", " KB", " MB", " GB", " TB", " PB"];

        private static string ToSize(double number, int precision = 2) {
            // unit's number of bytes
            const double unit = 1024;
            // suffix counter
            int i = 0;
            // as long as we're bigger than a unit, keep going
            while (number > unit) {
                number /= unit;
                i++;
            }
            
            // apply precision and current suffix
            return Math.Round(number, precision) + suffixes[i];
        }

        
        private static void RepairOldFiles() {
            var files = Directory.GetFiles(Settings.P_Client, "*", SearchOption.AllDirectories).Where(x => Path.GetFileName(x).StartsWith("Old__"));

            foreach (var oldFilename in files) {
                if (!File.Exists(oldFilename.Replace("Old__", ""))) {
                    File.Move(oldFilename, oldFilename.Replace("Old__", ""));
                } else {
                    File.Delete(oldFilename);
                }
            }
        }

        
        private static void MoveOldFilesToCurrent() {
            var files = Directory.GetFiles(Settings.P_Client, "*", SearchOption.AllDirectories).Where(x => Path.GetFileName(x).StartsWith("Old__"));

            foreach (var oldFilename in files) {
                string originalFilename = Path.Combine(Path.GetDirectoryName(oldFilename)!, (Path.GetFileName(oldFilename).Replace("Old__", "")));

                if (!File.Exists(originalFilename) && File.Exists(oldFilename))
                    File.Move(oldFilename, originalFilename);
            }
        }
    } 
}