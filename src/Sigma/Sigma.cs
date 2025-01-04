using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class BaseSigma : Character {
	public const float sigmaHeight = 50;
	public float sigmaSaberMaxCooldown = 1f;
	public float noBlockTime = 0;
	public const float maxLeapSlashCooldown = 2;
	public float tagTeamSwapProgress;
	public int tagTeamSwapCase;
	public long lastAttackFrame = -100;
	public long framesSinceLastAttack = 1000;

	public BaseSigma(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Sigma;
		// Special Sigma-only colider.
		spriteToCollider["head*"] = getSigmaHeadCollider();

		// Configure weapons if local.
		if (ownedByLocalPlayer) {
			weapons = configureWeapons();
		}

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

	public Collider getSigmaHeadCollider() {
		var rect = new Rect(0, 0, 14, 20);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Point getDashDustEffectPos(int xDir) {
		float dashXPos = -35;
		return pos.addxy(dashXPos * xDir + (5 * xDir), -4);
	}

	public bool isSigmaShooting() {
		return sprite.name.Contains("_shoot_") || sprite.name.EndsWith("_shoot");
	}

	public override bool isSoftLocked() {
		if (player.currentMaverick != null) return true;
		return base.isSoftLocked();
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

		if (!player.isAI && isPuppeteer && Options.main.puppeteerHoldOrToggle &&
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

		if (player.isAI && charState.attackCtrl) {
			foreach (Weapon weapon in weapons) {
				if (weapon is MaverickWeapon mw && mw.maverick == null) {
					if (mw.summonedOnce) {
						mw.summon(player, pos.addxy(0, -112), pos, xDir);
					}
					else if (canAffordMaverick(mw)) {
						buyMaverick(mw);
						mw.summon(player, pos.addxy(0, -112), pos, xDir);
						mw.summonedOnce = true;
					}
				}
			}

			return;
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
							maverick.aiCooldown = 60;
						}

						if (!isPuppeteer) {
							player.changeToSigmaSlot();
						}
					} else {
						cantAffordMaverickMessage();
					}
				} else if (isSummoner && !mw.isMenuOpened) {
					if (shootPressed && mw.shootCooldown == 0) {
						mw.shootCooldown = MaverickWeapon.summonerCooldown;
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
				sw.shootCooldown == 0 && charState is not Die && tagTeamSwapProgress == 0
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
					sw.shootCooldown = sw.fireRate;
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
		Helpers.decrementTime(ref noBlockTime);

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

	public override Collider getGlobalCollider() {
		Rect rect = new Rect(0, 0, 18, BaseSigma.sigmaHeight);
		if (sprite.name.Contains("_ra_")) {
			rect.y2 = 20;
		}
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getTerrainCollider() {
		Collider? overrideGlobalCollider = null;
		if (spriteToColliderMatch(sprite.name, out overrideGlobalCollider)) {
			return overrideGlobalCollider;
		}
		if (physicsCollider == null) {
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

	public override Point getCenterPos() {
		return pos.addxy(0, -32);
	}

	public override float getLabelOffY() {
		if (player.isMainPlayer && player.isTagTeam() && player.currentMaverick != null) {
			return player.currentMaverick.getLabelOffY();
		}
		if (sprite.name.Contains("_ra_")) {
			return 25;
		}
		return 62;
	}

	public override void render(float x, float y) {
		base.render(x, y);

		if (tagTeamSwapProgress > 0) {
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
	}

	public bool isAttacking() {
		if (isSigmaShooting() || sprite.name.Contains("attack")) {
			return true;
		}
		return false;
	}

	public override Point getCamCenterPos(bool ignoreZoom = false) {
		Maverick? maverick = player.currentMaverick;
		if (maverick != null && player.isTagTeam()) {
			if (maverick.state is MEnter me) {
				return me.getDestPos().round().addxy(camOffsetX, -24);
			}
			if (maverick.state is MorphMCHangState hangState) {
				return maverick.pos.addxy(camOffsetX, -24 + 17);
			}
			return maverick.pos.round().addxy(camOffsetX, -24);
		}
		return base.getCamCenterPos(ignoreZoom);
	}

	public List<Weapon> configureWeapons() {
		List<Weapon> retWeapons = new();
		if (Global.level.isTraining() && !Global.level.server.useLoadout) {
			retWeapons = Weapon.getAllSigmaWeapons(player).Select(w => w.clone()).ToList();
		} else if (Global.level.is1v1()) {
			if (player.maverick1v1 != null) {
				retWeapons = new List<Weapon>() {
					Weapon.getAllSigmaWeapons(player).Select(w => w.clone()).ToList()[player.maverick1v1.Value + 1]
				};
			} else if (!Global.level.isHyper1v1()) {
				int sigmaForm = Options.main.sigmaLoadout.sigmaForm;
				retWeapons = Weapon.getAllSigmaWeapons(player, sigmaForm).Select(w => w.clone()).ToList();
			}
		} else {
			retWeapons = player.loadout.sigmaLoadout.getWeaponsFromLoadout(player, Options.main.sigmaWeaponSlot);
		}
		// Preserve HP on death so can summon for free until they die.
		if (player.oldWeapons != null && player.isRefundableMode() &&
			player.previousLoadout?.sigmaLoadout?.commandMode == player.loadout.sigmaLoadout.commandMode
		) {
			foreach (var weapon in retWeapons) {
				if (weapon is not MaverickWeapon mw) continue;
				MaverickWeapon? matchingOldWeapon = player.oldWeapons.FirstOrDefault(
					w => w is MaverickWeapon && w.GetType() == weapon.GetType()
				) as MaverickWeapon;
				if (matchingOldWeapon == null) {
					continue;
				}
				if (matchingOldWeapon.lastHealth > 0 && matchingOldWeapon.summonedOnce) {
					mw.summonedOnce = true;
					mw.lastHealth = matchingOldWeapon.lastHealth;
					mw.isMoth = matchingOldWeapon.isMoth;
				}

			}
		}
		weaponSlot = 0;
		if (retWeapons.Count == 3) {
			weaponSlot = Options.main.sigmaWeaponSlot;
		}
		return retWeapons;
	}
}
