using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Sting : Weapon {

	public static Sting netWeapon = new();
	public Sting() : base() {
		index = (int)WeaponIds.Sting;
		killFeedIndex = 2;
		weaponBarBaseIndex = 2;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 2;
		weaknessIndex = (int)WeaponIds.Boomerang;
		shootSounds = new string[] { "csting", "csting", "csting", "stingCharge" };
		//rateOfFire = 0.75f;
		fireRateFrames = 45;
		damage = "2";
		effect = "Full Charge grants invulnerability.";
		hitcooldown = "0";
		specialAmmoUse = true;
	}

	public override float getAmmoUsageEX(int chargeLevel, Character character) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (mmx.stingActive) return 8;
		return getAmmoUsage(chargeLevel);
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new StingProj(this, pos, xDir, player, 0, player.getNextActorNetId(), true);
		} else {
			character.stingChargeTime = 8;
		}
	}
}

public class StingProj : Projectile {
	public int type = 0; //0 = initial proj, 1 = horiz, 2 = down, 3 = up
	public StingProj(
		Weapon weapon, Point pos, int xDir, Player player, 
		int type, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 300, 2, player, "sting_start", 
		0, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.Sting;
		maxTime = 0.6f;
		if (type == 1) {
			var sprite = "sting_flat";
			changeSprite(sprite, false);
			reflectable = true;
		} else if (type == 2 || type == 3) {
			var sprite = "sting_up";
			if (type == 3) {
				vel.y = -150;
			} else {
				vel.y = 150;
				yDir = -1;
			}
			changeSprite(sprite, false);
			reflectable = true;
			damager.damage = 2;
			projId = (int)ProjIds.StingDiag;
		}
		fadeSprite = "buster1_fade";
		this.type = type;
		/*
		if (player.character?.isInvisibleBS?.getValue() == true)
		{
			damager.damage = 1;
		}
		*/

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new StingProj(
			Sting.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();
		if (type == 0 && time > 0.05) {
			vel.x = 0;
		}
		if (type == 0) {
			if (isAnimOver()) {
				if (ownedByLocalPlayer) {
					new StingProj(
						weapon, pos.addxy(15 * xDir, 0), xDir, damager.owner,
						1, Global.level.mainPlayer.getNextActorNetId(), rpc: true
					);
					new StingProj(
						weapon, pos.addxy(15 * xDir, 8), xDir, damager.owner,
						2, Global.level.mainPlayer.getNextActorNetId(), rpc: true
					);
					new StingProj(
						weapon, pos.addxy(15 * xDir, -8), xDir, damager.owner,
						3, Global.level.mainPlayer.getNextActorNetId(), rpc: true
					);
				}
				destroySelfNoEffect();
			}
		}
	}

	public override void onReflect() {
		base.onReflect();
		if (sprite.name == "sting_up") {
			yDir *= -1;
		}
		vel.y *= -1;

	}
}
