using System;
using System.Collections.Generic;

namespace MMXOnline;

public class PunchyZero : Character {
	// Hypermode stuff.
	public bool isViral;
	public int awakenedPhase;
	public bool isAwakened => (awakenedPhase != 0);
	public bool isGenmuZero => (awakenedPhase >= 2);
	public bool isBlack;
	public int hyperMode;

	// Hypermode timers.
	public static readonly float maxBlackZeroTime = 20 * 60;
	public float hyperModeTimer;
	public float scrapDrainCounter = 120;
	public bool hyperOvertimeActive;
	
	// Hypermode effects stuff.
	public int awakenedAuraFrame;
	public float awakenedAuraAnimTime;
	public byte hypermodeBlink;

	// Weapons.
	public PunchyZeroMeleeWeapon meleeWeapon = new();
	public PZeroParryWeapon parryWeapon = new();
	public Weapon gigaAttack;
	public AwakenedAura awakenedAuraWeapon = new();
	public ZSaber saberSwingWeapon = new();
	public ZeroBuster busterWeapon = new();
	public int gigaAttackSelected;
	
	// Inputs.
	public int shootPressTime;
	public int parryPressTime;
	public int swingPressTime;
	public int specialPressTime;

	// Cooldowns.
	public float dashAttackCooldown;
	public float diveKickCooldown;
	public float uppercutCooldown;
	public float parryCooldown;
	public float hadangekiCooldown;
	public float genmureiCooldown;

	// Hypermode stuff.
	public float donutTimer = 0;
	public int donutsPending = 0;
	public int freeBusterShots = 0;

	// Creation code.
	public PunchyZero(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.PunchyZero;
		// Loadout stuff.
		PZeroLoadout pzeroLoadout = player.loadout.pzeroLoadout;

		gigaAttackSelected = pzeroLoadout.gigaAttack;
		gigaAttack = pzeroLoadout.gigaAttack switch {
			1 => new Messenkou(),
			2 => new RekkohaWeapon(),
			_ => new RakuhouhaWeapon(),
		};
		hyperMode = pzeroLoadout.hyperMode;
		altSoundId = AltSoundIds.X3;
	}

	public override CharState getAirJumpState() => new Jump() { sprite = "kuuenbu" };
	public override CharState getJumpState() {
		if (isDashing) {
			return new Jump() { sprite = "kuuenbu" };
		}
		return new Jump();
	}

	public override void update() {
		if (isAwakened) {
			updateAwakenedAura();
		}
		if (!Global.level.isHyper1v1()) {
			if (isBlack) {
				if (musicSource == null) {
					addMusicSource("zero_X1", getCenterPos(), true);
				}
			} else if (isAwakened) {
				if (musicSource == null) {
					addMusicSource("XvsZeroV2_megasfc", getCenterPos(), true);
				}
			} else if (isViral && ownedByLocalPlayer) {
				if (musicSource == null) {
					addMusicSource("introStageZeroX5_megasfc", getCenterPos(), true);
				}
			} else {
				destroyMusicSource();
			}
		}
		if (!ownedByLocalPlayer) {
			base.update();
			return;
		}

		// Local update starts here.
		inputUpdate();
		Helpers.decrementFrames(ref donutTimer);
		Helpers.decrementFrames(ref hadangekiCooldown);
		Helpers.decrementFrames(ref parryCooldown);
		Helpers.decrementFrames(ref dashAttackCooldown);
		Helpers.decrementFrames(ref diveKickCooldown);
		Helpers.decrementFrames(ref uppercutCooldown);
		Helpers.decrementFrames(ref aiAttackCooldown);
		gigaAttack.update();
		gigaAttack.charLinkedUpdate(this, true);
		base.update();

		// Hypermode timer.
		if (hyperModeTimer > 0) {
			hyperModeTimer -= Global.speedMul;
			if (hyperModeTimer <= 180) {
				hypermodeBlink = (byte)MathInt.Ceiling(hyperModeTimer - 180);
			}
			if (hyperModeTimer <= 0) {
				hypermodeBlink = 0;
				hyperModeTimer = 0;
				if (hyperOvertimeActive && isAwakened && player.currency >= 4) {
					awakenedPhase = 2;
					heal(player, awakenedPhase, true);
					gigaAttack.addAmmoPercentHeal(100);
				} else {
					awakenedPhase = 0;
					isBlack = false;
					float oldAmmo = gigaAttack.ammo;
					gigaAttack = gigaAttackSelected switch {
						1 => new Messenkou(),
						2 => new RekkohaWeapon(),
						_ => new RakuhouhaWeapon(),
					};
					gigaAttack.ammo = oldAmmo;
				}
				hyperOvertimeActive = false;
			}
		}
		// Genmu Zero scrap drain.
		else if (awakenedPhase == 2) {
			if (scrapDrainCounter > 0) {
				scrapDrainCounter--;
			} else {
				scrapDrainCounter = 120;
				player.currency--;
				if (player.currency < 0) {
					player.currency = 0;
					awakenedPhase = 0;
					isBlack = false;
					hyperOvertimeActive = false;
				}
			}
		}
		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= Global.speedMul;
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
		// For the donuts.
		if (donutsPending > 0 && donutTimer <= 0) {
			shootDonutProj(donutsPending * 9);
			donutsPending--;
			donutTimer = 9;
		}
		// Charge and release charge logic.
		if (isAwakened) {
			chargeLogic(shootDonuts);
		} else {
			chargeLogic(shoot);
		}
	}

