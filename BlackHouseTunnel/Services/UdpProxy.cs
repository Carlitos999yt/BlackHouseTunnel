using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackHouseTunnel.Services
{
    public static class UdpProxy
    {
        private class ClientSession
        {
            public Socket RemoteSocket { get; }
            public IPEndPoint ClientEndPoint { get; }
            public DateTime LastActivity { get; set; }
            public Task RelayTask { get; set; } = Task.CompletedTask;

            public ClientSession(Socket remoteSocket, IPEndPoint clientEndPoint)
            {
                RemoteSocket = remoteSocket;
                ClientEndPoint = clientEndPoint;
                LastActivity = DateTime.UtcNow;
            }
        }

        public const int PROXY_PORT = 55555;
        public const int WARM_PACKETS = 3;
        public const double WARM_INTERVAL_SEC = 0.4;

        public static int ActiveProxyPort => PROXY_PORT;

        private static CancellationTokenSource? _cts;
        private static Task? _proxyTask;
        private static Socket? _localListener;
        private static readonly ConcurrentDictionary<IPEndPoint, ClientSession> _sessions = new ConcurrentDictionary<IPEndPoint, ClientSession>();
        private static readonly object _stateLock = new object();
        private static bool _isRunning = false;

        public static bool IsRunning => _isRunning;

        private static void DisableConnReset(Socket s)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    s.IOControl(-1744830452, new byte[1], null);
                }
            }
            catch
            {
            }
        }

        public static bool StartProxy(string dstHost, int dstPort)
        {
            lock (_stateLock)
            {
                if (_isRunning)
                {
                    StopProxy();
                }
                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;
                try
                {
                    IPAddress[] result;
                    try
                    {
                        Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(dstHost);
                        if (!hostAddressesAsync.Wait(5000))
                        {
                            Console.WriteLine("[proxy] DNS lookup timed out");
                            return false;
                        }
                        result = hostAddressesAsync.Result;
                    }
                    catch
                    {
                        return false;
                    }
                    if (result.Length == 0)
                    {
                        return false;
                    }
                    IPAddress targetIp = result.FirstOrDefault((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork) ?? result[0];
                    _localListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _localListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
                    DisableConnReset(_localListener);
                    _localListener.Bind(new IPEndPoint(IPAddress.Loopback, PROXY_PORT));
                    _isRunning = true;
                    _proxyTask = Task.Run(() => WorkerLoop(targetIp, dstPort, token), token);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[UdpProxy] Proxy start failed for {dstHost}:{dstPort}", ex);
                    CleanupSockets();
                    _isRunning = false;
                    return false;
                }
            }
        }

        private static async Task WorkerLoop(IPAddress dstIp, int dstPort, CancellationToken token)
        {
            IPEndPoint dstEndPoint = new IPEndPoint(dstIp, dstPort);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
            try
            {
                while (!token.IsCancellationRequested && _localListener != null && _isRunning)
                {
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    SocketReceiveFromResult result;
                    try
                    {
                        result = await _localListener.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    if (result.ReceivedBytes <= 0)
                    {
                        continue;
                    }
                    if (result.ReceivedBytes >= 15 && Encoding.UTF8.GetString(buffer, 0, 15) == "BLACKHOUSE_NICK")
                    {
                        RbxmBridgeServer.RegisterClientNickname(Encoding.UTF8.GetString(buffer, 16, result.ReceivedBytes - 16));
                    }
                    else
                    {
                        if (!(result.RemoteEndPoint is IPEndPoint key))
                        {
                            continue;
                        }
                        ClientSession session = _sessions.GetOrAdd(key, delegate(IPEndPoint ep)
                        {
                            Socket socket = new Socket(dstIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
                            {
                                ReceiveTimeout = 2000,
                                SendTimeout = 2000
                            };
                            DisableConnReset(socket);
                            ClientSession newSess = new ClientSession(socket, ep);
                            newSess.RelayTask = Task.Run(() => RelayRemoteToLocal(newSess, token), token);
                            return newSess;
                        });
                        session.LastActivity = DateTime.UtcNow;
                        try
                        {
                            await session.RemoteSocket.SendToAsync(new ArraySegment<byte>(buffer, 0, result.ReceivedBytes), SocketFlags.None, dstEndPoint, token);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                CleanupSockets();
            }
        }

        private static async Task RelayRemoteToLocal(ClientSession session, CancellationToken token)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
            try
            {
                while (!token.IsCancellationRequested && _isRunning && _localListener != null)
                {
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    SocketReceiveFromResult result;
                    try
                    {
                        result = await session.RemoteSocket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    if (result.ReceivedBytes > 0)
                    {
                        session.LastActivity = DateTime.UtcNow;
                        try
                        {
                            await _localListener.SendToAsync(new ArraySegment<byte>(buffer, 0, result.ReceivedBytes), SocketFlags.None, session.ClientEndPoint, token);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                try
                {
                    session.RemoteSocket.Close();
                }
                catch
                {
                }
            }
        }

        public static void StopProxy(bool wait = true)
        {
            lock (_stateLock)
            {
                if (!_isRunning)
                {
                    return;
                }
                _isRunning = false;
                try
                {
                    _cts?.Cancel();
                }
                catch
                {
                }
                CleanupSockets();
                if (wait && _proxyTask != null)
                {
                    try
                    {
                        _proxyTask.Wait(1500);
                    }
                    catch
                    {
                    }
                }
                _proxyTask = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private static void CleanupSockets()
        {
            try
            {
                _localListener?.Close();
            }
            catch
            {
            }
            _localListener = null;
            foreach (KeyValuePair<IPEndPoint, ClientSession> session in _sessions)
            {
                try
                {
                    session.Value.RemoteSocket.Close();
                }
                catch
                {
                }
            }
            _sessions.Clear();
        }

        public static void SendClientNickname(string dstHost, int dstPort, string nickname)
        {
            if (string.IsNullOrWhiteSpace(dstHost) || dstPort <= 0 || string.IsNullOrWhiteSpace(nickname))
            {
                return;
            }
            try
            {
                Task.Run(delegate
                {
                    try
                    {
                        IPAddress[] hostAddresses = Dns.GetHostAddresses(dstHost);
                        if (hostAddresses.Length == 0)
                        {
                            return;
                        }
                        IPAddress targetIp = hostAddresses.FirstOrDefault((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork) ?? hostAddresses[0];
                        IPEndPoint remoteEP = new IPEndPoint(targetIp, dstPort);
                        using Socket socket = new Socket(targetIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                        DisableConnReset(socket);
                        byte[] bytes = Encoding.UTF8.GetBytes("BLACKHOUSE_NICK:" + nickname.Trim());
                        for (int num = 0; num < 5; num++)
                        {
                            socket.SendTo(bytes, remoteEP);
                            Thread.Sleep(50);
                        }
                    }
                    catch
                    {
                    }
                });
            }
            catch
            {
            }
        }

        public static int WarmTunnel(string? dstHost = null, int dstPort = 0, int proxyPort = PROXY_PORT, int packets = 5)
        {
            int num = 0;
            if (!string.IsNullOrEmpty(dstHost) && dstPort > 0)
            {
                try
                {
                    Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(dstHost);
                    if (hostAddressesAsync.Wait(1000) && hostAddressesAsync.Result.Length != 0)
                    {
                        IPEndPoint remoteEP = new IPEndPoint(hostAddressesAsync.Result[0], dstPort);
                        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        socket.SendTimeout = 500;
                        DisableConnReset(socket);
                        byte[] bytes = Encoding.UTF8.GetBytes("BLACKHOUSE_TUNNEL_WARMUP_V1");
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                socket.SendTo(bytes, remoteEP);
                                num++;
                            }
                            catch
                            {
                            }
                            Thread.Sleep(40);
                        }
                    }
                }
                catch
                {
                }
            }
            try
            {
                using Socket socket2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket2.SendTimeout = 500;
                DisableConnReset(socket2);
                IPEndPoint remoteEP2 = new IPEndPoint(IPAddress.Loopback, proxyPort);
                byte[] bytes2 = Encoding.UTF8.GetBytes("BLACKHOUSE_PROXY_WARMUP_V1");
                for (int j = 0; j < packets; j++)
                {
                    if (!_isRunning)
                    {
                        break;
                    }
                    try
                    {
                        socket2.SendTo(bytes2, remoteEP2);
                        num++;
                    }
                    catch
                    {
                        break;
                    }
                    Thread.Sleep(50);
                }
            }
            catch
            {
            }
            return num;
        }
    }
}
