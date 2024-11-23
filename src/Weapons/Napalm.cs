using System;
using System.Collections.Generic;

namespace MMXOnline;

public enum NapalmType {
	NoneBall = -1,
	RumblingBang,
	FireGrenade,
	SplashHit,
	NoneFlamethrower,
}

public class Napalm : Weapon {
	public float vileAmmoUsage;
	public Napalm(NapalmType napalmType) : base() {
		index = (int)WeaponIds.Napalm;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 30;
		type = (int)napalmType;

		if (napalmType == NapalmType.NoneBall) {
			displayName = "None(BALL)";
			description = new string[] { "Do not equip a Napalm.", "GRENADE will be used instead." };
			killFeedIndex = 126;
		} else if (napalmType == NapalmType.NoneFlamethrower) {
			displayName = "None(FLAMETHROWER)";
			description = new string[] { "Do not equip a Napalm.", "FLAMETHROWER will be used instead." };
			killFeedIndex = 126;
		} else if (napalmType == NapalmType.RumblingBang) {
			displayName = "Rumbling Bang";
			vileAmmoUsage = 8;
			fireRate = 60 * 2;
			description = new string[] { "This napalm sports a wide horizontal", "range but cannot attack upward." };
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "2/1";
			hitcooldown = "0.5";
			effect = "None.";
		}
		if (napalmType == NapalmType.FireGrenade) {
			displayName = "Flame Round";
			vileAmmoUsage = 16;
			fireRate = 60 * 4;
			description = new string[] { "This napalm travels along the", "ground, laying a path of fire." };
			killFeedIndex = 54;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "1/2";
			hitcooldown = "1/2";
			effect = "Fire DOT: 0.5/1";
		}
		if (napalmType == NapalmType.SplashHit) {
			displayName = "Splash Hit";
			vileAmmoUsage = 16;
			fireRate = 60 * 3;
			description = new string[] { "This napalm can attack foes above,", "but has a narrow horizontal range." };
			killFeedIndex = 79;
			vileWeight = 3;
			ammousage = vileAmmoUsage;
			damage = "2/1";
			hitcooldown = "0.5";
			effect = "Pushes towards it.";
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (type == (int)NapalmType.NoneBall || type == (int)NapalmType.NoneFlamethrower) return;
		if (shootCooldown == 0) {
			if (weaponInput == WeaponIds.Napalm) {
				if (vile.tryUseVileAmmo(vileAmmoUsage)) {
					vile.changeState(new NapalmAttack(NapalmAttackType.Napalm), true);
				}
			} else if (weaponInput == WeaponIds.VileFlamethrower) {
				var ground = Global.level.raycast(vile.pos, vile.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
				if (ground == null) {
					if (vile.tryUseVileAmmo(vileAmmoUsage)) {
						vile.setVileShootTime(this);
						vile.changeState(new AirBombAttack(true), true);
					}
				}
			} else if (weaponInput == WeaponIds.VileBomb) {
				var ground = Global.level.raycast(vile.pos, vile.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
				if (ground == null) {
					if (vile.player.vileAmmo >= vileAmmoUsage) {
						vile.setVileShootTime(this);
						vile.changeState(new AirBombAttack(true), true);
					}
				}
			}
		}
	}
}

public class NapalmGrenadeProj : Projectile {
	bool exploded;
	public NapalmGrenadeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 150, 2, player, "napalm_grenade", 0, 0.2f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.NapalmGrenade;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		this.vel = new Point(speed * xDir, -200);
		useGravity = true;
		collider.wallOnly = true;
		fadeSound = "explosion";
		fadeSprite = "explosion";
		shouldShieldBlock = false;
	}

	public override void update() {
		base.update();
		if (grounded) {
			explode();
		}
	}

	public override void onHitWall(CollideData other) {
		xDir *= -1;
		explode();
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer) explode();
	}

	public void explode() {
		if (exploded) return;
		exploded = true;
		if (ownedByLocalPlayer) {
			int[] distances = [-30, 30, -10, 10];
			foreach (int distance in distances) {
				new NapalmPartProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), distance * xDir, rpc: true);
			}
		}
		destroySelf();
	}
}

