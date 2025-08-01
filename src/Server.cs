﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Lidgren.Network;
using Newtonsoft.Json;
using ProtoBuf;

namespace MMXOnline;

public enum NetcodeModel {
	FavorAttacker,
	FavorDefender
}

[ProtoContract]
public class Server {
	// Must be a thread safe list
	[JsonIgnore]
	public static ConcurrentDictionary<Server, bool> servers = new ConcurrentDictionary<Server, bool>();

	[ProtoMember(1)] public string name;
	[ProtoMember(2)] public string level;
	[ProtoMember(3)] public string gameMode;
	[ProtoMember(4)] public int playTo;
	[ProtoMember(5)] public int botCount;
	[ProtoMember(6)] public int maxPlayers;
	[ProtoMember(7)] public Region? region;
	[ProtoMember(8)] public int? timeLimit;
	[ProtoMember(9)] public bool fixedCamera;
	[ProtoMember(10)] public bool hidden;

	[ProtoMember(11)] public bool started;
	[ProtoMember(12)] public int port;
	[ProtoMember(13)] public List<ServerPlayer> players = new List<ServerPlayer>();
	[ProtoMember(14)] public List<BanEntry> kickedPlayers = new List<BanEntry>();

	[ProtoMember(15)] public decimal gameVersion;

	[ProtoMember(16)] public Dictionary<string, int> playerJoinCount = new Dictionary<string, int>();
	[ProtoMember(17)] public Dictionary<string, DateTime> playerLastJoinTime = new Dictionary<string, DateTime>();

	[ProtoMember(18)] public NetcodeModel netcodeModel;
	[ProtoMember(19)] public int netcodeModelPing;
	[ProtoMember(20)] public bool isLAN;
	[ProtoMember(21)] public bool mirrored;
	[ProtoMember(22)] public bool useLoadout;

	[ProtoMember(23)] public string gameChecksum;
	[ProtoMember(24)] public string customMapChecksum;
	[ProtoMember(25)] public string customMapUrl;
	[ProtoMember(26)] public ExtraCpuCharData extraCpuCharData;
	[ProtoMember(27)] public CustomMatchSettings customMatchSettings;
	[ProtoMember(28)] public string shortLevelName;
	[ProtoMember(29)] public string ip = "";
	[ProtoMember(30)] public bool disableHtSt;
	[ProtoMember(31)] public bool disableVehicles;
	[ProtoMember(32)] public bool isP2P;
	[ProtoMember(33)] public byte extraOptions;
	[ProtoMember(33)] public byte[] teamScore;
	[ProtoMember(34)] public byte teamNum = 2;
	[ProtoMember(35)] public int altPlayTo;

	[JsonIgnore]
	public bool favorHost = true;

	[JsonIgnore]
	public int redScore;
	[JsonIgnore]
	public int blueScore;
	[JsonIgnore]
	public int nonSpecPlayerCountOnStart;
	[JsonIgnore]
	public ServerPlayer? host;
	[JsonIgnore]
	public int framesZeroPlayers;
	[JsonIgnore]
	public ServerPlayer? playerToAutobalance;
	[JsonIgnore]
	public NetServer s_server = null!;
	[JsonIgnore]
	public bool killServer;
	[JsonIgnore]
	public Dictionary<string, int> weaponKillStats = new Dictionary<string, int>();
	[JsonIgnore]
	public long uniqueID = 0;

	private bool shutdownVar;
	private long iterations = 0;
	private long lastUpdateTime = 0L;
	private long fpsLimit = TimeSpan.TicksPerSecond / 240;
	private bool loggedOnce = false;

	public bool isCustomMap() {
		return !string.IsNullOrEmpty(customMapChecksum);
	}

	public const byte sendStringRPCByte = 0;
	public const int maxOverflowSpectatorCount = 2;
	public const int maxRejoinCount = 3;
	public const float connectionTimeoutSeconds = 15;

	public const byte getServersQueryByte = 0;
	public const byte getServerQueryByte = 1;

	public const int maxPlayerCap = 25;

