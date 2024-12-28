using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HomingTorpedo : Weapon {
	public static HomingTorpedo netWeapon = new();

	public HomingTorpedo() : base() {
		index = (int)WeaponIds.HomingTorpedo;
		killFeedIndex = 1;
		weaponBarBaseIndex = 1;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 1;
		weaknessIndex = (int)WeaponIds.RollingShield;
		shootSounds = new string[] { "torpedo", "torpedo", "torpedo", "buster3" };
		fireRate = 38;
		damage = "2/1*6";
		effect = "Destroys on contact with projectiles or enemies.\nBesides of it's homing capabilities.";
		hitcooldown = "0";
		Flinch = "0/13";
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


		if (chargeLevel < 3) {
			new TorpedoProj(this, pos, xDir, player, 0, player.getNextActorNetId(true), rpc: true);
		} else {
			player.getNextActorNetId(true);
			new TorpedoProj(this, pos.addxy(0, 2), xDir, player, 1, player.getNextActorNetId(true), 30, true);
			new TorpedoProj(this, pos.addxy(0, 1), xDir, player, 1, player.getNextActorNetId(true), 15, true);
			new TorpedoProj(this, pos.addxy(0, 0), xDir, player, 1, player.getNextActorNetId(true), 0, true);
			new TorpedoProj(this, pos.addxy(0, -1), xDir, player, 1, player.getNextActorNetId(true), -15, true);
			new TorpedoProj(this, pos.addxy(0, -2), xDir, player, 1, player.getNextActorNetId(true), -30, true);
		}
	}
}

public class TorpedoProj : Projectile, IDamagable {
	public Actor? target;
	public float smokeTime = 0;
	public float maxSpeed = 150;
	int type;
	public TorpedoProj(
		Weapon weapon, Point pos, int xDir, Player player, 
		int type, ushort netProjId, float? angle = null, bool rpc = false
	) : base(
		weapon, pos, xDir, 150, 2, player, 
		(type == 0 ? "torpedo" : type == 1 ? "torpedo_charge" : "frog_torpedo"), 
		0, 0f, netProjId, player.ownedByLocalPlayer
	) {

		if (type == 0) projId = (int)ProjIds.Torpedo;
		else if (type == 1) projId = (int)ProjIds.TorpedoCharged;
		else if (type == 2) projId = (int)ProjIds.MechTorpedo;
		else if (type == 3) {
			projId = (int)ProjIds.LaunchOTorpedo;
			changeSprite("launcho_proj_ht", true);
		}

		maxTime = 2f;
		fadeOnAutoDestroy = true;
		reflectableFBurner = true;
		customAngleRendering = true;
		if (type == 1 || type == 3) {
			damager.damage = (type == 1 ? 1 : 2);
			damager.flinch = Global.halfFlinch;
		} else if (type == 2) {
			vel.x = 1;
			//damager.damage = 1;
		}
		this.type = type;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		this.angle = this.xDir == -1 ? 180 : 0;
		if (angle != null) {
			this.angle = angle + (this.xDir == -1 ? 180 : 0);
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TorpedoProj(
			HomingTorpedo.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.extraData[0], arg.netId
		);
	}

	bool homing = true;
	public void checkLandFrogTorpedo() {
		if (type == 2) {
			if (!isUnderwater()) {
				useGravity = true;
				maxTime = 1f;
				homing = false;
			} else {
				useGravity = true;
				homing = true;
			}
		}
	}

	public void reflect(float reflectAngle) {
		angle = reflectAngle;
		target = null;
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		checkLandFrogTorpedo();

		if (ownedByLocalPlayer && homing) {
			if (target != null) {
				if (!Global.level.gameObjects.Contains(target)) {
					target = null;
				}
			}


			if (target != null) {
				if (time < 3f) {
					var dTo = pos.directionTo(target.getCenterPos()).normalize();
					var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
					destAngle = Helpers.to360(destAngle);
					if (angle != null) angle = Helpers.lerpAngle((float)angle, destAngle, Global.spf * 3);
				} else {

				}
			}
			if (time >= 0.15) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, true, aMaxDist: Global.screenW * 0.75f);
			} else if (time < 0.15) {
				//this.vel.x += this.xDir * Global.spf * 300;
			}

			/*
			this.vel = this.vel.add(new Point(Helpers.cos(this.angle), Helpers.sin(this.angle)).times(this.maxSpeed * 0.25));
			if(this.vel.magnitude > this.maxSpeed) {
			  this.vel = this.vel.normalize().times(this.maxSpeed);
			}
			*/
			if (angle != null) {
				vel.x = Helpers.cosd((float)angle) * maxSpeed;
				vel.y = Helpers.sind((float)angle) * maxSpeed;
			}
		}

		smokeTime += Global.spf;
		if (smokeTime > 0.2) {
			smokeTime = 0;
			if (homing) new Anim(pos, "torpedo_smoke", 1, null, true);
		}
	}

	public override void renderFromAngle(float x, float y) {
		var angle = this.angle;
		var xDir = 1;
		var yDir = 1;
		var frameIndex = 0;
		float normAngle = 0;
		if (angle < 90) {
			xDir = 1;
			yDir = -1;
			normAngle = (float)angle;
		}
		if (angle >= 90 && angle < 180) {
			xDir = -1;
			yDir = -1;
			normAngle = 180 - (float)angle;
		} else if (angle >= 180 && angle < 270) {
			xDir = -1;
			yDir = 1;
			normAngle = (float)angle - 180;
		} else if (angle >= 270 && angle < 360) {
			xDir = 1;
			yDir = 1;
			normAngle = 360 - (float)angle;
		}

		if (normAngle < 18) frameIndex = 0;
		else if (normAngle >= 18 && normAngle < 36) frameIndex = 1;
		else if (normAngle >= 36 && normAngle < 54) frameIndex = 2;
		else if (normAngle >= 54 && normAngle < 72) frameIndex = 3;
		else if (normAngle >= 72 && normAngle < 90) frameIndex = 4;

		sprite.draw(frameIndex, pos.x + x, pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex, actor: this);
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damager.owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}
}
