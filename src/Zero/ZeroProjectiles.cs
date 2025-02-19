using System;

namespace MMXOnline;

public class ZBuster2Proj : Projectile {
	public ZBuster2Proj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster2", netId, player	
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 2;
		damager.flinch = Global.defFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.ZBuster2;
		ZBuster2Proj.hyorogaCode(this, player);

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ZBuster2Proj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public static void hyorogaCode(Projectile proj, Player player) {
		if (player.character?.sprite?.name?.Contains("hyoroga") == true) {
			proj.xDir = 1;
			proj.angle = 90;
			proj.incPos(new Point(0, 10));
			proj.vel.y = Math.Abs(proj.vel.x);
			proj.vel.x = 0;
		}
	}
}

public class ZBuster3Proj : Projectile {
	public ZBuster3Proj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zbuster2", netId, player	
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.ZBuster3;
		ZBuster2Proj.hyorogaCode(this, player);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ZBuster3Proj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class ZBuster4Proj : Projectile {
	float partTime;
	public ZBuster4Proj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zbuster4", netId, player	
	) {
		weapon = ZeroBuster.netWeapon;
		damager.damage = 6;
		damager.flinch = Global.defFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.ZBuster4;
		ZBuster2Proj.hyorogaCode(this, player);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ZBuster4Proj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		partTime += Global.spf;
		if (partTime > 0.075f) {
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
}

public class ZSaberProj : Projectile {
	public ZSaberProj(
		Point pos, int xDir, bool isAZ, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "zsaber_shot", netId, player	
	) {
		weapon = ZSaber.staticWeapon;
		damager.damage = 3;
		damager.hitCooldown = 30;
		vel = new Point(350 * xDir, 0);
		fadeSprite = "zsaber_shot_fade";
		reflectable = true;
		projId = (int)ProjIds.ZSaberProj;
		if (isAZ) {
			genericShader = player.zeroAzPaletteShader;
		}
		
		if (rpc) {
			rpcCreate(pos, player, netId, xDir, (isAZ ? (byte)1 : (byte)0));
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ZSaberProj(
			args.pos, args.xDir, args.extraData[0] == 1, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (time > 0.5) {
			destroySelf(fadeSprite);
		}
	}
}

public class ShingetsurinProj : Projectile {	
	public Actor? target;
	public float startTime = 0;
	public ShingetsurinProj(
		Point pos, int xDir, float startTime, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "shingetsurin_proj", netId, player

	) {
		weapon = Shingetsurin.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		vel = new Point(150 * xDir, 0);
		maxTime = 3f;
		destroyOnHit = false;
		time = startTime;
		//vel.x *= (1 - startTime);
		projId = (int)ProjIds.Shingetsurin;
		ZBuster2Proj.hyorogaCode(this, player);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)startTime);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ShingetsurinProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (time >= 1 && time < 2) {
			vel = new Point();
		} else if (time >= 2) {
			if (target == null) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, true);
			}
			else if (target != null) {
				vel = pos.directionToNorm(target.getCenterPos()).times(150);
			}
		}
	}
}

public class GenmuProj : Projectile {
	public int type = 0;
	public float initY = 0;

	public GenmuProj(
		Point pos, int xDir, int type, bool isAZ, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "genmu_proj", netId, player
	) {
		weapon = Genmu.netWeapon;
		damager.damage = 12;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		vel = new Point(300 * xDir, 0);
		this.type = type;
		initY = pos.y;
		maxTime = 0.5f;
		destroyOnHit = false;
		xScale = 0.75f;
		yScale = 0.75f;
		projId = (int)ProjIds.Gemnu;
		if (isAZ) {
			genericShader = player.zeroAzPaletteShader;
		}
		if (rpc) {
			rpcCreate(pos, player, netId, xDir, 
			new byte[] { (byte)type, isAZ ? (byte)1 : (byte)0}
			);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new GenmuProj(
			args.pos, args.xDir, args.extraData[0], args.extraData[1] == 1, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		float y = 0;
		if (type == 0) y = initY + MathF.Sin(time * 8) * 50;
		else y = initY + MathF.Sin(-time * 8) * 50;
		changePos(new Point(pos.x, y));
	}
}
