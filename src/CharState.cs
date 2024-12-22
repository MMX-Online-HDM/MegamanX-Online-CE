using System;
using System.Collections.Generic;

namespace MMXOnline;

public class CharState {
	public string sprite;
	public string defaultSprite;
	public string attackSprite;
	public string shootSprite;
	public string transitionSprite;
	public string landSprite = "";
	public string airSprite = "";
	public bool wasGrounded = true;
	public Point busterOffset;
	public Character character = null!;
	public Collider? lastLeftWallCollider;
	public Collider? lastRightWallCollider;
	public Wall? lastLeftWall;
	public Wall? lastRightWall;
	public Collider? wallKickLeftWall;
	public Collider? wallKickRightWall;
	public float stateTime;
	public float stateFrames;
	public string enterSound = "";
	public float framesJumpNotHeld = 0;
	public bool once;
	public bool useGravity = true;

	public bool invincible;
	public bool stunResistant;
	public bool superArmor;
	public bool immuneToWind;
	public int accuracy;
	public bool isGrabbedState;

	public bool wasVileHovering;

	// For grab states (I am grabber)
	public bool isGrabbing;

	// For grabbed states (I am the grabbed)
	public float grabTime = 4;

	// Gacel notes.
	// This should be inside the character object to sync while online.
	public virtual void releaseGrab() {
		grabTime = 0;
	}

	// Control system.
	// This dictates if it can attack or land.
	public bool attackCtrl;
	public bool[] altCtrls = new bool[1];
	public bool normalCtrl;
	public bool airMove;
	public bool canJump;
	public bool canStopJump;
	public bool exitOnLanding;
	public bool exitOnAirborne;
	public bool useDashJumpSpeed;
	public SpecialStateIds specialId;

	public CharState(string sprite, string shootSprite = "", string attackSprite = "", string transitionSprite = "") {
		this.sprite = string.IsNullOrEmpty(transitionSprite) ? sprite : transitionSprite;
		this.transitionSprite = transitionSprite;
		defaultSprite = sprite;
		this.shootSprite = shootSprite;
		this.attackSprite = attackSprite;
		stateTime = 0;
	}

	public bool canUseShootAnim() {
		return !string.IsNullOrEmpty(shootSprite);
	}

	public Player player {
		get {
			return character.player;
		}
	}

	public virtual void onExit(CharState newState) {
		if (!useGravity) {
			character.useGravity = true;
		}
		// Stop the dash speed on transition to any frame except jump/fall (dash lingers in air) or dash itself
		// TODO: Add a bool here to charstate.
		if (newState == null) {
			character.rideArmorPlatform = null;
			return;
		}
		if (newState is not Dash &&
			newState is not Jump &&
			newState is not Fall &&
			!(newState.useDashJumpSpeed && (!character.grounded || character.vel.y < 0))
		) {
			character.isDashing = false;
		}
		if (newState is Hurt || newState is Die ||
			newState is GenericStun
		) {
			character.onFlinchOrStun(newState);
		}
		if (character.rideArmorPlatform != null && (
			newState is Hurt || newState is Die ||
			newState is CallDownMech || newState.isGrabbedState == true
		)) {
			character.rideArmorPlatform = null;
		}
		if (invincible) {
			player.delaySubtank();
		}
		character.onExitState(this, newState);
	}

	public virtual void onEnter(CharState oldState) {
		if (!string.IsNullOrEmpty(enterSound)) {
			character.playSound(enterSound, sendRpc: true);
		}
		if (oldState is VileHover) {
			wasVileHovering = true;
		}
		if (!useGravity) {
			character.useGravity = false;
			character.stopMoving();
		}
		wasGrounded = character.grounded;
		if (this is not Jump and not WallKick && oldState.canStopJump == false) {
			canStopJump = false;
		}
	}

	public virtual bool canEnter(Character character) {
		if (character.charState is InRideArmor &&
			!(this is Die || this is Idle || this is Jump || this is Fall || this is StrikeChainHooked || this is ParasiteCarry || this is VileMK2Grabbed || this is DarkHoldState ||
			  this is NecroBurstAttack || this is UPGrabbed || this is WhirlpoolGrabbed || this is DeadLiftGrabbed || Helpers.isOfClass(this, typeof(GenericGrabbedState)))) {
			return false;
		}
		if (character.charState is DarkHoldState dhs && dhs.stunTime > 0) {
			if (this is not Die && this is not Hurt) {
				return false;
			}
		}
		if (character.charState is WarpOut && this is not WarpIn) {
			return false;
		}
		return true;
	}

	public virtual bool canExit(Character character, CharState newState) {
		return true;
	}

	public bool inTransition() {
		return (!string.IsNullOrEmpty(transitionSprite) &&
			sprite == transitionSprite &&
			character.sprite.name != null &&
			character.sprite.name.Contains(transitionSprite)
		);
	}

	public virtual void render(float x, float y) {
	}

