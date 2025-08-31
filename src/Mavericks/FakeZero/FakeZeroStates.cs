using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FakeZeroMState : MaverickState {
	public bool canReloadAmmo;
	public FakeZero zero = null!;

	public FakeZeroMState(
		string sprite, string transitionSprite = ""
	) : base(
		sprite, transitionSprite
	) {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		zero = maverick as FakeZero ?? throw new NullReferenceException();
	}
}

public class FakeZeroShootState : FakeZeroMState {
	private bool shotOnce;
	private bool shootReleased;
	private bool shootPressedAgain;
	public int busterType = 1;
	public int shootCount = 0;
	public int aiAttack;

	public FakeZeroShootState(int aiAttack = 0) : base("shoot") {
		this.aiAttack = aiAttack;
		canReloadAmmo = true;
		canJump = true;
		airMove = true;
		landSprite = "shoot";
		airSprite = "shoot_air";
		fallSprite = "shoot_fall";
	}

	public override void update() {
		base.update();

		if (aiAttack != 2 && !input.isHeld("shoot", player)) {
			shootReleased = true;
			maverick.sprite.frameSpeed = 1;
		}
		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos.HasValue) {
			shotOnce = true;
			if (busterType == 1 && maverick.ammo >= 4
				&& (aiAttack == 2 || !isAI && !shootReleased && input.isHeld("shoot", player))
			) {
				maverick.playSound("buster2X2", forcePlay: false, sendRpc: true);
				maverick.deductAmmo(FakeZero.shootLv2Ammo);
				new FakeZeroBuster2Proj(
					shootPos.Value, maverick.xDir, maverick,
					player.getNextActorNetId(), sendRpc: true
				);
				busterType = 2;
				canReloadAmmo = false;
			} else {
				maverick.playSound("busterX2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj(
					shootPos.Value, maverick.xDir, maverick,
					player.getNextActorNetId(), sendRpc: true
				);
				busterType = 0;
				shootCount++;
			}
			maverick.sprite.frameSpeed = 1;
		}
		if (!isAI && shootReleased && input.isPressed("shoot", player)) {
			shootPressedAgain = true;
		}
		if (busterType == 0 && shootCount < 2 && maverick.frameIndex >= 2 &&
			(shootPressedAgain || isAI && maverick.controlMode == MaverickModeId.Summoner)
		) {
			maverick.sprite.frameIndex = 0;
			maverick.sprite.frameTime = 0;
			shotOnce = false;
		}
		// Allow to state-cancel once shoot.
		if (maverick.frameIndex >= 4) {
			normalCtrl = true;
			if (maverick.grounded && maverick.input.getXDir(player) != 0) {
				maverick.changeState(new MRun());
			}
		}
		if (stateFrame > 30) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.sprite.frameSpeed = 0.5f;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		if (busterType == 0) {
			maverick.stateCooldowns[typeof(FakeZeroShootState)].cooldown = 0;
		}
	}
}

public abstract class FakeZeroComboShootState : FakeZeroMState {
	public bool shoot;
	public int shootFrame;
	public int shootNum;
	public int comboFrame;
	public bool continueCombo;

	public FakeZeroComboShootState(string sprite) : base(sprite) {
		canJump = true;
		airMove = true;
		canStopJump = true;
	}

	public virtual void shootFunct() { }
	public virtual void comboState() { }

	public override void update() {
		base.update();

		if (maverick.frameIndex >= shootFrame && !shoot) {
			maverick.turnToInput(input, player);
			shootFunct();
			maverick.deductAmmo(FakeZero.shootLv3Ammo);
			zero.shootNum = shootNum;
			shoot = true;
		}
		if (shoot && !continueCombo && maverick.input.isPressed(Control.Special1, player)) {
			continueCombo = true;
		}

		if (continueCombo && maverick.frameIndex >= comboFrame && maverick.ammo >= FakeZero.shootLv3Ammo) {
			comboState();
			return;
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
}

public class FakeZeroC1State : FakeZeroComboShootState {
	public FakeZeroC1State() : base("scombo") {
		shootFrame = 3;
		shootNum = 1;
		comboFrame = 6;
	}

	public override void shootFunct() {
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos == null) {
			return;
		}
		maverick.playSound("buster3X2", forcePlay: false, sendRpc: true);
		new FakeZeroBusterProj3(
			shootPos.Value, maverick.xDir, 0, zero,
			player.getNextActorNetId(), sendRpc: true
		);
	}

	public override void comboState() {
		maverick.changeState(new FakeZeroC2State());
	}
}

public class FakeZeroC2State : FakeZeroComboShootState {
	public FakeZeroC2State() : base("scombo2") {
		shootFrame = 3;
		shootNum = 2;
		comboFrame = 6;
	}