	public Server(
		decimal gameVersion, Region? region,
		string name, string level, string shortLevelName,
		string gameMode, int playTo, int botCount, int maxPlayers,
		int? timeLimit, bool fixedCamera, bool hidden,
		NetcodeModel netcodeModel, int netcodeModelPing, bool isLAN,
		bool mirrored, bool useLoadout,
		string gameChecksum, string customMapChecksum, string customMapUrl,
		ExtraCpuCharData extra1v1CpuCharData, CustomMatchSettings customMatchSettings,
		bool disableHtSt, bool disableVehicles, byte teamNum
	) {
		this.gameVersion = gameVersion;
		this.region = region;
		this.name = name;
		this.level = level;
		this.shortLevelName = shortLevelName;
		this.gameMode = gameMode;
		this.playTo = playTo;
		this.botCount = botCount;
		this.maxPlayers = maxPlayerCap;//Helpers.clamp(maxPlayers, 12, maxPlayerCap);
		this.timeLimit = (timeLimit == 0 ? null : timeLimit);
		this.fixedCamera = fixedCamera;
		this.hidden = hidden;
		this.netcodeModel = netcodeModel;
		this.netcodeModelPing = netcodeModelPing;
		this.isLAN = isLAN;
		this.mirrored = mirrored;
		this.useLoadout = useLoadout;
		this.gameChecksum = gameChecksum;
		this.customMapChecksum = customMapChecksum;
		this.customMapUrl = customMapUrl;
		this.extraCpuCharData = extra1v1CpuCharData;
		this.customMatchSettings = customMatchSettings;
		this.disableHtSt = disableHtSt;
		this.disableVehicles = disableVehicles;
		this.teamNum = teamNum;
		teamScore = new byte[teamNum];
		players = new List<ServerPlayer>();
		port = getNextAvailablePort();
	}

	public LevelData getLevelData() {
		if (mirrored) {
			return Global.levelDatas[level + "_mirrored"];
		}
		return Global.levelDatas[level];
	}

	public string getMapShortName() {
		return shortLevelName.Replace("_mirrored", "").Replace("_inverted", "");
	}

	public string getMapDisplayName() {
		return level.Replace("_mirrored", "").Replace("_inverted", "");
	}

	public int getNextAvailablePort() {
		for (int i = Global.basePort + 1; i < 65535; i++) {
			if (servers.Any(s => s.Key.port == i)) {
				continue;
			}
			return i;
		}
		return Global.basePort + 1;
	}

	public int getNextAvailablePlayerId() {
		if (players.Count == 0) return 0;
		var playerIds = players.Select(p => p.id).ToList();
		playerIds.Sort();
		for (int i = 0; i < playerIds.Count - 1; i++) {
			if (playerIds[i + 1] != playerIds[i] + 1) {
				return playerIds[i] + 1;
			}
		}
		return playerIds[playerIds.Count - 1] + 1;
	}

	public string getNextAvailableName(string playerName) {
		// Name already in use: append number at end
		while (true) {
			if (players.Any(p => p.name == playerName)) {
				char lastCharInName = playerName[playerName.Length - 1];
				if (int.TryParse(lastCharInName.ToString(), out var result)) {
					if (result != 9) {
						result++;
						playerName = playerName.Substring(0, playerName.Length - 1) + result.ToString();
					} else {
						playerName += "(1)";
					}
				} else {
					if (playerName.Length < 8) {
						playerName += "1";
					} else {
						playerName = playerName.Remove(playerName.Length - 1) + "1";
					}
				}
			} else {
				break;
			}
		}
		return playerName;
	}

	public ServerPlayer addPlayer(
		string name, ServerPlayer serverPlayer,
		NetConnection connection, bool isBot,
		int? overrideCharNum = null, int? overrideAlliance = null
	) {
		if (isBot) {
			serverPlayer = serverPlayer.clone();
			serverPlayer.isHost = false;
			serverPlayer.charNum = overrideCharNum ?? Helpers.randomRange((int)CharIds.X, (int)CharIds.BusterZero);
			serverPlayer.preferredAlliance = overrideAlliance;
		}

		serverPlayer.name = getNextAvailableName(name);
		serverPlayer.id = getNextAvailablePlayerId();
		serverPlayer.connection = connection;
		serverPlayer.joinedLate = started;
		serverPlayer.isBot = isBot;

		if (GameMode.isStringTeamMode(gameMode)) {
			if (serverPlayer.preferredAlliance != null) {
				serverPlayer.alliance = serverPlayer.preferredAlliance.Value;
			} else {
				serverPlayer.alliance = getMatchInitAutobalanceTeam(players, teamNum);
			}
		} else {
			serverPlayer.alliance = serverPlayer.id;
		}

		players.Add(serverPlayer);
		if (serverPlayer.isHost) host = serverPlayer;
		return serverPlayer;
	}

	public void start() {
		Thread thread = new Thread(work);
		thread.Start();
	}

