using System;

namespace MMXOnline;

public class LaunchOctopus : Maverick {
	public static Weapon netWeapon = new Weapon(WeaponIds.LaunchOGeneric, 96);
	public LaunchOMissileWeapon missileWeapon = new();
	public LaunchODrainWeapon meleeWeapon = new();
	public LaunchOHomingTorpedoWeapon homingTorpedoWeapon = new();
	public bool lastFrameWasUnderwater;

	public LaunchOctopus(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(MShoot), new(60, true) },
			{ typeof(LaunchOShoot), new(20, true) },
			{ typeof(LaunchOHomingTorpedoState), new(90, true) },
			{ typeof(LaunchOWhirlpoolState), new(2 * 60) }
		};

		weapon = new Weapon(WeaponIds.LaunchOGeneric, 96);

		awardWeaponId = WeaponIds.HomingTorpedo;
		weakWeaponId = WeaponIds.RollingShield;
		weakMaverickWeaponId = WeaponIds.ArmoredArmadillo;

		netActorCreateId = NetActorCreateId.LaunchOctopus;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 32;
		maxAmmo = 32;
		grayAmmoLevel = 8;
		barIndexes = (71, 60);
		height = 46;
	}

	float timeBeforeRecharge;
	public override void update() {
		base.update();
		subtractTargetDistance = 50;
		if (state is not LaunchOShoot) {
			Helpers.decrementTime(ref timeBeforeRecharge);
			if (timeBeforeRecharge == 0) {
				rechargeAmmo(4.5f);
			}
		} else {
			timeBeforeRecharge = 1;
		}

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (shootPressed()) {
					if (ammo > 0) {
						changeState(new LaunchOShoot(grounded));
					}
				} else if (specialPressed()) {
					changeState(new LaunchOHomingTorpedoState());
				}
			} else if (state is MJump || state is MFall) {
				if (input.isPressed(Control.Shoot, player)) {
					if (ammo > 0) {
						changeState(new LaunchOShoot(grounded));
					}
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new LaunchOWhirlpoolState());
				}
			}

			if ((state is MJump || state is MFall) && !grounded) {
				if (isUnderwater()) {
					if (input.isHeld(Control.Jump, player) && vel.y > -106) {
						vel.y = -106;
					}
				} else {
					if (lastFrameWasUnderwater && input.isHeld(Control.Jump, player) && input.isHeld(Control.Up, player)) {
						vel.y = -425;
					}
				}
			}
		}

		lastFrameWasUnderwater = isUnderwater();
	}

	public override string getMaverickPrefix() {
		return "launcho";
	}

	public override MaverickState[] strikerStates() {
		return [
			new LaunchOShoot(grounded),
			new LaunchOHomingTorpedoState(),
			new LaunchOWhirlpoolState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		if (enemyDist <= 70) {
			return [
				new LaunchOShoot(grounded),
				new LaunchOHomingTorpedoState(),
			];
		}
		return [
			new LaunchOShoot(grounded),
			new LaunchOHomingTorpedoState(),
			new LaunchOWhirlpoolState(),
		];
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		OctoSpin,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"launcho_spin" => MeleeIds.OctoSpin,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.OctoSpin => new GenericMeleeProj(
				meleeWeapon, pos, ProjIds.LaunchODrain, player,
				0, 0, 0, addToLevel: addToLevel
			),
			_ => null
		};
	}
}

#region weapons
public class LaunchOMissileWeapon : Weapon {
	public static LaunchOMissileWeapon netWeapon = new();
	public LaunchOMissileWeapon() {
		index = (int)WeaponIds.LaunchOMissile;
		killFeedIndex = 96;
	}
}

public class LaunchOWhirlpoolWeapon : Weapon {
	public static LaunchOWhirlpoolWeapon netWeapon = new();
	public LaunchOWhirlpoolWeapon() {
		index = (int)WeaponIds.LaunchOWhirlpool;
		killFeedIndex = 96;
	}
}

public class LaunchODrainWeapon : Weapon {
	public static LaunchODrainWeapon netWeapon = new();
	public LaunchODrainWeapon() {
		index = (int)WeaponIds.LaunchOMelee;
		killFeedIndex = 96;
	}
}

public class LaunchOHomingTorpedoWeapon : Weapon {
	public static LaunchOHomingTorpedoWeapon netWeapon = new();
	public LaunchOHomingTorpedoWeapon() {
		index = (int)WeaponIds.LaunchOHomingTorpedo;
		killFeedIndex = 96;
	}
}
#endregion

