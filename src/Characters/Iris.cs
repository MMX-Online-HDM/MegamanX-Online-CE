using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;
public class Iris : Character {
	public bool isHyperIris;
	public float hyperModeTimer = 5;
	public int shootPressTime;
	public int specialPressTime;
	public bool shootPressed => (shootPressTime > 0);
	public bool specialPressed => (specialPressTime > 0);
	public bool upPressed => player.input.isHeld(Control.Up, player);
	public bool downPressed => player.input.isHeld(Control.Down, player);
	public bool shootHeld => player.input.isHeld(Control.Shoot, player);
	public bool specialHeld => player.input.isHeld(Control.Special1, player);
	public bool LeftPressed;
	public float dashAttackCooldown;
	public int stockedBusterLv;
	public bool stockedSaber;
	public float lemonCooldown;
	public List<IrisBusterProj> zeroLemonsOnField = new();
	public ZSaberIris meleeWeapon = new();
	public IrisRakuhouha IrisRakuhouhaWeapon = new();
	public IrisRekkoha IrisRekkohaWeapon = new();
	public IrisBuster IrisBusterWeapon = new();
	public float RakuhouhaCooldown;
	public Iris(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Iris;
		altCtrlsLength = 2;
	}
	
	public override void update() {
		base.update();
		if (!Global.level.isHyper1v1()) {
			if (isHyperIris) {
				if (musicSource == null) {
					addMusicSource("Iris", getCenterPos(), true);
				}
			} else {
				destroyMusicSource();
			}
		}
		if (!ownedByLocalPlayer) {
			return;
		}	
		if (stockedBusterLv > 0 || stockedSaber) {
			var renderGfx = stockedBusterLv switch {
				_ when stockedSaber || stockedBusterLv == 2 => RenderEffectType.ChargeGreen,
				1 => RenderEffectType.ChargePink,
				2 => RenderEffectType.ChargeOrange,
				_ => RenderEffectType.ChargeBlue
			};
			addRenderEffect(renderGfx, 0.033333f, 0.1f);
		}	
		inputUpdate();
		IrisRakuhouhaWeapon.update();
		IrisRakuhouhaWeapon.charLinkedUpdate(this, true);
		IrisRekkohaWeapon.update();
		IrisRekkohaWeapon.charLinkedUpdate(this, true);
		Helpers.decrementFrames(ref dashAttackCooldown);
		Helpers.decrementFrames(ref lemonCooldown);
		Helpers.decrementTime(ref RakuhouhaCooldown);
		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				changeSpriteFromName(charState.defaultSprite, false);
				if (charState is WallSlide) {
					frameIndex = sprite.totalFrameNum - 1;
				}
			}
		}
		// Hypermode timer.
		if (isHyperIris && hyperModeTimer > 0) {
			Helpers.decrementTime(ref hyperModeTimer);
		}
		if (hyperModeTimer <= 0) isHyperIris = false;
		if (!isHyperIris) hyperModeTimer = 5;
		chargeLogic(shoot);
	}
	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int chargeType = 1;
			int level = getChargeLevel();
			var renderGfx = RenderEffectType.ChargeBlue;
			renderGfx = level switch {
				1 => RenderEffectType.ChargeBlue,
				2 => RenderEffectType.ChargeYellow,
				3 => RenderEffectType.ChargePink,
				_ => RenderEffectType.ChargeGreen,
			};
			addRenderEffect(renderGfx, 0.033333f, 0.1f);
			chargeEffect.update(getChargeLevel(), chargeType);
		}
	}	
	public bool hypermodeActive() {
		return isHyperIris;
	}
	public override bool canShoot() {
		return (!charState.invincible && !isInvulnerable() &&
			(charState.attackCtrl || (charState.altCtrls.Length >= 2 && charState.altCtrls[1]))
		);
	}
	public override bool canCharge() {
		if (!isHyperIris && getChargeLevel() >= 2) {
			return false;
		}
		return (!isInvulnerable (charState.attackCtrl || getChargeLevel() > 0) && (player.currency > 0));
	}
	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.WeaponRight, player);
	}
	public void setShootAnim() {
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = "iris_shoot"; }
			else { shootSprite = "iris_fall_shoot"; }
		}
		if (shootAnimTime == 0) {
			changeSprite(shootSprite, false);
		} else if (charState is Idle) {
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
		shootAnimTime = 0.3f;
	}
	public void shoot(int chargeLevel) {
		if (player.currency <= 0) { return; }
		int currencyUse = 0;

		// Cancel non-invincible states.
		if (!charState.attackCtrl && !charState.invincible) {
			changeToIdleOrFall();
		}
		if (chargeLevel == 0) {
			for (int i = zeroLemonsOnField.Count - 1; i >= 0; i--) {
				if (zeroLemonsOnField[i].destroyed) {
					zeroLemonsOnField.RemoveAt(i);
				}
			}
			if (zeroLemonsOnField.Count >= 2) { return; }
		}
		setShootAnim();
		Point shootPos = getShootPos();
		Point anim = getShootPos();
		int xDir = getShootXDir();
		if (chargeLevel >= 0) {
			new Anim(anim, "iris_effectproj", xDir, player.getNextActorNetId(), true, true);
		}
		if (chargeLevel == 0) {
			playSound("busterX3", sendRpc: true);
			var lemon = new IrisBusterProj(
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
			);
			zeroLemonsOnField.Add(lemon);
			lemonCooldown = 11;
		}
		else if (chargeLevel == 1) {
			playSound("buster2X3", sendRpc: true);
			new IrisBuster2Proj(
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
			);
			lemonCooldown = 22;
		} else if (chargeLevel == 2) {
			currencyUse = 1;
			playSound("buster3X3", sendRpc: true);
			new IrisBuster3Proj(
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
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
				changeState(new IrisDoubleBuster(false, true), true);
			}
		}
		else if (chargeLevel >= 4) {
			if (charState is WallSlide) {
				shoot(2);
				stockedBusterLv = 2;
				stockedSaber = true;
				lemonCooldown = 22;
				return;
			} else {
				shootAnimTime = 0;
				changeState(new IrisDoubleBuster(false, false), true);
			}
		}
		if (chargeLevel >= 1) {
			stopCharge();
		}
		if (currencyUse > 0) {
			if (player.currency > 0) {
				player.currency--;
			}
		}
	}
	// Non-attacks like guard and hypermode activation.
	public override bool normalCtrl() {
		// Hypermode activation.
		int cost = 10;
		if (player.currency >= cost && player.input.isHeld(Control.Special2, player) 
			&& charState is not HyperIrisStart and not WarpIn && (!isHyperIris)
		) {
			hyperProgress += Global.spf;
		} else {
			hyperProgress = 0;
		}
		if (hyperProgress >= 1 && player.currency >= 10) {
			hyperProgress = 0;
			changeState(new HyperIrisStart(), true);
			return true;
		}
		// If we changed state this frame. Return.
		// This is to prevent jumping guard shenanigans.
		bool changedState = base.normalCtrl();
		if (changedState) {
			return true;
		}
		if (charState.attackCtrl && charState is not Dash && grounded 
		   && (player.input.isHeld(Control.WeaponLeft, player))
		) {
			turnToInput(player.input, player);
			changeState(new SwordBlock());
			return true;
		}
		return false;
	}
	public override bool attackCtrl() {
		bool WeaponRightpressed = player.input.isPressed(Control.WeaponRight, player);
		if (!isCharging()) {
			if (WeaponRightpressed) {
				lastShootPressed = Global.frameCount;
			}
			int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
				if (WeaponRightpressed || framesSinceLastShootPressed < 6) {
				if (stockedBusterLv == 1) {
					if (charState is WallSlide) {
						shoot(1);
						lemonCooldown = 22;
						stockedBusterLv = 0;
						return true;
					}
					changeState(new IrisDoubleBuster(true, true), true);
					return true;
				}
				if (stockedBusterLv == 2) {
					if (charState is WallSlide) {
						shoot(2);
						lemonCooldown = 22;
						stockedBusterLv = 0;
						lastShootPressed = 0;
						return true;
					}
					changeState(new IrisDoubleBuster(true, false), true);
					return true;
				}
				if (stockedSaber) {
					if (charState is WallSlide wsState) {
						changeState(new IrisHadangekiWall(wsState.wallDir, wsState.wallCollider), true);
						return true;
					}
					changeState(new IrisHadangeki(), true);
					return true;
				}
				if (WeaponRightpressed) {
					if (charState is WallSlide wallSlide) {
						changeState(new IrisWallSlideSaberState(wallSlide.wallDir, wallSlide.wallCollider), true);				
						return true;
					}
				}
				if (lemonCooldown <= 0) {
				shoot(0);
				return true;
				}
			}
		}
		if (charState is WallSlide wallSlide1 && shootPressed) {
			changeState(new IrisWallSlideSaberState(wallSlide1.wallDir, wallSlide1.wallCollider), true);
			return true;
		}
		if (grounded && vel.y >= 0) {
			return groundAttacks();
		} else {
			return airAttacks();
		}
	}

	public bool groundAttacks() {
		int yDir = player.input.getYDir(player);
		if (yDir == 1 && downPressed && specialPressed) {
			if (RakuhouhaCooldown <= 0) { 
				if (IrisRakuhouhaWeapon.ammo >= 14 && IrisRakuhouhaWeapon.ammo <= 27) { 
					IrisRakuhouhaWeapon.addAmmo(-IrisRakuhouhaWeapon.getAmmoUsage(0), player);
					changeState(new IrisRakuhouhaState(), true);
					RakuhouhaCooldown = isHyperIris ? 1f : 2;
					return true;				
				} else if (IrisRakuhouhaWeapon.ammo >= 28) { 
					IrisRakuhouhaWeapon.addAmmo(-28, player);
					changeState(new IrisRekkohaState(), true);
					RakuhouhaCooldown = isHyperIris ? 1f : 2;
					return true;				
				}
			}
			lemonCooldown = 60;
		}
		if (upPressed && yDir == -1) {
			if (shootPressed) {
				changeState(new IrisRyuenjinState(), true);	
				return true;
			}
			if (specialPressed) {
				changeState(new IrisDenjinState(), true);	
				return true;
			}
			lemonCooldown = 60;
		}
		if (isDashing && (shootPressed || specialPressed)) {
			if (dashAttackCooldown > 0) {
				return false;
			}
			dashAttackCooldown = 60;
			slideVel = xDir * getDashSpeed();
			if (specialPressTime > shootPressTime) {
				changeState(new IrisShippuugaState(), true);
				return true;
			}
			changeState(new IrisDashSlashState(), true);
			lemonCooldown = 60;
			return true;
		}
		if (shootPressed) {
			// Crounch variant.
			if (yDir == 1) {
				changeState(new IrisCrouchSlashState(), true);
				return true;
			}
			changeState(new IrisSlash1State(), true);
			lemonCooldown = 60;
			return true;
		}
		if (specialPressed && specialPressTime > shootPressTime) {
			lemonCooldown = 60;
			changeState(new IrisRaijingekiState(), true);	
			return true;
		}		
		return false;
	}
	public bool airAttacks() {
		if (downPressed) {
			if (specialPressed) {
				changeState(new IrisHyouretsuzanState(), true);	
				return true;
			}
			if (shootPressed) {
				changeState(new IrisRakukojinState(), true);	
				return true;
			}
		}
		if (shootPressed) {
			changeState(new IrisAirSlashState(), true);	
			return true;
		}
		if (specialPressed) {
			changeState(new IrisRollingSlashtate(), true);	
			return true;
		}
		return false;
	}


	public override bool changeState(CharState newState, bool forceChange = false) {
		// Save old state.
		CharState oldState = charState;
		// Base function call.
		bool hasChanged = base.changeState(newState, forceChange);
		if (!hasChanged) {
			return false;
		}
		if (!newState.attackCtrl || newState.attackCtrl != oldState.attackCtrl) {
			shootPressTime = 0;
			specialPressTime = 0;
		}
		return true;
	}
	public override bool canAirJump() {
		return dashedInAir == 0;
	}

	public override float getRunSpeed() {
		float runSpeed = Physics.WalkSpeed;
		if (isHyperIris) {
			runSpeed *= 1.2f;
		}
		return runSpeed * getRunDebuffs();
	}

	public override float getDashSpeed() {
		if (flag != null || !isDashing) {
			return getRunSpeed();
		}
		float dashSpeed = 210;
		if (isHyperIris) {
			dashSpeed *= 1.2f;
		}
		return dashSpeed * getRunDebuffs();
	}
	public override void addAmmo(float amount) {
		IrisRakuhouhaWeapon.addAmmoHeal(amount);
		IrisRekkohaWeapon.addAmmoHeal(amount);
	}

	public override void addPercentAmmo(float amount) {
		IrisRakuhouhaWeapon.addAmmoPercentHeal(amount);
		IrisRekkohaWeapon.addAmmoPercentHeal(amount);

	}
	public override bool altCtrl(bool[] ctrls) {
		if (charState is IrisGenericMeleeState igms) {
			igms.altCtrlUpdate(ctrls);
		}
		return base.altCtrl(ctrls);
	}
	public override bool canAddAmmo() {
		return (IrisRakuhouhaWeapon.ammo < IrisRakuhouhaWeapon.maxAmmo) || (IrisRekkohaWeapon.ammo < IrisRekkohaWeapon.maxAmmo);
	}
	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);
		if (proj == null) {
			return null;
		}
		// Assing data variables.
		proj.meleeId = meleeId;
		proj.owningActor = this;
		// Damage based on fall speed.
		if (meleeId == (int)MeleeIds.Rakukojin) {
			float damage = 1 + Helpers.clamp(MathF.Floor(deltaPos.y * 0.9f), 0, 10);
			int flinch = 2 + Helpers.clamp((int)MathF.Floor(deltaPos.y * 5f), 13, 36);
			proj.damager.damage = damage;
			proj.damager.flinch = flinch;
		}
		return proj;
	}
	public enum MeleeIds {
		None = -1,
		// Ground
		HuSlash,
		HaSlash,
		HuhSlash,
		CrouchSlash,
		// Dash
		DashSlash,
		Shippuuga,
		// Air
		AirSlash,
		RollingSlash,
		// Ground Specials
		Raijingeki,
		// Up Specials
		Ryuenjin,
		Denjin,
		// Down specials
		Hyouretsuzan,
		Rakukojin,
		// Others
		LadderSlash,
		WallSlash,
		Gokumonken,
		Hadangeki,
	}
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			// Ground
			"iris_saber1" => MeleeIds.HuSlash,
			"iris_saber2" => MeleeIds.HaSlash,
			"iris_saber3" => MeleeIds.HuhSlash,
			"iris_saber_crouch" => MeleeIds.CrouchSlash,
			// Dash
			"iris_saber_dash" => MeleeIds.DashSlash,
			"iris_shippuga" => MeleeIds.Shippuuga,
			// Air
			"iris_saber_air" => MeleeIds.AirSlash,
			"iris_kuuenzan" => MeleeIds.RollingSlash,
			// Ground Speiclas
			"iris_raijingeki" => MeleeIds.Raijingeki,
			// Up Specials
			"iris_ryuenjin" => MeleeIds.Ryuenjin,
			"iris_eblade" => MeleeIds.Denjin,
			// Down specials
			"iris_hyouretsuzan" => MeleeIds.Hyouretsuzan,
			"iris_rakukojin" => MeleeIds.Rakukojin,
			// Others.
			"iris_ladder_attack" => MeleeIds.LadderSlash,
			"iris_wall_slide_attack" => MeleeIds.WallSlash,
			"iris_block" => MeleeIds.Gokumonken,
			"iris_saber" or "iris_saber_air2" => MeleeIds.Hadangeki,
			_ => MeleeIds.None
		});
	}
	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		return id switch {
			// Ground
			(int)MeleeIds.HuSlash => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaber1, player, 1, 0, 0.25f, isReflectShield: true,
				addToLevel: addToLevel
			),
			(int)MeleeIds.HaSlash => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaber2, player, 2, 0, 0.25f, isReflectShield: true,
				addToLevel: addToLevel
			),
			(int)MeleeIds.HuhSlash => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaber3, player,
				4, Global.defFlinch, 0.25f, isReflectShield: true,
				addToLevel: addToLevel
			),
			(int)MeleeIds.CrouchSlash => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaberCrouch, player, 2, 0, 0.25f, isReflectShield: true,
				addToLevel: addToLevel
			),
			// Dash
			(int)MeleeIds.DashSlash => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaberDash, player, 2, 0, 0.25f, isReflectShield: true,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Shippuuga => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisShippuuga, player, 2, Global.halfFlinch, 0.25f,
				addToLevel: addToLevel
			),
			// Air
			(int)MeleeIds.AirSlash => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaberAir, player, 2, 0, 0.25f, isReflectShield: true,
				addToLevel: addToLevel
			),
			(int)MeleeIds.RollingSlash =>  new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaberRollingSlash, player,
				1, 0, 0.125f, isDeflectShield: true,
				addToLevel: addToLevel
			),
			// Ground Specials
			(int)MeleeIds.Raijingeki => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisRaijingeki, player, 2, Global.defFlinch, 0.06f,
				addToLevel: addToLevel
			),
			// Up Specials
			(int)MeleeIds.Ryuenjin => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisRyuenjin, player, 3, 0, 0.2f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Denjin => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisDenjin, player, 1, Global.defFlinch, 0.09f,
				addToLevel: addToLevel
			),
			// Down specials
			(int)MeleeIds.Hyouretsuzan => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisHyouretsuzan, player, 2, Global.halfFlinch, 0.5f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Rakukojin => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisRakukojin, player, 2, Global.halfFlinch, 0.5f,
				addToLevel: addToLevel
			),
			// Others
			(int)MeleeIds.LadderSlash => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaberLadder, player, 3, 0, 0.25f, isReflectShield: true,
				addToLevel: addToLevel
			),
			(int)MeleeIds.WallSlash => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaberslide, player, 3, 0, 0.25f, isReflectShield: true,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Gokumonken => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSwordBlock, player, 0, 0, 0, isReflectShield: true,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Hadangeki => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.IrisSaberProjSwing, player,
				3, Global.defFlinch, 0.5f, isReflectShield: true,
				addToLevel: addToLevel
			),
			_ => null
		};
	}
	public void inputUpdate() {
		if (shootPressTime > 0) {
			shootPressTime--;
		}
		if (specialPressTime > 0) {
			specialPressTime--;
		}
		if (player.input.isPressed(Control.Shoot, player)) {
			shootPressTime = 6;
		}
		if (player.input.isPressed(Control.Special1, player)) {
			specialPressTime = 6;
		}
	}
	public override string getSprite(string spriteName) {
		return "iris_" + spriteName;
	}
	public override float getLabelOffY() {
		if (sprite.name.Contains("_ra_")) {
			return 25;
		}
		return 45;
	}
	public override void render(float x, float y) {
		base.render(x, y);
	}
	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;
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
		customData.Add((byte)MathF.Floor(IrisRakuhouhaWeapon.ammo));
		customData.Add((byte)MathF.Floor(IrisRekkohaWeapon.ammo));

		customData.Add(Helpers.boolArrayToByte([
			isHyperIris
		]));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-player data.
		IrisRakuhouhaWeapon.ammo = data[0];
		IrisRekkohaWeapon.ammo = data[1];
		bool[] flags = Helpers.byteToBoolArray(data[2]);
		isHyperIris = flags[0];
	}
	
}
public class ZSaberIris : Weapon {
	public static ZSaberIris staticWeapon = new();
	public ZSaberIris() : base() {
		index = (int)WeaponIds.IrisZSaber;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 48;
		killFeedIndex = 9;
		description = new string[] { "" };
	}
}
public class IrisBuster : Weapon {
	public static IrisBuster netWeapon = new();

