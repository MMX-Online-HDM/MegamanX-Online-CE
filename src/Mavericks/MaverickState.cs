using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class MaverickStateCooldown {
	// "Global" states no longer shares the global cooldown but sets all states at their max.
	public readonly bool isGlobal;
	public readonly bool startOnEnter;
	public readonly float maxCooldown;
	public float cooldown;

	public MaverickStateCooldown(float maxCooldown, bool startOnEnter = false, bool isGlobal = false) {
		this.isGlobal = isGlobal;
		this.startOnEnter = startOnEnter;
		this.maxCooldown = maxCooldown;
	}
}

public class MaverickStateConsecutiveData {
	public int consecutiveCount;
	public int maxConsecutiveCount;
	public float consecutiveDelay;

	public MaverickStateConsecutiveData(int consecutiveCount, int maxConsecutiveCount, float consecutiveDelay = 0) {
		this.consecutiveCount = consecutiveCount;
		this.maxConsecutiveCount = maxConsecutiveCount;
		this.consecutiveDelay = consecutiveDelay;
	}

	public bool isOver() {
		return consecutiveCount >= maxConsecutiveCount - 1;
	}
}

public class MaverickState {
	public string sprite;
	public string defaultSprite;
	public string transitionSprite;
	public float stateTime;
	public float stateFrame;
	public float framesJumpNotHeld = 0;
	public float flySoundTime;
	public string landSprite = "";
	public string airSprite = "";
	public string fallSprite = "";
	public bool wasGrounded = true;

	public bool once;
	public string enterSound = "";
	public bool canEnterSelf = true;
	public bool useGravity = true;
	public bool superArmor;
	public float consecutiveWaitTime;
	public bool stopMoving;
	public bool exitOnAnimEnd;
	public bool wasFlying;

	public Collider? lastLeftWallCollider;
	public Collider? lastRightWallCollider;
	public Wall? lastLeftWall;
	public Wall? lastRightWall;
	public bool hitLadder;

	public Maverick maverick = null!;
	public Player player { get { return maverick.player; } }
	public Input input { get { return maverick.input; } }

	// Control system.
	// This dictates if it can attack or land.
	public bool normalCtrl;
	public bool attackCtrl;
	public bool aiAttackCtrl;

	// Other movement stuff.
	public bool airMove;
	public bool canJump;
	public bool canStopJump;
	public bool stoppedJump;
	public bool exitOnLanding;
	public bool exitOnAirborne;
	public bool useDashJumpSpeed;
	public bool canBeCanceled = true;

	public MaverickStateConsecutiveData? consecutiveData;
	public bool isAI => maverick.aiBehavior != MaverickAIBehavior.Control && !player.isAI;

	public MaverickState(string sprite, string transitionSprite = "") {
		this.sprite = transitionSprite == "" ? sprite : transitionSprite;
		this.transitionSprite = transitionSprite;
		defaultSprite = sprite;
		stateTime = 0;
	}

	public virtual void preUpdate() {
	}

	public virtual void postUpdate() {
		airTrasition();
	}

	public virtual void update() {
		stateTime += Global.spf;

		if (inTransition()) {
			if (maverick.isAnimOver() && !Global.level.gameMode.isOver) {
				sprite = defaultSprite;
				maverick.changeSpriteFromName(sprite, true);
			}
		}

		CollideData? lastLeftWallData = maverick.getHitWall(-1, 0);
		lastLeftWallCollider = lastLeftWallData?.otherCollider;
		if (lastLeftWallCollider != null && !lastLeftWallCollider.isClimbable) lastLeftWallCollider = null;
		lastLeftWall = lastLeftWallData?.gameObject as Wall;

		CollideData? lastRightWallData = maverick.getHitWall(1, 0);
		lastRightWallCollider = lastRightWallData?.otherCollider;
		if (lastRightWallCollider != null && !lastRightWallCollider.isClimbable) lastRightWallCollider = null;
		lastRightWall = lastRightWallData?.gameObject as Wall;

		// Moving platforms detection
		CollideData? leftWallPlat = maverick.getHitWall(-5, 0);
		if (leftWallPlat?.gameObject is Wall leftWall && leftWall.isMoving) {
			maverick.move(leftWall.deltaMove, useDeltaTime: true);
			lastLeftWallCollider = leftWall.collider;
		} else if (leftWallPlat?.gameObject is Actor leftActor &&
			leftActor.isPlatform && leftActor.pos.x < maverick.pos.x
		) {
			lastLeftWallCollider = leftActor.collider;
		}

		CollideData? rightWallPlat = maverick.getHitWall(5, 0);
		if (rightWallPlat?.gameObject is Wall rightWall && rightWall.isMoving) {
			maverick.move(rightWall.deltaMove, useDeltaTime: true);
			lastRightWallCollider = rightWall.collider;
		} else if (rightWallPlat?.gameObject is Actor rightActor &&
			rightActor.isPlatform && rightActor.pos.x > maverick.pos.x
		) {
			lastRightWallCollider = rightActor.collider;
		}

		if (exitOnAnimEnd) {
			if (maverick.isAnimOver()) {
				maverick.changeToIdleOrFall();
			}
		}
	}

	public virtual bool canEnter(Maverick maverick) {
		return canEnterSelf || maverick.state.GetType() != GetType();
	}

