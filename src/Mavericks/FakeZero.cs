using System;
using System.Collections.Generic;
namespace MMXOnline;

public class FakeZero : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }
	float dashDist;
	float baseSpeed = 50;
	float accSpeed;
	int lastDirX;
	public Anim? exhaust;
	public const float topSpeed = 200;
	public float jumpXMomentum = 1;
	public float shootTime;

	// Ammo uses.
	public static int shootLv2Ammo = 4;
	public static int shootLv3Ammo = 5;
	public static int hadangekiAmmo = 4;

	public FakeZero(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(FakeZeroGroundPunchState), new(60) },
			{ typeof(FakeZeroShootAirState), new(30, true) },
			{ typeof(FakeZeroShootAir2State), new(30, true) },
			{ typeof(FakeZeroShootState), new(30, true) },
			{ typeof(FakeZeroShoot3State), new(30, true) }
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
		ammo = 32;
		maxAmmo = 32;
		grayAmmoLevel = 4;
		barIndexes = (60, 49);
		gameMavs = GameMavs.X2;
	}

	public override void preUpdate() {
		base.preUpdate();
		if (exhaust != null) {
			if (sprite.name.Contains("run")) {
				exhaust.zIndex = zIndex - 100;
				exhaust.visible = true;
				exhaust.xDir = xDir;
				exhaust.changePos(getFirstPOIOrDefault());
			} else {
				exhaust.visible = false;
			}
		}
	}

	public override float getAirSpeed() {
		return jumpXMomentum;
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (state.normalCtrl || state.attackCtrl || state.aiAttackCtrl) {
			rechargeAmmo(2);
		}
		shootTime -= Global.spf;
		if (lastDirX != xDir) {
			accSpeed = 0;
			dashDist = 0;
		}
		lastDirX = xDir;

		if (state is MRun || state is FakeZeroMeleeState) {
			dashDist += accSpeed * Global.spf;
			accSpeed += Global.spf * 500;
			if (accSpeed > topSpeed) accSpeed = topSpeed;
			/*
			if (dashDist > 250)
			{
				accSpeed = 0;
				dashDist = 0;
				changeState(new MIdle());
				return;
			}
			*/
		} else if (state is MJumpStart) {
			jumpXMomentum = 1 + 0.5f * (accSpeed / topSpeed);
		} else {
			accSpeed = 0;
		}

		if (!state.attackCtrl) {
			return;
		}
		if (grounded) {
			if (input.isHeld(Control.Shoot, player) && state is MRun) {
				changeState(new FakeZeroMeleeState());
			} else if (input.isHeld(Control.Shoot, player)) {
				changeState(new FakeZeroShootState(), false);
			} else if (input.isPressed(Control.Special1, player) && ammo >= 7 && shootTime <= 0) {
				changeState(new FakeZeroShoot3State());
			} else if (input.isPressed(Control.Dash, player)) {
				changeState(new FakeZeroGroundPunchState());
			} else if (input.isHeld(Control.Down, player) && state is not FakeZeroGuardState) {
				changeState(new FakeZeroGuardState());
			}
		} else {
			if (input.isPressed(Control.Shoot, player) && ammo >= 4) {
				changeState(new FakeZeroShootAir2State());
			} else if (input.isPressed(Control.Special1, player) && ammo >= 7) {
				changeState(new FakeZeroShootAir3State());
			}
		}
	}

	public override float getRunSpeed() {
		if (state is MRun || state is FakeZeroMeleeState) return baseSpeed + accSpeed;
		return 100;
	}

	public override string getMaverickPrefix() {
		return "fakezero";
	}

	public override MaverickState[] strikerStates() {
		return [
			new FakeZeroShootState(2),
			new FakeZeroShoot3State(),
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
			aiStates.Add(new FakeZeroShoot3State());
		}
		if (enemyDist <= 70) {
			aiStates.Add(new FakeZeroGroundPunchState());
		}
		else {
			aiStates.Add(new FakeZeroMeleeState(true));
		}
		return aiStates.ToArray();
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


public class FakeZeroMState : MaverickState {
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

public class FakeZeroBusterProj : Projectile {
	public FakeZeroBusterProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "buster1", netId, player
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 1;
		vel = new Point(250 * xDir, 0);
		projId = (int)ProjIds.FakeZeroBuster;
		reflectable = true;
		maxTime = 0.5f;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroBusterProj(
			args.pos, args.xDir, args.owner, args.netId, player: args.player
		);
	}
	public override void update() {
		base.update();
		if (MathF.Abs(vel.x) < 360 && reflectCount == 0) {
			vel.x += Global.spf * xDir * 900f;
			if (MathF.Abs(vel.x) >= 360) {
				vel.x = (float)xDir * 360;
			}
		}
	}
}

public class FakeZeroBuster2Proj : Projectile {
	public FakeZeroBuster2Proj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "fakezero_buster_proj", netId, player
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 2;
		vel = new Point(350 * xDir, 0);
		projId = (int)ProjIds.FakeZeroBuster2;
		reflectable = true;
		maxTime = 0.5f;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroBuster2Proj(
			args.pos, args.xDir, args.owner, args.netId, player: args.player
		);
	}
}

