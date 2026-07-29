using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NepTunnel.Services
{
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
                logFn?.Invoke($"✗ Could not bind to port {port}.", "err");
                logFn?.Invoke("  Is Roblox Studio already running? Close it and try again.", "err");
                logFn?.Invoke($"  OS Error: {ex.Message}", "dim");
                Stop();
                return false;
            }
            catch (Exception ex)
            {
                logFn?.Invoke($"✗ Unexpected error binding port: {ex.Message}", "err");
                Stop();
                return false;
            }
        }

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

                        if (match)
                        {
                            int nonceLen = result.ReceivedBytes - ECHO_REQ.Length;
                            byte[] resp = new byte[ECHO_RESP.Length + nonceLen];
                            Buffer.BlockCopy(ECHO_RESP, 0, resp, 0, ECHO_RESP.Length);
                            Buffer.BlockCopy(buffer, ECHO_REQ.Length, resp, ECHO_RESP.Length, nonceLen);

                            await _sock.SendToAsync(resp, SocketFlags.None, result.RemoteEndPoint, token);
                            EchoedCount++;
                            if (result.RemoteEndPoint is IPEndPoint ipEp)
                            {
                                lock (ClientIps)
                                {
                                    ClientIps.Add(ipEp.Address.ToString());
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (SocketException) { }
                catch (ObjectDisposedException) { break; }
                catch { }
            }
        }
    }

    public static class EchoClient
    {
        public static async Task RunEchoTestAsync(Action<string, string> logFn, string tunnelHost, int tunnelPort, int maxSuccesses = 3, double timeoutSec = 10.0)
        {
            logFn("─── Echo Round-Trip Test ───", "info");
            logFn($"Target: {tunnelHost}:{tunnelPort}", "dim");
            logFn("Sending probes directly to tunnel (bypassing local proxy)...", "warn");
            logFn("Note: Tunnels can take a few seconds to \"wake up\". Please wait...", "dim");

            using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.ReceiveTimeout = 500;
            if (OperatingSystem.IsWindows())
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                try { sock.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); } catch { }
            }

            IPAddress[] addrs;
            try
            {
                addrs = await Dns.GetHostAddressesAsync(tunnelHost);
                if (addrs.Length == 0)
                {
                    logFn($"✗ Send error: Could not resolve hostname {tunnelHost}", "err");
                    return;
                }
            }
            catch (Exception ex)
            {
                logFn($"✗ Send error: {ex.Message}", "err");
                return;
            }

            var targetEp = new IPEndPoint(addrs[0], tunnelPort);
            var sent = new Dictionary<string, long>();
            int received = 0;
            var rtts = new List<double>();

            long startTime = Stopwatch.GetTimestamp();
            double probeIntervalSec = 0.4;
            double nextProbeTimeSec = 0;
            bool icmpReject = false;
            var rng = new Random();

            while (GetElapsedSeconds(startTime) < timeoutSec && received < maxSuccesses)
            {
                double nowSec = GetElapsedSeconds(startTime);
                if (nowSec >= nextProbeTimeSec)
                {
                    byte[] nonce = new byte[8];
                    rng.NextBytes(nonce);
                    string nonceKey = Convert.ToBase64String(nonce);

                    byte[] probe = new byte[EchoServer.ECHO_REQ.Length + nonce.Length];
                    Buffer.BlockCopy(EchoServer.ECHO_REQ, 0, probe, 0, EchoServer.ECHO_REQ.Length);
                    Buffer.BlockCopy(nonce, 0, probe, EchoServer.ECHO_REQ.Length, nonce.Length);

                    try
                    {
                        await sock.SendToAsync(probe, SocketFlags.None, targetEp);
                        sent[nonceKey] = Stopwatch.GetTimestamp();
                        nextProbeTimeSec = nowSec + probeIntervalSec;
                    }
                    catch (Exception ex)
                    {
                        logFn($"✗ Send error: {ex.Message}", "err");
                        break;
                    }
                }

                try
                {
                    byte[] rcvBuffer = new byte[512];
                    EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

                    using var cts = new CancellationTokenSource(100);
                    var res = await sock.ReceiveFromAsync(rcvBuffer, SocketFlags.None, remoteEp, cts.Token);

                    if (res.ReceivedBytes >= EchoServer.ECHO_RESP.Length)
                    {
                        bool isEcho = true;
                        for (int i = 0; i < EchoServer.ECHO_RESP.Length; i++)
                        {
                            if (rcvBuffer[i] != EchoServer.ECHO_RESP[i])
                            {
                                isEcho = false;
                                break;
                            }
                        }

                        if (isEcho)
                        {
                            int nonceLen = res.ReceivedBytes - EchoServer.ECHO_RESP.Length;
                            byte[] nonceReceived = new byte[nonceLen];
                            Buffer.BlockCopy(rcvBuffer, EchoServer.ECHO_RESP.Length, nonceReceived, 0, nonceLen);
                            string key = Convert.ToBase64String(nonceReceived);

                            if (sent.TryGetValue(key, out long sendTicks))
                            {
                                double rttMs = (Stopwatch.GetTimestamp() - sendTicks) * 1000.0 / Stopwatch.Frequency;
                                rtts.Add(rttMs);
                                received++;

                                if (received == 1)
                                {
                                    logFn($"✓ First echo received! ({rttMs:F0} ms) Tunnel is waking up...", "ok");
                                }
                                else if (received == 2)
                                {
                                    logFn($"✓ Second echo received. Connection stabilizing...", "ok");
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset || ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    logFn("✗ ICMP Port Unreachable. Tunnel endpoint is actively rejecting.", "err");
                    icmpReject = true;
                    break;
                }
                catch { }
            }

            logFn("───────────────────────", "dim");
            if (received >= maxSuccesses)
            {
                double avg = rtts.Average();
                double mn = rtts.Min();
                double mx = rtts.Max();
                logFn($"✓ SUCCESS: {received} echoes received. Tunnel is LIVE and stable.", "ok");
                logFn($"  RTT: avg {avg:F0} ms | min {mn:F0} ms | max {mx:F0} ms", "ok");
                logFn("  You can now safely start your session.", "info");
            }
            else if (received > 0)
            {
                double avg = rtts.Average();
                logFn($"△ PARTIAL: {received}/{maxSuccesses} echoes. Tunnel is unstable.", "warn");
                logFn($"  RTT: avg {avg:F0} ms", "warn");
                logFn("  You might experience lag or disconnects in-game.", "warn");
            }
            else if (icmpReject)
            {
                logFn("✗ FAILED: Tunnel port is closed or host firewall is blocking it.", "err");
            }
            else
            {
                logFn("✗ FAILED: No echoes received within timeout.", "err");
                logFn("  Possible causes:", "dim");
                logFn("  1. Host has not started the Echo Server yet.", "dim");
                logFn("  2. Tunnel is down or misconfigured.", "dim");
                logFn("  3. Host firewall is blocking the tunnel agent.", "dim");
            }
        }

        private static double GetElapsedSeconds(long startTimestamp)
        {
            return (Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency;
        }
    }
}
