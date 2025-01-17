using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class KaiserSigmaBaseState : CharState {
	public bool canShootBallistics;
	public bool showExhaust;
	public int exhaustMoveDir;
	public KaiserSigma kaiserSigma;

	public KaiserSigmaBaseState(string sprite) : base(sprite) {
		immuneToWind = true;
	}

	public override void update() {
		base.update();
		character.stopMovingWeak();

		if (this is not KaiserSigmaHoverState &&
			this is not KaiserSigmaFallState &&
			kaiserSigma.kaiserHoverCooldown == 0
		) {
			Helpers.decrementTime(ref kaiserSigma.kaiserHoverTime);
		}

		Helpers.decrementTime(ref kaiserSigma.kaiserMissileShootTime);
		Helpers.decrementTime(ref kaiserSigma.kaiserLeftMineShootTime);
		Helpers.decrementTime(ref kaiserSigma.kaiserRightMineShootTime);
		Helpers.decrementTime(ref kaiserSigma.kaiserHoverCooldown);

		if (!isKaiserSigmaTouchingGround()) {
			if (character.charState is KaiserSigmaIdleState || character.charState is KaiserSigmaBeamState) {
				character.changeState(new KaiserSigmaHoverState(), true);
			}
		}

		if (Global.level.gameMode.isOver && Global.level.gameMode.playerWon(player)) {
			if (!kaiserSigma.kaiserWinTauntOnce && character.charState is KaiserSigmaIdleState) {
				kaiserSigma.kaiserWinTauntOnce = true;
				character.changeState(new KaiserSigmaTauntState(), true);
			}
		}

		if (canShootBallistics) {
			ballisticAttackLogic();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		kaiserSigma = character as KaiserSigma;
	}

	public void tauntLogic() {
		if (!Global.level.gameMode.isOver && player.input.isPressed(Control.Taunt, player)) {
			character.changeState(new ViralSigmaTaunt(false), true);
		}
	}

	public bool isKaiserSigmaTouchingGround() {
		return character.checkCollision(0, 5) != null;
	}

	public void ballisticAttackLogic() {
		bool weaponL = player.input.isPressed(Control.WeaponLeft, player);
		bool weaponR = player.input.isPressed(Control.WeaponRight, player);
		if (player.input.isPressed(Control.Special1, player) && kaiserSigma.isKaiserSigmaGrounded()) {
			if (kaiserSigma.kaiserMissileShootTime == 0) {
				kaiserSigma.kaiserMissileShootTime = 2f;
				var posL = character.getFirstPOIOrDefault("missileL");
				var posR = character.getFirstPOIOrDefault("missileR");

				Global.level.delayedActions.Add(
					new DelayedAction(() => {
						new KaiserSigmaMissileProj(
						new KaiserMissileWeapon(),
						posL.addxy(-8 * character.xDir, 0),
						player, player.getNextActorNetId(),
						rpc: true
					);
					},
					0f
				));
				Global.level.delayedActions.Add(
					new DelayedAction(() => {
						new KaiserSigmaMissileProj(
						new KaiserMissileWeapon(),
						posL, player, player.getNextActorNetId(),
						rpc: true
					);
					},
					0.15f
				));
				Global.level.delayedActions.Add(new DelayedAction(() => {
					new KaiserSigmaMissileProj(
					new KaiserMissileWeapon(),
					posR, player, player.getNextActorNetId(),
					rpc: true
				);
				},
					0.3f
				));
				Global.level.delayedActions.Add(
					new DelayedAction(() => {
						new KaiserSigmaMissileProj(
						new KaiserMissileWeapon(),
						posR.addxy(8 * character.xDir, 0),
						player, player.getNextActorNetId(),
						rpc: true
					);
					},
					0.45f
				));
			}
		} else if (weaponL || weaponR) {
			if ((weaponR && character.xDir == 1) || (weaponL && character.xDir == -1)) {
				if (kaiserSigma.kaiserRightMineShootTime == 0) {
					kaiserSigma.kaiserRightMineShootTime = 1;
					if (kaiserSigma.rightMineMod % 2 == 0) new KaiserSigmaMineProj(new KaiserMineWeapon(), character.getFirstPOIOrDefault("mineR1"), character.xDir, 0, player, player.getNextActorNetId(), rpc: true);
					else new KaiserSigmaMineProj(new KaiserMineWeapon(), character.getFirstPOIOrDefault("mineR2"), character.xDir, 1, player, player.getNextActorNetId(), rpc: true);
					kaiserSigma.rightMineMod++;
				}
			} else if ((weaponR && character.xDir == -1) || (weaponL && character.xDir == 1)) {
				if (kaiserSigma.kaiserRightMineShootTime == 0) {
					kaiserSigma.kaiserRightMineShootTime = 1;
					if (kaiserSigma.leftMineMod % 2 == 0) new KaiserSigmaMineProj(new KaiserMineWeapon(), character.getFirstPOIOrDefault("mineL1"), -character.xDir, 0, player, player.getNextActorNetId(), rpc: true);
					else new KaiserSigmaMineProj(new KaiserMineWeapon(), character.getFirstPOIOrDefault("mineL2"), -character.xDir, 1, player, player.getNextActorNetId(), rpc: true);
					kaiserSigma.leftMineMod++;
				}
			}
		}
	}
}

public class KaiserSigmaIdleState : KaiserSigmaBaseState {
	public KaiserSigmaIdleState() : base("idle") {
		canShootBallistics = true;
		immuneToWind = true;
	}

	public override void update() {
		base.update();

		if (player.input.isPressed(Control.Shoot, player)) {
			character.changeState(new KaiserSigmaBeamState(player.input.isHeld(Control.Down, player)));
		} else if (
			player.input.isHeld(Control.Up, player) ||
			player.input.isHeld(Control.Left, player) ||
			player.input.isHeld(Control.Right, player
		)) {
			if (kaiserSigma.kaiserHoverCooldown == 0 &&
				kaiserSigma.kaiserHoverTime < kaiserSigma.kaiserMaxHoverTime - 0.25f
			) {
				character.changeState(new KaiserSigmaHoverState(), true);
				return;
			}
		} else if (player.input.isPressed(Control.Dash, player)) {
			if (UpgradeMenu.subtankDelay > 0) {
				Global.level.gameMode.setHUDErrorMessage(player, "Cannot become Virus in battle");
			} else {
				character.changeState(new KaiserSigmaVirusState(), true);
			}
			return;
		} else if (player.input.isPressed(Control.Taunt, player)) {
			character.changeState(new KaiserSigmaTauntState(), true);
			return;
		}

		ballisticAttackLogic();
	}
}

public class KaiserSigmaTauntState : KaiserSigmaBaseState {
	public KaiserSigmaTauntState() : base("taunt") {
		immuneToWind = true;
	}

	public override void update() {
		base.update();

		if (character.isAnimOver()) {
			kaiserSigma.changeToKaiserIdleOrFall();
		}
	}
}

public class KaiserSigmaHoverState : KaiserSigmaBaseState {
	public KaiserSigmaHoverState() : base("hover") {
		immuneToWind = true;
		showExhaust = true;
		canShootBallistics = true;
		useGravity = false;
	}

	public override void update() {
		base.update();

		if (player.input.isPressed(Control.Jump, player) ||
			kaiserSigma.kaiserHoverTime > kaiserSigma.kaiserMaxHoverTime
		) {
			character.changeState(new KaiserSigmaFallState(), true);
			return;
		}

		var inputDir = player.input.getInputDir(player);
		var moveAmount = inputDir.times(75);
		moveAmount.y *= 0.5f;

		kaiserSigma.kaiserHoverTime += (Global.spf * 0.5f);
		if (moveAmount.y < 0) {
			kaiserSigma.kaiserHoverTime += (Global.spf * 1.5f);
		}
		if (kaiserSigma.kaiserHoverTime > kaiserSigma.kaiserMaxHoverTime) {
			moveAmount.y = 0;
		}

		exhaustMoveDir = 0;
		if (!moveAmount.isZero()) {
			CollideData? collideData = character.checkCollision(0, moveAmount.y * Global.spf);
			if (moveAmount.y > 0 && collideData?.isGroundHit() == true) {
				kaiserSigma.changeToKaiserIdleOrFall();
				character.playSound("crash", sendRpc: true);
				character.shakeCamera(sendRpc: true);
				return;
			}
			character.move(new Point(moveAmount.x, moveAmount.y));
			if (moveAmount.x != 0) {
				exhaustMoveDir = Math.Sign(moveAmount.x);
				character.xDir = exhaustMoveDir;
			}
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		kaiserSigma.kaiserHoverCooldown = 0.75f;
	}
}

public class KaiserSigmaFallState : KaiserSigmaBaseState {
	public float velY;
	public KaiserSigmaFallState() : base("fall") {
		immuneToWind = true;
	}

	public override void update() {
		base.update();

		character.addGravity(ref velY);

		var moveAmount = new Point(0, velY);

		/*
		if (!character.tryMove(moveAmount, out var hitData))
		{
			character.playSound("crash", sendRpc: true);
			character.shakeCamera(sendRpc: true);
			float snapY = hitData.getHitPointSafe().y;
			if (snapY > character.pos.y)
			{
				character.changePos(new Point(character.pos.x, snapY));
			}
			character.changeToKaiserIdleOrFall();
		}
		*/

		if (!moveAmount.isZero()) {
			if (moveAmount.y > 0 && character.checkCollision(0, moveAmount.y * Global.spf) != null) {
				kaiserSigma.changeToKaiserIdleOrFall();
				character.playSound("crash", sendRpc: true);
				character.shakeCamera(sendRpc: true);
				return;
			}
			character.move(new Point(moveAmount.x, moveAmount.y));
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		kaiserSigma.kaiserHoverCooldown = 0.75f;
	}
}

public class KaiserSigmaVirusState : CharState {
	public Anim kaiserShell;
	public Anim relocatedKaiserShell;
	public Anim viralSigmaHeadReturn;

	public bool isLerpingBack;
	Point kaiserSigmaDestPos;
	public bool startAnimOver;
	public bool isRelocating;
	public int origXDir;

	public KaiserSigma kaiserSigma;

	public KaiserSigmaVirusState() : base("virus") {
		immuneToWind = true;
	}

	public void lerpBack(Point destPos, bool isRelocating) {
		if (!isLerpingBack) {
			this.isRelocating = isRelocating;
			isLerpingBack = true;
			character.changeSpriteFromName("virus_return", true);
			kaiserSigmaDestPos = destPos;
			if (!isRelocating) {
				character.xDir = origXDir;
			}
		}
	}

	public override void update() {
		base.update();

		stateTime += Global.spf;
		character.stopMoving();

		if (!startAnimOver) {
			character.xScale += Global.spf * 2.5f;
			character.yScale = character.xScale;
			if (character.xScale > 1) {
				startAnimOver = true;
				character.xScale = 1;
				character.yScale = 1;
			}
			return;
		}

		if (player.input.isPressed(Control.Dash, player)) {
			//lerpBack(kaiserShell.pos, false);
		}

		if (isLerpingBack) {
			if (viralSigmaHeadReturn == null) {
				viralSigmaHeadReturn = new Anim(
					character.pos, "kaisersigma_virus_return",
					character.xDir, player.getNextActorNetId(), false, sendRpc: true
				) {
					zIndex = character.zIndex
				};
				character.visible = false;
				character.changePos(kaiserSigmaDestPos);
				character.changeSpriteFromName("idle", true);
			}

			// Note: Code to shrink is in Anim.cs
			if (isRelocating && relocatedKaiserShell == null) {
				relocatedKaiserShell = new Anim(kaiserSigmaDestPos, "kaisersigma_empty", character.xDir, player.getNextActorNetId(), false, sendRpc: true, fadeIn: true);
			}

			Point lerpDestPos = kaiserSigmaDestPos.addxy(12 * character.xDir, -94);
			viralSigmaHeadReturn.changePos(Point.lerp(viralSigmaHeadReturn.pos, lerpDestPos, Global.spf * 10));
			if (viralSigmaHeadReturn.pos.distanceTo(lerpDestPos) < 10 && viralSigmaHeadReturn.xScale == 0) {
				kaiserSigma.changeToKaiserIdleOrFall();

				character.destroyMusicSource();
				character.addMusicSource("kaiserSigma", character.getCenterPos(), true);
				RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddKaiserSigmaMusicSource);

				character.xScale = 1;
				character.yScale = 1;
			}
			return;
		}

		var inputDir = player.input.getInputDir(player);
		var moveAmount = inputDir.times(100);
		character.move(moveAmount);
		character.turnToInput(player.input, player);

		clampViralSigmaPos();

		bool canSpawnAtPos = KaiserSigma.canKaiserSpawn(kaiserSigma, out var spawnPoint);
		if (player.input.isPressed(Control.Shoot, player) || player.input.isPressed(Control.Jump, player)) {
			if (canSpawnAtPos) {
				lerpBack(spawnPoint, true);
				return;
			}
		}

		if (!canSpawnAtPos) {
			var redXPos = character.getCenterPos();
			DrawWrappers.DrawLine(redXPos.x - 10, redXPos.y - 10, redXPos.x + 10, redXPos.y + 10, Color.Red, 2, ZIndex.HUD);
			DrawWrappers.DrawLine(redXPos.x - 10, redXPos.y + 10, redXPos.x + 10, redXPos.y - 10, Color.Red, 2, ZIndex.HUD);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		kaiserShell = new Anim(character.pos, "kaisersigma_empty_fadeout", character.xDir, player.getNextActorNetId(), false, sendRpc: true) { zIndex = character.zIndex - 10 };

		character.changePos(character.pos.addxy(12 * character.xDir, -94));
		origXDir = character.xDir;

		character.xScale = 0;
		character.yScale = 0;

		RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddKaiserViralSigmaMusicSource);
		character.destroyMusicSource();
		character.addMusicSource("demo_X3", character.getCenterPos(), true, loop: false);

		kaiserSigma = character as KaiserSigma;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.visible = true;
		if (newState is Die && kaiserShell != null && !kaiserShell.destroyed) {
			var effectPos = kaiserShell.pos.addxy(0, -55);
			var ede = new ExplodeDieEffect(
				player, effectPos, effectPos, "empty",
				1, character.zIndex, false, 60, 3, false
			);
			Global.level.addEffect(ede);
			var anim = new Anim(
				kaiserShell.pos, kaiserShell.sprite.name, kaiserShell.xDir,
				player.getNextActorNetId(), false, sendRpc: true
			);
			anim.ttl = 3;
			anim.blink = true;
		}
		kaiserShell?.destroySelf();
		relocatedKaiserShell?.destroySelf();
		viralSigmaHeadReturn?.destroySelf();
	}
}

public class KaiserSigmaBeamState : KaiserSigmaBaseState {
	int state = 0;
	float chargeTime;
	KaiserSigmaBeamProj proj;
	float randPartTime;
	bool isDown;
	SoundWrapper chargeSound;
	SoundWrapper beamSound;
	public KaiserSigmaBeamState(bool isDown) : base(isDown ? "shoot" : "shoot2") {
		immuneToWind = true;
		canShootBallistics = true;
		this.isDown = isDown;
	}

	public override void update() {
		base.update();

		if (state == 0) {
			if (chargeTime == 0) {
				chargeSound = character.playSound("kaiserSigmaCharge", sendRpc: true);
			}
			chargeTime += Global.spf;
			Point shootPos = character.getFirstPOIOrDefault();

			randPartTime += Global.spf;
			if (randPartTime > 0.025f) {
				randPartTime = 0;
				var partSpawnAngle = Helpers.randomRange(0, 360);
				float spawnRadius = 20;
				float spawnSpeed = 150;
				var partSpawnPos = shootPos.addxy(Helpers.cosd(partSpawnAngle) * spawnRadius, Helpers.sind(partSpawnAngle) * spawnRadius);
				var partVel = partSpawnPos.directionToNorm(shootPos).times(spawnSpeed);
				new Anim(partSpawnPos, "kaisersigma_charge", 1, player.getNextActorNetId(), false, sendRpc: true) {
					vel = partVel,
					ttl = ((spawnRadius - 2) / spawnSpeed),
				};
			}

			if (chargeTime > 1f) {
				state = 1;
				proj = new KaiserSigmaBeamProj(
					new KaiserBeamWeapon(), shootPos, character.xDir,
					!isDown, player, player.getNextActorNetId(), rpc: true
				);
				beamSound = character.playSound("kaiserSigmaBeam", sendRpc: true);
			}
		} else if (state == 1) {
			if (proj.destroyed) {
				kaiserSigma.changeToKaiserIdleOrFall();
			}
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		proj?.destroySelf();
		if (chargeSound != null && !chargeSound.deleted) {
			chargeSound.sound.Stop();
			RPC.stopSound.sendRpc(chargeSound.soundBuffer.soundKey, character.netId);
		}
		if (beamSound != null && !beamSound.deleted) {
			beamSound.sound.Stop();
			RPC.stopSound.sendRpc(beamSound.soundBuffer.soundKey, character.netId);
		}
	}
}

public class KaiserBeamWeapon : Weapon {
	public KaiserBeamWeapon() : base() {
		weaponSlotIndex = 116;
		index = (int)WeaponIds.Sigma3KaiserBeam;
		killFeedIndex = 166;
	}
}

public class KaiserSigmaBeamProj : Projectile {
	public float beamAngle;
	public float beamWidth;
	const float beamLen = 150;
	const float maxBeamTime = 2;
	public KaiserSigmaBeamProj(
		Weapon weapon, Point pos, int xDir, bool isUp, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 0, 1, player, "empty", Global.miniFlinch, 0.15f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.Sigma3KaiserBeam;
		setIndestructableProperties();

		if (xDir == 1 && !isUp) beamAngle = 45;
		if (xDir == -1 && !isUp) beamAngle = 135;
		if (xDir == -1 && isUp) beamAngle = 225;
		if (xDir == 1 && isUp) beamAngle = 315;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)(isUp ? 1 : 0));
		}
	}

	public override void update() {
		base.update();

		if (globalCollider == null) {
			globalCollider = new Collider(getPoints(), true, null, false, false, 0, new Point(0, 0));
		} else {
			changeGlobalCollider(getPoints());
		}

		if (time < 1) {
			beamWidth += Global.spf * 20;
		} else if (time >= 1 && time < 1 + maxBeamTime) {
			beamWidth = 20;
			if (owner.input.isPressed(Control.Shoot, owner)) {
				time = 1 + maxBeamTime;
			}
		} else if (time >= 1 + maxBeamTime && time < 2 + maxBeamTime) {
			beamWidth -= Global.spf * 20;
		} else if (time >= 2 + maxBeamTime) {
			destroySelf();
		}
	}

	public List<Point> getPoints() {
		float ang1 = beamAngle - beamWidth;
		float ang2 = beamAngle + beamWidth;
		Point pos1 = new Point(beamLen * Helpers.cosd(ang1), beamLen * Helpers.sind(ang1));
		Point pos2 = new Point(beamLen * Helpers.cosd(ang2), beamLen * Helpers.sind(ang2));

		var points = new List<Point> {
			pos,
			pos.add(pos1),
			pos.add(pos2),
		};
		return points;
	}

	public override void render(float x, float y) {
		base.render(x, y);
		Color color = new Color(132, 132, 231);
		DrawWrappers.DrawPolygon(getPoints(), color, true, ZIndex.Character);
	}
}