public class NapalmPartProj : Projectile {
	float xDist;
	float maxXDist;
	float timeOffset;
	float timeOffset2;
	int secondOffset;
	float napalmPeriod = 0.5f;
	float napalmPeriod2 = 0.2f;
	int firstDir = 1;
	int secondDir = 1;

	public NapalmPartProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort netProjId, int xDist, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "napalm_part", 0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.Napalm;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)xDist);
		}
		vel.y = -40;
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = false;
		shouldShieldBlock = false;
		gravityModifier = 0.25f;
		frameIndex = Helpers.randomRange(0, sprite.totalFrameNum - 1);
		secondOffset = Helpers.randomRange(0, sprite.totalFrameNum - 1);
		timeOffset = Helpers.randomRange(0, 50) / 2;
		timeOffset2 = Helpers.randomRange(0, 50) / 2;
		if (Helpers.randomRange(0, 1) == 1) {
			firstDir = -1;
		}
		if (Helpers.randomRange(0, 1) == 1) {
			secondDir = -1;
		}
		maxXDist = xDist;
		maxTime = 4;
	}

	public override void update() {
		base.update();

		if (useGravity && isUnderwater()) {
			destroySelf(disableRpc: true);
			return;
		}
		if (xDist < MathF.Abs(maxXDist)) {
			float dist = maxXDist / 20 * Global.speedMul;
			xDist += MathF.Abs(dist);
			move(new Point(dist, 0), useDeltaTime: false);
			if (xDist > MathF.Abs(maxXDist)) {
				xDist = MathF.Abs(maxXDist);
			}
		}
		else if (grounded && useGravity) {
			useGravity = false;
			isStatic = true;
		}
	}

	public override void render(float x, float y) {
		if (!shouldRender(x, y)) {
			return;
		}
		float drawX = MathF.Round(pos.x + x);
		float drawY = MathF.Round(pos.y + y) + 1;
		float napalmTime = (time + timeOffset) % napalmPeriod;
		float napalmTime2 = (time + timeOffset2) % napalmPeriod2;
		float separation = 6 * (xDist / MathF.Abs(maxXDist));

		float alpha = MathF.Abs(1 - 2 * (napalmTime / napalmPeriod));
		float alpha2 = MathF.Abs(1 - 2 * (napalmTime2 / napalmPeriod2));
		for (int i = -1; i <= 1; i += 2) {
			int frameToDraw = frameIndex;
			if (i == -1) {
				frameToDraw = (frameIndex + secondOffset) % sprite.totalFrameNum;
			}
			sprite.draw(
				frameToDraw, drawX + i * separation * xDir, drawY, firstDir, yDir,
				getRenderEffectSet(),
				alpha,
				2 - alpha,
				2 - alpha,
				zIndex - 100,
				getShaders(), 0,
				actor: this, useFrameOffsets: true
			);
			sprite.draw(
				frameToDraw, drawX + i * separation * xDir, drawY, secondDir, yDir,
				getRenderEffectSet(),
				1 - alpha,
				1 + alpha2 / 2,
				1 + alpha2 / 2,
				zIndex - 100,
				getShaders(), 0,
				actor: this, useFrameOffsets: true
			);
		}
		renderHitboxes();
	}
}

public enum NapalmAttackType {
	Napalm,
	Ball,
	Flamethrower,
}

public class NapalmAttack : CharState {
	bool shot;
	NapalmAttackType napalmAttackType;
	float shootTime;
	int shootCount;
	Vile vile = null!;

	public NapalmAttack(NapalmAttackType napalmAttackType, string transitionSprite = "") :
		base(getSprite(napalmAttackType), "", "", transitionSprite) {
		this.napalmAttackType = napalmAttackType;
		useDashJumpSpeed = true;
	}

	public static string getSprite(NapalmAttackType napalmAttackType) {
		return napalmAttackType == NapalmAttackType.Flamethrower ? "crouch_flamethrower" : "crouch_nade";
	}

