using System;
namespace MMXOnline;

public class SigmaSlashWeapon : Weapon {
	public static SigmaSlashWeapon netWeapon = new();
	public SigmaSlashWeapon() : base() {
		index = (int)WeaponIds.SigmaSlash;
		killFeedIndex = 9;
	}
}

public class CmdSigmaState : CharState {
	public CmdSigma sigma = null!;

	public CmdSigmaState(
		string sprite, string shootSprite = "", string attackSprite = "",
		string transitionSprite = "", string transShootSprite = ""
	) : base(sprite, shootSprite, attackSprite, transitionSprite, transShootSprite
	) {
	}

	public override void onEnter(CharState oldState) {
		sigma = player.character as CmdSigma ?? throw new NullReferenceException();
		base.onEnter(oldState);
	}
}

public class SigmaSlashStateGround : CmdSigmaState {
	bool fired;

	public SigmaSlashStateGround() : base("attack") {
		airMove = true;
	}


	public override void update() {
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			character.playSound("sigmaSaber", sendRpc: true);
			Point off = new Point(30, -20);
			new SigmaSlashProj(
				character.pos.addxy(off.x * character.xDir, off.y), character.xDir,
				sigma, player, player.getNextActorNetId(), 4, 26, rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		base.update();
	}
}
public class SigmaSlashStateAir : CmdSigmaState {
	bool fired;
	public SigmaSlashStateAir() : base("attack_air") {
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = true;
		landSprite = "attack";
		airSprite = "attack_air";
	}

	public override void update() {
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			character.playSound("sigmaSaber", sendRpc: true);
			Point off = new Point(24, -22);
			new SigmaSlashProj(
				character.pos.addxy(off.x * character.xDir, off.y), character.xDir,
				sigma, player, player.getNextActorNetId(), 3, 13, rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		base.update();
	}
}

public class SigmaSlashStateDash : CmdSigmaState {
	bool fired;
	public SigmaSlashStateDash() : base("attack_dash") {
		airMove = true;
		canStopJump = true;
	}


	public override void update() {
		if (character.frameIndex >= 2 && !fired) {
			fired = true;
			character.playSound("sigmaSaber", sendRpc: true);
			Point off = new Point(26, -22);
			new SigmaSlashProj(
				character.pos.addxy(off.x * character.xDir, off.y), character.xDir,
				sigma, player, player.getNextActorNetId(), 4, 26, rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		base.update();
	}
}
/*
public class SigmaSlashState : CmdSigmaState {
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
		Point pos, int xDir, Actor owner, Player player, ushort? netId,
		float damage = 6, int flinch = 26, bool rpc = false
	) : base(
		pos, xDir, owner, "sigma_proj_slash", netId, player
	) {
		weapon = SigmaSlashWeapon.netWeapon;
		damager.damage = damage;
		damager.flinch = flinch;
		damager.hitCooldown = 30;
		reflectable = false;
		setIndestructableProperties();
		maxTime = 10f / 60f;
		projId = (int)ProjIds.SigmaSlash;
		isMelee = true;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		if (ownerPlayer?.character != null) {
			ownerActor = ownerPlayer.character;
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new SigmaSlashProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		if (frameIndex % 2 == 1) {
			alpha = 0.125f;
		} else {
			alpha = 1f;
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
		weaponBarBaseIndex = 50;
		weaponBarIndex = 39;
		killFeedIndex = 103;

		maxAmmo = 20;
		ammo = maxAmmo;
	}
}

public class SigmaBallProj : Projectile {
	public SigmaBallProj(
		Point pos, int xDir, float byteAngle, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "sigma_proj_ball", netId, player
	) {
		weapon = SigmaBallWeapon.netWeapon;
		damager.damage = 2;
		damager.hitCooldown = 12;
		byteAngle = byteAngle % 256;
		this.byteAngle = byteAngle;
		vel.x = 400 * Helpers.cosb(byteAngle);
		vel.y = 400 * Helpers.sinb(byteAngle);
		projId = (int)ProjIds.SigmaBall;
		maxTime = 0.5f;
		destroyOnHit = true;
		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new SigmaBallProj(
			args.pos, args.xDir, args.byteAngle, args.owner, args.player, args.netId
		);
	}
}

public class SigmaBallShoot : CmdSigmaState {
	public SigmaBallProj? SigmaBallsProjHead;
	public Anim? anim;
	public bool shoot;
	public float angle;

	public SigmaBallShoot() : base("shoot") {
	}

	public override void update() {
		base.update();
		character.turnToInput(player.input, player);
	
		if (character.frameIndex >= 1 && !shoot) {
			shoot = true;
			ammoReduction();
			shootProjectiles();
		} else if (character.sprite.frameIndex == 0) {
			shoot = false;
		}
	
		if (sigma.ballWeapon.ammo <= 0 || character.isAnimOver()) {
			character.changeToIdleOrFall();
		}

		// By disabling the code bellow, you can sort of make it MMX1 Accurate
		if (character.sprite.loopCount > 0 && !player.input.isHeld(Control.Special1, player)) {
			character.changeToIdleOrFall();
		}
	}

	public void shootProjectiles() {
		character.playSound("energyBall", sendRpc: true);
		Point shootPos = sigma.getFirstPOI() ?? sigma.getCenterPos();
		angleShoot();
		SigmaBallsProjHead = new SigmaBallProj
		(
			shootPos, 1, angle, sigma,
			player, player.getNextActorNetId(), rpc: true
		);
		anim = new Anim(shootPos, "sigma_proj_ball_muzzle", character.xDir,
			player.getNextActorNetId(), true, sendRpc: true);
	}

	public void ammoReduction() {
		sigma.ballWeapon.addAmmo(-4, player);
		sigma.sigmaAmmoRechargeCooldown = sigma.sigmaHeadBeamTimeBeforeRecharge;
	}

	public void angleShoot() {
		bool isRight = player.input.isHeld(Control.Right, player);
		bool isUp = player.input.isHeld(Control.Up, player);
		bool isDown = player.input.isHeld(Control.Down, player);
		bool isLeft = player.input.isHeld(Control.Left, player);
		if (character.xDir == 1) {
			if (isRight && isUp) {
				angle = 240;
			} else if (isDown && isRight) {
				angle = 22;
			} else if (isRight) {
				angle = 0;
			}  else if (isDown) {
				angle = 42;
			} else if (isUp) {
				angle = 216;
			} else {
				angle = 8;
			}
		} else if (character.xDir == -1) {
			if (isLeft && isUp) {
				angle = 145;
			} else if (isDown && isLeft) {
				angle = 102;
			} else if (isLeft) {
				angle = 128;
			}  else if (isDown) {
				angle = 82;
			} else if (isUp) {
				angle = 168;
			} else {
				angle = 120;
			}
		}
	}
}

public class SigmaWallDashState : CmdSigmaState {
	public bool fired;
	public int yDir;
	public Point vel;
	public bool fromGround;

	public SigmaWallDashState(int yDir, bool fromGround) : base("wall_dash") {
		this.yDir = yDir;
		this.fromGround = fromGround;
		superArmor = true;
		useDashJumpSpeed = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);

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

	public override void onExit(CharState? newState) {
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
			character.changeState(character.getFallState(), true);
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
				character.pos.addxy(off.x * character.xDir, off.y), character.xDir,
				sigma, player, player.getNextActorNetId(), damage: 4, rpc: true
			);
		}
	}
}
