using System;

namespace MMXOnline;

public enum VileMissileType {
	None = -1,
	ElectricShock,
	HumerusCrush,
	PopcornDemon
}

public class VileMissile : Weapon {
	public string projSprite = "";
	public float vileAmmo;

	public VileMissile(VileMissileType vileMissileType) : base() {
		index = (int)WeaponIds.ElectricShock;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 42;
		killFeedIndex = 17;
		type = (int)vileMissileType;

		if (vileMissileType == VileMissileType.None) {
			displayName = "None";
			description = new string[] { "Do not equip a Missile." };
			vileAmmo = 8;
			killFeedIndex = 126;
		} else if (vileMissileType == VileMissileType.ElectricShock) {
			rateOfFire = 0.75f;
			displayName = "Electric Shock";
			vileAmmo = 8;
			description = new string[] { "Stops enemies in their tracks,", "but deals no damage." };
			vileWeight = 3;
		} else if (vileMissileType == VileMissileType.HumerusCrush) {
			rateOfFire = 0.75f;
			displayName = "Humerus Crush";
			projSprite = "missile_hc_proj";
			vileAmmo = 8;
			description = new string[] { "This missile shoots straight", "and deals decent damage." };
			killFeedIndex = 74;
			vileWeight = 3;
		} else if (vileMissileType == VileMissileType.PopcornDemon) {
			rateOfFire = 0.75f;
			displayName = "Popcorn Demon";
			projSprite = "missile_pd_proj";
			vileAmmo = 12;
			description = new string[] { "This missile splits into 3", "and can cause great damage." };
			killFeedIndex = 76;
			vileWeight = 3;
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		Player player = vile.player;
		if (shootTime > 0) return;

		if (vile.charState is Idle || vile.charState is Run || vile.charState is Crouch) {
			if (vile.tryUseVileAmmo(vileAmmo)) {
				if (!vile.isVileMK2) {
					vile.setVileShootTime(this);
					vile.changeState(new MissileAttack(), true);
				} else if (!vile.charState.isGrabbing) {
					vile.setVileShootTime(this);
					MissileAttack.mk2ShootLogic(vile, vile.missileWeapon.type == (int)VileMissileType.ElectricShock);
				}
			}
		} else if (vile.charState is InRideArmor) {
			if (!vile.isVileMK2) {
				vile.setVileShootTime(this);
				if (vile.missileWeapon.type == 2 || vile.missileWeapon.type == 1) {
					vile.playSound("vileMissile", sendRpc: true);
					new VileMissileProj(vile.missileWeapon, vile.getFirstPOIOrDefault(), vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), new Point(vile.xDir, 0), rpc: true);
				} else {
					new StunShotProj(this, vile.pos.addxy(15 * vile.xDir, -10), vile.getShootXDir(), 0, player, player.getNextActorNetId(), vile.getVileShootVel(true), rpc: true);
				}
			} else {
				vile.setVileShootTime(this);
				if (vile.missileWeapon.type == 2 || vile.missileWeapon.type == 1) {
					vile.playSound("mk2stunshot", sendRpc: true);
					new VileMissileProj(vile.missileWeapon, vile.getFirstPOIOrDefault(), vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), new Point(vile.xDir, 0), rpc: true);
				} else {
					MissileAttack.mk2ShootLogic(vile, true);
				}
			}
		}
	}
}

public class VileMissileProj : Projectile {
	public VileMissile missileWeapon;
	bool split;
	int type;
	public VileMissileProj(VileMissile weapon, Point pos, int xDir, int type, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
		base(weapon, pos, xDir, 200, 3, player, weapon.projSprite, 0, 0.15f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "explosion";
		fadeSound = "explosion";
		projId = (int)ProjIds.VileMissile;
		maxTime = 0.6f;
		destroyOnHit = true;
		fadeOnAutoDestroy = true;
		missileWeapon = weapon;
		reflectableFBurner = true;
		this.type = type;
		canBeLocal = false; // TODO: Remove the need for this.

		if (weapon.type == (int)VileMissileType.HumerusCrush) {
			damager.damage = 3;
			// damager.flinch = Global.halfFlinch;
			this.vel.x = xDir * 350;
			maxTime = 0.35f;
		}
		if (weapon.type == (int)VileMissileType.PopcornDemon) {
			projId = (int)ProjIds.PopcornDemon;
			damager.damage = 2;
		}
		if (type == 1) {
			projId = (int)ProjIds.PopcornDemonSplit;
			this.xDir = 1;
			this.vel = vel.Value.times(speed);
			angle = this.vel.angle;
			damager.damage = 2;
			damager.hitCooldown = 0;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (missileWeapon.type == (int)VileMissileType.PopcornDemon && type == 0 && !split) {
			if (time > 0.3f || owner.input.isPressed(Control.Special1, owner)) {
				split = true;
				playSound("vileMissile", sendRpc: true);
				destroySelfNoEffect();
				new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, -1).normalize(), rpc: true);
				new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, 0), rpc: true);
				new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, 1).normalize(), rpc: true);
			}
		}
	}

	/*
	public override void onHitDamagable(IDamagable damagable)
	{
		base.onHitDamagable(damagable);

		if (damagable is Character character)
		{
			var victimCenter = character.getCenterPos();
			var bombCenter = pos;
			var dirTo = bombCenter.directionToNorm(victimCenter);
			character.vel.y = dirTo.y * 150;
			character.xPushVel = dirTo.x * 300;
		}
	}
	*/
}

