using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Lidgren.Network;
using Newtonsoft.Json;

namespace MMXOnline;

public class ServerClient {
	public NetClient client;
	public bool isHost;
	public ServerPlayer serverPlayer = null!;
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
		} catch { }

		return client;
	}

	public static ServerClient? Create(
		string serverIp, string serverName, int serverPort,
		ServerPlayer inputServerPlayer, out JoinServerResponse? joinServerResponse,
		out string error
	) {
		error = "";
		NetPeerConfiguration config = new NetPeerConfiguration(serverName);
		config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
		config.AutoFlushSendQueue = false;
		config.ConnectionTimeout = Server.connectionTimeoutSeconds;
		//config.Port = Global.clientPort;
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
					) ?? throw new Exception("Error Deserializing server response");
					serverClient.serverPlayer = (
						joinServerResponse.getLastPlayer()
						?? throw new Exception("Error Recovering player list.")
					);
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

	public static ServerClient? CreateHolePunch(
		NetClient client, long serverId, IPEndPoint serverIP, IPEndPoint? radminIP, ServerPlayer inputServerPlayer,
		out JoinServerResponse? joinServerResponse, out string log
	) {
		// Enable autoflush temporally.
		string localAdress = LANIPHelper.GetLocalIPAddress() + ":" + client.Port;
		client.Configuration.AutoFlushSendQueue = true;
		client.FlushSendQueue();
		log = "";
		// UDP Hole Punching happens here.
		NetOutgoingMessage regMsg = client.CreateMessage();
		regMsg.Write((byte)MasterServerMsg.ConnectPeersShort);
		regMsg.Write(serverId);
		regMsg.Write(IPEndPoint.Parse(localAdress));
		IPEndPoint? masterServerLocation = NetUtility.Resolve(
			MasterServerData.serverIp, MasterServerData.serverPort
		);
		if (masterServerLocation == null) {
			joinServerResponse = null;
			log = "Error getting host IP directon.";
			return null;
		}
		client.SendUnconnectedMessage(regMsg, masterServerLocation);
		client.FlushSendQueue();
		NetOutgoingMessage hail = client.CreateMessage(JsonConvert.SerializeObject(inputServerPlayer));
		// Wait for hole punching to happen.
		log += "\nSent connection message...";
		log += "\nLAN IP: " + localAdress;
		int count = 0;
		bool connected = false;
		NetIncomingMessage? msg;
		while (count < 20) {
			while ((msg = client.ReadMessage()) != null && !connected) {
				if (msg.MessageType == NetIncomingMessageType.NatIntroductionSuccess) {
					connected = true;
					log += "\nGot Connection MSG!";
					if (msg.SenderEndPoint == null) {
						joinServerResponse = null;
						log += "\nError: Null server endpoint.";
						return null;
					}
					client.Connect(msg.SenderEndPoint, hail);
					Thread.Sleep(100);
					goto exitLoop;
				} else {
					log += "\nGot other message.";
					log += "\nMSG type:" + msg.MessageType.ToString();
				}
			}
			count++;
			client.FlushSendQueue();
			Thread.Sleep(100);
		}

		// Do this if hole punch fails.
		log += "\nFailed to holepunch, trying direct connection anyway...";
		client.Connect(serverIP, hail);
		count = 0;
		while (count < 20 && !connected) {
			if (client.ConnectionsCount != 0) {
				log += "\nDirect connection approved.";
				connected = true;
				goto exitLoop;
			}
			count++;
			client.FlushSendQueue();
			Thread.Sleep(100);
		}

		// Try Radmin.
		if (client.ConnectionsCount == 0 &&
			Global.radminIP != null &&
			radminIP != null
		) {
			log += "\nFailed direct connection, using radmin...";
			log += "\nRadmin IP: " + Global.radminIP +
				"\nSever Radmin IP:" + radminIP.ToString();

			NetPeerConfiguration oldConfig = client.Configuration;
			client.Shutdown("Error");
			Thread.Sleep(50);
			client = new NetClient(oldConfig);
			client.Start();
			client.Connect(radminIP, hail);

			count = 0;
			while (count < 20 && !connected) {
				if (client.ConnectionsCount != 0) {
					log += "\nRadmin connection approved.";
					connected = true;
					goto exitLoop;
				}
				count++;
				client.FlushSendQueue();
				Thread.Sleep(100);
			}
		} else {
			log += "\nRadmin not avaliable, skipping.";
		}
		// Ok. We failed 3 times so we give up.
		if (client.ConnectionsCount != 0) {
			log += "\nFailed to connect.";
			joinServerResponse = null;
			client.Configuration.AutoFlushSendQueue = false;
			return null;
		}
		exitLoop:
		// If it works, continue.
		log += "\nStarting Serverclient.";
		log += "\nConections active: " + client.Connections.Count + " / " + client.ConnectionsCount;
		var serverClient = new ServerClient(client, inputServerPlayer.isHost);
		serverClient.serverId = serverId;
		client.FlushSendQueue();
		// Now try to connect to get server connect response after conection.
		count = 0;
		while (count <= 40) {
			serverClient.getMessages(out var messages, false);
			foreach (var message in messages) {
				if (message.StartsWith("joinservertargetedresponse:")) {
					log += "\nGot connection response.";
					joinServerResponse = (
						JsonConvert.DeserializeObject<JoinServerResponse>(
							message.RemovePrefix("joinservertargetedresponse:")
						)
					) ?? throw new Exception("Error Deserializing server response");
					serverClient.serverPlayer = (
						joinServerResponse.getLastPlayer()
						?? throw new Exception("Error Recovering player list.")
					);
					client.Configuration.AutoFlushSendQueue = false;
					return serverClient;
				} else if (message.StartsWith("hostdisconnect:")) {
					var reason = message.Split(':')[1];
					log = "\nCould not join: " + reason;
					joinServerResponse = null;
					serverClient.disconnect("Client couldn't get response");
					client.Configuration.AutoFlushSendQueue = false;
					return null;
				} else if (message.StartsWith("joinserverresponse:")) {
					log += "\nGot general response.";
				} else {
					log += "\nMessage: " + message;
				}
			}
			count++;
			client.FlushSendQueue();
			Thread.Sleep(100);
		}
		log += "\nFailed to get connect response from P2P server.";
		joinServerResponse = null;
		serverClient.disconnect("Client couldn't get response");
		client.Configuration.AutoFlushSendQueue = false;
		return null;
	}

	public static ServerClient? CreateDirect(
		string serverIp, int port, ServerPlayer inputServerPlayer,
		out JoinServerResponse? joinServerResponse, out string error
	) {
		error = "";
		NetPeerConfiguration config = new NetPeerConfiguration("XOD-P2P");
		config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
		config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
		config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
		config.AutoFlushSendQueue = false;
		config.ConnectionTimeout = Server.connectionTimeoutSeconds;
		// Create client.
		var client = new NetClient(config);
		client.Configuration.AutoFlushSendQueue = true;
		client.Start();
		NetOutgoingMessage hail = client.CreateMessage(JsonConvert.SerializeObject(inputServerPlayer));
		client.Connect(serverIp, port, hail);
		// Wait for connection.
		Thread.Sleep(100);
		Console.WriteLine("Starting Serverclient.");
		int count = 0;
		while (count < 40 && client.ConnectionsCount == 0) {
			Thread.Sleep(100);
			client.FlushSendQueue();
			count++;
		}
		if (client.ConnectionsCount == 0) {
			error = "Failed initial connection.";
			joinServerResponse = null;
			client.Configuration.AutoFlushSendQueue = false;
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
					) ?? throw new Exception("Error Deserializing server response");
					serverClient.serverPlayer = (
						joinServerResponse.getLastPlayer()
						?? throw new Exception("Error Recovering player list.")
					);
					client.Configuration.AutoFlushSendQueue = false;
					return serverClient;
				} else if (message.StartsWith("hostdisconnect:")) {
					var reason = message.Split(':')[1];
					error = "Could not join: " + reason;
					joinServerResponse = null;
					serverClient.disconnect("Client couldn't get response");
					client.Configuration.AutoFlushSendQueue = false;
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
		client.Configuration.AutoFlushSendQueue = false;
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
		if (rpcTemplate.netDeliveryMethod == NetDeliveryMethod.ReliableSequenced) {
			throw new Exception("Warning, cannot send Sequenced RPC on channel 0.");
		}
		byte rpcIndexByte = (byte)rpcIndex;
		NetOutgoingMessage om = client.CreateMessage();
		om.Write(rpcIndexByte);
		om.Write((ushort)0);
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
		om.Write((ushort)0);
		om.Write(message);
		client.SendMessage(om, rpcTemplate.netDeliveryMethod);
	}

	public void rpcSequenced(RPC rpcTemplate, ushort channel, params byte[] arguments) {
		int rpcIndex = RPC.templates.IndexOf(rpcTemplate);
		if (rpcIndex == -1) {
			throw new Exception("RPC index not found!");
		}
		if (channel <= 0) {
			throw new Exception("Warning, cannot send Sequenced RPC on channel 0.");
		}
		byte rpcIndexByte = (byte)rpcIndex;
		NetOutgoingMessage om = client.CreateMessage();
		om.Write(rpcIndexByte);
		om.Write(channel);
		om.Write((ushort)arguments.Length);
		om.Write(arguments);
		client.SendMessage(om, rpcTemplate.netDeliveryMethod, channel);
	}

	public void getMessages(out List<string> stringMessages, bool invokeRpcs) {
		if (gameLoopStopwatch.ElapsedMilliseconds > 250 && Global.level?.time > 1) {
			gameLoopLagTime = gameLoopStopwatch.ElapsedMilliseconds / 1000f;
		}
		gameLoopStopwatch.Restart();
		Helpers.decrementTime(ref gameLoopLagTime);

		stringMessages = new List<string>();
		NetIncomingMessage? im;
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
					byte rpcIndexByte = im.ReadByte();
					_ = im.ReadUInt16(); // Channel data. Not needed for recieving.
					RPC rpcTemplate;
					if (rpcIndexByte >= RPC.templates.Length) {
						rpcTemplate = new RPCUnknown();
					} else {
						rpcTemplate = RPC.templates[rpcIndexByte];
					}

					if (rpcTemplate is RPCPeriodicServerPing) {
						packetLossStopwatch.Restart();
						packetsReceived++;
					}

					if (!rpcTemplate.isString) {
						ushort argCount = im.ReadUInt16();
						var bytes = im.ReadBytes(argCount);
						if (invokeRpcs && Global.level != null) {
							if (rpcTemplate.isServerMessage || rpcTemplate.levelless) {
								rpcTemplate.invoke(bytes);
							}
							else if (rpcTemplate.isPreUpdate) {
								Global.level.pendingPreUpdateRpcs.Add(new PendingRPC(rpcTemplate, bytes));
							}
							else if (rpcTemplate.isCollision) {
								Global.level.pendingCollisionRpcs.Add(new PendingRPC(rpcTemplate, bytes));
							}
							else {
								Global.level.pendingUpdateRpcs.Add(new PendingRPC(rpcTemplate, bytes));
							}
						}
					} else {
						var message = im.ReadString();
						if (invokeRpcs && Global.level != null) {
							if (rpcTemplate.isServerMessage || rpcTemplate.levelless) {
								rpcTemplate.invoke(message);
							}
							else if (rpcTemplate.isPreUpdate) {
								Global.level.pendingPreUpdateRpcs.Add(new PendingRPC(rpcTemplate, message));
							}
							else if (rpcTemplate.isCollision) {
								Global.level.pendingCollisionRpcs.Add(new PendingRPC(rpcTemplate, message));
							}
							else {
								Global.level.pendingUpdateRpcs.Add(new PendingRPC(rpcTemplate, message));
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
