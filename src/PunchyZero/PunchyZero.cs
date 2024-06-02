using System;
using System.Collections.Generic;

namespace MMXOnline;

public class PunchyZero : Character {
	// Hypermode stuff.
	public float blackZeroTime;
	public float awakenedZeroTime;
	public bool isViral;
	public bool isAwakened;
	public bool isBlack;
	public bool secondPhaseHyper;
	public byte hypermodeBlink;
	public int hyperMode;
	public int awakenedAuraFrame;
	public float awakenedAuraAnimTime;

	// Weapons.
	public PunchyZeroMeleeWeapon meleeWeapon = new();
	public KKnuckleParry parryWeapon = new();
	public Weapon gigaAttack;
	public AwakenedAura awakenedAuraWeapon = new();
	public ZSaber saberSwingWeapon = new();
	public ZeroBuster busterWeapon = new();
	
	// Inputs.
	public int shootPressTime;
	public int parryPressTime;
	public int swingPressTime;
	public int specialPressTime;

	// Cooldowns.
	public float dashAttackCooldown = 0;
	public float diveKickCooldown = 0;
	public float parryCooldown = 0;
	public float swingCooldown = 0;
	public float uppercutCooldown = 0;

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
		
		gigaAttack = pzeroLoadout.gigaAttack switch {
			1 => new CFlasher(),
			2 => new RekkohaWeapon(),
			_ => new RakuhouhaWeapon(),
		};
		hyperMode = pzeroLoadout.hyperMode;
	}

	public override void update() {
		inputUpdate();
		Helpers.decrementFrames(ref donutTimer);
		Helpers.decrementFrames(ref swingCooldown);
		Helpers.decrementFrames(ref parryCooldown);
		Helpers.decrementFrames(ref dashAttackCooldown);
		Helpers.decrementFrames(ref diveKickCooldown);
		Helpers.decrementFrames(ref uppercutCooldown);
		gigaAttack.update();
		gigaAttack.charLinkedUpdate(this, true);

		if (isAwakened) {
			updateAwakenedAura();
		}

		base.update();

		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				changeSpriteFromName(charState.defaultSprite, false);
				if (charState is WallSlide) {
					frameIndex = sprite.frames.Count - 1;
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
		return (player.currency > 0 || freeBusterShots > 0) && donutsPending == 0;
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}
	
	public void setShootAnim() {
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = "zero_shoot"; }
			else { shootSprite = "zero_fall_shoot"; }
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
			playSound("buster2X3", sendRpc: true);
			new ZBuster2Proj(
				busterWeapon, shootPos, xDir, 0, player, player.getNextActorNetId(), rpc: true
			);
		} else if (chargeLevel == 2) {
			currencyUse = 1;
			playSound("buster3X3", sendRpc: true);
			new ZBuster3Proj(
				busterWeapon, shootPos, xDir, 0, player, player.getNextActorNetId(), rpc: true
			);
		} else if (chargeLevel == 3 || chargeLevel >= 4) {
			currencyUse = 1;
			playSound("buster4", sendRpc: true);
			new ZBuster4Proj(
				busterWeapon, shootPos, xDir, 0, player, player.getNextActorNetId(), rpc: true
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
		if (player.currency <= 0) { return; }
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
			time / 60f, player, player.getNextActorNetId(), rpc: true
		);
		playSound("shingetsurinx5", forcePlay: false, sendRpc: true);
		shootAnimTime = 0.3f;
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
			shootPressTime = 10;
		}
		if (player.input.isPressed(Control.Special1, player)) {
			specialPressTime = 10;
		}
		if (player.input.isPressed(Control.WeaponLeft, player) ||
			player.input.isPressed(Control.WeaponRight, player) && !isAwakened
		) {
			parryPressTime = 10;
		}
		if (player.input.isPressed(Control.WeaponRight, player) && isAwakened) {
			swingPressTime = 10;
		}
	}

	public override bool normalCtrl() {
		if (player.currency >= Player.zeroHyperCost &&
			player.input.isHeld(Control.Special2, player) &&
			!isViral && !isAwakened && !isBlack &&
			charState is not HyperZeroStart and not WarpIn
		) {
			hyperProgress += Global.spf;
		} else {
			hyperProgress = 0;
		}
		if (hyperProgress >= 1 && player.currency >= Player.zeroHyperCost) {
			hyperProgress = 0;
			changeState(new HyperPunchyZeroStart(), true);
			return true;
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		if (donutsPending != 0) {
			return false;
		}
		if (isAwakened && swingPressTime > 0 && swingCooldown == 0) {
			swingCooldown = 60;
			if (charState is WallSlide wallSlide) {
				changeState(new PunchyZeroHadangekiWall(wallSlide.wallDir, wallSlide.wallCollider), true);
				return true;
			}
			if (isDashing && grounded) {
				slideVel = xDir * getDashSpeed() * 0.9f;
			}
			changeState(new PunchyZeroHadangeki(), true);
			return true;
		}
		if (grounded && vel.y >= 0) {
			return groundAttacks();
		}
		return airAttacks();
	}


	public override void changeState(CharState newState, bool forceChange = false) {
		CharState? oldState = charState;
		base.changeState(newState, forceChange);
		if (!newState.attackCtrl || newState.attackCtrl != oldState?.attackCtrl) {
			shootPressTime = 0;
			specialPressTime = 0;
		}
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
		if ((shootPressTime > 0 || specialPressTime > 0) && yDir == 1) {
			changeState(new PZeroDropKickState(), true);
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
		if (yDir == 1 && gigaAttack.shootTime == 0) {
			if (gigaAttack is RekkohaWeapon) {
				gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
				changeState(new Rekkoha(gigaAttack), true);
			} else {
				gigaAttack.addAmmo(-gigaAttack.getAmmoUsage(0), player);
				changeState(new Rakuhouha(gigaAttack), true);
			}
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
		return "zero_" + spriteName;
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

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);
		if (proj != null) {
			proj.meleeId = meleeId;
			proj.owningActor = this;
			return proj;
		}
		return null;
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
			"zero_projswing" or "zero_projswing_air" => MeleeIds.SaberSwing,
			_ => MeleeIds.None
		});
	}

	public Projectile? getMeleeProjById(int id, Point? pos = null, bool addToLevel = true) {
		Point projPos = pos ?? new Point(0, 0);
		Projectile? proj = id switch {
			(int)MeleeIds.Punch => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroPunch, player,
				2, 0, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Punch2 => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroPunch2, player, 2, Global.halfFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Spin => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroSenpuukyaku, player, 2, Global.halfFlinch, 0.5f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.AirKick => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroAirKick, player, 3, 0, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Uppercut => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroShoryuken, player, 4, Global.defFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.StrongPunch => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroYoudantotsu, player, 6, Global.defFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.DropKick => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.PZeroEnkoukyaku, player, 4, Global.halfFlinch, 0.25f,
				addToLevel: addToLevel
			),
			(int)MeleeIds.Parry => new GenericMeleeProj(
				parryWeapon, projPos, ProjIds.PZeroParryStart, player, 0, 0, 0,
				addToLevel: addToLevel
			),
			(int)MeleeIds.ParryAttack => (new GenericMeleeProj(
				parryWeapon, projPos, ProjIds.PZeroParryAttack, player, 4, Global.defFlinch, 0.5f,
				addToLevel: addToLevel
			) {
				netcodeOverride = NetcodeModel.FavorDefender
			}),
			(int)MeleeIds.AwakenedAura => (new GenericMeleeProj(
				awakenedAuraWeapon, projPos, ProjIds.AwakenedAura, player, 2, 0, 0.5f,
				addToLevel: addToLevel
			) {
				netcodeOverride = NetcodeModel.FavorDefender
			}),
			(int)MeleeIds.SaberSwing => new GenericMeleeProj(
				saberSwingWeapon, projPos, ProjIds.ZSaberProjSwing, player,
				3, Global.defFlinch, 0.5f, isReflectShield: true,
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
					if (secondPhaseHyper) {
						damage = 4;
						flinch = Global.defFlinch;
					}
					Projectile proj = new GenericMeleeProj(
						awakenedAuraWeapon, centerPoint,
						ProjIds.AwakenedAura, player, damage, flinch, 0.5f
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
		if (specialState == (int)SpecialStateIds.PZeroParry &&
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

	public override void render(float x, float y) {
		if (isViral && visible) {
			addRenderEffect(RenderEffectType.Trail);
		} else {
			removeRenderEffect(RenderEffectType.Trail);
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
			if (secondPhaseHyper &&
				Global.frameCount % Global.normalizeFrames(6) > Global.normalizeFrames(3) &&
				Global.shaderWrappers.ContainsKey("awakened")
			) {
				shaders.Add(Global.shaderWrappers["awakened"]);
			}
			Global.sprites[auraSprite].draw(
				awakenedAuraFrame,
				pos.x + x + (xOff * auraXDir),
				pos.y + y + yOff, auraXDir,
				1, null, 1, 1, 1,
				zIndex - 1, shaders: shaders
			);
		}
		base.render(x, y);
	}
}
