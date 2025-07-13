using System;
using System.Collections.Generic;

namespace MMXOnline;

public class AcidBurst : Weapon {
	public static AcidBurst netWeapon = new();

	public AcidBurst() : base() {
		displayName = "Acid Burst";
		shootSounds = new string[] { "acidBurst", "acidBurst", "acidBurst", "acidBurst" };
		fireRate = 30;
		index = (int)WeaponIds.AcidBurst;
		weaponBarBaseIndex = 17;
		weaponBarIndex = 17;
		weaponSlotIndex = 17;
		killFeedIndex = 40;
		weaknessIndex = (int)WeaponIds.FrostShield;
		damage = "1/1";
		effect = "DOT: 2+1/3+1. Reduces Enemy Defense. Acid DMG \nstops when cured or immersed in water. Doesn't Assists.";
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (chargeLevel < 3) {
			new AcidBurstProj(pos, xDir, mmx, player, player.getNextActorNetId(), true);
		} else {
			player.setNextActorNetId(player.getNextActorNetId());
			new AcidBurstProjCharged(pos, xDir, 0, mmx, player, player.getNextActorNetId(true), true);
			new AcidBurstProjCharged(pos, xDir, 1, mmx, player, player.getNextActorNetId(true), true);
		}
	}
}

public class AcidBurstProj : Projectile {
	public AcidBurstProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "acidburst_proj", netId, player	
	) {
		weapon = AcidBurst.netWeapon;
		useGravity = true;
		maxTime = 1.5f;
		projId = (int)ProjIds.AcidBurst;
		vel = new Point(xDir * 100, -200);
		fadeSound = "acidBurst";
		checkUnderwater();

		// TODO: Fix this
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	
	public static Projectile rpcInvoke(ProjParameters args) {
		return new AcidBurstProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		checkUnderwater();
	}

	public void checkUnderwater() {
		if (isUnderwater()) {
			new BubbleAnim(pos, "bigbubble1") { vel = new Point(0, -75) };
			Global.level.delayedActions.Add(new DelayedAction(() => { new BubbleAnim(pos, "bigbubble2") { vel = new Point(0, -75) }; }, 0.1f));
			destroySelf();
			return;
		}
	}

	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;
		acidSplashEffect(other, ProjIds.AcidBurstSmall);
		destroySelf();
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) acidFadeEffect();
		base.onHitDamagable(damagable);
	}
}

public class AcidBurstProjSmall : Projectile {
	public AcidBurstProjSmall(
		Point pos, int xDir, Point vel, bool isSeahorse,
		ProjIds projId, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "acidburst_proj_small", netId, player	
	) {
		weapon = isSeahorse ? ToxicSeahorse.netWeapon : AcidBurst.netWeapon;
		useGravity = true;
		maxTime = 1.5f;
		this.projId = (int)projId;
		fadeSprite = "acidburst_fade";
		this.vel = vel;
		// TODO: Fix this
		canBeLocal = false;

		if (rpc) {
			byte[] extraArgs = new byte[] {  isSeahorse ? (byte)1 : (byte)0, (byte)(vel.x + 128),
			(byte)(vel.y + 128), (byte)projId }; 
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new AcidBurstProjSmall(
		 	arg.pos, arg.xDir, new Point(arg.extraData[0] - 128, arg.extraData[1]- 128), arg.extraData[2] == 1, 
			(ProjIds)arg.extraData[3], arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		checkUnderwater();
	}

	public void checkUnderwater() {
		if (isUnderwater()) {
			destroySelfNoEffect();
		}
	}

	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;
		destroySelf();
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) acidFadeEffect();
		base.onHitDamagable(damagable);
	}
}

public class AcidBurstProjCharged : Projectile {
	int bounces = 0;
	public AcidBurstProjCharged(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "acidburst_charged_start", netId, player	
	) {
		weapon = AcidBurst.netWeapon;
		maxTime = 4f;
		projId = (int)ProjIds.AcidBurstCharged;
		useGravity = true;
		fadeSound = "acidBurst";
		if (type == 0) {
			vel = new Point(xDir * 75, -270);
		} else if (type == 1) {
			vel = new Point(xDir * 150, -200);
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
		checkBigAcidUnderwater();
		
		// TODO: Fix this
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new AcidBurstProjCharged(
			arg.pos, arg.xDir, arg.extraData[0], 
			arg.owner, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (sprite.name == "acidburst_charged_start" && isAnimOver()) {
			changeSprite("acidburst_charged", true);
			vel.x = xDir * 100;
		}

		checkBigAcidUnderwater();
	}

	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;
		acidSplashEffect(other, ProjIds.AcidBurstSmall);
		bounces++;
		if (bounces > 4) {
			destroySelf();
			return;
		}

		var normal = other.hitData.normal ?? new Point(0, -1);

		if (normal.isSideways()) {
			vel.x *= -1;
			incPos(new Point(5 * MathF.Sign(vel.x), 0));
		} else {
			vel.y *= -1;
			if (vel.y < -300) vel.y = -300;
			incPos(new Point(0, 5 * MathF.Sign(vel.y)));
		}
		playSound("acidBurst", sendRpc: true);
	}

	bool acidSplashOnce;
	public override void onHitDamagable(IDamagable damagable) {
		if (ownedByLocalPlayer) {
			if (!acidSplashOnce) {
				acidSplashOnce = true;
				acidSplashParticles(pos, false, 1, 1, ProjIds.AcidBurstSmall);
				acidFadeEffect();
			}
		}
		base.onHitDamagable(damagable);
	}
}
