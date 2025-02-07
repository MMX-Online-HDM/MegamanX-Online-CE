using System;

namespace MMXOnline;

public class HyperNovaStrike : Weapon {
	public static HyperNovaStrike netWeapon = new();
	public const float ammoUsage = 14;

	public HyperNovaStrike() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		shootSounds = new string[] { "", "", "", "" };
		fireRate = 90;
		index = (int)WeaponIds.NovaStrike;
		weaponBarBaseIndex = 42;
		weaponBarIndex = 36;
		weaponSlotIndex = 95;
		killFeedIndex = 104;
		ammo = 28;
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
	}

	public override void shoot(Character character, int[] args) {
		if (character.ownedByLocalPlayer) {
			Point inputDir = character.player.input.getInputDir(character.player);
			character.changeState(new NovaStrikeState(inputDir), true);
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (Global.level?.isHyper1v1() == true) {
			return 0;
		}
		return ammoUsage;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return player.character?.flag == null && ammo >= getChipFactoredAmmoUsage(player);
	}

	public float getChipFactoredAmmoUsage(Player player) {
		return player.character is MegamanX mmx && mmx.hyperArmArmor == ArmorId.Max ? ammoUsage / 2 : ammoUsage;
	}
}

public class NovaStrikeState : CharState {
	int upOrDown;
	int leftOrRight;
	Anim? Nova;
	public NovaStrikeState(Point inputDir) : base("nova_strike") {
		immuneToWind = true;
		invincible = true;
		normalCtrl = false;
		attackCtrl = false;
		useGravity = false;

		if (inputDir.y != 0) upOrDown = (int)inputDir.y;
		else leftOrRight = 1;
	}

	public override void update() {
		base.update();

		if (!once && character.frameIndex >= 4) {
			once = true;
			if (Helpers.randomRange(0, 10) < 10) {
				character.playSound("novaStrikeX4", forcePlay: false, sendRpc: true);
			} else {
				character.playSound("novaStrikeX6", forcePlay: false, sendRpc: true);
			}
		}
		if (Nova != null) {
			Nova.incPos(character.deltaPos);
			if (character.xDir == 1) {
				if (upOrDown == 1) {
					Nova.angle = 90;
				} else if (upOrDown == -1) {
					Nova.angle = 270;
				}
			} else if (character.xDir == -1) {
				if (upOrDown == 1) {
					Nova.angle = 270;
				} else if (upOrDown == -1) {
					Nova.angle = 90;
				}
			}
		}
		if ((character.frameIndex >= 4)) {
			if (!character.tryMove(new Point(character.xDir * 350 * leftOrRight, 350 * upOrDown), out _) ||
				character.flag != null || stateTime > 0.6f
			) {
				character.changeToIdleOrFall();
				return;
			}
		}
		else if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.stopCharge();
		character.visible = false;
		Nova = new Anim(
			character.getCenterPos().addxy(character.xDir*8, 10),
			"mmx_nova_strike", character.xDir, player.getNextActorNetId(), false, sendRpc: true
		);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.yDir = 1;
		character.visible = true;
		Nova?.destroySelf();
	}

}
