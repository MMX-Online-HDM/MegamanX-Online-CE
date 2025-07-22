using System;
namespace MMXOnline;

public class FlameStag : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.FStagGeneric, 144); }
	public static Weapon staticUppercutWeapon = new Weapon(WeaponIds.FStagGeneric, 144);
	public Weapon uppercutWeapon;

	public Sprite antler;
	public Sprite antlerDown;
	public Sprite antlerSide;

	public FlameStag(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
		base(player, pos, destPos, xDir, netId, ownedByLocalPlayer) {
		weapon = getWeapon();
		uppercutWeapon = new Weapon(WeaponIds.FStagGeneric, 144, new Damager(player, 0, 0, 0));

		canClimbWall = true;
		width = 20;

		antler = new Sprite("fstag_antler");
		antlerDown = new Sprite("fstag_antler_down");
		antlerSide = new Sprite("fstag_antler_side");
		spriteFrameToSounds["fstag_run/2"] = "run";
		spriteFrameToSounds["fstag_run/6"] = "run";

		//stateCooldowns.Add(typeof(FStagShoot), new MaverickStateCooldown(false, false, 0.25f));
		stateCooldowns.Add(typeof(FStagDashChargeState), new MaverickStateCooldown(true, false, 0.75f));
		stateCooldowns.Add(typeof(FStagDashState), new MaverickStateCooldown(true, false, 0.75f));

		awardWeaponId = WeaponIds.SpeedBurner;
		weakWeaponId = WeaponIds.BubbleSplash;
		weakMaverickWeaponId = WeaponIds.BubbleCrab;

		netActorCreateId = NetActorCreateId.FlameStag;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}
		gameMavs = GameMavs.X2;
	}

	public override void update() {
		base.update();

		if (isUnderwater()) {
			antler.visible = false;
			antlerDown.visible = false;
			antlerSide.visible = false;
		} else {
			antler.visible = true;
			antlerDown.visible = true;
			antlerSide.visible = true;
		}

		antler.update();
		antlerDown.update();
		antlerSide.update();

		if (!ownedByLocalPlayer) return;

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new FStagShoot(false));
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(new FStagGrabState(false));
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new FStagDashChargeState());
				}
			} else if (state is MJump || state is MFall) {
				var inputDir = input.getInputDir(player);
				if (inputDir.x != 0) {
					if (!sprite.name.EndsWith("wall_dash")) changeSpriteFromName("wall_dash", true);
				} else {
					if (sprite.name.EndsWith("wall_dash")) changeSpriteFromName("fall", true);
				}
			} else if (state is MWallKick mwk) {
				var inputDir = input.getInputDir(player);
				if (inputDir.x != 0 && inputDir.x == mwk.kickDir) {
					if (!sprite.name.EndsWith("wall_dash")) changeSpriteFromName("wall_dash", true);
				} else {
					if (sprite.name.EndsWith("wall_dash")) changeSpriteFromName("fall", true);
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "fstag";
	}

	public override MaverickState[] strikerStates() {
		return [
			new FStagShoot(false),
			new FStagGrabState(true),
			new FStagDashChargeState(),
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		if (enemyDist <= 80) {
			return [
				new FStagShoot(false),
				new FStagGrabState(true),
				new FStagDashChargeState(),
			];
		}
		return [
			new FStagShoot(false),
			new FStagDashChargeState(),
		];
	}

	public Point? getAntlerPOI(out string tag) {
		tag = "";
		if (sprite.getCurrentFrame().POIs.Length > 0) {
			for (int i = 0; i < sprite.getCurrentFrame().POITags.Length; i++) {
				tag = sprite.getCurrentFrame().POITags[i];
				if (tag == "antler" || tag == "antler_side" || tag == "antler_down") {
					return getFirstPOIOffsetOnly(i);
				}
			}
		}
		return null;
	}

	public override float getRunSpeed() {
		return 200;
	}

	public override float getDashSpeed() {
		return 1.5f;
	}

	public Point? getAttackPOI() {
		if (sprite.getCurrentFrame().POIs.Length > 0) {
			int poiIndex = sprite.getCurrentFrame().POITags.FindIndex(tag => tag != "antler" && tag != "antler_side" && tag != "antler_down");
			if (poiIndex >= 0) return getFirstPOIOrDefault(poiIndex);
		}
		return null;
	}

	public override void render(float x, float y) {
		base.render(x, y);
		var antlerPOI = getAntlerPOI(out string tag);
		if (antlerPOI != null) {
			Sprite sprite = antler;
			if (tag == "antler_side") sprite = antlerSide;
			if (tag == "antler_down") sprite = antlerDown;
			sprite.draw(sprite.frameIndex, pos.x + (xDir * antlerPOI.Value.x), pos.y + antlerPOI.Value.y, xDir, 1, null, 1, 1, 1, zIndex + 100, useFrameOffsets: true);
		}
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		DashGrab,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"fstag_dash_grab" => MeleeIds.DashGrab,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.DashGrab => new GenericMeleeProj(
				weapon, pos, ProjIds.FStagUppercut, player,
				0, 0, 0, addToLevel: addToLevel
			),
			_ => null
		};
	}

}
public class FStagFireballProj : Projectile {
	public bool launched;
	public int type;
	public FStagFireballProj(
		Point pos, int xDir, int type, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fstag_fireball_proj", netId, player
	) {
		weapon = FlameStag.getWeapon();
		damager.damage = 2;
		damager.hitCooldown = 1;
		this.type = type;
		projId = (int)ProjIds.FStagFireball;
		maxTime = 0.75f;
		if (type == 0) vel = new Point(xDir * 350, 50);
		if (type == 1) vel = new Point(xDir * 350, -50);
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FStagFireballProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
		}
	}

}

