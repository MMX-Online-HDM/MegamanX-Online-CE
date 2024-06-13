using System;

namespace MMXOnline;

public enum GroundSpecialType {
	Raijingeki,
	Suiretsusen,
	TBreaker,
	MegaPunch
}

public class RaijingekiWeapon : Weapon {
	public static RaijingekiWeapon staticWeapon = new();

	public RaijingekiWeapon() : base() {
		//damager = new Damager(player, 2, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.Raijingeki;
		weaponBarBaseIndex = 22;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 10;
		type = (int)GroundSpecialType.Raijingeki;
		displayName = "Raijingeki";
		description = new string[] { "Powerful lightning attack." };
	}

	public static Weapon getWeaponFromIndex(int index) {
		return index switch {
			(int)GroundSpecialType.Raijingeki => new RaijingekiWeapon(),
			(int)GroundSpecialType.Suiretsusen => new SuiretsusenWeapon(),
			(int)GroundSpecialType.TBreaker => new TBreakerWeapon(),
			_ => throw new Exception("Invalid Zero air special weapon index!")
		};
	}

	public override void attack(Character character) {
		character.changeState(new Raijingeki(false), true);
	}

	public override void attack2(Character character) {
		character.changeState(new Raijingeki(true), true);
	}
}

public class Raijingeki2Weapon : Weapon {
	public static Raijingeki2Weapon staticWeapon = new();

	public Raijingeki2Weapon() : base() {
		//damager = new Damager(player, 2, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.Raijingeki2;
		weaponBarBaseIndex = 40;
		killFeedIndex = 35;
	}
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


public class SuiretsusenWeapon : Weapon {
	public static SuiretsusenWeapon staticWeapon = new();

	public SuiretsusenWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.Suiretsusen;
		killFeedIndex = 110;
		type = (int)GroundSpecialType.Suiretsusen;
		displayName = "Suiretsusen";
		description = new string[] { "Water element glaive with good reach." };
	}

	public override void attack(Character character) {
		if (shootTime == 0) {
			shootTime = 0;
			character.changeState(new SuiretsusanState(false), true);
		}
	}

	public override void attack2(Character character) {
		if (shootTime == 0) {
			shootTime = 0;
			character.changeState(new SuiretsusanState(true), true);
		}
	}
}

public class SuiretsusanState : CharState {
	public bool isAlt;
	public Zero zero;

	public SuiretsusanState(bool isAlt) : base("spear", "") {
		this.isAlt = isAlt;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero = character as Zero;
	}

	public override void update() {
		base.update();

		if (isAlt) {
			if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
			else character.sprite.frameSpeed = 1;
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class TBreakerWeapon : Weapon {
	public static TBreakerWeapon staticWeapon = new();

	public TBreakerWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.TBreaker;
		killFeedIndex = 107;
		type = (int)GroundSpecialType.TBreaker;
		displayName = "T-Breaker";
		description = new string[] { "A mighty hammer that can shatter barriers." };
	}

	public override void attack(Character character) {
		if (shootTime == 0) {
			shootTime = 0f;
			character.changeState(new TBreakerState(false), true);
		}
	}

	public override void attack2(Character character) {
		if (shootTime == 0) {
			shootTime = 0f;
			character.changeState(new TBreakerState(true), true);
		}
	}
}

public class TBreakerState : CharState {
	public float dashTime = 0;
	public Projectile fSplasherProj;
	public bool isAlt;
	public Zero zero;

	public TBreakerState(bool isAlt) : base("tbreaker", "") {
		this.isAlt = isAlt;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero = character as Zero;
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
