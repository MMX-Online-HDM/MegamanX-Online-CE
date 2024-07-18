using System;
using System.Collections.Generic;

namespace MMXOnline;

public enum VileFlamethrowerType {
	WildHorseKick,
	SeaDragonRage,
	DragonsWrath,
}

public abstract class VileFlamethrower : Weapon {
	public string projSprite = "";
	public string projFadeSprite = "";
	public int projId;

	public VileFlamethrower() : base() {
		rateOfFire = 1f;
		index = (int)WeaponIds.VileFlamethrower;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootTime == 0 && vile.player.vileAmmo > 0) {
			vile.setVileShootTime(this);
			vile.changeState(new FlamethrowerState(), true);
		}
	}
}

public class WildHorseKick : VileFlamethrower {
	public static WildHorseKick netWeapon = new();

	public WildHorseKick() : base() {
		rateOfFire = 1f;
		index = (int)WeaponIds.VileFlamethrower;
		type = (int)VileFlamethrowerType.WildHorseKick;
	
		displayName = "Wild Horse Kick";
		projSprite = "flamethrower_whk";
		projFadeSprite = "flamethrower_whk_fade";
		projId = (int)ProjIds.WildHorseKick;
		description = new string[] { "Shoot jets of flame from your leg.", "Strong, but not energy efficient." };
		killFeedIndex = 117;
		vileWeight = 2;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 8;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootTime == 0 && vile.player.vileAmmo > 0) {
			vile.setVileShootTime(this);
			vile.changeState(new FlamethrowerState(), true);
		}
	}
}


public class SeaDragonRage : VileFlamethrower {
	public static SeaDragonRage netWeapon = new();

	public SeaDragonRage() : base() {
		rateOfFire = 1f;
		index = (int)WeaponIds.VileFlamethrower;
		type = (int)VileFlamethrowerType.SeaDragonRage;
	
		displayName = "Sea Dragon's Rage";
		projSprite = "flamethrower_sdr";
		projFadeSprite = "flamethrower_sdr_fade";
		projId = (int)ProjIds.SeaDragonRage;
		description = new string[] { "This powerful flamethrower can freeze", "enemies and even be used underwater." };
		killFeedIndex = 119;
		vileWeight = 4;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 5;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootTime == 0 && vile.player.vileAmmo > 0) {
			vile.setVileShootTime(this);
			vile.changeState(new FlamethrowerState(), true);
		}
	}
}

public class DragonsWrath : VileFlamethrower {
	public static DragonsWrath netWeapon = new();

	public DragonsWrath() : base() {
		rateOfFire = 1f;
		index = (int)WeaponIds.VileFlamethrower;
		type = (int)VileFlamethrowerType.DragonsWrath;
	
		displayName = "Dragon's Wrath";
		projSprite = "flamethrower_dw";
		projFadeSprite = "flamethrower_dw_fade";
		description = new string[] { "A long arching flamethrower,", "useful against faraway enemies." };
		killFeedIndex = 118;
		projId = (int)ProjIds.DragonsWrath;
		vileWeight = 3;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 24;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootTime == 0 && vile.player.vileAmmo > 0) {
			vile.setVileShootTime(this);
			vile.changeState(new FlamethrowerState(), true);
		}
	}
}

public class FlamethrowerState : CharState {
	bool isGrounded;
	public float shootTime;
	public Point shootPOI = new Point(-1, -1);
	public Point groundShotPOI = new Point(12, -11);
	Vile vile = null!;

	public FlamethrowerState() : base("flamethrower") {
		useGravity = false;
	}

	public override void update() {
		base.update();
		character.turnToInput(player.input, player);

		shootTime += Global.speedMul;
		if (shootTime >= 4) {
			if (!vile.tryUseVileAmmo(2)) {
				character.changeToIdleOrFall();
				return;
			}
			shootTime = 0;
			character.playSound("flamethrower");
			Point poiPos;
			if (!isGrounded) {
				poiPos = character.getPOIPos(shootPOI);
			} else {
				poiPos = (character.getFirstPOI() ?? character.getPOIPos(groundShotPOI));

			}
			new FlamethrowerProj(
				vile.flamethrowerWeapon,
				poiPos,
				character.xDir, isGrounded, player,
				player.getNextActorNetId(), sendRpc: true
			);
		}

		if (character.loopCount >= 5 || !player.input.isHeld(Control.Special1, player)) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		vile = character as Vile ?? throw new NullReferenceException();
		character.stopMovingWeak();
		if (character.grounded && character.vel.y >= 0) {
			character.changeSpriteFromName("crouch_flamethrower", true);
			isGrounded = true;
		}
	}
}

public class FlamethrowerProj : Projectile {
	bool groundedVariant;

	public FlamethrowerProj(
		VileFlamethrower weapon, Point pos, int xDir,
		bool groundedVariant, Player player, ushort netProjId, bool sendRpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, weapon.projSprite, 0, 0.1f, netProjId, player.ownedByLocalPlayer
	) {
		projId = weapon.projId;
		fadeSprite = weapon.projFadeSprite;
		destroyOnHit = true;
		this.groundedVariant = groundedVariant;

		maxTime = 0.3f;
		if (weapon.type == (int)VileFlamethrowerType.SeaDragonRage) {
			maxTime = 0.2f;
		}
		if (!groundedVariant) {
			vel = new Point(xDir, 2f);
			vel = vel.normalize().times(350);
			if (weapon.type == (int)VileFlamethrowerType.DragonsWrath) {
				vel.x = xDir * 350;
				vel.y = 225;
			}
		} else {
			vel = new Point(xDir, -0.5f);
			vel = vel.normalize().times(350);
			if (weapon.type == (int)VileFlamethrowerType.DragonsWrath) {
				vel.x = xDir * 350;
				vel.y = -250;
				maxTime = 0.4f;
			}
		}
		angle = vel.angle;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)(groundedVariant ? 1 : 0));
		}
	}

	public override void update() {
		base.update();
		if (weapon.type != (int)VileFlamethrowerType.SeaDragonRage && isUnderwater()) {
			destroySelf();
			return;
		}
		if (!groundedVariant) {
			if (weapon.type == (int)VileFlamethrowerType.DragonsWrath) {
				vel.y -= Global.speedMul * 13.4f;
			} else {
				vel.x *= 0.9f;
			}
		} else {
			if (weapon.type == (int)VileFlamethrowerType.DragonsWrath) {
				vel.x -= xDir * Global.speedMul * 13.4f;
			} else {
				vel.y *= 0.9f;
			}
		}
	}

	public override void onHitWall(CollideData other) {
		if (weapon.type != (int)VileFlamethrowerType.DragonsWrath) {
			destroySelf(fadeSprite, disableRpc: true);
		} else if (vel.y >= 0) {
			vel.y = 0;
		}
	}
}