#region projectiles
public class LaunchOMissile : Projectile, IDamagable {
	public float smokeTime = 0;
	public LaunchOMissile(
		Point pos, int xDir, Actor owner, Player player, int unitVel, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "launcho_proj_missile", netId, player
	) {
		weapon = LaunchOMissileWeapon.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 9;
		vel = new Point(100 * xDir, 0);
		projId = (int)ProjIds.LaunchOMissle;
		maxTime = 0.75f;
		fadeOnAutoDestroy = true;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		vel.y = 100 * (unitVel switch {
			0 => -0.2f,
			1 => -0.05f,
			2 => 0.25f,
			_ => 0
		});
		reflectableFBurner = true;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)unitVel);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new LaunchOMissile(
			args.pos, args.xDir, args.owner, args.player, args.extraData[0], args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		if (MathF.Abs(vel.x) < 300) {
			vel.x += Global.spf * 300 * xDir;
		}

		smokeTime += Global.spf;
		if (smokeTime > 0.2) {
			smokeTime = 0;
			new Anim(pos, "torpedo_smoke", 1, null, true);
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return owner.alliance != damagerAlliance;
	}
	public bool canBeHealed(int healerAlliance) {
		return false;
	}
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}
	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}
	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}
	public bool isPlayableDamagable() {
		return false;
	}
}

public class LaunchOWhirlpoolProj : Projectile {
	public LaunchOWhirlpoolProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "launcho_whirlpool", netId, player
	) {
		weapon = LaunchOWhirlpoolWeapon.netWeapon;
		damager.hitCooldown = 15;
		vel = new Point(1 * xDir, 0);
		projId = (int)ProjIds.LaunchOWhirlpool;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		destroyOnHit = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new LaunchOWhirlpoolProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
	}

	/*
	public override bool shouldDealDamage(IDamagable damagable)
	{
		if (damagable is Actor actor && MathF.Abs(pos.x - actor.pos.x) > 40)
		{
			return false;
		}
		return true;
	}
	*/

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!damagable.isPlayableDamagable()) { return; }

		if (damagable is Character chr) {
			float modifier = 1;
			if (chr.isUnderwater()) modifier = 2;
			if (chr.isPushImmune()) return;
			float xMoveVel = MathF.Sign(pos.x - chr.pos.x);
			chr.move(new Point(xMoveVel * 100 * modifier, 0));
		}
		if (damagable is Actor actor) {
			float modifier = 1;
			if (actor.isUnderwater()) modifier = 2;
			float xMoveVel = MathF.Sign(pos.x - actor.pos.x);
			actor.move(new Point(xMoveVel * 100 * modifier, 0));
		}
	}
}
public class TorpedoProjChargedOcto : Projectile, IDamagable {
	public Actor? target;
	public float smokeTime = 0;
	public float maxSpeed = 150;
	public TorpedoProjChargedOcto(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, float? angle = null, bool rpc = false
	) : base(
		pos, xDir, owner, "launcho_proj_ht", netId, player	
	) {
		weapon = LaunchOctopus.netWeapon;
		damager.damage = 1;
		damager.flinch = Global.halfFlinch;
		vel = new Point(150 * xDir, 0);
		fadeSprite = "explosion";
		fadeSound = "explosion";
		maxTime = 2f;
		projId = (int)ProjIds.LaunchOTorpedo;
		fadeOnAutoDestroy = true;
		reflectableFBurner = true;
		customAngleRendering = true;
		this.angle = this.xDir == -1 ? 180 : 0;
		if (angle != null) {
			this.angle = angle.Value + (this.xDir == -1 ? 180 : 0);
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TorpedoProjChargedOcto(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
	bool homing = true;
	public void reflect(float reflectAngle) {
		angle = reflectAngle;
		target = null;
	}
	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}
	public override void update() {
		base.update();
		if (ownedByLocalPlayer && homing) {
			if (target != null) {
				if (!Global.level.gameObjects.Contains(target)) {
					target = null;
				}
			}
			if (target != null) {
				if (time < 3f) {
					var dTo = pos.directionTo(target.getCenterPos()).normalize();
					var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
					destAngle = Helpers.to360(destAngle);
					angle = Helpers.lerpAngle(angle, destAngle, Global.spf * 3);
				}
			}
			if (time >= 0.15) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, true, aMaxDist: Global.screenW * 0.75f);
			} else if (time < 0.15) {
				//this.vel.x += this.xDir * Global.spf * 300;
			}
			vel.x = Helpers.cosd(angle) * maxSpeed;
			vel.y = Helpers.sind(angle) * maxSpeed;
		}
		smokeTime += Global.spf;
		if (smokeTime > 0.2) {
			smokeTime = 0;
			if (homing) new Anim(pos, "torpedo_smoke", 1, null, true);
		}
	}
	public override void renderFromAngle(float x, float y) {
		var angle = this.angle;
		var xDir = 1;
		var yDir = 1;
		var frameIndex = 0;
		float normAngle = 0;
		if (angle < 90) {
			xDir = 1;
			yDir = -1;
			normAngle = angle;
		}
		if (angle >= 90 && angle < 180) {
			xDir = -1;
			yDir = -1;
			normAngle = 180 - angle;
		} else if (angle >= 180 && angle < 270) {
			xDir = -1;
			yDir = 1;
			normAngle = angle - 180;
		} else if (angle >= 270 && angle < 360) {
			xDir = 1;
			yDir = 1;
			normAngle = 360 - angle;
		}

		if (normAngle < 18) frameIndex = 0;
		else if (normAngle >= 18 && normAngle < 36) frameIndex = 1;
		else if (normAngle >= 36 && normAngle < 54) frameIndex = 2;
		else if (normAngle >= 54 && normAngle < 72) frameIndex = 3;
		else if (normAngle >= 72 && normAngle < 90) frameIndex = 4;

		sprite.draw(frameIndex, pos.x + x, pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex, actor: this);
	}
	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damager.owner.alliance != damagerAlliance;
	}
	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}
	public bool canBeHealed(int healerAlliance) {
		return false;
	}
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}
	public bool isPlayableDamagable() {
		return false;
	}
}
#endregion

