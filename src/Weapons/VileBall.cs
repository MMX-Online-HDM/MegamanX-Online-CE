using System;
using System.Collections.Generic;

namespace MMXOnline;

public enum VileBallType {
	NoneNapalm = -1,
	ExplosiveRound,
	SpreadShot,
	PeaceOutRoller,
	NoneFlamethrower,
}

public class VileBall : Weapon {
	public float vileAmmoUsage;
	public VileBall(VileBallType vileBallType) : base() {
		fireRate = 60;
		index = (int)WeaponIds.VileBomb;
		weaponBarBaseIndex = 27;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 15;
		type = (int)vileBallType;

		if (vileBallType == VileBallType.NoneNapalm) {
			displayName = "None(NAPALM)";
			description = new string[] { "Do not equip a Ball.", "NAPALM will be used instead." };
			killFeedIndex = 126;
		} else if (vileBallType == VileBallType.NoneFlamethrower) {
			displayName = "None(FLAMETHROWER)";
			description = new string[] { "Do not equip a Ball.", "FLAMETHROWER will be used instead." };
			killFeedIndex = 126;
		} else if (vileBallType == VileBallType.ExplosiveRound) {
			displayName = "Explosive Round";
			vileAmmoUsage = 8;
			description = new string[] { "These bombs split into two", "upon contact with the ground." };
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "2";
			hitcooldown = "0.2";
			effect = "Splits on ground.";
		} else if (vileBallType == VileBallType.SpreadShot) {
			displayName = "Spread Shot";
			vileAmmoUsage = 5;
			description = new string[] { "Unleash a fan of energy shots", "that stun enemies in their tracks." };
			killFeedIndex = 55;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "1";
			effect = "Stuns Enemies. CD: 2";
		} else if (vileBallType == VileBallType.PeaceOutRoller) {
			displayName = "Peace Out Roller";
			vileAmmoUsage = 16;
			fireRate = 75;
			description = new string[] { "This electric bombs splits into two upon", "upon contact with the ground." };
			killFeedIndex = 80;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "3";
			hitcooldown = "0.5";
			Flinch = "6";
			effect = "Splits,no destroy on hit.";
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		return vileAmmoUsage;
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (type == (int)VileBallType.NoneNapalm || type == (int)VileBallType.NoneFlamethrower) return;
		if (shootCooldown == 0) {
			if (weaponInput == WeaponIds.VileBomb) {
				var ground = Global.level.raycast(vile.pos, vile.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
				if (ground == null) {
					if (vile.tryUseVileAmmo(vileAmmoUsage)) {
						vile.setVileShootTime(this);
						vile.changeState(new AirBombAttack(false), true);
					}
				}
			} else if (weaponInput == WeaponIds.Napalm) {
				vile.setVileShootTime(this);
				vile.changeState(new NapalmAttack(NapalmAttackType.Ball), true);
			} else if (weaponInput == WeaponIds.VileFlamethrower) {
				var ground = Global.level.raycast(vile.pos, vile.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
				if (ground == null) {
					if (vile.tryUseVileAmmo(vileAmmoUsage)) {
						vile.setVileShootTime(this);
						vile.changeState(new AirBombAttack(false), true);
					}
				}
			}
		}
	}
}

public class VileBombProj : Projectile {
	int type;
	bool split;
	public VileBombProj(Weapon weapon, Point pos, int xDir, Player player, int type, ushort netProjId, Point? vel = null, bool rpc = false) :
		base(weapon, pos, xDir, 100, 2, player, type == 0 ? "vile_bomb_air" : "vile_bomb_ground", 0, 0.2f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.VileBomb;
		if (type == 0) maxTime = 0.45f;
		if (type == 1) maxTime = 0.3f;
		destroyOnHit = true;
		this.type = type;
		if (vel != null) this.vel = (Point)vel;
		if (type == 0) {
			fadeSprite = "explosion";
			fadeSound = "explosion";
			useGravity = true;
		} else {
			projId = (int)ProjIds.VileBombSplit;
			fadeSprite = "vile_stun_shot_fade";
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public override void update() {
		base.update();
	}

	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;
		if (!other.gameObject.collider.isClimbable) return;
		if (split) return;
		if (type == 0) {
			var normal = other?.hitData?.normal;
			if (normal != null) {
				normal = normal.Value.leftNormal();
			} else {
				normal = new Point(1, 0);
			}
			Point normal2 = (Point)normal;
			normal2.multiply(300);
			destroySelf(fadeSprite);
			split = true;
			new VileBombProj(weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), normal2, rpc: true);
			new VileBombProj(weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), normal2.times(-1), rpc: true);
			destroySelf();
		}
	}
}

public class PeaceOutRollerProj : Projectile {
	int type;
	bool split;
	public PeaceOutRollerProj(
		Weapon weapon, Point pos, int xDir, Player player, int type, ushort netProjId, Point? vel = null, bool rpc = false
	) : base(
		weapon, pos, xDir, 75, 3, player, "ball_por_proj", Global.miniFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.PeaceOutRoller;
		maxTime = 0.5f;
		if (type == 1) maxTime = 0.4f;
		destroyOnHit = false;
		this.type = type;
		if (vel != null) this.vel = (Point)vel;
		if (type == 0) {
			this.vel.y = 50;
			useGravity = true;
			gravityModifier = 0.5f;
		} else {
		}
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public override void update() {
		base.update();
	}

	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;
		if (!other.gameObject.collider.isClimbable) return;
		if (split) return;
		if (type == 0) {
			var normal = other?.hitData?.normal;
			if (normal != null) {
				normal = normal.Value.leftNormal();
			} else {
				normal = new Point(1, 0);
			}
			Point normal2 = (Point)normal;
			normal2.multiply(250);
			destroySelf(fadeSprite);
			split = true;
			playSound("ballPOR", sendRpc: true);
			new PeaceOutRollerProj(weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), normal2, rpc: true);
			new PeaceOutRollerProj(weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), normal2.times(-1), rpc: true);
			destroySelf();
		}
	}
}

