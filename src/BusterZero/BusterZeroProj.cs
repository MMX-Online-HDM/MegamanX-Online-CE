namespace MMXOnline;

public class DZBusterProj : Projectile {
	bool deflected;

	public DZBusterProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster1", netId, player	
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 1;
		vel = new Point(240 * xDir, 0);
		fadeSprite = "buster1_fade";
		reflectable = true;
		maxTime = 0.5175f;
		projId = (int)ProjIds.DZBuster;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
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
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class DZBuster2Proj : Projectile {
	public DZBuster2Proj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zbuster2", netId, player	
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 2;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.DZBuster2;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZBuster2Proj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class DZBuster3Proj : Projectile {
	float partTime;

	public DZBuster3Proj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zbuster4", netId, player	
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.DZBuster3;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
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
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class DZHadangekiProj : Projectile {
	public DZHadangekiProj(
		Point pos, int xDir, bool isBZ, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zsaber_shot", netId, player
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 3;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "zsaber_shot_fade";
		reflectable = true;
		projId = (int)ProjIds.DZHadangeki;
		maxTime = 0.5f;
		if (isBZ) {
			damager.damage = 4;
			genericShader = player.zeroPaletteShader;
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, isBZ ? (byte)1 : (byte)0);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZHadangekiProj(
			args.pos, args.xDir, args.extraData[0] == 1, args.owner, args.player, args.netId
		);
	}
}
