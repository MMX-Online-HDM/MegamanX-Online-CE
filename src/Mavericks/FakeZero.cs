using System;
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

	public FakeZero(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
		base(player, pos, destPos, xDir, netId, ownedByLocalPlayer) {
		stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, false, 0));
		stateCooldowns.Add(typeof(FakeZeroGroundPunchState), new MaverickStateCooldown(false, false, 1.5f));
		stateCooldowns.Add(typeof(FakeZeroShootAirState), new MaverickStateCooldown(false, true, 0.5f));
		stateCooldowns.Add(typeof(FakeZeroShootAir2State), new MaverickStateCooldown(false, true, 0.5f));

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
		);

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

		if (state is not FakeZeroShoot2State or FakeZeroShoot3State
		 	or FakeZeroShootAir2State or FakeZeroShootAir3State) {
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

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isHeld(Control.Shoot, player) && state is MRun) {
					changeState(new FakeZeroMeleeState());
				} else if (input.isPressed(Control.Shoot, player)) {
					changeState(getShootState(false), true);
				} else if (input.isPressed(Control.Up, player) && ammo >= 4 && shootTime <= 0) {
					changeState(new FakeZeroShoot2State(), false);
				} else if (input.isPressed(Control.Special1, player) && ammo >= 7 && shootTime <= 0) {
					changeState(new FakeZeroShoot3State());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new FakeZeroGroundPunchState());
				} else if (input.isHeld(Control.Down, player)) {
					changeState(new FakeZeroGuardState());
				}
			} else if (state is MJump || state is MFall || state is MWallKick) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new FakeZeroShootAirState(), true);
				} else if (input.isPressed(Control.Up, player) && ammo >= 4 && shootTime <= 0) {
					changeState(new FakeZeroShootAir2State());
				} else if (input.isPressed(Control.Special1, player) && ammo >= 7 && shootTime <= 0) {
					changeState(new FakeZeroShootAir3State());
				}
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

	public override MaverickState[] aiAttackStates() {
		var attacks = new MaverickState[]
		{
				getShootState(true),
				new FakeZeroShoot2State(),
				new FakeZeroShoot3State(),
				new FakeZeroGroundPunchState(),
		};
		return attacks;
	}

	public override MaverickState getRandomAttackState() {
		return aiAttackStates().GetRandomItem();
	}

	public MaverickState getShootState(bool isAI) {
		var mshoot = new MShoot((Point pos, int xDir) => {
			new FakeZeroBusterProj(
				pos, xDir, this, player, player.getNextActorNetId(), rpc: true
			);
		}, "busterX2");
		if (isAI) {
			mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.15f);
		}
		return mshoot;
	}

	public override void onDestroy() {
		base.onDestroy();
		exhaust?.destroySelf();
	}
}
public class FakeZeroBusterProj : Projectile {
	public FakeZeroBusterProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "buster1", netId, player
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 1;
		vel = new Point(250 * xDir, 0);
		projId = (int)ProjIds.FakeZeroBuster;
		reflectable = true;
		maxTime = 0.5f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroBusterProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
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

public class FakeZeroBusterProj2 : Projectile {
	public FakeZeroBusterProj2(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fakezero_buster_proj", netId, player	
	) {
		weapon = FakeZero.getWeapon();
		damager.damage = 2;
		vel = new Point(350 * xDir, 0);
		projId = (int)ProjIds.FakeZeroBuster2;
		reflectable = true;
		maxTime = 0.5f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FakeZeroBusterProj2(
			args.pos, args.xDir, args.owner, args.player, args.netId
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

public class FakeZeroShoot2State : MaverickState {
	public FakeZeroShoot2State() : base("shoot") {
		attackCtrl = true;
	}
	public FakeZero darkZero = null!;
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		darkZero = maverick as FakeZero ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		maverick.turnToInput(input,player);
		Point? shootPos = maverick.getFirstPOI();
		if (shootPos != null) {
			if (!once) {
				once = true;
				maverick.deductAmmo(4);
				maverick.playSound("buster2X2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj2(
					shootPos.Value, maverick.xDir, darkZero,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		darkZero.shootTime = 1;
	}
}
public class FakeZeroShootAirState : MaverickState {
	public FakeZero darkZero = null!;
	public FakeZeroShootAirState() : base("shoot_air") {
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		darkZero = maverick as FakeZero ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();

		airCode();
		Point? shootPos = maverick.getFirstPOI();

		if (shootPos != null) {
			if (!once) {
				once = true;
				maverick.playSound("busterX2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj(
					shootPos.Value, maverick.xDir, darkZero,
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}

		if (maverick.isAnimOver() || maverick.grounded) {
			maverick.changeToIdleOrFall();
		}
	}
}

public class FakeZeroShootAir2State : MaverickState {
	public FakeZeroShootAir2State() : base("shoot_air2") {
	}
	public FakeZero darkZero = null!;
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		darkZero = maverick as FakeZero ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		airCode();
		Point? shootPos = maverick.getFirstPOI();

		if (shootPos != null) {
			if (!once) {
				once = true;
				maverick.deductAmmo(4);
				maverick.playSound("buster2X2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj2(
					shootPos.Value, maverick.xDir, darkZero,
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
		darkZero.shootTime = 1;
	}
}
public class FakeZeroShootAir3State : MaverickState {
	public FakeZeroShootAir3State() : base("shoot_air2") {
	}
	public FakeZero darkZero = null!;
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		darkZero = maverick as FakeZero ?? throw new NullReferenceException();

	}

	public override void update() {
		base.update();

		airCode();
		Point? shootPos = maverick.getFirstPOI();

		if (shootPos != null) {
			if (!once) {
				once = true;
				maverick.deductAmmo(5);
				maverick.playSound("buster3X2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj3(
					shootPos.Value, maverick.xDir, 0, darkZero,
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
		darkZero.shootTime = 1;
	}
}

public class FakeZeroShoot3State : MaverickState {
	int shootNum;
	int lastShootFrame;
	public FakeZeroShoot3State() : base("shoot2") {
	}
	public FakeZero darkZero = null!;
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		darkZero = maverick as FakeZero ?? throw new NullReferenceException();
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
				maverick.deductAmmo(5);
				maverick.playSound("buster3X2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj3(
					shootPos.Value, maverick.xDir, 0, darkZero,
					player, player.getNextActorNetId(), rpc: true
				);	
			} else if (shootNum == 1) {
				maverick.deductAmmo(7);
				maverick.playSound("buster3X2", forcePlay: false, sendRpc: true);
				new FakeZeroBusterProj3(
					shootPos.Value, maverick.xDir, 1, darkZero,
					player, player.getNextActorNetId(), rpc: true
				);	
			} else if (shootNum == 2) {
				maverick.deductAmmo(4);
				maverick.playSound("buster4X2", forcePlay: false, sendRpc: true);
				new FakeZeroSwordBeamProj(
					shootPos.Value, maverick.xDir, darkZero,
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
		darkZero.shootTime = 1;
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

public class FakeZeroMeleeState : MaverickState {
	public FakeZeroMeleeProj? proj;
	Anim? ProjVisible;
	public FakeZero darkZero = null!;
	public FakeZeroMeleeState() : base("run_attack") {
		enterSound = "saber3";
		aiAttackCtrl = true;
	}

	public override void update() {
		base.update();
		darkZero.turnToInput(input, player);
		proj?.turnToInput(input, player);
		ProjVisible?.turnToInput(input, player);
		proj?.changePos(darkZero.getFirstPOIOrDefault(1));
		ProjVisible?.changePos(darkZero.getFirstPOIOrDefault(1));

		var move = new Point(0, 0);
		if (input.isHeld(Control.Left, player)) {	
			move.x = -maverick.getRunSpeed();
		} else if (input.isHeld(Control.Right, player)) {	
			move.x = maverick.getRunSpeed();
		}
		if (move.magnitude > 0) {
			maverick.move(move);
		} else {
			maverick.changeState(new MIdle());
		}
		groundCode();
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		darkZero = maverick as FakeZero ?? throw new NullReferenceException();
		ProjVisible = new Anim(
			maverick.getFirstPOIOrDefault(1), "fakezero_run_sword", maverick.xDir,
			player.getNextActorNetId(), false, sendRpc: true
		);

		proj = new FakeZeroMeleeProj(
			maverick.getFirstPOIOrDefault(1), maverick.xDir,
			darkZero, player, player.getNextActorNetId(), rpc: true);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		proj?.destroySelf();
		ProjVisible?.destroySelf();
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
		canBeCanceled = false;
	}

	public override void update() {
		base.update();

		if (!input.isHeld(Control.Down, player)) {
			maverick.changeToIdleOrFall();
		}
	}
}

public class FakeZeroGroundPunchState : MaverickState {
	public FakeZero darkZero = null!;
	public FakeZeroGroundPunchState() : base("groundpunch") {
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		darkZero = maverick as FakeZero ?? throw new NullReferenceException();
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
			maverick.pos.addxy(dist, 0), maverick.xDir, darkZero,
			player, player.getNextActorNetId(), rpc: true
		);

	}
}
