using System.Diagnostics;
using System.Net;
using Client;
using Microsoft.Web.WebView2.Core;
using Client.Utils;

namespace Launcher
{
    public partial class AMain : Form
    {
        private FileDownloader? _file_downloader;
        private long _total_size;
        private int _total_file_count, _valid_file_count;
        private int _download_task_count;

        public bool Completed, Checked, CleanFiles;
        private bool LabelSwitch;
        private bool ErrorFound;

        private List<FileInformation> _server_files = [];
        private readonly Queue<FileInformation> _need_download_files = new();

        private Stopwatch _stopwatch = Stopwatch.StartNew();

        public Thread? _workThread;

        private bool dragging;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private readonly Config ConfigForm = new();

        private bool Restart;

        public AMain()
        {
            InitializeComponent();

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
            BackColor = Color.FromArgb(1, 0, 0);
            try {
                CleanUp();
                GetServerFileInfos();

                if (_server_files.Count == 0) {
                    MessageBox.Show(GameLanguage.PatchErr);
                    Completed = true;
                    return;
                }

                _total_file_count = _server_files.Count;
                long valid_size = 0;
                foreach (FileInformation info in _server_files) {
                    _total_size += info.Length;
                    if (CheckFile(info)) {
                        valid_size += info.Length;
                        _valid_file_count++;
                    } else {
                        _need_download_files.Enqueue(info);
                    }
                }

                _file_downloader = new FileDownloader() {
                    DownloadedSize = valid_size
                };

                Checked = true;
                _stopwatch = Stopwatch.StartNew();
                BeginDownload().Wait();
            }
            catch (EndOfStreamException ex) {
                MessageBox.Show("End of stream found. Host is likely using a pre version 1.1.0.0 patch system");
                SaveError(ex.ToString());
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Error");
                SaveError(ex.ToString());
            }

            _file_downloader = null;
            Completed = true;
            _stopwatch.Stop();
        }

        
        private async Task BeginDownload() {
            ServicePointManager.DefaultConnectionLimit = Settings.P_Concurrency;

            List<Task<bool>> tasks = [];
            for (var i = 0; i < Settings.P_Concurrency; i++) {
                if (_need_download_files.TryDequeue(out var file_info)) {
                    var task = _file_downloader!.AsyncDownload(Settings.P_Host, file_info, Settings.P_Client);
                    tasks.Add(task);
                    ++_download_task_count;
                }
            }

            while (tasks.Count > 0) {
                Task<bool> finish_task = await Task.WhenAny(tasks);
                tasks.Remove(finish_task);
                --_download_task_count;
                if (await finish_task) {
                    _valid_file_count++;
                }
                else {
                    // _error_count++;
                    ErrorFound = true;
                }
                
                if (_need_download_files.TryDequeue(out var file_info)) {
                    var task = _file_downloader!.AsyncDownload(Settings.P_Host, file_info, Settings.P_Client);
                    tasks.Add(task);
                    ++_download_task_count;
                }
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
            return _server_files.Any(info => name.EndsWith(info.RelativePath));
        }

        
        private void GetServerFileInfos() {
            _server_files = [];
            string uri_string = Settings.P_Host + Path.ChangeExtension(Settings.P_PatchFileName, ".gz");
            Stream? stream = FileDownloader.AsyncDownload(uri_string).Result;
            if (stream == null) return;
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            
            using BinaryReader reader = new (ms);
            if (reader.ReadByte() == 60) {
                //assume we got a html page back with an error code so it's not a patchlist
                return;
            }
            
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                _server_files.Add(new FileInformation(reader));
            }
        }


        private bool CheckFile(FileInformation info) {
            info.RelativePath = info.RelativePath.Replace(@"\", "/");
            if (info.RelativePath != "PList.gz" && (info.Compressed != info.Length || info.Compressed == 0)) {
                info.RelativePath += ".gz";
            }
            
            FileInformation? local_info = GetFileInformation(Settings.P_Client, info.RelativePath);
            _valid_file_count++;
            return (local_info != null && info.Length == local_info.Length && info.Creation == local_info.Creation);
        }


        // private async Task DownloadFile(Download dl) {
        //first remove the original file if needed
        // string[] special_files = [".dll", ".exe", ".pdb"];
        // if (File.Exists(output_path) && special_files.Contains(Path.GetExtension(output_path).ToLower())) {
        //     string old_filename = Path.Combine(output_dir, "Old__" + Path.GetFileName(output_path));
        //     try {
        //         //if there's another previous backup: delete it first
        //         if (File.Exists(old_filename)) {
        //             File.Delete(old_filename);   
        //         }
        //         
        //         File.Move(output_path, old_filename);
        //         
        //     } catch (UnauthorizedAccessException ex) {
        //         SaveError(ex.ToString());
        //         _error_count++;
        //         string error_msg = _error_count >= 5 ? "Too many problems occured, no longer displaying future errors" :
        //             $"Problem occured saving this file: {output_path}";
        //         MessageBox.Show(error_msg);
        //         
        //     } catch (Exception ex) {
        //         SaveError(ex.ToString());
        //         _error_count++;
        //         string error_msg = _error_count >= 5 ? "Too many problems occured, no longer displaying future errors" :
        //             $"Problem occured saving this file: {output_path}";
        //         MessageBox.Show(error_msg);
        //         
        //     } finally {
        //         //Might cause an infinite loop if it can never gain access
        //         Restart = true;
        //     }
        // }
        // }

        
        private static FileInformation? GetFileInformation(string root_dir, string relative_path) {
            string file_path = Path.Combine(root_dir, relative_path);
            if (!File.Exists(file_path)) return null;

            FileInfo info = new FileInfo(file_path);
            return new FileInformation {
                RelativePath = relative_path,
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
                if (Completed) {
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

                ActionLabel.Visible = true;
                SpeedLabel.Visible = true;
                CurrentFile_label.Visible = true;
                CurrentPercent_label.Visible = true;
                TotalPercent_label.Visible = true;

                long downloaded_size = _file_downloader!.DownloadedSize;
                ActionLabel.Text = LabelSwitch ? 
                    $"{_total_file_count - _valid_file_count} Files Remaining" : 
                    $"{(_total_size - downloaded_size) / 1024 / 1024:#,##0}MB Remaining";

                CurrentFile_label.Text = $"<Concurrent> {_download_task_count}";
                SpeedLabel.Text = ToSize(_file_downloader.DownloadSpeed);

                TotalProg_pb.Width = (int)(5.5f * (100.0f * (downloaded_size) / _total_size));
                TotalPercent_label.Text = $"{(int)(100.0f * (downloaded_size) / _total_size)} %";

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

            foreach (var path in files) {
                string original_path = path.Replace("Old__", "");
                if (!File.Exists(original_path)) {
                    File.Move(path, original_path);
                } else {
                    File.Delete(path);
                }
            }
        }

        
        private static void MoveOldFilesToCurrent() {
            var files = Directory.GetFiles(Settings.P_Client, "*", SearchOption.AllDirectories).Where(x => Path.GetFileName(x).StartsWith("Old__"));
            foreach (var path in files) {
                string original_path = path.Replace("Old__", "");

                if (!File.Exists(original_path))
                    File.Move(path, original_path);
            }
        }
    } 
}