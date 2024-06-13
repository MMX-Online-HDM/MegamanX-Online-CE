using System;

namespace MMXOnline;

public class ZBuster2Proj : Projectile {
	public ZBuster2Proj(
		Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		ZeroBuster.netWeapon, pos, xDir, 350, 2, player, "zbuster2",
		Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		if (type == 0) {
			projId = (int)ProjIds.ZBuster2;
			changeSprite("buster2", true);
		} else {
			projId = (int)ProjIds.DZBuster2;
			damager.flinch = 0;
		}
		ZBuster2Proj.hyorogaCode(this, player);

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
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
		Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		ZeroBuster.netWeapon, pos, xDir, 350, 4, player, "zbuster3",
		Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		if (type == 0) {
			projId = (int)ProjIds.ZBuster3;
		} else {
			projId = (int)ProjIds.DZBuster3;
			damager.flinch = Global.halfFlinch;
			damager.damage = 3;
		}
		ZBuster2Proj.hyorogaCode(this, player);
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}
}

public class ZBuster4Proj : Projectile {
	float partTime;
	public ZBuster4Proj(
		Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		ZeroBuster.netWeapon, pos, xDir, 350, 6, player, "zbuster4",
		Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		if (type == 0) {
			projId = (int)ProjIds.ZBuster4;
		} else {
			projId = (int)ProjIds.DZBuster4;
			damager.damage = 3;
			damager.flinch = Global.halfFlinch;
		}
		ZBuster2Proj.hyorogaCode(this, player);
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
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
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		ZSaber.staticWeapon, pos, xDir, 300, 3, player, "zsaber_shot",
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "zsaber_shot_fade";
		reflectable = true;
		projId = (int)ProjIds.ZSaberProj;
		if (player.character is Zero zero) {
			if (zero.isBlack == true) {
				genericShader = player.zeroPaletteShader;
			}
			if (zero.isAwakened == true) {
				genericShader = player.zeroAzPaletteShader;
			}
		}
		
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
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

	public ShingetsurinProj(
		Point pos, int xDir, float startTime, Player player, ushort netProjId, bool rpc = false
	) : base(
		Shingetsurin.netWeapon, pos, xDir, 150, 2, player, "shingetsurin_proj",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 3f;
		destroyOnHit = false;
		time = startTime;
		//vel.x *= (1 - startTime);
		projId = (int)ProjIds.Shingetsurin;
		ZBuster2Proj.hyorogaCode(this, player);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
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
				if (target != null) {
					vel = pos.directionToNorm(target.getCenterPos()).times(speed);
				}
			}
			forceNetUpdateNextFrame = true;
		}
	}
}

public class GenmuProj : Projectile {
	public int type = 0;
	public float initY = 0;

	public GenmuProj(
		Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false
	) : base(
		Genmu.netWeapon, pos, xDir, 300, 12, player, "genmu_proj",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		this.type = type;
		initY = pos.y;
		maxTime = 0.5f;
		destroyOnHit = false;
		xScale = 0.75f;
		yScale = 0.75f;
		projId = (int)ProjIds.Gemnu;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
	}

	public override void update() {
		base.update();

		float y = 0;
		if (type == 0) y = initY + MathF.Sin(time * 8) * 50;
		else y = initY + MathF.Sin(-time * 8) * 50;
		changePos(new Point(pos.x, y));
	}
}