	public virtual void update() {
		stateTime += Global.spf;
		if (!character.ownedByLocalPlayer) {
			return;
		}
		if (inTransition()) {
			character.frameSpeed = 1;
			if (character.isAnimOver() && !Global.level.gameMode.isOver) {
				sprite = defaultSprite;
				if (character.shootAnimTime > 0 && shootSprite != "") {
					character.changeSpriteFromName(shootSprite, true);
				} else {
					character.changeSpriteFromName(sprite, true);
				}
			}
		}
		var lastLeftWallData = character.getHitWall(-1, 0);
		lastLeftWallCollider = lastLeftWallData != null ? lastLeftWallData.otherCollider : null;
		if (lastLeftWallCollider != null && !lastLeftWallCollider.isClimbable) {
			lastLeftWallCollider = null;
		}
		lastLeftWall = lastLeftWallData?.gameObject as Wall;

		var lastRightWallData = character.getHitWall(1, 0);
		lastRightWallCollider = lastRightWallData != null ? lastRightWallData.otherCollider : null;
		if (lastRightWallCollider != null && !lastRightWallCollider.isClimbable) {
			lastRightWallCollider = null;
		}
		lastRightWall = lastRightWallData?.gameObject as Wall;

		var wallKickLeftData = character.getHitWall(-8, 0);
		if (wallKickLeftData?.otherCollider?.isClimbable == true && wallKickLeftData?.gameObject is Wall) {
			wallKickLeftWall = wallKickLeftData.otherCollider;
		} else {
			wallKickLeftWall = null;
		}
		var wallKickRightData = character.getHitWall(8, 0);
		if (wallKickRightData?.otherCollider?.isClimbable == true && wallKickRightData?.gameObject is Wall) {
			wallKickRightWall = wallKickRightData.otherCollider;
		} else {
			wallKickRightData = null;
		}


		// Moving platforms detection
		CollideData? leftWallPlat = character.getHitWall(-Global.spf * 300, 0);
		if (leftWallPlat?.gameObject is Wall leftWall && leftWall.isMoving) {
			character.move(leftWall.deltaMove, useDeltaTime: true);
			lastLeftWallCollider = leftWall.collider;
		} else if (leftWallPlat?.gameObject is Actor leftActor && leftActor.isPlatform && leftActor.pos.x < character.pos.x) {
			lastLeftWallCollider = leftActor.collider;
		}

		CollideData? rightWallPlat = character.getHitWall(Global.spf * 300, 0);
		if (rightWallPlat?.gameObject is Wall rightWall && rightWall.isMoving) {
			character.move(rightWall.deltaMove, useDeltaTime: true);
			lastRightWallCollider = rightWall.collider;
		} else if (rightWallPlat?.gameObject is Actor rightActor && rightActor.isPlatform && rightActor.pos.x > character.pos.x) {
			lastRightWallCollider = rightActor.collider;
		}

		airTrasition();
		wasGrounded = character.grounded;
	}

	public virtual void airTrasition() {
		if (airSprite != "" && !character.grounded && wasGrounded && sprite != airSprite) {
			sprite = airSprite;
			int oldFrameIndex = character.sprite.frameIndex;
			float oldFrameTime = character.sprite.frameTime;
			character.changeSprite(sprite, false);
			character.sprite.frameIndex = oldFrameIndex;
			character.sprite.frameTime = oldFrameTime;
		} else if (landSprite != "" && character.grounded && !wasGrounded && sprite != landSprite) {
			sprite = landSprite;
			int oldFrameIndex = character.sprite.frameIndex;
			float oldFrameTime = character.sprite.frameTime;
			character.changeSpriteFromName(sprite, false);
			character.sprite.frameIndex = oldFrameIndex;
			character.sprite.frameTime = oldFrameTime;
		}
	}

	public void landingCode() {
		character.playSound("land", sendRpc: true);
		character.dashedInAir = 0;
		changeToIdle();
		if (character.ai != null) {
			character.ai.jumpTime = 0;
		}
	}

	public void groundCodeWithMove() {
		if (player.character.canTurn()) {
			if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
				if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
				if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
				if (player.character.canMove()) character.changeState(new Run());
			}
		}
	}

	public void changeToIdle(string ts = "") {
		if (character.grounded &&
			string.IsNullOrEmpty(ts) && (
			player.input.isHeld(Control.Left, player) ||
			player.input.isHeld(Control.Right, player))
		) {
			character.changeState(new Run());
		} else {
			character.changeToIdleOrFall(ts);
		}
	}

	public void checkLadder(bool isGround) {
		if (character.charState is LadderClimb) {
			return;
		}
		if (player.input.isHeld(Control.Up, player)) {
			List<CollideData> ladders = Global.level.getTerrainTriggerList(character, new Point(0, 0), typeof(Ladder));
			if (ladders != null && ladders.Count > 0 && ladders[0].gameObject is Ladder ladder) {
				var midX = ladders[0].otherCollider.shape.getRect().center().x;
				if (Math.Abs(character.pos.x - midX) < 12) {
					var rect = ladders[0].otherCollider.shape.getRect();
					var snapX = (rect.x1 + rect.x2) / 2;
					if (Global.level.checkTerrainCollisionOnce(character, snapX - character.pos.x, 0) == null) {
						float? incY = null;
						if (isGround) incY = -10;
						character.changeState(new LadderClimb(ladder, midX, incY));
					}
				}
			}
		}
		if (isGround && player.input.isPressed(Control.Down, player)) {
			character.checkLadderDown = true;
			var ladders = Global.level.getTerrainTriggerList(character, new Point(0, 1), typeof(Ladder));
			if (ladders.Count > 0 && ladders[0].gameObject is Ladder ladder) {
				var rect = ladders[0].otherCollider.shape.getRect();
				var snapX = (rect.x1 + rect.x2) / 2;
				float xDist = snapX - character.pos.x;
				if (MathF.Abs(xDist) < 10 && Global.level.checkTerrainCollisionOnce(character, xDist, 30) == null) {
					var midX = ladders[0].otherCollider.shape.getRect().center().x;
					character.changeState(new LadderClimb(ladder, midX, 30));
					character.stopCamUpdate = true;
				}
			}
			character.checkLadderDown = false;
		}
	}

	public void clampViralSigmaPos() {
		float w = 25;
		float h = 35;
		if (character.pos.y < h) {
			Point destPos = new Point(character.pos.x, h);
			Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
			character.changePos(lerpPos);
		}
		if (character.pos.x < w) {
			Point destPos = new Point(w, character.pos.y);
			Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
			character.changePos(lerpPos);
		}

		float rightBounds = Global.level.width - w;
		if (character.pos.x > rightBounds) {
			Point destPos = new Point(rightBounds, character.pos.y);
			Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
			character.changePos(lerpPos);
		}
	}
}

public class WarpIn : CharState {
	public bool warpSoundPlayed;
	public float destY;
	public float destX;
	public float startY;
	public Anim warpAnim;
	bool warpAnimOnce;

	// Sigma-specific
	public bool isSigma { get { return player.isSigma; } }
	public int sigmaRounds;
	public const float yOffset = 200;
	public bool landOnce;
	public bool decloaked;
	public bool addInvulnFrames;
	public bool sigma2Once;

