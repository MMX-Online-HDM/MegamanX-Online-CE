namespace MMXOnline;

public class SigmaSlashWeapon : Weapon {
	public static SigmaSlashWeapon netWeapon = new();

	public SigmaSlashWeapon() : base() {
		index = (int)WeaponIds.SigmaSlash;
		killFeedIndex = 9;
	}
}
public class SigmaSlashStateGround : CharState {
	bool fired;
	public SigmaSlashStateGround() : base("attack") {
		useDashJumpSpeed = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
		landSprite = "attack";
		airSprite = "attack_air";
	}
	public override void update() {
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			character.playSound("sigmaSaber", sendRpc: true);
			Point off = new Point(30, -20);
			new SigmaSlashProj(
				SigmaSlashWeapon.netWeapon, character.pos.addxy(off.x * character.xDir, off.y),
				character.xDir, player, player.getNextActorNetId(), 4, 26, rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		base.update();
	}
}
public class SigmaSlashStateAir : CharState {
	bool fired;
	public SigmaSlashStateAir() : base("attack_air") {
		useDashJumpSpeed = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
		landSprite = "attack";
		airSprite = "attack_air";
	}
	public override void update() {
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			character.playSound("sigmaSaber", sendRpc: true);
			Point off = new Point(20, -30);
			new SigmaSlashProj(
				SigmaSlashWeapon.netWeapon, character.pos.addxy(off.x * character.xDir, off.y),
				character.xDir, player, player.getNextActorNetId(), 3, 13, rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		base.update();
	}
}
public class SigmaSlashStateDash : CharState {
	bool fired;
	public SigmaSlashStateDash() : base("attack_dash") {
		useDashJumpSpeed = true;
		airMove = true;
		canJump = true;
		canStopJump = true;
		landSprite = "attack";
		airSprite = "attack_dash";
	}
	public override void update() {
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			character.playSound("sigmaSaber", sendRpc: true);
			Point off = new Point(30, -20);
			new SigmaSlashProj(
				SigmaSlashWeapon.netWeapon, character.pos.addxy(off.x * character.xDir, off.y),
				character.xDir, player, player.getNextActorNetId(), 4, 26, rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		base.update();
	}
}
/*
public class SigmaSlashState : CharState {
	CharState prevCharState;
	int attackFrame = 2;
	bool fired;
	public SigmaSlashState(CharState prevCharState) : base(prevCharState.attackSprite) {
		this.prevCharState = prevCharState;
		if (prevCharState is Dash || prevCharState is AirDash) {
			attackFrame = 1;
		}
		useDashJumpSpeed = true;
		airMove = true;
	}

	public override void update() {
		base.update();

		if (!character.grounded) {
			landSprite = "attack";
		}

		if (prevCharState is Dash) {
			if (character.frameIndex < attackFrame) {
				character.move(new Point(character.getDashSpeed() * character.xDir, 0));
			}
		}

		if (character.frameIndex >= attackFrame && !fired) {
			fired = true;
			character.playSound("sigmaSaber", sendRpc: true);

			Point off = new Point(30, -20);
			if (character.sprite.name == "sigma_attack_air") {
				off = new Point(20, -30);
			}

			float damage = character.grounded ? 4 : 3;
			int flinch = character.grounded ? Global.defFlinch : 13;
			new SigmaSlashProj(
				SigmaSlashWeapon.netWeapon, character.pos.addxy(off.x * character.xDir, off.y),
				character.xDir, player, player.getNextActorNetId(), damage: damage, flinch: flinch, rpc: true
			);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
*/


public class SigmaSlashProj : Projectile {
	public SigmaSlashProj(
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId,
		float damage = 6, int flinch = 26, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, damage, player, "sigma_proj_slash", flinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		reflectable = false;
		destroyOnHit = false;
		shouldShieldBlock = false;
		setIndestructableProperties();
		maxTime = 0.1f;
		projId = (int)ProjIds.SigmaSlash;
		isMelee = true;
		if (player.character != null) {
			owningActor = player.character;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		if (owner?.character != null) {
			incPos(owner.character.deltaPos);
		}
	}
}

public class SigmaBallWeapon : Weapon {
	public static SigmaBallWeapon netWeapon = new();

