using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NepTunnel.Services;

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

	public bool IsRunning
	{
		get
		{
			if (_sock != null && _cts != null)
			{
				return !_cts.IsCancellationRequested;
			}
			return false;
		}
	}

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
			_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			if (OperatingSystem.IsWindows())
			{
				try
				{
					_sock.IOControl(-1744830452, new byte[1], null);
				}
				catch
				{
				}
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
			logFn?.Invoke("  OS Error: " + ex.Message, "dim");
			Stop();
			return false;
		}
		catch (Exception ex2)
		{
			logFn?.Invoke("Unexpected error binding port: " + ex2.Message, "err");
			Stop();
			return false;
		}
	}

	public void Stop()
	{
		try
		{
			_cts?.Cancel();
		}
		catch
		{
		}
		try
		{
			_sock?.Close();
		}
		catch
		{
		}
		_sock = null;
		if (_serverTask != null)
		{
			try
			{
				_serverTask.Wait(1000);
			}
			catch
			{
			}
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
				EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
				SocketReceiveFromResult socketReceiveFromResult = await _sock.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, token);
				if (socketReceiveFromResult.ReceivedBytes < ECHO_REQ.Length)
				{
					continue;
				}
				bool flag = true;
				for (int i = 0; i < ECHO_REQ.Length; i++)
				{
					if (buffer[i] != ECHO_REQ[i])
					{
						flag = false;
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
				EndPoint remoteEndPoint2 = socketReceiveFromResult.RemoteEndPoint;
				if (remoteEndPoint2 is IPEndPoint senderEp)
				{
					await _sock.SendToAsync(ECHO_RESP, SocketFlags.None, senderEp, token);
					EchoedCount++;
					string item = senderEp.Address.ToString();
					lock (ClientIps)
					{
						ClientIps.Add(item);
					}
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch
			{
			}
		}
	}
}
