using System;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class DZBusterProj : Projectile {
	bool deflected;

	public DZBusterProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		ZeroBuster.netWeapon, pos, xDir,
		240, 1, player, "buster1", 0, 0,
		netId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster1_fade";
		reflectable = true;
		maxTime = 0.5175f;
		projId = (int)ProjIds.DZBuster;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (!deflected && System.MathF.Abs(vel.x) < 360) {
			vel.x += Global.spf * xDir * 900f;
			if (System.MathF.Abs(vel.x) >= 360) {
				vel.x = (float)xDir * 360;
			}
		}
	}

	public override void onDeflect() {
		base.onDeflect();
		deflected = true;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZBusterProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}
}

public class DZBuster2Proj : Projectile {
	public DZBuster2Proj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		ZeroBuster.netWeapon, pos, xDir,
		350, 2, player, "zbuster2", 0, 0,
		netId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.DZBuster2;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZBuster2Proj(
			args.pos, args.xDir, args.player, args.netId
		);
	}
}

public class DZBuster3Proj : Projectile {
	float partTime;

	public DZBuster3Proj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		ZeroBuster.netWeapon, pos, xDir,
		350, 3, player, "zbuster4", Global.halfFlinch, 0,
		netId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.DZBuster4;
		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public override void update() {
		base.update();
		partTime += Global.speedMul;
		if (partTime >= 5) {
			partTime = 0;
			new Anim(
				pos.addxy(-20 * xDir, 0).addRand(0, 12),
				"zbuster4_part", 1, null, true
			) {
				vel = vel,
				acc = new Point(-vel.x * 2, 0)
			};
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZBuster3Proj(
			args.pos, args.xDir, args.player, args.netId
		);
	}
}

public class DZHadangekiProj : Projectile {
	public DZHadangekiProj(
		Point pos, int xDir, bool isBZ, Player player, ushort? netId, bool rpc = false
	) : base(
		ZeroBuster.netWeapon, pos, xDir,
		350, 3, player, "zsaber_shot", Global.halfFlinch, 0,
		netId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "zsaber_shot_fade";
		reflectable = true;
		projId = (int)ProjIds.ZSaberProj;
		maxTime = 0.5f;
		if (isBZ) {
			damager.damage = 4;
			genericShader = player.zeroPaletteShader;
		}
		if (rpc) {
			rpcCreate(pos, player, netId, xDir, (isBZ ? (byte)0 : (byte)1));
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZHadangekiProj(
			args.pos, args.xDir, args.extraData[0] == 1, args.player, args.netId
		);
	}
}
