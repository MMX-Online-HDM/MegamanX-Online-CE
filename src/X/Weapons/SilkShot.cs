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
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel >= 3) {
			new SilkShotProjCharged(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		// } else if (chargeLevel >= 2) {
		//		new SilkShotProjLv2(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		} else if (chargeLevel < 3) {
			new SilkShotProj(pos, xDir, mmx, player, player.getNextActorNetId(), true);
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
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "silkshot_proj", netId, player	
	) {
		weapon = SilkShot.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 0;
		damager.flinch = 0;
		vel = new Point(200 * xDir, 0);
		maxTime = 6f;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		useGravity = true;
		vel.y = -100;
		projId = (int)ProjIds.SilkShot;
		healAmount = 2;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		frameSpeed = 0;
		frameIndex = Helpers.randomRange(0, sprite.totalFrameNum - 1);
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SilkShotProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
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
				pos, xDir, this, damager.owner, 0, 64 * i + 32,
				damager.owner.getNextActorNetId(), rpc: true
			);
		}
	}
}

public class SilkShotProjShrapnel : Projectile {
	public SilkShotProjShrapnel(
		Point pos, int xDir, Actor owner, Player player, int type,
		float byteAngle, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "silkshot_piece", netId, player	
	) {
		weapon = SilkShot.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 30;
		vel = new Point(300 * xDir, 0);
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
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SilkShotProjShrapnel(
			args.pos, args.xDir, args.owner, args.player,
			args.extraData[0], args.byteAngle, args.netId
		);
	}
}

public class SilkShotProjCharged : Projectile {
	bool splitOnce;
	public SilkShotProjCharged(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "silkshot_proj_charged", netId, player	
	) {
		weapon = SilkShot.netWeapon;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		vel = new Point(200 * xDir, 0);
		maxTime = 6f;
		fadeSprite = "explosion";
		fadeSound = "silkShotChargedExplosion";
		useGravity = true;
		vel.y = -100;
		projId = (int)ProjIds.SilkShotCharged;
		healAmount = 6;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SilkShotProjCharged(
			args.pos, args.xDir, args.owner, args.player, args.netId
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
				pos, xDir, this, damager.owner, i % 2, i * 32,
				damager.owner.getNextActorNetId(), rpc: true
			);
		}
	}
}


public class SilkShotProjLv2 : Projectile {
	bool splitOnce;
	public SilkShotProjLv2(
		Point pos, int xDir, Actor owner, Player player, ushort? netProjId, bool rpc = false
	) : base(
		pos, xDir, owner, "silkshot_proj", netProjId, player	
	) {
		weapon = SilkShot.netWeapon;
		damager.damage = 3;
		damager.flinch = Global.defFlinch;
		vel = new Point(200 * xDir, 0);
		maxTime = 6f;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		frameSpeed = 0;
		useGravity = true;
		vel.y = -100;
		projId = (int)ProjIds.SilkShotChargedLv2;
		healAmount = 6;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SilkShotProjLv2(
			arg.pos, arg.xDir, arg.owner, arg.player,  arg.netId
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
				pos, xDir, this, damager.owner, 0, i * 32,
				damager.owner.getNextActorNetId(), rpc: true
			);
		}
	}
}
