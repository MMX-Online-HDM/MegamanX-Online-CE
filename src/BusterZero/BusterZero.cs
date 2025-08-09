using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MMXOnline;

public class BusterZero : Character {
	public float zSaberCooldown;
	public float lemonCooldown;
	public bool isBlackZero;
	public int stockedBusterLv;
	public bool stockedSaber;
	public float stockedTime;
	public List<DZBusterProj> zeroLemonsOnField = new();
	public ZBusterSaber meleeWeapon = new();
	public int lastShootPressed;
	public float aiAttackCooldown;
	public float jumpTimeAI;

	public BusterZero(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, int? heartTanks = null, bool isATrans = false
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer,
		isWarpIn, heartTanks, isATrans
	) {
		charId = CharIds.BusterZero;
		altSoundId = AltSoundIds.X3;
	}
	public override CharState getTauntState() {
		return new BZeroTaunt();
	}
	public override void preUpdate() {
		base.preUpdate();
		if (!ownedByLocalPlayer) {
			return;
		}
		// Cooldowns.
		Helpers.decrementFrames(ref zSaberCooldown);
		Helpers.decrementFrames(ref lemonCooldown);
		Helpers.decrementFrames(ref aiAttackCooldown);
		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= speedMul;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name == getSprite(charState.shootSpriteEx)) {
					changeSpriteFromName(charState.defaultSprite, false);
					if (charState is WallSlide) {
						frameIndex = sprite.totalFrameNum - 1;
					}
				}
			}
		}
		if (stockedSaber || stockedBusterLv > 0) {
			stockedTime += Global.spf;
			if (stockedTime >= 61f / 60f) {
				stockedTime = 0;
				playSound("stockedSaber");
			}
		}
	}
	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		// Hypermode music.
		if (!Global.level.isHyper1v1()) {
			if (isBlackZero && ownedByLocalPlayer) {
				if (musicSource == null) {
					addMusicSource("zero_X3", getCenterPos(), true);
				}
			} else {
				destroyMusicSource();
			}
		}
		// Charge and release charge logic.
		chargeLogic(shoot);
	}

	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int chargeType = 0;
			chargeEffect.update(getChargeLevel(), chargeType);
		}
	}

	public override bool canCharge() {
		return (stockedBusterLv == 0 && !stockedSaber && !isInvulnerableAttack());
	}

	public override bool normalCtrl() {
		// Handles Standard Hypermode Activations.
		if (player.currency >= Player.zBusterZeroHyperCost &&
			!isBlackZero &&
			player.input.isHeld(Control.Special2, player) &&
			charState is not HyperZeroStart and not WarpIn
		) {
			hyperProgress += Global.spf;
		} else {
			hyperProgress = 0;
		}
		if (hyperProgress >= 1 && player.currency >= Player.zBusterZeroHyperCost) {
			hyperProgress = 0;
			changeState(new HyperBusterZeroStart(), true);
			return true;
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		if (specialPressed) {
			if (zSaberCooldown == 0) {
				if (stockedSaber) {
					changeState(new BusterZeroHadangeki(), true);
					return true;
				}
				if (charState is WallSlide wallSlide) {
					changeState(new BusterZeroMeleeWall(wallSlide.wallDir, wallSlide.wallCollider), true);
				} else {
					changeState(new BusterZeroMelee(), true);
				}
				return true;
			}
		}
		if (!isCharging()) {
			if (shootPressed) {
				lastShootPressed = Global.floorFrameCount;
			}
			int framesSinceLastShootPressed = Global.floorFrameCount - lastShootPressed;
			if (shootPressed || framesSinceLastShootPressed < 6) {
				if (stockedBusterLv >= 1) {
					if (charState is WallSlide) {
						int chargeLevel = stockedBusterLv;
						if (stockedBusterLv >= 3) {
							stockedBusterLv -= 2;
							chargeLevel = stockedBusterLv;
						} else {
							stockedBusterLv = 0;
						}
						shoot(chargeLevel);
						lemonCooldown = 22;
						lastShootPressed = 0;
						return true;
					}
					changeState(new BusterZeroDoubleBuster(true, stockedBusterLv), true);
					return true;
				}
				if (stockedSaber) {
					if (charState is WallSlide wsState) {
						changeState(new BusterZeroHadangekiWall(wsState.wallDir, wsState.wallCollider), true);
						return true;
					}
					changeState(new BusterZeroHadangeki(), true);
					return true;
				}
				if (lemonCooldown <= 0) {
					shoot(0);
					return true;
				}
			}
		}
		return base.attackCtrl();
	}

	// Shoots stuff.
	public void shoot(int chargeLevel) {
		if (chargeLevel == 0) {
			for (int i = zeroLemonsOnField.Count - 1; i >= 0; i--) {
				if (zeroLemonsOnField[i].destroyed || zeroLemonsOnField[i].reflectCount > 0) {
					zeroLemonsOnField.RemoveAt(i);
				}
			}
			if (zeroLemonsOnField.Count >= 3) { return; }
		}
		string shootSprite = getSprite(charState.shootSpriteEx);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = "zero_shoot"; } else { shootSprite = "zero_fall_shoot"; }
		}
		if (shootAnimTime == 0) {
			changeSprite(shootSprite, false);
		} else if (charState is Idle && !charState.inTransition()) {
			frameIndex = 0;
			frameTime = 0;
		}
		if (charState is LadderClimb) {
			if (player.input.isHeld(Control.Left, player)) {
				this.xDir = -1;
			} else if (player.input.isHeld(Control.Right, player)) {
				this.xDir = 1;
			}
		}
		shootAnimTime = DefaultShootAnimTime;
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		if (chargeLevel == 0) {
			playSound("busterX3", sendRpc: true);
			var lemon = new DZBusterProj(
				shootPos, xDir, this, player, player.getNextActorNetId(), rpc: true
			);
			zeroLemonsOnField.Add(lemon);
			lemonCooldown = 9;
		} else if (chargeLevel == 1) {
			playSound("buster2X3", sendRpc: true);
			new DZBuster2Proj(
				shootPos, xDir, this, player, player.getNextActorNetId(), rpc: true
			);
			lemonCooldown = 22;
		} else if (chargeLevel == 2) {
			playSound("buster3X3", sendRpc: true);
			new DZBuster3Proj(
				shootPos, xDir, this, player, player.getNextActorNetId(), rpc: true
			);
			lemonCooldown = 22;
		} else if (chargeLevel == 3) {
			if (charState is WallSlide) {
				shoot(2);
				stockedBusterLv = 1;
				lemonCooldown = 22;
				return;
			} else {
				shootAnimTime = 0;
				changeState(new BusterZeroDoubleBuster(false, 3), true);
			}
		} else if (chargeLevel >= 4) {
			if (charState is WallSlide) {
				shoot(2);
				stockedBusterLv = 2;
				stockedSaber = true;
				lemonCooldown = 22;
				return;
			} else {
				shootAnimTime = 0;
				changeState(new BusterZeroDoubleBuster(false, 4), true);
			}
		}
		if (chargeLevel >= 1) {
			stopCharge();
		}
	}

	public override int getMaxChargeLevel() {
		return 4;
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"zero_projswing" or "zero_projswing_air" or "zero_wall_slide_attack" => MeleeIds.SaberSwing,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Projectile? proj = id switch {
			(int)MeleeIds.SaberSwing => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.DZMelee, player,
				isBlackZero ? 4 : 3, Global.defFlinch, isReflectShield: true,
				isZSaberClang: true, isZSaberEffect: true,
				addToLevel: addToLevel
			),
			_ => null
		};
		return proj;
	}

	public enum MeleeIds {
		None = -1,
		SaberSwing,
	}

	public override string getSprite(string spriteName) {
		if (Global.sprites.ContainsKey("bzero_" + spriteName)) {
			return "bzero_" + spriteName;
		}
		return "zero_" + spriteName;
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override void increaseCharge() {
		float factor = 1;
		if (isBlackZero) factor = 1.5f;
		chargeTime += Global.speedMul * factor;
	}

	public override float getRunSpeed() {
		float runSpeed = 90;
		if (isBlackZero) {
			runSpeed *= 1.15f;
		}
		return runSpeed * getRunDebuffs();
	}

	public override float getDashSpeed() {
		float dashSpeed = 3.45f * 60f;
		if (isBlackZero) {
			dashSpeed *= 1.15f;
		}
		return dashSpeed * getRunDebuffs();
	}

	public override bool canAirDash() {
		return dashedInAir == 0 || (dashedInAir == 1 && isBlackZero);
	}

	public override bool canAirJump() {
		return dashedInAir == 0 || (dashedInAir == 1 && isBlackZero);
	}

	public override float getLabelOffY() {
		if (sprite.name.Contains("_ra_")) {
			return 25;
		}
		return 45;
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;

		if (isBlackZero) {
			palette = player.zeroPaletteShader;
			palette?.SetUniform("palette", 1);
			palette?.SetUniform("paletteTexture", Global.textures["hyperBusterZeroPalette"]);
		}
		if (Global.isOnFrameCycle(4)) {
			switch (getChargeLevel()) {
				case 1:
					palette = Player.ZeroBlueC;
					break;
				case 2:
					palette = Player.ZeroBlueC;
					break;
				case 3:
					palette = Player.ZeroPinkC;
					break;
				case 4:
					palette = Player.ZeroGreenC;
					break;
			}
			if (stockedSaber || stockedBusterLv == 2) {
				palette = Player.ZeroGreenC;
			}
			if (stockedBusterLv == 1) {
				palette = Player.ZeroPinkC;
			}
		}
		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}
		shaders.AddRange(baseShaders);
		return shaders;
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();
		customData.Add(Helpers.boolArrayToByte([
			isBlackZero,
			stockedSaber
		]));
		customData.Add((byte)stockedBusterLv);
		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];
		bool[] flags = Helpers.byteToBoolArray(data[0]);
		isBlackZero = flags[0];
		stockedSaber = flags[1];
		stockedBusterLv = data[1];
	}

	public override void aiAttack(Actor? target) {
		Helpers.decrementTime(ref jumpTimeAI);
		if (charState.normalCtrl) {
			player.press(Control.Shoot);
		}
		// Go hypermode 
		if (player.currency >= 10 && !isBlackZero && !isInvulnerable()
			&& charState is not HyperBusterZeroStart and not WarpIn) {
			changeState(new HyperBusterZeroStart(), true);
		}
		float enemyDist = 300;
		float enemyDistY = 30;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
			enemyDistY = MathF.Abs(target.pos.y - pos.y);
		}
		bool isTargetClose = enemyDist <= 40;
		bool isTargetInAir = enemyDistY >= 20;
		bool canHitMaxCharge = (!isTargetInAir && getChargeLevel() >= 4);
		bool isFacingTarget = (pos.x < target?.pos.x && xDir == 1) || (pos.x >= target?.pos.x && xDir == -1);
		int ZBattack = Helpers.randomRange(0, 2);
		if (isTargetInAir) {
			doJumpAI();
		}
		if (!isInvulnerable() && charState is not LadderClimb && aiAttackCooldown <= 0 && target != null) {
			switch (ZBattack) {
				// Release full charge if we have it.
				case >= 0 when canHitMaxCharge && isFacingTarget:
					turnToPos(target.getCenterPos());
					player.release(Control.Shoot);
					break;
				// Saber swing when target is close.
				case 0 when isTargetClose:
					turnToPos(target.getCenterPos());
					player.press(Control.Special1);
					break;
				// Another action if the enemy is on Do Jump and do SaberSwing.
				case 1 when isTargetClose:
					turnToPos(target.getCenterPos());
					if (isTargetInAir) doJumpAI();	
					player.press(Control.Special1);
					break;
				// Press Shoot to lemon.
				default:
					player.press(Control.Shoot);
					break;
			}
			aiAttackCooldown = 10;
		}
		base.aiAttack(target);
	}

	public override void aiDodge(Actor? target) {
		base.aiDodge(target);
		if (!charState.attackCtrl) {
			return;
		}
		foreach (GameObject gameObject in getCloseActors(64, true, false, false)) {
			if (gameObject is Projectile proj &&
				proj.damager.owner.alliance != player.alliance && charState.attackCtrl
			) {
				if (proj.projId != (int)ProjIds.RollingShield &&
					proj.projId != (int)ProjIds.FrostShield &&
					proj.projId != (int)ProjIds.SwordBlock &&
					proj.projId != (int)ProjIds.FrostShieldAir &&
					proj.projId != (int)ProjIds.FrostShieldChargedPlatform &&
					proj.projId != (int)ProjIds.FrostShieldPlatform &&
					zSaberCooldown <= 0
				) {
					if (target != null)
					turnToPos(target.getCenterPos());
					changeState(new BusterZeroMelee(), true);
				}
			}
		}
	}
	public void doJumpAI(float jumpTimeAI = 0.75f) {
		if (jumpTimeAI > 0) {
			player.press(Control.Jump);
		}
	}
}
