using System;

namespace MMXOnline;

public class SigmaClawWeapon : Weapon {
	public static SigmaClawWeapon netWeapon = new();

	public SigmaClawWeapon() : base() {
		index = (int)WeaponIds.Sigma2Claw;
		killFeedIndex = 132;
	}
}

public class SigmaClawState : CharState {
	CharState prevCharState;
	float slideVel;
	bool isAir;
	public NeoSigma neoSigma = null!;

	public SigmaClawState(CharState prevCharState, bool isAir) : base("attack") {
		this.prevCharState = prevCharState;
		this.isAir = isAir;
		useDashJumpSpeed = true;
		airMove = true;
	}

	public override void update() {
		base.update();

		if (!character.grounded) {
			landSprite = "attack";
		} else if (isAir && character.grounded) {
			once = true;
		}

		if (MathF.Abs(slideVel) > 0) {
			character.move(new Point(slideVel, 0));
			slideVel -= Global.spf * 750 * character.xDir;
			if (MathF.Abs(slideVel) < Global.spf * 1000) {
				slideVel = 0;
			}
		}

		/*
		if (!player.input.isHeld(Control.Shoot, player))
		{
			shootHeldContinuously = false;
		}
		if (shootHeldContinuously && character.grounded && character.frameIndex >= 4)
		{
			character.changeToIdleOrFall();
			return;
		}
		*/

		if (player.input.isPressed(Control.Shoot, player) &&
			character.grounded && character.frameIndex >= 4 &&
			sprite != "attack2" &&
			!once
		) {
			once = true;
			sprite = "attack2";
			defaultSprite = sprite;
			neoSigma.normalAttackCooldown = neoSigma.sigmaSaberMaxCooldown;
			character.changeSpriteFromName(sprite, true);
			character.playSound("sigma2slash", sendRpc: true);
			return;
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		neoSigma = character as NeoSigma ?? throw new NullReferenceException();
		if (oldState is Dash) {
			slideVel = character.xDir * character.getDashSpeed();
		}
		if (character.grounded) {
			sprite = "attack";
		} else {
			sprite = "attack_air";
		}
		if (!String.IsNullOrEmpty(prevCharState.attackSprite)) {
			sprite = prevCharState.attackSprite;
		}
		defaultSprite = sprite;
		character.changeSprite(sprite, true);
		character.playSound("sigma2slash", sendRpc: true);
	}
}

public class SigmaElectricBallWeapon : Weapon {
	public static SigmaElectricBallWeapon netWeapon = new();
	public SigmaElectricBallWeapon() : base() {
		index = (int)WeaponIds.Sigma2Ball;
		killFeedIndex = 135;
	}
}

public class SigmaElectricBallProj : Projectile {
	public SigmaElectricBallProj(
		Point pos, int xDir, float byteAngle, Actor owner, 
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "sigma2_ball", netId, player
	) {
		weapon = SigmaElectricBallWeapon.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 12;
		damager.flinch = Global.miniFlinch;
		projId = (int)ProjIds.Sigma2Ball;
		destroyOnHit = false;
		maxTime = 0.5f;
		byteAngle = byteAngle % 256;
		vel.x = 200 * Helpers.cosb(byteAngle);
		vel.y = 200 * Helpers.sinb(byteAngle);
		this.byteAngle = byteAngle;
		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new SigmaElectricBallProj(
			args.pos, args.xDir, args.byteAngle, args.owner, args.player, args.netId
		);
	}
}

public class SigmaElectricBallState : CharState {
	bool fired;
	public NeoSigma neoSigma = null!;
	public SigmaElectricBallState() : base("shoot") {
		enterSound = "sigma2shoot";
		invincible = true;
	}

	public override void update() {
		base.update();

		if (character.frameIndex > 0 && !fired) {
			fired = true;
			character.playSound("sigma2ball", sendRpc: true);
			Point pos = character.pos.addxy(0, -20);
			for (int i = 256; i >= 128; i -= 32) {
				new SigmaElectricBallProj(
					pos, 1, i, neoSigma, player, 
					player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		neoSigma = character as NeoSigma ?? throw new NullReferenceException();
	}
}

public class SigmaElectricBall2Weapon : Weapon {
	public static SigmaElectricBall2Weapon netWeapon = new();
	public SigmaElectricBall2Weapon() : base() {
		index = (int)WeaponIds.Sigma2Ball2;
		killFeedIndex = 135;
	}
}

public class SigmaElectricBall2Proj : Projectile {
	public SigmaElectricBall2Proj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "sigma2_ball2", netId, player
	) {
		weapon = SigmaElectricBall2Weapon.netWeapon;
		damager.damage = 6;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 12;
		projId = (int)ProjIds.Sigma2Ball2;
		destroyOnHit = false;
		maxTime = 0.4f;
		vel = new Point(300*xDir,0);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new SigmaElectricBall2Proj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}
public class SigmaElectricBall2StateEX : CharState {
	public bool fired, soundFired;
	public SigmaElectricBall2Proj? SigmaBalls;
	public NeoSigma neoSigma = null!;
	public SigmaElectricBall2StateEX() : base("shoot2") {
		invincible = true;
	}
	public override void update() {
		character.turnToInput(player.input, player);

		if (character.frameIndex >= 13 && !soundFired) {
			soundFired = true;
			character.playSound("neoSigmaESpark", sendRpc: true);
		}

		Point shootPos = character.getCenterPos().addxy(52*character.xDir, -8);
		if (character.frameIndex >= 17 && !fired) {
			fired = true;
			SigmaBalls = new SigmaElectricBall2Proj(
				shootPos, character.xDir, neoSigma, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		base.update();
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		neoSigma = character as NeoSigma ?? throw new NullReferenceException();
	}
}

/*
public class SigmaElectricBall2State : CharState {
	bool fired;
	bool sound;
	public SigmaElectricBall2State(string transitionSprite = "") : base("shoot2", "", "", transitionSprite) {
		invincible = true;
	}

	public override void update() {
		base.update();

		character.turnToInput(player.input, player);

		if (!sound && character.frameIndex >= 13) {
			sound = true;
			character.playSound("neoSigmaESpark", sendRpc: true);
		}

		if (!fired) {
			fired = true;
			new SigmaElectricBall2Proj(
				new SigmaElectricBall2Weapon(), character.getCenterPos(),
				character.xDir, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
*/
public class SigmaCooldownState : CharState {
	public SigmaCooldownState(string sprite) : base(sprite) {
	}

	public override void update() {
		base.update();
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class SigmaUpDownSlashState : CharState {
	bool isUp;
	public SigmaUpDownSlashState(bool isUp) : base(isUp ? "upslash" : "downslash") {
		this.isUp = isUp;
		enterSound = "sigma2slash";
		exitOnLanding = true;
	}

	public override void update() {
		base.update();

		var moveAmount = new Point(0, isUp ? -1 : 1);

		float maxStateTime = isUp ? 0.35f : 0.5f;
		if (stateTime > maxStateTime ||
			Global.level.checkTerrainCollisionOnce(character, moveAmount.x, moveAmount.y, moveAmount) != null
		) {
			character.changeState(
				character.grounded ? new SigmaCooldownState("downslash_land") : new Fall(), true
			);
			return;
		}

		character.move(moveAmount.times(300));
	}

	public override bool canEnter(Character character) {
		if (!base.canEnter(character)) return false;
		if (isUp && character.dashedInAir > 0) return false;
		return true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.stopMoving();
		if (isUp) {
			character.unstickFromGround();
			character.dashedInAir++;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
