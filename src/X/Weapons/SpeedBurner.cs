using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SpeedBurner : Weapon {
	public static SpeedBurner netWeapon = new(); 

	public SpeedBurner() : base() {
		displayName = "Speed Burner";
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		shootSounds = new string[] { "speedBurner", "speedBurner", "speedBurner", "speedBurnerCharged" };
		fireRate = 60;
		switchCooldown = 45;
		index = (int)WeaponIds.SpeedBurner;
		weaponBarBaseIndex = 16;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 16;
		killFeedIndex = 27;
		weaknessIndex = (int)WeaponIds.BubbleSplash;
		damage = "2/4";
		effect = "Fire DOT: 1. Charged Grants Super Armor. Self Damage\non contact of a wall. Burn won't give assists.";
		hitcooldown = "0";
		flinch = "0/26";
		flinchCD = "0/0.5";
		hasCustomChargeAnim = true;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (chargeLevel < 3) {
			if (!character.isUnderwater()) {
				new SpeedBurnerProj(pos, xDir, mmx, player, player.getNextActorNetId(), true);
			} else {
				player.setNextActorNetId(player.getNextActorNetId());
				new SpeedBurnerProjWater(pos, xDir, 0, mmx, player, player.getNextActorNetId(true), true);
				new SpeedBurnerProjWater(pos, xDir, 1, mmx, player, player.getNextActorNetId(true), true);
			}
		} else {
			if (character.ownedByLocalPlayer) {
				character.changeState(new SpeedBurnerCharState(), true);
			}
		}
	}
}

public class SpeedBurnerProj : Projectile {
	float groundSpawnTime;
	float airSpawnTime;
	int groundSpawns;
	public SpeedBurnerProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "speedburner_start", netId, player	
	) {
		weapon = SpeedBurner.netWeapon;
		damager.damage = 2;
		vel = new Point(275 * xDir, 0);
		maxTime = 0.6f;
		projId = (int)ProjIds.SpeedBurner;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SpeedBurnerProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (sprite.name == "speedburner_start") {
			if (isAnimOver()) {
				changeSprite("speedburner_proj", true);
			}
		}
		Helpers.decrementTime(ref groundSpawnTime);
		Helpers.decrementTime(ref airSpawnTime);

		if (airSpawnTime == 0) {
			var anim = new Anim(
				pos.addxy(Helpers.randomRange(-10, 10),
				Helpers.randomRange(-10, 10)), "speedburner_dust", xDir, null, true
			);
			anim.vel.x = 50 * xDir;
			anim.vel.y = 10;
			airSpawnTime = 0.05f;
		}
		if (!ownedByLocalPlayer) {
			return;
		}
		CollideData? hit = Global.level.raycast(pos, pos.addxy(0, 18), [typeof(Wall)]);

		if (hit != null && groundSpawnTime == 0) {
			Point spawnPos = pos.addxy((groundSpawns * -15 + 10) * xDir, 0);
			spawnPos.y = hit.hitData.hitPoint?.y - 1 ?? pos.y;
			new SpeedBurnerProjGround(
				spawnPos, xDir, this, damager.owner, damager.owner.getNextActorNetId(), rpc: true
			);
			groundSpawns++;

			groundSpawnTime = 0.075f;
		}
	}
}

public class SpeedBurnerProjWater : Projectile {
	float initY;
	float offsetTime;
	float smokeTime;
	public SpeedBurnerProjWater(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "speedburner_underwater", netId, player	
	) {
		weapon = SpeedBurner.netWeapon;
		damager.damage = 1;
		vel = new Point(275 * xDir, 0);
		maxTime = 0.6f;
		projId = (int)ProjIds.SpeedBurnerWater;
		initY = pos.y;
		if (type == 1) {
			offsetTime = MathF.PI / 2;
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SpeedBurnerProjWater(
			args.pos, args.xDir, args.extraData[0],
			args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		smokeTime += Global.spf;
		if (smokeTime > 0.1f) {
			smokeTime = 0;
			new Anim(pos, "torpedo_smoke", xDir, null, true);
		}

		var y = initY + MathF.Sin((Global.level.time + offsetTime) * 10) * 15;
		changePos(new Point(pos.x, y));
	}
}

public class SpeedBurnerProjGround : Projectile {
	public SpeedBurnerProjGround(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "speedburner_ground", netId, player	
	) {
		weapon = SpeedBurner.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 15;
		vel = new Point(275 * xDir, 0);
		maxTime = 0.4f;
		destroyOnHit = true;
		frameIndex = Helpers.randomRange(0, 2);
		projId = (int)ProjIds.SpeedBurnerTrail;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new SpeedBurnerProjGround(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class SpeedBurnerCharState : CharState {
	Anim? proj;

	public SpeedBurnerCharState() : base("speedburner") {
		superArmor = true;
		immuneToWind = true;
	}

	public override void update() {
		base.update();

		if (character.isUnderwater() && proj != null) {
			proj.destroySelf();
			proj = null;
		}

		character.move(new Point(character.xDir * 350, 0));

		CollideData? collideData = Global.level.checkTerrainCollisionOnce(character, character.xDir, 0);
		if (collideData != null && collideData.isSideWallHit() && character.ownedByLocalPlayer) {
			character.applyDamage(2, player, character, (int)WeaponIds.SpeedBurner, (int)ProjIds.SpeedBurnerRecoil);
			//character.changeState(new Hurt(-character.xDir, Global.defFlinch, 0), true);
			character.changeToIdleOrFall();
			character.playSound("hurt", sendRpc: true);
			return;
		} else if (stateTime > 0.6f) {
			character.changeToIdleOrFall();
			return;
		}

		if (proj != null) {
			proj.changePos(character.pos.addxy(0, -15));
			proj.xDir = character.xDir;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel.y = 0;
		if (!character.isUnderwater()) {
			proj = new Anim(character.pos, "speedburner_charged", character.xDir, player.getNextActorNetId(), false, sendRpc: true);
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
		if (proj != null && !proj.destroyed) proj.destroySelf();
	}
}
