using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

# nullable disable

[ProtoContract]
public class RPCMatchOverResponse {
	[ProtoMember(1)] public HashSet<int> winningAlliances = [];
	[ProtoMember(2)] public string winMessage = "";
	[ProtoMember(3)] public string loseMessage = "";
	[ProtoMember(4)] public string loseMessage2 = "";
	[ProtoMember(5)] public string winMessage2 = "";

	public RPCMatchOverResponse() { }
}

[ProtoContract]
public class RPCAnimModel {
	[ProtoMember(1)] public long? zIndex;
	[ProtoMember(2)] public ushort? zIndexRelActorNetId;
	[ProtoMember(3)] public bool fadeIn;
	[ProtoMember(4)] public bool hasRaColorShader;

	public RPCAnimModel() { }
}

[ProtoContract]
public class ControlPointResponseModel {
	[ProtoMember(1)] public int alliance;
	[ProtoMember(2)] public int num;
	[ProtoMember(3)] public bool locked;
	[ProtoMember(4)] public bool captured;
	[ProtoMember(5)] public float captureTime;
	public ControlPointResponseModel() { }
}

[ProtoContract]
public class MagnetMineResponseModel {
	[ProtoMember(1)] public float x;
	[ProtoMember(2)] public float y;
	[ProtoMember(3)] public ushort netId;
	[ProtoMember(4)] public int playerId;
	public MagnetMineResponseModel() { }
}

[ProtoContract]
public class TurretResponseModel {
	[ProtoMember(1)] public float x;
	[ProtoMember(2)] public float y;
	[ProtoMember(3)] public ushort netId;
	[ProtoMember(4)] public int playerId;
	public TurretResponseModel() { }
}

[ProtoContract]
public class JoinLateResponseModel {
	[ProtoMember(1)] public List<PlayerPB> players;
	[ProtoMember(2)] public ServerPlayer newPlayer;
	[ProtoMember(3)] public List<ControlPointResponseModel> controlPoints;
	[ProtoMember(4)] public List<MagnetMineResponseModel> magnetMines;
	[ProtoMember(5)] public List<TurretResponseModel> turrets;
	public JoinLateResponseModel() { }
}

[ProtoContract]
public class PeriodicServerSyncModel {
	[ProtoMember(1)] public List<ServerPlayer> players;
	public PeriodicServerSyncModel() { }
}

[ProtoContract]
public class PeriodicHostSyncModel {
	[ProtoMember(1)] public RPCMatchOverResponse matchOverResponse;
	[ProtoMember(2)] public int redPoints;
	[ProtoMember(3)] public HashSet<byte> crackedWallBytes = new HashSet<byte>();
	[ProtoMember(4)] public byte virusStarted;
	[ProtoMember(5)] public byte safeZoneSpawnIndex;
	[ProtoMember(6)] public byte[] teamPoints;
}

[ProtoContract]
public class PlayerPB {
	[ProtoMember(1)] public int newAlliance;
	[ProtoMember(2)] public int newCharNum;
	[ProtoMember(3)] public ushort curMaxNetId;
	[ProtoMember(4)] public bool warpedIn;
	[ProtoMember(5)] public float readyTime;
	[ProtoMember(6)] public bool readyTextOver;
	[ProtoMember(7)] public ushort armorFlag;
	[ProtoMember(8)] public LoadoutData loadoutData;
	[ProtoMember(9)] public Disguise disguise;
	[ProtoMember(10)] public ushort charNetId;
	[ProtoMember(11)] public bool isATrans;
	[ProtoMember(12)] public ushort charRollingShieldNetId;
	[ProtoMember(13)] public float charXPos;
	[ProtoMember(14)] public float charYPos;
	[ProtoMember(15)] public int charXDir;
	[ProtoMember(16)] public LoadoutData atransLoadout;
	[ProtoMember(17)] public int currentCharNum;
	[ProtoMember(18)] public int preAtransCharId;

	[ProtoMember(19)] public ServerPlayer serverPlayer;

	public PlayerPB() { }

	public PlayerPB(Player player) {
		// Initliazlize "null" data.
		currentCharNum = -1;
		preAtransCharId = -1;
		charNetId = ushort.MaxValue;
		charRollingShieldNetId = ushort.MaxValue;
		charXDir = 1;
		// Populate data.
		serverPlayer = player.serverPlayer;

		newAlliance = player.newAlliance;
		newCharNum = player.newCharNum;

		curMaxNetId = player.curMaxNetId;
		warpedIn = player.warpedIn;
		readyTime = player.readyTime;
		readyTextOver = player.readyTextOver;
		armorFlag = player.armorFlag;
		loadoutData = player.loadout;
		disguise = player.disguise;
		atransLoadout = player.atransLoadout;

		if (player.character?.netId != null) {
			currentCharNum = (int)player.character.charId;
			charNetId = player.character.netId.Value;
			charRollingShieldNetId = (
				(player.character as MegamanX)?.chargedRollingShieldProj?.netId ?? ushort.MaxValue
			);
			charXPos = player.character.pos.x;
			charYPos = player.character.pos.y;
			charXDir = player.character.xDir;
			isATrans = player.character.isATrans;

			if (player.character.linkedATransChar != null) {
				preAtransCharId = (int)player.character.linkedATransChar.charId;
			}
		}
	}
}

# nullable enable