	public override void update() {
		base.update();

		if (napalmAttackType == NapalmAttackType.Napalm) {
			if (!shot && character.sprite.frameIndex == 2) {
				shot = true;
				vile.setVileShootTime(vile.napalmWeapon);
				var poi = character.sprite.getCurrentFrame().POIs[0];
				poi.x *= character.xDir;

				Projectile proj;
				if (napalmAttackType == NapalmAttackType.Napalm) {
					if (vile.napalmWeapon.type == (int)NapalmType.RumblingBang) {
						proj = new NapalmGrenadeProj(vile.napalmWeapon, character.pos.add(poi), character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
					} else if (vile.napalmWeapon.type == (int)NapalmType.FireGrenade) {
						character.playSound("FireNappalmMK2", forcePlay: false, sendRpc: true);
						proj = new MK2NapalmGrenadeProj(vile.napalmWeapon, character.pos.add(poi), character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
					} else if (vile.napalmWeapon.type == (int)NapalmType.SplashHit) {
						proj = new SplashHitGrenadeProj(vile.napalmWeapon, character.pos.add(poi), character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
					}
				}
			}
		} else if (napalmAttackType == NapalmAttackType.Ball) {
			if (vile.grenadeWeapon.type == (int)VileBallType.ExplosiveRound) {
				if (shootCount < 3 && character.sprite.frameIndex == 2) {
					if (!vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
						character.changeState(new Crouch(""), true);
						return;
					}
					shootCount++;
					vile.setVileShootTime(vile.grenadeWeapon);
					var poi = character.sprite.getCurrentFrame().POIs[0];
					poi.x *= character.xDir;
					Projectile proj = new VileBombProj(vile.grenadeWeapon, character.pos.add(poi), character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
					proj.vel = new Point(character.xDir * 150, -200);
					proj.maxTime = 0.6f;
					character.sprite.frameIndex = 0;
				}
			} else if (vile.grenadeWeapon.type == (int)VileBallType.SpreadShot) {
				shootTime += Global.spf;
				var poi = character.getFirstPOI();
				if (shootTime > 0.06f && poi != null && shootCount <= 4) {
					if (!vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
						character.changeState(new Crouch(""), true);
						return;
					}
					shootTime = 0;
					character.sprite.frameIndex = 1;
					Point shootDir = Point.createFromAngle(-45).times(150);
					if (shootCount == 1) shootDir = Point.createFromAngle(-22.5f).times(150);
					if (shootCount == 2) shootDir = Point.createFromAngle(0).times(150);
					if (shootCount == 3) shootDir = Point.createFromAngle(22.5f).times(150);
					if (shootCount == 4) shootDir = Point.createFromAngle(45f).times(150);
					new StunShotProj(vile.grenadeWeapon, poi.Value, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(shootDir.x * character.xDir, shootDir.y), rpc: true);
					shootCount++;
				}
			} else if (vile.grenadeWeapon.type == (int)VileBallType.PeaceOutRoller) {
				if (!shot && character.sprite.frameIndex == 2) {
					if (!vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
						character.changeState(new Crouch(""), true);
						return;
					}
					shot = true;
					vile.setVileShootTime(vile.grenadeWeapon);
					var poi = character.sprite.getCurrentFrame().POIs[0];
					poi.x *= character.xDir;
					Projectile proj = new PeaceOutRollerProj(vile.grenadeWeapon, character.pos.add(poi), character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
					proj.vel = new Point(character.xDir * 150, -200);
					proj.gravityModifier = 1;
				}
			}
		} else {
			shootTime += Global.spf;
			var poi = character.getFirstPOI();
			if (shootTime > 0.06f && poi != null) {
				if (!vile.tryUseVileAmmo(2)) {
					character.changeState(new Crouch(""), true);
					return;
				}
				shootTime = 0;
				character.playSound("flamethrower");
				new FlamethrowerProj(vile.flamethrowerWeapon, poi.Value, character.xDir, true, player, player.getNextActorNetId(), sendRpc: true);
			}

			if (character.loopCount > 4) {
				character.changeState(new Crouch(""), true);
				return;
			}
		}

		if (character.isAnimOver()) {
			character.changeState(new Crouch(""), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		vile = character as Vile ?? throw new NullReferenceException();
	}
}

public class MK2NapalmGrenadeProj : Projectile {
	public MK2NapalmGrenadeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
		base(weapon, pos, xDir, 150, 1, player, "napalm_grenade2", 0, 0.2f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.NapalmGrenade2;
		this.vel = new Point(speed * xDir, -200);
		useGravity = true;
		collider.wallOnly = true;
		fadeSound = "explosion";
		fadeSprite = "explosion";
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (grounded) {
			destroySelf();
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		Point destroyPos = other?.hitData?.hitPoint ?? pos;
		changePos(destroyPos);
		destroySelf();
	}

	public override void onDestroy() {
		if (!ownedByLocalPlayer) return;
		new MK2NapalmProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
	}
}

public class MK2NapalmProj : Projectile {
	float flameCreateTime = 1;
	public MK2NapalmProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 100, 1f, player, "napalm2_proj", 0, 1f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 2;
		projId = (int)ProjIds.Napalm2;
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (ownedByLocalPlayer) {
			flameCreateTime += Global.spf;
			if (flameCreateTime > 0.1f) {
				flameCreateTime = 0;
				new MK2NapalmFlame(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
			}
		}

		var hit = Global.level.checkTerrainCollisionOnce(this, vel.x * Global.spf, 0, null);
		if (hit?.gameObject is Wall && hit?.hitData?.normal != null && !(hit.hitData.normal.Value.isAngled())) {
			if (ownedByLocalPlayer) {
				new MK2NapalmWallProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
			}
			destroySelf();
		}
	}
}


public class MK2NapalmFlame : Projectile {
	public MK2NapalmFlame(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 1, player, "napalm2_flame", 0, 1f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.Napalm2Flame;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = true;
		shouldShieldBlock = false;
		gravityModifier = 0.25f;
	}

	public override void update() {
		base.update();
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
			return;
		}
		if (loopCount > 8) {
			destroySelf(disableRpc: true);
			return;
		}
	}
	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
	}
}

public class MK2NapalmWallProj : Projectile {
	public MK2NapalmWallProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 2, player, "napalm2_wall", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 1f;
		projId = (int)ProjIds.Napalm2Wall;
		vel = new Point(0, -200);
		destroyOnHit = false;
		shouldShieldBlock = false;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
		}
	}
}


public class SplashHitGrenadeProj : Projectile {
	bool exploded;
	public SplashHitGrenadeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 150, 2, player, "napalm_sh_grenade", 0, 0.2f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.NapalmGrenadeSplashHit;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		this.vel = new Point(speed * xDir, -200);
		useGravity = true;
		collider.wallOnly = true;
		fadeSound = "explosion";
		fadeSprite = "explosion";
		shouldShieldBlock = false;
	}

	public override void update() {
		base.update();
		if (grounded) {
			explode();
		}
	}

	public override void onHitWall(CollideData other) {
		explode();
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		explode();
	}

	public void explode() {
		if (exploded) return;
		exploded = true;
		if (ownedByLocalPlayer) {
			var hit = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, 100), new List<Type>() { typeof(Wall) });
			new SplashHitProj(
				weapon, hit?.getHitPointSafe() ?? pos, xDir,
				owner, owner.getNextActorNetId(), sendRpc: true
			);
		}
		destroySelf();
	}
}

public class SplashHitProj : Projectile {
	Player player;
	public SplashHitProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, 1, 0, 1, player, "napalm_sh_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.NapalmSplashHit;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		destroyOnHit = false;
		maxTime = 1.5f;
		this.player = player;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
	}

	public override bool shouldDealDamage(IDamagable damagable) {
		if (damagable is Actor actor && MathF.Abs(pos.x - actor.pos.x) > 40) {
			return false;
		}
		return true;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable is Character chr) {
			float modifier = 1;
			if (chr.isUnderwater()) modifier = 2;
			if (chr.isImmuneToKnockback()) return;
			float xMoveVel = MathF.Sign(pos.x - chr.pos.x);
			chr.move(new Point(xMoveVel * 50 * modifier, 0));
		}
	}
}