	public override void shootFunct() {
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos == null) {
			return;
		}
		maverick.playSound("buster3X2", forcePlay: false, sendRpc: true);
		new FakeZeroBusterProj3(
			shootPos.Value, maverick.xDir, 0, zero,
			player.getNextActorNetId(), sendRpc: true
		);
	}

	public override void comboState() {
		maverick.changeState(new FakeZeroC3State());
	}
}


public class FakeZeroC3State : FakeZeroComboShootState {
	public FakeZeroC3State() : base("scombo3") {
		shootFrame = 3;
		shootNum = 3;
		comboFrame = 6;
	}

	public override void shootFunct() {
		Point shootPos = maverick.pos.addxy(30 * maverick.xDir, -28);
		maverick.playSound("buster4X2", forcePlay: false, sendRpc: true);
		new FakeZeroSwordBeamProj(
			shootPos, maverick.xDir, zero,
			player.getNextActorNetId(), sendRpc: true
		);
	}

	public override void comboState() {
		maverick.changeState(new FakeZeroC1State());
	}
}

public class FakeZeroMeleeState : FakeZeroMState {
	public FakeZeroMeleeProj? proj;
	public float dashSpeed = 3.25f;
	private bool isAiAttack;
	Anim? projVisible;

	public FakeZeroMeleeState(bool isAiAttack = false) : base("run_attack") {
		this.isAiAttack = isAiAttack;
		enterSound = "saber3";
		normalCtrl = true;
		canReloadAmmo = true;
		useGravity = false;
	}

	public override void update() {
		base.update();
		if (proj != null) {
			proj.xDir = zero.xDir;
			proj.changePos(zero.getFirstPOIOrDefault(1));
		}
		if (projVisible != null) {
			projVisible.xDir = zero.xDir;
			projVisible.changePos(zero.getFirstPOIOrDefault(1));
		}
		maverick.moveXY(zero.xDir * dashSpeed, 0);
		if (stateFrame >= 20) {
			if (player.input.getXDir(player) != 0) {
				maverick.changeState(new MRun());
				return;
			}
			maverick.xFlinchPushVel = dashSpeed * 0.5f * maverick.xDir;
			maverick.changeToIdleOrFall("skid");
			return;
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		projVisible = new Anim(
			maverick.getFirstPOIOrDefault(1), "fakezero_run_sword", maverick.xDir,
			player.getNextActorNetId(), false, sendRpc: true
		);
		proj = new FakeZeroMeleeProj(
			maverick.getFirstPOIOrDefault(1), maverick.xDir,
			zero, player.getNextActorNetId(), sendRpc: true
		);
		maverick.playSound("dashX2");
		float runSpeed = zero.getRunSpeed() / 60f * 1.25f;
		if (runSpeed > dashSpeed) {
			dashSpeed = runSpeed;
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		proj?.destroySelf();
		projVisible?.destroySelf();
	}
}

public class FakeZeroGuardState : MaverickState {
	public FakeZeroGuardState() : base("guard") {
		aiAttackCtrl = true;
		attackCtrl = true;
		superArmor = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		if (isAI) {
			if (stateFrame >= 13) {
				maverick.changeToIdleOrFall();
			}
		} else {
			bool holdGuard;
			if (maverick.useChargeJump) {
				holdGuard = input.isHeld(Control.Down, player);
			} else {
				holdGuard = input.isHeld(Control.Up, player);
			}
			if (!holdGuard) {
				maverick.changeToIdleOrFall();
			}
		}
	}
}

public class FakeZeroGroundPunchState : FakeZeroMState {
	public FakeZeroGroundPunchState() : base("groundpunch") {
		canReloadAmmo = true;
	}

	public override void update() {
		base.update();

		if (maverick.frameIndex == 3 && !once) {
			maverick.playSound("crashX2", forcePlay: false, sendRpc: true);
			maverick.shakeCamera(sendRpc: true);
			once = true;
			RockProjectile(15);
			RockProjectile(-15);

			Global.level.delayedActions.Add(new DelayedAction(() => {
				RockProjectile(35);
				RockProjectile(-35);
			}, 0.075f));

			Global.level.delayedActions.Add(new DelayedAction(() => {
				RockProjectile(-55);
				RockProjectile(55);
			}, 0.15f));
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
	public void RockProjectile(int dist) {
		new FakeZeroRockProj(
			maverick.pos.addxy(dist, 0), maverick.xDir, zero,
			player.getNextActorNetId(), sendRpc: true
		);

	}
}
