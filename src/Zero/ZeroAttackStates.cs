using System;

namespace MMXOnline;

public abstract class ZeroGenericMeleeState : ZeroState {
	public int comboFrame = Int32.MaxValue;
	public string sound = "";
	public bool soundPlayed;
	public int soundFrame = Int32.MaxValue;
	public bool exitOnOver = true;

	public ZeroGenericMeleeState(string spr) : base(spr) {
	}

	public override void update() {
		base.update();
		if (character.sprite.frameIndex >= soundFrame && !soundPlayed) {
			character.playSound(sound, forcePlay: false, sendRpc: true);
			soundPlayed = true;
		}
		if (character.sprite.frameIndex >= comboFrame) {
			altCtrls[0] = true;
		}
		if (exitOnOver && character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.turnToInput(player.input, player);
	}

	public virtual bool altCtrlUpdate(bool[] ctrls) {
		return false;
	}
}

public class ZeroSlash1State : ZeroGenericMeleeState {
	public ZeroSlash1State() : base("attack") {
		sound = "saber1";
		soundFrame = 4;
		comboFrame = 6;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero.zeroTripleStartTime = Global.time;
	}


	public override bool altCtrlUpdate(bool[] ctrls) {
		if (zero.specialPressed &&
			zero.specialPressTime > zero.shootPressTime &&
			character.sprite.frameIndex >= 8 
		) {
			zero.groundSpecial.attack2(zero);
			return true;
		}
		if (zero.shootPressed || player.isAI) {
			zero.shootPressTime = 0;
			zero.changeState(new ZeroSlash2State(), true);
			return true;
		}
		return false;
	}
}

public class ZeroSlash2State : ZeroGenericMeleeState {
	public ZeroSlash2State() : base("attack2") {
		sound = "saber2";
		soundFrame = 1;
		comboFrame = 3;
	}

	public override bool altCtrlUpdate(bool[] ctrls) {
		if (zero.specialPressed &&
			zero.specialPressTime > zero.shootPressTime &&
			character.sprite.frameIndex >= 6 
		) {
			zero.groundSpecial.attack2(zero);
			return true;
		}
		if (zero.shootPressed || player.isAI) {
			zero.shootPressTime = 0;
			zero.changeState(new ZeroSlash3State(), true);
			return true;
		}
		return false;
	}
}

public class ZeroSlash3State : ZeroGenericMeleeState {
	public ZeroSlash3State() : base("attack3") {
		sound = "saber3";
		soundFrame = 1;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		zero.zeroTripleSlashEndTime = Global.time;
	}
}

public class ZeroAirSlashState : ZeroGenericMeleeState {
	public ZeroAirSlashState() : base("attack_air") {
		sound = "saber1";
		airSprite = "attack_air";
		landSprite = "attack_air_ground";
		soundFrame = 3;
		comboFrame = 7;

		airMove = true;
		canJump = true;
		exitOnLanding = false;
		useDashJumpSpeed = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (character.sprite.frameIndex >= comboFrame) {
			attackCtrl = true;
		}
	}
}

public class ZeroRollingSlashtate : ZeroGenericMeleeState {
	public ZeroRollingSlashtate() : base("attack_air2") {
		sound = "saber1";
		soundFrame = 1;
		comboFrame = 7;

		airMove = true;
		canJump = true;
		exitOnLanding = true;
		useDashJumpSpeed = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (zero.sprite.loopCount >= 1) {
			character.changeToIdleOrFall();
			return;
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		zero.kuuenzanCooldown = 30;
	}
}

public class ZeroCrouchSlashState : ZeroGenericMeleeState {
	public ZeroCrouchSlashState() : base("attack_crouch") {
		sound = "saber1";
		soundFrame = 1;
	}
}

public class ZeroDashSlashState : ZeroGenericMeleeState {
	public ZeroDashSlashState() : base("attack_dash") {
		sound = "saber1";
		soundFrame = 1;
	}
}

public class ZeroShippuugaState : ZeroGenericMeleeState {
	public ZeroShippuugaState() : base("attack_dash2") {
		sound = "saber1";
		soundFrame = 1;
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		altCtrls[1] = true;
	}
}


public class ZeroMeleeWall : WallSlideAttack {
	bool fired;

	public ZeroMeleeWall(
		int wallDir, Collider wallCollider
	) : base(
		"wall_slide_attack", wallDir, wallCollider
	) {
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		exitOnAnimEnd = true;
		canCancel = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			character.playSound("zerosaberx3", sendRpc: true);
		}
	}
}

public class ZeroDoubleBuster : ZeroState {
	public bool fired1;
	public bool fired2;
	public bool isSecond;
	public bool shootPressedAgain;
	public bool isPinkCharge;

