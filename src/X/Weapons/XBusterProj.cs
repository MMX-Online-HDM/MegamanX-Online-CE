using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;
public class BusterProj : Projectile {
	public BusterProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster1", netId, player
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 0;
		damager.flinch = 0;
		vel = new Point(250 * xDir, 0);
		fadeSprite = "buster1_fade";
		reflectable = true;
		maxTime = 0.5175f;
		projId = (int)ProjIds.Buster;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BusterProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (MathF.Abs(vel.x) < 360 && reflectCount == 0) {
			vel.x += Global.spf * xDir * 900f;
			if (MathF.Abs(vel.x) >= 360) {
				vel.x = (float)xDir * 360;
			}
		}
	}
}

public class Buster2Proj : Projectile {
	public Buster2Proj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster2", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 2;
		vel = new Point(350 * xDir, 0);
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster2;
		fadeOnAutoDestroy = true;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new Buster2Proj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class Buster3LightProj : Projectile {
	public Buster3LightProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster3", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster3;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new Buster3LightProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class Buster3GigaProj : Projectile {
	public Buster3GigaProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster3_x2", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster3Giga;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new Buster3GigaProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class Buster3MaxProj : Projectile {
	float partTime;
	public Buster3MaxProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster3_x3", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster3Max;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public override void update() {
		base.update();
		partTime += Global.spf;
		if (partTime > 0.05f) {
			partTime = 0;
			new Anim(pos.addRand(0, 16), "buster4_x3_part", 1, null, true) { acc = new Point(-vel.x * 3f, 0) };
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new Buster3MaxProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class Buster4GigaProj: Projectile {
	public Buster4GigaProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster3_x2", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster4Giga;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new Buster4GigaProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class Buster4Giga2Proj: Projectile {
	public List<Sprite> spriteMids = new List<Sprite>();
	public Buster4Giga2Proj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster4_x2", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		vel = new Point(350 * xDir, 0);
		fadeOnAutoDestroy = true;
		fadeSprite = "buster4_x2_fade";
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster4Giga2;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		for (int i = 0; i < 6; i++) {
			var midSprite = new Sprite("buster4_x2_orbit");
			spriteMids.Add(midSprite);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new Buster4Giga2Proj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
	public override void render(float x, float y) {
		base.render(x, y);
		float piHalf = MathF.PI / 2;
		float xOffset = 8;
		float partTime = (time * 0.75f);
		for (int i = 0; i < 6; i++) {
			float t = 0;
			float xOff2 = 0;
			float sinX = 0;
			if (i < 3) {
				t = partTime - (i * 0.025f);
				xOff2 = i * xDir * 3;
				sinX = 5 * MathF.Cos(partTime * 20);
			} else {
				t = partTime + (MathF.PI / 4) - ((i - 3) * 0.025f);
				xOff2 = (i - 3) * xDir * 3;
				sinX = 5 * MathF.Sin((partTime) * 20);
			}
			float sinY = 15 * MathF.Sin(t * 20);
			long zIndexTarget = zIndex - 1;
			float currentOffset = (t * 20) % (MathF.PI * 2);
			if (currentOffset > piHalf && currentOffset < piHalf * 3) {
				zIndexTarget = zIndex + 1;
			}
			spriteMids[i].draw(
				spriteMids[i].frameIndex,
				pos.x + x + sinX - xOff2 + xOffset,
				pos.y + y + sinY, xDir, yDir,
				getRenderEffectSet(), 1, 1, 1, zIndexTarget
			);
			spriteMids[i].update();
		}
	}
}
public class Buster4MaxProj: Projectile {
	float partTime;
	public Buster4MaxProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster4_x3", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		maxTime = 0.85f;
		projId = (int)ProjIds.Buster4Max;
		vel.x = 0;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public override void update() {
		base.update();
		vel.x += Global.spf * xDir * 550;
		if (MathF.Abs(vel.x) > 300) vel.x = 300 * xDir;
		partTime += Global.spf;
		if (partTime > 0.05f) {
			partTime = 0;
			new Anim(pos.addRand(0, 16), "buster4_x3_part", 1, null, true) { acc = new Point(-vel.x * 3f, 0) };
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new Buster4MaxProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class Buster4Proj : Projectile {
	public int type = 0;
	public float offsetTime = 0;
	public float initY = 0;
	bool smoothStart = false;

	public Buster4Proj(
		Point pos, int xDir, Actor owner, Player player,
		int type, float offsetTime, ushort netId,
		bool smoothStart = false, bool rpc = false
	) : base(
		pos, xDir, owner, "buster4", netId, player

	) {
		weapon = XBuster.netWeapon;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 60;
		fadeSprite = "buster4_fade";
		this.type = type;
		initY = this.pos.y;
		this.smoothStart = smoothStart;
		maxTime = 0.6f;
		projId = (int)ProjIds.Buster4;
		vel = new Point(396*xDir,0);
		if (rpc) {
			byte[] extraArgs = [(byte)type, (byte)offsetTime, (byte)(smoothStart ? 1 : 0)];
			rpcCreate(pos, player, netId, xDir, extraArgs);
		}

		this.offsetTime = offsetTime / 60f;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new Buster4Proj(
			arg.pos, arg.xDir, arg.owner, arg.player,
			arg.extraData[0], arg.extraData[1], arg.netId
		);
	}

	public override void update() {
		base.update();
		base.frameIndex = type;
		float currentOffsetTime = offsetTime;
		if (smoothStart && time < 5f / 60f) {
			currentOffsetTime *= (time / 5f) * 60f;
		}
		float y = initY + (MathF.Sin((time + currentOffsetTime) * (MathF.PI * 6)) * 15f);
		changePos(new Point(pos.x, y));
	}
}
public class BusterX3Proj2 : Projectile {
	public int type = 0;
	public List<Point> lastPositions = new List<Point>();
	public BusterX3Proj2(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, type == 0 || type == 3 ? "buster4_x3_orbit" : "buster4_x3_orbit2", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 1;
		vel = new Point(400 * xDir, 0);
		fadeSprite = "buster4_fade";
		this.type = type;
		reflectable = true;
		maxTime = 0.675f;
		projId = (int)ProjIds.BusterX3Proj2;
		if (type == 0) vel = new Point(-200 * xDir, -100);
		if (type == 1) vel = new Point(-150 * xDir, -50);
		if (type == 2) vel = new Point(-150 * xDir, 50);
		if (type == 3) vel = new Point(-200 * xDir, 100);
		frameSpeed = 0;
		frameIndex = 0;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BusterX3Proj2(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		float maxSpeed = 600;
		vel.inc(new Point(Global.spf * 1500 * xDir, 0));
		if (MathF.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
		lastPositions.Add(pos);
		if (lastPositions.Count > 4) lastPositions.RemoveAt(0);
	}

	public override void render(float x, float y) {
		string spriteName = type == 0 || type == 3 ? "buster4_x3_orbit" : "buster4_x3_orbit2";
		//if (lastPositions.Count > 3) Global.sprites[spriteName].draw(1, lastPositions[3].x + x, lastPositions[3].y + y, 1, 1, null, 1, 1, 1, zIndex);
		if (lastPositions.Count > 2) Global.sprites[spriteName].draw(2, lastPositions[2].x + x, lastPositions[2].y + y, 1, 1, null, 1, 1, 1, zIndex);
		if (lastPositions.Count > 1) Global.sprites[spriteName].draw(3, lastPositions[1].x + x, lastPositions[1].y + y, 1, 1, null, 1, 1, 1, zIndex);
		base.render(x, y);
	}
}

public class BusterPlasmaProj : Projectile {
	public HashSet<IDamagable> hitDamagables = new HashSet<IDamagable>();
	public BusterPlasmaProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster_plasma", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 15;
		vel = new Point(400 * xDir, 0);
		maxTime = 0.5f;
		projId = (int)ProjIds.BusterX3Plasma;
		destroyOnHit = false;
		xScale = 0.75f;
		yScale = 0.75f;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BusterPlasmaProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer && hitDamagables.Count < 1) {
			if (!hitDamagables.Contains(damagable)) {
				hitDamagables.Add(damagable);
				float xThreshold = 10;
				Point targetPos = damagable.actor().getCenterPos();
				float distToTarget = MathF.Abs(pos.x - targetPos.x);
				Point spawnPoint = pos;
				if (distToTarget > xThreshold) spawnPoint = new Point(targetPos.x + xThreshold * Math.Sign(pos.x - targetPos.x), pos.y);
				new BusterPlasmaHitProj(spawnPoint, xDir, this, owner, owner.getNextActorNetId(), rpc: true);
			}
		}
	}
}

public class BusterPlasmaHitProj : Projectile {
	public BusterPlasmaHitProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster_plasma_hit", netId, player	
	) {
		weapon = XBuster.netWeapon;
		damager.damage = 1;
		damager.hitCooldown = 15;
		maxTime = 2f;
		projId = (int)ProjIds.BusterX3PlasmaHit;
		destroyOnHit = false;
		netcodeOverride = NetcodeModel.FavorDefender;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BusterPlasmaHitProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
