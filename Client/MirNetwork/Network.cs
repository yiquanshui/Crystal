using System.Collections.Concurrent;
using System.Net.Sockets;
using Client.MirControls;
using C = ClientPackets;


namespace Client.MirNetwork
{
    internal static class Network
    {
        private static TcpClient? _client;
        public static int ConnectAttempt = 0;
        private const int MaxAttempts = 20;
        private static bool ErrorShown;
        public static bool Connected;
        private static long TimeOutTime, TimeConnected, RetryTime = CMain.Time + 5000;

        private static ConcurrentQueue<Packet>? _receiveList;
        private static ConcurrentQueue<Packet>? _sendList;

        private static byte[] _rawData = [];
        private static readonly byte[] _rawBytes = new byte[8 * 1024];

        public static void Connect() {
            if (_client != null)
                Disconnect();

            if (ConnectAttempt >= MaxAttempts) {
                if (ErrorShown) {
                    return;
                }

                ErrorShown = true;

                MirMessageBox errorBox = new("Error Connecting to Server", MirMessageBoxButtons.Cancel);
                errorBox.CancelButton.Click += (o, e) => Program.Form.Close();
                errorBox.Label.Text = $"Maximum Connection Attempts Reached: {MaxAttempts}" +
                                      $"{Environment.NewLine}Please try again later or check your connection settings.";
                errorBox.Show();
                return;
            }

            ConnectAttempt++;

            try {
                _client = new TcpClient { NoDelay = true };
                _client.BeginConnect(Settings.IPAddress, Settings.Port, Connection, null);
            }
            catch (ObjectDisposedException ex) {
                if (Settings.LogErrors) CMain.SaveError(ex.ToString());
                Disconnect();
            }
        }

        
        private static void Connection(IAsyncResult result) {
            try {
                _client?.EndConnect(result);

                if (_client is { Connected: false } or null) {
                    Connect();
                    return;
                }

                _receiveList = new ConcurrentQueue<Packet>();
                _sendList = new ConcurrentQueue<Packet>();
                _rawData = [];

                TimeOutTime = CMain.Time + Settings.TimeOut;
                TimeConnected = CMain.Time;

                BeginReceive();
            }
            catch (SocketException) {
                Thread.Sleep(100);
                Connect();
            }
            catch (Exception ex) {
                if (Settings.LogErrors) CMain.SaveError(ex.ToString());
                Disconnect();
            }
        }

        
        private static void BeginReceive() {
            if (_client is not { Connected: true }) return;

            try {
                _client.Client.BeginReceive(_rawBytes, 0, _rawBytes.Length, SocketFlags.None, ReceiveData, _rawBytes);
            }
            catch {
                Disconnect();
            }
        }
        
        
        private static void ReceiveData(IAsyncResult result) {
            if (_client is not { Connected: true }) return;

            int dataRead;

            try {
                dataRead = _client.Client.EndReceive(result);
            }
            catch {
                Disconnect();
                return;
            }

            if (dataRead == 0) {
                Disconnect();
            }

            byte[] new_data = (result.AsyncState as byte[])!;

            byte[] temp = _rawData;
            _rawData = new byte[dataRead + temp.Length];
            if (temp.Length > 0) {
                _rawData = new byte[dataRead + temp.Length];
                Buffer.BlockCopy(temp, 0, _rawData, 0, temp.Length);
            } 
            else {
                _rawData = new byte[dataRead];
            }
            
            Buffer.BlockCopy(new_data, 0, _rawData, temp.Length, dataRead);

            int packet_bytes = 0;
            while (Packet.ReceivePacket(_rawData, out _rawData) is { } p) {
                packet_bytes += p.GetPacketBytes().Count();
                _receiveList?.Enqueue(p);
            }

            CMain.BytesReceived += packet_bytes;

            BeginReceive();
        }

        
        private static void BeginSend(List<byte> data) {
            if (_client is not { Connected: true } || data.Count == 0) return;
            
            try {
                _client.Client.BeginSend(data.ToArray(), 0, data.Count, SocketFlags.None, SendData, null);
            }
            catch {
                Disconnect();
            }
        }
        
        
        private static void SendData(IAsyncResult result) {
            try {
                _client?.Client.EndSend(result);
            }
            catch {
                // ignored
            }
        }

        
        public static void Disconnect() {
            if (_client == null) return;

            _client.Close();

            TimeConnected = 0;
            Connected = false;
            _sendList = null;
            _client = null;
            _receiveList = null;
        }

        
        public static void Process() {
            if (_client is not { Connected: true }) {
                if (Connected) {
                    while (_receiveList is { IsEmpty: false }) {
                        if (!_receiveList.TryDequeue(out Packet? p)) continue;
                        
                        if (p is not ServerPackets.Disconnect && p is not ServerPackets.ClientVersion) continue;

                        MirScene.ActiveScene?.ProcessPacket(p);
                        _receiveList = null;
                        return;
                    }

                    MirMessageBox.Show("Lost connection with the server.", true);
                    Disconnect();
                }
                else if (CMain.Time >= RetryTime)
                {
                    RetryTime = CMain.Time + 5000;
                    Connect();
                }
                return;
            }

            if (!Connected && TimeConnected > 0 && CMain.Time > TimeConnected + 5000) {
                Disconnect();
                Connect();
                return;
            }

            while (_receiveList is { IsEmpty: false }) {
                if (!_receiveList.TryDequeue(out Packet? p)) continue;
                MirScene.ActiveScene?.ProcessPacket(p);
            }

            if (CMain.Time > TimeOutTime && _sendList is { IsEmpty: true })
                _sendList.Enqueue(new C.KeepAlive());

            if (_sendList == null || _sendList.IsEmpty) return;

            TimeOutTime = CMain.Time + Settings.TimeOut;

            List<byte> data = [];
            while (!_sendList.IsEmpty) {
                if (_sendList.TryDequeue(out Packet? p)) 
                    data.AddRange(p.GetPacketBytes());
            }

            CMain.BytesSent += data.Count;
            BeginSend(data);
        }
        
        
        public static void Enqueue(Packet p) {
            _sendList?.Enqueue(p);
        }
    }
}