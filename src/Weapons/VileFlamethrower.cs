using System;
using System.Collections.Generic;

namespace MMXOnline;

public enum VileFlamethrowerType {
	None = -1,
	WildHorseKick,
	SeaDragonRage,
	DragonsWrath,
}

public abstract class VileFlamethrower : Weapon {
	public string projSprite = "";
	public string projFadeSprite = "";
	public int projId;

	public VileFlamethrower() : base() {
		fireRate = 60;
		index = (int)WeaponIds.VileFlamethrower;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown == 0 && vile.energy.ammo > 0) {
			vile.setVileShootTime(this);
			vile.changeState(new FlamethrowerState(), true);
		}
	}
}
public class NoneFlamethrower : VileFlamethrower {
	public static NoneFlamethrower netWeapon = new();
	public NoneFlamethrower() : base() {
		fireRate = 0;
		index = (int)WeaponIds.VileFlamethrower;
		type = (int)VileFlamethrowerType.None;
		displayName = "None";
		killFeedIndex = 126;
		vileWeight = 0;
		damage = "0";
		hitcooldown = "0";
		ammousage = 0;
		fireRate = 0;
		vileWeight = 0;
	}	
}

public class WildHorseKick : VileFlamethrower {
	public static WildHorseKick netWeapon = new();

	public WildHorseKick() : base() {
		fireRate = 60;
		index = (int)WeaponIds.VileFlamethrower;
		type = (int)VileFlamethrowerType.WildHorseKick;
	
		displayName = "Wild Horse Kick";
		projSprite = "flamethrower_whk";
		projFadeSprite = "flamethrower_whk_fade";
		projId = (int)ProjIds.WildHorseKick;
		description = new string[] { "Shoot jets of flame from your leg.", "Strong, but not energy efficient." };
		killFeedIndex = 117;
		vileWeight = 2;
		damage = "1";
		hitcooldown = "0.1";
		effect = "Fire DOT: 0.5";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 8;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown == 0 && vile.energy.ammo > 0) {
			vile.setVileShootTime(this);
			vile.changeState(new FlamethrowerState(), true);
		}
	}
}


public class SeaDragonRage : VileFlamethrower {
	public static SeaDragonRage netWeapon = new();

	public SeaDragonRage() : base() {
		fireRate = 60;
		index = (int)WeaponIds.VileFlamethrower;
		type = (int)VileFlamethrowerType.SeaDragonRage;
	
		displayName = "Sea Dragon's Rage";
		projSprite = "flamethrower_sdr";
		projFadeSprite = "flamethrower_sdr_fade";
		projId = (int)ProjIds.SeaDragonRage;
		description = new string[] { "This powerful flamethrower can freeze", "enemies and even be used underwater." };
		killFeedIndex = 119;
		vileWeight = 4;
		damage = "1";
		hitcooldown = "0.1";
		effect = "Stack hits to freeze.";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 5;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown == 0 && vile.energy.ammo > 0) {
			vile.setVileShootTime(this);
			vile.changeState(new FlamethrowerState(), true);
		}
	}
}

public class DragonsWrath : VileFlamethrower {
	public static DragonsWrath netWeapon = new();

	public DragonsWrath() : base() {
		fireRate = 60;
		index = (int)WeaponIds.VileFlamethrower;
		type = (int)VileFlamethrowerType.DragonsWrath;
	
		displayName = "Dragon's Wrath";
		projSprite = "flamethrower_dw";
		projFadeSprite = "flamethrower_dw_fade";
		description = new string[] { "A long arching flamethrower,", "useful against faraway enemies." };
		killFeedIndex = 118;
		projId = (int)ProjIds.DragonsWrath;
		vileWeight = 3;
		damage = "1";
		hitcooldown = "0.1";
		effect = "None.";
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 24;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown == 0 && vile.energy.ammo > 0) {
			vile.setVileShootTime(this);
			vile.changeState(new FlamethrowerState(), true);
		}
	}
}

public class FlamethrowerState : VileState {
	bool isGrounded;
	public float shootTime;
	public Point shootPOI = new Point(-1, -1);
	public Point groundShotPOI = new Point(12, -11);