	public void periodicPing(NetServer s_server, ServerPlayer? prioritizedAutobalancePlayer = null) {
		NetOutgoingMessage om = s_server.CreateMessage();

		foreach (var player in players) {
			if (player.connection != null) {
				player.ping = (int)MathF.Round(player.connection.AverageRoundtripTime * 1000);
			}
		}

		if ((!hidden || isP2P) && GameMode.isStringTeamMode(gameMode) &&
			gameMode != GameMode.TeamElimination && level != "training"
		) {
			int[] teamSizes = GameMode.getAllianceCounts(players, teamNum);
			int biggerTeam = teamSizes.Max();
			int smallerTeam = teamSizes.Min();
			// Check if a team has +2 characters than other team to move 1.
			bool areTeamsUnbalanced = (biggerTeam - 1 >= smallerTeam);

			if (playerToAutobalance != null) {
				// Player left match
				if (!players.Contains(playerToAutobalance)) {
					playerToAutobalance = null;
				}
				// Player was already autobalanced
				else if (playerToAutobalance.alliance == playerToAutobalance.autobalanceAlliance) {
					playerToAutobalance.autobalanceAlliance = null;
					playerToAutobalance = null;
				}
				// Teams no longer unbalanced
				else if (!areTeamsUnbalanced) {
					playerToAutobalance.autobalanceAlliance = null;
					playerToAutobalance = null;
				}
			} else if (areTeamsUnbalanced) {
				int lowerCharTeam = getAutobalanceTeam(teamSizes);
				if (lowerCharTeam >= 0) {
					playerToAutobalance = (
						selectPlayerToAutobalance(lowerCharTeam, prioritizedAutobalancePlayer)
					);
					if (playerToAutobalance != null) {
						if (!playerToAutobalance.isBot) {
							playerToAutobalance.alreadyAutobalanced = true;
						}
						playerToAutobalance.autobalanceAlliance = lowerCharTeam;
					}
				}
			}
		}

		var syncModel = new PeriodicServerSyncModel() { players = players };
		byte[] bytes = Helpers.serialize(syncModel);
		RPC.periodicServerSync.sendFromServer(s_server, bytes);
	}

	// Returns the lower player count team.
	// Returns -1 if there is not posible to autobalance.
	public static int getAutobalanceTeam(int[] teamSizes) {
		int biggerTeam = teamSizes.Max();
		int smallerTeam = teamSizes.Min();
		int lowerCharTeam = Array.IndexOf(teamSizes, smallerTeam);
		// Avoid putting people in 1 player teams if we have more than 3.
		if (teamSizes.Length >= 3 && smallerTeam < 2) {
			int teamsWithPeople = 0;
			int ogLowerTeam = lowerCharTeam;
			smallerTeam = 2;
			lowerCharTeam = -1;
			for (int i = 0; i < teamSizes.Length; i++) {
				if (teamSizes[i] > 0) {
					teamsWithPeople++;
					if (teamSizes[i] < smallerTeam) {
						smallerTeam = teamSizes[i];
						lowerCharTeam = i;
					}
				}
			}
			if (teamsWithPeople < 2) {
				lowerCharTeam = ogLowerTeam;
			}
		}
		if (lowerCharTeam >= 0) {
			return lowerCharTeam;
		}
		return -1;
	}

	public static int getMatchInitAutobalanceTeam(List<Player> players, int teamNum) {
		int[] teamSizes = GameMode.getAllianceCounts(players, teamNum);
		int biggerTeam = teamSizes.Max();
		int smallerTeam = teamSizes.Min();
		if (biggerTeam == smallerTeam) {
			return Helpers.randomRange(0, teamSizes.Length - 1);
		}
		// We try to use autobalance rules if posible.
		int lowerCharTeam = getAutobalanceTeam(teamSizes);
		if (lowerCharTeam >= 0) {
			return lowerCharTeam;
		}
		// We search for the first lower char team
		// if is not really posible to autobalance this out.
		return Array.IndexOf(teamSizes, smallerTeam);
	}

	public static int getMatchInitAutobalanceTeam(List<ServerPlayer> players, int teamNum) {
		int[] teamSizes = GameMode.getAllianceCounts(players, teamNum);
		int biggerTeam = teamSizes.Max();
		int smallerTeam = teamSizes.Min();
		if (biggerTeam == smallerTeam) {
			return Helpers.randomRange(0, teamSizes.Length - 1);
		}
		// We try to use autobalance rules if posible.
		int lowerCharTeam = getAutobalanceTeam(teamSizes);
		if (lowerCharTeam >= 0) {
			return lowerCharTeam;
		}
		// We search for the first lower char team
		// if is not really posible to autobalance this out.
		return Array.IndexOf(teamSizes, smallerTeam);
	}

	public ServerPlayer? selectPlayerToAutobalance(
		int allianceToChooseFrom, ServerPlayer? prioritizedAutobalancePlayer
	) {
		var pool = players.FindAll(p => p.alliance == allianceToChooseFrom && !p.isSpectator);

		// Bots take first priority, even over the prioritized autobalance player
		var firstBot = pool.FirstOrDefault(p => p.isBot);
		if (firstBot != null) return firstBot;

		// Then the prioritized autobalance player
		if (prioritizedAutobalancePlayer != null && pool.Contains(prioritizedAutobalancePlayer)) {
			return prioritizedAutobalancePlayer;
		}

		// Then, a random player not autobalanced (if all left were, set flag to false)
		if (pool.All(p => p.alreadyAutobalanced)) {
			pool.ForEach(p => p.alreadyAutobalanced = false);
		}
		pool = pool.FindAll(p => !p.alreadyAutobalanced);
		if (pool.Count == 0) { 
			pool = players.FindAll(p => p.alliance == allianceToChooseFrom && !p.isSpectator);
		}
		if (pool.Count == 0) {
			return null;
		}
		return pool.GetRandomItem();
	}

