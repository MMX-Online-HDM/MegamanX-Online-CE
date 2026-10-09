using System.Collections.Generic;

namespace MMXOnline;

public enum PickupType {
	None,
	Health,
	Ammo,
	Bolts
}

public enum PickupTypeRpc {
	LargeHealth,
	SmallHealth,
	LargeAmmo,
	SmallAmmo
}

public class Pickup : Actor {
	public Player ownerPlayer;
	public float healAmount = 0;
	public float altHealAmount = 0;
	public PickupType pickupType = PickupType.None;
	public bool teamOnly;
	public bool spawnUp;

	public Pickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false, bool spawnUp = false
	) : base(
		sprite, pos, netId, ownedByLocalPlayer, false
	) {
		this.teamOnly = teamOnly;
		this.spawnUp = spawnUp;
		canBeLocal = true;
		ownerPlayer = owner;

		if (collider != null) {
			collider.wallOnly = true;
			collider.isTrigger = false;
		}

		this.cActorId = cActorId;
		if (spawnUp) {
			vel.y -= 5 * 60;
		}

		if (!ownedByLocalPlayer && teamOnly) {
			alpha *= 0.75f;
		}
		if (Global.level.gameMode.isTeamMode && teamOnly) {
			int alliance = owner.alliance;

			RenderEffectType? allianceEffect = alliance switch {
				0 => RenderEffectType.BlueShadow,
				1 => RenderEffectType.RedShadow,
				2 => RenderEffectType.GreenShadow,
				3 => RenderEffectType.PurpleShadow,
				4 => RenderEffectType.YellowShadow,
				5 => RenderEffectType.OrangeShadow,
				_ => null
			};
			if (allianceEffect != null) {
				addRenderEffect(allianceEffect.Value);
			}
		}

		if (sendRpc) {
			RPC.createActor.sendRpc(
				this, ownerPlayer, null, getSerialExtra()
			);
		}
		this.spawnUp = false;
		syncOnLateJoin = true;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		int leeway = 200;

		if (pos.x > Global.level.width + leeway
			|| pos.x < -leeway ||
			pos.y > Global.level.height + leeway ||
			pos.y < -leeway
		) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.otherCollider?.flag == (int)HitboxFlag.Hitbox) {
			return;
		} 
		if (other.gameObject is Actor actor && actor.ownedByLocalPlayer) {
			if (canUse(ownerPlayer.alliance, actor)) {
				use(actor);
			}
		}
	}

	public virtual void use(Actor actor) {
		destroySelf(doRpcEvenIfNotOwned: true);
	}

	public virtual bool canUse(int alliance, Actor actor) {
		if (!teamOnly) {
			return true;
		}
		return (
			actor is Character character && character.player.alliance == alliance ||
			actor is Maverick maverick && maverick.player.alliance == alliance
		);
	}

	// Net data.
	public override int getSerialPlayerID() => ownerPlayer.id;
	public override int getSerialCID() => (int)cActorId;
	public override byte[] getSerialExtra() => [
		(byte)(teamOnly ? 1 : 0), (byte)(spawnUp ? 1 : 0)
	];
}