public class FStagShoot : MaverickState {
	bool shotOnce;
	FStagFireballProj? fireball;
	bool isSecond;
	public FlameStag FlameStagger = null!;

	public FStagShoot(bool isSecond) : base(isSecond ? "punch2" : "punch") {
		this.isSecond = isSecond;
	}
	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		FlameStagger = maverick as FlameStag ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();

		Point shootPos = FlameStagger.getCenterPos();
			if (maverick.frameIndex >= 7 && !shotOnce && !isSecond) {
				shotOnce = true;
				FlameStagger.playSound("fstagShoot", sendRpc: true);
				fireball = new FStagFireballProj(
					shootPos, maverick.xDir, 0, FlameStagger, player, 
					player.getNextActorNetId(), rpc: true);
			}
			if (isSecond && maverick.frameIndex >= 4 && !once) {
				FlameStagger.playSound("fstagShoot", sendRpc: true);
				once = true;
				fireball = new FStagFireballProj(
					shootPos, maverick.xDir, 1, FlameStagger, player, 
					player.getNextActorNetId(), rpc: true);
			}


		if (!isSecond && maverick.frameIndex >= 8) {
			if (isAI || input.isPressed(Control.Shoot, player)) {
				maverick.changeState(new FStagShoot(true));
				return;
			}
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
			return;
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		if (fireball != null && fireball.vel.isZero()) {
			fireball.destroySelf();
		}
	}
}

