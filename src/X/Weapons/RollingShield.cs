using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RollingShield : Weapon {
	public static RollingShield netWeapon = new();
	public bool freeAmmoNextCharge;

	public RollingShield() : base() {
		index = (int)WeaponIds.RollingShield;
		killFeedIndex = 3;
		weaponBarBaseIndex = 3;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 3;
		weaknessIndex = 6;
		shootSounds = new string[] { "rollingShield", "rollingShield", "rollingShield", "" };
		fireRate = 45;
		damage = "2/1";
		effect = "Mobile Shield That Deletes Projectiles.";
		hitcooldown = "0/0.33";	
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) {
			if (freeAmmoNextCharge) {
				freeAmmoNextCharge = false;
				return 0;
			}
			return 8;
		}
		return 1;
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new RollingShieldProj(pos, xDir, mmx, player, player.getNextActorNetId(), true);	
		} else {
			if (mmx.chargedRollingShieldProj?.destroyed == false && args.Length >= 1 && args[1] != 0) {
				mmx.specialBuster.shoot(character, [3, 1]);
				freeAmmoNextCharge = true;
			} else {
				mmx.chargedRollingShieldProj = new RollingShieldProjCharged(
					pos, xDir, mmx, player, player.getNextActorNetId(), true
				);
			}
		}
	}
}


public class RollingShieldProj : Projectile {
	public RollingShieldProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "rolling_shield", netId, player	
	) {
		weapon = RollingShield.netWeapon;
		damager.damage = 2;
		projId = (int)ProjIds.RollingShield;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		useGravity = true;
		if (collider != null) {
			collider.wallOnly = true;
		}
		vel.x = 0;
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new RollingShieldProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		if (!ownedByLocalPlayer) {
			base.update();
			return;
		}

		move(new Point(xDir * 200, 0));
		if (Global.level.checkTerrainCollisionOnce(this, 0, -1) == null) {
			var collideData = Global.level.checkTerrainCollisionOnce(this, xDir, 0, vel);
			if (collideData?.hitData?.normal != null && !(collideData.hitData.normal.Value.isAngled())) {
				xDir *= -1;
			}
		} else {
			//this.vel.x = 0;
		}

		base.update();

		if (time > 1.5) {
			destroySelf(fadeSprite, fadeSound);
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (damagable is not TorpedoProjX or TorpedoProjChargedX or 
			TorpedoProjChargedOcto or TorpedoProjMech
		) {
			base.onHitDamagable(damagable);
		}
	}
}

public class RollingShieldProjCharged : Projectile {
	public MegamanX mmx = null!;
	public LoopingSound? rollingShieldSound;
	public float ammoDecCooldown = 0;
	public RollingShieldProjCharged(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "rolling_shield_charge_flash", netId, player	
	) {
		weapon = RollingShield.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 20;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.RollingShieldCharged;
		fadeSprite = "rolling_shield_charge_break";
		fadeSound = "hit";
		useGravity = false;
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		rollingShieldSound = new LoopingSound("rollingShieldCharge", "rollingShieldChargeLoop", this);
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		neverReflect = true;
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new RollingShieldProjCharged(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			if (rollingShieldSound != null) {
				rollingShieldSound.play();
			}
			return;
		}
		// In case it gets reflected (somehow) it implodes.
		// This to prevent it from killing X when reflected.
		if (mmx?.player != owner) {
			destroySelf();
			return;
		}
		if (isAnimOver() && sprite.name == "rolling_shield_charge_flash") {
			changeSprite("rolling_shield_charge", true);
		}
		if (mmx?.currentWeapon is not RollingShield { ammo: >0 }) {
			destroySelf();
		}
		if (rollingShieldSound != null) {
			rollingShieldSound.play();
		}
		changePos(mmx?.getCenterPos() ?? new Point(0,0));
		if (ammoDecCooldown > 0) {
			ammoDecCooldown -= speedMul;
			if (ammoDecCooldown <= 0) ammoDecCooldown = 0;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (mmx is not null) {
			base.onHitDamagable(mmx);
		}
		decAmmo(1);
	}

	public void decAmmo(float amount = 1) {
		if (mmx?.currentWeapon is RollingShield && ammoDecCooldown == 0) {
			ammoDecCooldown = damager.hitCooldown;
			mmx?.currentWeapon?.addAmmo(-amount, damager.owner);
		}
	}

	public override void onDestroy() {
		if (damager.owner.character != null) {
			if (mmx is not null) {
				mmx.chargedRollingShieldProj = null;
			}
		}
		if (rollingShieldSound != null) {
			rollingShieldSound.destroy();
			rollingShieldSound = null;
		}
	}
}
