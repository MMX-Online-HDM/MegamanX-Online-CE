using System;

namespace MMXOnline;

public enum GroundSpecialType {
	Raijingeki,
	Suiretsusen,
	TBreaker,
	MegaPunch
}

public class Raijingeki : ZeroState {
	bool playedSoundYet;
	bool isAlt;

	public Raijingeki(bool isAlt) : base(isAlt ? "raijingeki2" : "raijingeki") {
		this.isAlt = isAlt;
	}

	public override void update() {
		base.update();

		if (isAlt) {
			if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
			else character.sprite.frameSpeed = 1;
		}

		if (character.sprite.frameIndex > 10 && !playedSoundYet) {
			playedSoundYet = true;
			character.playSound("raijingeki", sendRpc: true);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class SuiretsusanState : ZeroState {
	public bool isAlt;

	public SuiretsusanState(bool isAlt) : base("spear") {
		this.isAlt = isAlt;
	}

	public override void update() {
		base.update();

		if (isAlt) {
			if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
			else character.sprite.frameSpeed = 1;
		}

		var pois = character.sprite.getCurrentFrame().POIs;
		if (pois != null && pois.Length > 0 && !once) {
			once = true;
			new SuiretsusenProj(
				character.getFirstPOIOrDefault(), character.xDir,
				zero, player, player.getNextActorNetId(), rpc: true
			);
			character.playSound("spear", sendRpc: true);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class SuiretsusenProj : Projectile {
	public SuiretsusenProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "spear_proj", netId, player
	) {
		weapon = SuiretsusenWeapon.staticWeapon;
		damager.damage = 6;
		damager.hitCooldown = 45;
		damager.flinch = Global.defFlinch;
		vel = new Point(200 * xDir, 0);
		projId = (int)ProjIds.SuiretsusanProj;
		destroyOnHit = false;
		shouldVortexSuck = false;
		shouldShieldBlock = false;
		isMelee = true;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		if (ownerPlayer?.character != null) {
			owningActor = ownerPlayer.character;
		}
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf();
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new SuiretsusenProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class TBreakerState : ZeroState {
	public float dashTime = 0;
	public bool isAlt;
	public TBreakerState(bool isAlt) : base("tbreaker") {
		this.isAlt = isAlt;
	}

	public override void update() {
		base.update();
		if (isAlt) {
			if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
			else character.sprite.frameSpeed = 1;
		}
		var pois = character.sprite.getCurrentFrame().POIs;
		if (pois != null && pois.Length > 0 && !once) {
			once = true;
			Point poi = character.getFirstPOIOrDefault();
			var rect = new Rect(poi.x - 10, poi.y - 5, poi.x + 10, poi.y + 5);
			Shape shape = rect.getShape();
			var hit = Global.level.checkCollisionShape(shape, null);
			if (hit != null && hit.gameObject is Wall) {
				new TBreakerProj(
					poi, character.xDir, zero,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class TBreakerProj : Projectile {
	public TBreakerProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "tbreaker_shockwave", netId, player
	) {
		weapon = TBreakerWeapon.staticWeapon;
		damager.damage = 0;
		damager.hitCooldown = 9;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.TBreakerProj;
		destroyOnHit = false;
		shouldVortexSuck = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TBreakerProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void onStart() {
		base.onStart();
		shakeCamera(sendRpc: true);
		playSound("crashX3", sendRpc: true);
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf();
		}
	}
}