	public WarpIn(bool addInvulnFrames = true) : base("warp_in") {
		this.addInvulnFrames = addInvulnFrames;
	}

	public override void update() {
		if (!character.ownedByLocalPlayer) return;
		if (!Global.level.mainPlayer.readyTextOver) return;

		if (warpAnim == null && !warpAnimOnce) {
			warpAnimOnce = true;
			warpAnim = new Anim(character.pos.addxy(0, -yOffset), character.getSprite("warp_beam"), character.xDir, player.getNextActorNetId(), false, sendRpc: true);
			warpAnim.splashable = false;
		}

		if (warpAnim == null) {
			character.visible = true;
			character.frameSpeed = 1;
			if (character is CmdSigma && character.sprite.frameIndex >= 2 && !decloaked) {
				decloaked = true;
				var cloakAnim = new Anim(character.getFirstPOI() ?? character.getCenterPos(), "sigma_cloak", character.xDir, player.getNextActorNetId(), true);
				cloakAnim.vel = new Point(-25 * character.xDir, -10);
				cloakAnim.fadeOut = true;
				cloakAnim.setzIndex(character.zIndex - 1);
			}

			if (isSigma && player.isSigma2() && character.sprite.frameIndex >= 4 && !sigma2Once) {
				sigma2Once = true;
				character.playSound("sigma2start", sendRpc: true);
			}

			if (character.isAnimOver()) {
				character.grounded = true;
				character.pos.y = destY;
				character.pos.x = destX;
				character.changeToIdleOrFall();
			}
			return;
		}

		if (character.player == Global.level.mainPlayer && !warpSoundPlayed) {
			warpSoundPlayed = true;
			character.playSound("warpIn", sendRpc: true);
		}

		float yInc = Global.spf * 450 * getSigmaRoundsMod(sigmaRounds);
		warpAnim.incPos(new Point(0, yInc));

		if (isSigma && !landOnce && warpAnim.pos.y >= destY - 1) {
			landOnce = true;
			warpAnim.changePos(new Point(warpAnim.pos.x, destY - 1));
		}

		if (warpAnim.pos.y >= destY) {
			if (!isSigma || sigmaRounds > 6) {
				warpAnim.destroySelf();
				warpAnim = null;
			} else {
				sigmaRounds++;
				landOnce = false;
				warpAnim.changePos(new Point(warpAnim.pos.x, destY - getSigmaYOffset(sigmaRounds)));
			}
		}
	}

	float getSigmaRoundsMod(int aSigmaRounds) {
		if (!isSigma) return 1;
		return 2;
	}

	float getSigmaYOffset(int aSigmaRounds) {
		if (aSigmaRounds == 0) return yOffset;
		if (aSigmaRounds == 1) return yOffset;
		if (aSigmaRounds == 2) return yOffset;
		if (aSigmaRounds == 3) return yOffset * 0.75f;
		if (aSigmaRounds == 4) return yOffset * 0.5f;
		return yOffset * 0.25f;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
		character.visible = false;
		character.frameSpeed = 0;
		destY = character.pos.y;
		destX = character.pos.x;
		startY = character.pos.y;

		if (player.warpedInOnce || Global.debug) {
			sigmaRounds = 10;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.visible = true;
		character.useGravity = true;
		character.splashable = true;
		if (warpAnim != null) {
			warpAnim.destroySelf();
		}
		if (addInvulnFrames && character.ownedByLocalPlayer) {
			character.invulnTime = (player.warpedInOnce || Global.level.joinedLate) ? 2 : 0;
		}
		player.warpedInOnce = true;
	}
}

public class WarpOut : CharState {
	public bool warpSoundPlayed;
	public float destY;
	public float startY;
	public Anim warpAnim;
	public const float yOffset = 200;
	public bool isSigma { get { return player.isSigma; } }
	public bool is1v1MaverickStart;

	public WarpOut(bool is1v1MaverickStart = false) : base("warp_beam") {
		this.is1v1MaverickStart = is1v1MaverickStart;
	}

	public override void update() {
		if (warpAnim == null) {
			return;
		}
		if (is1v1MaverickStart) {
			return;
		}

		if (character.player == Global.level.mainPlayer && !warpSoundPlayed) {
			warpSoundPlayed = true;
			character.playSound("warpOut", forcePlay: true, sendRpc: true);
		}

		warpAnim.pos.y -= Global.spf * 1000;

		if (character.pos.y <= destY) {
			warpAnim.destroySelf();
			warpAnim = null;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
		character.visible = false;
		destY = character.pos.y - yOffset;
		startY = character.pos.y;
		if (!is1v1MaverickStart) {
			warpAnim = new Anim(character.pos, character.getSprite("warp_beam"), character.xDir, player.getNextActorNetId(), false, sendRpc: true);
			warpAnim.splashable = false;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (warpAnim != null) {
			warpAnim.destroySelf();
		}
	}
}

public class Idle : CharState {
	public Idle(string transitionSprite = "") : base("idle", "shoot", "attack", transitionSprite) {
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if ((character is RagingChargeX || player.health < 4)) {
			if (Global.sprites.ContainsKey(character.getSprite("weak"))) {
				defaultSprite = "weak";
				if (!inTransition()) {
					sprite = defaultSprite;
					character.changeSpriteFromName("weak", true);
				}
			}
		}
		if ((character is PunchyZero)) {
			if (player.health < 4) {
				defaultSprite = "pweak";
			} else {
				defaultSprite = "pidle";
			}
			if (!inTransition()) {
				sprite = defaultSprite;
				character.changeSpriteFromName(sprite, true);
			}
		} 
		if (character is Zero) {
			if (player.health <= 4) {
				sprite = "weak";
			} else {
				sprite = "idle";
			}
			character.changeSpriteFromName(sprite, true);				
		}
		character.dashedInAir = 0;
	}

	public override void update() {
		base.update();

		if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
			if (!character.isSoftLocked() && character.canTurn()) {
				if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
				if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
				if (player.character.canMove()) character.changeState(new Run());
			}
		}

		if (Global.level.gameMode.isOver) {
			if (Global.level.gameMode.playerWon(player)) {
				if (!character.sprite.name.Contains("_win")) {
					character.changeSpriteFromName("win", true);
				}
			} else {
				if (!character.sprite.name.Contains("lose")) {
					string loseSprite = "lose";
					character.changeSpriteFromName(loseSprite, true);
				}
			}
		}
	}
}

public class Run : CharState {
	public Run() : base("run", "run_shoot", "attack") {
		accuracy = 5;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		var move = new Point(0, 0);
		float runSpeed = character.getRunSpeed();
		if (stateFrames <= 4) {
			runSpeed = 60 * character.getRunDebuffs();
		}
		if (player.input.isHeld(Control.Left, player)) {
			character.xDir = -1;
			if (player.character.canMove()) move.x = -runSpeed;
		} else if (player.input.isHeld(Control.Right, player)) {
			character.xDir = 1;
			if (player.character.canMove()) move.x = runSpeed;
		}
		if (move.magnitude > 0) {
			character.move(move);
		} else {
			character.changeToIdleOrFall();
		}
	}
}

public class Crouch : CharState {
	public Crouch(string transitionSprite = "crouch_start"
	) : base(
		"crouch", "crouch_shoot", "attack_crouch", transitionSprite
	) {
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.globalCollider = character.getCrouchingCollider();
	}