public class AirBombAttack : CharState {
	int bombNum;
	bool isNapalm;
	Vile vile = null!;

	public AirBombAttack(bool isNapalm, string transitionSprite = "") : base("air_bomb_attack", "", "", transitionSprite) {
		this.isNapalm = isNapalm;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();

		if (isNapalm) {
			var poi = character.getFirstPOI();
			if (!once && poi != null) {
				once = true;
				if (vile.napalmWeapon.type == (int)NapalmType.RumblingBang) {
					var proj = new NapalmGrenadeProj(vile.napalmWeapon, poi.Value, character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
					proj.vel = new Point(character.xDir * 100, 0);
				}
				if (vile.napalmWeapon.type == (int)NapalmType.FireGrenade) {
					var proj = new MK2NapalmGrenadeProj(vile.napalmWeapon, poi.Value, character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
					proj.vel = new Point(character.xDir * 100, 0);
				}
				if (vile.napalmWeapon.type == (int)NapalmType.SplashHit) {
					var proj = new SplashHitGrenadeProj(vile.napalmWeapon, poi.Value, character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
					proj.vel = new Point(character.xDir * 100, 0);
				}
			}

			if (stateTime > 0.25f) {
				character.changeToIdleOrFall();
			}

			return;
		}

		if (vile.grenadeWeapon.type == (int)VileBallType.ExplosiveRound) {
			if (bombNum > 0 && player.input.isPressed(Control.Special1, player)) {
				character.changeState(new Fall(), true);
				return;
			}

			var inputDir = player.input.getInputDir(player);
			if (inputDir.x == 0) inputDir.x = character.xDir;
			if (stateTime > 0f && bombNum == 0) {
				bombNum++;
				new VileBombProj(vile.grenadeWeapon, character.pos, (int)inputDir.x, player, 0, character.player.getNextActorNetId(), rpc: true);
			}
			if (stateTime > 0.23f && bombNum == 1) {
				if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
					character.changeState(new Fall(), true);
					return;
				}
				bombNum++;
				new VileBombProj(vile.grenadeWeapon, character.pos, (int)inputDir.x, player, 0, character.player.getNextActorNetId(), rpc: true);
			}
			if (stateTime > 0.45f && bombNum == 2) {
				if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
					character.changeState(new Fall(), true);
					return;
				}
				bombNum++;
				new VileBombProj(vile.grenadeWeapon, character.pos, (int)inputDir.x, player, 0, character.player.getNextActorNetId(), rpc: true);
			}

			if (stateTime > 0.68f) {
				character.changeToIdleOrFall();
			}
		} else if (vile.grenadeWeapon.type == (int)VileBallType.SpreadShot) {
			var ebw = new VileElectricBomb();
			if (bombNum > 0 && player.input.isPressed(Control.Special1, player)) {
				character.changeToIdleOrFall();
				return;
			}

			if (stateTime > 0f && bombNum == 0) {
				bombNum++;
				new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(150 * character.xDir, 0), rpc: true);
			}
			if (stateTime > 0.1f && bombNum == 1) {
				if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
					character.changeToIdleOrFall();
					return;
				}
				bombNum++;
				new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(133 * character.xDir, 75), rpc: true);
			}
			if (stateTime > 0.2f && bombNum == 2) {
				if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
					character.changeToIdleOrFall();
					return;
				}
				bombNum++;
				new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(75 * character.xDir, 133), rpc: true);
			}
			if (stateTime > 0.3f && bombNum == 3) {
				if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
					character.changeToIdleOrFall();
					return;
				}
				bombNum++;
				new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(0, 150), rpc: true);
			}
			if (stateTime > 0.4f && bombNum == 4) {
				if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
					character.changeToIdleOrFall();
					return;
				}
				bombNum++;
				new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(-75 * character.xDir, 133), rpc: true);
			}
			if (stateTime > 0.5f && bombNum == 5) {
				if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
					character.changeToIdleOrFall();
					return;
				}
				bombNum++;
				new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(-133 * character.xDir, 75), rpc: true);
			}
			if (stateTime > 0.6f && bombNum == 6) {
				if (!vile.tryUseVileAmmo(vile.grenadeWeapon.getAmmoUsage(0))) {
					character.changeToIdleOrFall();
					return;
				}
				bombNum++;
				new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(-150 * character.xDir, 0), rpc: true);
			}

			if (stateTime > 0.66f) {
				character.changeToIdleOrFall();
			}
		} else if (vile.grenadeWeapon.type == (int)VileBallType.PeaceOutRoller) {
			if (stateTime > 0f && bombNum == 0) {
				bombNum++;
				new PeaceOutRollerProj(vile.grenadeWeapon, character.pos, character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
			}

			if (stateTime > 0.25f) {
				character.changeToIdleOrFall();
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point();
		vile = character as Vile ?? throw new NullReferenceException();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}

public class VileElectricBomb : Weapon {
	public VileElectricBomb() : base() {
		fireRate = 60;
		index = (int)WeaponIds.VileBomb;
		weaponBarBaseIndex = 55;
		weaponBarIndex = 55;
		killFeedIndex = 55;
	}
}