	public override bool canCharge() {
		return (player.currency > 0 || freeBusterShots > 0) && donutsPending == 0 && !isInvulnerableAttack();
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public void setShootAnim() {
		string shootSprite = getSprite(charState.shootSpriteEx);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = "zero_shoot"; }
			else { shootSprite = "zero_fall_shoot"; }
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
	}

	public void shoot(int chargeLevel) {
		if (player.currency <= 0 && freeBusterShots <= 0) { return; }
		if (chargeLevel == 0) { return; }
		int currencyUse = 0;

		// Cancel non-invincible states.
		if (!charState.attackCtrl && !charState.invincible) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		setShootAnim();
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		// Shoot stuff.
		if (chargeLevel == 1) {
			currencyUse = 1;
			playSound("buster2", sendRpc: true);
			new ZBuster2Proj(
				shootPos, xDir, this, player, player.getNextActorNetId(), rpc: true
			);
		} else if (chargeLevel == 2) {
			currencyUse = 1;
			playSound("buster2X3", sendRpc: true);
			new ZBuster3Proj(
				shootPos, xDir, this, player, player.getNextActorNetId(), rpc: true
			);
		} else if (chargeLevel == 3 || chargeLevel >= 4) {
			currencyUse = 1;
			playSound("buster3X3", sendRpc: true);
			new ZBuster4Proj(
				shootPos, xDir, this, player, player.getNextActorNetId(), rpc: true
			);
		}
		if (currencyUse > 0) {
			if (freeBusterShots > 0) {
				freeBusterShots--;
			} else if (player.currency > 0) {
				player.currency--;
			}
		}
	}

	public void shootDonuts(int chargeLevel) {
		if (player.currency <= 0 && freeBusterShots <= 0) { return; }
		if (chargeLevel == 0) { return; }
		int currencyUse = 0;

		// Cancel non-invincible states.
		if (!charState.attackCtrl && !charState.invincible) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		setShootAnim();
		shootDonutProj(0);
		if (chargeLevel >= 2) {
			donutTimer = 9;
			donutsPending = (chargeLevel - 1);
		}
		currencyUse = 1;
		if (currencyUse > 0) {
			if (player.currency > 0) {
				player.currency--;
			}
		}
	}

	public void shootDonutProj(int time) {
		setShootAnim();
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		new ShingetsurinProj(
			shootPos, xDir,
			time / 60f, this, player, player.getNextActorNetId(), rpc: true
		);
		playSound("shingetsurinx5", forcePlay: false, sendRpc: true);
		shootAnimTime = DefaultShootAnimTime;
	}

	public void updateAwakenedAura() {
		awakenedAuraAnimTime += Global.speedMul;
		if (awakenedAuraAnimTime > 4) {
			awakenedAuraAnimTime = 0;
			awakenedAuraFrame++;
			if (awakenedAuraFrame > 3) {
				awakenedAuraFrame = 0;
			}
		}
	}

	public void inputUpdate() {
		if (shootPressTime > 0) {
			shootPressTime--;
		}
		if (specialPressTime > 0) {
			specialPressTime--;
		}
		if (parryPressTime > 0) {
			parryPressTime--;
		}
		if (swingPressTime > 0) {
			swingPressTime--;
		}
		if (player.input.isPressed(Control.Shoot, player)) {
			shootPressTime = 6;
		}
		if (player.input.isPressed(Control.Special1, player)) {
			specialPressTime = 6;
		}
		if (player.input.isPressed(Control.WeaponLeft, player) ||
			player.input.isPressed(Control.WeaponRight, player) && !isAwakened
		) {
			parryPressTime = 6;
		}
		if (player.input.isPressed(Control.WeaponRight, player) && isAwakened) {
			swingPressTime = 6;
		}
	}

