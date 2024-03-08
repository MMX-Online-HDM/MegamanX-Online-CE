using Lidgren.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace MMXOnline;

public class ServerClient {
	public NetClient client;
	public bool isHost;
	public ServerPlayer serverPlayer;
	Stopwatch packetLossStopwatch = new Stopwatch();
	Stopwatch gameLoopStopwatch = new Stopwatch();
	public long packetsReceived;
	public long? serverId = null;

	private ServerClient(NetClient client, bool isHost) {
		this.client = client;
		this.isHost = isHost;
	}

	public static NetClient GetPingClient(string serverIp) {
		NetPeerConfiguration config = new NetPeerConfiguration("matchmaking");
		config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
		config.AutoFlushSendQueue = true;

		// TODO: Add a flag for this.
		//config.SimulatedMinimumLatency = Global.simulatedLatency;
		//config.SimulatedLoss = Global.simulatedPacketLoss;
		//config.SimulatedDuplicatesChance = Global.simulatedDuplicates;

		var client = new NetClient(config);
		client.Start();
		NetOutgoingMessage hail = client.CreateMessage("a");
		try {
			client.Connect(serverIp, Global.basePort, hail);
		} catch {}

		return client;
	}

	public static ServerClient Create(
		string serverIp, string serverName, int serverPort,
		ServerPlayer inputServerPlayer, out JoinServerResponse joinServerResponse,
		out string error
	) {
		error = null;
		NetPeerConfiguration config = new NetPeerConfiguration(serverName);
		config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
		config.AutoFlushSendQueue = false;
		config.ConnectionTimeout = Server.connectionTimeoutSeconds;
		config.Port = Global.clientPort;
		/*
		#if DEBUG
		config.SimulatedMinimumLatency = Global.simulatedLatency;
		config.SimulatedLoss = Global.simulatedPacketLoss;
		config.SimulatedDuplicatesChance = Global.simulatedDuplicates;
		#endif
		*/
		var client = new NetClient(config);
		client.Start();
		NetOutgoingMessage hail = client.CreateMessage(JsonConvert.SerializeObject(inputServerPlayer));
		client.Connect(serverIp, serverPort, hail);

		var serverClient = new ServerClient(client, inputServerPlayer.isHost);

		int count = 0;
		while (count < 20) {
			serverClient.getMessages(out var messages, false);
			foreach (var message in messages) {
				if (message.StartsWith("joinservertargetedresponse:")) {
					joinServerResponse = (
						JsonConvert.DeserializeObject<JoinServerResponse>(
							message.RemovePrefix("joinservertargetedresponse:")
						)
					);
					serverClient.serverPlayer = joinServerResponse.getLastPlayer();
					return serverClient;
				} else if (message.StartsWith("hostdisconnect:")) {
					var reason = message.Split(':')[1];
					error = "Could not join: " + reason;
					joinServerResponse = null;
					serverClient.disconnect("Client couldn't get response");
					return null;
				}
			}
			count++;
			Thread.Sleep(100);
		}

		error = "Failed to get connect response from relay server.";
		joinServerResponse = null;
		serverClient.disconnect("client couldn't get response");
		return null;
	}