	public override void update() {
		base.update();

		var dpadXDir = player.input.getXDir(player);
		if (dpadXDir != 0) {
			character.xDir = dpadXDir;
		}

		if (!character.grounded || !player.isCrouchHeld()) {
			character.changeToIdleOrFall("crouch_start");
			return;
		}
		if (Global.level.gameMode.isOver) {
			if (Global.level.gameMode.playerWon(player)) {
				if (!character.sprite.name.Contains("_win")) {
					character.changeSpriteFromName("win", true);
				}
			} else {
				if (!character.sprite.name.Contains("lose")) {
					character.changeSpriteFromName("lose", true);
				}
			}
		}
	}
}

public class SwordBlock : CharState {
	public SwordBlock() : base("block") {
		immuneToWind = true;
		superArmor = true;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
		stunResistant = true;
	}

	public override void update() {
		base.update();

		bool isHoldingGuard = (
			player.input.isHeld(Control.WeaponLeft, player) ||
			player.input.isHeld(Control.WeaponRight, player)
		);
		if (!isHoldingGuard) {
			character.changeToIdleOrFall();
			return;
		}
		if (Global.level.gameMode.isOver) {
			if (Global.level.gameMode.playerWon(player)) {
				if (!character.sprite.name.Contains("_win")) {
					character.changeSpriteFromName("win", true);
				}
			} else {
				if (!character.sprite.name.Contains("lose")) {
					character.changeSpriteFromName("lose", true);
				}
			}
		}
	}
}

public class ZeroClang : CharState {
	public int hurtDir;
	public float hurtSpeed;

	public ZeroClang(int dir) : base("clang") {
		hurtDir = dir;
		hurtSpeed = dir * 100;
	}

	public override void update() {
		base.update();
		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 400 * Global.spf, hurtDir);
			character.move(new Point(hurtSpeed, 0));
		}
		/*
		if (this.character.isAnimOver()) {
			this.character.changeToIdleOrFall();
		}
		*/
		if (hurtSpeed == 0) {
			character.changeToIdleOrFall();
		}
	}
}

public class Jump : CharState {
	public Jump() : base("jump", "jump_shoot", Options.main.getAirAttack()) {
		accuracy = 5;
		enterSound = "jump";
		exitOnLanding = true;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		if (character.vel.y > 0) {
			if (character.sprite.name.EndsWith("cannon_air") == false) {
				character.changeState(new Fall());
			}
			return;
		}
	}
}

public class Fall : CharState {
	public float limboVehicleCheckTime;
	public Actor limboVehicle;

	public Fall() : base("fall", "fall_shoot", Options.main.getAirAttack(), "fall_start") {
		accuracy = 5;
		exitOnLanding = true;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = false;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		if (limboVehicleCheckTime > 0) {
			limboVehicleCheckTime -= Global.spf;
			if (limboVehicle.destroyed || limboVehicleCheckTime <= 0) {
				limboVehicleCheckTime = 0;
				character.useGravity = true;
				character.limboRACheckCooldown = 1;
				limboVehicleCheckTime = 0;
			}
		}
	}