public class FStagTrailProj : Projectile {
	float sparkTime;
	public FStagTrailProj(
		Point pos, int xDir, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fstag_fire_trail", netId, player
	) {
		weapon = FlameStag.getWeapon();
		damager.damage = 1;
		damager.hitCooldown = 60;
		projId = (int)ProjIds.FStagDashTrail;
		setIndestructableProperties();
		if (isUnderwater()) {
			destroySelf();
			return;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FStagTrailProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		Helpers.decrementTime(ref sparkTime);
		if (sparkTime <= 0) {
			sparkTime = 0.1f;
			new Anim(getFirstPOIOrDefault(), "fstag_fire_trail_extra", xDir, null, true);
		}

		if (!ownedByLocalPlayer) return;

		if (isAnimOver()) {
			destroySelf();
		}
	}
}

public class FStagDashChargeProj : Projectile {
	public FStagDashChargeProj(
		Point pos, int xDir, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "fstag_fire_body", netId, player
	) {
		weapon = FlameStag.getWeapon();
		damager.damage = 1;
		damager.hitCooldown = 15;
		projId = (int)ProjIds.FStagDashCharge;
		setIndestructableProperties();
		if (isUnderwater()) {
			visible = false;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FStagDashChargeProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
}

public class FStagDashChargeState : MaverickState {
	FStagDashChargeProj? proj;
	public FlameStag FlameStagger = null!;
	public FStagDashChargeState() : base("angry") {
	}

	public override void update() {
		base.update();
		if (FlameStagger == null) return;

		proj?.incPos(maverick.deltaPos);
		maverick.turnToInput(input, player);

		if (isAI) {
			if (stateTime > 0.4f) {
				maverick.changeState(new FStagDashState(stateTime));
			}
		} else if ((!input.isHeld(Control.Dash, player) && stateTime > 0.2f) || stateTime > 0.6f) {
			maverick.changeState(new FStagDashState(stateTime));
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		FlameStagger = maverick as FlameStag ?? throw new NullReferenceException();
		proj = new FStagDashChargeProj(
			maverick.getFirstPOIOrDefault("fire_body"), maverick.xDir,
			FlameStagger, player, player.getNextActorNetId(), rpc: true
		);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		proj?.destroySelf();
	}
}

public class  FStagDashProj : Projectile {
	public int type = 0;
	public FStagDashProj(
		Point pos, int xDir, int type, Actor owner,	Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner,  type == 0 ? "fstag_fire_dash" : "fstag_fire_updash", netId, player
	) {
		weapon = FlameStag.getWeapon();
		damager.damage = 3;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		projId = (int)ProjIds.FStagDash;
		setIndestructableProperties();
		this.type = type;
		visible = false;
		if (type == 2) {
			yDir = -1;
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new FStagDashProj(
			args.pos, args.xDir, args.extraData[0],
			args.owner, args.player, args.netId
		);
	}
}

public class FStagDashState : MaverickState {
	float trailTime;
	FStagDashProj? proj;
	float chargeTime;
	public FlameStag FlameStagger = null!;
	public Anim? ProjVisible;
	public FStagDashState(float chargeTime) : base("dash") {
		this.chargeTime = chargeTime;
		enterSound = "fstagDash";
	}

	public override void update() {
		base.update();
		if (FlameStagger == null) return;

		if (input.isPressed(Control.Special1, player)) {
			maverick.changeState(new FStagGrabState(true));
			return;
		}
		ProjVisible?.changePos(maverick.getFirstPOIOrDefault("fire_dash"));
		proj?.changePos(maverick.getFirstPOIOrDefault("fire_dash"));
		maverick.move(new Point(maverick.xDir * 400, 0));
		Helpers.decrementTime(ref trailTime);
		if (trailTime <= 0) {
			trailTime = 0.04f;
			new FStagTrailProj(
			 	maverick.getFirstPOIOrDefault("fire_trail"), maverick.xDir,
				FlameStagger ,player, player.getNextActorNetId(), rpc: true
			);
		}
		if (input.isPressed(Control.Dash, player) || stateTime > chargeTime) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
		FlameStagger = maverick as FlameStag ?? throw new NullReferenceException();

		ProjVisible = new Anim(
			maverick.getFirstPOIOrDefault("fire_dash"), "fstag_fire_dash", maverick.xDir,
			player.getNextActorNetId(), false, sendRpc: true
		);
		proj = new FStagDashProj(
			maverick.getFirstPOIOrDefault("fire_dash"), maverick.xDir, 0,
			FlameStagger, player, player.getNextActorNetId(), rpc: true
		);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		proj?.destroySelf();
		ProjVisible?.destroySelf();
	}
}

public class FStagGrabState : MaverickState {
	float xVel = 400;
	public Character? victim;
	float endLagTime;
	public FStagGrabState(bool fromDash) : base("dash_grab") {
		if (!fromDash) xVel = 0;
		aiAttackCtrl = true;
	}

	public override void update() {
		base.update();
		if (player == null) return;

		xVel = Helpers.lerp(xVel, 0, Global.spf * 2);
		maverick.move(new Point(maverick.xDir * xVel, 0));

		maverick.turnToInput(input, player);

		if (victim == null && maverick.frameIndex >= 6) {
			maverick.changeToIdleOrFall();
			return;
		}

		if (maverick.isAnimOver()) {
			if (victim != null) {
				endLagTime += Global.spf;
				if (endLagTime > 0.25f) {
					maverick.changeState(new FStagUppercutState(victim));
				}
			} else {
				maverick.changeToIdleOrFall();
			}
		}
	}

	public override bool trySetGrabVictim(Character grabbed) {
		if (victim == null) {
			victim = grabbed;
			return true;
		}
		return false;
	}
}

public class FStagUppercutState : MaverickState {
	FStagDashProj? proj;
	float yDist;
	public FlameStag FlameStagger = null!;
	int state;
	public Anim? ProjVisible;
	public Character victim;
	float topDelay;
	int upHitCount;
	int downHitCount;
	public FStagUppercutState(Character victim) : base("updash") {
		this.victim = victim;
		enterSound = "fstagUppercut";
	}

	public override void update() {
		base.update();
		if (FlameStagger == null) return;

		proj?.changePos(maverick.pos);
		ProjVisible?.changePos(maverick.pos);

		float speed = 450;
		float yFactor = 1;
		if (state == 2) {
			yFactor = -1;
		}

		Point moveAmount = new Point(maverick.xDir * 50, -speed * yFactor);
		if (state != 1) {
			maverick.move(moveAmount);
			yDist += Global.spf * speed;
		}

		if (state == 0) {
			var hit = checkCollisionNormal(moveAmount.x * Global.spf, moveAmount.y * Global.spf);
			if (hit != null) {
				if (hit.isCeilingHit()) {
					crashAndDamage(true);
					reverse();
				} else {
					upHitCount++;
					if (upHitCount > 5) {
						crashAndDamage(true);
						reverse();
					} else {
						maverick.xDir *= -1;
					}
				}
			} else if (yDist > 224) {
				reverse();
			}
		} else if (state == 1) {
			topDelay += Global.spf;
			if (topDelay > 0.1f) {
				state = 2;
			}
		} else {
			var hit = checkCollisionNormal(moveAmount.x * Global.spf, moveAmount.y * Global.spf);
			if (hit != null) {
				if (hit.isGroundHit()) {
					crashAndDamage(false);
					maverick.changeToIdleOrFall();
				} else {
					downHitCount++;
					if (downHitCount > 5) {
						crashAndDamage(false);
						maverick.changeToIdleOrFall();
					} else {
						maverick.xDir *= -1;
					}
				}
			}
		}
	}

	public Character? getVictim() {
		if (victim == null) return null;
		if (!victim.sprite.name.EndsWith("_grabbed")) {
			return null;
		}
		return victim;
	}

	public void crashAndDamage(bool isCeiling) {
		if (getVictim() != null) {
			FlameStagger.uppercutWeapon.applyDamage(
				victim, false, FlameStagger, (int)ProjIds.FStagUppercut,
				overrideDamage: isCeiling ? 3 : 5, overrideFlinch: isCeiling ? 0 : Global.defFlinch,
				sendRpc: true
			);
		}
		maverick.playSound("crash", sendRpc: true);
		maverick.shakeCamera(sendRpc: true);
	}

	public void reverse() {
		if (state == 0) {
			state = 1;
			proj?.changeSprite("fstag_fire_downdash", true);
			FlameStagger.changeSpriteFromName("downdash", true);
			ProjVisible?.changeSprite("fstag_fire_downdash", true);
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.unstickFromGround();
		FlameStagger = maverick as FlameStag ?? throw new NullReferenceException();
		ProjVisible = new Anim(
			FlameStagger.pos, "fstag_fire_updash", maverick.xDir,
			player.getNextActorNetId(), false, sendRpc: true
		);
		proj = new FStagDashProj(
			FlameStagger.pos, FlameStagger.xDir, 1, FlameStagger,
			player, player.getNextActorNetId(), rpc: true);
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		FlameStagger.useGravity = true;
		proj?.destroySelf();
		ProjVisible?.destroySelf();
		if (getVictim() != null) {
			victim.releaseGrab(maverick);
		}
	}
}

public class FStagGrabbed : GenericGrabbedState {
	public Character? grabbedChar;
	public float timeNotGrabbed;
	string lastGrabberSpriteName = "";
	public const float maxGrabTime = 4;
	public FStagGrabbed(FlameStag grabber) : base(grabber, maxGrabTime, "_dash_grab") {
		customUpdate = true;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) { return; }

		string grabberSpriteName = grabber.sprite?.name ?? "";
		if (grabberSpriteName.EndsWith("_dash_grab") == true) {
			trySnapToGrabPoint(true);
		} else if (grabberSpriteName.EndsWith("_updash") == true || grabberSpriteName.EndsWith("_downdash") == true) {
			grabTime -= player.mashValue();
			if (grabTime <= 0) {
				character.changeToIdleOrFall();
			}

			if (lastGrabberSpriteName != grabberSpriteName) {
				trySnapToGrabPoint(true);
			} else {
				character.incPos(grabber.deltaPos);
			}
		} else {
			timeNotGrabbed += Global.spf;
			if (timeNotGrabbed > 1f) {
				character.changeToIdleOrFall();
				return;
			}
		}
		lastGrabberSpriteName = grabberSpriteName;
	}
}

public class FStagWallDashState : MaverickState {
	public FStagWallDashState() : base("wall_dash") {
	enterSound = "jumpx2";
	}

	public override void update() {
		base.update();
		if (maverick.grounded) {
			landingCode();
			return;
		}
		wallClimbCode();
		if (Global.level.checkTerrainCollisionOnce(maverick, 0, -1) != null && maverick.vel.y < 0) {
			maverick.vel.y = 0;
		}
		maverick.move(new Point(maverick.xDir * 350, 0));
	}
}
