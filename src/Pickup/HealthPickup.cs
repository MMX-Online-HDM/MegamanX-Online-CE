namespace MMXOnline;

public abstract class BaselineHealthPickup : Pickup {
	public BaselineHealthPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false, bool spawnUp = false, bool isRushPickup = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly, spawnUp: spawnUp
	) {
		pickupType = PickupType.Health;
		healAmount = 8;
	}

	public override bool canUse(int alliance, Actor actor) {
		if (actor is not IDamagable idmg) {
			return false;
		}
		return base.canUse(alliance, actor) && idmg.canBeHealed();
	}

	public override void use(Actor actor) {
		if (actor is not IDamagable idmg) {
			return;
		}
		idmg.heal(Player.stagePlayer, healAmount);
		base.use(actor);
	}
}

public class LargeHealthPickup : BaselineHealthPickup {
	public LargeHealthPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false, bool isRushPickup = false
	) : base(
		owner, pos, "pickup_health_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeHealthPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 8;
		syncOnLateJoin = true;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new LargeHealthPickup(
			arg.player, arg.pos, arg.netId, true, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeHealthPickup(
			arg.player, arg.pos, arg.netId, false, 
			teamOnly: arg.extraData[0] == 1, isRushPickup: arg.extraData[2] == 1
		);
	}
}

public class SmallHealthPickup : BaselineHealthPickup {
	public SmallHealthPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false, bool isRushPickup = false
	) : base(
		owner, pos, "pickup_health_small", netId, ownedByLocalPlayer,
		CActorIds.SmallHealthPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 4;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new SmallHealthPickup(
			arg.player, arg.pos, arg.netId, true, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallHealthPickup(
			arg.player, arg.pos, arg.netId, false, 
			teamOnly: arg.extraData[0] == 1, isRushPickup: arg.extraData[2] == 1
		);
	}
}