	public void setLimboVehicleCheck(Actor limboVehicle) {
		if (limboVehicleCheckTime == 0 && character.limboRACheckCooldown == 0) {
			this.limboVehicle = limboVehicle;
			limboVehicleCheckTime = 1;
			character.stopMoving();
			character.useGravity = false;
			if (limboVehicle is RideArmor ra) {
				RPC.checkRAEnter.sendRpc(player.id, ra.netId, ra.neutralId, ra.raNum);
			} else if (limboVehicle is RideChaser rc) {
				RPC.checkRCEnter.sendRpc(player.id, rc.netId, rc.neutralId);
			}
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}

public class Dash : CharState {
	public float dashTime = 0;
	public string initialDashButton;
	public int initialDashDir;
	public bool stop;
	public Anim dashSpark;

	public Dash(string initialDashButton) : base("dash", "dash_shoot", "attack_dash") {
		enterSound = "dash";
		this.initialDashButton = initialDashButton;
		accuracy = 10;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);

		initialDashDir = character.xDir;
		if (character is Axl && (character.currentWeapon as AxlWeapon)?.isTwoHanded(false) == true) {
			if (player.input.isHeld(Control.Left, player)) initialDashDir = -1;
			else if (player.input.isHeld(Control.Right, player)) initialDashDir = 1;
		}

		character.isDashing = true;
		character.globalCollider = character.getDashingCollider();
		dashSpark = new Anim(
			character.getDashSparkEffectPos(initialDashDir),
			"dash_sparks", initialDashDir, player.getNextActorNetId(),
			true, sendRpc: true
		);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (!dashSpark.destroyed) {
			dashSpark.destroySelf();
		}
	}

	public static void dashBackwardsCode(Character character, int initialDashDir) {
		if (character is Axl) {
			if (character.xDir != initialDashDir) {
				if (!character.sprite.name.EndsWith("backwards")) {
					character.changeSpriteFromName("dash_backwards", false);
				}
			} else {
				if (character.sprite.name.EndsWith("backwards")) {
					character.changeSpriteFromName("dash", false);
				}
			}
		}
	}

	public override void update() {
		dashBackwardsCode(character, initialDashDir);
		base.update();

		if (!player.isAI && !player.input.isHeld(initialDashButton, player) && !stop) {
			dashTime = 50;
		}
		float speedModifier = 1;
		float distanceModifier = 1;
		float inputXDir = player.input.getInputDir(player).x;
		if (dashTime > Global.spf * 32 * distanceModifier || stop) {
			if (!stop) {
				dashTime = 0;
				character.frameIndex = 0;
				character.sprite.frameTime = 0;
				character.sprite.animTime = 0;
				character.sprite.frameSpeed = 0.1f;
				stop = true;
			} else {
				if (inputXDir != 0 && character.grounded) {
					character.changeState(new Run(), true);
				} else {
					character.changeToIdleOrFall();
				}
				return;
			}
		}
		if (dashTime > Global.spf * 3 || stop) {
			var move = new Point(0, 0);
			move.x = character.getDashSpeed() * initialDashDir * speedModifier;
			character.move(move);
		} else {
			var move = new Point(0, 0);
			move.x = Physics.DashStartSpeed * character.getRunDebuffs() * initialDashDir * speedModifier; ;
			character.move(move);
		}
		if (dashTime <= Global.spf * 3 || stop) {
			if (inputXDir != 0 && inputXDir != initialDashDir) {
				character.xDir = (int)inputXDir;
				initialDashDir = (int)inputXDir;
			}
		}
		dashTime += Global.spf;
		if (stateTime > 0.1 && !character.isUnderwater()) {
			stateTime = 0;
			new Anim(
				character.getDashDustEffectPos(initialDashDir),
				"dust", initialDashDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
		}
		if (!character.grounded) {
			character.dashedInAir++;
			character.changeState(new Fall());
		}
	}
}

public class AirDash : CharState {
	public float dashTime = 0;
	public string initialDashButton;
	public int initialDashDir;
	public bool stop;

	public AirDash(string initialDashButton) : base("dash", "dash_shoot") {
		enterSound = "dashX2";
		this.initialDashButton = initialDashButton;
		accuracy = 10;
		attackCtrl = true;
	}

	public override void update() {
		Dash.dashBackwardsCode(character, initialDashDir);

		base.update();

		if (!player.input.isHeld(initialDashButton, player) && !stop) {
			dashTime = 50;
		}
		float speedModifier = 1;
		float distanceModifier = 1;
		if (character.sprite.name.EndsWith("unpo_grab_dash")) {
			speedModifier = 1.25f;
			distanceModifier = 1.25f;
		}
		if (dashTime > Global.spf * 28 * distanceModifier || stop) {
			if (!stop) {
				dashTime = 0;
				stop = true;
				character.frameIndex = 0;
				character.sprite.frameTime = 0;
				character.sprite.animTime = 0;
				character.sprite.frameSpeed = 0.1f;
				stop = true;
			} else {
				character.changeState(new Fall());
			}
		}
		if (dashTime > Global.spf * 3 || stop) {
			var move = new Point(0, 0);
			move.x = character.getDashSpeed() * initialDashDir * speedModifier;
			character.move(move);
		} else {
			var move = new Point(0, 0);
			move.x = Physics.DashStartSpeed * character.getRunDebuffs() * initialDashDir * speedModifier;
			character.move(move);
		}
		dashTime += Global.spf;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);

		initialDashDir = character.xDir;

		if (character is Axl && (character.currentWeapon as AxlWeapon)?.isTwoHanded(false) == true) {
			if (player.input.isHeld(Control.Left, player)) initialDashDir = -1;
			else if (player.input.isHeld(Control.Right, player)) initialDashDir = 1;
		}

		character.isDashing = true;
		character.useGravity = false;
		character.vel = new Point(0, 0);
		character.dashedInAir++;
		character.globalCollider = character.getDashingCollider();
		new Anim(character.getDashSparkEffectPos(initialDashDir), "dash_sparks", initialDashDir, null, true);
	}

	public override void onExit(CharState newState) {
		character.useGravity = true;
		base.onExit(newState);
	}
}

public class WallSlide : CharState {
	public int wallDir;
	public float dustTime;
	public Collider wallCollider;
	MegamanX? mmx;

	public WallSlide(
		int wallDir, Collider wallCollider
	) : base(
		"wall_slide", "wall_slide_shoot", "wall_slide_attack"
	) {
		enterSound = "land";
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		accuracy = 2;
		attackCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX;
		character.dashedInAir = 0;
		if (player.isAI) {
			character.ai.jumpTime = 0;
		}
	}

	public override void update() {
		base.update();
		if (character.grounded) {
			character.changeToIdleOrFall();
			return;
		}
		/*
		if (player.input.isPressed(Control.Jump, player)) {
			if (player.input.isHeld(Control.Dash, player)) {
				character.isDashing = true;
			}
			character.vel.y = -character.getJumpPower();
			character.changeState(new WallKick(wallDir * -1));
			return;
		}
		*/
		if (character is CmdSigma && player.input.isPressed(Control.Special1, player) && character.flag == null) {
			int yDir = player.input.isHeld(Control.Down, player) ? 1 : -1;
			character.changeState(new SigmaWallDashState(yDir, false), true);
			return;
		}

		character.useGravity = false;
		character.vel.y = 0;

		/*
		if (wallDir == -1 && wallCollider?.actor?.isPlatform == true)
		{
			float charWidth = character.collider?.shape.getRect().w() ?? 0;
			character.changePos(new Point(wallCollider.shape.getRect().x2 + 1 + charWidth / 2, character.pos.y));
		}

		if (wallDir == 1 && wallCollider?.actor?.isPlatform == true)
		{
			float charWidth = character.collider?.shape.getRect().w() ?? 0;
			character.changePos(new Point(wallCollider.shape.getRect().x1 - 1 - charWidth / 2, character.pos.y));
		}
		*/

		if (stateFrames >= 9) {
			if (mmx == null || mmx.strikeChainProj == null || mmx.strikeChainChargedProj == null) {
				var hit = character.getHitWall(wallDir, 0);
				var hitWall = hit?.gameObject as Wall;

				if (wallDir != player.input.getXDir(player)) {
					player.character.changeState(new Fall());
				} else if (hitWall == null || !hitWall.collider.isClimbable) {
					var hitActor = hit?.gameObject as Actor;
					if (hitActor == null || !hitActor.isPlatform) {
						player.character.changeState(new Fall());
					}
				}
			}
			character.move(new Point(0, 100));
		}

		dustTime += Global.speedMul;
		if (stateFrames > 12 && dustTime > 6) {
			dustTime = 0;
			generateDust(character);
		}
	}