	public virtual void onEnter(MaverickState oldState) {
		wasGrounded = maverick.grounded && maverick.vel.y >= 0;
		if (oldState is MFly) wasFlying = true;
		if (enterSound != "") maverick.playSound(enterSound, sendRpc: true);
		if (!useGravity) maverick.useGravity = false;
		if (stopMoving) maverick.stopMoving();
		if (sprite == landSprite && !wasGrounded) {
			sprite = airSprite;
			if (maverick.vel.y >= 0 && fallSprite != "") {
				sprite = fallSprite;
			}
			maverick.changeSpriteFromName(sprite, true);
		}
	}

	public virtual void onExit(MaverickState newState) {
		if (maverick is NeonTiger nt) {
			if (newState is not MJumpStart and not MJump and not MFall
				and not NeonTPounceState and not NeonTAirClawState
			) {
				nt.isDashing = false;
			}
		}
		if (maverick.controlMode == MaverickModeId.Striker) {
			if (!aiAttackCtrl && this is not MEnter && (newState is MIdle || newState is MFall)) {
				maverick.aiCooldown = maverick.maxAICooldown;
				maverick.autoExit = true;
			}
		} else if (!newState.aiAttackCtrl) {
			maverick.aiCooldown = maverick.maxAICooldown;
		}
		if (!useGravity) {
			maverick.useGravity = true;
		}
	}

	public bool inTransition() {
		return transitionSprite != "" && sprite == transitionSprite;
	}

	public bool isHoldStateOver(float minTime, float maxTime, float aiTime, string control) {
		if (isAI) {
			return stateTime > aiTime;
		} else {
			return stateTime > maxTime || (!input.isHeld(control, player) && stateTime > minTime);
		}
	}

	public void landingCode(bool exitMLand = true) {
		if (maverick.canStomp) {
			maverick.shakeCamera(sendRpc: true);
			maverick.playSound("crash", sendRpc: true);
		}
		if (maverick is FlameMammoth fm) {
			new FlameMStompShockwave(fm.pos, fm.xDir, fm, player, player.getNextActorNetId(), rpc: true);
		}
		if (maverick is VoltCatfish vc) {
			if (!vc.bouncedOnce) {
				vc.bouncedOnce = true;
				maverick.changeState(new VoltCBounce(), true);
				return;
			} else {
				vc.bouncedOnce = false;
			}
		}
		maverick.dashSpeed = 1;
		if (exitMLand) maverick.changeState(new MLand(maverick.landingVelY));
		else maverick.changeToIdleFallOrFly();
	}

	public virtual void airTrasition() {
		bool changedSprite = false;
		if (airSprite != "" && !maverick.grounded && wasGrounded && sprite == landSprite) {
			sprite = airSprite;
			if (maverick.vel.y >= 0 && fallSprite != "") {
				sprite = fallSprite;
			}
			changedSprite = true;
		}
		else if (
			landSprite != "" && maverick.grounded && !wasGrounded &&
			(sprite == airSprite || sprite == fallSprite)
		) {
			maverick.playSound("land", sendRpc: true);
			sprite = landSprite;
			changedSprite = true;
		}
		else if (fallSprite != "" && !maverick.grounded && sprite == airSprite && maverick.vel.y >= 0) {
			sprite = fallSprite;
			changedSprite = true;
		}
		if (changedSprite) {
			int oldFrameIndex = maverick.sprite.frameIndex;
			float oldFrameTime = maverick.sprite.frameTime;
			float oldFrameSpeed = maverick.sprite.frameSpeed;
			maverick.changeSpriteFromName(sprite, false);
			if (oldFrameIndex < maverick.sprite.totalFrameNum) {
				maverick.sprite.frameIndex = oldFrameIndex;
				maverick.sprite.frameTime = oldFrameTime;
			} else {
				maverick.sprite.frameIndex = maverick.sprite.totalFrameNum - 1;
				maverick.sprite.frameTime = maverick.sprite.getCurrentFrame().duration;
			}
			maverick.sprite.frameSpeed = oldFrameSpeed;
		}
	}

	public void climbIfCheckClimbTrue() {
		if (input.isHeld(Control.Up, player) && this is not StingCClimb && checkClimb()) {
			if (maverick.grounded) {
				maverick.incPos(new Point(0, -7.5f));
			}
			maverick.changeState(new StingCClimb());
		}
	}

	public bool checkClimb() {
		if (maverick.collider == null) {
			return false;
		}
		Rect rect = maverick.collider.shape.getRect();
		rect.x1 += 10;
		rect.x2 -= 10;
		rect.y1 += 15;
		rect.y2 -= 15;
		var shape = rect.getShape();
		var ladders = Global.level.getTerrainTriggerList(maverick, new Point(0, 0), typeof(Ladder));
		var backwallZones = maverick is StingChameleon ? Global.level.getTerrainTriggerList(
			shape, typeof(BackwallZone)
		) : new List<CollideData>();
		if (ladders.Count > 0 || (
			backwallZones.Count > 0 &&
			!backwallZones.Any(bw => bw.gameObject is BackwallZone { isExclusion: true }))
		) {
			if (ladders.Count > 0) hitLadder = true;
			return true;
		}
		return false;
	}

