namespace MMXOnline;

public class DZBusterProj : Projectile {
	public bool deflected;

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
		maxTime = 30 / 60f;
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
		Point pos, int xDir, int type, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "zsaber_shot", netId, altPlayer
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 3;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "zsaber_shot_fade";
		reflectable = true;
		projId = (int)ProjIds.DZHadangeki;
		maxTime = 0.5f;
		if (type == 1) {
			damager.flinch = Global.miniFlinch;
			genericShader = ownerPlayer.nightmareZeroShader;
		}
		if (type == 2) {
			damager.damage = 4;
			damager.flinch = Global.miniFlinch;
			genericShader = ownerPlayer.zeroPaletteShader;
		}
		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZHadangekiProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.netId, altPlayer: args.player
		);
	}
}

public class DZShinLemonProj : Projectile {
	public bool deflected;

	public DZShinLemonProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "shinbuster_lemon", netId, player	
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 1;
		vel = new Point(240 * xDir, 0);
		fadeSprite = "shinbuster_lemon_fade";
		reflectable = true;
		maxTime = 34 / 60f;
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
		return new DZShinLemonProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class DZShinBusterProj : Projectile {
	public DZShinBusterProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "shinbuster_large", netId, altPlayer
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 2;
		damager.flinch = Global.halfFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "shinbuster_large_fade";
		reflectable = true;
		maxTime = 38 / 60f;
		projId = (int)ProjIds.DZShinBuster;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZShinBusterProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}
}

public class DZShinGetsurinProj : Projectile {	
	public Point? direction;
	public bool phase2;

	public DZShinGetsurinProj(
		Point pos, int xDir, float startTime, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "shingetsurin_proj", netId, altPlayer

	) {
		weapon = Shingetsurin.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		vel = new Point(250 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		maxTime = 1.5f;
		destroyOnHit = false;
		time = startTime;
		projId = (int)ProjIds.DZShinGetsurin;
		ZBuster2Proj.hyorogaCode(this, ownerPlayer);
		canBeLocal = false;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)startTime);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZShinGetsurinProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (!phase2 && time >= 0.5f) {
			if (time < 1) {
				vel.x = 0;
			} else {
				Actor? target = Global.level.getClosestTarget(pos, damager.owner.alliance, true);
				if (target != null) {
					vel = pos.directionToNorm(target.getCenterPos()).times(250);
					phase2 = true;
				} else {
					vel.x = 250 * xDir;
				}
			}
		}
	}
}

public class DZShinHadangekiProj : Projectile {
	public DZShinHadangekiProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "zsaber_shot", netId, altPlayer
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 3;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		vel = new Point(350 * xDir, 0);
		destroyOnHit = false;
		fadeOnAutoDestroy = true;
		fadeSprite = "zsaber_shot_fade";
		projId = (int)ProjIds.DZShinHadangeki;
		genericShader = ownerPlayer.zeroAzPaletteShader;
		maxTime = 37 / 60f;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public override void update() {
		base.update();
		Actor? closestEnemy = Global.level.getClosestTarget(
			new Point (pos.x + (40 * xDir), pos.y),
			damager.owner.alliance, false, 120
		);
		if (closestEnemy == null) {
			return;
		}
		Point enemyPos = closestEnemy.getCenterPos();

		if (enemyPos.y + 1 < pos.y) {
			moveXY(0, -0.5f);
		}
		if (enemyPos.y - 1 > pos.y) {
			moveXY(0, 0.5f);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new DZShinHadangekiProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}
}