public class FakeZeroBusterProj3 : Projectile {
	public int type = 0;
	public FakeZeroBusterProj3(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fakezero_buster2_proj", netId, player
	) {
		weapon = FakeZero.getWeapon();
		damager.flinch = Global.halfFlinch;
		if (type == 0) {
			damager.damage = 2;
		} else if (type == 1) {
			damager.damage = 3;
		}
		vel = new Point(325 * xDir, 0);
		projId = (int)ProjIds.FakeZeroBuster3;
		maxTime = 0.75f;
		reflectable = true;
		this.type = type;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroBusterProj3(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (time > 0.55f) {
			damager.damage = 2;
		}
	}
}

public class FakeZeroSwordBeamProj : Projectile {
	public FakeZeroSwordBeamProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fakezero_sword_proj", netId, player
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		vel = new Point(325 * xDir, 0);
		projId = (int)ProjIds.FakeZeroSwordBeam;
		maxTime = 0.75f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroSwordBeamProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (time > 0.55f) {
			damager.flinch = 0;
			damager.damage = 2;
		}
	}
}

public class FakeZeroShootState : FakeZeroMState {
	private bool shotOnce;
	private bool shootReleased;
	private bool shootPressedAgain;
	public int busterType = 1;
	public int shootCount = 0;
	private int aiAttack;

	public FakeZeroShootState(int aiAttack = 0) : base("shoot", "") {
		this.aiAttack = aiAttack;
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
		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.sprite.frameSpeed = 0.6f;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		if (busterType == 0) {
			maverick.stateCooldowns[typeof(FakeZeroShootState)].cooldown = 0;
		}
	}
}

public class FakeZeroShootAirState : FakeZeroMState {
	public FakeZeroShootAirState() : base("shoot_air") {
	}

	public override void update() {
		base.update();

		airCode();
		Point? shootPos = maverick.getFirstPOI();

		if (shootPos != null) {
			if (!once) {
				once = true;
				maverick.playSound("busterX2", forcePlay: false, sendRpc: true);
				new FakeZeroBuster2Proj(
					shootPos.Value, maverick.xDir, maverick,
					player.getNextActorNetId(), sendRpc: true
				);
			}
		}

		if (maverick.isAnimOver() || maverick.grounded) {
			maverick.changeToIdleOrFall();
		}
	}
}

public class FakeZeroShootAir2State : FakeZeroMState {
	public FakeZeroShootAir2State() : base("shoot_air2") {
	}

	public override void update() {
		base.update();
		airCode();
		Point? shootPos = maverick.getFirstPOI();

		if (shootPos != null) {
			if (!once) {
				once = true;
				maverick.deductAmmo(FakeZero.shootLv2Ammo);
				maverick.playSound("buster2X2", forcePlay: false, sendRpc: true);
				new FakeZeroBuster2Proj(
					shootPos.Value, maverick.xDir, maverick,
					player.getNextActorNetId(), sendRpc: true
				);
			}
		}

		if (maverick.isAnimOver() || maverick.grounded) {
			maverick.changeToIdleOrFall();
		}
	}
	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		zero.shootTime = 1;
	}
}
public class FakeZeroShootAir3State : FakeZeroMState {
	public FakeZeroShootAir3State() : base("shoot_air2") {
	}

	public override void update() {
		base.update();

		airCode();
		Point? shootPos = maverick.getFirstPOI();

		if (shootPos != null) {
			if (!once) {
				once = true;
				maverick.deductAmmo(FakeZero.shootLv3Ammo);
				maverick.playSound("buster3X2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj3(
					shootPos.Value, maverick.xDir, 0, zero,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}

		if (maverick.isAnimOver() || maverick.grounded) {
			maverick.changeToIdleOrFall();
		}
	}
	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		zero.shootTime = 1;
	}
}

public class FakeZeroShoot3State : FakeZeroMState {
	int shootNum;
	int lastShootFrame;