	public override bool normalCtrl() {
		// Hypermode activation.
		int cost = Player.zeroHyperCost;
		if (isAwakened) {
			cost = 4;
		}
		if (player.currency >= cost &&
			player.input.isHeld(Control.Special2, player) &&
			charState is not HyperPunchyZeroStart and not WarpIn && (
				!isViral && !isAwakened && !isBlack ||
				isAwakened && !hyperOvertimeActive
			)
		) {
			hyperProgress += Global.spf;
		} else {
			hyperProgress = 0;
		}
		if (hyperProgress >= 1 && (isViral || isAwakened || isBlack)) {
			hyperProgress = 0;
			hyperOvertimeActive = true;
			Global.level.gameMode.setHUDErrorMessage(player, "Overtime mode active");
		}
		else if (hyperProgress >= 1 && player.currency >= Player.zeroHyperCost) {
			hyperProgress = 0;
			changeState(new HyperPunchyZeroStart(), true);
			return true;
		}
		// Regular states.
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		if (donutsPending != 0) {
			return false;
		}
		if (isAwakened && swingPressTime > 0 && hadangekiCooldown == 0) {
			hadangekiCooldown = 60;
			if (charState is WallSlide wallSlide) {
				changeState(new PunchyZeroHadangekiWall(wallSlide.wallDir, wallSlide.wallCollider), true);
				return true;
			}
			if (isDashing && grounded) {
				slideVel = xDir * getDashSpeed() * 0.9f;
			}
			if (grounded && vel.y >= 0 && isGenmuZero) {
				if (genmureiCooldown == 0) {
					genmureiCooldown = 120;
					changeState(new PunchyZeroGenmureiState(), true);
					return true;
				}
			} else {
				changeState(new AwakenedPunchyZeroHadangeki(), true);
				return true;
			}
			return true;
		}
		if (grounded && vel.y >= 0) {
			return groundAttacks();
		}
		return airAttacks();
	}


