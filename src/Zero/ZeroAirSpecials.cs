using System;

namespace MMXOnline;

public enum AirSpecialType {
	Kuuenzan,
	FSplasher,
	Hyoroga
}

public class KuuenzanWeapon : Weapon {
	public static KuuenzanWeapon staticWeapon = new();

	public KuuenzanWeapon() : base() {
		//damager = new Damager(player, 3, 0, 0.5f);
		index = (int)WeaponIds.Kuuenzan;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 48;
		killFeedIndex = 121;
		type = (int)AirSpecialType.Kuuenzan;
		displayName = "Kuuenzan";
		description = new string[] { "Standard spin attack in the air." };
		damage = "1";
		hitcooldown = "0.125";
		Flinch = "0";
		effect = "None.";
	}

	public static Weapon getWeaponFromIndex(int index) {
		return index switch {
			(int)AirSpecialType.Kuuenzan => new KuuenzanWeapon(),
			(int)AirSpecialType.FSplasher => new FSplasherWeapon(),
			(int)AirSpecialType.Hyoroga => new HyorogaWeapon(),
			_ => throw new Exception("Invalid Zero air special weapon index!")
		};
	}
}

public class FSplasherWeapon : Weapon {
	public static FSplasherWeapon staticWeapon = new();

	public FSplasherWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.FSplasher;
		killFeedIndex = 109;
		type = (int)AirSpecialType.FSplasher;
		displayName = "Hisuishou";
		description = new string[] { "A Mobile Aereal slightly faster Dash." };
		damage = "2";
		hitcooldown = "0.5";
		Flinch = "0";
		effect = "None.";
	}

	public override void attack(Character character) {
		if (character.dashedInAir > 0) return;
		if (shootCooldown > 0) return;
		shootCooldown = 1;
		character.changeState(new FSplasherState(), true);
	}
}

public class FSplasherState : CharState {
	public float dashTime = 0;
	public Projectile fSplasherProj;
	Zero zero;

	public FSplasherState() : base("dash", "") {
		enterSound = "fsplasher";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero = character as Zero;

		character.isDashing = true;
		character.useGravity = false;
		character.vel = new Point(0, 0);
		character.dashedInAir++;
		fSplasherProj = new FSplasherProj(
			character.pos, character.xDir,
			player, player.getNextActorNetId(), sendRpc: true
		);
	}

	public override void onExit(CharState newState) {
		character.useGravity = true;
		if (fSplasherProj != null) {
			fSplasherProj.destroySelf();
			fSplasherProj = null;
		}
		zero.airSpecial.shootCooldown = 1;
		base.onExit(newState);
	}

	public override bool canEnter(Character character) {
		if (!base.canEnter(character)) return false;
		return character.flag == null;
	}

	public override void update() {
		base.update();

		float upSpeed = 0;
		var inputDir = player.input.getInputDir(player);
		if (inputDir.y < 0) upSpeed = -1;
		else if (inputDir.y > 0) upSpeed = 1;
		else upSpeed = 0;

		if (fSplasherProj != null) {
			fSplasherProj.incPos(character.deltaPos);
		}

		CollideData collideData = Global.level.checkTerrainCollisionOnce(character, character.xDir, upSpeed);
		if (collideData != null) {
			character.changeState(new Fall(), true);
			return;
		}

		float modifier = 1f;
		dashTime += Global.spf;
		if (dashTime > 0.6) {
			character.changeState(new Fall());
			return;
		}

		var move = new Point(0, 0);
		move.x = character.getDashSpeed() * character.xDir * modifier;
		move.y = upSpeed * 100;
		character.move(move);
		if (stateTime > 0.1) {
			stateTime = 0;
		}
	}
}

public class FSplasherProj : Projectile {
	public FSplasherProj(
		Point pos, int xDir,
		Player player, ushort netProjId, bool sendRpc = false
	) : base(
		FSplasherWeapon.staticWeapon, pos, xDir, 0, 2, player, "fsplasher_proj",
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.FSplasher;
		destroyOnHit = false;
		shouldVortexSuck = false;
		shouldShieldBlock = false;
		canBeLocal = false;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}
}

public class HyorogaWeapon : Weapon {
	public static HyorogaWeapon staticWeapon = new();

	public HyorogaWeapon() : base() {
		//damager = new Damager(player, 3, Global.defFlinch, 0.06f);
		index = (int)WeaponIds.Hyoroga;
		killFeedIndex = 108;
		type = (int)AirSpecialType.Hyoroga;
		displayName = "Hyoroga";
		description = new string[] { "Cling to ceilings and rain down icicles with ATTACK." };
		damage = "4-3";
		hitcooldown = "0.25-0.15";
		Flinch = "0-13";
		effect = "Can Freeze enemies on contact.";
	}

