using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SonicSlicer : Weapon {
	public static SonicSlicer netWeapon = new();

	public SonicSlicer() : base() {
		shootSounds = new string[] { "sonicSlicer", "sonicSlicer", "sonicSlicer", "sonicSlicerCharged" };
		fireRate = 60;
		switchCooldown = 45;
		index = (int)WeaponIds.SonicSlicer;
		weaponBarBaseIndex = 13;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 13;
		killFeedIndex = 24;
		weaknessIndex = (int)WeaponIds.CrystalHunter;
		damage = "2/4";
		effect = "U: Bounces on Wall. Breaks W.Sponge Shield.\nC: Decreases vertical speed drastically.";
		hitcooldown = "0/0.25";
		Flinch = "0/26";
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new SonicSlicerStart(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		} else {
			new Anim(pos, "sonicslicer_charge_start", xDir, null, true);
			player.setNextActorNetId(player.getNextActorNetId());
			new SonicSlicerProjCharged( pos, 0, xDir, mmx, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged( pos, 1, xDir, mmx, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged( pos, 2, xDir, mmx, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged( pos, 3, xDir, mmx, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged( pos, 4, xDir, mmx, player, player.getNextActorNetId(true), true);
		}
	}
}

public class SonicSlicerStart : Projectile {
	public SonicSlicerStart(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "sonicslicer_start", netId, player	
	) {
		weapon = SonicSlicer.netWeapon;
		damager.damage = 1;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.SonicSlicerStart;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SonicSlicerStart(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (sprite.isAnimOver()) {
			if (ownedByLocalPlayer) {
				new SonicSlicerProj(pos, xDir, 0, this, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
				new SonicSlicerProj(pos, xDir, 1, this, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
			}
			destroySelf();
		}
	}
}

public class SonicSlicerProj : Projectile {
	public Sprite twin;
	int type;
	public SonicSlicerProj(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "sonicslicer_proj", netId, player	
	) {
		weapon = SonicSlicer.netWeapon;
		damager.damage = 2;
		vel = new Point(200 * xDir, 0);
		maxTime = 0.75f;
		this.type = type;
		collider.wallOnly = true;
		projId = (int)ProjIds.SonicSlicer;

		twin = new Sprite("sonicslicer_twin");

		vel.y = 50;
		if (type == 1) {
			vel.x *= 1.25f;
			frameIndex = 1;
		}
		if (type == 1) {
			vel.y = 0;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SonicSlicerProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (type == 0) vel.y -= Global.spf * 100;
		else vel.y -= Global.spf * 50;

		var collideData = Global.level.checkTerrainCollisionOnce(this, xDir, 0, vel);
		if (collideData != null && collideData.hitData != null) {
			playSound("dingX2");
			xDir *= -1;
			vel.x *= -1;
			new Anim(pos, "sonicslicer_sparks", xDir, null, true);
			//RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
		}

		int velYSign = MathF.Sign(vel.y);
		if (velYSign != 0) {
			collideData = Global.level.checkTerrainCollisionOnce(this, 0, velYSign, vel);
			if (collideData != null && collideData.hitData != null) {
				playSound("dingX2");
				vel.y *= -1;
				new Anim(pos, "sonicslicer_sparks", xDir, null, true);
				//RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		float ox = -vel.x * Global.spf * 3;
		float oy = -vel.y * Global.spf * 3;
		twin.draw(frameIndex, pos.x + x + ox, pos.y + y + oy, 1, 1, null, 0.5f, 1, 1, zIndex);
	}
}

public class SonicSlicerProjCharged : Projectile {
	public Point dest;
	public bool fall;
	public SonicSlicerProjCharged(
		Point pos, int num, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "sonicslicer_charged", netId, player	
	) {
		weapon = SonicSlicer.netWeapon;
		damager.damage = 4;
		damager.hitCooldown = 15;
		damager.flinch = Global.defFlinch;
		vel = new Point(250 * xDir, 0);
		fadeSprite = "sonicslicer_charged_fade";
		maxTime = 1;
		projId = (int)ProjIds.SonicSlicerCharged;
		destroyOnHit = true;

		if (num == 0) dest = pos.addxy(-60, -100);
		if (num == 1) dest = pos.addxy(-30, -100);
		if (num == 2) dest = pos.addxy(-0, -100);
		if (num == 3) dest = pos.addxy(30, -100);
		if (num == 4) dest = pos.addxy(60, -100);

		vel.x = 0;
		useGravity = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)num);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SonicSlicerProjCharged(
			args.pos, args.extraData[0], args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!fall) {
			float x = Helpers.lerp(pos.x, dest.x, Global.spf * 10);
			changePos(new Point(x, pos.y));
			vel.y += -40;
		}
		if (vel.y <= -375) fall = true;
		if (vel.y > 100) yDir = -1;
		if (fall) vel.y += 30;
		
	}
}
