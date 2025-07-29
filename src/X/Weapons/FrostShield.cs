using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FrostShield : Weapon {
	public static FrostShield netWeapon = new();

	public FrostShield() : base() {
		displayName = "Frost Shield";
		shootSounds = ["frostShield", "frostShield", "frostShield", "frostShieldCharged"];
		fireRate = 60;
		switchCooldown = 45;
		index = (int)WeaponIds.FrostShield;
		weaponBarBaseIndex = 23;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 23;
		killFeedIndex = 46;
		weaknessIndex = (int)WeaponIds.ParasiticBomb;
		damage = "2+2/3+3";
		hitcooldown = "0/60";
		flinch = "0/26-26";
		maxAmmo = 16;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) { return 4; }
		return 1;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (chargeLevel < 3) {
			new FrostShieldProj(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		} else {
			if (character.isUnderwater() == true) {
				new FrostShieldProjPlatform(pos, xDir, mmx, player, player.getNextActorNetId(), true);
			} else {
				if (mmx.chargedFrostShield?.destroyed == false) {
					mmx.chargedFrostShield.destroySelf();
				}
				mmx.chargedFrostShield = new FrostShieldProjCharged(
					pos, xDir, mmx, player, player.getNextActorNetId(), true
				);
			}
		}
	}
}

