using System.Net.Http.Handlers;
using System.Net.Http.Headers;

namespace Client.Utils;

public class FileDownloader {
    private long _downloaded_size;
    private float _download_speed;
    private long _speed_count;
    
    private DateTime _last_speed_update_time = DateTime.Now;

    public long DownloadedSize {
        get => _downloaded_size;
        init => _downloaded_size = value;
    }

    public float DownloadSpeed {
        get {
            DateTime now = DateTime.Now;
            TimeSpan diff_time = now - _last_speed_update_time;
            if (diff_time > TimeSpan.FromSeconds(1)) {
                _last_speed_update_time = now;
                _download_speed = Interlocked.Exchange(ref _speed_count, 0) / (float)diff_time.TotalSeconds;
            }
            
            return _download_speed; 
            
        }
    }


    public async Task<bool> AsyncDownload(string host, FileInformation file_info, string root_save_dir) {
        string save_path = Path.Combine(root_save_dir, file_info.RelativePath);
        string save_dir = Path.GetDirectoryName(save_path)!;
        if (!Directory.Exists(save_dir)) {
            Directory.CreateDirectory(save_dir);
        }

        try {
            if (File.Exists(save_path)) {
                File.Delete(save_path);
            }

            using HttpClientHandler http_handler = new();
            http_handler.AllowAutoRedirect = true;
            using ProgressMessageHandler progress_handler = new(http_handler);
            long bytes_transferred = 0;
            progress_handler.HttpReceiveProgress += (_, args) => {
                Interlocked.Add(ref _speed_count, args.BytesTransferred - bytes_transferred); 
                bytes_transferred = args.BytesTransferred;
            };

            using var client = new HttpClient(progress_handler);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptCharset.Clear();
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

            // if (need_login) {
            //     string auth_info = Settings.P_Login + ":" + Settings.P_Password;
            //     auth_info = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(auth_info));
            //     client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth_info);
            // }

            HttpResponseMessage response = await client.GetAsync($"{host}{file_info.RelativePath}", HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) {
                return false;
            }

            byte[] data = await response.Content.ReadAsByteArrayAsync();
            if (file_info.Compressed > 0 && file_info.Compressed != data.Length) {
                data = Functions.DecompressBytes(data);
            }

            Interlocked.Add(ref _downloaded_size, data.Length);
            await File.WriteAllBytesAsync(save_path, data);
            return true;
        }
        catch (HttpRequestException e) {
            await File.AppendAllTextAsync(@".\Error.txt",
                $"[{DateTime.Now}] {file_info.RelativePath} could not be downloaded. ({e.Message}) {Environment.NewLine}");
        }
        catch (Exception e) {
            Console.WriteLine(e);

            if (File.Exists(save_path)) {
                File.Delete(save_path);
            }

            MessageBox.Show($"Failed to download file: {file_info.RelativePath}");
        }

        return false;
    }
    
    
    // private void UpdateDownloadSpeed(long new_downloaded_size) {
    //     Interlocked.Add(ref _speed_count, new_downloaded_size); 
    //     datetime now = datetime.now;
    //     timespan diff_time = now - _last_speed_update_time;
    //     if (diff_time > timespan.fromseconds(1)) {
    //         _last_speed_update_time = now;
    //         Interlocked.Exchange(ref _download_speed, _speed_count / (float)diff_time.TotalSeconds);
    //         Interlocked.Exchange(ref _speed_count, 0);
    //     }
    // }
    
    
    //download small file
    public static async Task<Stream?> AsyncDownload(string url) {
        try {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptCharset.Clear();
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) {
                return null;
            }

            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception e) {
            Console.WriteLine(e);
            return null;
        }
    }
}