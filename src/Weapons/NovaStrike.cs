using System;

namespace MMXOnline;

public class NovaStrike : Weapon {
	public const float ammoUsage = 16;
	public NovaStrike(Player? player) : base() {
		if (player != null) {
			damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		}
		shootSounds = new string[] { "", "", "", "" };
		//rateOfFire = 1.5f;
		fireRateFrames = 90;
		index = (int)WeaponIds.NovaStrike;
		weaponBarBaseIndex = 42;
		weaponBarIndex = 36;
		weaponSlotIndex = 95;
		killFeedIndex = 104;
		ammo = 32;
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
	}

	public override void shoot(Character character, int[] args) {
		if (character.ownedByLocalPlayer) {
			Point inputDir = character.player.input.getInputDir(character.player);
			character.changeState(new NovaStrikeState(inputDir), true);
		}
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		mmx.novaStrikeCooldown = fireRateFrames;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (Global.level?.isHyper1v1() == true) {
			return 0;
		}
		return ammoUsage;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return player.character?.flag == null && ammo >= (player.hasChip(3) ? ammoUsage / 2 : ammoUsage);
	}
}

public class NovaStrikeState : CharState {
	int upOrDown;
	int leftOrRight;
	public NovaStrikeState(Point inputDir) : base(getNovaDir(inputDir), "", "", "nova_strike_start") {
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

		if (!once && character.isAnimOver()) {
			once = true;

			if (Helpers.randomRange(0, 10) < 10) {
				character.playSound("novaStrikeX4", forcePlay: false, sendRpc: true);
			} else {
				character.playSound("novaStrikeX6", forcePlay: false, sendRpc: true);
			}
		}

		if (!inTransition()) {
			if (!character.tryMove(new Point(character.xDir * 350 * leftOrRight, 350 * upOrDown), out _) ||
				character.flag != null || stateTime > 0.6f
			) {
				character.changeToIdleOrFall();
				return;
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.stopCharge();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.yDir = 1;
	}

	static string getNovaDir(Point input) {
		if (input.y != 0) {
			if (input.y == -1) return "nova_strike_up";
			return "nova_strike_down";
		}

		return "nova_strike";
	}
}
