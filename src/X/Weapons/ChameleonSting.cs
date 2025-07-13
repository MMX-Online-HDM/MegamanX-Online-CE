using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ChameleonSting : Weapon {
	public static ChameleonSting netWeapon = new();
	public bool freeAmmoNextCharge;

	public ChameleonSting() : base() {
		displayName = "Chameleon Sting";
		index = (int)WeaponIds.ChameleonSting;
		killFeedIndex = 2;
		weaponBarBaseIndex = 2;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 2;
		weaknessIndex = (int)WeaponIds.BoomerangCutter;
		shootSounds = ["csting", "csting", "csting", "stingCharge"];
		fireRate = 45;
		damage = "2";
		effect = "Splits. \nFull Charge grants invulnerability.";
		hitcooldown = "0";
	}

	public override float getAmmoUsageEX(int chargeLevel, Character character) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		if (chargeLevel < 3 && mmx.stingActiveTime > 0) {
			return 4;
		}
		if (chargeLevel >= 3 && freeAmmoNextCharge) {
			freeAmmoNextCharge = false;
			return 0;
		}
		return getAmmoUsage(chargeLevel);
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new StingProj(pos, xDir, mmx, player, 0, player.getNextActorNetId(), true);
		} else {
			if (mmx.stingActiveTime > 0 && args.Length >= 1 && args[1] != 0) {
				mmx.specialBuster.shoot(character, [3, 1]);
				freeAmmoNextCharge = true;
				return;
			}
			mmx.stingActiveTime = 480;
		}
	}
}

public class StingProj : Projectile {
	public int type = 0; //0 = initial proj, 1 = horiz, 2 = down, 3 = up
	public StingProj(
		Point pos, int xDir, Actor owner, Player player, int type, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "sting_start", netId, player
	) {
		damager.damage = 2;
		damager.hitCooldown = 0.25f;
		vel = new Point(300 * xDir, 0);
		weapon = ChameleonSting.netWeapon;
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
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new StingProj(
			args.pos, args.xDir, args.owner, args.player, args.extraData[0], args.netId
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
						pos.addxy(15 * xDir, 0), xDir, this, damager.owner,
						1, Global.level.mainPlayer.getNextActorNetId(), rpc: true
					);
					new StingProj(
						pos.addxy(15 * xDir, 8), xDir, this, damager.owner,
						2, Global.level.mainPlayer.getNextActorNetId(), rpc: true
					);
					new StingProj(
						pos.addxy(15 * xDir, -8), xDir, this, damager.owner,
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
