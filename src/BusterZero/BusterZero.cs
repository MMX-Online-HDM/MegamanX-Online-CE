using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BusterZero : Character {
	public float zSaberCooldown;
	public float lemonCooldown;
	public bool isBlackZero;
	public int stockedBusterLv;
	public bool stockedSaber;
	public List<DZBusterProj> zeroLemonsOnField = new();
	public ZBusterSaber meleeWeapon = new();

	public BusterZero(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.BusterZero;
	}

	public override void update() {
		base.update();
		if (stockedBusterLv > 0 || stockedSaber) {
			var renderGfx = stockedBusterLv switch {
				_ when stockedSaber || stockedBusterLv == 2 => RenderEffectType.ChargeGreen,
				1 => RenderEffectType.ChargePink,
				2 => RenderEffectType.ChargeOrange,
				_ => RenderEffectType.ChargeBlue
			};
			addRenderEffect(renderGfx, 0.033333f, 0.1f);
		}
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
		// Cooldowns.
		Helpers.decrementFrames(ref zSaberCooldown);
		Helpers.decrementFrames(ref lemonCooldown);

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
		// Charge and release charge logic.
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
				if (charState is WallSlide) {
					changeState(new BusterZeroMelee(), true);
				} else {
					changeState(new BusterZeroMelee(), true);
				}
				return true;
			}
		}
		if (!isCharging()) {
			if (shootPressed) {
				lastShootPressed = Global.frameCount;
			}
			int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
			if (shootPressed || framesSinceLastShootPressed < 6) {
				if (stockedBusterLv == 1) {
					if (charState is WallSlide) {
						shoot(1);
						lemonCooldown = 22;
						stockedBusterLv = 0;
						return true;
					}
					changeState(new BusterZeroDoubleBuster(true, true), true);
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
					changeState(new BusterZeroDoubleBuster(true, false), true);
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
				if (zeroLemonsOnField[i].destroyed) {
					zeroLemonsOnField.RemoveAt(i);
				}
			}
			if (zeroLemonsOnField.Count >= 3) { return; }
		}
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = "zero_shoot"; } else { shootSprite = "zero_fall_shoot"; }
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
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		if (chargeLevel == 0) {
			playSound("busterX3", sendRpc: true);
			var lemon = new DZBusterProj(
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
			);
			zeroLemonsOnField.Add(lemon);
			lemonCooldown = 9;
		} else if (chargeLevel == 1) {
			playSound("buster2X3", sendRpc: true);
			new DZBuster2Proj(
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
			);
			lemonCooldown = 22;
		} else if (chargeLevel == 2) {
			playSound("buster3X3", sendRpc: true);
			new DZBuster3Proj(
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
				changeState(new BusterZeroDoubleBuster(false, true), true);
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
				changeState(new BusterZeroDoubleBuster(false, false), true);
			}
		}
		if (chargeLevel >= 1) {
			stopCharge();
		}
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"zero_projswing" or "zero_projswing_air" => MeleeIds.SaberSwing,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Projectile? proj = id switch {
			(int)MeleeIds.SaberSwing => new GenericMeleeProj(
				meleeWeapon, projPos, ProjIds.DZMelee, player,
				isBlackZero ? 4 : 3, Global.defFlinch, 0.5f, isReflectShield: true, addToLevel: addToLevel
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
		if (flag != null || !isDashing) {
			return getRunSpeed();
		}
		float dashSpeed = 210;
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
		]));
		return customData;
	}
	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];
		bool[] flags = Helpers.byteToBoolArray(data[0]);
		isBlackZero = flags[0];
	}
}