	public ZeroDoubleBuster(bool isSecond, bool isPinkCharge) : base("doublebuster") {
		this.isSecond = isSecond;
		superArmor = true;
		this.isPinkCharge = isPinkCharge;
		airMove = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) return;

		if (player.input.isPressed(Control.Shoot, player)) {
			shootPressedAgain = true;
		}

		if (!fired1 && character.frameIndex == 3) {
			fired1 = true;
			if (!isPinkCharge) {
				character.playSound("buster3X3", sendRpc: true);
				new ZBuster4Proj(
					character.getShootPos(),
					character.getShootXDir(), zero, player, player.getNextActorNetId(), rpc: true
				);
			} else {
				character.playSound("buster2X3", sendRpc: true);
				new ZBuster2Proj(
					character.getShootPos(), character.getShootXDir(),
					zero, player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (!fired2 && character.frameIndex == 7) {
			fired2 = true;
			if (!isPinkCharge) {
				//zero.doubleBusterDone = true;
			} else {
				//character.stockCharge(false);
			}
			character.playSound("buster3X3", sendRpc: true);
			new ZBuster4Proj(
				character.getShootPos(), character.getShootXDir(),
				zero, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else if (!isSecond && character.frameIndex >= 4 && !shootPressedAgain) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded || character.canAirJump() && character.flag == null) &&
				player.input.isPressed(Control.Jump, player)
			) {
				if (!character.grounded) {
					character.dashedInAir++;
				}
				character.vel.y = -character.getJumpPower();
				sprite = "doublebuster_air";
				defaultSprite = sprite;
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);

		if (!isPinkCharge) {
			//character.stockSaber(true);
		} else {
			//character.stockCharge(!isSecond);
		}
		sprite = "doublebuster";
		defaultSprite = sprite;
		landSprite = "doublebuster";
		if (!character.grounded || character.vel.y < 0) {
			defaultSprite = sprite;
			sprite = "doublebuster_air";
		}
		character.changeSpriteFromName(sprite, true);
		if (isSecond) {
			character.frameIndex = 4;
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (isSecond && character is Zero zero) {
			//zero.doubleBusterDone = true;
		}
	}
}

public class AwakenedZeroHadangeki : ZeroState {
	bool fired;

	public AwakenedZeroHadangeki() : base("projswing") {
		landSprite = "projswing";
		airSprite = "projswing_air";
		useDashJumpSpeed = true;
		airMove = true;
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (character.grounded) {
			character.isDashing = false;
		}
		if (character.frameIndex >= 7 && !fired) {
			character.playSound("zerosaberx3", sendRpc: true);
			fired = true;
			new ZSaberProj(
				character.pos.addxy(30 * character.xDir, -20), character.xDir, 
				isAZ: zero.isAwakened ? true : false, zero,
				player, player.getNextActorNetId(), rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded || character.canAirJump() && character.flag == null) &&
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
		if (!character.grounded || character.vel.y < 0) {
			sprite = "projswing_air";
			defaultSprite = sprite;
			character.changeSpriteFromName(sprite, true);
		}
	}
}

public class AwakenedZeroHadangekiWall : ZeroState {
	bool fired;
	public int wallDir;
	public Collider wallCollider;

	public AwakenedZeroHadangekiWall(int wallDir, Collider wallCollider) : base("wall_slide_attack") {
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		superArmor = true;
		useGravity = false;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 4 && !fired) {
			character.playSound("zerosaberx3", sendRpc: true);
			fired = true;
			new ZSaberProj(
				character.pos.addxy(30 * -wallDir, -20), -wallDir,
				isAZ: zero.isAwakened ? true : false, zero,
				player, player.getNextActorNetId(), rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeState(new WallSlide(wallDir, wallCollider) { enterSound = "" });
			character.sprite.frameIndex = character.sprite.totalFrameNum - 1;
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		useGravity = true;
	}
}

public class GenmureiState : ZeroState {
	bool fired;
	public GenmureiState() : base("genmu") { }

	public override void update() {
		base.update();

		if (character.frameIndex >= 8 && !fired) {
			fired = true;
			character.playSound("genmureix5", sendRpc: true);
			new GenmuProj(
				character.pos.addxy(30 * character.xDir, -25), character.xDir, 0, 
				isAZ: zero.isAwakened ? true : false,
				zero, player, player.getNextActorNetId(), rpc: true
			);
			new GenmuProj(
				character.pos.addxy(30 * character.xDir, -25), character.xDir, 1, 
				isAZ: zero.isAwakened ? true : false,
				zero, player, player.getNextActorNetId(), rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