	public IrisBuster() : base() {
		index = (int)WeaponIds.IrisBuster;
		killFeedIndex = 4;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 0;
		shootSounds = new string[] { "buster", "buster2", "buster3", "buster4" };
		rateOfFire = 0.15f;
		description = new string[] { "Shoot uncharged Z-Buster with ATTACK." };
	}
}
public class IrisRakuhouha : Weapon {
	public static IrisRakuhouha netWeapon = new();
	public IrisRakuhouha() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		ammo = 0;
		maxAmmo = 28;
		rateOfFire = 1;
		index = (int)WeaponIds.IrisRakuhouha;
		weaponBarBaseIndex = 27;
		weaponBarIndex = 35;
		killFeedIndex = 16;
		weaponSlotIndex = 51;
		displayName = "Rakuhouha";
		description = new string[] { "" };
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 14;
	}
}
public class IrisRekkoha : Weapon {
	public static IrisRekkoha netWeapon = new();
	public IrisRekkoha() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		ammo = 0;
		maxAmmo = 28;
		rateOfFire = 1;
		index = (int)WeaponIds.IrisRekkoha;
		weaponBarBaseIndex = 27;
		weaponBarIndex = 33;
		killFeedIndex = 16;
		weaponSlotIndex = 51;
		displayName = "Rekkoha";
		description = new string[] { "" };
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 28;
	}
}
public abstract class IrisGenericMeleeState : CharState {
	public Iris iris = null!;

