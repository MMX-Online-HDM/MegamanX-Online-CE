using System;
using System.Collections.Generic;

namespace MMXOnline;

public enum NapalmType {
	None = -3,
	NoneFlamethrower = -2,
	NoneBall = -1,
	RumblingBang,
	FireGrenade,
	SplashHit,
}

public class Napalm : Weapon {
	public float vileAmmoUsage;
	public static Napalm netWeaponRB = new Napalm(NapalmType.RumblingBang);
	public static Napalm netWeaponFG = new Napalm(NapalmType.FireGrenade);
	public static Napalm netWeaponSH = new Napalm(NapalmType.SplashHit);
	public Napalm(NapalmType napalmType) : base() {
		index = (int)WeaponIds.Napalm;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 30;
		type = (int)napalmType;
		if (napalmType == NapalmType.None) {
			displayName = "None";
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
		} else if (napalmType == NapalmType.NoneBall) {
			displayName = "None(GRENADE)";
			description = new string[] { "Do not equip a Napalm.", "GRENADE will be used instead." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
			effect = "Equip Grenade.";
		} else if (napalmType == NapalmType.NoneFlamethrower) {
			displayName = "None(FLAMETHROWER)";
			description = new string[] { "Do not equip a Napalm.", "FLAMETHROWER will be used instead." };
			killFeedIndex = 126;
			ammousage = 0;
			vileAmmoUsage = 0;
			fireRate = 0;
			vileWeight = 0;
			effect = "Equip Flamethrower.";
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
			hitcooldown = "1/0.5";
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
		if (shootCooldown > 0) return;
		if (vile.napalmWeapon.type == (int)NapalmType.None) return;
		vile.changeState(new NapalmAttackNapalm(), true);
	}
}

public class NapalmGrenadeProj : Projectile {
	bool exploded;
	public NapalmGrenadeProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "napalm_grenade", netId, player
	) {
		weapon = Napalm.netWeaponRB;
		projId = (int)ProjIds.RumblingBangGrenade;
		damager.damage = 2;
		damager.hitCooldown = 12;
		vel = new Point(150 * xDir, -200);
		useGravity = true;
		collider.wallOnly = true;
		fadeSound = "explosionX3";
		fadeSprite = "explosion";
		shouldShieldBlock = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new NapalmGrenadeProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
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
				new NapalmPartProj(
					pos, xDir, this, owner,
					owner.getNextActorNetId(),
					distance * xDir, rpc: true
				);
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
		Point pos, int xDir, Actor owner, Player player, ushort? netId, int xDist, bool rpc = false
	) : base(
		pos, xDir, owner, "napalm_part", netId, player
	) {
		weapon = Napalm.netWeaponRB;
		damager.damage = 1;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.RumblingBangProj;
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
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)xDist);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new NapalmPartProj(
			args.pos, args.xDir, args.owner, args.player, args.netId, args.extraData[0]
		);
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
		} else if (grounded && useGravity) {
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

public class MK2NapalmGrenadeProj : Projectile {
	public MK2NapalmGrenadeProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "napalm_grenade2", netId, player
	) {
		weapon = Napalm.netWeaponFG;
		damager.damage = 1;
		damager.hitCooldown = 12;
		projId = (int)ProjIds.FlameRoundGrenade;
		this.vel = new Point(150 * xDir, -200);
		useGravity = true;
		collider.wallOnly = true;
		fadeSound = "explosionX3";
		fadeSprite = "explosion";
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new MK2NapalmGrenadeProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
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
		new MK2NapalmProj(pos, xDir, this, owner, owner.getNextActorNetId(), rpc: true);
	}
}

