using System.Diagnostics;
using System.Net;
using System.Threading;
using Lidgren.Network;

namespace MMXOnline;

public enum LeaveMatchScenario {
	LeftManually,
	MatchOver,
	ServerShutdown,
	Recreate,
	Rejoin,
	Kicked,
	RecreateMS,
	RejoinMS,
}

public class LeaveMatchSignal {
	public LeaveMatchScenario leaveMatchScenario;
	public Server newServerData;
	public string kickReason;
	public bool isMasterServer;

	public LeaveMatchSignal(
		LeaveMatchScenario leaveMatchScenario,
		Server newServerData, string kickReason,
		bool isMasterServer = false
	) {
		this.leaveMatchScenario = leaveMatchScenario;
		this.newServerData = newServerData;
		this.kickReason = kickReason;
		this.isMasterServer = isMasterServer;
	}

	public void createNewServer() {
		Stopwatch stopWatch = new Stopwatch();
		stopWatch.Start();
		byte[] serverResponse = null;
		do {
			serverResponse = Global.matchmakingQuerier.send(
				newServerData.region.ip, "GetServer:" + newServerData.name, 1000
			);
			if (stopWatch.ElapsedMilliseconds > 5000) {
				Menu.change(new ErrorMenu("Error: Could not create new server.", new MainMenu()));
				return;
			}
		}
		while (!serverResponse.IsNullOrEmpty());

		HostMenu.createServer(
			SelectCharacterMenu.playerData.charNum, newServerData,
			null, true, new MainMenu(), out string errorMessage
		);

		if (!string.IsNullOrEmpty(errorMessage)) {
			// Retry once
			HostMenu.createServer(
				SelectCharacterMenu.playerData.charNum, newServerData,
				null, true, new MainMenu(), out errorMessage
			);
			if (!string.IsNullOrEmpty(errorMessage)) {
				Menu.change(new ErrorMenu(errorMessage, new MainMenu()));
			}
		}
	}

	public void rejoinNewServer() {
		Stopwatch stopWatch = new Stopwatch();
		stopWatch.Start();
		byte[] serverResponse;
		do {
			serverResponse = Global.matchmakingQuerier.send(
				newServerData.region.ip, "GetServer:" + newServerData.name, 1000
			);
			if (stopWatch.ElapsedMilliseconds > 5000) {
				Menu.change(new ErrorMenu("Error: Could not rejoin new server.", new MainMenu()));
				return;
			}
		}
		while (serverResponse.IsNullOrEmpty());

		Server server = Helpers.deserialize<Server>(serverResponse);

		JoinMenu.joinServer(server);
	}

	public void rejoinNewServerMS() {
		Thread.Sleep(500);
		JoinMenuP2P joinMenu = new JoinMenuP2P(false);
		joinMenu.requestServerDetails(newServerData.uniqueID);
		joinMenu.netClient.FlushSendQueue();

		NetIncomingMessage msg;
		// Respond to connection messages.
		for (int i = 0; i <= 50; i++) {
			while ((msg = joinMenu.netClient.ReadMessage()) != null) {
				if (msg.MessageType == NetIncomingMessageType.UnconnectedData &&
					msg.ReadByte() == 101
				) {
					(long, SimpleServerData, IPEndPoint) serverData = joinMenu.receiveServerDetails(msg);
					if (serverData.Item2 != null) {
						joinMenu.joinServer(serverData.Item1, serverData.Item2, serverData.Item3);
						return;
					}
				}
			}
			if (i == 20) {
				joinMenu.requestServerDetails(newServerData.uniqueID);
				joinMenu.netClient.FlushSendQueue();
			}
			Thread.Sleep(100);
		}
		Program.setLastUpdateTimeAsNow();
	}

	public void reCreateMS() {
		HostMenu.reCreateP2PMatch(
			SelectCharacterMenu.playerData.charNum, newServerData,
			new MainMenu(), out string errorMessage
		);

		if (!string.IsNullOrEmpty(errorMessage)) {
			Menu.change(new ErrorMenu(errorMessage, new MainMenu()));
		}
	}
}