public class VileMK2StunShot : Weapon {
	public VileMK2StunShot() : base() {
		rateOfFire = 0.75f;
		index = (int)WeaponIds.MK2StunShot;
		killFeedIndex = 67;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		new StunShotProj(this, pos, xDir, 0, player, netProjId);
	}
}

public class StunShotProj : Projectile {
	public StunShotProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
		base(weapon, pos, xDir, 150, 0, player, type == 0 ? "vile_stun_shot" : "vile_ebomb_start", 0, 0.15f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "vile_stun_shot_fade";
		projId = (int)ProjIds.ElectricShock;
		maxTime = 0.75f;
		destroyOnHit = true;
		canBeLocal = false; // TODO: Remove the need for this.

		if (vel != null) {
			if (type == 0) {
				var norm = vel.Value.normalize();
				this.vel.x = norm.x * speed * player.character.getShootXDir();
				this.vel.y = norm.y * speed;
				this.vel.x *= 1.5f;
				this.vel.y *= 2f;
			} else {
				this.vel = vel.Value;
			}
		}

		if (type == 1) {
			damager.damage = 1;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
	}
}

public class VileMK2StunShotProj : Projectile {
	public VileMK2StunShotProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
		base(weapon, pos, xDir, 150, 1, player, "vile_stun_shot2", 0, 0.15f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "vile_stun_shot_fade";
		projId = (int)ProjIds.MK2StunShot;
		maxTime = 0.75f;
		destroyOnHit = true;

		if (vel != null) {
			var norm = vel.Value.normalize();
			this.vel.x = norm.x * speed * player.character.getShootXDir();
			this.vel.y = norm.y * speed;
			this.vel.x *= 1.5f;
			if (player.character.charState is InRideArmor) this.vel.y *= 1.5f;
			else this.vel.y *= 2f;
			if (this.vel.y == 0) this.vel.y = 5;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
	}
}

public class MissileAttack : CharState {
	public MissileAttack() : base("idle_shoot", "", "", "") {
		exitOnAirborne = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();

		groundCodeWithMove();

		if (character.sprite.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public static void shootLogic(Vile vile) {
		Player player = vile.player;
		bool isStunShot = vile.missileWeapon.type == (int)VileMissileType.ElectricShock;
		if (vile.sprite.getCurrentFrame().POIs.IsNullOrEmpty()) return;
		Point shootVel = vile.getVileShootVel(isStunShot);

		Point shootPos = vile.setCannonAim(shootVel);

		if (isStunShot) {
			new StunShotProj(vile.missileWeapon, shootPos, vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), shootVel, rpc: true);
		} else {
			vile.playSound("vileMissile", sendRpc: true);
			new VileMissileProj(vile.missileWeapon, shootPos, vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), shootVel, rpc: true);
		}
	}

	public static void mk2ShootLogic(Vile vile, bool isStunShot) {
		Player player = vile.player;
		Point? headPosNullable = vile.getVileMK2StunShotPos();
		if (headPosNullable == null) return;

		vile.playSound("mk2stunshot", sendRpc: true);
		new Anim(headPosNullable.Value, "dust", 1, vile.player.getNextActorNetId(), true, true);

		if (isStunShot) {
			new VileMK2StunShotProj(new VileMK2StunShot(), headPosNullable.Value, vile.getShootXDir(), vile.player, vile.player.getNextActorNetId(), vile.getVileShootVel(true), rpc: true);
		} else {
			new VileMissileProj(vile.missileWeapon, headPosNullable.Value, vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), vile.getVileShootVel(false), rpc: true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		shootLogic(character as Vile ?? throw new NullReferenceException());
	}
}
