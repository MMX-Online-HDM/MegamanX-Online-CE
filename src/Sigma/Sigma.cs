using System;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public abstract class BaseSigma : Character {
	public const float sigmaHeight = 50;
	public float sigmaSaberMaxCooldown = 1f;
	public float noBlockTime = 0;
	public bool isHyperSigma;
	public const float maxLeapSlashCooldown = 2;
	public float tagTeamSwapProgress;
	public int tagTeamSwapCase;
	public float sigmaAmmoRechargeCooldown = 0.5f;
	public float sigmaAmmoRechargeTime;
	public float maxSigma3FireballCooldown = 0.39f;
	public float sigma3ShieldCooldown;
	public float maxSigma3ShieldCooldown = 1.125f;
	public float sigmaHeadBeamRechargePeriod = 0.05f;
	public float sigmaHeadBeamTimeBeforeRecharge = 0.33f;

	public float viralSigmaTackleCooldown;
	public float viralSigmaTackleMaxCooldown = 1;
	public string lastHyperSigmaSprite;
	public int lastHyperSigmaFrameIndex;
	public int lastHyperSigmaXDir;
	public float lastViralSigmaAngle;
	public float viralSigmaAngle;
	//public ShaderWrapper viralSigmaShader;
	//public ShaderWrapper sigmaShieldShader;

	// TODO: Move this to a diferent class.
	public float viralSigmaBeamLength;
	public int lastViralSigmaXDir = 1;
	public Character possessTarget;
	public float possessEnemyTime;
	public float maxPossessEnemyTime;
	public int numPossesses;

	public WolfSigmaHead head;
	public WolfSigmaHand leftHand;
	public WolfSigmaHand rightHand;

	public BaseSigma(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		// Special Sigma-only colider.
		spriteToCollider["head*"] = getSigmaHeadCollider();

		// For 1v1 mavericks.
		CharState intialCharState;
		if (ownedByLocalPlayer) {
			if (player.maverick1v1 != null) {
				intialCharState = new WarpOut(true);
			} else if (isWarpIn) {
				intialCharState = new WarpIn();
			} else {
				intialCharState = new Idle();
			}
		} else {
			intialCharState = new Idle();
		}
		changeState(intialCharState);
	}

	public void getViralSigmaPossessTarget() {
		var collideDatas = Global.level.getTriggerList(this, 0, 0);
		foreach (var collideData in collideDatas) {
			if (collideData?.gameObject is Character chr &&
				chr.canBeDamaged(player.alliance, player.id, (int)ProjIds.Sigma2ViralPossess) &&
				chr.player.canBePossessed()) {
				possessTarget = chr;
				maxPossessEnemyTime = 2 + (Helpers.clampMax(numPossesses, 4) * 0.5f);
				//2 - Helpers.progress(chr.player.health, chr.player.maxHealth);
				return;
			}
		}
	}

	public bool canPossess(Character target) {
		if (target == null || target.destroyed) return false;
		if (!target.player.canBePossessed()) return false;
		var collideDatas = Global.level.getTriggerList(this, 0, 0);
		foreach (var collideData in collideDatas) {
			if (collideData.gameObject is Character chr &&
				chr.canBeDamaged(player.alliance, player.id, (int)ProjIds.Sigma2ViralPossess)
			) {
				if (target == chr) {
					return true;
				}
			}
		}
		return false;
	}

	public bool isSigmaShooting() {
		return sprite.name.Contains("_shoot_") || sprite.name.EndsWith("_shoot");
	}

	public override void preUpdate() {
		base.preUpdate();

		if (!ownedByLocalPlayer) {
			return;
		}

		bool isSummoner = player.isSummoner();
		bool isPuppeteer = player.isPuppeteer();
		bool isStriker = player.isStriker();
		bool isTagTeam = player.isTagTeam();

		if (isPuppeteer && Options.main.puppeteerHoldOrToggle &&
			!player.input.isHeld(Control.WeaponLeft, player) &&
			!player.input.isHeld(Control.WeaponRight, player)
		) {
			player.changeToSigmaSlot();
		}

		player.changeWeaponControls();

		if (invulnTime > 0) return;

		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool spcPressed = player.input.isPressed(Control.Special1, player);
		if (flag != null) {
			shootPressed = false;
			spcPressed = false;
		}

		if (isPuppeteer) {
			if (player.weapon is MaverickWeapon mw2 && mw2.maverick != null) {
				if (mw2.maverick.aiBehavior != MaverickAIBehavior.Control && mw2.maverick.state is not MExit) {
					becomeMaverick(mw2.maverick);
				}
			} else if (player.currentMaverick != null) {
				becomeSigma(pos, xDir);
			}
		}

		// "Global" command prototype
		if (player.weapon is SigmaMenuWeapon &&
			player.currentMaverick == null && player.mavericks.Count > 0 &&
			grounded && player.input.isHeld(Control.Down, player) &&
			(isPuppeteer || isSummoner) && charState is not IssueGlobalCommand) {
			if (player.input.isCommandButtonPressed(player)) {
				Global.level.gameMode.hudErrorMsgTime = 0;
				if (player.currentMaverickCommand == MaverickAIBehavior.Defend) {
					player.currentMaverickCommand = MaverickAIBehavior.Follow;
					Global.level.gameMode.setHUDErrorMessage(player, "Issued follow command.", playSound: false);
				} else {
					player.currentMaverickCommand = MaverickAIBehavior.Defend;
					Global.level.gameMode.setHUDErrorMessage(player, "Issued hold position command.", playSound: false);
				}

				foreach (var maverick in player.mavericks) {
					maverick.aiBehavior = player.currentMaverickCommand;
				}

				changeState(new IssueGlobalCommand(), true);
			}
		} else if (player.weapon is SigmaMenuWeapon &&
			player.currentMaverick == null && player.mavericks.Count > 0 &&
			grounded && player.input.isHeld(Control.Up, player) &&
			(isPuppeteer || isSummoner) && charState is not IssueGlobalCommand
		) {
			if (player.input.isCommandButtonPressed(player)) {
				foreach (var maverick in player.mavericks) {
					maverick.changeState(new MExit(maverick.pos, true), ignoreCooldown: true);
				}
				changeState(new IssueGlobalCommand(), true);
			}
		}

		if (player.weapon is SigmaMenuWeapon && player.currentMaverick == null &&
			player.mavericks.Count > 0 && grounded &&
			(player.input.isHeld(Control.Right, player) || player.input.isHeld(Control.Left, player))
			&& isSummoner && charState is not IssueGlobalCommand && charState is not Dash
		) {
			if (player.input.isCommandButtonPressed(player)) {
				Global.level.gameMode.hudErrorMsgTime = 0;

				player.currentMaverickCommand = MaverickAIBehavior.Attack;
				Global.level.gameMode.setHUDErrorMessage(player, "Issued attack-move command.", playSound: false);

				foreach (var maverick in player.mavericks) {
					maverick.aiBehavior = player.currentMaverickCommand;
					maverick.attackDir = xDir;
				}

				changeState(new IssueGlobalCommand(), true);
			}
		}

		if (player.currentMaverick == null && !isTagTeam) {
			if (player.weapon is MaverickWeapon mw &&
				(!isStriker || mw.cooldown == 0) && (shootPressed || spcPressed)
			) {
				if (mw.maverick == null) {
					if (canAffordMaverick(mw)) {
						if (!(charState is Idle || charState is Run || charState is Crouch)) return;
						if (isStriker && player.mavericks.Count > 0) return;
						buyMaverick(mw);
						var maverick = player.maverickWeapon.summon(player, pos.addxy(0, -112), pos, xDir);
						if (isStriker) {
							mw.maverick.health = mw.lastHealth;
							if (player.input.isPressed(Control.Shoot, player)) {
								maverick.startMoveControl = Control.Shoot;
							} else if (player.input.isPressed(Control.Special1, player)) {
								maverick.startMoveControl = Control.Special1;
							}
						}
						/*
						else if (isSummoner)
						{
							mw.shootTime = MaverickWeapon.summonerCooldown;
							if (player.input.isPressed(Control.Shoot, player))
							{
								maverick.startMoveControl = Control.Shoot;
							}
							else if (player.input.isPressed(Control.Special1, player))
							{
								maverick.startMoveControl = Control.Special1;
							}
						}
						*/

						changeState(new CallDownMaverick(maverick, true, false), true);

						if (isSummoner) {
							maverick.aiCooldown = 1f;
						}

						if (!isPuppeteer) {
							player.changeToSigmaSlot();
						}
					} else {
						cantAffordMaverickMessage();
					}
				} else if (isSummoner && !mw.isMenuOpened) {
					if (shootPressed && mw.shootTime == 0) {
						mw.shootTime = MaverickWeapon.summonerCooldown;
						changeState(new CallDownMaverick(mw.maverick, false, false), true);
						player.changeToSigmaSlot();
					}
				}
				return;
			}
		}

		bool isMaverickIdle = player.currentMaverick?.state is MIdle mIdle;
		if (player.currentMaverick is MagnaCentipede ms && ms.reversedGravity) isMaverickIdle = false;

		bool isSigmaIdle = charState is Idle;
		if (isTagTeam && shootPressed) {
			if (isMaverickIdle && player.weapon is SigmaMenuWeapon sw &&
				sw.shootTime == 0 && charState is not Die && tagTeamSwapProgress == 0
			) {
				tagTeamSwapProgress = Global.spf;
				tagTeamSwapCase = 0;
			} else if (player.weapon is MaverickWeapon mw &&
				(mw.maverick == null || mw.maverick != player.currentMaverick) &&
				mw.cooldown == 0 && (isSigmaIdle || isMaverickIdle)
			) {
				if (canAffordMaverick(mw)) {
					tagTeamSwapProgress = Global.spf;
					tagTeamSwapCase = 1;
				} else {
					cantAffordMaverickMessage();
				}
			}
		}

		if (player.currentMaverick != null) {
			if (!isMaverickIdle || !player.currentMaverick.grounded) {
				tagTeamSwapProgress = 0;
			}
		} else {
			if (!isSigmaIdle || !grounded) {
				tagTeamSwapProgress = 0;
			}
		}

		if (tagTeamSwapProgress > 0) {
			tagTeamSwapProgress += Global.spf * 2;
			if (tagTeamSwapProgress > 1) {
				tagTeamSwapProgress = 0;
				if (tagTeamSwapCase == 0) {
					var sw = player.weapons.FirstOrDefault(w => w is SigmaMenuWeapon);
					sw.shootTime = sw.rateOfFire;
					player.currentMaverick.changeState(new MExit(player.currentMaverick.pos, true));
					becomeSigma(player.currentMaverick.pos, player.currentMaverick.xDir);
				} else {
					if (player.weapon is MaverickWeapon mw && mw.maverick == null) {
						buyMaverick(mw);

						Point currentPos = pos;
						if (player.currentMaverick == null) {
							changeState(new WarpOut());
						} else {
							currentPos = player.currentMaverick.pos;
							player.currentMaverick.changeState(new MExit(currentPos, true));
						}

						mw.summon(player, currentPos.addxy(0, -112), currentPos, xDir);
						mw.maverick.health = mw.lastHealth;
						becomeMaverick(mw.maverick);
					}
				}
			}
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		Helpers.decrementTime(ref saberCooldown);
		Helpers.decrementTime(ref noBlockTime);
		Helpers.decrementTime(ref viralSigmaTackleCooldown);
		if (viralSigmaBeamLength < 1 && charState is not ViralSigmaBeamState) {
			viralSigmaBeamLength += Global.spf * 0.1f;
			if (viralSigmaBeamLength > 1) viralSigmaBeamLength = 1;
		}
		if (player.sigmaAmmo >= player.sigmaMaxAmmo) {
			weaponHealAmount = 0;
		}
		if (weaponHealAmount > 0 && player.health > 0) {
			weaponHealTime += Global.spf;
			if (weaponHealTime > 0.05) {
				weaponHealTime = 0;
				weaponHealAmount--;
				player.sigmaAmmo = Helpers.clampMax(player.sigmaAmmo + 1, player.sigmaMaxAmmo);
				playSound("heal", forcePlay: true);
			}
		}
		if (player.maverick1v1 != null && player.readyTextOver &&
			!player.maverick1v1Spawned && player.respawnTime <= 0 && player.weapons.Count > 0
		) {
			player.maverick1v1Spawned = true;
			var mw = player.weapons[0] as MaverickWeapon;
			if (mw != null) {
				mw.summon(player, pos.addxy(0, -112), pos, xDir);
				mw.maverick.health = mw.lastHealth;
				becomeMaverick(mw.maverick);
			}
		}

		if (isHyperSigmaBS.getValue() && player.isSigma2() && charState is not Die) {
			lastHyperSigmaSprite = sprite?.name;
			lastHyperSigmaFrameIndex = frameIndex;
			lastViralSigmaAngle = angle ?? 0;

			var inputDir = player.input.getInputDir(player);
			if (inputDir.x != 0) lastViralSigmaXDir = MathF.Sign(inputDir.x);

			possessTarget = null;
			if (charState is ViralSigmaIdle) {
				getViralSigmaPossessTarget();
			}

			if (charState is not ViralSigmaRevive) {
				angle = Helpers.moveAngle(angle ?? 0, viralSigmaAngle, Global.spf * 500, snap: true);
			}
			if (player.weapons.Count >= 3) {
				if (isWading()) {
					if (player.weapons[2] is MechaniloidWeapon meW && meW.mechaniloidType != MechaniloidType.Fish) {
						player.weapons[2] = new MechaniloidWeapon(player, MechaniloidType.Fish);
					}
				} else {
					if (player.weapons[2] is MechaniloidWeapon meW && meW.mechaniloidType != MechaniloidType.Bird) {
						player.weapons[2] = new MechaniloidWeapon(player, MechaniloidType.Bird);
					}
				}
			}
		}
		if (invulnTime > 0) {
			return;
		}
		if ((charState is Die || charState is WarpOut) && player.currentMaverick != null && !visible) {
			changePos(player.currentMaverick.pos);
		}
		if (charState is WarpOut) return;
		if (player.currentMaverick != null) {
			return;
		}
		if (player.weapon is MaverickWeapon && (
			player.input.isHeld(Control.Shoot, player) ||
			player.input.isHeld(Control.Special1, player))
		) {
			return;
		}
		/*
		if (charState.canAttack() &&
			player.input.isHeld(Control.Shoot, player) &&
			player.weapon is not MaverickWeapon && !isAttacking() &&
			player.isSigma2() && saberCooldown == 0
		) {
			saberCooldown = 0.2f;
			changeState(new SigmaClawState(charState, !grounded), true);
			playSound("sigma2slash", sendRpc: true);
			return;
		}
		*/
		if (player.weapon is MaverickWeapon mw2 && player.input.isPressed(Control.Special2, player)) {
			mw2.isMenuOpened = true;
		}
	}

	public override bool normalCtrl() {
		var changedState = base.normalCtrl();
		if (changedState || !charState.normalCtrl) {
			return true;
		}
		if (grounded && player.isCrouchHeld() && canGuard() &&
			!isAttacking() && noBlockTime == 0 &&
			charState is not SigmaBlock
		) {
			changeState(new SigmaBlock());
			return true;
		}
		return false;
	}

	public virtual bool canGuard() {
		if (isDashing) {
			return false;
		}
		return true;
	}

	public override bool canCrouch() {
		return false;
	}

	// This can run on both owners and non-owners. So data used must be in sync
	public override Projectile? getProjFromHitbox(Collider collider, Point centerPoint) {
		if (sprite.name.Contains("sigma_block") && !collider.isHurtBox()) {
			return new GenericMeleeProj(
				player.sigmaSlashWeapon, centerPoint, ProjIds.SigmaSwordBlock, player,
				0, 0, 0, isDeflectShield: true
			);
		}
		return null;
	}

	public void becomeSigma(Point pos, int xDir) {
		var prevCamPos = getCamCenterPos();
		if (player.isPuppeteer()) {
			resetMaverickBehavior();
			//stopCamUpdate = true;
			//Global.level.snapCamPos(getCamCenterPos());
			return;
		}

		resetMaverickBehavior();
		stopCamUpdate = true;
		Point raycastPos = pos.addxy(0, -5);
		Point? warpInPos = Global.level.getGroundPosNoKillzone(raycastPos, Global.screenH);

		if (warpInPos == null) {
			var nearestSpawnPoint = Global.level.getClosestSpawnPoint(pos);
			warpInPos = Global.level.getGroundPos(nearestSpawnPoint.pos);
		}

		changePos(warpInPos.Value);
		this.xDir = xDir;
		changeState(new WarpIn(false), true);
		Global.level.snapCamPos(getCamCenterPos(), prevCamPos);
	}

	public void becomeMaverick(Maverick maverick) {
		resetMaverickBehavior();
		maverick.aiBehavior = MaverickAIBehavior.Control;
		//stopCamUpdate = true;
		//Global.level.snapCamPos(getCamCenterPos());
		if (maverick.state is not MEnter && maverick.state is not MorphMHatchState && maverick.state is not MFly) {
			//To bring back puppeteer cancel, uncomment this
			if (Options.main.puppeteerCancel) {
				maverick.changeToIdleFallOrFly();
			}
		}
	}

	public void resetMaverickBehavior() {
		foreach (var weapon in player.weapons) {
			if (weapon is MaverickWeapon mw) {
				if (mw.maverick != null && mw.maverick.aiBehavior == MaverickAIBehavior.Control) {
					mw.maverick.aiBehavior = player.currentMaverickCommand;
				}
				if (mw.isMenuOpened) {
					mw.isMenuOpened = false;
				}
			}
		}
	}

	public void buyMaverick(MaverickWeapon mw) {
		//if (Global.level.is1v1()) player.health -= (player.maxHealth / 2);
		if (player.isStriker()) return;
		if (player.isRefundableMode() && mw.summonedOnce) return;
		else player.currency -= getMaverickCost();
	}

	private void cantAffordMaverickMessage() {
		//if (Global.level.is1v1()) Global.level.gameMode.setHUDErrorMessage(player, "Maverick requires 16 HP");
		Global.level.gameMode.setHUDErrorMessage(
			player, "Maverick requires " + getMaverickCost() + " " + Global.nameCoins
		);
	}

	public bool canAffordMaverick(MaverickWeapon mw) {
		//if (Global.level.is1v1()) return player.health > (player.maxHealth / 2);
		if (player.isStriker()) return true;
		if (player.isRefundableMode() && mw.summonedOnce) return true;

		return player.currency >= getMaverickCost();
	}

	public int getMaverickCost() {
		if (player.isSummoner()) return 3;
		if (player.isPuppeteer()) return 3;
		if (player.isStriker()) return 0;
		if (player.isTagTeam()) return 5;
		return 3;
	}

	public override bool canClimbLadder() {
		if (isSigmaShooting()) {
			return false;
		}
		return base.canClimbLadder();
	}

	public override bool isSoundCentered() {
		if (isHyperSigma) return false;
		return base.isSoundCentered();
	}

	public override Collider getGlobalCollider() {
		Rect rect = new Rect(0, 0, 18, BaseSigma.sigmaHeight);
		if (sprite.name.Contains("_ra_")) {
			rect.y2 = 20;
		}
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getTerrainCollider() {
		if (physicsCollider == null || isHyperSigma) {
			return null;
		}
		float hSize = 40;
		if (sprite.name.Contains("crouch")) {
			hSize = 32;
		}
		if (sprite.name.Contains("dash")) {
			hSize = 32;
		}
		if (sprite.name.Contains("_ra_")) {
			hSize = 20;
		}
		return new Collider(
			new Rect(0f, 0f, 18, hSize).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0, 0)
		);
	}

	public override Collider getDashingCollider() {
		var rect = new Rect(0, 0, 18, 40);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getCrouchingCollider() {
		var rect = new Rect(0, 0, 18, 40);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getRaCollider() {
		var rect = new Rect(0, 0, 18, 30);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getRcCollider() {
		var rect = new Rect(0, -24, 18, 0);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	// This is not used... tecnically.
	public override Collider getBlockCollider() {
		var rect = new Rect(0, 0, 18, 34);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override void render(float x, float y) {
		base.render(x, y);

		bool drewStatusProgress = drawStatusProgress();
		bool drewSubtankHealing = drawSubtankHealing();

		if (!drewStatusProgress && !drewSubtankHealing && player.isSigma && tagTeamSwapProgress > 0) {
			float healthBarInnerWidth = 30;

			float progress = 1 - (tagTeamSwapProgress / 1);
			float width = progress * healthBarInnerWidth;

			getHealthNameOffsets(out bool shieldDrawn, ref progress);

			Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
			Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);

			DrawWrappers.DrawRect(
				topLeft.x, topLeft.y, botRight.x, botRight.y,
				true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White
			);
			DrawWrappers.DrawRect(
				topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1,
				true, Color.Yellow, 0, ZIndex.HUD - 1
			);

			Fonts.drawText(
				FontType.DarkGreen, "Swapping...", pos.x, pos.y - 15 + currentLabelY, Alignment.Center,
				true, depth: ZIndex.HUD
			);
			deductLabelY(labelCooldownOffY);
		}

		if (!drewStatusProgress && !drewSubtankHealing &&
			player.isViralSigma() && charState is ViralSigmaPossessStart
		) {
			float healthBarInnerWidth = 30;

			float progress = (possessEnemyTime / maxPossessEnemyTime);
			float width = progress * healthBarInnerWidth;

			getHealthNameOffsets(out bool shieldDrawn, ref progress);

			Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
			Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);

			DrawWrappers.DrawRect(
				topLeft.x, topLeft.y, botRight.x, botRight.y,
				true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White
			);
			DrawWrappers.DrawRect(
				topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1,
				true, Color.Yellow, 0, ZIndex.HUD - 1
			);
			Fonts.drawText(
				FontType.DarkGreen, "Possessing...", pos.x, pos.y - 15 + currentLabelY,
				Alignment.Center, true, depth: ZIndex.HUD
			);
			deductLabelY(labelCooldownOffY);
		}
	}

	public override void destroySelf(
		string spriteName = null, string fadeSound = null, bool disableRpc = false,
		bool doRpcEvenIfNotOwned = false, bool favorDefenderProjDestroy = false
	) {
		head?.explode();
		leftHand?.destroySelf();
		rightHand?.destroySelf();

		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);
	}

	public override bool isAttacking() {
		if (isSigmaShooting()) {
			return true;
		}
		return base.isAttacking();
	}
}