	public static ServerClient CreateHolePunchAlt(
		NetClient client, long serverId, IPEndPoint serverIP, ServerPlayer inputServerPlayer,
		out JoinServerResponse joinServerResponse, out string log
	) {
		// Enable autoflush temporally.
		client.Configuration.AutoFlushSendQueue = true;
		client.FlushSendQueue();
		log = null;
		// UDP Hole Punching happens here.
		NetOutgoingMessage regMsg = client.CreateMessage();
		regMsg.Write((byte)MasterServerMsg.ConnectPeers);
		regMsg.Write(serverId);
		regMsg.Write(new IPEndPoint(NetUtility.GetMyAddress(out _), client.Port));
		IPEndPoint masterServerLocation = NetUtility.Resolve(MasterServerData.serverIp, MasterServerData.serverPort);
		client.SendUnconnectedMessage(regMsg, masterServerLocation);
		client.FlushSendQueue();
		log += "Sent HolePunch message..."; 
		// Wait for hole punching to happen.
		int count = 0;
		bool connected = false;
		NetIncomingMessage msg;
		Stopwatch clock = Stopwatch.StartNew();
		while (clock.Elapsed.Seconds < 4 && !connected) {
			while ((msg = client.ReadMessage()) != null && !connected) {
				if (msg.MessageType == NetIncomingMessageType.NatIntroductionSuccess) {
					connected = true;
					client.Connect(serverIP);
					log += "Got Connection MSG!";
					goto exitLoop;
				} else {
					log += "Got other message.\n";
					log += "MSG type:" + msg.MessageType.ToString() + "\n";
				}
			}
		}
		exitLoop:
		clock.Reset();
		// Do this if hole punch fails.
		if (!connected) {
			log += "Failed to holepunch, resorting to direct connection anyway.";
			log += "Conections active: " + client.Connections.Count;
			clock.Start();
			client.Connect(serverIP);
			client.FlushSendQueue();
			while (clock.Elapsed.Seconds < 4 && !connected) {
				if (client.ConnectionStatus == NetConnectionStatus.Connected) {
					connected = true;
				}
			}
		}
		clock.Reset();
		clock.Stop();
		// Ok. We failed 2 times so we give up.
		if (!connected) {
			log += "Failed to connect.";
			joinServerResponse = null;
			client.Configuration.AutoFlushSendQueue = false;
			return null;
		}
		log = null;
		client.FlushSendQueue();
		Thread.Sleep(100);
		// If it works, continue.
		log += "Starting Serverclient.";
		log += "Conections active: " + client.Connections.Count;
		var serverClient = new ServerClient(client, inputServerPlayer.isHost);
		serverClient.serverId = serverId;
		// Now try to connect to get server connect response after conection.
		count = 0;
		while (count < 20) {
			serverClient.getMessages(out var messages, false);
			foreach (var message in messages) {
				if (message.StartsWith("joinservertargetedresponse:")) {
					log += "Got connection response.";
					joinServerResponse = (
						JsonConvert.DeserializeObject<JoinServerResponse>(
							message.RemovePrefix("joinservertargetedresponse:")
						)
					);
					serverClient.serverPlayer = joinServerResponse.getLastPlayer();
					client.Configuration.AutoFlushSendQueue = false;
					return serverClient;
				} else if (message.StartsWith("hostdisconnect:")) {
					var reason = message.Split(':')[1];
					log = "Could not join: " + reason;
					joinServerResponse = null;
					serverClient.disconnect("Client couldn't get response");
					client.Configuration.AutoFlushSendQueue = false;
					return null;
				} else if (message.StartsWith("joinserverresponse:")) {
					log += "Got general response.";
				} else {
					log += "Message: " + message;
				}
			}
			count++;
			Thread.Sleep(100);
		}
		log += "Failed to get connect response from P2P server.";
		joinServerResponse = null;
		serverClient.disconnect("Client couldn't get response");
		client.Configuration.AutoFlushSendQueue = false;
		return null;
	}

	public static ServerClient CreateDirect(
		string serverIp, int port, ServerPlayer inputServerPlayer,
		out JoinServerResponse joinServerResponse, out string error
	) {
		error = null;
		NetPeerConfiguration config = new NetPeerConfiguration("XOD-P2P");
		config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
		config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
		config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
		config.AutoFlushSendQueue = false;
		config.ConnectionTimeout = Server.connectionTimeoutSeconds;
		// Create client.
		var client = new NetClient(config);
		client.Start();
		NetOutgoingMessage hail = client.CreateMessage(JsonConvert.SerializeObject(inputServerPlayer));
		client.Connect(serverIp, port, hail);
		// Wait for connection.
		Thread.Sleep(100);
		Console.WriteLine("Starting Serverclient.");
		int count = 0;
		while (count < 20 && client.ConnectionsCount == 0) {
			Thread.Sleep(100);
			client.FlushSendQueue();
		}
		if (client.ConnectionsCount == 0) {
			error = "Failed initial connection.";
			joinServerResponse = null;
			return null;
		}
		var serverClient = new ServerClient(client, inputServerPlayer.isHost);
		// Now try to connect to get server connect response after conection.
		count = 0;
		while (count < 20) {
			serverClient.getMessages(out var messages, false);
			foreach (var message in messages) {
				if (message.StartsWith("joinservertargetedresponse:")) {
					Console.WriteLine("Got connection response.");
					joinServerResponse = (
						JsonConvert.DeserializeObject<JoinServerResponse>(
							message.RemovePrefix("joinservertargetedresponse:")
						)
					);
					serverClient.serverPlayer = joinServerResponse.getLastPlayer();
					return serverClient;
				} else if (message.StartsWith("hostdisconnect:")) {
					var reason = message.Split(':')[1];
					error = "Could not join: " + reason;
					joinServerResponse = null;
					serverClient.disconnect("Client couldn't get response");
					return null;
				} else if (message.StartsWith("joinserverresponse:")) {
					Console.WriteLine("Got general response.");
				} else {
					Console.WriteLine("Message: " + message);
				}
			}
			count++;
			Thread.Sleep(100);
		}

		error = "Failed to get connect response from server.";
		joinServerResponse = null;
		serverClient.disconnect("Client couldn't get response");
		return null;
	}