	public FakeZeroShoot3State() : base("shoot2") {
	}

	public override void update() {
		base.update();

		Point? shootPos = maverick.getFirstPOI();

		if (maverick.frameIndex == 4 || maverick.frameIndex == 9) {
			maverick.turnToInput(input, player);
		}

		if (maverick.ammo < 8 && maverick.frameIndex == 4) {
			maverick.changeState(new MIdle());
			return;
		}

		if (shootPos != null && maverick.frameIndex != lastShootFrame) {
			if (shootNum == 0) {
				maverick.deductAmmo(FakeZero.shootLv3Ammo);
				maverick.playSound("buster3X2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj3(
					shootPos.Value, maverick.xDir, 0, zero,
					player, player.getNextActorNetId(), rpc: true
				);
			} else if (shootNum == 1) {
				maverick.deductAmmo(FakeZero.shootLv3Ammo);
				maverick.playSound("buster3X2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj3(
					shootPos.Value, maverick.xDir, 1, zero,
					player, player.getNextActorNetId(), rpc: true
				);
			} else if (shootNum == 2) {
				maverick.deductAmmo(FakeZero.hadangekiAmmo);
				maverick.playSound("buster4X2", forcePlay: false, sendRpc: true);
				new FakeZeroSwordBeamProj(
					shootPos.Value, maverick.xDir, zero,
					player, player.getNextActorNetId(), rpc: true
				);
			}
			shootNum++;
			lastShootFrame = maverick.frameIndex;
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		zero.shootTime = 1;
	}
}

public class FakeZeroMeleeProj : Projectile {
	public FakeZeroMeleeProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fakezero_run_sword", netId, player
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 30;
		projId = (int)ProjIds.FakeZeroMelee;
		setIndestructableProperties();
		visible = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroMeleeProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (deltaPos.magnitude > FakeZero.topSpeed * Global.spf) {
			damager.damage = 4;
			damager.flinch = Global.defFlinch;
		} else if (deltaPos.magnitude <= FakeZero.topSpeed * Global.spf && deltaPos.magnitude > 150 * Global.spf) {
			damager.damage = 3;
			damager.flinch = Global.halfFlinch;
		} else {
			damager.damage = 2;
			damager.flinch = 0;
		}
	}
}

public class FakeZeroMeleeState : FakeZeroMState {
	public FakeZeroMeleeProj? proj;
	private bool isAiAttack;
	Anim? projVisible;

	public FakeZeroMeleeState(bool isAiAttack = false) : base("run_attack") {
		this.isAiAttack = isAiAttack;
		enterSound = "saber3";
	}

	public override void update() {
		base.update();
		zero.turnToInput(input, player);
		proj?.turnToInput(input, player);
		projVisible?.turnToInput(input, player);
		proj?.changePos(zero.getFirstPOIOrDefault(1));
		projVisible?.changePos(zero.getFirstPOIOrDefault(1));

		Point move = new Point(input.getXDir(player), 0);
		if (isAiAttack) {
			move.x = zero.xDir;
		}
		if (move.magnitude > 0) {
			move.x *= zero.getRunSpeed();
			maverick.move(move);
		} else {
			maverick.changeToIdleOrFall();
			return;
		}
		if (isAiAttack && stateFrame >= 25) {
			maverick.changeToIdleOrFall();
			return;
		}
		groundCode();
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		projVisible = new Anim(
			maverick.getFirstPOIOrDefault(1), "fakezero_run_sword", maverick.xDir,
			player.getNextActorNetId(), false, sendRpc: true
		);
		proj = new FakeZeroMeleeProj(
			maverick.getFirstPOIOrDefault(1), maverick.xDir,
			zero, player, player.getNextActorNetId(), rpc: true);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		proj?.destroySelf();
		projVisible?.destroySelf();
	}
}

public class FakeZeroRockProj : Projectile {
	public FakeZeroRockProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fakezero_rock", netId, player
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 6;
		projId = (int)ProjIds.FakeZeroGroundPunch;
		maxTime = 1.25f;
		useGravity = true;
		vel = new Point(0, -500);

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroRockProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class FakeZeroGuardState : MaverickState {
	public FakeZeroGuardState() : base("guard") {
		aiAttackCtrl = true;
		attackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();

		if (!input.isHeld(Control.Down, player)) {
			maverick.changeToIdleOrFall();
		}
	}
}

public class FakeZeroGroundPunchState : FakeZeroMState {
	public FakeZeroGroundPunchState() : base("groundpunch") {
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
			player, player.getNextActorNetId(), rpc: true
		);

	}
}