	public override bool changeState(CharState newState, bool forceChange = true) {
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

	public override bool altCtrl(bool[] ctrls) {
		if (charState is PZeroGenericMeleeState zgms) {
			return zgms.altCtrlUpdate(ctrls);
		}
		return false;
	}

	public bool groundAttacks() {
		if (parryPressTime > 0 && parryCooldown == 0) {
			changeState(new PZeroParry(), true);
			return true;
		}
		int yDir = player.input.getYDir(player);
		if (isDashing && dashAttackCooldown == 0 &&
			player.input.getYDir(player) == 0 && shootPressTime > 0
		) {
			changeState(new PZeroSpinKick(), true);
			return true;
		}
		if (shootPressTime > 0) {
			if (yDir == -1) {
				changeState(new PZeroShoryuken(), true);
				return true;
			}
			if (grounded && isDashing) {
				slideVel = xDir * getDashSpeed() * 0.8f;
			}
			changeState(new PZeroPunch1(), true);
			return true;
		}
		if (specialPressTime > 0) {
			return groundSpcAttacks();
		}
		return false;
	}

	public bool airAttacks() {
		int yDir = player.input.getYDir(player);
		if (diveKickCooldown == 0 && (shootPressTime > 0 || specialPressTime > 0) && yDir == 1) {
			changeState(new PZeroDiveKickState(), true);
			return true;
		}
		if (shootPressTime > 0) {
			changeState(new PZeroKick(), true);
		}
		return false;
	}

	public bool groundSpcAttacks() {
		int yDir = player.input.getYDir(player);
		if (yDir == -1) {
			changeState(new PZeroShoryuken(), true);
			return true;
		}
		if (yDir == 1) {
			if (gigaAttack.shootCooldown > 0 || gigaAttack.ammo < gigaAttack.getAmmoUsage(0)) {
				return false;
			}
			gigaAttack.shoot(this, []);
			return true;
		}
		if (isDashing) {
			slideVel = xDir * getDashSpeed() * 0.8f;
		}
		changeState(new PZeroYoudantotsu(), true);
		return true;
	}

	public override bool canAirJump() {
		return dashedInAir == 0;
	}

	public override string getSprite(string spriteName) {
		if (Global.sprites.ContainsKey("pzero_" + spriteName)) {
			return "pzero_" + spriteName;
		}
		return "zero_" + spriteName;
	}

	public override bool isToughGuyHyperMode() {
		return isBlack || isGenmuZero;
	}
	public override bool canShoot() {
		if (isInvulnerableAttack()) return false;
		return base.canShoot();
	}
	public bool hypermodeActive() {
		return isBlack || isAwakened || isViral;
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

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;
		if (isBlack) {
			palette = player.zeroPaletteShader;
			palette?.SetUniform("palette", 1);
			palette?.SetUniform("paletteTexture", Global.textures["hyperZeroPalette"]);
		}
		if (isAwakened) {
			palette = player.zeroAzPaletteShader;
		}
		if (isViral) {
			palette = player.nightmareZeroShader;
		}
		if (Global.isOnFrameCycle(4)) {
			switch (getChargeLevel()) {
				case 1:
					palette = Player.ZeroBlueC;
					break;
				case 2:
					palette = Player.ZeroBlueC;
					break;
				case >=3:
					palette = Player.ZeroPinkC;
					break;
			}
		}
		if (palette != null && hypermodeBlink > 0) {
			float blinkRate = MathInt.Ceiling(hypermodeBlink / 30f);
			palette = ((Global.frameCount % (blinkRate * 2) >= blinkRate) ? null : palette);
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

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"zero_punch" => MeleeIds.Punch,
			"zero_punch2" => MeleeIds.Punch2,
			"zero_spinkick" => MeleeIds.Spin,
			"zero_kick_air" => MeleeIds.AirKick,
			"zero_parry_start" => MeleeIds.Parry,
			"zero_parry" => MeleeIds.ParryAttack,
			"zero_shoryuken" => MeleeIds.Uppercut,
			"zero_megapunch" => MeleeIds.StrongPunch,
			"zero_dropkick" => MeleeIds.DropKick,
			"zero_projswing" or "zero_projswing_air" or "zero_wall_slide_attack" => MeleeIds.SaberSwing,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Projectile? proj = id switch {
			(int)MeleeIds.Punch => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroPunch, player,
				2, 0, 15,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Punch2 => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroPunch2, player, 2, Global.halfFlinch, 15,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Spin => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroSenpuukyaku, player, 2, Global.halfFlinch,
				addToLevel: addToLevel
			),
			(int)MeleeIds.AirKick => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroAirKick, player, 3, 0, 15,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Uppercut => new GenericMeleeProj(
				ZeroShoryukenWeapon.staticWeapon, projPos, ProjIds.PZeroShoryuken, player, 4, Global.defFlinch,
				addToLevel: addToLevel
			),
			(int)MeleeIds.StrongPunch => new GenericMeleeProj(
				MegaPunchWeapon.staticWeapon, projPos, ProjIds.PZeroYoudantotsu, player, 6, Global.defFlinch,
				addToLevel: addToLevel
			),
			(int)MeleeIds.DropKick => new GenericMeleeProj(
				DropKickWeapon.staticWeapon, projPos, ProjIds.PZeroEnkoukyaku, player, 4, Global.halfFlinch,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Parry => new GenericMeleeProj(
				parryWeapon, projPos, ProjIds.PZeroParryStart, player, 0, 0, 0,
				addToLevel: addToLevel
			),
			(int)MeleeIds.ParryAttack => (new GenericMeleeProj(
				parryWeapon, projPos, ProjIds.PZeroParryAttack, player, 4, Global.defFlinch,
				addToLevel: addToLevel
			) {
				netcodeOverride = NetcodeModel.FavorDefender
			}),
			(int)MeleeIds.AwakenedAura => (new GenericMeleeProj(
				awakenedAuraWeapon, projPos, ProjIds.AwakenedAura, player, 2, 0,
				addToLevel: addToLevel
			) {
				netcodeOverride = NetcodeModel.FavorDefender
			}),
			(int)MeleeIds.SaberSwing => new GenericMeleeProj(
				saberSwingWeapon, projPos, ProjIds.ZSaberProjSwing, player,
				3, Global.defFlinch, isReflectShield: true,
				addToLevel: addToLevel
			),
			_ => null
		};
		return proj;
	}

	public override Dictionary<int, Func<Projectile>> getGlobalProjs() {
		if (isAwakened && globalCollider != null) {
			Dictionary<int, Func<Projectile>> retProjs = new() {
				[(int)ProjIds.AwakenedAura] = () => {
					playSound("awakenedaura", forcePlay: true, sendRpc: true); 
					Point centerPoint = globalCollider.shape.getRect().center();
					float damage = 2;
					int flinch = 0;
					if (isGenmuZero) {
						damage = 4;
						flinch = Global.defFlinch;
					}
					Projectile proj = new GenericMeleeProj(
						awakenedAuraWeapon, centerPoint,
						ProjIds.AwakenedAura, player, damage, flinch
					) {
						globalCollider = globalCollider.clone(),
						meleeId = (int)MeleeIds.AwakenedAura
					};
					return proj;
				}
			};
			return retProjs;
		}
		return base.getGlobalProjs();
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.AwakenedAura) {
			if (isGenmuZero) {
				proj.damager.damage = 4;
				proj.damager.flinch = Global.defFlinch;
			}
		}
	}

	public enum MeleeIds {
		None = -1,
		Punch,
		Punch2,
		Spin,
		StrongPunch,
		AirKick,
		Uppercut,
		DropKick,
		Parry,
		ParryAttack,
		SaberSwing,
		AwakenedAura
	}

	// For parry purposes.
	public override void onCollision(CollideData other) {
		if (charState.specialId == SpecialStateIds.PZeroParry &&
			other.gameObject is Projectile proj &&
			proj.damager?.owner?.teamAlliance != player.teamAlliance &&
			charState is PZeroParry zeroParry &&
			proj.damager?.damage > 0 &&
			zeroParry.canParry(proj, proj.projId)
		) {
			zeroParry.counterAttack(proj.owner, proj);
			return;
		}
		base.onCollision(other);
	}

	public override void addAmmo(float amount) {
		gigaAttack.addAmmoHeal(amount);
	}

	public override void addPercentAmmo(float amount) {
		gigaAttack.addAmmoPercentHeal(amount);
	}

	public override bool canAddAmmo() {
		return (gigaAttack.ammo < gigaAttack.maxAmmo);
	}

	public override float getRunSpeed() {
		float runSpeed = Physics.WalkSpeed;
		if (isBlack) {
			runSpeed *= 1.15f;
		}
		return runSpeed * getRunDebuffs();
	}

	public override float getLabelOffY() {
		if (sprite.name.Contains("_ra_")) {
			return 25;
		}
		return 45;
	}

	public override void render(float x, float y) {
		if (isViral && visible) {
			addRenderEffect(RenderEffectType.Trail);
		} else {
			removeRenderEffect(RenderEffectType.Trail);
		}
		float auraAlpha = 1;
		if (isAwakened && visible && hypermodeBlink > 0) {
			float blinkRate = MathInt.Ceiling(hypermodeBlink / 2f);
			bool blinkActive = Global.frameCount % (blinkRate * 2) >= blinkRate;
			if (!blinkActive) {
				auraAlpha = 0.5f;
			}
		}
		if (isAwakened && visible) {
			float xOff = 0;
			int auraXDir = 1;
			float yOff = 5;
			string auraSprite = "zero_awakened_aura";
			if (sprite.name.Contains("dash")) {
				auraSprite = "zero_awakened_aura2";
				auraXDir = xDir;
				yOff = 8;
			}
			var shaders = new List<ShaderWrapper>();
			if (isGenmuZero &&
				Global.frameCount % Global.normalizeFrames(6) > Global.normalizeFrames(3) &&
				Global.shaderWrappers.ContainsKey("awakened")
			) {
				shaders.Add(Global.shaderWrappers["awakened"]);
			}
			Global.sprites[auraSprite].draw(
				awakenedAuraFrame,
				pos.x + x + (xOff * auraXDir),
				pos.y + y + yOff, auraXDir,
				1, null, auraAlpha, 1, 1,
				zIndex - 1, shaders: shaders
			);
		}
		base.render(x, y);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();
		customData.Add((byte)MathF.Floor(gigaAttack.ammo));

		customData.Add(Helpers.boolArrayToByte([
			hypermodeBlink > 0,
			isAwakened,
			isGenmuZero,
			isBlack,
			isViral,
		]));
		if (hypermodeBlink > 0) {
			customData.Add(hypermodeBlink);
		}

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-player data.
		gigaAttack.ammo = data[0];
		bool[] flags = Helpers.byteToBoolArray(data[1]);
		awakenedPhase = (flags[2] ? 2 : (flags[1] ? 1 : 0));
		isBlack = flags[3];
		isViral = flags[4];

		if (flags[0]) {
			hypermodeBlink = data[2];
		}
	}
	
	public float aiAttackCooldown;
	public override void aiAttack(Actor? target) {
		bool isTargetInAir = pos.y < target?.pos.y - 20;
		bool isTargetClose = pos.x < target?.pos.x - 10;
		bool canHitMaxCharge = (!isTargetInAir && getChargeLevel() >= 4);
		bool isFacingTarget = (pos.x < target?.pos.x && xDir == 1) || (pos.x >= target?.pos.x && xDir == -1);
		if (player.currency >= Player.zeroHyperCost && !isInvulnerable() &&
		   charState is not (HyperPunchyZeroStart or LadderClimb) && !hypermodeActive() && !player.isMainPlayer
		) {
			changeState(new HyperPunchyZeroStart(), true);
		}
		int ZKattack = Helpers.randomRange(0, 6);
		if (!isInvulnerable() && charState is not LadderClimb && aiAttackCooldown <= 0 && charState.attackCtrl) {
			switch (ZKattack) {
				case 0 when grounded && isFacingTarget:
					changeState(new PZeroPunch1(), true);
					break;
				case 1 when grounded && isFacingTarget:
					changeState(new PZeroShoryuken(), true);
					break;
				case 2 when charState is Dash:
					changeState(new PZeroSpinKick(), true);
					break;
				case 3 when grounded && gigaAttack.shootCooldown <= 0 && gigaAttack.ammo >= gigaAttack.getAmmoUsage(0):
					if (gigaAttack is RekkohaWeapon) {
						gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
						changeState(new PunchyZeroRekkohaState(gigaAttack), true);
					} else if (gigaAttack is RakuhouhaWeapon) {
						gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
						changeState(new PunchyZeroRakuhouhaState(gigaAttack), true);
					}
					else if (gigaAttack is Messenkou) {
						gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
						changeState(new PunchyZeroCFlasherState(gigaAttack), true);
					}
					else if (gigaAttack is ShinMessenkou) {
						gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
						changeState(new PunchyZeroShinMessenkouState(gigaAttack), true);
					}
					else if (gigaAttack is DarkHoldWeapon) {
						gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
						changeState(new PunchyZeroDarkHoldShootState(gigaAttack), true);
					}
					break;
				case 4 when grounded && isFacingTarget:
					changeState(new PZeroYoudantotsu(), true);
					break;
				case 5 when charState is Fall:
					changeState(new PZeroDiveKickState(), true);
					break;
				case 6 when charState is Jump or Fall:
					changeState(new PZeroKick(), true);
					break;
			}
			aiAttackCooldown = 10;
		}
		base.aiAttack(target);
	}
	public override void aiDodge(Actor? target) {
		foreach (GameObject gameObject in getCloseActors(48, true, false, false)) {
			if (gameObject is Projectile proj&& proj.damager.owner.alliance != player.alliance && charState.attackCtrl) {
				//Projectile is not 
				if (!(proj.projId == (int)ProjIds.RollingShieldCharged || proj.projId == (int)ProjIds.RollingShield
					|| proj.projId == (int)ProjIds.MagnetMine || proj.projId == (int)ProjIds.FrostShield || proj.projId == (int)ProjIds.FrostShieldCharged
					|| proj.projId == (int)ProjIds.FrostShieldAir || proj.projId == (int)ProjIds.FrostShieldChargedPlatform || proj.projId == (int)ProjIds.FrostShieldPlatform)
				) {
					if (gigaAttack.shootCooldown <= 0 && grounded) {
						switch (gigaAttack) {
							case RekkohaWeapon when gigaAttack.ammo >= 28:
								gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
								changeState(new PunchyZeroRekkohaState(gigaAttack), true);
								break;
							case Messenkou when gigaAttack.ammo >= 7:
								gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
								changeState(new PunchyZeroCFlasherState(gigaAttack), true);
								break;
							case RakuhouhaWeapon when gigaAttack.ammo >= 14:
								gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
								changeState(new PunchyZeroRakuhouhaState(gigaAttack), true);
								break;
							case DarkHoldWeapon when gigaAttack.ammo >= 14 && isViral:
								gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
								changeState(new PunchyZeroDarkHoldShootState(gigaAttack), true);
								break;
							case ShinMessenkou when gigaAttack.ammo >= 14 && isAwakened:
								gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
								changeState(new PunchyZeroShinMessenkouState(gigaAttack), true);
								break;
						}
					} else if (!(proj.projId == (int)ProjIds.SwordBlock) && grounded) {
					turnToInput(player.input, player);
					changeState(new PZeroParry(), true);
				}
				}
			}
		}
		base.aiDodge(target);
	}
}