	public void work() {
		try {
			Helpers.debugLog("Starting server " + name + " on port " + port);
			string appID = name;
			if (isP2P) {
				appID = "XOD-P2P";
			}
			NetPeerConfiguration config = new NetPeerConfiguration(appID);
			config.MaximumConnections = maxPlayers + maxOverflowSpectatorCount;
			config.Port = port;
			config.AutoFlushSendQueue = false;
			config.EnableUPnP = true;
			config.ConnectionTimeout = connectionTimeoutSeconds;
			config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
			if (isP2P) {
				config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
				config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
			}
			/*
			#if DEBUG
			config.SimulatedMinimumLatency = Global.simulatedLatency;
			config.SimulatedLoss = Global.simulatedPacketLoss;
			config.SimulatedDuplicatesChance = Global.simulatedDuplicates;
			#endif
			*/
			s_server = new NetServer(config);
			s_server.Start();
			s_server.UPnP?.ForwardPort(config.Port, "XOD P2P", s_server.Port);
			if (uniqueID == 0) {
				uniqueID = s_server.UniqueIdentifier;
			}
			if (isLAN) {
				// This will get our LAN IP. Not our internet IP.
				ip = LANIPHelper.GetLocalIPAddress();
			}
			if (isP2P && uniqueID != 0) {
				// If we are a P2P server we cannot use a public IP as the masterserver handles that.
				ip = LANIPHelper.GetLocalIPAddress();
				// Send our info to masterserver to say that we are open now.
				IPEndPoint? masterServerLocation = NetUtility.Resolve(
					MasterServerData.serverIp, MasterServerData.serverPort
				);
				// First. The basic info.
				NetOutgoingMessage basicInfoMsg = s_server.CreateMessage();
				basicInfoMsg.Write((byte)MasterServerMsg.RegisterHost);
				basicInfoMsg.Write(uniqueID);
				basicInfoMsg.Write(IPEndPoint.Parse(ip + ":" + s_server.Port));
				if (Global.radminIP != "") {
					basicInfoMsg.Write(IPEndPoint.Parse(Global.radminIP + ":" + s_server.Port));
				}

				// Second. The match list info.
				NetOutgoingMessage listInfoMsg = s_server.CreateMessage();
				listInfoMsg.Write((byte)MasterServerMsg.RegisterInfo);
				listInfoMsg.Write(uniqueID);
				listInfoMsg.Write(name);
				listInfoMsg.Write((byte)maxPlayers);
				listInfoMsg.Write((byte)players.Count); // Current players.
				listInfoMsg.Write(gameMode);
				listInfoMsg.Write(level);
				listInfoMsg.Write(Global.shortForkName);

				// Finally. The detailed info about the match to others to connect.
				NetOutgoingMessage detailedInfoMsg = s_server.CreateMessage();
				detailedInfoMsg.Write((byte)MasterServerMsg.RegisterDetails);
				detailedInfoMsg.Write(uniqueID);
				var simpleServerData = new SimpleServerData(
					name, level, gameVersion, gameChecksum, customMapChecksum, customMapUrl
				);
				string jsonString = JsonConvert.SerializeObject(simpleServerData);
				detailedInfoMsg.Write(jsonString);

				// Then Send all of these in a row.
				s_server.SendUnconnectedMessage(basicInfoMsg, masterServerLocation);
				s_server.SendUnconnectedMessage(listInfoMsg, masterServerLocation);
				s_server.SendUnconnectedMessage(detailedInfoMsg, masterServerLocation);
			}
		} catch (Exception ex) {
			if (Global.debug) {
				throw;
			} else {
				Logger.logException(ex, true);
				servers.Remove(this, out _);
				return;
			}
		}
		while (!shutdownVar) {
			try {
				runIteration();
			} catch (Exception ex) {
				Logger.LogFatalException(ex);
			}
		}
	}

	public void shutdown(string reason) {
		if (isP2P && uniqueID != 0) {
			IPEndPoint masterServerLocation = NetUtility.Resolve(
				MasterServerData.serverIp, MasterServerData.serverPort
			);
			NetOutgoingMessage netMsg = s_server.CreateMessage();
			netMsg.Write((byte)MasterServerMsg.DeleteHost);
			netMsg.Write(uniqueID);
			s_server.SendUnconnectedMessage(netMsg, masterServerLocation);
		}
		s_server.FlushSendQueue();
		s_server.Shutdown(reason);
		s_server.FlushSendQueue();
		s_server.UPnP?.DeleteForwardingRule(s_server.Port);
		servers.Remove(this, out _);
		shutdownVar = true;
	}

