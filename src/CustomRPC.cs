using System;
using Lidgren.Network;

namespace MMXOnline;

// Does nothing by default.
// But can be used for mods that need a RPC.
// Generally, make sub-RPCs out of this one.
public class RPCCustom : RPC {
	public RPCCustom() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
		isPreUpdate = true;
	}

	// Use custom invoke here.
	// Normally you want to use the first byte to determine the RPC type.
	// The call another custom RPC from it.
	public override void invoke(params byte[] arguments) {
		if (Global.serverClient == null) {
			return;
		}
		byte type = arguments[0];
		byte[] finalArguments = arguments[1..];

		// Call a custom RPC invoke() function here.
		switch (type) {
			case (byte)RpcCustomType.ChangeOwnership:
				RPC.changeOwnership.invoke(finalArguments);
				break;
			case (byte)RpcCustomType.Reflect:
				RPC.reflect.invoke(finalArguments);
				break;
			case (byte)RpcCustomType.Deflect:
				RPC.deflect.invoke(finalArguments);
				break;
			case (byte)RpcCustomType.UpdateMaxTime:
				RPC.updateMaxTime.invoke(finalArguments);
				break;
			case (byte)RpcCustomType.ReviveSigma:
				RPC.reviveSigma.invoke(finalArguments);
				break;
		}
	}

	// Call this from the sendRpc function of your custom RPCs.
	public void sendRpc(byte type, byte[] arguments) {
		byte[] sendValues = new byte[arguments.Length + 1];
		sendValues[0] = type;
		Array.Copy(arguments, 0, sendValues, 1, arguments.Length);

		if (Global.serverClient != null) {
			Global.serverClient.rpc(RPC.custom, sendValues);
		}
	}
}

// Does nothing by default.
// Used when a unknow index is sent.
public class RPCUnknown : RPC {
	public RPCUnknown() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}
}

// For the RPCCustom "type" argument.
public enum RpcCustomType {
	ChangeOwnership,
	Reflect,
	Deflect,
	UpdateMaxTime,
	ReviveSigma
}

public class RpcChangeOwnership : RPC {
	public RpcChangeOwnership() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(byte[] arguments) {
		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
		Actor? actor = Global.level.getActorByNetId(netId, true);
		Player? player = Global.level.getPlayerById(arguments[0]);
		if (actor is not Projectile proj) {
			return;
		}
		if (Global.level.mainPlayer == player) {
			proj.takeOwnership();
		}
	}

	public void sendRpc(int playerId, ushort netId) {
		if (Global.serverClient == null) {
			return;
		}
		byte[] netIdBytes = BitConverter.GetBytes(netId);
		byte[] sendValues = new byte[] {
			(byte)playerId,
			netIdBytes[0],
			netIdBytes[1]
		};
		RPC.custom.sendRpc((byte)RpcCustomType.ChangeOwnership, sendValues);
	}
}

public class RpcReflect : RPC {
	public RpcReflect() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(byte[] arguments) {
		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
		Actor? actor = Global.level.getActorByNetId(netId);
		Player player = Global.level.getPlayerById(arguments[0]);
		if (actor is not Projectile proj) {
			return;
		}
		proj.reflect(player);
	}

	public void sendRpc(int playerId, ushort netId) {
		if (Global.serverClient == null) {
			return;
		}
		byte[] netIdBytes = BitConverter.GetBytes(netId);
		byte[] sendValues = new byte[] {
			(byte)playerId,
			netIdBytes[0],
			netIdBytes[1]
		};
		RPC.custom.sendRpc((byte)RpcCustomType.Reflect, sendValues);
	}
}

public class RpcDeflect : RPC {
	public RpcDeflect() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(byte[] arguments) {
		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
		Actor? actor = Global.level.getActorByNetId(netId);
		Player player = Global.level.getPlayerById(arguments[0]);
		if (actor is not Projectile proj) {
			return;
		}
		proj.deflect(player);
	}

	public void sendRpc(int playerId, ushort netId) {
		if (Global.serverClient == null) {
			return;
		}
		byte[] netIdBytes = BitConverter.GetBytes(netId);
		byte[] sendValues = new byte[] {
			(byte)playerId,
			netIdBytes[0],
			netIdBytes[1]
		};
		RPC.custom.sendRpc((byte)RpcCustomType.Deflect, sendValues);
	}
}

public class RpcUpdateMaxTime : RPC {
	public RpcUpdateMaxTime() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(byte[] arguments) {
		float newMaxTime = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);
		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
		Actor? actor = Global.level.getActorByNetId(netId);
		if (actor is not Projectile proj) {
			return;
		}
		proj.maxTime = newMaxTime / 60f;
	}

	public void sendRpc(ushort netId, float time) {
		if (Global.serverClient == null) {
			return;
		}
		int timeOnFrames = (int)MathF.Ceiling(time * 60f);
		if (timeOnFrames > UInt16.MaxValue) {
			timeOnFrames = UInt16.MaxValue;
		}

		byte[] netIdBytes = BitConverter.GetBytes(netId);
		byte[] timeBytes = BitConverter.GetBytes((ushort)timeOnFrames);
		byte[] sendValues = new byte[] {
			netIdBytes[0],
			netIdBytes[1],
			timeBytes[0],
			timeBytes[1]
		};
		RPC.custom.sendRpc((byte)RpcCustomType.UpdateMaxTime, sendValues);
	}
}

public class RpcReviveSigma : RPC {
	public RpcReviveSigma() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(byte[] arguments) {
		Player player = Global.level.getPlayerById(arguments[0]);
		ushort netId = BitConverter.ToUInt16(arguments[2..4]);
		float posX = BitConverter.ToSingle(arguments[4..8]);
		float posY = BitConverter.ToSingle(arguments[8..12]);
	
		player.reviveSigmaNonOwner(arguments[1], new Point(posX, posY), netId);
	}

	public void sendRpc( 
		int form, Point spawnPoint, int playerId, ushort netId) {
		if (Global.serverClient == null) {
			return;
		}
		byte[] netIdBytes = BitConverter.GetBytes(netId);
		byte[] posXBytes = BitConverter.GetBytes(spawnPoint.x);
		byte[] posYBytes = BitConverter.GetBytes(spawnPoint.y);

		byte[] sendValues = new byte[] {
			(byte)playerId,
			(byte)form,
			netIdBytes[0],
			netIdBytes[1],
			posXBytes[0], posXBytes[1], posXBytes[2], posXBytes[3],
			posYBytes[0], posYBytes[1], posYBytes[2], posYBytes[3],
		};
		RPC.custom.sendRpc((byte)RpcCustomType.ReviveSigma, sendValues);
	}
}
