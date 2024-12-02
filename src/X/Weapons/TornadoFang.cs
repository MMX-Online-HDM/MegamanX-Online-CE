using System;
using System.Collections.Generic;

namespace MMXOnline;

public class TornadoFang : Weapon {

	public static TornadoFang netWeapon = new();

	public TornadoFang() : base() {
		shootSounds = new string[] { "busterX3", "busterX3", "busterX3", "tunnelFang" };
		fireRate = 60;
		index = (int)WeaponIds.TornadoFang;
		weaponBarBaseIndex = 24;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 24;
		killFeedIndex = 47;
		weaknessIndex = (int)WeaponIds.AcidBurst;
		damage = "1/1";
		effect = "Inflicts Slowdown. Doesn't destroy on hit.\nUncharged won't give assists.";
		hitcooldown = "0.25/0.125";
		Flinch = "0/26";
		FlinchCD = "0/1";
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel < 3) {
			if (timeSinceLastShoot != null && timeSinceLastShoot < fireRate) return 2;
			else return 1;
		}
		return 8;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			if (character.ownedByLocalPlayer && character is MegamanX mmx) {
				if (timeSinceLastShoot != null && timeSinceLastShoot < fireRate) {
					new TornadoFangProj(this, pos, xDir, 1, player, player.getNextActorNetId(), rpc: true);
					new TornadoFangProj(this, pos, xDir, 2, player, player.getNextActorNetId(), rpc: true);
					timeSinceLastShoot = null;
				} else {
					new TornadoFangProj(this, pos, xDir, 0, player, player.getNextActorNetId(), rpc: true);
					timeSinceLastShoot = 0;
					mmx.shootCooldown = 30;
				}
			}
		} else {
			var ct = new TornadoFangProjCharged(this, pos, xDir, player, player.getNextActorNetId(), true);
			if (character.ownedByLocalPlayer && character is MegamanX mmx) {
				mmx.chargedTornadoFang = ct;
			}
		}
	}
}

public class TornadoFangProj : Projectile {
	int state = 0;
	float stateTime = 0;
	public Anim exhaust;
	int type;
	float sparksCooldown;

	public TornadoFangProj(
		Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 100, 1, player, "tunnelfang_proj", 0, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1.5f;
		projId = (int)ProjIds.TornadoFang;
		exhaust = new Anim(pos, "tunnelfang_exhaust", xDir, null, false);
		exhaust.setzIndex(zIndex - 100);
		destroyOnHit = false;
		this.type = type;
		if (type != 0) {
			vel.x = 0;
			vel.y = (type == 1 ? -100 : 100);
			projId = (int)ProjIds.TornadoFang2;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, new byte[] { (byte)type });
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TornadoFangProj(
			TornadoFang.netWeapon, arg.pos, arg.xDir, 
			arg.extraData[0], arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		Helpers.decrementFrames(ref sparksCooldown);
		exhaust.pos = pos;
		exhaust.xDir = xDir;
		if (state == 0) {
			if (type == 0) {
				if (stateTime > 9) {
					vel.x = 0;
				}
			} else if (type == 1 || type == 2) {
				if (stateTime > 9) {
					vel.y = 0;
				}
				if (stateTime > 9 && stateTime < 18) vel.x = 100 * xDir;
				else vel.x = 0;
			}
			stateTime += Global.speedMul;
			if (stateTime >= 45) {
				state = 1;
			}
		} else if (state == 1) {
			vel.x += Global.spf * 500 * xDir;
			if (MathF.Abs(vel.x) > 350) vel.x = 350 * xDir;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				vel.x = 4 * xDir;
				// To update the reduced speed.
				if (ownedByLocalPlayer) {
				forceNetUpdateNextFrame = true;
				}

				if (damagable is not CrackedWall) {
					time -= Global.spf;
					if (time < 0) time = 0;
				}

				if (sparksCooldown == 0) {
					playSound("tunnelFangDrill");
					var sparks = new Anim(pos, "tunnelfang_sparks", xDir, null, true);
					sparks.setzIndex(zIndex + 100);
					sparksCooldown = 15;
				}

				var chr = damagable as Character;
				if (chr != null && chr.ownedByLocalPlayer && !chr.isImmuneToKnockback()) {
					chr.vel = Point.lerp(chr.vel, Point.zero, Global.speedMul);
					chr.slowdownTime = 0.25f;
				}
			}
		}	
	}

	public override void onDestroy() {
		base.onDestroy();
		exhaust?.destroySelf();
	}
}

public class TornadoFangProjCharged : Projectile {
	public MegamanX? mmx;
	float sparksCooldown;

	public TornadoFangProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 300, 1, player, "tunnelfang_charged", 
		Global.defFlinch, 0.125f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.TornadoFangCharged;
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		mmx = (player.character as MegamanX);
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TornadoFangProjCharged(
			TornadoFang.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref sparksCooldown);

		if (!ownedByLocalPlayer) return;

		if (mmx == null || mmx.destroyed) {
			destroySelf();
			return;
		}

		weapon.addAmmo(-Global.spf * 5, mmx.player);
		if (weapon.ammo <= 0) {
			destroySelf();
			return;
		}

		if (mmx.currentWeapon is not TornadoFang && mmx.currentWeapon is not HyperCharge) {
			destroySelf();
			return;
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (mmx != null) {
			changePos(mmx.getShootPos());
			xDir = mmx.getShootXDir();
		}
		
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (sparksCooldown == 0) {
			playSound("tunnelFangDrill");
			var sparks = new Anim(pos.addxy(15 * xDir, 0), "tunnelfang_sparks", xDir, null, true);
			sparks.setzIndex(zIndex + 100);
			sparksCooldown = 0.25f;
		}

		if (damagable is Character chr) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
			chr.slowdownTime = 0.25f;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx?.removeLastingProjs();
	}
}