	public FlamethrowerState() : base("flamethrower") {
		useGravity = false;
		useDashJumpSpeed = true;
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
			if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.DragonsWrath) {
				new FlamethrowerDragonsWrath(
					poiPos, character.xDir, isGrounded, vile, player,
					player.getNextActorNetId(), rpc: true
				);
			}
			else if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.WildHorseKick) {
				new FlamethrowerWildHorseKick(
					poiPos, character.xDir, isGrounded, vile, player,
					player.getNextActorNetId(), rpc: true
				);
			}
			else if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.SeaDragonRage) {
				new FlamethrowerSeaDragonRage(
					poiPos, character.xDir, isGrounded, vile, player,
					player.getNextActorNetId(), rpc: true
				);
			}
		}

		if (character.loopCount >= 5 || !player.input.isHeld(Control.Special1, player)) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMovingWeak();
		if (character.grounded && character.vel.y >= 0) {
			character.changeSpriteFromName("crouch_flamethrower", true);
			isGrounded = true;
		}
	}
}
public class FlamethrowerWildHorseKick : Projectile {
	public bool groundedVariant;
	public FlamethrowerWildHorseKick(
		Point pos, int xDir, bool groundedVariant,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "flamethrower_whk", netId, player
	) {
		weapon = WildHorseKick.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 6;
		maxTime = 0.3f;
		destroyOnHit = true;
		destroyOnHitWall = true;
		this.groundedVariant = groundedVariant;
		angle = vel.angle;
		fadeOnAutoDestroy = true;
		fadeSprite = "flamethrower_whk_fade";
		projId = (int)ProjIds.WildHorseKick;
		if (!groundedVariant) {
			vel = new Point(100 * xDir, 350);
		} else {
			vel = new Point(350*xDir, -150);
			maxTime = 0.35f;
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)(groundedVariant ? 1 : 0));
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FlamethrowerWildHorseKick(
			args.pos, args.xDir, args.extraData[0] == 1,
			args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (!groundedVariant) {
			vel.x -= 5*xDir;
		} else {
			vel.y += 11;
		}
	}	
}
public class FlamethrowerDragonsWrath : Projectile {
	public bool groundedVariant;
	public FlamethrowerDragonsWrath(
		Point pos, int xDir, bool groundedVariant,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "flamethrower_dw", netId, player
	) {
		weapon = DragonsWrath.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 6;
		maxTime = 0.4f;
		destroyOnHit = true;
		destroyOnHitWall = true;
		this.groundedVariant = groundedVariant;
		angle = vel.angle;
		fadeOnAutoDestroy = true;
		fadeSprite = "flamethrower_dw_fade";
		projId = (int)ProjIds.DragonsWrath;
		if (!groundedVariant) {
			vel.x = xDir * 350;
			vel.y = 225;
		} else {
			vel.x = xDir * 450;
			vel.y = -20;
			maxTime = 0.4f;
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)(groundedVariant ? 1 : 0));
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FlamethrowerDragonsWrath(
			args.pos, args.xDir, args.extraData[0] == 1,
			args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (!groundedVariant) {
			vel.y -= Global.speedMul * 13.4f;
		} else {
			vel.x -= 12*xDir;
			vel.y -= 10;
		}
	}	
}
public class FlamethrowerSeaDragonRage : Projectile {
	public bool groundedVariant;
	public FlamethrowerSeaDragonRage(
		Point pos, int xDir, bool groundedVariant,
		Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "flamethrower_sdr", netId, player
	) {
		weapon = SeaDragonRage.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 6;
		maxTime = 0.25f;
		destroyOnHit = true;
		destroyOnHitWall = true;
		this.groundedVariant = groundedVariant;
		angle = vel.angle;
		fadeOnAutoDestroy = true;
		fadeSprite = "flamethrower_sdr_fade";
		projId = (int)ProjIds.SeaDragonRage;
		if (!groundedVariant) {
			vel = new Point(100 * xDir, 255);
		} else {
			maxTime = 0.225f;
			vel = new Point(350*xDir, -150);
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)(groundedVariant ? 1 : 0));
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FlamethrowerSeaDragonRage(
			args.pos, args.xDir, args.extraData[0] == 1,
			args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (!groundedVariant) {
			vel.x -= 5*xDir;
		}  else {
			vel.y += 11;
		}
	}	
}
/*
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
*/