public class MK2NapalmProj : Projectile {
	float flameCreateTime = 1;
	public MK2NapalmProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "napalm2_proj", netId, player
	) {
		weapon = Napalm.netWeaponFG;
		damager.damage = 1;
		damager.hitCooldown = 60;
		vel = new Point(100 * xDir, 0);
		maxTime = 2;
		projId = (int)ProjIds.FlameRoundProj;
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new MK2NapalmProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		flameCreateTime += Global.spf;
		if (flameCreateTime > 6f / 60f) {
			flameCreateTime = 0;
			new MK2NapalmFlame(pos, xDir, this, owner, owner.getNextActorNetId(), rpc: true);
		}
		var hit = Global.level.checkTerrainCollisionOnce(this, vel.x * Global.spf, 0, null);
		if (hit?.gameObject is Wall && hit?.hitData?.normal != null && !(hit.hitData.normal.Value.isAngled())) {
			new MK2NapalmWallProj(pos, xDir, this, owner, owner.getNextActorNetId(), rpc: true);
			destroySelf();
		}
	}
	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
	}
}

public class MK2NapalmFlame : Projectile {
	public MK2NapalmFlame(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "napalm2_flame", netId, player
	) {
		weapon = Napalm.netWeaponFG;
		damager.damage = 1;
		damager.hitCooldown = 60;
		projId = (int)ProjIds.FlameRoundFlameProj;
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = true;
		shouldShieldBlock = false;
		gravityModifier = 0.25f;
		maxTime = 48f / 60f;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new MK2NapalmFlame(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

	}
	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
	}
}

public class MK2NapalmWallProj : Projectile {
	public MK2NapalmWallProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "napalm2_wall", netId, player
	) {
		weapon = Napalm.netWeaponFG;
		damager.damage = 2;
		damager.hitCooldown = 30;
		maxTime = 1f;
		projId = (int)ProjIds.FlameRoundWallProj;
		vel = new Point(0, -200);
		destroyOnHit = false;
		shouldShieldBlock = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new MK2NapalmWallProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
		}
	}
}

public class SplashHitGrenadeProj : Projectile {
	public SplashHitGrenadeProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "napalm_sh_grenade", netId, player
	) {
		weapon = Napalm.netWeaponSH;
		damager.damage = 2;
		damager.hitCooldown = 12;
		projId = (int)ProjIds.SplashHitGrenade;
		vel = new Point(150 * xDir, -200);
		fadeSound = "explosionX3";
		fadeSprite = "explosion";
		useGravity = true;
		collider.wallOnly = true;
		shouldShieldBlock = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SplashHitGrenadeProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
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
		new SplashHitProj(
			pos, xDir, this, owner,
			owner.getNextActorNetId(), rpc: true
		);
	}
}

public class SplashHitProj : Projectile {
	public SplashHitProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "napalm_sh_proj", netId, player
	) {
		weapon = Napalm.netWeaponSH;
		damager.damage = 1;
		damager.hitCooldown = 30;
		vel = new Point(0, 0);
		projId = (int)ProjIds.SplashHitProj;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		destroyOnHit = false;
		maxTime = 1.5f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SplashHitProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
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
		if (!damagable.isPlayableDamagable()) { return; }
		if (damagable is not Actor actor || !actor.ownedByLocalPlayer) {
			return;
		}
		float modifier = 1;
		if (actor.grounded) { modifier = 0.5f; };
		if (damagable is Character character) {
			if (character.isPushImmune()) { return; }
			if (character.charState is Crouch) { modifier = 0.25f; }
		}
		if (actor.isUnderwater()) { modifier = 2; }
		float xMoveVel = MathF.Sign(pos.x - actor.pos.x);
		actor.move(new Point(xMoveVel * 50 * modifier, 0));
	}
}

public abstract class NapalmAttackTypes : VileState {
	public string sound = "";
	public bool soundPlayed;
	public int soundFrame = Int32.MaxValue;
	public bool shot;

	public NapalmAttackTypes(string spr) : base(spr) {
	}