	public override void onExit(CharState newState) {
		character.useGravity = true;
		base.onExit(newState);
	}

	public static void generateDust(Character character) {
		Point animPoint = character.pos.addxy(12 * character.xDir, 0);
		Rect rect = new Rect(animPoint.addxy(-3, -3), animPoint.addxy(3, 3));
		if (Global.level.checkCollisionShape(rect.getShape(), null) != null) {
			new Anim(animPoint, "dust", character.xDir, character.player.getNextActorNetId(), true, sendRpc: true);
		}
	}
}

public class WallSlideAttack : CharState {
	public int wallDir;
	public float dustTime;
	public Collider wallCollider;
	public bool exitOnAnimEnd;
	public bool canCancel;

	public WallSlideAttack(string anim, int wallDir, Collider wallCollider) : base(anim) {
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		useGravity = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.dashedInAir = 0;
	}

	public override void update() {
		base.update();
		if (canCancel && (character.grounded || player.input.getXDir(player) != wallDir)) {
			character.changeToIdleOrFall();
			return;
		}
		if (!character.grounded) {
			character.move(new Point(0, 100));
			dustTime += Global.speedMul;
		}
		if (stateFrames > 12 && dustTime > 6) {
			dustTime = 0;
			WallSlide.generateDust(character);
		}
		if (exitOnAnimEnd && character.isAnimOver()) {
			WallSlide wallSlideState = new WallSlide(wallDir, wallCollider) { enterSound = "", stateFrames = 14 };
			character.changeState(wallSlideState);
			character.sprite.frameIndex = character.sprite.totalFrameNum - 1;
			return;
		}
	}
}

public class WallKick : CharState {
	public WallKick() : base("wall_kick", "wall_kick_shoot") {
		enterSound = "jump";
		accuracy = 5;
		exitOnLanding = true;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		if (character.vel.y > 0) {
			character.changeState(new Fall());
		}
	}
}

public class LadderClimb : CharState {
	public Ladder ladder;
	public float snapX;
	public float? incY;
	public LadderClimb(Ladder ladder, float snapX, float? incY = null) : base("ladder_climb", "ladder_shoot", "ladder_attack", "ladder_start") {
		this.ladder = ladder;
		this.snapX = MathF.Round(snapX);
		this.incY = incY;
		attackCtrl = true;
		immuneToWind = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.changePos(new Point(snapX, character.pos.y));

		if (incY != null) {
			character.incPos(new Point(0, (float)incY));
		}

		if (character.player == Global.level.mainPlayer) {
			Global.level.lerpCamTime = 0.25f;
		}
		character.stopMoving();
		character.useGravity = false;
		character.dashedInAir = 0;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.frameSpeed = 1;
		character.useGravity = true;
	}

	public override void update() {
		base.update();
		character.changePos(new Point(snapX, character.pos.y));
		character.xPushVel = 0;
		character.vel.x = 0;
		character.vel.y = 0;
		if (inTransition()) {
			return;
		}
		bool isAttacking = (
			character.sprite.name != character.getSprite("ladder_climb")
		);

		if (isAttacking) {
			character.frameSpeed = 1;
		} else {
			character.frameSpeed = 0;
		}
		if (!isAttacking && character.canClimbLadder()) {
			if (player.input.isHeld(Control.Up, player)) {
				character.move(new Point(0, -75));
				character.frameSpeed = 1;
			} else if (player.input.isHeld(Control.Down, player)) {
				character.move(new Point(0, 75));
				character.frameSpeed = 1;
			}
		}

		var ladderTop = ladder.collider.shape.getRect().y1;
		var yDist = character.physicsCollider.shape.getRect().y2 - ladderTop;
		if (!ladder.collider.isCollidingWith(character.physicsCollider) || MathF.Abs(yDist) < 12) {
			if (player.input.isHeld(Control.Up, player)) {
				var targetY = ladderTop - 1;
				if (Global.level.checkTerrainCollisionOnce(character, 0, targetY - character.pos.y) == null && MathF.Abs(targetY - character.pos.y) < 20) {
					character.changeState(new LadderEnd(targetY));
				}
			} else {
				character.changeState(new Fall());
			}
		} else if (!player.isAI && player.input.isPressed(Control.Jump, player)) {
			if (!isAttacking) {
				dropFromLadder();
			}
		}

		if (character.grounded) {
			character.changeToIdleOrFall();
		}
	}

	// AI should call this manually when they want to drop from a ladder
	public void dropFromLadder() {
		character.changeState(new Fall());
	}
}

public class LadderEnd : CharState {
	public float targetY;
	public LadderEnd(float targetY) : base("ladder_end") {
		this.targetY = targetY;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.stopMoving();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}

	public override void update() {
		base.update();
		character.xPushVel = 0;
		character.vel.x = 0;
		character.vel.y = 0;
		if (character.isAnimOver()) {
			if (character.player == Global.level.mainPlayer) {
				Global.level.lerpCamTime = 0.25f;
			}
			//this.character.pos.y = this.targetY;
			character.incPos(new Point(0, targetY - character.pos.y));
			character.stopCamUpdate = true;
			character.grounded = true;
			character.changeToIdleOrFall();
		}
	}
}

public class Taunt : CharState {
	float tauntTime = 1;
	Anim? zeroching;
	public Taunt() : base("win") {
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.charNum == 0) tauntTime = 0.75f;
		if (player.charNum == 1) tauntTime = 0.7f;
		if (player.charNum == 3) tauntTime = 0.75f;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		zeroching?.destroySelf();
	}

