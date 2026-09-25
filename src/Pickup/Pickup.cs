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
	public Point rsVel = new Point(0, -300);
	public bool isRushPickup;
	public float time = 600;
	Actor? target;

	public Pickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false, bool spawnUp = false, bool isRushPickup = false
	) : base(
		sprite, pos, netId, ownedByLocalPlayer, false
	) {
		this.teamOnly = teamOnly;
		this.spawnUp = spawnUp;
		this.isRushPickup = isRushPickup;
		canBeLocal = !isRushPickup;
		ownerPlayer = owner;

		if (collider != null) {
			collider.wallOnly = true;
			collider.isTrigger = false;
		}

		this.cActorId = cActorId;
		if (spawnUp) {
			vel.y -= 5 * 60;
		}

		if (teamOnly) {
			vel = rsVel;
		}

		if (!ownedByLocalPlayer && isRushPickup) {
			alpha *= 0.75f;
		}

		if (Global.level.gameMode.isTeamMode && isRushPickup) {
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
		syncOnLateJoin = true;
	}

	public override void update() {
		base.update();

		if (isRushPickup) {
			Helpers.decrementFrames(ref time);
				if (time <= 180) {
					visible = time % 3 == 0;
				if (time <= 0) destroySelf();
			}
		}


		if (!ownedByLocalPlayer) {
			return;
		}

		if (Global.isOnFrameCycle(4) && isRushPickup) {
			var closeActors = Global.level.getTargets(pos, ownerPlayer.alliance, false, 48, includeAllies: true);

			foreach (var actor in closeActors) {
				if (actor is not Character c) continue;
				if (!canUse(c.player.alliance, c)) continue;

				target = c;
				break;
			}
		}

		if (target != null) {	
			moveToPos(target.pos, 300);
		} 
		
		int leeway = 500;

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
		if (other.gameObject is Character chr && chr.ownedByLocalPlayer) {
			if (canUse(ownerPlayer.alliance, chr)) {
				use(chr);
			}
		}
	}

	public virtual void use(Character chr) {
		destroySelf(doRpcEvenIfNotOwned: true);
	}

	public virtual bool canUse(int alliance, Character chr) {
		return !teamOnly || chr.player.alliance == ownerPlayer.alliance;	
	}

	// Net data.
	public override int getSerialPlayerID() => ownerPlayer.id;
	public override int getSerialCID() => (int)cActorId;
	public override byte[] getSerialExtra() => [
		(byte)(teamOnly ? 1 : 0), (byte)(spawnUp ? 1 : 0), (byte)(isRushPickup ? 1 : 0)
	];
}
