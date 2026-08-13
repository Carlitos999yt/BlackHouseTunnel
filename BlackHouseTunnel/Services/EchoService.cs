using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackHouseTunnel.Services
{
    public class EchoServer
    {
        public static readonly byte[] ECHO_REQ = Encoding.ASCII.GetBytes("BLACKHOUSE_TEST\0");
        public static readonly byte[] ECHO_RESP = Encoding.ASCII.GetBytes("BLACKHOUSE_ECHO\0");

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
                logFn?.Invoke($"No se pudo vincular al puerto {port}.", "err");
                logFn?.Invoke($"  Error SO: {ex.Message}", "dim");
                Stop();
                return false;
            }
            catch (Exception ex)
            {
                logFn?.Invoke($"Error al vincular el puerto: {ex.Message}", "err");
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
                            await _sock.SendToAsync(new ArraySegment<byte>(ECHO_RESP), SocketFlags.None, result.RemoteEndPoint, token);
                            EchoedCount++;
                            if (result.RemoteEndPoint is IPEndPoint ep)
                            {
                                lock (ClientIps)
                                {
                                    ClientIps.Add(ep.Address.ToString());
                                }
                            }
                        }
                    }
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
                catch
                {
                }
            }
        }
    }

    public class EchoClient
    {
        public static async Task RunEchoTestAsync(string host, int port, Action<string, string> logFn, int count = 5, int timeoutMs = 2000)
        {
            logFn("--- Echo Test (Prueba de Latencia UDP) ---", "info");
            logFn($"Enviando {count} paquetes a {host}:{port}...", "warn");

            IPAddress targetIp;
            try
            {
                var addrs = await Dns.GetHostAddressesAsync(host);
                if (addrs.Length == 0)
                {
                    logFn($"✗ Error DNS: No se encontró IP para {host}", "err");
                    return;
                }
                targetIp = addrs[0];
            }
            catch (Exception ex)
            {
                logFn($"✗ Error resolución DNS: {ex.Message}", "err");
                return;
            }

            using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (OperatingSystem.IsWindows())
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                try { sock.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); } catch { }
            }

            IPEndPoint remoteEp = new IPEndPoint(targetIp, port);
            int received = 0;
            List<double> rtts = new List<double>();
            byte[] respBuf = new byte[512];

            for (int i = 1; i <= count; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await sock.SendToAsync(new ArraySegment<byte>(EchoServer.ECHO_REQ), SocketFlags.None, remoteEp);

                    using var cts = new CancellationTokenSource(timeoutMs);
                    EndPoint fromEp = new IPEndPoint(IPAddress.Any, 0);
                    var result = await sock.ReceiveFromAsync(respBuf, SocketFlags.None, fromEp, cts.Token);

                    stopwatch.Stop();
                    double ms = stopwatch.Elapsed.TotalMilliseconds;

                    if (result.ReceivedBytes >= EchoServer.ECHO_RESP.Length)
                    {
                        received++;
                        rtts.Add(ms);
                        logFn($"  Paquete #{i}: Éxito ({ms:F1} ms)", "ok");
                    }
                    else
                    {
                        logFn($"  Paquete #{i}: Respuesta inválida", "err");
                    }
                }
                catch (OperationCanceledException)
                {
                    stopwatch.Stop();
                    logFn($"  Paquete #{i}: Tiempo de espera agotado ({timeoutMs}ms)", "err");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    logFn($"  Paquete #{i}: Error ({ex.Message})", "err");
                }

                await Task.Delay(200);
            }

            double lossRate = (double)(count - received) / count * 100;
            logFn($"Resultados: {received}/{count} recibidos (Pérdida: {lossRate:F0}%)", received > 0 ? "ok" : "err");
            if (rtts.Count > 0)
            {
                double min = rtts[0], max = rtts[0], sum = 0;
                foreach (var r in rtts)
                {
                    if (r < min) min = r;
                    if (r > max) max = r;
                    sum += r;
                }
                double avg = sum / rtts.Count;
                logFn($"  Latencia RTT: Mín = {min:F1}ms, Máx = {max:F1}ms, Prom = {avg:F1}ms", "ok");
            }
        }
    }
}
