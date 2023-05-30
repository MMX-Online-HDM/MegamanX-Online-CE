using System;
using Lidgren.Network;

namespace MMXOnline;

// Does nothing by default.
// But can be used for mods that need a RPC.
// Generally, make sub-RPCs out of this one.
public class RPCCustom : RPC {
	public RPCCustom() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	// Use custom invoke here.
	// Normally you want to use the first byte to determine the RPC type.
	// The call another custom RPC from it.
	public override void invoke(params byte[] arguments) {
		byte type = arguments[0];
		byte[] finalArguments = arguments[1..];

		switch (type) {
			case 0:
				// Call a custom RPC invoke() function here.
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

// For the RPCCustom "type" argument.
public enum RpcCustomType {
	placeholder //Replace this for something else.
}
