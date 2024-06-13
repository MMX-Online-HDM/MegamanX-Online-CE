using System;

namespace MMXOnline;

public enum GroundSpecialType {
	Raijingeki,
	Suiretsusen,
	TBreaker,
	MegaPunch
}

public class Raijingeki : CharState {
	bool playedSoundYet;
	bool isAlt;
	public Raijingeki(bool isAlt) : base(isAlt ? "raijingeki2" : "raijingeki", "", "") {
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
			character.changeState(new Idle());
		}
	}
}

public class SuiretsusanState : CharState {
	public bool isAlt;
	public SuiretsusanState(bool isAlt) : base("spear", "") {
		this.isAlt = isAlt;
	}

	public override void update() {
		base.update();

		if (isAlt) {
			if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
			else character.sprite.frameSpeed = 1;
		}

		var pois = character.sprite.getCurrentFrame().POIs;
		if (pois != null && pois.Count > 0 && !once) {
			once = true;
			new SuiretsusenProj(
				character.getFirstPOIOrDefault(),
				character.xDir, player, player.getNextActorNetId(), sendRpc: true
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
		Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false
	) : base(
		SuiretsusenWeapon.staticWeapon, pos, xDir, 200, 6, player, "spear_proj",
		Global.defFlinch, 0.75f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.SuiretsusanProj;
		destroyOnHit = false;
		shouldVortexSuck = false;
		shouldShieldBlock = false;
		isMelee = true;
		if (player?.character != null) {
			owningActor = player.character;
		}

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf();
		}
	}
}

public class TBreakerState : CharState {
	public float dashTime = 0;
	public Projectile fSplasherProj;
	public bool isAlt;

	public TBreakerState(bool isAlt) : base("tbreaker") {
		this.isAlt = isAlt;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}

	public override void update() {
		base.update();

		if (isAlt) {
			if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
			else character.sprite.frameSpeed = 1;
		}

		var pois = character.sprite.getCurrentFrame().POIs;
		if (pois != null && pois.Count > 0 && !once) {
			once = true;
			Point poi = character.getFirstPOIOrDefault();
			var rect = new Rect(poi.x - 10, poi.y - 5, poi.x + 10, poi.y + 5);
			Shape shape = rect.getShape();
			var hit = Global.level.checkCollisionShape(shape, null);
			if (hit != null && hit.gameObject is Wall) {
				new TBreakerProj(
					poi, character.xDir,
					player, player.getNextActorNetId(), sendRpc: true
				);
			}
		}

		if (character.isAnimOver()) {
			character.changeState(new Idle(), true);
		}
	}
}

public class TBreakerProj : Projectile {
	public TBreakerProj(
		Point pos, int xDir,
		Player player, ushort netProjId, bool sendRpc = false
	) : base(
		TBreakerWeapon.staticWeapon, pos, xDir, 0, 0, player, "tbreaker_shockwave",
		0, 0.15f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.TBreakerProj;
		destroyOnHit = false;
		shouldVortexSuck = false;
		shouldShieldBlock = false;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void onStart() {
		base.onStart();
		shakeCamera(sendRpc: true);
		playSound("crash", sendRpc: true);
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf();
		}
	}
}
