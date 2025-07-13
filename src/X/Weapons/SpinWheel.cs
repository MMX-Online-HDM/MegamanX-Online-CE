using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SpinWheel : Weapon {
	public static SpinWheel netWeapon = new();

	public SpinWheel() : base() {
		displayName = "Spin Wheel";
		shootSounds = new string[] { "spinWheel", "spinWheel", "spinWheel", "spinWheelCharged" };
		fireRate = 60;
		switchCooldown = 45;
		index = (int)WeaponIds.SpinWheel;
		weaponBarBaseIndex = 12;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 12;
		killFeedIndex = 20 + (index - 9);
		weaknessIndex = (int)WeaponIds.StrikeChain;
		damage = "1/1*8";
		effect = "Inflicts Slowdown. Doesn't destroy on hit.\nUncharged won't give assists.";
		hitcooldown = "12/0";
		flinch = "0/26";
		maxAmmo = 16;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) { return 4; }
		return 1;
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new SpinWheelProj(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		} else {
			new SpinWheelProjChargedStart(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		}
	}
}

public class SpinWheelProj : Projectile {
	int started;
	float startedTime;
	public Anim? sparks;
	float soundTime;
	float startMaxTime = 2.5f;
	float lastHitTime;
	const float hitCooldown = 0.2f;
	float maxTimeProj = 2.5f;

	public SpinWheelProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "spinwheel_start", netId, player	
	) {
		weapon = SpinWheel.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 12;
		vel = new Point(0 * xDir, 0);
		destroyOnHit = false;
		projId = (int)ProjIds.SpinWheel;
		maxTimeProj = startMaxTime;
		maxTime = startMaxTime;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SpinWheelProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (ownedByLocalPlayer && time >= maxTimeProj) {
			destroySelf();
			return;
		}
		if (collider != null) {
			collider.isTrigger = false;
			collider.wallOnly = true;
		}
		if (started == 0 && sprite.isAnimOver()) {
			started = 1;
			changeSprite("spinwheel_proj", true);
			useGravity = true;
			if (collider != null) {
				collider.isTrigger = false;
				collider.wallOnly = true;
			}
		}
		if (started == 1) {
			startedTime += Global.spf;
			if (startedTime > 1) {
				started = 2;
				maxTimeProj = startMaxTime;
			}
		}
		if (started == 2) {
			vel.x = xDir * 250;
			if (lastHitTime > 0) vel.x = xDir * 4;
			Helpers.decrementTime(ref lastHitTime);
			if (Global.level.checkTerrainCollisionOnce(this, 0, -1) == null) {
				var collideData = Global.level.checkTerrainCollisionOnce(this, xDir, 0, vel);
				if (collideData != null && collideData.hitData != null &&
					collideData.hitData.normal != null && !((Point)collideData.hitData.normal).isAngled()
				) {
					xDir *= -1;
					if (sparks != null) sparks.xDir *= -1;
					maxTimeProj = startMaxTime;
					if (ownedByLocalPlayer) {
						startMaxTime -= 0.2f;
					}
				}
			}
			soundTime += Global.spf;
			if (soundTime > 0.15f) {
				soundTime = 0;
				//playSound("spinWheelLoop");
			}
		}
		if (started > 0 && grounded && !destroyed) {
			if (sparks == null) {
				sparks = new Anim(pos, "spinwheel_sparks", xDir, null, false);
				playSound("spinWheelGround", forcePlay: true);
			}
			sparks.pos = pos.addxy(-xDir * 10, 10);
			sparks.visible = true;
		} else {
			if (sparks != null) {
				sparks.visible = false;
			}
		}
	}

	public override void onDestroy() {
		if (sparks != null) {
			sparks.destroySelf();
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (damagable is CrackedWall) {
			damager.hitCooldownSeconds = hitCooldown;
			return;
		}

		lastHitTime = hitCooldown;

		var chr = damagable as Character;
		if (chr != null && chr.ownedByLocalPlayer && !chr.isSlowImmune()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
			chr.slowdownTime = 0.25f;
		}

		base.onHitDamagable(damagable);
	}
}

public class SpinWheelProjChargedStart : Projectile {
	public SpinWheelProjChargedStart(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "spinwheel_charged_start", netId, player	
	) {
		weapon = SpinWheel.netWeapon;
		damager.damage = 4;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.SpinWheelChargedStart;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SpinWheelProjChargedStart(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			if (ownedByLocalPlayer) {
				for (int i = 0; i < 8; i++) {
					new SpinWheelProjCharged(
						pos, 1, this, damager.owner, i,
						damager.owner.getNextActorNetId(), rpc: true
					);
				}
			}
			destroySelf();
		}
	}
}

public class SpinWheelProjCharged : Projectile {

	public SpinWheelProjCharged(
		Point pos, int xDir, Actor owner, Player player, int type, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "spinwheel_charged", netId, player	
	) {
		weapon = SpinWheel.netWeapon;
		damager.damage = 1;
		damager.flinch = Global.defFlinch;
		projId = (int)ProjIds.SpinWheelCharged;
		maxTime = 0.75f;
		vel = Point.createFromByteAngle(type * 32).times(200);

		if (vel.x < 0) {
			xDir *= -1;
			xScale *= -1;
		}
		if (vel.y > 0) yScale *= -1;

		if (type % 2 != 0) changeSprite("spinwheel_charged_diag", true);
		else if (type is 2 or 6) changeSprite("spinwheel_charged_up", true);

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] {(byte)type});
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SpinWheelProjCharged(
			args.pos, args.xDir, args.owner, args.player, args.extraData[0], args.netId
		);
	}
}