	public void runIteration() {
		long timeNow = (DateTimeOffset.UtcNow - Global.UnixEpoch).Ticks;
		long deltaTime = timeNow - lastUpdateTime;
		if (deltaTime < fpsLimit) {
			return;
		}
		lastUpdateTime = timeNow;
		iterations++;
		if (iterations >= 480) {
			iterations = 0;
		}

		if (killServer) {
			shutdown("Server shut down by in-game mod.");
			return;
		}
		byte playerNum = (byte)s_server.Connections.Count;

		if (playerNum == 0) {
			framesZeroPlayers++;
			if (framesZeroPlayers > 600) {
				Helpers.debugLog("Zero players for 5 second. Shutting down server");
				shutdown("Zero players, shutting down server.");
				return;
			}
		} else {
			// We inform the masterServer that we are still alive.
			if (isP2P && iterations % 240 == 0) {
				NetOutgoingMessage msg = s_server.CreateMessage();
				msg.Write((byte)MasterServerMsg.UpdatePlayerNum);
				msg.Write(uniqueID);
				msg.Write(playerNum);
				s_server.SendUnconnectedMessage(msg, MasterServerData.serverIp, MasterServerData.serverPort);
			}
			if (iterations % 120 == 0) {
				periodicPing(s_server);
			}
			if (iterations % 24 == 0) {
				RPC.periodicServerPing.sendFromServer(s_server, []);
			}

			framesZeroPlayers = 0;
			s_server.FlushSendQueue();
		}

		//s_server.MessageReceivedEvent.WaitOne();
		NetIncomingMessage? im;
		while ((im = s_server.ReadMessage()) != null) {
			List<NetConnection> all = s_server.Connections.ToList(); // get copy
			if (im.SenderConnection != null) {
				all.Remove(im.SenderConnection);
			}
			if (im.MessageType == NetIncomingMessageType.StatusChanged) {
				NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();
				string reason = im.ReadString();
				/*Helpers.debugLog(
					NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) +
					" " + status + ": " + reason
				);*/
				if (status == NetConnectionStatus.Connected) {
					onConnect(im);
				} else if (status == NetConnectionStatus.Disconnected) {
					if (started && reason == "Connection timed out") {
						var player = players.FirstOrDefault(p => p.connection == im.SenderConnection);
						string details = "";
						if (player != null) {
							details = player.deviceId + ":" + player.name;
						}
						Logger.logEvent("timeout", details, isServer: true, forceLog: true);
					}
					onDisconnect(im, reason);
				}
			} else if (im.MessageType == NetIncomingMessageType.Data) {
				byte rpcIndexByte = im.ReadByte();
				RPC rpcTemplate;
				if (rpcIndexByte >= RPC.templates.Length) {
					rpcTemplate = new RPCUnknown();
					rpcTemplate.index = rpcIndexByte;
				} else {
					rpcTemplate = RPC.templates[rpcIndexByte];
				}
				if (rpcTemplate.isServerMessage) {
					processServerMessage(im, rpcTemplate);
				} else {
					processClientMessage(im, rpcTemplate, rpcIndexByte, all);
				}
			} else if (im.MessageType == NetIncomingMessageType.ConnectionApproval) {
				if (im.SenderConnection != null) {
					string ipAddress = im.SenderConnection.RemoteEndPoint?.Address.ToString() ?? "";
					BanEntry? banEntry = Global.banList.FirstOrDefault(b => b.isBanned(ipAddress, "", 0));
					if (banEntry == null) {
						im.SenderConnection.Approve();
					} else {
						im.SenderConnection.Deny();
					}
				}
			} else {
				string text = im.ReadString();
				Helpers.debugLog(text);
			}

			s_server.Recycle(im);
		}

		//stopWatch.Stop();
		//Helpers.logDebug(stopWatch.Elapsed.TotalMilliseconds);

		s_server.FlushSendQueue();
	}

