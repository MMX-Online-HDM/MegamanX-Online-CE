

using System;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class BusterZeroMelee : CharState {
	bool fired;
	[AllowNull]
	public BusterZero zero;

	public BusterZeroMelee() : base("projswing") {
		landSprite = "projswing";
		airSprite = "projswing_air";
		airMove = true;
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 4 && !fired) {
			fired = true;
			character.playSound("ZeroSaberX3", sendRpc: true);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded || character.canAirJump()) &&
				player.input.isPressed(Control.Jump, player)
			) {
				if (!character.grounded) {
					character.dashedInAir++;
				}
				character.vel.y = -character.getJumpPower();
				sprite = "projswing_air";
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero = character as BusterZero;
		if (zero == null) {
			throw new NullReferenceException();
		}
		if (!character.grounded || character.vel.y < 0) {
			sprite = "projswing_air";
			character.changeSpriteFromName(sprite, true);
		}
	}

	public override void onExit(CharState oldState) {
		base.onEnter(oldState);
		zero.zSaberCooldown = 36f/60f;
	}
}

public class BusterZeroDoubleBuster : CharState {
	public bool fired1;
	public bool fired2;
	public bool isSecond;
	public bool shootPressedAgain;
	public bool isPinkCharge;
	[AllowNull]
	BusterZero zero;

	public BusterZeroDoubleBuster(bool isSecond, bool isPinkCharge) : base("doublebuster") {
		this.isSecond = isSecond;
		this.isPinkCharge = isPinkCharge;
		airMove = true;
		superArmor = true;
		landSprite = "doublebuster";
		airSprite = "doublebuster_air";
	}

	public override void update() {
		base.update();
		if (player.input.isPressed(Control.Shoot, player)) {
			shootPressedAgain = true;
		}
		if (!fired1 && character.frameIndex == 3) {
			fired1 = true;
			character.playSound("buster3X3", sendRpc: true);
			new ZBuster4Proj(
				zero.busterWeapon, character.getShootPos(), character.getShootXDir(),
				1, player, player.getNextActorNetId(), rpc: true
			);
		}
		if (!fired2 && character.frameIndex == 7) {
			fired2 = true;
			if (!isPinkCharge) {
				zero.stockedBusterLv = 0;
				character.playSound("buster3X3", sendRpc: true);
				new ZBuster4Proj(
					zero.busterWeapon, character.getShootPos(),
					character.getShootXDir(), 1, player, player.getNextActorNetId(), rpc: true
				);
			} else {
				zero.stockedBusterLv = 0;
				character.playSound("buster2X3", sendRpc: true);
				new ZBuster2Proj(
					zero.busterWeapon, character.getShootPos(), character.getShootXDir(),
					1, player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else if (!isSecond && character.frameIndex >= 4 && !shootPressedAgain) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded || character.canAirJump()) &&
				player.input.isPressed(Control.Jump, player)
			) {
				if (!character.grounded) {
					character.dashedInAir++;
				}
				character.vel.y = -character.getJumpPower();
				sprite = "doublebuster_air";
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero = character as BusterZero;
		if (zero == null) {
			throw new NullReferenceException();
		}
		// Non-full charge.
		if (isPinkCharge) {
			zero.stockedBusterLv = 1;
		}
		// Full charge.
		else {
			// We add Z-Saber charge if we fire the full charge and we were at 0 charge before.
			if (zero.stockedBusterLv != 2 || !isSecond) {
				zero.stockedSaber = true;
			}
			zero.stockedBusterLv = 2;
		}
		if (!character.grounded || character.vel.y < 0) {
			sprite = "doublebuster_air";
			character.changeSpriteFromName(sprite, true);
		}
		if (isSecond) {
			character.frameIndex = 4;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		// We check if we fired the second shot. If not we add the stocked charge.
		if (!fired2) {
			if (isPinkCharge) {
				zero.stockedBusterLv = 1;
			} else {
				zero.stockedBusterLv = 2;
				zero.stockedSaber = true;
			}
		}
	}
}

public class BusterZeroHadangeki : CharState {
	bool fired;
	[AllowNull]
	public BusterZero zero;

	public BusterZeroHadangeki() : base("projswing") {
		landSprite = "projswing";
		airSprite = "projswing_air";
		airMove = true;
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 4 && !fired) {
			character.playSound("ZeroSaberX3", sendRpc: true);
			zero.stockedSaber = false;
			fired = true;
			new ZSaberProj(
				new ZSaber(player), character.pos.addxy(30 * character.xDir, -20),
				character.xDir, player, player.getNextActorNetId(), rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded || character.canAirJump()) &&
				player.input.isPressed(Control.Jump, player)
			) {
				if (!character.grounded) {
					character.dashedInAir++;
				}
				character.vel.y = -character.getJumpPower();
				sprite = "projswing_air";
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero = character as BusterZero;
		if (zero == null) {
			throw new NullReferenceException();
		}
		if (!character.grounded || character.vel.y < 0) {
			sprite = "projswing_air";
			defaultSprite = sprite;
			character.changeSpriteFromName(sprite, true);
		}
	}

	public override void onExit(CharState oldState) {
		base.onEnter(oldState);
		zero.zSaberCooldown = 36f/60f;
	}
}