	public override void update() {
		base.update();

		if (player.charNum == 2) {
			if (character.isAnimOver()) {
				character.changeToIdleOrFall();
			}
		} else if (stateTime >= tauntTime) {
			character.changeToIdleOrFall();
		}

		if (player.charNum == (int)CharIds.Zero && player.input.isHeld(Control.Up, player)) {
			character.changeSprite("zero_win2", true);
			if (character.isAnimOver()) {
				character.changeToIdleOrFall();
			}
		}
		if (character.sprite.name == "zero_win2" && character.frameIndex == 1 && !once) {
			once = true;
			character.playSound("ching", sendRpc: true);
			zeroching = new Anim(
				character.pos.addxy(character.xDir, -25f),
				"zero_ching", -character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: true, sendRpc: true
			);
		}
	}
}

public class Die : CharState {
	bool sigmaHasMavericks;

	public Die() : base("die") {
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.stopMoving();
		character.stopCharge();
		new Anim(character.pos.addxy(0, -12), "die_sparks", 1, null, true);
		player.lastDeathWasXHyper = character is RagingChargeX;
		
		if (character.ownedByLocalPlayer && character.player.isDisguisedAxl) {
			character.player.revertToAxlDeath();
			character.changeSpriteFromName("die", true);
		}
		if (character is Vile vile) {
			player.lastDeathWasVileMK2 = vile.isVileMK2 == true;
			player.lastDeathWasVileMK5 = vile.isVileMK5 == true;
		} else {
			player.lastDeathWasVileMK2 = false;
			player.lastDeathWasVileMK5 = false;
		}
		player.lastDeathWasSigmaHyper = character is WolfSigma or ViralSigma or KaiserSigma;
		player.lastDeathPos = character.getCenterPos();
		//why is this here
		if (player.isAI) player.selectedRAIndex = Helpers.randomRange(0, 3);
		sigmaHasMavericks = player.isSigma && player.mavericks.Count > 0;

		if (character.ownedByLocalPlayer && character is WolfSigma wolfSigma) {
			player.destroyCharacter();
			Global.serverClient?.rpc(RPC.destroyCharacter, (byte)player.id);
			var anim = new Anim(
				character.pos, "sigma_wolf_head_drop", 1, player.getNextActorNetId(), false, sendRpc: true
			);
			anim.useGravity = true;
			anim.ttl = 3;
			anim.blink = true;
			anim.collider.wallOnly = true;
			var ede = new ExplodeDieEffect(
				player, character.pos, character.pos, "empty", 1, character.zIndex, false, 20, 3, false
			);
			ede.host = anim;
			Global.level.addEffect(ede);
		}
		else if (character.ownedByLocalPlayer && character is ViralSigma viralSigma) {
			player.destroyCharacter();
			Global.serverClient?.rpc(RPC.destroyCharacter, (byte)player.id);
			var anim = new Anim(
				character.pos, viralSigma.lastViralSprite, 1, player.getNextActorNetId(), false, sendRpc: true
			);
			anim.ttl = 3;
			anim.blink = true;
			anim.frameIndex = viralSigma.lastViralFrameIndex;
			anim.frameSpeed = 0;
			anim.angle = viralSigma.lastViralAngle;
			var ede = new ExplodeDieEffect(
				player, character.pos, character.pos, "empty", 1, character.zIndex, false, 20, 3, false
			);
			ede.host = anim;
			Global.level.addEffect(ede);
		} else if (character.ownedByLocalPlayer && character is KaiserSigma kaiserSigma) {
			player.destroyCharacter();
			Global.serverClient?.rpc(RPC.destroyCharacter, (byte)player.id);
			string deathSprite = "";
			if (kaiserSigma.lastHyperSigmaSprite.StartsWith("kaisersigma_virus")) {
				deathSprite = kaiserSigma.lastHyperSigmaSprite;
				Point explodeCenterPos = character.pos.addxy(0, -16);
				var ede = new ExplodeDieEffect(
					player, explodeCenterPos, explodeCenterPos,
					"empty", 1, character.zIndex, false, 16, 3, false
				);
				Global.level.addEffect(ede);
			} else {
				deathSprite = kaiserSigma.lastHyperSigmaSprite + "_body";
				if (!Global.sprites.ContainsKey(deathSprite)) {
					deathSprite = "kaisersigma_idle";
				}
				Point explodeCenterPos = character.pos.addxy(0, -55);
				var ede = new ExplodeDieEffect(
					player, explodeCenterPos, explodeCenterPos, "empty",
					1, character.zIndex, false, 60, 3, false
				);
				Global.level.addEffect(ede);

				var headAnim = new Anim(
					character.pos, kaiserSigma.lastHyperSigmaSprite, 1,
					player.getNextActorNetId(), false, sendRpc: true
				);
				headAnim.ttl = 3;
				headAnim.blink = true;
				headAnim.setFrameIndexSafe(kaiserSigma.lastHyperSigmaFrameIndex);
				headAnim.xDir = kaiserSigma.lastHyperSigmaXDir;
				headAnim.frameSpeed = 0;
			}

			var anim = new Anim(
				character.pos, deathSprite, 1, player.getNextActorNetId(),
				false, sendRpc: true, zIndex: ZIndex.Background + 1000
			);
			anim.ttl = 3;
			anim.blink = true;
			anim.setFrameIndexSafe(kaiserSigma.lastHyperSigmaFrameIndex);
			anim.xDir = kaiserSigma.lastHyperSigmaXDir;
			anim.frameSpeed = 0;
		}
		/*if (character is Zero zero) {
			if (zero.isNightmareZeroBS.getValue()) {
				character.playSound("zndie", sendRpc: true);
			}
		}*/
	}

