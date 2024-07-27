using System.Collections.Generic;

namespace MMXOnline;

public class ElectricSpark : Weapon {
	public ElectricSpark() : base() {
		index = (int)WeaponIds.ElectricSpark;
		killFeedIndex = 6;
		weaponBarBaseIndex = 6;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 6;
		weaknessIndex = 8;
		shootSounds = new string[] { "electricSpark", "electricSpark", "electricSpark", "electricSpark" };
		rateOfFire = 0.5f;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		if (chargeLevel < 3) {
			new ElectricSparkProj(this, pos, xDir, player, 0, netProjId);
		} else {
			new ElectricSparkProjChargedStart(this, pos, xDir, player, netProjId);
		}
	}
}

public class ElectricSparkProj : Projectile {
	public int type = 0;
	public bool split = false;
	public ElectricSparkProj(
		Weapon weapon, Point pos, int xDir,
		Player player, int type, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 150, 2, player, "electric_spark",
		Global.miniFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.ElectricSpark;
		maxTime = 1.2f;

		if (type == 1) {
			maxTime = 0.4f;
			vel.x = 0;
			vel.y = 450;
		}
		else if (type == 2) {
			maxTime = 0.4f;
			vel.x = 0;
			vel.y = -450;
		} 

		fadeSprite = "electric_spark_fade";
		//this.fadeSound = "explosion";
		this.type = type;
		reflectable = true;
		shouldShieldBlock = false;

		if (rpc) {
			byte[] extraArgs;
			extraArgs = new byte[] { (byte)type };
			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public override void onHitWall(CollideData other) {
		if (split) return;
		if (type == 0) {
			destroySelf(fadeSprite);
			split = true;
			if (!ownedByLocalPlayer) {
				return;
			}

			new ElectricSparkProj(
				weapon, pos.clone(), xDir, damager.owner, 1,
				Global.level.mainPlayer.getNextActorNetId(), true
			);
			new ElectricSparkProj(
				weapon, pos.clone(), xDir, damager.owner, 2,
				Global.level.mainPlayer.getNextActorNetId(), rpc: true
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
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId
	) : base(
		weapon, pos, xDir, 0, 4, player, "electric_spark_charge_start",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.ElectricSparkCharged;
		destroyOnHit = false;
		shouldShieldBlock = false;
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
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
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
}