	public void onConnect(NetIncomingMessage im) {
		string connectMessage = im.SenderConnection?.RemoteHailMessage?.ReadString() ?? "";
		if (im.SenderConnection == null || connectMessage == "") {
			return;
		}
		ServerPlayer? playerContract = JsonConvert.DeserializeObject<ServerPlayer>(connectMessage);

		if (playerContract == null) {
			return;
		}

		bool shouldSpec = false;
		if (!started) {
			if (level.EndsWith("_1v1") && players.Count >= 4) {
				shouldSpec = true;
			}
		} else {
			if (level.EndsWith("_1v1") || gameMode == GameMode.Race) {
				shouldSpec = true;
			}
		}

		// Too many existing connections
		if (players.Count >= maxPlayers + maxOverflowSpectatorCount) {
			// Bot exists: remove it
			if (players.Count(p => p.isBot) > 0) {
				var firstBot = players.First(p => p.isBot);
				players.RemoveAll(p => p.id == firstBot.id);
				periodicPing(s_server);
			}
			// Something really wrong happened here...
			else {
				im.SenderConnection.Disconnect("Could not join, over 12 players.");
				return;
			}
		}

		// Too many existing players:
		if (players.Count(p => !p.isSpectator) >= maxPlayers) {
			// Bot exists: remove it
			if (players.Count(p => p.isBot) > 0) {
				var firstBot = players.First(p => p.isBot);
				players.RemoveAll(p => p.id == firstBot.id);
				periodicPing(s_server);
			}
			// Join spectator
			else {
				shouldSpec = true;
			}
		}

		var player = addPlayer(playerContract.name, playerContract, im.SenderConnection, false);

		if (playerContract.isHost && players.Count(p => p.isBot) == 0) {
			for (int i = 0; i < botCount; i++) {
				var cpuData = extraCpuCharData.cpuDatas.ElementAtOrDefault(i);
				int? overrideCharNum = null;
				if (cpuData != null && !cpuData.isRandom) {
					overrideCharNum = cpuData?.charNum;
				}
				int? overrideAlliance = null;
				if (GameMode.isStringTeamMode(gameMode) &&
					cpuData != null &&
					cpuData?.alliance >= 0
				) {
					overrideAlliance = cpuData?.alliance;
				};
				addPlayer("BOT", playerContract, im.SenderConnection, true, overrideCharNum, overrideAlliance);
			}
		}

		if (!player.isBot && !string.IsNullOrEmpty(player.deviceId)) {
			if (!playerJoinCount.ContainsKey(player.deviceId)) {
				playerJoinCount[player.deviceId] = 0;
			}
			if (playerJoinCount[player.deviceId] > maxRejoinCount + 1) {
				playerJoinCount[player.deviceId] = 0;
			}
			playerJoinCount[player.deviceId]++;
			playerLastJoinTime[player.deviceId] = DateTime.UtcNow;
		}

		player.isSpectator = shouldSpec;

		string joinMsg = string.Format("Player {0} with id {1} and alliance {2} joined {3}",
			player.name,
			player.id.ToString(),
			player.alliance.ToString(),
			(playerContract.isHost ? " as host" : ""));

		Helpers.debugLog(joinMsg);

		NetOutgoingMessage om = s_server.CreateMessage();
		om.Write(sendStringRPCByte);
		om.Write("joinserverresponse:" + JsonConvert.SerializeObject(player));
		s_server.SendToAll(om, NetDeliveryMethod.ReliableOrdered);
		Helpers.debugLog("Sending general response");

		NetOutgoingMessage omTargeted = s_server.CreateMessage();
		omTargeted.Write(sendStringRPCByte);
		var joinServerResponse = new JoinServerResponse(this);
		omTargeted.Write("joinservertargetedresponse:" + JsonConvert.SerializeObject(joinServerResponse));
		Helpers.debugLog("Sending response to: " + im.SenderConnection.RemoteEndPoint.ToString());
		s_server.SendMessage(omTargeted, im.SenderConnection, NetDeliveryMethod.ReliableOrdered);
	}

	public void onDisconnect(NetIncomingMessage im, string reason) {
		var disconnectedPlayer = players.FirstOrDefault(p => p.connection == im.SenderConnection && !p.isBot);
		if (disconnectedPlayer != null) {
			players.Remove(disconnectedPlayer);
		}
		bool hostDisconnectBeforeStart = (disconnectedPlayer != null && disconnectedPlayer.isHost && !started);
		if (players.Count == 0 ||
			reason == "Recreate" ||
			reason == "RecreateMS" ||
			host == null ||
			hostDisconnectBeforeStart
		) {
			NetOutgoingMessage om = s_server.CreateMessage();
			Helpers.debugLog("Host left, shutting down server " + name);

			if (Global.showDiagnostics && s_server?.Statistics != null) {
				float downloadedBytes = s_server.Statistics.ReceivedBytes / 1000000f;
				float uploadedBytes = s_server.Statistics.SentBytes / 1000000f;
				Helpers.debugLog("Received: " + downloadedBytes.ToString("0.00") + " mb");
				Helpers.debugLog("Sent: " + uploadedBytes.ToString("0.00") + " mb");
			}

			if (reason == "RecreateMS") {
				shutdown("RecreateMS");
			} else if (reason == "Recreate") {
				shutdown("Recreate");
			} else {
				shutdown("Host left, shutting down server.");
			}
			return;
		} else if (s_server.Connections.Count > 0) {
			NetOutgoingMessage om;
			if (host != null && host.connection == im.SenderConnection) {
				// Host promotion: find the first non-bot player and promote them to host
				host = players.FirstOrDefault(p => !p.isBot);
				if (host != null) {
					// Host found: send this message to clients and make all bot share the host's connection.
					host.isHost = true;
					foreach (var player in players) {
						if (player.isBot) {
							player.connection = host.connection;
						}
					}
					RPC.hostPromotion.sendFromServer(s_server, [(byte)host.id]);

					// Remove all bots if host leaves, to prevent a class of unmaintainable bugs
					foreach (var player in players.ToList()) {
						if (player.isBot) {
							players.Remove(player);
						}
					}
					periodicPing(s_server);
				} else {
					// No host found: shut down server, everyone left
					shutdown("All players left, shutting down server.");
					return;
				}
			}
			if (disconnectedPlayer != null) {
				om = s_server.CreateMessage();
				om.Write(sendStringRPCByte);
				om.Write("clientdisconnect:" + JsonConvert.SerializeObject(disconnectedPlayer));
				s_server.SendToAll(om, NetDeliveryMethod.ReliableOrdered, 0);
			}
		} else {
			// No connections: shut down server, everyone left
			shutdown("All players left, shutting down server.");
			return;
		}
	}