	public override void attack(Character character) {
		//if (character.charState is Fall) return;
		for (int i = 1; i <= 4; i++) {
			CollideData collideData = Global.level.checkTerrainCollisionOnce(character, 0, -10 * i, autoVel: true);
			if (collideData != null && collideData.gameObject is Wall wall
				&& !wall.isMoving && !wall.topWall && collideData.isCeilingHit()
			) {
				character.changeState(new HyorogaStartState(), true);
				return;
			}
		}
	}
}

public class HyorogaStartState : CharState {
	public HyorogaStartState() : base("hyoroga_rise") {
		specialId = SpecialStateIds.HyorogaStart;
	}

	public override void update() {
		base.update();

		if (character.sprite.name == "zero_hyoroga_rise") {
			if (character.deltaPos.isCloseToZero()) {
				character.changeSprite("zero_hyoroga_start", true);
				character.gravityModifier = -1;
				character.useGravity = true;
			}
		} else if (character.sprite.name == "zero_hyoroga_start") {
			if (character.isAnimOver()) {
				character.changeState(new HyorogaState(), true);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel = new Point(0, 0);
		character.gravityModifier = -1;
		character.dashedInAir = 0;
		
	}

	public override void onExit(CharState newState) {
		character.useGravity = true;
		character.gravityModifier = 1;
		base.onExit(newState);
	}
}

public class HyorogaState : CharState {
	Zero? zero;
	public HyorogaState() : base("hyoroga") {
		normalCtrl = true;
	}
	public override void update() {
		base.update();
		if (player.input.isPressed(Control.Special1, player)) {
			character.changeState(new HyorogaStateA(), true);
		}	
		if (player.input.isPressed(Control.Shoot, player) && character.getChargeLevel() >= 1) {
			character.changeState(new HyorogaStateB(), true);	
		}
		if (player.input.isPressed(Control.Jump, player)) {
			character.changeState(new Fall(), true);
		}
		character.turnToInput(player.input, player);
	}	
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel = new Point(0, 0);
		character.useGravity = false;
		character.gravityModifier = 0;
		zero = character as Zero;
	}
	public override void onExit(CharState newState) {
		character.useGravity = true;
		character.gravityModifier = 1;
		base.onExit(newState);
	}
}
public class HyorogaStateA : CharState {
	public HyorogaStateA() : base("hyoroga_attack") {
	}
	public override void update() {
		character.vel = new Point(0, 0);
		character.useGravity = false;
		character.gravityModifier = 0;
		base.update();
		var pois = character.sprite.getCurrentFrame().POIs;	
		if (pois != null && pois.Length > 0) {
			var poi = character.getFirstPOIOrDefault();
			new HyorogaProj(
				poi, 0,
				player, player.getNextActorNetId(), sendRpc: true
			);
			new HyorogaProj(
				poi, 1,
				player, player.getNextActorNetId(), sendRpc: true
			);
			new HyorogaProj(
				poi, -1,
				player, player.getNextActorNetId(), sendRpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeState(new HyorogaState(), true);			
		}
	}
}
public class HyorogaStateB : CharState {
	public bool fired;
	public HyorogaStateB() : base("hyoroga_shoot") {
	}
	public override void update() {
		character.vel = new Point(0, 0);
		character.useGravity = false;
		character.gravityModifier = 0;
		base.update();
		if (!fired && character.frameIndex == 3) {
			fired = true;
			switch (character.getChargeLevel()) {
				case 1:
					character.playSound("buster2X3", sendRpc: true);
					new ZBuster2Proj(character.getShootPos(), character.xDir, 0, 
					player, player.getNextActorNetId(), rpc: true);
					break;
				case 2:
					character.playSound("buster3X3", sendRpc: true);
					new ZBuster3Proj(character.getShootPos(), character.xDir, 0, 
					player, player.getNextActorNetId(), rpc: true);
					break;
				case >=3:
					character.playSound("buster4", sendRpc: true);
					new ZBuster4Proj(character.getShootPos(), character.xDir, 0, 
					player, player.getNextActorNetId(), rpc: true);
					break;
			}
			player.currency--;
			character.stopCharge();		
		}
		if (character.isAnimOver()) {
			character.changeState(new HyorogaState(), true);			
		}
	}
}

public class HyorogaProj : Projectile {
	public HyorogaProj(
		Point pos, int velDir, Player player,
		ushort netProjId, bool sendRpc = false
	) : base(
		HyorogaWeapon.staticWeapon, pos, 1, 0, 3, player, "hyoroga_proj",
		Global.halfFlinch, 0.15f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.HyorogaProj;
		destroyOnHit = true;
		destroyOnHitWall = true;
		this.vel.x = velDir * 250 * 0.375f;
		this.vel.y = 250;
		maxTime = 0.4f;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)(velDir + 128));
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		playSound("iceBreak");
		Anim.createGibEffect("hyoroga_proj_pieces", getCenterPos(), owner);
	}
}