	public SigmaBallWeapon() : base() {
		index = (int)WeaponIds.SigmaBall;
		killFeedIndex = 103;
	}
}

public class SigmaBallProj : Projectile {
	public SigmaBallProj(
		Weapon weapon, Point pos, float byteAngle, Player player,
		ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 400, 2, player, "sigma_proj_ball",
		0, 0.2f, netProjId, player.ownedByLocalPlayer
	) {
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		vel.x = 400 * Helpers.cosb(byteAngle);
		vel.y = 400 * Helpers.sinb(byteAngle);
		projId = (int)ProjIds.SigmaBall;
		maxTime = 0.5f;
		destroyOnHit = true;
		if (rpc) {
			rpcCreateByteAngle(pos, player, netProjId, byteAngle);
		}
	}
}
public class SigmaBallShootEX : CharState {
	public CmdSigma? Sigma;
	public SigmaBallProj? SigmaBallsProjHead;
	public Anim? anim;
	public bool shoot;
	public float angle;
	public SigmaBallShootEX() : base("shoot") {
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		Sigma = character as CmdSigma;
	}
	public override void update() {
		character.turnToInput(player.input, player);
		if (character.frameIndex >= 1 && !shoot) {
			shoot = true;
			ammoReduction();
			shootProjectiles();
		} else if (character.sprite.frameIndex == 0) {
			shoot = false;
		}
		if (player.sigmaAmmo <= 0 || character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		//By disabling the code bellow, you can sort of make it MMX1 Accurate
		if (character.sprite.loopCount > 0 && !player.input.isHeld(Control.Special1, player)) {
			character.changeToIdleOrFall();
			return;
		}
		base.update();
	}
	public void shootProjectiles() {
		character.playSound("energyBall", sendRpc: true);
		Point shootPos = character.getFirstPOI() ?? character.getCenterPos();
		angleShoot();
		SigmaBallsProjHead = new SigmaBallProj(
			SigmaBallWeapon.netWeapon, shootPos, angle,
			player, player.getNextActorNetId(), rpc: true);
		anim = new Anim(shootPos, "sigma_proj_ball_muzzle", character.xDir,
			player.getNextActorNetId(), true, sendRpc: true);
	}
	public void ammoReduction() {
		player.sigmaAmmo -= 4;
		if (player.sigmaAmmo < 0) player.sigmaAmmo = 0;
		if (Sigma != null) {
			Sigma.sigmaAmmoRechargeCooldown = Sigma.sigmaHeadBeamTimeBeforeRecharge;
		}
	}
	public void angleShoot() {
		if (character.xDir == 1) {
			if (player.input.isHeld(Control.Down, player)) {
				angle = 42;
			} else if (player.input.isHeld(Control.Up, player)) {
				angle = 216;
			} else {
				angle = 8;
			}
		} else if (character.xDir == -1) {
			if (player.input.isHeld(Control.Down, player)) {
				angle = 94;
			} else if (player.input.isHeld(Control.Up, player)) {
				angle = 164;
			} else {
				angle = 120;
			}
		}
	}
}

public class SigmaBallShoot : CharState {
	bool shot;
	public CmdSigma sigma;

	public SigmaBallShoot(string transitionSprite = "") : base("shoot", "", "", transitionSprite) {
	}

