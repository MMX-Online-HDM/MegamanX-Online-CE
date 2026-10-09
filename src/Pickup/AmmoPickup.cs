namespace MMXOnline;

public abstract class BaselineAmmoPickup : Pickup {
	public BaselineAmmoPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false, bool spawnUp = false, bool isRushPickup = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly, spawnUp: spawnUp
	) {
		pickupType = PickupType.Ammo;
		healAmount = 50;
		altHealAmount = 8;
		syncOnLateJoin = true;
	}

	public override bool canUse(int alliance, Actor actor) {
		if (!base.canUse(alliance, actor)) {
			return false;
		}
		if (actor is Character chara && chara.canAddAmmo()) {
			return true;
		}
		if (actor is Maverick mvk && mvk.ammo < mvk.maxAmmo) {
			return true;
		}
		return false;
	}

	public override void use(Actor actor) {
		if (actor is Character chara) {
			chara.addPercentAmmo(healAmount);
		} else if (actor is Maverick maverick) {
			maverick.addAmmo(altHealAmount);
		} else {
			return;
		}
		base.use(actor);
	}
}

public class LargeAmmoPickup : BaselineAmmoPickup {
	public LargeAmmoPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false, bool isRushPickup = false
	) : base(
		owner, pos, "pickup_ammo_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 50;
		altHealAmount = 8;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new LargeAmmoPickup(
			arg.player, arg.pos, arg.netId, true, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeAmmoPickup(
			arg.player, arg.pos, arg.netId, false, 
			teamOnly: arg.extraData[0] == 1, isRushPickup: arg.extraData[2] == 1
		);
	}
}

public class SmallAmmoPickup : BaselineAmmoPickup {
	public SmallAmmoPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false, bool isRushPickup = false
	) : base(
		owner, pos, "pickup_ammo_small", netId, ownedByLocalPlayer, 
		CActorIds.SmallAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 25;
		altHealAmount = 4;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new SmallAmmoPickup(
			arg.player, arg.pos, arg.netId, true, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallAmmoPickup(
			arg.player, arg.pos, arg.netId, false, 
			teamOnly: arg.extraData[0] == 1, isRushPickup: arg.extraData[2] == 1
		);
	}
}