	public void processServerMessage(NetIncomingMessage im, RPC rpcTemplate) {
		if (rpcTemplate is RPCUpdateStarted) {
			started = true;
			nonSpecPlayerCountOnStart = players.Count(p => p.isSpectator);
		} else if (rpcTemplate is RPCLogWeaponKills) {
			logWeaponKills();
		} else if (rpcTemplate is RPCReportPlayerRequest) {
			string playerName = im.ReadString();
			var player = players.FirstOrDefault(p => p.name == playerName);
			if (player != null) {
				NetOutgoingMessage om = s_server.CreateMessage();
				om.Write((byte)RPC.templates.IndexOf(RPC.reportPlayerResponse));
				ReportedPlayer reportedPlayer = new ReportedPlayer(
					player.name, player.connection?.RemoteEndPoint?.Address?.ToString() ?? "", player.deviceId
				);
				string reportedPlayerJson = JsonConvert.SerializeObject(reportedPlayer);
				om.Write(reportedPlayerJson);
				if (im.SenderConnection != null) {
					s_server.SendMessage(om, im.SenderConnection, rpcTemplate.netDeliveryMethod, 0);
				}
			}
		} else if (rpcTemplate is RPCKickPlayerRequest) {
			string kickPlayerJson = im.ReadString();
			RPCKickPlayerJson? kickPlayerObj = JsonConvert.DeserializeObject<RPCKickPlayerJson>(kickPlayerJson);

			if (kickPlayerObj?.banTimeMinutes > 0) {
				string deviceId = kickPlayerObj.deviceId;
				string kickReason = kickPlayerObj.banReason;
				DateTime bannedUntil = DateTime.UtcNow.AddMinutes(kickPlayerObj.banTimeMinutes);
				kickedPlayers.Add(new BanEntry("", deviceId, kickReason, bannedUntil, 0));
			}

			NetOutgoingMessage om = s_server.CreateMessage();
			om.Write((byte)RPC.templates.IndexOf(RPC.kickPlayerResponse));
			om.Write(kickPlayerJson);
			s_server.SendToAll(om, rpcTemplate.netDeliveryMethod, 0);
		} else if (rpcTemplate is RPCUpdatePlayer) {
			ushort argCount = BitConverter.ToUInt16(im.ReadBytes(2), 0);
			var bytes = im.ReadBytes(argCount);
			int playerId = bytes[0];
			int kills = BitConverter.ToUInt16(new byte[] { bytes[1], bytes[2] }, 0);
			int deaths = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[4] }, 0);
			var player = players.FirstOrDefault(p => p.id == playerId);
			if (player != null) {
				player.kills = kills;
				player.deaths = deaths;
				periodicPing(s_server);
			}
		} else if (rpcTemplate is RPCAddBot) {
			if (players.Count < 10) {
				ushort argCount = BitConverter.ToUInt16(im.ReadBytes(2), 0);
				var bytes = im.ReadBytes(argCount);

				int charNum = bytes[0];
				int team = bytes[1];

				if (charNum == 255) charNum = Helpers.randomRange((int)CharIds.X, (int)CharIds.BusterZero);
				int? preferredAlliance = null;

				if (team >= 0 && team <= teamNum) {
					preferredAlliance = team;
				}
				var serverPlayer = new ServerPlayer(
					"BOT", 0, false, charNum, preferredAlliance, "", im.SenderConnection, host?.startPing
				);
				if (im.SenderConnection != null) {
					addPlayer("BOT", serverPlayer, im.SenderConnection, true, overrideCharNum: charNum);
				}
				periodicPing(s_server);
			}
		} else if (rpcTemplate is RPCRemoveBot) {
			ushort argCount = BitConverter.ToUInt16(im.ReadBytes(2), 0);
			var bytes = im.ReadBytes(argCount);
			int playerId = bytes[0];
			players.RemoveAll(p => p.id == playerId);
			periodicPing(s_server);
		} else if (rpcTemplate is RPCMakeSpectator) {
			ushort argCount = BitConverter.ToUInt16(im.ReadBytes(2), 0);
			var bytes = im.ReadBytes(argCount);
			int playerId = bytes[0];
			int spectator = bytes[1];
			bool isSpectator = (spectator == 0);
			if (!isSpectator && players.Count(p => !p.isSpectator) >= maxPlayers) {
				return;
			}

			ServerPlayer? player = players.FirstOrDefault(p => p.id == playerId);
			if (player != null) {
				player.isSpectator = isSpectator;
				periodicPing(s_server, player);
			}
		}
	}

	public void processClientMessage(
		NetIncomingMessage im, RPC rpcTemplate, byte rpcIndexByte, List<NetConnection> all
	) {
		NetOutgoingMessage om = s_server.CreateMessage();

		if (!rpcTemplate.isString) {
			ushort argCount = BitConverter.ToUInt16(im.ReadBytes(2), 0);

			om.Write(rpcIndexByte);
			om.Write(argCount);
			byte[] bytes;

			// Safetly for Unknown RPCs.
			if (rpcTemplate is RPCUnknown) {
				bytes = new byte[argCount];
				if (!im.TryReadBytes(bytes)) {
					return;
				}
			}
			// For know ones we just can and should crash.
			else {
				bytes = im.ReadBytes(argCount);
			}
			foreach (byte b in bytes) {
				om.Write(b);
			}

			// Weapon kill logging
			if (rpcIndexByte == RPC.templates.IndexOf(RPC.killPlayer)) {
				int hasOwnerId = bytes[0];
				int killerId = bytes[1];
				int assisterId = bytes[2];
				int? weaponIndex = null;
				ushort? projId = null;
				if (bytes.Length >= 6) {
					weaponIndex = bytes[5];
				}
				if (bytes.Length >= 7) {
					projId = BitConverter.ToUInt16(new byte[] { bytes[6], bytes[7] }, 0);
				}

				if (hasOwnerId != 0 && weaponIndex != null && projId != null) {
					var killer = players.FirstOrDefault(p => p.id == killerId);
					var assister = players.FirstOrDefault(p => p.id == assisterId);
					if ((killer != null && !killer.isBot) ||
						(assister != null && !assister.isBot)
					) {
						addWeaponKillStat(projId, weaponIndex);
					}
				}
			}

			// Team score update
			if ((gameMode == GameMode.CTF || gameMode == GameMode.TeamDeathmatch) &&
				rpcIndexByte == RPC.templates.IndexOf(RPC.syncTeamScores)
			) {
				teamScore = bytes;
				if (Global.level?.gameMode != null) {
					Global.level.gameMode.teamPoints = bytes;
				}
			}
		} else {
			var message = im.ReadString();
			om.Write(rpcIndexByte);
			om.Write(message);

			if (rpcIndexByte == RPC.templates.IndexOf(RPC.switchTeam)) {
				RPCSwitchTeam.getMessageParts(message, out int playerId, out int alliance);
				var player = players.FirstOrDefault(p => p.id == playerId);
				if (player != null) {
					player.alliance = alliance;
					Helpers.debugLog("Changed alliance of player " + playerId + " to " + alliance);
				}
			}

			/*
			Helpers.logDebug(
				NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " broadcasted: " + message)
			);
			*/
		}

		if (rpcTemplate.toHostOnly) {
			if (host?.connection != null) {
				s_server.SendMessage(om, host.connection, rpcTemplate.netDeliveryMethod, 0);
			}
		} else if (all.Count > 0) {
			s_server.SendMessage(om, all, rpcTemplate.netDeliveryMethod, 0);
		}
	}

	public string getNetcodeString() {
		if (netcodeModel == NetcodeModel.FavorAttacker) {
			return "A " + netcodeModelPing.ToString();
		}
		return "D";
	}

	public void addWeaponKillStat(int? projId, int? weaponIndex) {
		if (projId == null) return;
		try {
			ProjIds pid = (ProjIds)projId.Value;
			string key = pid.ToString();
			if (pid == ProjIds.Burn) {
				if (weaponIndex == 4) key = "FireWaveBurn";
				else if (weaponIndex == 54) key = "MK2NapalmBurn";
				else if (weaponIndex == 11) key = "RyuenjinBurn";
				else if (weaponIndex == 27) key = "SpeedBurnerBurn";
				else if (weaponIndex != null) key = "BurnAssist";
				else key = "BurnUnknown";
			}

			if (!weaponKillStats.ContainsKey(key)) {
				weaponKillStats[key] = 0;
			}
			weaponKillStats[key]++;
		} catch { }
	}

	public void logWeaponKills() {
		if (loggedOnce) {
			return;
		}
		loggedOnce = true;
		foreach (var kvp in weaponKillStats) {
			string weaponName = kvp.Key;
			Logger.logEvent("weapon_kills", weaponName, kvp.Value, forceLog: true);
		}
	}
}

public class ShutdownException : Exception {
}
