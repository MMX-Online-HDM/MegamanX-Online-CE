using System;
using System.Collections.Generic;

namespace MMXOnline;

public partial class RPCCreateActor : RPC {
	public static Dictionary<int, ActorRpcCreate> functs = new Dictionary<int, ActorRpcCreate> {
		// HP
		{ (int)CActorIds.LargeHealthPickup, LargeHealthPickup.rpcInvoke },
		{ (int)CActorIds.SmallHealthPickup, SmallHealthPickup.rpcInvoke },
		// Ammo
		{ (int)CActorIds.LargeAmmoPickup, LargeAmmoPickup.rpcInvoke },
		{ (int)CActorIds.SmallAmmoPickup, SmallAmmoPickup.rpcInvoke },
		// Enemies.
		{ (int)CActorIds.Met, Met.rpcInvoke },
	};
}