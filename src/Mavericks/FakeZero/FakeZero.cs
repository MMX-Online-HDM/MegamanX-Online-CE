using System;
using System.Collections.Generic;
namespace MMXOnline;

public class FakeZero : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }
	public float dashDist;
	public float baseSpeed = 50;
	public float accSpeed;
	public int lastDirX;
	public Anim? exhaust;
	public float topSpeed = 200;
	public int shootNum = 0;

	// Ammo uses.
	public static int shootLv2Ammo = 3;
	public static int shootLv3Ammo = 4;

	// Main creation function.
	public FakeZero(
		Player player, Point pos, int xDir, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(FakeZeroMeleeState), new(30) },
			{ typeof(FakeZeroGroundPunchState), new(60) },
			{ typeof(FakeZeroShootState), new(30, true) },
		};

		weapon = getWeapon();
		awardWeaponId = WeaponIds.Buster;
		weakWeaponId = WeaponIds.SpeedBurner;
		weakMaverickWeaponId = WeaponIds.FlameStag;
		canClimbWall = true;
		canClimb = true;

		netActorCreateId = NetActorCreateId.FakeZero;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		exhaust = new Anim(
			pos, "fakezero_exhaust", xDir,
			player.getNextActorNetId(), false, sendRpc: false
		) {
			visible = false
		};

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 28;
		maxAmmo = 28;
		grayAmmoLevel = 3;
		barIndexes = (60, 49);
		gameMavs = GameMavs.X2;
		height = 36;
	}

	public override void preUpdate() {
		base.preUpdate();
		if (exhaust != null) {
			if (sprite.name.Contains("dash")) {
				exhaust.zIndex = zIndex - 100;
				exhaust.visible = true;
				exhaust.xDir = xDir;
				exhaust.changePos(getFirstPOIOrDefault());
			} else {
				exhaust.visible = false;
			}
		}
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (state.normalCtrl || state.attackCtrl || state.aiAttackCtrl ||
			state is FakeZeroMState { canReloadAmmo: true }
		) {
			rechargeAmmo(2);
		}
		if (lastDirX != xDir) {
			accSpeed = 0;
			dashDist = 0;
			if (state is MRun mrun) {
				mrun.once = false;
				changeSpriteFromName("run", true);
				frameIndex = 1;
			}
		}
		lastDirX = xDir;

		if (state is MRun || state is FakeZeroMeleeState) {
			dashDist += accSpeed * Global.spf;
			accSpeed += Global.spf * 150;
			if (accSpeed > topSpeed) {
				accSpeed = topSpeed;
			}
		} else if (grounded && state is not MLand and not MJumpStart || state is MHurt) {
			accSpeed = 0;
		}
	}

	public override bool attackCtrl() {
		if (input.isHeld(Control.Shoot, player) && state is MRun) {
			changeState(new FakeZeroMeleeState());
			return true;
		}
		if (input.isHeld(Control.Shoot, player)) {
			changeState(new FakeZeroShootState(), false);
			return true;
		}
		if (input.isPressed(Control.Special1, player) && ammo >= shootLv3Ammo) {
			changeState(getBusterComboState());
			return true;
		}
		if (input.isPressed(Control.Dash, player)) {
			changeState(new FakeZeroGroundPunchState());
			return true;
		}
		if (grounded) {
			bool holdGuard;
			if (useChargeJump) {
				holdGuard = input.isHeld(Control.Down, player);
			} else {
				holdGuard = input.isHeld(Control.Up, player);
			}
			if (holdGuard &&state is not FakeZeroGuardState) {
				changeState(new FakeZeroGuardState());
				return true;
			}
		}
		return false;
	}

	public MaverickState getBusterComboState() {
		return shootNum switch {
			1 => new FakeZeroC2State(),
			2 => new FakeZeroC3State(),
			_ => new FakeZeroC1State()
		};
	}

	public override float getRunSpeed() {
		float retSpeed = baseSpeed + accSpeed;
		if (retSpeed > Physics.WalkSpeedSec) {
			return retSpeed * getRunDebuffs();
		}
		return Physics.WalkSpeedSec * getRunDebuffs();
	}

	public override string getMaverickPrefix() {
		return "fakezero";
	}

	public override MaverickState[] strikerStates() {
		return [
			new FakeZeroShootState(2),
			new FakeZeroC1State(),
			new FakeZeroGroundPunchState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		List<MaverickState> aiStates = [
			new FakeZeroShootState(1)
		];
		float enemyDist = 300;

		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		if (ammo >= shootLv2Ammo * 2 || ammo >= shootLv2Ammo && Helpers.randomRange(0, 2) == 0) {
			aiStates.Add(new FakeZeroShootState(2));
		}
		if (ammo >= shootLv3Ammo) {
			aiStates.Add(getBusterComboState());
		}
		if (enemyDist <= 70) {
			aiStates.Add(new FakeZeroGroundPunchState());
		}
		else {
			aiStates.Add(new FakeZeroMeleeState(true));
		}
		return aiStates.ToArray();
	}

	public override void aiUpdate() {
		base.aiUpdate();
		if (controlMode == MaverickModeId.Summoner &&
			Helpers.randomRange(0, 2) == 1 && ammo >= 8 && state.aiAttackCtrl
		) {
			foreach (GameObject gameObject in getCloseActors(64, true, false, false)) {
				if (gameObject is Projectile proj &&
					proj.damager.owner.alliance != player.alliance &&
					!proj.isMelee
				) {
					changeState(new FakeZeroGuardState());
				}
			}
		}
	}

	public MaverickState getShootState(bool isAI) {
		var mshoot = new MShoot((Point pos, int xDir) => {
			new FakeZeroBusterProj(
				pos, xDir, this, player.getNextActorNetId(), sendRpc: true
			);
		}, "busterX2");
		if (isAI) {
			mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.001f);
		}
		return mshoot;
	}

	public override void onDestroy() {
		base.onDestroy();
		exhaust?.destroySelf();
	}
}
