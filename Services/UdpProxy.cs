using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NepTunnel.Services
{
    public static class UdpProxy
    {
        public const int PROXY_PORT = 55555;
        public const int WARM_PACKETS = 3;
        public const double WARM_INTERVAL_SEC = 0.40;

        private static CancellationTokenSource? _cts;
        private static Task? _proxyTask;
        private static Socket? _localListener;
        private static readonly ConcurrentDictionary<IPEndPoint, ClientSession> _sessions = new();
        private static readonly object _stateLock = new();
        private static bool _isRunning = false;

        public static bool IsRunning => _isRunning;

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

        private static void DisableConnReset(Socket s)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    const int SIO_UDP_CONNRESET = -1744830452; // 0x9800000C
                    s.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
                }
            }
            catch { }
        }

        public static bool StartProxy(string dstHost, int dstPort)
        {
            lock (_stateLock)
            {
                if (_isRunning)
                {
                    StopProxy(wait: true);
                }

                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                try
                {
                    // Fast pre-flight DNS lookup
                    IPAddress[] addresses;
                    try
                    {
                        var dnsTask = Dns.GetHostAddressesAsync(dstHost);
                        if (!dnsTask.Wait(1500))
                        {
                            Console.WriteLine("[proxy] DNS lookup timed out");
                            return false;
                        }
                        addresses = dnsTask.Result;
                    }
                    catch
                    {
                        return false;
                    }

                    if (addresses.Length == 0) return false;
                    IPAddress targetIp = addresses[0];

                    _localListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _localListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    DisableConnReset(_localListener);
                    _localListener.Bind(new IPEndPoint(IPAddress.Loopback, PROXY_PORT));

                    _isRunning = true;

                    _proxyTask = Task.Run(() => WorkerLoop(targetIp, dstPort, token), token);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[proxy start error] {ex.Message}");
                    CleanupSockets();
                    _isRunning = false;
                    return false;
                }
            }
        }

        private static async Task WorkerLoop(IPAddress dstIp, int dstPort, CancellationToken token)
        {
            var dstEndPoint = new IPEndPoint(dstIp, dstPort);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);

            try
            {
                while (!token.IsCancellationRequested && _localListener != null && _isRunning)
                {
                    EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                    SocketReceiveFromResult result;
                    try
                    {
                        result = await _localListener.ReceiveFromAsync(buffer, SocketFlags.None, remoteEp, token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (SocketException) { continue; }
                    catch (ObjectDisposedException) { break; }

                    if (result.ReceivedBytes <= 0) continue;

                    if (result.RemoteEndPoint is IPEndPoint clientEp)
                    {
                        var session = _sessions.GetOrAdd(clientEp, ep =>
                        {
                            var rSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            rSock.ReceiveTimeout = 2000;
                            rSock.SendTimeout = 2000;
                            DisableConnReset(rSock);

                            var newSess = new ClientSession(rSock, ep);
                            newSess.RelayTask = Task.Run(() => RelayRemoteToLocal(newSess, token), token);
                            return newSess;
                        });

                        session.LastActivity = DateTime.UtcNow;

                        try
                        {
                            await session.RemoteSocket.SendToAsync(
                                new ArraySegment<byte>(buffer, 0, result.ReceivedBytes),
                                SocketFlags.None,
                                dstEndPoint,
                                token);
                        }
                        catch { }
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
                    EndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);
                    SocketReceiveFromResult result;
                    try
                    {
                        result = await session.RemoteSocket.ReceiveFromAsync(buffer, SocketFlags.None, senderEp, token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (SocketException) { continue; }
                    catch (ObjectDisposedException) { break; }

                    if (result.ReceivedBytes <= 0) continue;

                    session.LastActivity = DateTime.UtcNow;

                    try
                    {
                        await _localListener.SendToAsync(
                            new ArraySegment<byte>(buffer, 0, result.ReceivedBytes),
                            SocketFlags.None,
                            session.ClientEndPoint,
                            token);
                    }
                    catch { }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                try { session.RemoteSocket.Close(); } catch { }
            }
        }

        public static void StopProxy(bool wait = true)
        {
            lock (_stateLock)
            {
                if (!_isRunning) return;
                _isRunning = false;

                try
                {
                    _cts?.Cancel();
                }
                catch { }

                CleanupSockets();

                if (wait && _proxyTask != null)
                {
                    try
                    {
                        _proxyTask.Wait(1500);
                    }
                    catch { }
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
            catch { }
            _localListener = null;

            foreach (var kvp in _sessions)
            {
                try
                {
                    kvp.Value.RemoteSocket.Close();
                }
                catch { }
            }
            _sessions.Clear();
        }

        public static int WarmTunnel(int proxyPort = PROXY_PORT, int packets = WARM_PACKETS, double intervalSec = WARM_INTERVAL_SEC)
        {
            int sent = 0;
            try
            {
                using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.SendTimeout = 1000;
                var ep = new IPEndPoint(IPAddress.Loopback, proxyPort);
                var rng = new Random();

                for (int i = 0; i < packets; i++)
                {
                    if (!_isRunning) break;

                    byte[] payload = new byte[2 + rng.Next(6, 20)];
                    payload[0] = 0xFF;
                    payload[1] = 0x00;
                    rng.NextBytes(new Span<byte>(payload, 2, payload.Length - 2));

                    try
                    {
                        sock.SendTo(payload, ep);
                        sent++;
                    }
                    catch { break; }

                    Thread.Sleep((int)(intervalSec * 1000));
                }
            }
            catch { }
            return sent;
        }
    }
}
