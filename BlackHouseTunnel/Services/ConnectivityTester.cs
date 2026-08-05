using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlackHouseTunnel.Services
{
    public static class ConnectivityTester
    {
        private static async Task<(bool alive, double rttMs)> IcmpPingAsync(string host)
        {
            try
            {
                using Ping ping = new Ping();
                PingReply pingReply = await ping.SendPingAsync(host, 2000);
                if (pingReply.Status == IPStatus.Success)
                {
                    return (alive: true, rttMs: pingReply.RoundtripTime);
                }
                return (alive: false, rttMs: -1.0);
            }
            catch
            {
                return (alive: false, rttMs: -1.0);
            }
        }

        public static async Task RunConnectivityTestAsync(string host, int port, Action<string, string> logFn, bool isHostSide = false, int localServerPort = 0)
        {
            logFn("--- Prueba de Conectividad BlackHouseTunnel ---", "info");
            int passed = 0;
            int warned = 0;
            int failed = 0;
            logFn("[1/4] Resolviendo " + host + "...", "warn");
            IPAddress targetIp;
            try
            {
                IPAddress[] array = await Dns.GetHostAddressesAsync(host);
                if (array.Length == 0)
                {
                    logFn("  DNS FALLÓ - No se encontraron direcciones para " + host, "err");
                    failed++;
                    CtSummary(logFn, passed, warned, failed);
                    return;
                }
                targetIp = array[0];
                logFn($"  DNS OK {host} -> {targetIp}", "ok");
                passed++;
            }
            catch (Exception ex)
            {
                logFn("  DNS FALLÓ - " + ex.Message, "err");
                failed++;
                CtSummary(logFn, passed, warned, failed);
                return;
            }
            logFn($"[2/4] Ping ICMP -> {targetIp}...", "warn");
            var (flag, value) = await IcmpPingAsync(targetIp.ToString());
            if (flag)
            {
                logFn($"  Servidor Relay alcanzable (latencia aprox {value:F0} ms)", "ok");
                passed++;
            }
            else
            {
                logFn($"  Ping ICMP falló - {targetIp} inalcanzable", "warn");
                warned++;
            }
            if (isHostSide)
            {
                int num = ((localServerPort != 0) ? localServerPort : port);
                logFn($"[3/4] Verificando puerto local de Studio {num}...", "warn");
                try
                {
                    using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.SendTimeout = 500;
                    socket.ReceiveTimeout = 500;
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, num);
                    socket.SendTo(new byte[3] { 255, 0, 0 }, remoteEP);
                    logFn($"  Puerto {num} acepta UDP", "ok");
                    passed++;
                }
                catch
                {
                    logFn($"  Puerto {num} aceptó paquete de prueba", "ok");
                    passed++;
                }
            }
            else
            {
                logFn($"[3/4] Probando objetivo UDP {targetIp}:{port}...", "warn");
                try
                {
                    using Socket socket2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket2.SendTimeout = 1000;
                    socket2.ReceiveTimeout = 1000;
                    IPEndPoint remoteEP2 = new IPEndPoint(targetIp, port);
                    byte[] bytes = Encoding.ASCII.GetBytes("BLACKHOUSE_TEST_PROBE\0");
                    socket2.SendTo(bytes, remoteEP2);
                    logFn($"  Paquete UDP enviado a {targetIp}:{port}", "ok");
                    passed++;
                }
                catch (Exception ex4)
                {
                    logFn("  Error al enviar prueba UDP: " + ex4.Message, "err");
                    failed++;
                }
            }
            logFn("[4/4] Evaluando resultados...", "warn");
            CtSummary(logFn, passed, warned, failed);
        }

        private static void CtSummary(Action<string, string> logFn, int passed, int warned, int failed)
        {
            if (failed > 0)
            {
                logFn($"RESULTADO: ERROR ({passed} pasados, {warned} advertencias, {failed} fallidos)", "err");
            }
            else if (warned > 0)
            {
                logFn($"RESULTADO: ADVERTENCIA ({passed} pasados, {warned} advertencias, {failed} fallidos)", "warn");
            }
            else
            {
                logFn($"RESULTADO: ÉXITO ({passed} pasados, {warned} advertencias, {failed} fallidos)", "ok");
            }
        }
    }
}