public class FrostShieldProj : Projectile {
	int state = 0;
	float stateTime;
	public Anim exhaust;
	public bool noSpawn;
	public FrostShieldProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "frostshield_start", netId, player
	) {
		weapon = FrostShield.netWeapon;
		damager.damage = 2;
		vel = new Point(3 * xDir, 0);
		maxTime = 3;
		projId = (int)ProjIds.FrostShield;
		destroyOnHit = true;
		exhaust = new Anim(pos, "frostshield_exhaust", xDir, null, false);
		if (Global.level.server?.customMatchSettings?.frostShieldNerf == false) {
			isShield = true;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostShieldProj(
			arg.pos, arg.xDir, arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		exhaust.pos = pos;
		exhaust.xDir = xDir;

		if (state == 0) {
			stateTime += Global.spf;
			if (stateTime >= 0.5f) {
				state = 1;
				changeSprite("frostshield_proj", true);
			}
		} else if (state == 1) {
			vel.x += Global.spf * 200 * xDir;
			if (MathF.Abs(vel.x) > 150) vel.x = 150 * xDir;
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}

	public void shatter() {
		breakFreeze(owner);
		if (ownedByLocalPlayer && noSpawn == false) {
			new FrostShieldProjAir(pos, -xDir, vel.x, this, owner, owner.getNextActorNetId(), rpc: true);
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		exhaust?.destroySelf();
		shatter();
	}
}

public class FrostShieldProjAir : Projectile {
	public FrostShieldProjAir(
		Point pos, int xDir, float xVel, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "frostshield_air", netId, player
	) {
		weapon = FrostShield.netWeapon;
		maxTime = 3;
		projId = (int)ProjIds.FrostShieldAir;
		useGravity = true;
		destroyOnHit = false;
		if (collider != null) { collider.wallOnly = true; }
		canBeLocal = false; // TODO: Allow local.
		vel = new Point(-xVel * 0.5f, -150);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)(xVel + 128));
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostShieldProjAir(
			arg.pos, arg.xDir, arg.extraData[0] - 128,
			arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		var wall = Global.level.checkTerrainCollisionOnce(this, vel.x * Global.spf, vel.y * Global.spf, vel);
		if (wall != null && wall.gameObject is Wall) {
			vel.x *= -1;
		}
		if (!ownedByLocalPlayer) return;
		if (grounded) {
			destroySelf();
			new FrostShieldProjGround(pos, xDir, this, owner, owner.getNextActorNetId(), rpc: true);
		}
	}
}

public class FrostShieldProjGround : Projectile, IDamagable {
	float health = 4;
	
	public FrostShieldProjGround(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "frostshield_ground_start", netId, player
	) {
		weapon = FrostShield.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		vel = new Point(0 * xDir, 0);
		maxTime = 5;
		projId = (int)ProjIds.FrostShieldGround;
		destroyOnHit = true;
		if (Global.level.server?.customMatchSettings?.frostShieldNerf == false) {
			isShield = true;
		}
		playSound("frostShield");
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostShieldProjGround(
			arg.pos, arg.xDir, arg.owner, arg.player, arg.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		moveWithMovingPlatform();
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damagerAlliance != owner.alliance;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public bool isInvincible(Player attacker, int? projId) {
		if (projId == null) {
			return true;
		}
		return !Damager.canDamageFrostShield(projId.Value);
	}

	public bool isPlayableDamagable() {
		return false;
	}

	public override void onDestroy() {
		base.onDestroy();
		breakFreeze(owner);
	}
}

public class FrostShieldProjCharged : Projectile {
	public Character? character;
	public FrostShieldProjCharged(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "frostshield_charged_start", netId, player
	) {
		weapon = FrostShield.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 60;
		damager.flinch = Global.defFlinch;
		vel = new Point(300 * xDir, 0);
		maxTime = 5;
		projId = (int)ProjIds.FrostShieldCharged;
		destroyOnHit = false;
		shouldVortexSuck = false;
		character = player.character;
		if (Global.level.server?.customMatchSettings?.frostShieldChargedNerf == false) {
			isShield = true;	
		} else if (Global.level.server?.customMatchSettings?.frostShieldChargedNerf == true) {
			isShield = false;
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostShieldProjCharged(
			arg.pos, arg.xDir, arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (isAnimOver()) {
			if (character?.charState is Dash || character?.charState is AirDash) {
				if (damager.damage != 3) updateDamager(3);
			} else {
				if (damager.damage != 0) updateDamager(0);
			}
		}

		if (character == null || character.destroyed) {
			destroySelf();
			return;
		}

		if (frameTime > 2 && character.player.input.isPressed(Control.Shoot, character.player)) {
			destroySelf();
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (character != null) {
			changePos(character.getNullableShootPos() ?? character.pos.addxy(14, -18));
			xDir = character.getShootXDir();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		breakFreeze(owner);
		if (!ownedByLocalPlayer) return;

		if (owner.character is MegamanX mmx && mmx.chargedFrostShield == this) {
			mmx.chargedFrostShield = null;
		}
		new FrostShieldProjChargedGround(pos, character?.xDir ?? 1, this, owner, owner.getNextActorNetId(), rpc: true);
	}
}

public class FrostShieldProjChargedGround : Projectile {
	public Anim slideAnim;
	public Character? character;
	public FrostShieldProjChargedGround(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "frostshield_charged_ground", netId, player
	) {
		weapon = FrostShield.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 60;
		damager.flinch = Global.defFlinch;
		maxTime = 4;
		projId = (int)ProjIds.FrostShieldChargedGrounded;
		destroyOnHit = true;
		shouldVortexSuck = false;
		character = player.character;
		useGravity = true;
		if (Global.level.server?.customMatchSettings?.frostShieldChargedNerf == false) {
			isShield = true;	
		} else if (Global.level.server?.customMatchSettings?.frostShieldChargedNerf == true) {
			isShield = false;
		}
		vel = new Point(xDir * 150, -100);
		if (collider != null) { collider.wallOnly = true; }
		slideAnim = new Anim(pos, "frostshield_charged_slide", xDir, null, false);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostShieldProjChargedGround(
			arg.pos, arg.xDir, arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		slideAnim.visible = grounded;
		slideAnim.changePos(pos.addxy(-xDir * 5, 0));
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		//if (!other.isGroundHit()) destroySelf();
		if (other.isSideWallHit()) {
			vel.x *= -1;
			xDir *= -1;
			slideAnim.xDir *= -1;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		breakFreeze(owner);
		slideAnim?.destroySelf();
	}
}

public class FrostShieldProjPlatform : Projectile {
	public FrostShieldProjPlatform(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "frostshield_charged_platform", netId, player
	) {
		weapon = FrostShield.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 60;
		damager.flinch = Global.defFlinch;
		maxTime = 8;
		projId = (int)ProjIds.FrostShieldChargedPlatform;
		setIndestructableProperties();
		isShield = true;
		if (collider != null) { collider.wallOnly = true; }
		grounded = false;
		canBeGrounded = false;
		useGravity = false;
		isPlatform = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostShieldProjPlatform(
			arg.pos, arg.xDir, arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (isAnimOver() && isUnderwater()) {
			if (damager.damage != 0) {
				updateLocalDamager(0);
			}
			move(new Point(0, -100));
		}
	}
}