	public void wallClimbCode() {
		//This logic can be abit confusing, but we are trying to mirror the actual Mega man X wall climb physics
		//In the actual game, X will not initiate a climb if you directly hugging a wall, jump and push in its direction UNTIL you start falling OR you move away and jump into it
		if ((input.isPressed(Control.Left, player) && !player.isAI) || (input.isHeld(Control.Left, player) && (maverick.vel.y > -150 || lastLeftWallCollider == null))) {
			if (maverick.canClimbWall) {
				if (lastLeftWallCollider != null) {
					maverick.changeState(new MWallSlide(-1, lastLeftWall));
					return;
				}
			} else {
				if (lastLeftWallCollider != null && input.isPressed(Control.Jump, player) && Global.maverickWallClimb) {
					maverick.vel.y = -maverick.getJumpPower() * maverick.getYMod();
					maverick.changeState(new MWallKick(1, "jump"));
				}
			}
		} else if ((input.isPressed(Control.Right, player) && !player.isAI) || (input.isHeld(Control.Right, player) && (maverick.vel.y > -150 || lastRightWallCollider == null))) {
			if (maverick.canClimbWall) {
				if (lastRightWallCollider != null) {
					maverick.changeState(new MWallSlide(1, lastRightWall));
					return;
				}
			} else {
				if (lastRightWallCollider != null && input.isPressed(Control.Jump, player) && Global.maverickWallClimb) {
					maverick.vel.y = -maverick.getJumpPower() * maverick.getYMod();
					maverick.changeState(new MWallKick(-1, "jump"));
				}
			}
		}
	}

	public void morphMothBeam(Point shootPos, bool isGround) {
		maverick.playSound("morphmBeam", sendRpc: true);
		Point shootDir;
		var inputDir = input.getInputDir(player);
		if (inputDir.isZero()) shootDir = isGround ? new Point(maverick.xDir, 0) : new Point(0, 1);
		else if (inputDir.x == 0 && inputDir.y == -1) shootDir = new Point(maverick.xDir, -1);
		else shootDir = inputDir;

		if (shootDir.x != 0) shootDir.x = maverick.xDir;

		new MorphMBeamProj(maverick.weapon, shootPos, shootPos.add(shootDir.normalize().times(150)), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
	}

	public CollideData? checkCollision(float x, float y, bool autoVel = false) {
		return Global.level.checkTerrainCollisionOnce(maverick, x, y, autoVel: autoVel);
	}

	// Use this for code that slides the maverick across the ground and needs to check if a side wall was hit.
	// Be sure to pass in y = -2 (or -2 offset).
	// This will handle inclines properly, for example sliding from an incline to another inline, or to flat ground.
	public CollideData? checkCollisionSlide(float x, float y) {
		var hitWall = Global.level.checkTerrainCollisionOnce(maverick, x, y, autoVel: true);
		if (maverick.deltaPos.isCloseToZero(1) && stateFrame > 1) {
			return hitWall;
		}
		return null;
	}

	// Use this for code that needs to check for an accurate normal especially when hitting corners.
	public CollideData? checkCollisionNormal(float x, float y) {
		return Global.level.checkTerrainCollisionOnce(
			maverick, x, y, checkPlatforms: true
		);
	}

	public virtual bool trySetGrabVictim(Character grabbed) {
		return false;
	}
}


public class MLimboState : MaverickState {
	public MLimboState() : base("") {
		aiAttackCtrl = true;
		canBeCanceled = false;
	}
}

public class MIdle : MaverickState {
	public MIdle(string transitionSprite = "") : base("idle", transitionSprite) {
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	float attackCooldown = 0;
	public override void update() {
		base.update();

		if (maverick == null) return;

		Helpers.decrementTime(ref attackCooldown);

		bool dashCondition = input.isHeld(Control.Left, player) || input.isHeld(Control.Right, player);

		if (dashCondition) {
			if (!maverick.isAttacking() && (maverick.aiBehavior != MaverickAIBehavior.Control || maverick is not BoomerangKuwanger)) {
				if (input.isHeld(Control.Left, player)) maverick.xDir = -1;
				if (input.isHeld(Control.Right, player)) maverick.xDir = 1;

				bool changeToRun = true;
				if (maverick is OverdriveOstrich) {
					Global.breakpoint = true;
					var hit = Global.level.checkTerrainCollisionOnce(maverick, maverick.xDir, -2, vel: new Point(maverick.xDir, 0));
					Global.breakpoint = false;
					if (hit?.isSideWallHit() == true) {
						changeToRun = false;
					}
				}

				if (changeToRun) {
					maverick.changeState(new MRun());
				}
			}
		}

		if (Global.level.gameMode.isOver && player != null && maverick != null) {
			if (Global.level.gameMode.playerWon(player)) {
				maverick.changeState(new MTaunt());
			}
		}
	}
}

public class MEnter : MaverickState {
	public float destY;
	public MEnter(Point destPos) : base("enter") {
		destY = destPos.y;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		maverick.alpha = Helpers.clamp01(stateTime * 2);
		maverick.incPos(new Point(0, maverick.vel.y * Global.spf));
		maverick.vel.y += Global.speedMul * Physics.Gravity;
		if (maverick.pos.y >= destY) {
			maverick.changePos(new Point(maverick.pos.x, destY));
			if (maverick is DrDoppler) {
				maverick.changeState(new DrDopplerUncoatState(), true);
			} else {
				landingCode();
			}
		}
	}

	public Point getDestPos() {
		return new Point(maverick.pos.x, destY);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
		maverick.alpha = 0;
		maverick.pos.y = destY - 32;
		if (maverick.controlMode != MaverickModeId.TagTeam && !once) {
			maverick.playSound("warpIn", sendRpc: true);
			once = true;
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
		maverick.alpha = 1;
	}
}

public class MExit : MaverickState {
	public float destY;
	public float startY;
	public Point destPos;