	public int comboFrame = Int32.MaxValue;

	public string sound = "";
	public bool soundPlayed;
	public int soundFrame = Int32.MaxValue;
	public bool exitOnOver = true;

	public IrisGenericMeleeState(string spr) : base(spr) {
	}

	public override void update() {
		base.update();
		if (character.sprite.frameIndex >= soundFrame && !soundPlayed) {
			character.playSound(sound, forcePlay: false, sendRpc: true);
			soundPlayed = true;
		}
		if (character.sprite.frameIndex >= comboFrame) {
			altCtrls[0] = true;
		}
		if (exitOnOver && character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.turnToInput(player.input, player);
		iris = character as Iris ?? throw new NullReferenceException();
	}

	public virtual bool altCtrlUpdate(bool[] ctrls) {
		return false;
	}
}
public class IrisSlash1State : IrisGenericMeleeState {
	public IrisSlash1State() : base("saber1") {
		sound = "saber1";
		soundFrame = 4;
		comboFrame = 6;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}


	public override bool altCtrlUpdate(bool[] ctrls) {
		if (iris.shootPressed || player.isAI) {
			iris.shootPressTime = 0;
			iris.changeState(new IrisSlash2State(), true);
			return true;
		}
		return false;
	}
}
public class IrisSlash2State : IrisGenericMeleeState {
	public IrisSlash2State() : base("saber2") {
		sound = "saber2";
		soundFrame = 1;
		comboFrame = 3;
	}