public class KaiserMissileWeapon : Weapon {
	public KaiserMissileWeapon() : base() {
		weaponSlotIndex = 114;
		index = (int)WeaponIds.Sigma3KaiserMissile;
		killFeedIndex = 164;
	}
}

public class KaiserSigmaMissileProj : Projectile {
	public Actor? target;
	public float smokeTime = 0;
	public float maxSpeed = 150;
	public float health = 2;
	public KaiserSigmaMissileProj(Weapon weapon, Point pos, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, 1, 0, 2, player, "kaisersigma_missile", Global.defFlinch, 0f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.Sigma3KaiserMissile;
		maxTime = 2f;
		fadeOnAutoDestroy = true;
		reflectableFBurner = true;
		netcodeOverride = NetcodeModel.FavorDefender;

		fadeSprite = "explosion";
		fadeSound = "explosion";
		angle = 270;

		if (rpc) {
			rpcCreateAngle(pos, player, netProjId, xDir);
		}
	}
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

		if (ownedByLocalPlayer) {
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
					if (angle != null) angle = Helpers.lerpAngle((float)angle, destAngle, Global.spf * 3);
				}
			}
			if (time >= 0.1 && target == null) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, false, aMaxDist: Global.screenW);
			}

			if (angle != null) {
				forceNetUpdateNextFrame = true;
				vel.x = Helpers.cosd((float)angle) * maxSpeed;
				vel.y = Helpers.sind((float)angle) * maxSpeed;
			}

		}

		smokeTime += Global.spf;
		if (smokeTime > 0.2) {
			smokeTime = 0;
			new Anim(pos, "torpedo_smoke", 1, null, true);
		}
	}

	public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId) {
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damager.owner.alliance != damagerAlliance; }
	public bool isInvincible(Player attacker, int? projId) { return false; }
	public bool canBeHealed(int healerAlliance) { return false; }
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
}

