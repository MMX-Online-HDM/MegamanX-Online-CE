using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ElectricSpark : Weapon {
	public static ElectricSpark netWeapon = new();

	public ElectricSpark() : base() {
		displayName = "Electric Spark";
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
		flinch = "6/26";
		flinchCD = "1/0";
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new ElectricSparkProj(pos, xDir, mmx, player, 0, player.getNextActorNetId(), rpc: true);
		} else {
			new ElectricSparkProjChargedStart(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		}
	}
}

public class ElectricSparkProj : Projectile {
	public int type = 0;
	public bool split = false;
	public ElectricSparkProj(
		Point pos, int xDir, Actor owner, Player player, int type, ushort netProjId,
		(int x, int y)? vel = null, bool rpc = false
	) : base(
		pos, xDir, owner, "electric_spark", netProjId, player	
	) {
		weapon = ElectricSpark.netWeapon;
		damager.damage = 2;
		damager.flinch = Global.miniFlinch;
		projId = (int)ProjIds.ElectricSpark;
		maxTime = 1.2f;
		if (type == 0) {
			base.vel = new Point(150 * xDir, 0);
		}
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
			arg.pos, arg.xDir, arg.owner,
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

            Point normal2 =  new Point(0, 1);
			if (normal != null) normal2 = (Point)normal;
            normal2.multiply(150 * 3);

			new ElectricSparkProj(
				pos.clone(), xDir, this , damager.owner, 1,
				Global.level.mainPlayer.getNextActorNetId(), ((int)normal2.x, (int)normal2.y), true
			);
			new ElectricSparkProj(
				pos.clone(), xDir, this, damager.owner, 2,
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
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "electric_spark_charge_start", netId, player
	) {
		weapon = ElectricSpark.netWeapon;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.ElectricSparkChargedStart;
		destroyOnHit = false;
		shouldShieldBlock = false;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		projId = (int)ProjIds.ElectricSparkCharged;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ElectricSparkProjChargedStart(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (sprite.isAnimOver()) {
			destroySelf();
			if (ownedByLocalPlayer) {
				new ElectricSparkProjCharged(
					pos.addxy(-1 * xDir, 0), -1 * xDir, this, damager.owner,
					damager.owner.getNextActorNetId(true), rpc: true
				);
				new ElectricSparkProjCharged(
					pos.addxy(1 * xDir, 0), 1 * xDir, this, damager.owner,
					damager.owner.getNextActorNetId(true), rpc: true
				);
			}
		}
	}
}

public class ElectricSparkProjCharged : Projectile {
	public ElectricSparkProjCharged(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "electric_spark_charge", netId, player	
	) {
		weapon = ElectricSpark.netWeapon;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		vel = new Point(450 * xDir, 0);
		projId = (int)ProjIds.ElectricSparkCharged;
		maxTime = 0.3f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ElectricSparkProjCharged(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