#region states
public class OctopusMState : MaverickState {
	public LaunchOctopus LauncherOctopuld = null!;
	public OctopusMState(
		string sprite, string transitionSprite = ""
	) : base(
		sprite, transitionSprite
	) {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		LauncherOctopuld = maverick as LaunchOctopus ?? throw new NullReferenceException();
	}
}
public class LaunchOShoot : OctopusMState {
	int shootState;
	bool isGrounded;
	float afterShootTime;
	public LaunchOShoot(bool isGrounded) : base(isGrounded ? "shoot" : "air_shoot") {
		this.isGrounded = isGrounded;
		airMove = true;
		canJump = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (LauncherOctopuld == null) return;

		if (isGrounded && !maverick.grounded) {
			sprite = "air_shoot";
			maverick.changeSpriteFromName(sprite, true);
			isGrounded = false;
		} else if (!isGrounded && maverick.grounded) {
			maverick.changeState(new MIdle());
			return;
		}

		Point? shootPos = LauncherOctopuld.getFirstPOI();
		int xDir = LauncherOctopuld.xDir;
		if (shootState == 0 && shootPos != null) {
			shootState = 1;
			maverick.playSound("torpedo", sendRpc: true);

			if (maverick.ammo >= 1) new LaunchOMissile(
				shootPos.Value.addxy(0, -3), xDir, LauncherOctopuld,
				player, 0, player.getNextActorNetId(), rpc: true
			);
			if (maverick.ammo >= 2) new LaunchOMissile(
				shootPos.Value.addxy(0, 0), xDir, LauncherOctopuld,
				player, 1, player.getNextActorNetId(), rpc: true
			);
			if (maverick.ammo >= 3) new LaunchOMissile(
				shootPos.Value.addxy(0, 5), xDir, LauncherOctopuld,
				player, 2, player.getNextActorNetId(), rpc: true
			);

			maverick.ammo -= 3;
			if (maverick.ammo < 0) maverick.ammo = 0;
		}

		if (shootState == 1 && (isAI || input.isPressed(Control.Shoot, player))) {
			maverick.frameSpeed = 0;
			shootState = 2;
		}

		if (shootState == 2 && shootPos != null) {
			afterShootTime += Global.spf;
			if (afterShootTime > 0.325f) {
				shootState = 3;
				maverick.frameSpeed = 1;
				sprite += "2";
				maverick.changeSpriteFromName(sprite, true);
				maverick.playSound("torpedo", sendRpc: true);
				if (maverick.ammo >= 1) new LaunchOMissile(
					shootPos.Value.addxy(0, -3), xDir, LauncherOctopuld,
					player, 0, player.getNextActorNetId(), rpc: true
				);
				if (maverick.ammo >= 2) new LaunchOMissile(
					shootPos.Value.addxy(0, 0), xDir, LauncherOctopuld,
					player, 1, player.getNextActorNetId(), rpc: true
				);
				if (maverick.ammo >= 3) new LaunchOMissile(
					shootPos.Value.addxy(0, 5), xDir, LauncherOctopuld,
					player, 2, player.getNextActorNetId(), rpc: true
				);
				maverick.ammo -= 3;
				if (maverick.ammo < 0) maverick.ammo = 0;
			}
		}

		if (maverick.ammo == 0 || LauncherOctopuld.isAnimOver()) {
			if (maverick.grounded) LauncherOctopuld.changeState(new MIdle());
			else LauncherOctopuld.changeState(new MFall());
		}
	}
}

public class LaunchOHomingTorpedoState : OctopusMState {
	public bool shootOnce;
	public LaunchOHomingTorpedoState() : base("ht") {
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
	}

