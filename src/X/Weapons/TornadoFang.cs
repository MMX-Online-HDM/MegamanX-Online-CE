using System;
using System.Collections.Generic;

namespace MMXOnline;

public class TornadoFang : Weapon {
	public float doubleShootCooldown;
	public static TornadoFang netWeapon = new();

	public TornadoFang() : base() {
		shootSounds = new string[] { "busterX3", "busterX3", "busterX3", "tunnelFang" };
		fireRate = 60;
		switchCooldown = 45;
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
			if (doubleShootCooldown > 0) { return 2; }
			else { return 1; }
		}
		return 0;
	}

	public override void update() {
		base.update();
		Helpers.decrementFrames(ref doubleShootCooldown);
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (chargeLevel < 3) {
			if (character.ownedByLocalPlayer) {
				if (doubleShootCooldown > 0) {
					new TornadoFangProj(pos, xDir, 1, mmx, player, player.getNextActorNetId(), rpc: true);
					new TornadoFangProj(pos, xDir, 2, mmx, player, player.getNextActorNetId(), rpc: true);
					doubleShootCooldown = 0;
				} else {
					new TornadoFangProj(pos, xDir, 0, mmx, player, player.getNextActorNetId(), rpc: true);
					doubleShootCooldown = 30;
				}
			}
		} else {
			var ct = new TornadoFangProjCharged(pos, xDir, mmx, player, player.getNextActorNetId(), true);
			if (character.ownedByLocalPlayer) {
				mmx.chargedTornadoFang = ct;
			}
			doubleShootCooldown = 0;
		}
	}

	public override float getFireRate(Character character, int chargeLevel, int[] args) {
		if (chargeLevel < 3 && doubleShootCooldown > 0) {
			return 10;
		}
		return fireRate;
	}
}

public class TornadoFangProj : Projectile {
	int state = 0;
	float stateTime = 0;
	public Anim exhaust;
	int type;
	float sparksCooldown;

	public TornadoFangProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "tunnelfang_proj", netId, player
	) {
		weapon = TornadoFang.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 15;
		vel = new Point(100 * xDir, 0);
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
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)type });
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TornadoFangProj(
			arg.pos, arg.xDir, arg.extraData[0],
			arg.owner, arg.player, arg.netId
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
				if (chr != null && chr.ownedByLocalPlayer && !chr.isSlowImmune()) {
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
	public MegamanX mmx = null!;
	float sparksCooldown;
	public bool unliked;
	public Point offset;
	public Anim? exhaust;

	public TornadoFangProjCharged(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "tunnelfang_charged", netId, player
	) {
		weapon = TornadoFang.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 7;
		damager.flinch = Global.defFlinch;
		projId = (int)ProjIds.TornadoFangCharged;
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TornadoFangProjCharged(
			arg.pos, arg.xDir, arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref sparksCooldown);
		if (!ownedByLocalPlayer) return;

		if (!unliked && mmx.chargedTornadoFang == this) {
			if (mmx.player.input.isPressed(Control.Shoot, mmx.player)) {
				unlink();
				return;
			}
			if (mmx == null || mmx.destroyed) {
				destroySelf();
				return;
			}
			if (mmx.currentWeapon is TornadoFang && time >= 6f / 60f) {
				mmx.currentWeapon.addAmmo(speedMul * -0.04f, mmx.player);
			}
			if (mmx.currentWeapon.ammo <= 0) {
				destroySelf();
				return;
			}
			if (mmx.currentWeapon is not TornadoFang && mmx.currentWeapon is not HyperCharge) {
				destroySelf();
				return;
			}
		}
		else if (!unliked) {
			unlink();
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (!unliked && mmx != null && mmx.chargedTornadoFang == this) {
			xDir = mmx.getShootXDir();
			if (mmx.currentFrame.POIs.Length == 0) {
				changePos(mmx.pos.add(offset));
			} else {
				int busterOffset = 2;
				if (mmx.armArmor == ArmorId.Max) {
					busterOffset = 3;
				}
				Point busterPos = mmx.getShootPos().addxy(-busterOffset * xDir, -1);
				changePos(busterPos);
				offset = busterPos - mmx.pos;
			}
		}
	}

	public void unlink() {
		if (unliked) {
			return;
		}
		if (mmx.chargedTornadoFang == this) {
			mmx.chargedTornadoFang = null;
		}
		if (mmx.currentWeapon is TornadoFang) {
			mmx.currentWeapon.addAmmo(-6, mmx.player);
		}
		unliked = true;
		vel.x = xDir * 125;
		time = 0;
		maxTime = 2;
		exhaust = new Anim(pos.addxy(-3 * xDir, 0), "tunnelfang_exhaust", xDir, null, false, host: this);
		exhaust.setzIndex(zIndex - 100);
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (sparksCooldown == 0) {
			playSound("tunnelFangDrill");
			var sparks = new Anim(pos.addxy(15 * xDir, 0), "tunnelfang_sparks", xDir, null, true);
			sparks.setzIndex(zIndex + 100);
			sparksCooldown = 0.25f;
		}

		if (damagable is Character chr && !chr.isSlowImmune()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
			chr.slowdownTime = 0.25f;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (exhaust != null) {
			exhaust.destroySelf();
		}
		if (!ownedByLocalPlayer) {
			return;
		}
		if (mmx.chargedTornadoFang == this) {
			mmx.chargedTornadoFang = null;
		}
		if (unliked) {
			new Anim(pos, "explosion", xDir, damager.owner.getNextActorNetId(), true, true);
			playSound("explosion", true, true);
		}
	}
}