	public override void onExit(CharState newState) {
	}

	public override void update() {
		character.xPushVel = 0;
		character.vel.x = 0;
		character.vel.y = 0;
		base.update();
		if (!character.ownedByLocalPlayer) {
			return;
		}
		if (sigmaHasMavericks) {
			if (stateTime > 0.75f && !once) {
				once = true;
				player.destroySigmaEffect();
				character.visible = false;
			}

			if (once) {
				/*
				// This code would allow controlling Mavs after death,
				// but would result in camera issues and spectate issues that need to be resolved first.
				if (player.currentMaverick == null)
				{
					if (player.isPuppeteer())
					{
						foreach (var weapon in new List<Weapon>(player.weapons))
						{
							if (weapon is MaverickWeapon mw && mw.maverick != null)
							{
								player.weapons.RemoveAll(w => w is SigmaMenuWeapon);
								character.becomeMaverick(mw.maverick);
								player.weaponSlot = player.weapons.IndexOf(weapon);
								return;
							}
						}
					}
				}
				else
				{
					return;
				}
				*/

				if (!player.isTagTeam()) {
					foreach (var weapon in new List<Weapon>(player.weapons)) {
						if (weapon is MaverickWeapon mw && mw.maverick != null) {
							mw.maverick.changeState(new MExit(mw.maverick.pos, true), true);
						}
					}
				}

				player.destroySigma();
			}
		} else if (player.isVile || player.isSigma) {
			if (stateTime > 0.75f && !once) {
				once = true;
				character.visible = false;
				player.explodeDieStart();
			}

			if (stateTime > 2.25f) {
				destroyRideArmor();
				player.explodeDieEnd();
			}
		} else {
			if (stateTime > 0.75f) {
				destroyRideArmor();
				player.destroyCharacter();
				Global.serverClient?.rpc(RPC.destroyCharacter, (byte)player.id);
			}
		}
	}

	public void destroyRideArmor() {
		if (character.linkedRideArmor != null) {
			character.linkedRideArmor.selfDestructTime = Global.spf;
			RPC.actorToggle.sendRpc(character.linkedRideArmor.netId, RPCActorToggleType.StartMechSelfDestruct);
		}
	}

	public override bool canExit(Character character, CharState newState) {
		if (character.charState is Die &&
			newState is not VileRevive and
			not WolfSigmaRevive and
			not ViralSigmaRevive and
			not KaiserSigmaRevive and
			not XReviveStart and
			not XRevive
		) {
			return false;
		}
		return base.canExit(character, newState);
	}
}

public class GenericGrabbedState : CharState {
	public Actor grabber;
	public long savedZIndex;
	public string grabSpriteSuffix;
	public bool reverseZIndex;
	public bool freeOnHitWall;
	public bool lerp;
	public bool freeOnGrabberLeave;
	public string additionalGrabSprite;
	public float notGrabbedTime;
	public float maxNotGrabbedTime;
	public bool customUpdate;
	public GenericGrabbedState(
		Actor grabber, float maxGrabTime, string grabSpriteSuffix,
		bool reverseZIndex = false, bool freeOnHitWall = true,
		bool lerp = true, string additionalGrabSprite = "", float maxNotGrabbedTime = 0.5f
	) : base(
		"grabbed"
	) {
		this.isGrabbedState = true;
		this.grabber = grabber;
		grabTime = maxGrabTime;
		this.grabSpriteSuffix = grabSpriteSuffix;
		this.reverseZIndex = reverseZIndex;
		//Don't use this unless absolutely needed, it causes issues with octopus grab in FTD
		//this.freeOnHitWall = freeOnHitWall;
		this.lerp = lerp;
		this.additionalGrabSprite = additionalGrabSprite;
		this.maxNotGrabbedTime = maxNotGrabbedTime;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) { return; }
		if (customUpdate) return;

		if (grabber.sprite.name.EndsWith(grabSpriteSuffix) == true || (
				!string.IsNullOrEmpty(additionalGrabSprite) &&
				grabber.sprite.name.EndsWith(additionalGrabSprite) == true
			)
		) {
			bool didNotHitWall = trySnapToGrabPoint(lerp);
			if (!didNotHitWall && freeOnHitWall) {
				character.changeToIdleOrFall();
				return;
			}
		} else {
			notGrabbedTime += Global.spf;
			if (notGrabbedTime > maxNotGrabbedTime) {
				character.changeToIdleOrFall();
				return;
			}
		}

		grabTime -= player.mashValue();
		if (grabTime <= 0) {
			character.changeToIdleOrFall();
		}
	}

	public bool trySnapToGrabPoint(bool lerp) {
		Point grabberGrabPoint = grabber.getFirstPOIOrDefault("g");
		Point victimGrabOffset = character.pos.subtract(character.getFirstPOIOrDefault("g", 0));

		Point destPos = grabberGrabPoint.add(victimGrabOffset);
		if (character.pos.distanceTo(destPos) > 25) lerp = true;
		Point lerpPos = lerp ? Point.lerp(character.pos, destPos, 0.25f) : destPos;

		var hit = Global.level.checkTerrainCollisionOnce(character, lerpPos.x - character.pos.x, lerpPos.y - character.pos.y);
		if (hit?.gameObject is Wall) {
			return false;
		}

		character.changePos(lerpPos);
		return true;
	}

	public override bool canEnter(Character character) {
		if (!base.canEnter(character)) {
			return false;
		}
		return !character.isInvulnerable() && !character.charState.invincible;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		//character.stopCharge();
		character.useGravity = false;
		character.grounded = false;
		savedZIndex = character.zIndex;
		if (!reverseZIndex) character.setzIndex(grabber.zIndex - 100);
		else character.setzIndex(grabber.zIndex + 100);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.grabInvulnTime = 2;
		if (this is VileMK2Grabbed) {
			character.stunInvulnTime = 1;
		}
		character.useGravity = true;
		character.setzIndex(savedZIndex);
	}
}

public class NetLimbo : CharState {
	public NetLimbo() : base("not_a_real_sprite") {

	}
}
