using System.Collections.Generic;

namespace MMXOnline;

public class IceGattling : AxlWeapon {
	public IceGattling(int altFire) : base(altFire) {
		shootSounds = new string[] { "iceGattling", "iceGattling", "iceGattling", "gaeaShield" };
		fireRate = 6;
		index = (int)WeaponIds.IceGattling;
		weaponBarBaseIndex = 37;
		weaponSlotIndex = 57;
		killFeedIndex = 72;
		rechargeAmmoCooldown = 300;
		altRechargeAmmoCooldown = 300;
		sprite = "axl_arm_icegattling";
		flashSprite = "axl_pistol_flash";
		chargedFlashSprite = "axl_pistol_flash";
		if (altFire == 1) {
			altRechargeAmmoCooldown = 0;
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel == 3) {
			return 8;
		}
		return 0.5f;
	}
	public void useAmmo(Axl axl, int chargelevel) {
		if (axl.axlWeapon != null) {
			axl.recoilTime = 0.2f;
			axl.playSound(shootSounds[chargelevel]);
			float ammoUsage = getAmmoUsage(chargelevel);
			axl.axlWeapon.ammo -= ammoUsage;
			if (ammo < 0) ammo = 0;
		}
    }
	public void shootLogic(Axl axl) {
		if (axl.axlWeapon is not IceGattling) return;
		if (axl.axlWeapon.shootCooldown > 0) return;
		if (!axl.canShoot()) {
			axl.isRevving = false;
			axl.gaeaHeld = false;
			return;
		}
		Point bulletPos = axl.getAxlBulletPos();
		Point cursorPos = axl.getCorrectedCursorPos();
		var dirTo = bulletPos.directionTo(cursorPos);
		float aimAngle = dirTo.angle;
		bool isAltRev = (axl.altShootHeld && axl.loadout.iceGattlingAlt == 1);
		bool isGaea = (axl.altShootPressed && axl.loadout.iceGattlingAlt == 0);

		if (axl.shootHeld || isAltRev) {
			axl.isRevving = true;
			axl.revTime += Global.spf * 2 * (axl.isWhiteAxl() ? 10 : (isAltRev ? 2 : 1));
			if (axl.revTime > 1) axl.revTime = 1;
		} else {
			axl.isRevving = false;
		}

		if (axl.shootHeld && axl.revTime >= 1) {
			axlGetProjectile(
				this, bulletPos, axl.axlXDir, axl.player, aimAngle, axl.axlCursorTarget,
				axl.axlHeadshotTarget, cursorPos, 0, axl.player.getNextActorNetId()
			);
			axl.axlWeapon.shootCooldown = fireRate;
			useAmmo(axl, 0);
		}

		if (isGaea && axl.gaeaShield == null && axl.axlWeapon.ammo > 8) {
			axl.gaeaHeld = true;
			axlGetAltProjectile(
				this, bulletPos, axl.axlXDir, axl.player, aimAngle, axl.axlCursorTarget,
				axl.axlHeadshotTarget, cursorPos, 0, axl.player.getNextActorNetId()
			);
			useAmmo(axl, 3);
		} else if (axl.altShootPressed && axl.gaeaShield != null) {
			axl.gaeaHeld = false;
		}
     }
	public override void axlGetAltProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
		 IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		if (!player.ownedByLocalPlayer) return;
		if (player.character is not Axl axl) return;
		axl.gaeaShield = new GaeaShieldProj(weapon, bulletPos, xDir, player, netId, rpc: true);
		RPC.playSound.sendRpc(shootSounds[3], player.character?.netId);
    }

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
	 	IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		if (!player.ownedByLocalPlayer) return; 		
		Point bulletDir = Point.createFromAngle(angle);
		Projectile? bullet = null;
		bullet = new IceGattlingProj(weapon, bulletPos, xDir, player, bulletDir, netId);
		RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
	}
}


public class IceGattlingProj : Projectile {
	public float sparkleTime = 0;
	public IceGattlingProj(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, ushort netProjId) :
		base(weapon, pos, xDir, 400, 1, player, "icegattling_proj", 0, 0.1f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.4f;
		reflectable = true;
		vel.x = bulletDir.x * speed;
		vel.y = bulletDir.y * speed;
		projId = (int)ProjIds.IceGattling;
		fadeSprite = "icegattling_proj_fade";
		updateAngle();
		if ((player.character as Axl)?.isWhiteAxl() == true) {
			projId = (int)ProjIds.IceGattlingHyper;
		}
	}

	public void updateAngle() {
		byteAngle = vel.byteAngle;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}

	public override void update() {
		/*
		if (getHeadshotVictim(owner, out IDamagable victim, out Point? hitPoint))
		{
			damager.applyDamage(victim, false, weapon, this, (int)ProjIds.IceGattlingHeadshot);
			damager.damage = 0;
			destroySelf();
			return;
		}
		*/

		base.update();

		sparkleTime += Global.spf;
		if (sparkleTime > 0.05) {
			sparkleTime = 0;
			new Anim(pos, "shotgun_ice_sparkles", 1, null, true);
		}
	}
}


public class GaeaShieldProj : Projectile {
	public Axl? axl;
	public GaeaShieldProj(
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 0, 0, player, "gaea_shield_proj", 0, 1, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 10;
		if (ownedByLocalPlayer) {
			axl = (player.character as Axl);
		}
		fadeSound = "explosion";
		fadeSprite = "explosion";
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.GaeaShield;
		destroyOnHit = false;
		shouldVortexSuck = false;
		isReflectShield = true;
		isShield = true;
		shouldShieldBlock = false;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (axl == null || axl.destroyed) {
			destroySelf();
			return;
		}

		if (axl.player.input.isPressed(Control.Special1, axl.player)) {
			destroySelf();
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		if (!ownedByLocalPlayer ||
			destroyed ||
			axl == null
		) {
			return;
		}

		Point bulletPos = axl.getAxlBulletPos(1);
		byteAngle = axl.getShootAngle(true);
		changePos(bulletPos);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (axl != null) {
			axl.gaeaShield = null;
		}
	}
}
