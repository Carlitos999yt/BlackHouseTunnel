using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NepTunnel.Services
{
    // Echo server running on host side to verify incoming UDP probe packets from joiners.
    public class EchoServer
    {
        public static readonly byte[] ECHO_REQ = Encoding.ASCII.GetBytes("NEP_TEST\0");
        public static readonly byte[] ECHO_RESP = Encoding.ASCII.GetBytes("NEP_ECHO\0");

        private Socket? _sock;
        private CancellationTokenSource? _cts;
        private Task? _serverTask;

        public int Port { get; private set; }
        public int EchoedCount { get; private set; }
        public HashSet<string> ClientIps { get; } = new HashSet<string>();
        public bool IsRunning => _sock != null && _cts != null && !_cts.IsCancellationRequested;

        // Starts listening for echo packets on the specified UDP port.
        public bool Start(int port, Action<string, string>? logFn = null)
        {
            Stop();

            Port = port;
            EchoedCount = 0;
            ClientIps.Clear();
            _cts = new CancellationTokenSource();

            try
            {
                _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                if (OperatingSystem.IsWindows())
                {
                    const int SIO_UDP_CONNRESET = -1744830452;
                    try { _sock.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); } catch { }
                }

                _sock.Bind(new IPEndPoint(IPAddress.Any, port));
                _sock.ReceiveTimeout = 500;

                _serverTask = Task.Run(() => RunLoop(_cts.Token), _cts.Token);
                return true;
            }
            catch (SocketException ex)
            {
                logFn?.Invoke($"Could not bind to port {port}.", "err");
                logFn?.Invoke("  Is Roblox Studio already running? Close it and try again.", "err");
                logFn?.Invoke($"  OS Error: {ex.Message}", "dim");
                Stop();
                return false;
            }
            catch (Exception ex)
            {
                logFn?.Invoke($"Unexpected error binding port: {ex.Message}", "err");
                Stop();
                return false;
            }
        }

        // Stops the echo server socket listener loop.
        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            try { _sock?.Close(); } catch { }
            _sock = null;

            if (_serverTask != null)
            {
                try { _serverTask.Wait(1000); } catch { }
            }
            _serverTask = null;
            _cts?.Dispose();
            _cts = null;
        }

        // Internal worker loop handling incoming echo request packets and responding.
        private async Task RunLoop(CancellationToken token)
        {
            byte[] buffer = new byte[512];
            while (!token.IsCancellationRequested && _sock != null)
            {
                try
                {
                    EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                    var result = await _sock.ReceiveFromAsync(buffer, SocketFlags.None, remoteEp, token);

                    if (result.ReceivedBytes >= ECHO_REQ.Length)
                    {
                        bool match = true;
                        for (int i = 0; i < ECHO_REQ.Length; i++)
                        {
                            if (buffer[i] != ECHO_REQ[i])
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match && result.RemoteEndPoint is IPEndPoint senderEp)
                        {
                            await _sock.SendToAsync(ECHO_RESP, SocketFlags.None, senderEp, token);
                            EchoedCount++;
                            string ipStr = senderEp.Address.ToString();
                            lock (ClientIps)
                            {
                                ClientIps.Add(ipStr);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Ignore transient network errors during shutdown
                }
            }
        }
    }

    // Echo client used by joiners to send test probe packets to host.
    public static class EchoClient
    {
        // Sends test probe packets to target host and measures round-trip latency.
        public static async Task RunEchoTestAsync(Action<string, string> logFn, string host, int port, int probeCount = 5)
        {
            logFn($"=== RUNNING ECHO TEST TO {host}:{port} ===", "info");
            logFn($"Sending {probeCount} test packets directly to tunnel...", "dim");

            IPAddress targetIp;
            try
            {
                IPAddress[] addrs = await Dns.GetHostAddressesAsync(host);
                if (addrs.Length == 0)
                {
                    logFn($"DNS error: Could not resolve {host}", "err");
                    return;
                }
                targetIp = addrs[0];
            }
            catch (Exception ex)
            {
                logFn($"DNS error resolving {host}: {ex.Message}", "err");
                return;
            }

            using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.SendTimeout = 2000;
            sock.ReceiveTimeout = 2000;
            if (OperatingSystem.IsWindows())
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                try { sock.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); } catch { }
            }

            var targetEp = new IPEndPoint(targetIp, port);
            int received = 0;
            var rtts = new List<double>();
            byte[] respBuf = new byte[512];

            for (int i = 1; i <= probeCount; i++)
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    await sock.SendToAsync(EchoServer.ECHO_REQ, SocketFlags.None, targetEp);

                    using var cts = new CancellationTokenSource(2000);
                    EndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);

                    try
                    {
                        var result = await sock.ReceiveFromAsync(respBuf, SocketFlags.None, senderEp, cts.Token);
                        sw.Stop();

                        if (result.ReceivedBytes >= EchoServer.ECHO_RESP.Length)
                        {
                            bool match = true;
                            for (int j = 0; j < EchoServer.ECHO_RESP.Length; j++)
                            {
                                if (respBuf[j] != EchoServer.ECHO_RESP[j])
                                {
                                    match = false;
                                    break;
                                }
                            }

                            if (match)
                            {
                                received++;
                                double ms = sw.Elapsed.TotalMilliseconds;
                                rtts.Add(ms);
                                logFn($"  Probe {i}/{probeCount}: ECHO OK! rtt={ms:F1}ms", "ok");
                            }
                            else
                            {
                                logFn($"  Probe {i}/{probeCount}: Received unknown data", "warn");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        logFn($"  Probe {i}/{probeCount}: TIMEOUT (no response in 2.0s)", "err");
                    }
                }
                catch (Exception ex)
                {
                    logFn($"  Probe {i}/{probeCount}: Error sending - {ex.Message}", "err");
                }

                if (i < probeCount)
                {
                    await Task.Delay(400);
                }
            }

            logFn("---------------------------------------", "dim");
            if (received > 0)
            {
                double avgRtt = rtts.Count > 0 ? rtts.Average() : 0;
                logFn($"ECHO SUCCESSFUL! Received {received}/{probeCount} responses (avg {avgRtt:F1}ms)", "ok");
                logFn("Your tunnel is active and accepting incoming UDP traffic.", "ok");
            }
            else
            {
                logFn($"ECHO FAILED: Received 0/{probeCount} responses.", "err");
                logFn("Possible causes:", "warn");
                logFn("  1. Host has not started session or Echo Server yet", "warn");
                logFn("  2. Host firewall is blocking UDP port", "warn");
                logFn("  3. Tunnel address or port is wrong", "warn");
            }
        }
    }
}