	public override void update() {
		base.update();
		if (character.sprite.frameIndex >= soundFrame && !soundPlayed) {
			character.playSound(sound, forcePlay: false, sendRpc: true);
			soundPlayed = true;
		}
		if (character.isAnimOver()) {
			character.changeState(new Crouch(""), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.turnToInput(player.input, player);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (vile.grenadeWeapon.type == (int)VileBallType.NoneNapalm) {
			if (vile.napalmWeapon.type == (int)NapalmType.RumblingBang) {
				vile.napalmWeapon.shootCooldown = 120;
				vile.grenadeWeapon.shootCooldown = 120;
			} else if (vile.napalmWeapon.type == (int)NapalmType.FireGrenade) {
				vile.napalmWeapon.shootCooldown = 240;
				vile.grenadeWeapon.shootCooldown = 240;
			} else if (vile.napalmWeapon.type == (int)NapalmType.SplashHit) {
				vile.napalmWeapon.shootCooldown = 180;
				vile.grenadeWeapon.shootCooldown = 180;
			}
		}
	}
}
public class NapalmAttackNapalm : NapalmAttackTypes {
	public NapalmAttackNapalm() : base("crouch_nade") {
		sound = "FireNappalmMK2";
		soundFrame = 2;
	}
	public override void update() {
		base.update();
		if (!shot && character.sprite.frameIndex == 2) {
			shot = true;
			vile.setVileShootTime(vile.napalmWeapon);
			var poi = character.sprite.getCurrentFrame().POIs[0];
			poi.x *= character.xDir;
			if (vile.napalmWeapon.type == (int)NapalmType.RumblingBang) {
				vile.setVileShootTime(vile.napalmWeapon);
				new NapalmGrenadeProj(
					character.pos.add(poi), character.xDir, vile, character.player,
					character.player.getNextActorNetId(), rpc: true
				);
			} else if (vile.napalmWeapon.type == (int)NapalmType.FireGrenade) {
				new MK2NapalmGrenadeProj(
					character.pos.add(poi), character.xDir, vile, character.player,
					character.player.getNextActorNetId(), rpc: true
				);
			} else if (vile.napalmWeapon.type == (int)NapalmType.SplashHit) {
				new SplashHitGrenadeProj(
					character.pos.add(poi), character.xDir, vile, character.player,
					character.player.getNextActorNetId(), rpc: true
				);
			}
		}
	}
}
public class NapalmAttackBombs : NapalmAttackTypes {
	int bombNum = 0;
	public NapalmAttackBombs() : base("crouch_nade") {
		sound = "FireNappalmMK2";
		soundFrame = 2;
	}
	public override void update() {
		base.update();
		int xDir = vile.xDir;
		if (vile.grenadeWeapon.type == (int)VileBallType.ExplosiveRound &&
			character.frameIndex == 2 && bombNum <= 2
		) {
			if (vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
				new VileBombProj(
					character.getCenterPos().addxy(15 * xDir, 2), xDir, 1, vile, player,
					character.player.getNextActorNetId(), rpc: true
				);
				character.sprite.frameIndex = 0;
				bombNum++;
			}
		}
		if (!shot && character.sprite.frameIndex == 2) {
			if (vile.grenadeWeapon.type == (int)VileBallType.SpreadShot) {
				shot = true;
				if (vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
					for (int i = 0; i < 7; i++) {
						new StunShotProj2(
							character.getCenterPos().addxy(20 * xDir, 0), xDir, 0, i + 1, vile,
							character.player, character.player.getNextActorNetId(), rpc: true
						);
					}
				}
			} else if (vile.grenadeWeapon.type == (int)VileBallType.PeaceOutRoller) {
				shot = true;
				if (vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
					new PeaceOutRollerProj(
						character.getCenterPos().addxy(20 * xDir, 0), xDir, 1, vile, player,
						character.player.getNextActorNetId(), rpc: true
					);
				}
			}
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (vile.grenadeWeapon.type == (int)VileBallType.PeaceOutRoller) {
			vile.grenadeWeapon.shootCooldown = 75;
			vile.napalmWeapon.shootCooldown = 75;
		} else {
			vile.grenadeWeapon.shootCooldown = 60;
			vile.napalmWeapon.shootCooldown = 60;
		}
	}
}
public class NapalmAttackFlamethrower : NapalmAttackTypes {
	int bombNum = 0;
	public NapalmAttackFlamethrower() : base("crouch_nade") {
		sound = "FireNappalmMK2";
		soundFrame = 2;
	}
	public override void update() {
		base.update();
		int xDir = vile.xDir;
		if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.DragonsWrath)
			if (character.frameIndex == 2 && bombNum <= 6) {
				if (vile.tryUseVileAmmo(3.5f)) {
					new FlamethrowerDragonsWrath(
						character.getCenterPos().addxy(15 * xDir, 2), character.xDir, true, vile, player,
						player.getNextActorNetId(), rpc: true
					);
					character.sprite.frameIndex = 1;
					bombNum++;
					character.playSound("flamethrower");
				}
			}
		if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.SeaDragonRage)
			if (character.frameIndex == 2 && bombNum <= 5) {
				if (vile.tryUseVileAmmo(3.5f)) {
					new FlamethrowerSeaDragonRage(
						character.getCenterPos().addxy(15 * xDir, 2), character.xDir, true, vile, player,
						player.getNextActorNetId(), rpc: true
					);
					character.sprite.frameIndex = 1;
					bombNum++;
					character.playSound("flamethrower");
				}
			}
		if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.WildHorseKick)
			if (character.frameIndex == 2 && bombNum <= 5) {
				if (vile.tryUseVileAmmo(3.5f)) {
					new FlamethrowerWildHorseKick(
						character.getCenterPos().addxy(15 * xDir, 2), character.xDir, true, vile, player,
						player.getNextActorNetId(), rpc: true
					);
					character.sprite.frameIndex = 1;
					bombNum++;
					character.playSound("flamethrower");
				}
			}
		if (!player.input.isHeld(Control.Special1, player)) {
			character.changeState(new Crouch(""), true);
		}

	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		vile.flamethrowerWeapon.shootCooldown = 60;
		vile.grenadeWeapon.shootCooldown = 60;
	}
}
/*
public class NapalmAttack : VileState {
	bool shot;
	NapalmAttackType napalmAttackType;
	float shootTime;
	int shootCount;

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
						proj = new NapalmGrenadeProj(
							character.pos.add(poi), character.xDir, vile, character.player,
							character.player.getNextActorNetId(), rpc: true
						);
					} else if (vile.napalmWeapon.type == (int)NapalmType.FireGrenade) {
						character.playSound("FireNappalmMK2", forcePlay: false, sendRpc: true);
						proj = new MK2NapalmGrenadeProj(
							character.pos.add(poi), character.xDir, vile, character.player,
							character.player.getNextActorNetId(), rpc: true
						);
					} else if (vile.napalmWeapon.type == (int)NapalmType.SplashHit) {
						proj = new SplashHitGrenadeProj(
							character.pos.add(poi), character.xDir, vile, character.player,
							character.player.getNextActorNetId(), rpc: true
						);
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
					Projectile proj = new VileBombProj(
						character.pos.add(poi), character.xDir, vile,
						player, character.player.getNextActorNetId(), rpc: true
					);
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
					//new StunShotProj(vile.grenadeWeapon, poi.Value, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(shootDir.x * character.xDir, shootDir.y), rpc: true);
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
					Projectile proj = new PeaceOutRollerProj(
						character.pos.add(poi), character.xDir, vile, player, 
						character.player.getNextActorNetId(), rpc: true
					);
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
				if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.DragonsWrath) {
					new FlamethrowerDragonsWrath(
						poi.Value, character.xDir, true, vile, player,
						player.getNextActorNetId(), rpc: true
					);
				}
				else if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.WildHorseKick) {
					new FlamethrowerWildHorseKick(
						poi.Value, character.xDir, true, vile, player,
						player.getNextActorNetId(), rpc: true
					);
				}
				else if (vile.flamethrowerWeapon.type == (int)VileFlamethrowerType.SeaDragonRage) {
					new FlamethrowerSeaDragonRage(
						poi.Value, character.xDir, true, vile, player,
						player.getNextActorNetId(), rpc: true
					);
				}
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
} */
