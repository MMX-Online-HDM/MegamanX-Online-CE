using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SilkShot : Weapon {
	public static SilkShot netWeapon = new();

	public SilkShot() : base() {
		shootSounds = new string[] { "silkShot", "silkShot", "silkShot", "silkShotCharged" };
		fireRate = 45;
		index = (int)WeaponIds.SilkShot;
		weaponBarBaseIndex = 11;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 11;
		killFeedIndex = 20 + (index - 9);
		weaknessIndex = (int)WeaponIds.SpeedBurner;
		damage = "2+1/4+1";
		effect = "Able to heal allies.\nRewards one metal by healing 16 HP.";
		Flinch = "0/26";
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel >= 3) {
			new SilkShotProjCharged(this, pos, xDir, player, player.getNextActorNetId(), true);
		} else if (chargeLevel >= 2) {
			new SilkShotProjLv2(pos, xDir, player, player.getNextActorNetId(), true);
		} else {
			new SilkShotProj(this, pos, xDir, player, player.getNextActorNetId(), true);
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) return 8;
		if (chargeLevel >= 2) return 4;
		else return 1;
	}
}

public class SilkShotProj : Projectile {
	bool splitOnce;
	public SilkShotProj(
		Weapon weapon, Point pos, int xDir, Player player, 
		ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 200, 2, player, "silkshot_proj", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 6f;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		useGravity = true;
		vel.y = -100;
		projId = (int)ProjIds.SilkShot;
		healAmount = 2;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		frameSpeed = 0;
		frameIndex = Helpers.randomRange(0, sprite.totalFrameNum - 1);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SilkShotProj(
			SilkShot.netWeapon, arg.pos, 
			arg.xDir, arg.player,  arg.netId
		);
	}

	public override void onHitWall(CollideData other) {
		destroySelf(disableRpc: true);
		if (!ownedByLocalPlayer) return;
		split();
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) {
			split();
		}
		base.onHitDamagable(damagable);
	}

	public void split() {
		if (!ownedByLocalPlayer) return;
		if (!splitOnce) splitOnce = true;
		else return;
		for (int i = 0; i < 4; i++) {
			new SilkShotProjShrapnel(
				weapon, pos, xDir, damager.owner, 0, 64 * i + 32,
				damager.owner.getNextActorNetId(), rpc: true
			);
		}
	}
}

public class SilkShotProjShrapnel : Projectile {
	public SilkShotProjShrapnel(
		Weapon weapon, Point pos, int xDir, Player player, int type,
		float byteAngle, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 300, 1, player, "silkshot_piece", 
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		reflectable = true;
		this.vel.x = MathInt.SquareSinB(byteAngle) * 300f;
		this.vel.y = MathInt.SquareCosB(byteAngle) * 300f;
		projId = (int)ProjIds.SilkShotShrapnel;
		healAmount = 1;
		if (type == 1) {
			changeSprite("silkshot_proj", true);
		}
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)byteAngle, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SilkShotProjShrapnel(
			SilkShot.netWeapon, arg.pos, arg.xDir, arg.player, 
			arg.extraData[1], arg.extraData[0], arg.netId
		);
	}
}

public class SilkShotProjCharged : Projectile {
	bool splitOnce;
	public SilkShotProjCharged(
		Weapon weapon, Point pos, int xDir, Player player,
		ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 200, 4, player, "silkshot_proj_charged",
		Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 6f;
		fadeSprite = "explosion";
		fadeSound = "silkShotChargedExplosion";
		useGravity = true;
		vel.y = -100;
		projId = (int)ProjIds.SilkShotCharged;
		healAmount = 6;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SilkShotProjCharged(
			SilkShot.netWeapon, arg.pos, 
			arg.xDir, arg.player,  arg.netId
		);
	}

	public override void onHitWall(CollideData other) {
		destroySelf(disableRpc: true);
		if (!ownedByLocalPlayer) return;
		split();
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) split();
		base.onHitDamagable(damagable);
	}

	public void split() {
		if (!splitOnce) splitOnce = true;
		else return;

		for (int i = 0; i < 8; i++) {
			new SilkShotProjShrapnel(
				weapon, pos, xDir, damager.owner, i % 2, i * 32,
				damager.owner.getNextActorNetId(), rpc: true
			);
		}
	}
}


public class SilkShotProjLv2 : Projectile {
	bool splitOnce;
	public SilkShotProjLv2(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SilkShot.netWeapon, pos, xDir, 200, 3, player, "silkshot_proj",
		Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 6f;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		frameSpeed = 0;
		useGravity = true;
		vel.y = -100;
		projId = (int)ProjIds.SilkShotChargedLv2;
		healAmount = 6;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SilkShotProjLv2(
			arg.pos, arg.xDir, arg.player,  arg.netId
		);
	}

	public override void onHitWall(CollideData other) {
		if (ownedByLocalPlayer) {
			split();
		}
		destroySelf(disableRpc: true);
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) {
			split();
		}
		base.onHitDamagable(damagable);
	}

	public void split() {
		if (!splitOnce) splitOnce = true;
		else return;

		for (int i = 0; i < 8; i++) {
			new SilkShotProjShrapnel(
				weapon, pos, xDir, damager.owner, 0, i * 32,
				damager.owner.getNextActorNetId(), rpc: true
			);
		}
	}
}