public class KaiserMineWeapon : Weapon {
	public KaiserMineWeapon() : base() {
		weaponSlotIndex = 115;
		index = (int)WeaponIds.Sigma3KaiserMine;
		killFeedIndex = 165;
	}
}

public class KaiserSigmaMineProj : Projectile, IDamagable {
	bool firstHit;
	float hitWallCooldown;
	float health = 3;
	int type;
	bool startWall;
	public KaiserSigmaMineProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 100, 4, player, "kaisersigma_mine", Global.defFlinch, 0.15f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.Sigma3KaiserMine;
		maxTime = 4f;
		this.type = type;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		netcodeOverride = NetcodeModel.FavorDefender;

		if (type == 1) {
			vel.y = 100;
			vel = vel.normalize().times(speed);
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void onStart() {
		base.onStart();
		if (Global.level.checkCollisionShape(collider.shape, null) != null) {
			startWall = true;
		}
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref hitWallCooldown);
		if (startWall) {
			if (Global.level.checkCollisionShape(collider.shape, null) == null) {
				startWall = false;
			}
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;
		if (hitWallCooldown > 0) return;
		if (startWall) return;

		bool didHit = false;
		if (!firstHit && type == 0) {
			firstHit = true;
			vel.x *= -1;
			vel.y = -speed;
			vel = vel.normalize().times(speed);
			didHit = true;
		} else if (other.isSideWallHit()) {
			vel.x *= -1;
			vel = vel.normalize().times(speed);
			didHit = true;
		} else if (other.isCeilingHit() || other.isGroundHit()) {
			vel.y *= -1;
			vel = vel.normalize().times(speed);
			didHit = true;
		}
		if (didHit) {
			//playSound("gbeetleProjBounce", sendRpc: true);
			hitWallCooldown = 0.1f;
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damager.owner.alliance != damagerAlliance; }
	public bool isInvincible(Player attacker, int? projId) { return false; }
	public bool canBeHealed(int healerAlliance) { return false; }
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
	public bool isPlayableDamagable() { return false; }
}

public class KaiserStompWeapon : Weapon {
	public KaiserStompWeapon(Player player) : base() {
		index = (int)WeaponIds.Sigma3KaiserStomp;
		killFeedIndex = 163;
		damager = new Damager(player, 12, Global.defFlinch, 1);
	}
}

public class KaiserSigmaRevive : CharState {
	int state = 0;
	public ExplodeDieEffect explodeDieEffect;
	public Point spawnPoint;
	public KaiserSigmaRevive(ExplodeDieEffect explodeDieEffect) : base("enter") {
		this.explodeDieEffect = explodeDieEffect;
	}

	float alphaTime;
	public override void update() {
		base.update();

		if (state == 0) {
			if (explodeDieEffect == null || explodeDieEffect.destroyed) {
				state = 1;
				character.addMusicSource("kaiserSigma", character.pos, true);
				RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddKaiserSigmaMusicSource);
				character.visible = true;
				character.changePos(spawnPoint);
			}
		} else if (state == 1) {
			alphaTime += Global.spf;
			if (alphaTime >= 0.2) character.alpha = 0.2f;
			if (alphaTime >= 0.4) character.alpha = 0.4f;
			if (alphaTime >= 0.6) character.alpha = 0.6f;
			if (alphaTime >= 0.8) character.alpha = 0.8f;
			if (alphaTime >= 1) character.alpha = 1f;
			if (character.alpha >= 1) {
				character.alpha = 1;
				character.frameSpeed = 1;
				state = 2;
			}
		} else if (state == 2) {
			if (Global.debug && player.input.isPressed(Control.Special1, player)) {
				character.frameIndex = character.sprite.totalFrameNum - 1;
			}

			if (character.isAnimOver()) {
				state = 3;
			}
		} else if (state == 3) {
			if (stateTime > 0.5f) {
				player.health = 1;
				character.addHealth(player.maxHealth);
				state = 4;
			}
		} else if (state == 4) {
			if (Global.debug && player.input.isPressed(Control.Special1, player)) {
				player.health = player.maxHealth;
			}

			if (player.health >= player.maxHealth) {
				character.invulnTime = 0.5f;
				character.useGravity = true;
				character.stopMoving();
				character.grounded = false;
				character.canBeGrounded = false;

				character.changeState(new KaiserSigmaIdleState(), true);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		spawnPoint = character.pos;
		character.syncScale = true;
		character.frameIndex = 0;
		character.frameSpeed = 0;
		//character.immuneToKnockback = true;
		character.alpha = 0;
		player.sigmaAmmo = player.sigmaMaxAmmo;
		KaiserSigma kaiserSigma = character as KaiserSigma;
	}
}