	public override void update() {
		base.update();

		if (character.sprite.loopCount > 0 && !player.input.isHeld(Control.Special1, player)) {
			character.changeToIdleOrFall();
			return;
		}

		Point vel = new Point(0, 0.2f);
		bool lHeld = player.input.isHeld(Control.Left, player) && !player.isAI;
		bool rHeld = player.input.isHeld(Control.Right, player) && !player.isAI;
		bool uHeld = player.input.isHeld(Control.Up, player) || player.isAI;
		bool dHeld = player.input.isHeld(Control.Down, player) && !player.isAI;

		if (lHeld) {
			character.xDir = -1;
			vel.x = -2;
		} else if (rHeld) {
			character.xDir = 1;
			vel.x = 2;
		}

		if (uHeld) {
			vel.y = -1;
			if (vel.x == 0) vel.x = character.xDir * 0.5f;
		} else if (dHeld) {
			vel.y = 1;
			if (vel.x == 0) vel.x = character.xDir * 0.5f;
		} else vel.x = character.xDir;

		if (!uHeld && !dHeld && (lHeld || rHeld)) {
			vel.y = 0;
			vel.x = character.xDir;
		}

		if (character.sprite.frameIndex == 0) {
			shot = false;
		}
		if (character.sprite.frameIndex == 1 && !shot) {
			shot = true;
			Point poi = character.getFirstPOI() ?? character.getCenterPos();

			player.sigmaAmmo -= 4;
			if (player.sigmaAmmo < 0) player.sigmaAmmo = 0;
			sigma.sigmaAmmoRechargeCooldown = sigma.sigmaHeadBeamTimeBeforeRecharge;
			character.playSound("energyBall", sendRpc: true);
			/*
			new SigmaBallProj(
				SigmaBallWeapon.netWeapon, poi, character.xDir, player,
				player.getNextActorNetId(), rpc: true
			);
			new Anim(
				poi, "sigma_proj_ball_muzzle", character.xDir,
				player.getNextActorNetId(), true, sendRpc: true
			);
			*/
		}

		if (character.sprite.loopCount > 5 || player.sigmaAmmo <= 0) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		sigma = character as CmdSigma;
		character.vel = new Point();
	}
}

public class SigmaWallDashState : CharState {
	bool fired;
	int yDir;
	Point vel;
	bool fromGround;
	public CmdSigma sigma;

	public SigmaWallDashState(int yDir, bool fromGround) : base("wall_dash") {
		this.yDir = yDir;
		this.fromGround = fromGround;
		superArmor = true;
		useDashJumpSpeed = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		sigma = character as CmdSigma;
		float xSpeed = 350;
		if (!fromGround) {
			character.xDir *= -1;
		} else {
			character.unstickFromGround();
			character.incPos(new Point(0, -5));
		}
		character.isDashing = true;
		character.dashedInAir++;
		character.stopMoving();
		vel = new Point(character.xDir * xSpeed, yDir * 100);
		character.useGravity = false;
	}

	public override void onExit(CharState newState) {
		character.useGravity = true;
		sigma.leapSlashCooldown = CmdSigma.maxLeapSlashCooldown;
		base.onExit(newState);
	}

	public override void update() {
		base.update();

		var collideData = Global.level.checkTerrainCollisionOnce(character, vel.x * Global.spf, vel.y * Global.spf);
		if (collideData?.gameObject is Wall wall) {
			var collideData2 = Global.level.checkTerrainCollisionOnce(character, vel.x * Global.spf, 0);
			if (collideData2?.gameObject is Wall wall2 && wall2.collider.isClimbable) {
				character.changeState(new WallSlide(character.xDir, wall2.collider) { enterSound = "" }, true);
			} else {
				if (vel.y > 0) {
					character.changeToIdleOrFall();
				} else {
					character.isDashing = true;
					character.changeToIdleOrFall();
				}
			}
		}

		character.move(vel);

		if (stateTime > 0.7f) {
			character.changeState(new Fall(), true);
		}

		if (player.input.isPressed(Control.Shoot, player) &&
			!fired && sigma.saberCooldown == 0 && character.invulnTime == 0
		) {
			if (yDir == 0) {
				character.changeState(new SigmaSlashStateDash());
				return;
			}

			fired = true;
			sigma.saberCooldown = sigma.sigmaSaberMaxCooldown;

			character.playSound("sigmaSaber", sendRpc: true);
			character.changeSpriteFromName("wall_dash_attack", true);

			Point off = new Point(30, -20);
			new SigmaSlashProj(
				SigmaSlashWeapon.netWeapon, character.pos.addxy(off.x * character.xDir, off.y),
				character.xDir, player, player.getNextActorNetId(), damage: 4, rpc: true
			);
		}
	}
}