	bool isRecall;
	public const float yPos = 164;
	public MExit(Point destPos, bool isRecall) : base("exit") {
		this.destPos = destPos;
		this.isRecall = isRecall;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		float dist = MathF.Abs(startY - destY);
		float trav = MathF.Abs(maverick.pos.y - destY);
		float per = trav / dist;

		maverick.alpha = Helpers.clamp01(per);
		maverick.incPos(new Point(0, maverick.vel.y * Global.spf));
		maverick.vel.y += Physics.Gravity * Global.speedMul * maverick.getYMod();
		if ((maverick.getYMod() == 1 && maverick.pos.y < destY) || (maverick.getYMod() == -1 && maverick.pos.y > destY)) {
			maverick.changePos(destPos.addxy(0, -yPos * maverick.getYMod()));
			if (!isRecall) {
				maverick.changeState(new MEnter(destPos));
			} else {
				maverick.destroySelf();
			}
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
		maverick.vel.x = 0;
		maverick.vel.y = -400 * maverick.getYMod();
		startY = maverick.pos.y;
		destY = maverick.pos.y - (32 * maverick.getYMod());
		if (maverick.controlMode != MaverickModeId.TagTeam && !once) {
			maverick.playSound("warpOut", sendRpc: true);
			once = true;
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.alpha = 1;
		maverick.useGravity = true;
		maverick.vel = new Point();
	}
}

public class MTaunt : MaverickState {
	public MTaunt() : base("taunt") {
		attackCtrl = true;
		aiAttackCtrl = true;
	}

	public override void update() {
		base.update();
		if (maverick.isAnimOver() && !Global.level.gameMode.playerWon(player)) {
			maverick.changeToIdleFallOrFly();
		}
		if (maverick.sprite.name == "bcrab_taunt" && stateTime >= 14 / 60f && !once) {
			once = true;
			maverick.playSound("sigma2start", sendRpc: true);
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		if (maverick is FlameMammoth) {
			maverick.playSound("flamemTaunt", sendRpc: true);
		} else if (maverick is Velguarder) {
			maverick.playSound("velgHowl", sendRpc: true);
		} else if (maverick is OverdriveOstrich) {
			maverick.playSound("overdriveoTaunt", sendRpc: true);
		} else if (maverick is FakeZero) {
			maverick.playSound("ching", sendRpc: true);
		} else if (maverick is CrystalSnail) {
			maverick.playSound("csnailSpecial", sendRpc: true);
		}
		if (maverick is BlastHornet && player.input.isHeld(Control.Up, player)) {
			maverick.changeSprite("bhornet_taunt2", true);
			if (maverick.isAnimOver()) {
				maverick.changeState(new MIdle());
			}
		}
	}
}

public class MRun : MaverickState {
	float dustTime;
	float runSoundTime;
	int xDir = 1;

	public MRun() : base("run") {
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
		exitOnAirborne = true;
	}

	public override void preUpdate() {
		base.preUpdate();
		xDir = maverick.xDir;
	}

	public override void update() {
		base.update();
		int inputDir = input.getXDir(player);

		var oo = maverick as OverdriveOstrich;
		if (oo != null) {
			Helpers.decrementTime(ref dustTime);
			Helpers.decrementTime(ref runSoundTime);
			if (dustTime == 0 && oo.getRunSpeed() >= oo.dustSpeed) {
				new Anim(oo.pos.addxy(-oo.xDir * 15, 0), "dust", oo.xDir, player.getNextActorNetId(), true, sendRpc: true) { frameSpeed = 1.5f };
				dustTime = 0.05f;
			}
			if (runSoundTime == 0 && oo.getRunSpeed() >= oo.dustSpeed) {
				oo.playSound("overdriveoRun", sendRpc: true);
				runSoundTime = 0.175f;
			}

			if (xDir == 1 && inputDir != 1 && oo.getRunSpeed() >= oo.skidSpeed) {
				oo.changeState(new OverdriveOSkidState(), true);
				return;
			}
			if (xDir == -1 && inputDir == 1 && oo.getRunSpeed() >= oo.skidSpeed) {
				oo.changeState(new OverdriveOSkidState(), true);
				return;
			}
		}

		var move = new Point(0, 0);
		if (inputDir == -1) {
			maverick.xDir = -1;
			move.x = -maverick.getRunSpeed();
		} else if (inputDir == 1) {
			maverick.xDir = 1;
			move.x = maverick.getRunSpeed();
		}
		if (move.magnitude > 0) {
			maverick.move(move);
		} else {
			if (oo != null && oo.getRunSpeed() >= 100) {
				oo.accSpeed -= Global.spf * 500;
				maverick.move(new Point(oo.getRunSpeed() * oo.xDir, 0));
				//oo.changeState(new OverdriveOSkidState(), true);
				return;
			} else {
				maverick.changeState(new MIdle());
			}
		}

		if (maverick is OverdriveOstrich oo2 && move.x != 0) {
			bool overWallSkidSpeed = oo2.getRunSpeed() >= oo2.wallSkidSpeed;
			var hit = checkCollisionSlide(MathF.Sign(move.x), -2);
			if (hit?.isSideWallHit() == true && maverick.deltaPos.isZero() && stateFrame > 1) {
				if (overWallSkidSpeed) {
					oo2.changeState(new OverdriveOSkidState(), true);
				} else {
					maverick.changeState(new MIdle());
				}
			}
		}
		if (maverick is FakeZero fzero && !once) {
			if (fzero.getRunSpeed() > 230) {
				fzero.changeSpriteFromName("run2", true);
				fzero.sprite.frameSpeed = 1;
				once = true;
			} else if (fzero.frameIndex > 0) {
				float speed = ((fzero.getRunSpeed() / 100) - 1) / 2;
				if (speed > 0) {
					fzero.sprite.frameSpeed = 1 + speed * 1.25f;
				}
			}
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		flySoundTime = 0;
	}
}

public class MJumpStart : MaverickState {
	public float jumpFramesHeld;
	public float preJumpFramesHeld;
	public float maxPreJumpFrames = 2;
	public float maxJumpFrames = 12;
	public float additionalJumpPower;
	public bool isChargeJump;

	public MJumpStart(float additionalJumpPower = 1) : base("jump_start") {
		this.additionalJumpPower = additionalJumpPower;
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		int inputDir = input.getXDir(player);
		if (inputDir != 0) {
			maverick.xDir = inputDir;
			maverick.move(new Point(Physics.WalkSpeed * inputDir, 0));
		}

		if (maverick is BoomerangKuwanger ||
			(maverick is OverdriveOstrich oo && oo.deltaPos.magnitude > 100 * Global.spf)
		) {
			maverick.vel.y = -maverick.getJumpPower() * maverick.getYMod() * (MathF.Abs(maverick.deltaPos.x / 10f) + 1);
			maverick.changeState(new MJump());
			return;
		}

		if (maverick is FakeZero && maverick.getRunSpeed() > 125) {
			maverick.vel.y = -maverick.getJumpPower() * maverick.getYMod() * additionalJumpPower;;
			maverick.changeState(new MJump());
			return;
		}

		if (maverick is NeonTiger nt && nt.isDashing) {
			maverick.vel.y = -maverick.getJumpPower() * getJumpModifier() * maverick.getYMod() * 0.7f;
			var jumpState = new NeonTPounceState();
			maverick.changeState(jumpState);
			return;
		}

		if (!maverick.useChargeJump) {
			if (stateFrame > 2) {
				maverick.vel.y = -maverick.getJumpPower() * maverick.getYMod() * additionalJumpPower;;
				maverick.changeState(new MJump());
			}
			return;
		}

		bool jumpHeld = player.input.isHeld(Control.Jump, player);
		if (maverick.aiBehavior != MaverickAIBehavior.Control) {
			jumpHeld = true;
		}
		if (!jumpHeld) {
			maverick.vel.y = -maverick.getJumpPower() * getJumpModifier() * maverick.getYMod() * additionalJumpPower;
			maverick.changeState(new MJump());
			return;
		}
		else if (stateFrame > maxPreJumpFrames) {
			jumpFramesHeld += maverick.speedMul;
			if (jumpFramesHeld > maxJumpFrames) { jumpFramesHeld = maxJumpFrames; }
		}
		if (maverick.isAnimOver() && stateFrame >= 10) {
			maverick.vel.y = -maverick.getJumpPower() * getJumpModifier() * maverick.getYMod() * additionalJumpPower;
			maverick.changeState(new MJump());
		}
	}

	public float getJumpModifier() {
		if (!isChargeJump) {
			return 1;
		}
		float minHeight = 0.75f;
		float maxHeight = 1.5f;

		return minHeight + (maxHeight - minHeight) * (jumpFramesHeld / maxJumpFrames);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		isChargeJump = maverick.useChargeJump;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		if (isChargeJump) {
			maverick.dashSpeed = 1 + (jumpFramesHeld / maxJumpFrames);
		}
	}
}

public class MJump : MaverickState {
	public int jumpFramesHeld = 0;
	public bool fromCling;
	public MaverickState? followUpAiState;

	public MJump(MaverickState? followUpAiState = null) : base("jump") {
		this.followUpAiState = followUpAiState;
		enterSound = "jump";
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
		exitOnLanding = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();

		if (maverick.vel.y * maverick.getYMod() > 0) {
			int lastFrameIndex = maverick.frameIndex;
			float lastFrameTime = maverick.frameTime;
			//float lastFrameTime = maverick.frameTime;
			maverick.changeState(followUpAiState ?? new MFall());
			if (maverick.state is MFall && maverick is Velguarder) {
				maverick.frameIndex = lastFrameIndex;
				maverick.frameTime = lastFrameTime;
			}
			return;
		}
	}
}

public class MFall : MaverickState {
	public MFall(string transitionSprite = "") : base("fall", transitionSprite) {
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
		exitOnLanding = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (player == null) return;
	}
}

public class MFly : MaverickState {
	public MFly(string transitionSprite = "") : base("fly", transitionSprite) {
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		if (player == null) return;
		Helpers.decrementFrames(ref maverick.flyBar);

		if (Global.level.checkTerrainCollisionOnce(maverick, 0, -maverick.getYMod()) != null && maverick.vel.y * maverick.getYMod() < 0) {
			maverick.vel.y = 0;
		}

		Point move;
		if (maverick is BlastHornet) {
			move = getHornetMove();
		} else {
			move = getMove();
		}

		if (move.magnitude > 0) {
			maverick.move(move);
		}

		if (input.isPressed(Control.Jump, player)) {
			maverick.changeToIdleOrFall();
		} else if (maverick.grounded) {
			landingCode();
		}
	}

	public Point flyVel;
	float flyVelAcc = 500;
	float flyVelMaxSpeed = 175;
	public Point getHornetMove() {
		var inputDir = input.getInputDir(player);
		if (inputDir.isZero()) {
			flyVel = Point.lerp(flyVel, Point.zero, Global.spf * 5f);
		} else {
			float ang = flyVel.angleWith(inputDir);
			float modifier = Math.Clamp(ang / 90f, 1, 2);

			flyVel.inc(inputDir.times(Global.spf * flyVelAcc * modifier));
			if (flyVel.magnitude > flyVelMaxSpeed) {
				flyVel = flyVel.normalize().times(flyVelMaxSpeed);
			}
		}

		if (maverick.flyBar <= 0 || maverick.gravityWellModifier > 1) {
			flyVel.y = 100;
		}

		maverick.turnToInput(input, player);

		var hit = maverick.checkCollision(flyVel.x * Global.spf, flyVel.y * Global.spf);
		if (hit != null && !hit.isGroundHit()) {
			flyVel = flyVel.subtract(flyVel.project(hit.getNormalSafe()));
		}

		return flyVel;
	}

	public Point getMove() {
		Point move = new Point();

		if (input.isHeld(Control.Left, player)) {
			move.x = -maverick.getRunSpeed() * maverick.getAirSpeed();
			maverick.xDir = -1;
		} else if (input.isHeld(Control.Right, player)) {
			move.x = maverick.getRunSpeed() * maverick.getAirSpeed();
			maverick.xDir = 1;
		}

		if (maverick.flyBar > 0 && maverick.gravityWellModifier <= 1) {
			if (input.isHeld(Control.Up, player)) {
				if (maverick.pos.y > -5) {
					move.y = -maverick.getRunSpeed();
				}
			} else if (input.isHeld(Control.Down, player)) {
				move.y = maverick.getRunSpeed();
			}
		} else {
			move.y = maverick.getRunSpeed();
		}

		if (!maverick.isUnderwater()) {
			move.x *= 1.25f;
		} else {
			move.x *= 0.75f;
			move.y *= 0.75f;
			maverick.frameSpeed = 0.75f;
		}

		if (move.y != 0) {
			if (sprite != "fly_fall") {
				sprite = "fly_fall";
				maverick.changeSpriteFromName(sprite, false);
			}
		} else {
			if (sprite != "fly") {
				sprite = "fly";
				maverick.changeSpriteFromName(sprite, false);
			}
		}

		return move;
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;

		float flyVelX = 0;
		if (maverick.dashSpeed > 1) {
			flyVelX = maverick.deltaPos.normalize().times(flyVelMaxSpeed).x;
		}
		float flyVelY = maverick.vel.y;
		flyVel = new Point(flyVelX, flyVelY);
		if (flyVel.magnitude > flyVelMaxSpeed) flyVel = flyVel.normalize().times(flyVelMaxSpeed);

		maverick.dashSpeed = 1;
		maverick.stopMoving();
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
		maverick.stopMoving();
	}
}

public class MLand : MaverickState {
	float landingVelY;
	bool jumpHeldOnce;
	public MLand(float landingVelY) : base("land") {
		this.landingVelY = landingVelY;
		enterSound = "land";
		normalCtrl = true;
		attackCtrl = true;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		if (landingVelY < 360) maverick.frameSpeed = 1.5f;
		if (landingVelY < 315) maverick.frameSpeed = 2f;
	}

	public override void update() {
		base.update();
		int inputDir = input.getXDir(player);
		
		if (maverick is OverdriveOstrich oo) {
			if (oo.getRunSpeed() > 100) maverick.frameSpeed = 2;
			if (oo.getRunSpeed() > 200) maverick.frameSpeed = 3;
			if (maverick.isAnimOver() || oo.getRunSpeed() > 300) {
				maverick.changeState(new MIdle());
			}
			if (inputDir != 0) {
				maverick.changeState(new MRun());
			}
		} else if (maverick is BubbleCrab bc && bc.shield != null) {
			jumpHeldOnce = jumpHeldOnce || input.isHeld(Control.Jump, player);
			if (!once) {
				once = true;
				bc.shield.startShieldBounceY();
				bc.playSound("bcrabBounce", sendRpc: true);
			}
			if (bc.isAnimOver() || bc.shield.shieldBounceTimeY > bc.shield.halfShieldBounceMaxTime) {
				if (jumpHeldOnce) {
					float additionalJumpPower = 1 + (landingVelY / 400) * 0.25f;
					bc.changeState(new MJumpStart(additionalJumpPower));
				} else {
					maverick.changeToIdleRunOrFall();
				}
			}
		} else {
			if (maverick.isAnimOver()) {
				if (inputDir == 0) {
					maverick.changeToIdleRunOrFall();
					return;
				}
				maverick.changeState(new MRun());
				return;
			}
		}
		if (inputDir != 0) {
			maverick.xDir = inputDir;
			maverick.move(new(maverick.getRunSpeed() * inputDir, 0));
		}
	}
}

public class MHurt : MaverickState {
	public float flinchYPos;
	public bool isCombo;
	public int hurtDir;
	public float hurtSpeed;
	public float flinchTime;

	public MHurt(int dir, int flinchFrames, float? oldComboPos = null) : base("hurt") {
		this.flinchTime = flinchFrames;
		hurtDir = dir;
		hurtSpeed = dir * 1.6f;
		flinchTime = flinchFrames;
		if (oldComboPos != null) {
			isCombo = true;
			flinchYPos = oldComboPos.Value;
		}
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 1.6f / flinchTime * Global.speedMul, hurtDir);
			maverick.move(new Point(hurtSpeed * 60f, 0));
		}
		if (stateFrame >= flinchTime) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		if (oldState is MFly) {
			wasFlying = true;
			maverick.useGravity = false;
		} else if (
			oldState.wasFlying && oldState is StormEDiveState or StormEAirShootState or StormEEggState or
			MorphMSweepState or MorphMShootAir or BHornetShootState or BHornetShoot2State or BHornetStingState
		) {
			wasFlying = true;
			maverick.useGravity = false;
		} else if (oldState is MHurt flinchState) {
			isCombo = true;
			flinchYPos = flinchState.flinchYPos;
			wasFlying = flinchState.wasFlying;
			if (wasFlying) {
				maverick.useGravity = false;
			}
		}
		if (shouldKnockUp()) {
			if (isCombo) {
				maverick.vel.y = (-0.125f * (flinchTime - 1)) * 60f;
				if (isCombo && maverick.pos.y < flinchYPos) {
					// Magic equation. Changing gravity from 0.25 probably super-break this.
					// That said, we do not change base gravity.
					maverick.vel.y *= (0.002f * flinchTime - 0.076f) * (flinchYPos - maverick.pos.y) + 1;
				}
			} else {
				flinchYPos = maverick.pos.y;
			}
		}
		if (maverick.sprite.name.EndsWith("hurt")) {
			maverick.changeSpriteFromName("die", true);
		}
	}

	public override void onExit(MaverickState newState) {
		maverick.useGravity = true;
		base.onExit(newState);
	}

	public bool noMove() {
		return (
			maverick is LaunchOctopus lc && lc.isUnderwater() ||
			maverick.armorClass == Maverick.ArmorClass.Heavy ||
			maverick is StormEagle && wasFlying
		);
	}

	public bool shouldKnockUp() {
		return (!wasFlying && maverick.armorClass != Maverick.ArmorClass.Heavy);
		//return maverick is StingChameleon or Velguarder or WireSponge or ChillPenguin;
	}
}

public class MDie : MaverickState {
	bool isEnvDeath;
	Point deathPos;
	public float spawnTime = 0;
	public int radius = 28;
	public MDie(bool isEnvDeath) : base("die") {
		this.isEnvDeath = isEnvDeath;
		canBeCanceled = false;
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
		maverick.stopMovingS();
		maverick.globalCollider = null;
		deathPos = maverick.pos;
		if (maverick is Velguarder) {
			maverick.visible = false;
			new VelGDeathAnim(deathPos, maverick.xDir, player, player.getNextActorNetId(), sendRpc: true);
		} else if (maverick is FakeZero) {
			maverick.visible = false;
			Anim.createGibEffect("fakezero_piece", maverick.getCenterPos(), player, gibPattern: GibPattern.SemiCircle, sendRpc: true);
			maverick.playSound("explosion", sendRpc: true);
		}
		if (isEnvDeath) {
			maverick.lastGroundedPos = null;
		}
	}

	public override void update() {
		base.update();
		if (player == null) return;
		if (maverick.destroyed) return;

		float maxTime = 0.75f;
		if (maverick is Velguarder) {
			maxTime = 0.5f;
		} else if (maverick is FakeZero) {
			maxTime = 0;
		}
		if (stateTime > maxTime && !once) {
			once = true;

			if (maverick is not Velguarder && maverick is not FakeZero) {
				var dieEffect = ExplodeDieEffect.createFromActor(maverick.player, maverick, 20, 5.5f, true, overrideCenterPos: maverick.getCenterPos());
				Global.level.addEffect(dieEffect);
			}

			if (player.maverick1v1 != null) {
				player.maverick1v1Kill();
			} else if (maverick.ownerChar?.currentMaverick == maverick) {
				// If sigma is not dead, become sigma
				if (player.character != null && player.character.charState is not Die) {
					Point spawnPos;
					Point closestSpawnPoint = Global.level.getClosestSpawnPoint(deathPos).pos;
					if (isEnvDeath) {
						if (maverick.lastGroundedPos == null) {
							spawnPos = closestSpawnPoint;
						} else {
							spawnPos = deathPos.distanceTo(closestSpawnPoint) < deathPos.distanceTo(maverick.lastGroundedPos.Value) ?
								closestSpawnPoint :
								maverick.lastGroundedPos.Value;
						}
					} else if (maverick.lastGroundedPos != null) {
						spawnPos = deathPos.distanceTo(closestSpawnPoint) < deathPos.distanceTo(maverick.lastGroundedPos.Value) ?
								closestSpawnPoint :
								maverick.lastGroundedPos.Value;
					} else {
						spawnPos = closestSpawnPoint;
					}

					(player.character as BaseSigma)?.becomeSigma(spawnPos, maverick.xDir);
					player.removeWeaponSlot(player.weapons.FindIndex(w => w is MaverickWeapon mw && mw.maverick == maverick));
					player.changeWeaponSlot(player.weapons.FindIndex(w => w is SigmaMenuWeapon));
				}
				/*
				// If sigma is dead, become the next maverick if available
				else if (player.character != null)
				{
					var firstAvailableMaverick = player.weapons.FirstOrDefault(w => w is MaverickWeapon mw && mw.maverick != null && mw.maverick != maverick) as MaverickWeapon;
					if (firstAvailableMaverick != null)
					{
						player.character.becomeMaverick(firstAvailableMaverick.maverick);
					}

					player.weapons.RemoveAll(w => w is MaverickWeapon mw && mw.maverick == maverick);

					if (firstAvailableMaverick != null)
					{
						player.weaponSlot = player.weapons.IndexOf(firstAvailableMaverick);
					}
					else
					{
						player.weaponSlot = 0;
					}
				}
				*/
			} else {
				player.weapons.RemoveAll(w => w is MaverickWeapon mw && mw.maverick == maverick);
				player.changeToSigmaSlot();
			}

			maverick.destroySelf();
		}
	}
}

public class MWallSlide : MaverickState {
	public int wallDir;
	public float dustTime;
	public Wall wall;
	public bool leftOff;
	public MWallSlide(int wallDir, Wall wall) : base("wall_slide") {
		this.wallDir = wallDir;
		this.wall = wall;
		enterSound = "land";
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		if (maverick.grounded) {
			maverick.changeToIdleOrFall();
			return;
		}
		if (input.isPressed(Control.Jump, player)) {
			maverick.vel.y = -maverick.getJumpPower();
			maverick.changeState(new MWallKick(wallDir * -1));
			return;
		} else if (input.isPressed(Control.Dash, player)) {
			if (maverick is Velguarder) {
				maverick.changeState(new VelGPounceState());
				maverick.vel.y = -maverick.getJumpPower() * 0.75f;
				maverick.xDir *= -1;
			} else if (maverick is FlameStag) {
				maverick.changeState(new FStagWallDashState());
				if (!input.isHeld(Control.Down, player)) maverick.vel.y = -maverick.getJumpPower() * 1.5f;
				else maverick.incPos(new Point(-wallDir * 5, 0));
				maverick.xDir *= -1;
			} else if (maverick is NeonTiger nt) {
				nt.isDashing = true;
				maverick.changeState(new NeonTPounceState());
				maverick.vel.y = -100;
				maverick.xDir *= -1;
			}
			return;
		}

		maverick.useGravity = false;
		maverick.vel.y = 0;

		if (stateTime > 0.15) {
			var dirHeld = wallDir == -1 ? input.isHeld(Control.Left, player) : input.isHeld(Control.Right, player);
			if (!dirHeld || Global.level.checkTerrainCollisionOnce(maverick, wallDir, 0) == null) {
				maverick.changeState(new MFall());
			}
			if (maverick is not NeonTiger) {
				maverick.move(new Point(0, 100));
			} else {
				maverick.stopMovingS();
			}
		}

		if (maverick is not NeonTiger) {
			dustTime += Global.spf;
			if (stateTime > 0.2 && dustTime > 0.1) {
				dustTime = 0;
				new Anim(maverick.pos.addxy(maverick.xDir * 12, 0), "dust", maverick.xDir, null, true);
			}
		}
	}

	public MWallSlide cloneLeaveOff() {
		return new MWallSlide(wallDir, wall) {
			leftOff = true,
		};
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMovingS();
		if (leftOff) {
			maverick.frameIndex = maverick.sprite.totalFrameNum - 1;
			maverick.useGravity = false;
		}
	}

	public override void onExit(MaverickState newState) {
		maverick.useGravity = true;
		base.onExit(newState);
	}
}

public class MWallKick : MaverickState {
	public int kickDir;
	public float kickSpeed;
	public bool cancelKick;

	public MWallKick(int kickDir, string? overrideSprite = null) : base(overrideSprite ?? "wall_kick") {
		this.kickDir = kickDir;
		kickSpeed = kickDir * 150;
		enterSound = "jump";
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		if (kickSpeed != 0) {
			kickSpeed = Helpers.toZero(kickSpeed, 800 * Global.spf, kickDir);
			bool stopMove = false;
			if (player.input.isHeld(Control.Left, player) && kickSpeed < 0 ||
				player.input.isHeld(Control.Right, player) && kickSpeed > 0
			) {
				cancelKick = true;
			}
			maverick.move(new Point(kickSpeed, 0));
		} else {
			airMove = true;
		}
		if (maverick.vel.y > 0 || cancelKick) {
			maverick.changeState(new MFall());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		new Anim(maverick.pos.addxy(12 * maverick.xDir, 0), "wall_sparks", maverick.xDir, null, true);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
	}
}

// A generic shoot projectile state that any Maverick can use
public class MShoot : MaverickState {
	bool shotOnce;
	string shootSound;
	public Action<Point, int> getProjectile;
	public float shootFramesHeld;
	bool shootReleased;
	public MShoot(Action<Point, int> getProjectile, string shootSound) : base("shoot") {
		this.getProjectile = getProjectile;
		this.shootSound = shootSound;
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (input.isHeld(Control.Shoot, player)) {
			if (!shootReleased) {
				shootFramesHeld += maverick.speedMul;
			}
		} else {
			shootReleased = true;
		}

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			if (!string.IsNullOrEmpty(shootSound)) maverick.playSound(shootSound, sendRpc: true);
			getProjectile(shootPos.Value, maverick.xDir);
		}

		if (consecutiveWaitTime > 0 && consecutiveData != null) {
			consecutiveWaitTime += Global.spf;
			if (consecutiveWaitTime >= consecutiveData.consecutiveDelay) {
				consecutiveData.consecutiveCount++;
				MShoot newState = new MShoot(getProjectile, shootSound) {
					consecutiveData = consecutiveData
				};
				maverick.changeState(newState, ignoreCooldown: true);
			}
		}

		if (maverick.isAnimOver()) {
			if (consecutiveData?.isOver() == false) {
				if (consecutiveWaitTime == 0) {
					maverick.changeSpriteFromName("idle", true);
					consecutiveWaitTime = Global.spf;
				}
			} else {
				maverick.changeToIdleFallOrFly();
			}
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
	}
}
