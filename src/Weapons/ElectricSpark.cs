using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace MMXOnline;

public class ElectricSpark : Weapon {

	public static ElectricSpark netWeapon = new();
	public ElectricSpark() : base() {
		index = (int)WeaponIds.ElectricSpark;
		killFeedIndex = 6;
		weaponBarBaseIndex = 6;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 6;
		weaknessIndex = (int)WeaponIds.ShotgunIce;
		shootSounds = new string[] { "electricSpark", "electricSpark", "electricSpark", "electricSpark" };
		fireRate = 30;
		damage = "2/4";
		effect =  "Can Split. Charged: Doesn't destroy on hit.";
		hitcooldown = "0/0.5";
		Flinch = "6/26";
		FlinchCD = "1/0";
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new ElectricSparkProj(this, pos, xDir, player, 0, player.getNextActorNetId(), rpc: true);
		} else {
			new ElectricSparkProjChargedStart(this, pos, xDir, player, player.getNextActorNetId(), true);
		}
	}
}

public class ElectricSparkProj : Projectile {
	public int type = 0;
	public bool split = false;
	public ElectricSparkProj(
		Weapon weapon, Point pos, int xDir, Player player, 
		int type, ushort netProjId, (int x, int y)? vel = null, bool rpc = false
	) : base(
		weapon, pos, xDir, 150, 2, player, "electric_spark",
		Global.miniFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.ElectricSpark;
		maxTime = 1.2f;

		if (type >= 1) {
			maxTime = 0.4f;

			if (vel != null) base.vel = new Point(vel.Value.x, vel.Value.y);
		}

		fadeSprite = "electric_spark_fade";
		this.type = type;
		reflectable = true;
		shouldShieldBlock = false;
		canBeLocal = false;

		if (rpc) {
			byte[] extraArgs;

			if (vel != null) {
				extraArgs = new byte[] { 
					(byte)type, 
					(byte)(vel.Value.x + 128),
					(byte)(vel.Value.y + 128) };
			} else {
				extraArgs = new byte[] { (byte)type, (byte)(128 + xDir), 128 };
			}

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ElectricSparkProj(
			ElectricSpark.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.extraData[0], arg.netId,
			(arg.extraData[1] - 128, arg.extraData[2] - 128)
		);
	}

	public override void onHitWall(CollideData other) {
		if (split) return;
		if (type == 0) {
			destroySelf(fadeSprite);
			split = true;
			if (!ownedByLocalPlayer) {
				return;
			}

			var normal = other?.hitData?.normal;
            if (normal != null) {
                if (normal.Value.x == 0) normal = new Point(-1, 0);
                normal = ((Point)normal).leftNormal();
            }

            Point normal2 = (Point)normal;
            normal2.multiply(speed * 3);

			new ElectricSparkProj(
				weapon, pos.clone(), xDir, damager.owner, 1,
				Global.level.mainPlayer.getNextActorNetId(), ((int)normal2.x, (int)normal2.y), true
			);
			new ElectricSparkProj(
				weapon, pos.clone(), xDir, damager.owner, 2,
				Global.level.mainPlayer.getNextActorNetId(), ((int)normal2.x * -1, (int)normal2.y * -1), rpc: true
			);
		}
	}

	public override void onReflect() {
		vel.y *= -1;
		base.onReflect();
	}
}

public class ElectricSparkProjChargedStart : Projectile {
	public ElectricSparkProjChargedStart(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 4, player, "electric_spark_charge_start",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.ElectricSparkChargedStart;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ElectricSparkProjChargedStart(
			ElectricSpark.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (sprite.isAnimOver()) {
			destroySelf();
			if (ownedByLocalPlayer) {
				new ElectricSparkProjCharged(
					weapon, pos.addxy(-1, 0), -1, damager.owner,
					damager.owner.getNextActorNetId(true), rpc: true
				);
				new ElectricSparkProjCharged(
					weapon, pos.addxy(1, 0), 1, damager.owner,
					damager.owner.getNextActorNetId(true), rpc: true
				);
			}
		}
	}
}

public class ElectricSparkProjCharged : Projectile {
	public ElectricSparkProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 450, 4, player, "electric_spark_charge",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.ElectricSparkCharged;
		maxTime = 0.3f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ElectricSparkProjCharged(
			ElectricSpark.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.netId
		);
	}
}