	public override void update() {
		base.update();
		if (LauncherOctopuld == null) return;

		if (maverick.frameIndex == 3 && !shootOnce) {
			shootOnce = true;
			maverick.playSound("torpedo", sendRpc: true);
			var pois = maverick.currentFrame.POIs;
			new TorpedoProjChargedOcto(
				LauncherOctopuld.pos.add(pois[0]), 1, LauncherOctopuld, 
				player, player.getNextActorNetId(), 0, rpc: true
			);
			new TorpedoProjChargedOcto(
				LauncherOctopuld.pos.add(pois[1]), 1, LauncherOctopuld, 
				player, player.getNextActorNetId(), 0, rpc: true
			);
			new TorpedoProjChargedOcto(
				LauncherOctopuld.pos.add(pois[2]), 1, LauncherOctopuld,
				player, player.getNextActorNetId(), 180, rpc: true
			);
			new TorpedoProjChargedOcto(
				LauncherOctopuld.pos.add(pois[3]), 1, LauncherOctopuld,
				player, player.getNextActorNetId(), 180, rpc: true
			);
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
}

public class LaunchOWhirlpoolState : OctopusMState {
	public bool shootOnce;
	LaunchOWhirlpoolProj? whirlpool;
	float whirlpoolSoundTime;
	int initYDir = 1;
	public LaunchOWhirlpoolState() : base("spin") {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		maverick.useGravity = false;
		whirlpool = new LaunchOWhirlpoolProj(
			maverick.pos.addxy(0, isAI ? -100 : 25), 1,
			LauncherOctopuld ,player, player.getNextActorNetId(), rpc: true
		);
		maverick.playSound("launchoWhirlpool", sendRpc: true);
	}

	public override void update() {
		base.update();
		if (LauncherOctopuld == null) return;
		if (!maverick.tryMove(new Point(0, 100 * initYDir), out _)) {
			if (initYDir == 1) {
				maverick.unstickFromGround();
			}
			initYDir *= -1;
		}

		whirlpoolSoundTime += Global.spf;
		if (whirlpoolSoundTime > 0.5f) {
			whirlpoolSoundTime = 0;
			maverick.playSound("launchoWhirlpool", sendRpc: true);
		}

		if (stateTime > 2f) {
			maverick.changeState(new MFall());
		}
	}

	public override bool trySetGrabVictim(Character grabbed) {
		maverick.changeState(new LaunchODrainState(grabbed), true);
		return true;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		whirlpool?.destroySelf();
		maverick.useGravity = true;
	}
}

public class LaunchODrainState : OctopusMState {
	Character victim;
	float soundTime = 1;
	float leechTime = 0.5f;
	public bool victimWasGrabbedSpriteOnce;
	float timeWaiting;
	public LaunchODrainState(Character grabbedChar) : base("drain") {
		this.victim = grabbedChar;
	}

	public override void update() {
		base.update();
		if (player == null) return;
		leechTime += Global.spf;

		if (victimWasGrabbedSpriteOnce && !victim.sprite.name.EndsWith("_grabbed")) {
			maverick.changeState(new MFall(), true);
			return;
		}

		if (victim.sprite.name.EndsWith("_grabbed") || victim.sprite.name.EndsWith("_die")) {
			victimWasGrabbedSpriteOnce = true;
		}
		if (!victimWasGrabbedSpriteOnce) {
			timeWaiting += Global.spf;
			if (timeWaiting > 1) {
				victimWasGrabbedSpriteOnce = true;
			}
			if (maverick.isDefenderFavored()) {
				if (leechTime > 0.5f) {
					leechTime = 0;
					maverick.addHealth(2, true);
				}
				return;
			}
		}

		if (leechTime > 0.5f) {
			leechTime = 0;
			maverick.addHealth(2, true);
			var damager = new Damager(player, 2, 0, 0);
			damager.applyDamage(victim, false, new LaunchODrainWeapon(), maverick, (int)ProjIds.LaunchODrain);
		}

		soundTime += Global.spf;
		if (soundTime > 1f) {
			soundTime = 0;
			maverick.playSound("launchoDrain", sendRpc: true);
		}

		if (stateTime > 4f) {
			maverick.changeState(new MFall());
		}
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		maverick.useGravity = false;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
		victim?.releaseGrab(maverick);
		var whirlpoolCooldown = maverick.stateCooldowns[typeof(LaunchOWhirlpoolState)];
		whirlpoolCooldown.cooldown = whirlpoolCooldown.maxCooldown;
	}
}

public class WhirlpoolGrabbed : GenericGrabbedState {
	public const float maxGrabTime = 4;
	public WhirlpoolGrabbed(LaunchOctopus grabber) : base(grabber, maxGrabTime, "_drain") {
	}
}

#endregion
