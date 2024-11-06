using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SonicSlicer : Weapon {

	public static SonicSlicer netWeapon = new();

	public SonicSlicer() : base() {
		shootSounds = new string[] { "sonicSlicer", "sonicSlicer", "sonicSlicer", "sonicSlicerCharged" };
		fireRate = 60;
		index = (int)WeaponIds.SonicSlicer;
		weaponBarBaseIndex = 13;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 13;
		killFeedIndex = 24;
		weaknessIndex = (int)WeaponIds.CrystalHunter;
		damage = "2/4";
		effect = "Bounces on Wall. Breaks W.Sponge Shield.";
		hitcooldown = "0/0.25";
		Flinch = "0/26";
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new SonicSlicerStart(this, pos, xDir, player, player.getNextActorNetId(), true);
		} else {
			new Anim(pos, "sonicslicer_charge_start", xDir, null, true);
			player.setNextActorNetId(player.getNextActorNetId());
			new SonicSlicerProjCharged(this, pos, 0, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged(this, pos, 1, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged(this, pos, 2, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged(this, pos, 3, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged(this, pos, 4, player, player.getNextActorNetId(true), true);
		}
	}
}

public class SonicSlicerStart : Projectile {
	public SonicSlicerStart(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "sonicslicer_start", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.SonicSlicerStart;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SonicSlicerStart(
			SonicSlicer.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (sprite.isAnimOver()) {
			if (ownedByLocalPlayer) {
				new SonicSlicerProj(weapon, pos, xDir, 0, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
				new SonicSlicerProj(weapon, pos, xDir, 1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
			}
			destroySelf();
		}
	}
}

public class SonicSlicerProj : Projectile {
	public Sprite twin;
	int type;
	public SonicSlicerProj(
		Weapon weapon, Point pos, int xDir, int type, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 200, 2, player, "sonicslicer_proj", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
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
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SonicSlicerProj(
			SonicSlicer.netWeapon, arg.pos, arg.xDir, 
			arg.extraData[0], arg.player, arg.netId
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
		Weapon weapon, Point pos, int num, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 300, 4, player, "sonicslicer_charged", 
		Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
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
			rpcCreate(pos, player, netProjId, 1, (byte)num);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SonicSlicerProjCharged(
			SonicSlicer.netWeapon, arg.pos, arg.extraData[0], arg.player, arg.netId
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