	public override bool altCtrlUpdate(bool[] ctrls) {
		if (iris.shootPressed || player.isAI) {
			iris.shootPressTime = 0;
			iris.changeState(new IrisSlash3State(), true);
			return true;
		}
		return false;
	}
}
public class IrisSlash3State : IrisGenericMeleeState {
	public IrisSlash3State() : base("saber3") {
		sound = "saber3";
		soundFrame = 1;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}
}
public class IrisAirSlashState : IrisGenericMeleeState {
	public IrisAirSlashState() : base("saber_air") {
		sound = "saber1";
		soundFrame = 3;
		comboFrame = 8;

		airMove = true;
		canJump = true;
		exitOnLanding = true;
		useDashJumpSpeed = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (character.sprite.frameIndex >= comboFrame) {
			attackCtrl = true;
		}
	}
}
public class IrisRollingSlashtate : IrisGenericMeleeState {
	public IrisRollingSlashtate() : base("kuuenzan") {
		sound = "saber1";
		soundFrame = 1;
		comboFrame = 7;

		airMove = true;
		canJump = true;
		exitOnLanding = true;
		useDashJumpSpeed = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (iris.sprite.loopCount >= 1) {
			character.changeToIdleOrFall();
			return;
		}
	}
}
public class IrisCrouchSlashState : IrisGenericMeleeState {
	public IrisCrouchSlashState() : base("saber_crouch") {
		sound = "saber1";
		soundFrame = 1;
	}
}
public class IrisDashSlashState : IrisGenericMeleeState {
	public IrisDashSlashState() : base("saber_dash") {
		sound = "saber1";
		soundFrame = 1;
	}
}
public class IrisShippuugaState : IrisGenericMeleeState {
	public IrisShippuugaState() : base("shippuga") {
		sound = "saber1";
		soundFrame = 1;
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		altCtrls[1] = true;
	}
}
public class IrisRyuenjinState : IrisGenericMeleeState {
	float timeInWall;
	public IrisRyuenjinState() : base("ryuenjin") {
		sound = "ryuenjin";
		soundFrame = 1;
	}
	public override void update() {
		base.update(); 
		if (character.sprite.frameIndex >= 4 && character.sprite.frameIndex < 6) {
			character.dashedInAir++;
			float ySpeedMod = 1f;
			character.vel.y = -character.getJumpPower() * ySpeedMod;
		}
		if (character.sprite.frameIndex >= 4 && character.sprite.frameIndex < 7) {
			float speed = 100;
			character.move(new Point(character.xDir * speed, 0));
		}
		var wallAbove = Global.level.checkTerrainCollisionOnce(character, 0, -10);
		if (wallAbove != null && wallAbove.gameObject is Wall) {
			timeInWall += Global.spf;
			if (timeInWall > 0.1f) {
				character.changeState(new Fall());
				return;
			}
		}		
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
public class IrisDenjinState : IrisGenericMeleeState {
	float timeInWall;
	public IrisDenjinState() : base("eblade") {
		sound = "raijingeki";
		soundFrame = 11;
	}
	public override void update() {
		base.update(); 

		if (character.sprite.frameIndex >= 12 && character.sprite.frameIndex < 14) {
			character.dashedInAir++;
			float ySpeedMod = 1.025f;
			character.vel.y = -character.getJumpPower() * ySpeedMod;
		}
		if (character.sprite.frameIndex >= 12 && character.sprite.frameIndex < 16) {
			float speed = 100;
			character.move(new Point(character.xDir * speed, 0));
		}
		var wallAbove = Global.level.checkTerrainCollisionOnce(character, 0, -10);
		if (wallAbove != null && wallAbove.gameObject is Wall) {
			timeInWall += Global.spf;
			if (timeInWall > 0.1f) {
				character.changeState(new Fall());
				return;
			}
		}		
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
public class IrisRaijingekiState : IrisGenericMeleeState {
	public IrisRaijingekiState() : base("raijingeki") {
		sound = "raijingeki";
		soundFrame = 9;
	}
}
public class IrisHyouretsuzanState : IrisGenericMeleeState {
	public IrisHyouretsuzanState() : base("hyouretsuzan") {
		sound = "saber1";
		soundFrame = 1;
	}
	public override void update() {
		base.update(); 
		if (character.grounded) {
			character.changeState(new IrisDownThrustLandH(), true);
		}
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (character.vel.y < 0) {
			character.vel.y = 0;
		}
	}
}
public class IrisRakukojinState : IrisGenericMeleeState {
	public IrisRakukojinState() : base("rakukojin") {
		sound = "saber1";
		soundFrame = 1;
	}
	public override void update() {
		base.update(); 
		if (character.grounded) {
			character.changeState(new IrisDownThrustLandR(), true);
		}
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (character.vel.y < 0) {
			character.vel.y = 0;
		}
	}
}
public class IrisDownThrustLandR : IrisGenericMeleeState {
	public IrisDownThrustLandR() : base("downthrust_land") {
		exitOnAirborne = true;		
	}
	public override void update() {
		base.update(); 
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		altCtrls[1] = true;
		character.playSound("land", sendRpc: true);
		character.playSound("swordthud", sendRpc: true);
	}
}
public class IrisDownThrustLandH : IrisGenericMeleeState {
	public IrisDownThrustLandH() : base("downthrust_land") {
		exitOnAirborne = true;
	}
	public override void update() {
		base.update(); 
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		altCtrls[1] = true;
		character.playSound("land", sendRpc: true);
		character.breakFreeze(player, character.pos.addxy(character.xDir * 5, 0), sendRpc: true);
	}
}
public class IrisRakuhouhaState : IrisGenericMeleeState {
	public bool fired;
	public int loop;
	public float time;
	public float timerakuhouha;
	public IrisRakuhouhaState() : base("rakuhouha") {
		sound = "crash";
		soundFrame = 6;
		invincible = true;
	}
	public override void update() {
		base.update();
		float x = character.pos.x;
		float y = character.pos.y-8;
		int xDir = character.getShootXDir();
		if (character.frameIndex == 7 && loop < 4) {
			character.frameIndex = 5;
			character.shakeCamera(sendRpc: true);
			loop++;
		}
		if (character.frameIndex == 5 && !fired) {
			for (int i = 256; i >= 128; i -= 16) {
				new IrisRakuhouhaProj(new Point(x, y), xDir, i,
				 player, player.getNextActorNetId(), rpc: true);
			}
			fired = true;
		}
	}
	public override void onExit(CharState newState) {
		IrisRakuhouha.netWeapon.shootTime = IrisRakuhouha.netWeapon.rateOfFire;
		base.onExit(newState);
	}
}
public class IrisRekkohaState : IrisGenericMeleeState {
	bool fired1 = false;
	bool fired2 = false;
	bool fired3 = false;
	bool fired4 = false;
	bool fired5 = false;
	int loop;
	public RekkohaEffect? effect;
	public IrisRekkohaState() : base("rakuhouha") {
		sound = "rekkoha";
		soundFrame = 5;
		invincible = true;
	}
	public override void update() {
		base.update();
		float topScreenY = Global.level.getTopScreenY(character.pos.y);
		if (character.frameIndex == 7 && loop < 19) {
			character.frameIndex = 5;
			character.shakeCamera(sendRpc: true);
			loop++;
		}
		if (stateTime > 26/60f && !fired1) {
			fired1 = true;
			new IrisRekkohaProj(new Point(character.pos.x, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 0.6f && !fired2) {
			fired2 = true;
			new IrisRekkohaProj(new Point(character.pos.x - 35, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new IrisRekkohaProj(new Point(character.pos.x + 35, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 0.8f && !fired3) {
			fired3 = true;
			new IrisRekkohaProj(new Point(character.pos.x - 70, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new IrisRekkohaProj(new Point(character.pos.x + 70, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 1f && !fired4) {
			fired4 = true;
			new IrisRekkohaProj(new Point(character.pos.x - 110, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new IrisRekkohaProj(new Point(character.pos.x + 110, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 1.2f && !fired5) {
			fired5 = true;
			new IrisRekkohaProj(new Point(character.pos.x - 150, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new IrisRekkohaProj(new Point(character.pos.x + 150, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.isMainPlayer) {
			effect = new RekkohaEffect();
		}
	}

	public override void onExit(CharState newState) {
		IrisRekkoha.netWeapon.shootTime = IrisRekkoha.netWeapon.rateOfFire;
		base.onExit(newState);
	}
}
public class IrisWallSlideSaberState : IrisGenericMeleeState {
	public int wallDir;
	public float dustTime;
	public Collider wallCollider;
	public IrisWallSlideSaberState(int wallDir, Collider wallCollider) : base("wall_slide_attack") {
		sound = "saber1";
		soundFrame = 1;
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		useGravity = false;
	}
	public override void update() {
		base.update();
		character.move(new Point(0, 100));
		if (character.isAnimOver()) {
			character.changeState(new WallSlide(wallDir, wallCollider));
			character.sprite.frameIndex = character.sprite.totalFrameNum - 1;
		}
	}
	public override void onExit(CharState newState) {
		character.useGravity = true;
		base.onExit(newState);
	}
}
public class IrisLadderSaberState : IrisGenericMeleeState {
	public IrisLadderSaberState() : base("ladder_attack") {
		sound = "saber1";
		soundFrame = 1;
	}
}
public class IrisRakuhouhaProj : Projectile {
	public IrisRakuhouhaProj(
		Point pos, int xDir, float byteAngle, Player player, ushort? netId, bool rpc = false) 
		: base(IrisRakuhouha.netWeapon, pos, 1, 300, 3, player, "iris_rakuhouha_proj",
		 Global.defFlinch, 1f, netId, player.ownedByLocalPlayer
	) {
		byteAngle = byteAngle % 256;
		fadeSprite = "buster4_fade";
		reflectable = true;
		projId = (int)ProjIds.IrisRakuhouha;
		maxTime = 0.5f;
		fadeOnAutoDestroy = true;
		vel.x = 300 * Helpers.cosb(byteAngle);
		vel.y = 300 * Helpers.sinb(byteAngle);
		this.byteAngle = byteAngle;
		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new IrisRakuhouhaProj(
			args.pos, args.xDir, args.byteAngle, args.player, args.netId
		);
	}
}
public class IrisRekkohaProj : Projectile {
	float len = 0;
	private float reverseLen;
	private bool updatedDamager = false;
	public IrisRekkohaProj(Point pos, Player player, ushort? netId, bool rpc = false
	) : base(
		IrisRekkoha.netWeapon, pos, 1, 0, 4, player, "rekkoha_proj",
		Global.defFlinch, 0.45f, netId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.IrisRekkoha;
		vel.y = 400;
		maxTime = 1.6f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
		netcodeOverride = NetcodeModel.FavorDefender;
		isOwnerLinked = true;
		if (player.character != null) {
			owningActor = player.character;
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new IrisRekkohaProj(
			args.pos, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		len += Global.spf * 300;
		if (time >= 1f && !updatedDamager) {
			updateLocalDamager(0, 0);
			updatedDamager = true;
		}
		if (len >= 200) {
			len = 200;
			reverseLen += Global.spf * 200 * 2;
			if (reverseLen > 200) {
				reverseLen = 200;
			}
			vel.y = 100;
		}
		Rect newRect = new Rect(0, 0, 16, 60 + len - reverseLen);
		globalCollider = new Collider(newRect.getPoints(), true, this, false, false, 0, new Point(0, 0));
	}
	public override void render(float x, float y) {
		float finalLen = len - reverseLen;

		float newAlpha = 1;
		if (len <= 50) {
			newAlpha = len / 50;
		}
		if (reverseLen >= 100) {
			newAlpha = (200 - reverseLen) / 100;
		}

		alpha = newAlpha;

		float basePosY = System.MathF.Floor(pos.y + y);

		Global.sprites["rekkoha_proj_mid"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY - 63f,
			1, 1, null, alpha,
			1f, finalLen / 23f + 0.01f, zIndex
		);
		Global.sprites["rekkoha_proj_top"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY - 63f - finalLen,
			1, 1, null, alpha,
			1f, 1f, zIndex
		);
		Global.sprites["rekkoha_proj"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY,
			1, 1, null, alpha,
			1f, 1f, zIndex
		);
	}
}
public class IrisBusterProj : Projectile {
	bool deflected;
	float partTime;
	public IrisBusterProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		IrisBuster.netWeapon, pos, xDir,
		80, 1, player, "iris_busterproj", 0, 0,
		netId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster4_fade";
		fadeOnAutoDestroy = true;
		reflectable = true;
		maxTime = 0.575f;
		projId = (int)ProjIds.Irisbuster;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (!deflected && System.MathF.Abs(vel.x) < 360) {
			vel.x += Global.spf * xDir * 550;
			if (System.MathF.Abs(vel.x) >= 360) {
				vel.x = (float)xDir * 360;
			}
		}
		partTime += Global.speedMul;
		if (partTime >= 7) {
			partTime = 0;
			new Anim(
				pos.addxy(-20 * xDir, 0).addRand(0, 2),
				"iris_buster3effect", 1, null, true
			) {
				vel = vel,
				acc = new Point(-vel.x * 1, 0)
			};
		}
	}

	public override void onDeflect() {
		base.onDeflect();
		deflected = true;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new IrisBusterProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}
}

public class IrisBuster2Proj : Projectile {
	bool deflected;
	float partTime;
	public IrisBuster2Proj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		IrisBuster.netWeapon, pos, xDir,
		100, 2, player, "iris_buster2proj", 0, 0,
		netId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.65f;
		projId = (int)ProjIds.Irisbuster2;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}
	public override void update() {
		base.update();
		if (!deflected && System.MathF.Abs(vel.x) < 400) {
			vel.x += Global.spf * xDir * 500;
			if (System.MathF.Abs(vel.x) >= 400) {
				vel.x = (float)xDir * 400;
			}
		}
		partTime += Global.speedMul;
		if (partTime >= 6) {
			partTime = 0;
			new Anim(
				pos.addxy(-20 * xDir, 0).addRand(0, 4),
				"iris_buster3effect", 1, null, true
			) {
				vel = vel,
				acc = new Point(-vel.x * 1, 0)
			};
		}
	}

	public override void onDeflect() {
		base.onDeflect();
		deflected = true;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new IrisBuster2Proj(
			args.pos, args.xDir, args.player, args.netId
		);
	}
}

public class IrisBuster3Proj : Projectile {
	float partTime;
	bool deflected;
	public IrisBuster3Proj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		IrisBuster.netWeapon, pos, xDir,
		100, 3, player, "iris_buster3proj", Global.halfFlinch, 0,
		netId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.7f;
		projId = (int)ProjIds.Irisbuster3;
		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public override void update() {
		base.update();
		partTime += Global.speedMul;
		if (partTime >= 4) {
			partTime = 0;
			new Anim(
				pos.addxy(-20 * xDir, 0).addRand(0, 12),
				"iris_buster3effect", 1, null, true
			) {
				vel = vel,
				acc = new Point(-vel.x * 2, 0)
			};
		}
		if (!deflected && System.MathF.Abs(vel.x) < 460) {
			vel.x += Global.spf * xDir * 500;
			if (System.MathF.Abs(vel.x) >= 460) {
				vel.x = (float)xDir * 400;
			}
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new IrisBuster3Proj(
			args.pos, args.xDir, args.player, args.netId
		);
	}
}

public class IrisHadangekiProj : Projectile {
	public IrisHadangekiProj(
		Point pos, int xDir, bool isHyperIris, Player player, ushort? netId, bool rpc = false
	) : base(
		IrisBuster.netWeapon, pos, xDir,
		350, 3, player, "zsaber_shot", 0, 0,
		netId, player.ownedByLocalPlayer
	) {
		fadeOnAutoDestroy = true;
		fadeSprite = "zsaber_shot_fade";
		reflectable = true;
		projId = (int)ProjIds.IrisHadangekiProj;
		maxTime = 0.5f;
		if (isHyperIris) {
			damager.damage = 4;
			genericShader = player.zeroPaletteShader;
		}
		if (rpc) {
			rpcCreate(pos, player, netId, xDir, (isHyperIris ? (byte)0 : (byte)1));
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new IrisHadangekiProj(
			args.pos, args.xDir, args.extraData[0] == 1, args.player, args.netId
		);
	}
}
public class IrisDoubleBuster : CharState {
	public bool fired1;
	public bool fired2;
	public bool isSecond;
	public bool shootPressedAgain;
	public bool isPinkCharge;
	Iris iris = null!;

	public IrisDoubleBuster(bool isSecond, bool isPinkCharge) : base("doublebuster") {
		this.isSecond = isSecond;
		this.isPinkCharge = isPinkCharge;
		airMove = true;
		superArmor = true;
		landSprite = "doublebuster";
		airSprite = "doublebuster_air";
	}

	public override void update() {
		base.update();
		if (player.input.isPressed(Control.WeaponRight, player)) {
			shootPressedAgain = true;
		}
		if (!fired1 && character.frameIndex == 2) {
			fired1 = true;
			character.playSound("buster3X3", sendRpc: true);
			new IrisBuster3Proj(
				character.getShootPos(), character.getShootXDir(),
				player, player.getNextActorNetId(), rpc: true
			);
		}
		if (!fired2 && character.frameIndex == 6) {
			fired2 = true;
			if (!isPinkCharge) {
				iris.stockedBusterLv = 0;
				character.playSound("buster3X3", sendRpc: true);
				new IrisBuster3Proj(
					character.getShootPos(), character.getShootXDir(),
					player, player.getNextActorNetId(), rpc: true
				);
			} else {
				iris.stockedBusterLv = 0;
				character.playSound("buster2X3", sendRpc: true);
				new IrisBuster2Proj(
					character.getShootPos(), character.getShootXDir(),
					player, player.getNextActorNetId(), rpc: true
				);
			}
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else if (!isSecond && character.frameIndex >= 4 && !shootPressedAgain) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded || character.canAirJump() && character.flag == null) &&
				player.input.isPressed(Control.Jump, player)
			) {
				if (!character.grounded) {
					character.dashedInAir++;
				}
				character.vel.y = -character.getJumpPower();
				sprite = "doublebuster_air";
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		iris = character as Iris ?? throw new NullReferenceException();
		// Non-full charge.
		if (isPinkCharge) {
			iris.stockedBusterLv = 1;
		}
		// Full charge.
		else {
			// We add Z-Saber charge if we fire the full charge and we were at 0 charge before.
			if (iris.stockedBusterLv != 2 || !isSecond) {
				iris.stockedSaber = true;
			}
			iris.stockedBusterLv = 2;
		}
		if (!character.grounded || character.vel.y < 0) {
			sprite = "doublebuster_air";
			character.changeSpriteFromName(sprite, true);
		}
		if (isSecond) {
			character.frameIndex = 4;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		// We check if we fired the second shot. If not we add the stocked charge.
		if (!fired2) {
			if (isPinkCharge) {
				iris.stockedBusterLv = 1;
			} else {
				iris.stockedBusterLv = 2;
				iris.stockedSaber = true;
			}
		}
	}
}
public class IrisHadangeki : CharState {
	bool fired;
	public Iris iris = null!;

	public IrisHadangeki() : base("saber") {
		landSprite = "saber";
		airSprite = "saber_air2";
		airMove = true;
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 7 && !fired) {
			character.playSound("zerosaberx3", sendRpc: true);
			iris.stockedSaber = false;
			fired = true;
			new IrisHadangekiProj(
				character.pos.addxy(30 * character.xDir, -20), character.xDir,
				iris.isHyperIris, player, player.getNextActorNetId(), rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded || character.canAirJump() && character.flag == null) &&
				player.input.isPressed(Control.Jump, player)
			) {
				if (!character.grounded) {
					character.dashedInAir++;
				}
				character.vel.y = -character.getJumpPower();
				sprite = "saber_air2";
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		iris = character as Iris ?? throw new NullReferenceException();
		if (!character.grounded || character.vel.y < 0) {
			sprite = "saber_air2";
			defaultSprite = sprite;
			character.changeSpriteFromName(sprite, true);
		}
	}
}
public class IrisHadangekiWall : CharState {
	bool fired;
	public Iris iris = null!;
	public int wallDir;
	public Collider wallCollider;

	public IrisHadangekiWall(int wallDir, Collider wallCollider) : base("wall_slide_attack") {
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		superArmor = true;
		useGravity = false;
	}

	public override void update() {
		base.update();
		if (character.frameIndex >= 4 && !fired) {
			character.playSound("zerosaberx3", sendRpc: true);
			iris.stockedSaber = false;
			fired = true;
			new IrisHadangekiProj(
				character.pos.addxy(30 * -wallDir, -20), -wallDir,
				iris.isHyperIris, player, player.getNextActorNetId(), rpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeState(new WallSlide(wallDir, wallCollider));
			character.sprite.frameIndex = character.sprite.totalFrameNum - 1;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		iris = character as Iris ?? throw new NullReferenceException();
	}

	public override void onExit(CharState oldState) {
		base.onExit(oldState);
		useGravity = true;
	}
}
public class HyperIrisStart : CharState {
	public float radius = 200;
	public float time;
	Iris iris = null!;
	Anim? LightX3;

	public HyperIrisStart() : base("hyper_start") {
		invincible = true;
	}

	public override void update() {
		base.update();
		if (time == 0) {
			if (radius >= 0) {
				radius -= Global.spf * 200;
			} else {
				time = Global.spf;
				radius = 0;
				character.playSound("ching");
				character.fillHealthToMax();
			}
		} else {
			time += Global.spf;
			if (time >= 1) {
				character.changeToLandingOrFall();
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		iris = character as Iris ?? throw new NullReferenceException();
		character.useGravity = false;
		character.vel = new Point();
		if (iris == null) {
			throw new NullReferenceException();
		}
		LightX3 = new Anim(
				character.pos.addxy(50 * character.xDir, 0f),
				"iris_colonel", -character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: false, sendRpc: true
			);
		LightX3.blink = true;
		character.player.currency -= 10;
		character.playSound("blackzeroentry", forcePlay: false, sendRpc: true);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		LightX3?.destroySelf();
		character.useGravity = true;
		if (character != null) {
			character.invulnTime = 0.5f;
		}
		iris.hyperModeTimer = 44f;
		iris.isHyperIris = true;
	}

	public override void render(float x, float y) {
		base.render(x, y);
		Point pos = character.getCenterPos();
		DrawWrappers.DrawCircle(
			pos.x + x, pos.y + y, radius, false, Color.White, 5, character.zIndex + 1, true, Color.White
		);
	}
}