	/*
	public void broadcast(string message)
	{
		send("broadcast:" + message);
	}

	public void sendToHost(string message)
	{
		send("host:" + message);
	}

	public void ping()
	{
		send("ping");
	}

	private void send(string message)
	{
		NetOutgoingMessage om = client.CreateMessage(message);
		client.SendMessage(om, NetDeliveryMethod.Unreliable);
		client.FlushSendQueue();
	}
	*/

	float gameLoopLagTime;
	public bool isLagging() {
		//Global.debugString1 = packetLossStopwatch.ElapsedMilliseconds.ToString();
		if (packetLossStopwatch.ElapsedMilliseconds > 1500 || gameLoopLagTime > 0) {
			return true;
		}
		return false;
	}

	public void disconnect(string disconnectMessage) {
		client.Disconnect(disconnectMessage);
		flush();
	}

	public void flush() {
		client.FlushSendQueue();
	}

	public void rpc(RPC rpcTemplate, params byte[] arguments) {
		int rpcIndex = RPC.templates.IndexOf(rpcTemplate);
		if (rpcIndex == -1) {
			throw new Exception("RPC index not found!");
		}
		byte rpcIndexByte = (byte)rpcIndex;
		NetOutgoingMessage om = client.CreateMessage();
		om.Write(rpcIndexByte);
		om.Write((ushort)arguments.Length);
		om.Write(arguments);
		client.SendMessage(om, rpcTemplate.netDeliveryMethod);
	}

	public void rpc(RPC rpcTemplate, string message) {
		int rpcIndex = RPC.templates.IndexOf(rpcTemplate);
		if (rpcIndex == -1) {
			throw new Exception("RPC index not found!");
		}
		byte rpcIndexByte = (byte)rpcIndex;
		NetOutgoingMessage om = client.CreateMessage();
		om.Write(rpcIndexByte);
		om.Write(message);
		client.SendMessage(om, rpcTemplate.netDeliveryMethod);
	}

	public void getMessages(out List<string> stringMessages, bool invokeRpcs) {
		if (gameLoopStopwatch.ElapsedMilliseconds > 250 && Global.level?.time > 1) {
			gameLoopLagTime = gameLoopStopwatch.ElapsedMilliseconds / 1000f;
		}
		gameLoopStopwatch.Restart();
		Helpers.decrementTime(ref gameLoopLagTime);

		stringMessages = new List<string>();
		NetIncomingMessage im;
		while ((im = client.ReadMessage()) != null) {
			string text = "";
			// handle incoming message
			switch (im.MessageType) {
				case NetIncomingMessageType.DebugMessage:
				case NetIncomingMessageType.ErrorMessage:
				case NetIncomingMessageType.WarningMessage:
				case NetIncomingMessageType.VerboseDebugMessage:
					text = im.ReadString();
					//Global.logToConsole("Misc message: " + text);
					break;
				case NetIncomingMessageType.ConnectionLatencyUpdated:
					//var latency = (int)MathF.Round(im.ReadFloat() * 1000);
					//Global.logToConsole("Average latency: " + latency.ToString());
					break;
				case NetIncomingMessageType.StatusChanged:
					NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();
					string reason = im.ReadString();
					if (status == NetConnectionStatus.Disconnected) {
						stringMessages.Add("hostdisconnect:" + reason);
					}

					break;
				case NetIncomingMessageType.Data:

					var rpcIndexByte = im.ReadByte();
					var rpcTemplate = RPC.templates[rpcIndexByte];

					if (rpcTemplate is RPCPeriodicServerPing) {
						packetLossStopwatch.Restart();
						packetsReceived++;
					}

					if (!rpcTemplate.isString) {
						ushort argCount = BitConverter.ToUInt16(im.ReadBytes(2), 0);
						var bytes = im.ReadBytes((int)argCount);
						if (invokeRpcs) {
							Helpers.tryWrap(() => { rpcTemplate.invoke(bytes); }, false);
						}
					} else {
						var message = im.ReadString();
						if (invokeRpcs) {
							if (rpcTemplate is RPCJoinLateResponse) {
								rpcTemplate.invoke(message);
							} else {
								Helpers.tryWrap(() => { rpcTemplate.invoke(message); }, false);
							}
						}
						stringMessages.Add(message);
					}

					break;
				default:
					//Output("Unhandled type: " + im.MessageType + " " + im.LengthBytes + " bytes");
					break;
			}
			client.Recycle(im);
		}
	}
}
