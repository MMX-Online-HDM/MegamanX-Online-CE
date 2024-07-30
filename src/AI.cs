using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public enum AITrainingBehavior {
	Default,
	Idle,
	Attack,
	Jump,
	Crouch,
	Guard
}

public class AI {
	public Character character;
	public AIState aiState;
	public Actor? target;
	public float shootTime;
	public float dashTime = 0;
	public float jumpTime = 0;
	public float weaponTime = 0;
	public float maxChargeTime = 0;
	public int framesChargeHeld = 0;
	public float jumpZoneTime = 0;
	public bool flagger = false; //Will this ai aggressively capture the flag?
	public static AITrainingBehavior trainingBehavior;
	public int axlAccuracy;
	public int mashType; //0=no mash, 1 = light, 2 = heavy

	public Player player { get { return character.player; } }

	public int targetUpdateFrame;
	public static int targetUpdateCounter;
	public int projId;
	public bool isWildDance;

	public AI(Character character) {
		this.character = character;
		aiState = new AimAtPlayer(this.character);
		if (Global.level.flaggerCount < 2) {
			flagger = true;
			Global.level.flaggerCount++;
		}
		axlAccuracy = Helpers.randomRange(10, 30);
		mashType = Helpers.randomRange(0, 2);
		if (Global.level.isTraining()) mashType = 0;
		targetUpdateFrame = targetUpdateCounter;
		targetUpdateCounter++;
		if (targetUpdateCounter >= Server.maxPlayerCap / 2) {
			targetUpdateCounter = 0;
		}
	}

	public void doJump(float jumpTime = 0.75f) {
		if (this.jumpTime == 0) {
			//this.player.release(Control.Jump);
			player.press(Control.Jump);
			this.jumpTime = jumpTime;
		}
	}

	public RideChaser? raceAiSetupRc;

	public RideChaser? getRaceAIChaser() {
		List<RideChaser> rideChasers = new();
		foreach (GameObject go in Global.level.gameObjects) {
			if (go is RideChaser rc && !rc.destroyed && rc.character == null) {
				rideChasers.Add(rc);
			}
		}
		rideChasers = rideChasers.OrderBy(rc => rc.pos.distanceTo(character.pos)).ToList();

		return rideChasers.FirstOrDefault();
	}

	//Ride Chaser AI
	public void raceChaserAI() {
		if (character == null || character.charState is WarpIn) {
			return;
		}

		if (character.rideChaser == null) {
			var bestAIRideChaser = getRaceAIChaser();
			if (bestAIRideChaser != null) {
				raceAiSetupRc = bestAIRideChaser;
			} else if (raceAiSetupRc != null && ((raceAiSetupRc.character != null && raceAiSetupRc.character != character) || raceAiSetupRc.destroyed)) {
				raceAiSetupRc = null;
			}

			if (raceAiSetupRc == null) return;

			bool movedLastFrame = false;
			if (character.pos.x - raceAiSetupRc.pos.x > 5) {
				player.press(Control.Left);
				movedLastFrame = true;
			} else if (character.pos.x - raceAiSetupRc.pos.x < -5) {
				player.press(Control.Right);
				movedLastFrame = true;
			}

			if (!movedLastFrame) {
				player.press(Control.Jump);
			} else {
				player.release(Control.Jump);
			}
		} else {
			bool shouldShoot = false;
			var hits = Global.level.raycastAll(character.getCenterPos(), character.getCenterPos().addxy(character.xDir * 100, 0), new List<Type>() { typeof(RideChaser), typeof(Character) });
			foreach (var hit in hits) {
				if (hit?.gameObject is RideChaser rc) {
					if (rc.character != null && rc.character != character) {
						shouldShoot = true;
						break;
					}
				} else if (hit?.gameObject is Character hitChar) {
					if (hitChar != character) {
						shouldShoot = true;
						break;
					}
				}
			}
			if (shouldShoot) {
				player.press(Control.Shoot);
			} else {
				player.release(Control.Shoot);
			}

			var brakeZones = Global.level.getTriggerList(character.abstractedActor(), 0, 0, null, typeof(BrakeZone));
			if ((Global.level.gameMode as Race)?.getPlace(character.player) > 1) {
				dashTime = 100;
			} else {
				dashTime = 0;
			}

			if ((dashTime > 0 || jumpTime > 0) && brakeZones.Count == 0) {
				player.press(Control.Dash);
				dashTime -= Global.spf;
				if (dashTime < 0) dashTime = 0;
			}

			var turnZones = Global.level.getTriggerList(character.abstractedActor(), 0, 0, null, typeof(TurnZone));
			if (turnZones.FirstOrDefault()?.gameObject is TurnZone turnZone && turnZone.xDir != character.xDir) {
				if (turnZone.xDir == -1) {
					player.release(Control.Left);
					player.press(Control.Left);
				} else {
					player.release(Control.Right);
					player.press(Control.Right);
				}
			}

			if (jumpTime == 0) {
				var jumpZones = Global.level.getTriggerList(character.abstractedActor(), 0, 0, null, typeof(JumpZone));
				int jumpTurnZoneCount = turnZones.Count(turnZone => turnZone.gameObject is TurnZone tz && tz.jumpAfterTurn && tz.xDir == character.xDir);

				if (jumpZones.Count + jumpTurnZoneCount > 0 && character.rideChaser?.grounded == true) {
					jumpTime = (jumpZones.FirstOrDefault()?.gameObject as JumpZone)?.jumpTime ?? 0.5f;
				} else if (Helpers.randomRange(0, 300) < 1) {
					jumpTime = 0.5f;
				}
			} else {
				player.release(Control.Jump);
				player.press(Control.Jump);
				jumpTime -= Global.spf;
				if (jumpTime <= 0) {
					jumpTime = 0;
				}
			}
		}
	}
	//End of Ride Chaser AI

	public virtual void update() {
		if (Global.level.isRace() && Global.level.supportsRideChasers && Global.level.levelData.raceOnly) {
			raceChaserAI();
			return;
		}
		if (Global.debug || Global.level.isTraining()) {
			if (trainingBehavior == AITrainingBehavior.Idle) {
				player.release(Control.Shoot);
				player.release(Control.Jump);
				return;
			}
			if (trainingBehavior == AITrainingBehavior.Attack) {
				player.release(Control.Jump);
				player.press(Control.Shoot);
				return;
			}
			if (trainingBehavior == AITrainingBehavior.Jump) {
				player.release(Control.Shoot);
				player.press(Control.Jump);
				return;
			}
			if (trainingBehavior == AITrainingBehavior.Guard) {
				player.press(Control.WeaponLeft);
				return;
			}
			if (trainingBehavior == AITrainingBehavior.Crouch) {
				if (player.isSigma) {
					character?.changeState(new SigmaBlock(), true);
					player.press(Control.Down);
				} else {
					player.press(Control.Down);
				}
				return;
			}
		}
		if (Global.level.gameMode.isOver) {
			return;
		}
		// Buy Section
		if (!player.isMainPlayer && character is MegamanX &&
			player.aiArmorUpgradeIndex < player.aiArmorUpgradeOrder.Count && !Global.level.is1v1()
		) {
			var upgradeNumber = player.aiArmorUpgradeOrder[player.aiArmorUpgradeIndex];
			if (upgradeNumber == 0 && player.currency >= MegamanX.bootsArmorCost) {
				UpgradeArmorMenu.upgradeBootsArmor(player, player.aiArmorPath);
				player.aiArmorUpgradeIndex++;
			} else if (upgradeNumber == 1 && player.currency >= MegamanX.bodyArmorCost) {
				UpgradeArmorMenu.upgradeBodyArmor(player, player.aiArmorPath);
				player.aiArmorUpgradeIndex++;
			} else if (upgradeNumber == 2 && player.currency >= MegamanX.headArmorCost) {
				UpgradeArmorMenu.upgradeHelmetArmor(player, player.aiArmorPath);
				player.aiArmorUpgradeIndex++;
			} else if (upgradeNumber == 3 && player.currency >= MegamanX.armArmorCost) {
				UpgradeArmorMenu.upgradeArmArmor(player, player.aiArmorPath);
				player.aiArmorUpgradeIndex++;
			}
		}
		if (!player.isMainPlayer && character is Vile) {
			if (player.currency >= 3 && !player.frozenCastle) {
				player.frozenCastle = true;
				player.currency -= Vile.frozenCastleCost;
			}
			if (player.currency >= 3 && !player.speedDevil) {
				player.speedDevil = true;
				player.currency -= Vile.speedDevilCost;
			}
		}
		if (!player.isMainPlayer) {
			if (player.heartTanks < 8 && player.currency >= 2) {
				player.currency -= 2;
				player.heartTanks++;
				float currentMaxHp = player.maxHealth;
				player.maxHealth = player.getMaxHealth();
				player.character?.addHealth(player.maxHealth - currentMaxHp);
			}
		}
		if (framesChargeHeld > 0 && character is MegamanX) {
			if (character.chargeTime < maxChargeTime) {
				//console.log("HOLD");
				player.press(Control.Shoot);
				if (player.isAxl && player.weapon is AxlBullet or DoubleBullet)
					player.press(Control.Special1);
			} else {
				//this.player.release(control.Shoot.key);
			}
		}
		if (target != null && target.destroyed) {
			target = null;
		}
		if (!Global.isSkippingFrames && Global.level.nonSkippedframeCount % 60 == targetUpdateFrame) {
			if (target != null && (
					target.destroyed ||
					character.pos.distanceTo(target.pos) > 400 ||
					!Global.level.gameObjects.Contains(target)
				)
			) {
				target = null;
			}
			if (target == null) {
				target = Global.level.getClosestTarget(
					character.pos, player.alliance, true, isRequesterAI: true,
					aMaxDist: 400
				);
			}
		}
		/*if (character is KaiserSigma || character is BaseSigma sigma && sigma.isHyperSigma) {
			int attack = Helpers.randomRange(0, 1);
			if (attack == 0) {
				player.release(Control.Special1);
				player.press(Control.Special1);
			} else if (attack == 1) {
				player.release(Control.Shoot);
				player.press(Control.Shoot);
			}
			if (Helpers.randomRange(0, 60) < 5) {
				player.changeWeaponSlot(Helpers.randomRange(0, 2));
			}
			return;
		}*/

		if (aiState is not InJumpZone) {
			var jumpZones = Global.level.getTriggerList(character.abstractedActor(), 0, 0, null, typeof(JumpZone));
			var neighbor = (aiState as FindPlayer)?.neighbor;
			if (neighbor != null) {
				jumpZones = jumpZones.FindAll(j => !neighbor.isJumpZoneExcluded(j.gameObject.name));
			}
			if (jumpZones.Count > 0 && jumpZones[0].gameObject is JumpZone jumpZone) {
				var jumpZoneDir = character.xDir;
				if (jumpZone.forceDir != 0) {
					jumpZoneDir = jumpZone.forceDir;
				}
				if (jumpZoneDir == 0) jumpZoneDir = -1;

				if (jumpZone.targetNode == null || jumpZone.targetNode == aiState.getNextNodeName()) {
					if (aiState is not FindPlayer) {
						changeState(new InJumpZone(character, jumpZone, jumpZoneDir));
					} else {
						if (jumpZone.forceDir == -1) {
							player.press(Control.Left);
						} else if (jumpZone.forceDir == 1) {
							player.press(Control.Right);
						}

						if (character.charState is not LadderClimb) {
							doJump();
							jumpZoneTime += Global.spf;
							if (jumpZoneTime > 2 && character.player.isVile) {
								jumpZoneTime = 0;
								player.press(Control.Up);
							}
						}
					}
				} else {
				}
			} else {
				jumpZoneTime = 0;
			}
		}

		if (character.flag != null) {
			target = null;
		} else if (Global.level.gameMode is CTF) {
			/*
			foreach (var player in Global.level.players) {
				if (player.character != null &&
					player.alliance != character.player.alliance &&
					player.character.flag != null
				) {
					target = player.character;
					break;
				}
			}
			*/
		}

		float stuckTime = (aiState as FindPlayer)?.stuckTime ?? 0;
		bool inNodeTransition = (aiState as FindPlayer)?.nodeTransition != null;

		if (aiState is not InJumpZone) {
			if (target == null) {
				if (aiState is not FindPlayer) {
					changeState(new FindPlayer(character));
				}
			} else {
				if (aiState is FindPlayer) {
					changeState(new AimAtPlayer(character));
				}
			}

			if (target != null) {
				if (character.charState is LadderClimb) {
					doJump();
				}
				var xDist = target.pos.x - character.pos.x;
				if (Math.Abs(xDist) > getMaxDist()) {
					changeState(new MoveTowardsTarget(character));
				}
			}
		}

		if (aiState.facePlayer && target != null) {
			if (character.pos.x > target.pos.x) {
				if (character.xDir != -1) {
					player.press(Control.Left);
				}
			} else {
				if (character.xDir != 1) {
					player.press(Control.Right);
				}
			}
			/*if (player.isAxl) {
				player.axlCursorPos = target.pos
					.addxy(-Global.level.camX, -Global.level.camY)
					.addxy(Helpers.randomRange(
						-axlAccuracy, axlAccuracy
					), Helpers.randomRange(
						-axlAccuracy, axlAccuracy
					));
			}*/
		}

		//Always do as AI
		if (character is MegamanX mmx4) {
			dommxAI(mmx4);
		} else if (character is Zero zero) {
			//doZeroAI(zero);
		} else if (character is BaseSigma sigma4) {
			doSigmaAI(sigma4);
		} else if (character is Vile vile4) {
			doVileAI(vile4);
		} else if (character is Axl axl4) {
			doAxlAI(axl4);
		} else if (character is PunchyZero pzero) {
			doKnuckleAI(pzero);
		} else if (character is BusterZero bzero) {
			doBusterZeroAI(bzero);
		}

		//Should AI Attack?
		if (aiState.shouldAttack && target != null) {
			if (shootTime == 0) {
				if (character is MegamanX mmx2) {
					mmxAIAttack(mmx2);
				} else if (character is Zero zero2) {
					zeroAIAttack(zero2);
				} else if (character is BaseSigma sigma2) {
					sigmaAIAttack(sigma2);
				} else if (character is Vile vile2) {
					vileAIAttack(vile2);
				} else if (character is Axl axl2) {
					axlAIAttack(axl2);
				} else if (character is BusterZero zero) {
					busterZeroAIAttack(zero);
				} else if (character is PunchyZero pzero) {
					KnuckleZeroAIAttack(pzero);
					return;
				}

				// is Facing the target?
				if (character.isFacing(target)) {
					//Makes the AI release the charge
					if (framesChargeHeld > 0) {
						if (character.chargeTime >= maxChargeTime) {
							player.release(Control.Shoot);
							framesChargeHeld = 0;
						}
					}
				}
			}
			// The amount of actions the AI will do, the lower the number, the higher the actions
			// At 0.01 is Kaizo Level
			shootTime += Global.spf;
			if (shootTime > 0.08) {
				shootTime = 0;
			}
		}
		//End of AI should attack

		//The AI should dodge if a projectile is close to him
		/*
		if (aiState.shouldDodge && target != null) {
			if (character is Zero zero3) {
				zeroAIDodge(zero3);
			}
			if (character is BaseSigma sigma3) {
				sigmaAIDodge(sigma3);
			}
			if (character is Axl axl3) {
				axlAIDodge(axl3);
			}
			if (character is MegamanX mmx3) {
				mmxAIDodge(mmx3);
			}
			if (character is PunchyZero pzero) {
				knuckleZeroAIDodge(pzero);
			}
			if (character is BusterZero bzero) {
				busterzeroAIDodge(bzero);
			}
		}
		*/
		//End of The AI Dodging

		//The AI should randomly charge weapon?
		if (aiState.randomlyChargeWeapon 
			&& character is MegamanX or Axl or BusterZero
			&& framesChargeHeld == 0 && player.character?.canCharge() == true
		) {
			if (Helpers.randomRange(0, 20) < 1) {
				if (player.isAxl) {
					if (player.weapon is AxlBullet || player.weapon is DoubleBullet) {
						character?.increaseCharge();
					}
				} else {
					maxChargeTime = 4.25f * 60f;
				}
				framesChargeHeld = 1;
			}
		}
		//End of Randomly Charge Weapon
		//The AI randomly change state?
		if (aiState.randomlyChangeState && character != null) {
			if (Helpers.randomRange(0, 100) < 1) {
				var randAmount = Helpers.randomRange(-100, 100);
				changeState(new MoveToPos(character, character.pos.addxy(randAmount, 0)));
				return;
			}
		} 
		//Randomly Dash
		if (aiState.randomlyDash && character != null &&
			character.charState is not WallKick &&
			character.grounded &&
			!inNodeTransition && stuckTime == 0 &&
			character.charState.normalCtrl &&
			character.charState is not Dash or AirDash or UpDash
		) {
				if (Helpers.randomRange(0, 75) < 5) {
				dashTime = Helpers.randomRange(0.3f, 0.5f);
			}
			if (dashTime > 0) {
				character.changeState(
				new Dash(Control.Dash)
			);
				dashTime -= Global.spf;
				if (dashTime < 0) dashTime = 0;
			}

		}	
		//Randomly Jump
		if (aiState.randomlyJump && !inNodeTransition && stuckTime == 0) {
			if (Helpers.randomRange(0, 650) < 3) {
				jumpTime = Helpers.randomRange(0.25f, 0.75f);
			}
		}
		//Randomly Change Weapon
		if (aiState.randomlyChangeWeapon &&
			(player.isX || player.isAxl || player.isVile) &&
			!player.lockWeapon && player.character?.isStealthy(-1) != null &&
			(character as MegamanX)?.chargedRollingShieldProj == null
		) {
			weaponTime += Global.spf;
			if (weaponTime > 5) {
				weaponTime = 0;
				var wasBuster = (player.weapon is Buster or AxlBullet);
				player.changeWeaponSlot(getRandomWeaponIndex());
				if (wasBuster && maxChargeTime > 0) {
					maxChargeTime = 4.25f * 60f;
				}
			}
		}

		if (player.weapon != null && player.weapon.ammo <= 0 && player.weapon is not Buster or AxlBullet) {
			player.changeWeaponSlot(getRandomWeaponIndex());
		}

		if (player.vileAmmo <= 0 && player.weapon is not VileCannon) {
			player.changeWeaponSlot(getRandomWeaponIndex());
		}

		aiState.update();

		if (jumpTime > 0) {
			jumpTime -= Global.spf;
			if (jumpTime < 0) {
				jumpTime = 0;
			}
		}
	}

	public int getRandomWeaponIndex() {
		if (player.weapons.Count == 0) return 0;
		List<Weapon> weapons = player.weapons.FindAll(w => w is not DNACore or IceGattling or BlastLauncher).ToList();
		return weapons.IndexOf(weapons.GetRandomItem());                                         // removing IceGattling until know the bug
	}

	public void changeState(AIState newState, bool forceChange = false) {
		if (aiState is FindPlayer && newState is not FindPlayer && character.flag != null) {
			return;
		}
		if (flagger && aiState is FindPlayer && newState is not FindPlayer && Global.level.gameMode is CTF) {
			return;
		}
		if (aiState is FindPlayer && newState is not FindPlayer && Global.level.gameMode is Race) {
			return;
		}
		if (forceChange || newState.canChangeTo()) {
			aiState = newState;
		}
	}

	public float getMaxDist() {
		var maxDist = Global.screenW / 2;
		int? raNum = player.character?.rideArmor?.raNum;
		if (raNum != null && raNum != 2) maxDist = 35;
		if (player.isZero || player.isSigma || character is PunchyZero) return 50;
		return maxDist;
	}

	//Start of X 
	public void mmxAIAttack(Character mmx2) {
		bool isTargetInAir = target?.pos.y < character.pos.y - 50;
		bool isTargetBellowYou = target?.pos.y < character.pos.y + 10;
		bool isTargetSuperClose = target?.pos.x - 3 >= character.pos.x;
		bool isTargetClose = target?.pos.x - 15 > character.pos.x;
		bool isTargetSSC = target?.pos.x == character.pos.x;
		// X start
		if (character is MegamanX megamanX) {
			int SpeedBurner = player.weapons.FindIndex(w => w is SpeedBurner);
			int FrostShield = player.weapons.FindIndex(w => w is FrostShield);
			int TriadThunder = player.weapons.FindIndex(w => w is TriadThunder);
			int GravityWell = player.weapons.FindIndex(w => w is GravityWell);
			int TunnelFang = player.weapons.FindIndex(w => w is TunnelFang);
			int AcidBurst = player.weapons.FindIndex(w => w is AcidBurst);
			int ParasiticBomb = player.weapons.FindIndex(w => w is ParasiticBomb);
			int CrystalHunter = player.weapons.FindIndex(w => w is CrystalHunter);
			int SilkShot = player.weapons.FindIndex(w => w is SilkShot);
			int SpinWheel = player.weapons.FindIndex(w => w is SpinWheel);
			int ElectricSpark = player.weapons.FindIndex(w => w is ElectricSpark);
			int RollingShield = player.weapons.FindIndex(w => w is RollingShield);
			int Tornado = player.weapons.FindIndex(w => w is Tornado);
			int Torpedo = player.weapons.FindIndex(w => w is Torpedo);
			int Sting = player.weapons.FindIndex(w => w is Sting);
			int Boomerang = player.weapons.FindIndex(w => w is Boomerang);
			int ShotgunIce = player.weapons.FindIndex(w => w is ShotgunIce);
			int SonicSlicer = player.weapons.FindIndex(w => w is SonicSlicer);
			int StrikeChain = player.weapons.FindIndex(w => w is StrikeChain);
			int BubbleSplash = player.weapons.FindIndex(w => w is BubbleSplash);

			int Xattack = Helpers.randomRange(0, 12);
			if (!player.isDead && megamanX.charState.canAttack() && megamanX.canShoot() && megamanX.canChangeWeapons()
				&& character.charState.normalCtrl && character.charState is not LadderClimb
			) {
				switch (Xattack) {
					case 0:
						// If X AI is facing Zero or Sigma
						if (target is Zero or CmdSigma or NeoSigma or Doppma) {
							switch (Helpers.randomRange(0, 10)) {
								// SpeedBurner	
								case 0:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(SpeedBurner);
									megamanX.player.press(Control.Shoot);
									break;
								// Frost Shield
								case 1:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(FrostShield);
									megamanX.player.press(Control.Shoot);
									break;
								// Triad Thunder
								case 2:
									if (isTargetSSC)
										megamanX.player.changeWeaponSlot(TriadThunder);
									megamanX.player.press(Control.Shoot);
									break;
								case 3:
									// Gravity Well
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(GravityWell);
									megamanX.player.press(Control.Shoot);
									break;
								// Tunnel Fang
								case 4:
									if (isTargetSuperClose)
										megamanX.player.changeWeaponSlot(TunnelFang);
									megamanX.player.press(Control.Shoot);
									break;
								// Acid Burst
								case 5:
									if (isTargetSSC)
										megamanX.player.changeWeaponSlot(AcidBurst);
									megamanX.player.press(Control.Shoot);
									break;
								// Parasite Bomb
								case 6:
									if (isTargetSSC)
										megamanX.player.changeWeaponSlot(ParasiticBomb);
									megamanX.player.press(Control.Shoot);
									break;
								// Crystal Hunter
								case 7:
									if (isTargetSSC)
										megamanX.player.changeWeaponSlot(CrystalHunter);
									megamanX.player.press(Control.Shoot);
									break;
								// SilkShot
								case 8:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(SilkShot);
									megamanX.player.press(Control.Shoot);
									break;
								// Spin Wheel
								case 9:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(SpinWheel);
									megamanX.player.press(Control.Shoot);
									break;
								// Electric Spark
								case 10:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(ElectricSpark);
									megamanX.player.press(Control.Shoot);
									break;
							}
						}
						break;
					case 1:
						// If X AI is facing X or Vile
						if (target is MegamanX or Vile) {
							switch (Helpers.randomRange(0, 11)) {
								// Rolling Shield	
								case 0:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(RollingShield);
									megamanX.player.press(Control.Shoot);
									break;
								// Storm Tornado
								case 1:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(Tornado);
									megamanX.player.press(Control.Shoot);
									break;
								// Torpedo
								case 2:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(Torpedo);
									megamanX.player.press(Control.Shoot);
									break;
								case 3:
									// C Sting
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(Sting);
									megamanX.player.press(Control.Shoot);
									break;
								// B Cutter
								case 4:
									if (isTargetSuperClose)
										megamanX.player.changeWeaponSlot(Boomerang);
									megamanX.player.press(Control.Shoot);
									break;
								// S Ice
								case 5:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(ShotgunIce);
									megamanX.player.press(Control.Shoot);
									break;
								// Speed Burner
								case 6:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(SpeedBurner);
									megamanX.player.press(Control.Shoot);
									break;
								// Crystal Hunter
								case 7:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(CrystalHunter);
									megamanX.player.press(Control.Shoot);
									doJump(0.75f);
									break;
								// Sonic Slicer
								case 8:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(SonicSlicer);
									megamanX.player.press(Control.Shoot);
									break;
								// Electric Spark
								case 9:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(ElectricSpark);
									megamanX.player.press(Control.Shoot);
									break;
								// FShield
								case 10:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(FrostShield);
									megamanX.player.press(Control.Shoot);
									break;
								// Acid
								case 11:
									if (isTargetSuperClose)
										megamanX.player.changeWeaponSlot(AcidBurst);
									megamanX.player.press(Control.Shoot);
									break;
							}
						}
						break;
					case 2:
						// If X AI is facing Axl	
						if (target is Axl) {
							switch (Helpers.randomRange(0, 10)) {
								// Rolling Shield	
								case 0:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(RollingShield);
									megamanX.player.press(Control.Shoot);
									break;
								// Strike Chain
								case 1:
									if (isTargetSuperClose)
										megamanX.player.changeWeaponSlot(StrikeChain);
									megamanX.player.press(Control.Shoot);
									break;
								// Bubbles
								case 2:
									if (isTargetSuperClose)
										megamanX.player.changeWeaponSlot(BubbleSplash);
									megamanX.player.press(Control.Shoot);
									break;
								case 3:
									// P Bomb
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(ParasiticBomb);
									megamanX.player.press(Control.Shoot);
									break;
								// Acid
								case 4:
									if (isTargetSuperClose)
										megamanX.player.changeWeaponSlot(AcidBurst);
									megamanX.player.press(Control.Shoot);
									break;
								// F Shield
								case 5:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(FrostShield);
									megamanX.player.press(Control.Shoot);
									break;
								// BCutter
								case 6:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(Boomerang);
									megamanX.player.press(Control.Shoot);
									break;
								// Crystal Hunter
								case 7:
									if (isTargetClose)
										player.changeWeaponSlot(CrystalHunter);
									player.press(Control.Shoot);
									doJump(0.75f);
									break;
								// SilkShot
								case 8:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(SilkShot);
									megamanX.player.press(Control.Shoot);
									break;
								// Sonic Slicer
								case 9:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(SonicSlicer);
									megamanX.player.press(Control.Shoot);
									break;
								// Electric Spark
								case 10:
									if (isTargetClose)
										megamanX.player.changeWeaponSlot(ElectricSpark);
									megamanX.player.press(Control.Shoot);
									break;
							}
						}
						break;
					case 3:
						if (target is MegamanX or Axl or Vile or NeoSigma) {
							if (megamanX.hasHadoukenEquipped() && megamanX.canUseFgMove() && isTargetSuperClose) {
								megamanX.player.currency -= 3;
								megamanX.player.fgMoveAmmo = 0;
								megamanX.changeState(new Hadouken(), true);
							}
						}
						break;
					case 4:
						if (target is Character && (player.isWolfSigma() || player.isViralSigma() || target is KaiserSigma && isTargetInAir == true && isTargetSuperClose)
						|| target is MegamanX or Zero or Axl or Vile or CmdSigma or NeoSigma or Doppma) {
							if (megamanX.hasShoryukenEquipped() && megamanX.canUseFgMove()) {
								megamanX.player.currency -= 3;
								megamanX.player.fgMoveAmmo = 0;
								megamanX.changeState(new Shoryuken(megamanX.isUnderwater()), true);
							}
						}
						break;
					case 5:
						if (player.armorFlag == 0) {
							megamanX.player.press(Control.Special1);
							megamanX.player.release(Control.Special1);
						}
						break;
					case 6:
						int novaStrikeSlot = player.weapons.FindIndex(w => w is NovaStrike);
						if (player.hasUltimateArmor()) {
							megamanX.player.changeWeaponSlot(novaStrikeSlot);
							if (megamanX.player.weapon.ammo >= 16) {
								megamanX.player.press(Control.Shoot);
							} else { megamanX.player.changeWeaponSlot(getRandomWeaponIndex()); }
						}
						break;
					case 7:
						int gCrushSlot = player.weapons.FindIndex(w => w is GigaCrush);
						if (player.hasBodyArmor(2)) {
							megamanX.player.changeWeaponSlot(gCrushSlot);
							if (megamanX.player.weapon.ammo == 32)
								megamanX.player.press(Control.Shoot);
							else {
								megamanX.player.changeWeaponSlot(getRandomWeaponIndex());
							}
						}
						break;
					case 8:
						int hyperbuster = player.weapons.FindIndex(w => w is HyperBuster);
						if (player.hasArmArmor(3)) {
							player.changeWeaponSlot(hyperbuster);
							if (megamanX.player.weapon.ammo >= 16) {
								megamanX.player.press(Control.Shoot);
								megamanX.player.release(Control.Shoot);
							} else { megamanX.player.changeWeaponSlot(getRandomWeaponIndex()); }
						}
						break;
					case 9:
						if (Helpers.randomRange(0, 30) < 5) {
							megamanX.player.changeWeaponSlot(0);
							megamanX.shoot(true);
						}
						break;
					case 10:
						if (megamanX.stockedXSaber) {
							megamanX.player.press(Control.Shoot);
							megamanX.player.release(Control.Shoot);
						}
						break;
					case 11:
						if (megamanX.stockedCharge) {
							megamanX.player.press(Control.Shoot);
							megamanX.player.release(Control.Shoot);
						}
						break;
					case 12:
						if (Helpers.randomRange(0, 30) < 5) {
							megamanX.player.release(Control.Shoot);
						}
						break;
				}
			}
			int UPXAttack = Helpers.randomRange(0, 5);
			//UP X section
			if (megamanX?.isHyperX == true) {
				switch (UPXAttack) {
					case 0:
						megamanX.player.press(Control.Special1);
						break;
					case 1:
						megamanX.player.press(Control.Shoot);
						megamanX.player.release(Control.Shoot);
						break;
					case 2:
						megamanX.player.release(Control.Special1);
						break;
					case 3:
						megamanX.player.press(Control.WeaponRight);
						megamanX.player.release(Control.WeaponRight);
						break;
					case 4:
						if (megamanX.charState is Dash or AirDash or Run) {
							megamanX.charState.isGrabbing = true;
							megamanX.changeSpriteFromName("unpo_grab_dash", true);
						}
						break;
					case 5 when megamanX.grounded:
						megamanX.changeState(new X6SaberState(megamanX.grounded), true);
						break;
				}
			}
		}
	}
	public void dommxAI(Character mmx4) {
		// X:
		if (character is MegamanX mmx) {

			/*if (player.canUpgradeUltimateX() && player.health >= player.maxHealth) {
				if (!mmx.boughtUltimateArmorOnce) {
					player.currency -= Player.ultimateArmorCost;
					mmx.boughtUltimateArmorOnce = true;
				}
				player.setUltimateArmor(true);
				return;
			} Deactivated until fixed */ 

			if (player.hasAllX3Armor() && player.canUpgradeGoldenX() && player.health >= player.maxHealth) {
				if (!mmx.boughtGoldenArmorOnce) {
					player.currency -= Player.goldenArmorCost;
					mmx.boughtGoldenArmorOnce = true;
				}
				player.setGoldenArmor(true);
				return;
			}

			if (player.canReviveX() && !player.isMainPlayer) {
				player.reviveX();

			}
			if (mmx.canCharge()) {
				player.character.increaseCharge();
			}

			if (mmx.isHyperX && mmx.canShoot()) {
				//mmx.unpoShotCount = Math.Max(mmx.unpoShotCount, 4);
				player.release(Control.Shoot);
				player.press(Control.Shoot);
			}
		}
	}

	public void mmxAIDodge(Character mmx) {
		int RollingShield = player.weapons.FindIndex(w => w is RollingShield);
		foreach (GameObject gameObject in mmx.getCloseActors(64, true, false, false)) {
			if (gameObject is Projectile proj) {
				if (proj.damager.owner.alliance != player.alliance) {
					//Start of X
					//Putting X 
					if (player.character is MegamanX X) {
						if (character != null && proj.isFacing(character) &&
						character.withinX(proj, 60) && character.withinY(proj, 30)) {

							if (player.hasArmor() || !player.hasArmor() && !X.isHyperX) {
								if (X.player.weapon.ammo > 0) {
									player.changeWeaponSlot(RollingShield);
									player.press(Control.Shoot);
									player.release(Control.Shoot);
								}
							}

							int novaStrikeSlot = player.weapons.FindIndex(w => w is NovaStrike);
							if (player.hasUltimateArmor()) {
								X.player.changeWeaponSlot(novaStrikeSlot);
								if (X.player.weapon.ammo >= 16) {
									X.player.press(Control.Shoot);
								} else { X.player.changeWeaponSlot(getRandomWeaponIndex()); }
							}

							if (X.isHyperX) {
								if (X.unpoAbsorbedProj != null) {
									X.changeState(new XUPParryProjState(X.unpoAbsorbedProj, true, false), true);
									player.press(Control.WeaponLeft); player.release(Control.WeaponLeft);
									X.unpoAbsorbedProj = null;
									return;
								} else {
									X.changeState(new XUPParryStartState(), true); player.press(Control.WeaponLeft); player.release(Control.WeaponLeft);
								}
							}
						}
					}
				}
			}
		}
	}
	//End of X
	//Start of Saber Zero
	public void zeroAIAttack(Zero zero) {
		bool isTargetClose = target?.pos.x - 15 > character.pos.x;
		bool isTargetInAir = target?.pos.y < character.pos.y - 50;
		// Go hypermode 
		if (player.currency >= Player.zeroHyperCost && !zero.isSpriteInvulnerable() && !zero.isInvulnerable() &&
		   zero.charState is not (HyperZeroStart or LadderClimb) && !zero.hypermodeActive()
		) {
			zero.changeState(new HyperZeroStart(), true);
		}
		ComboAttacks(zero);
		WildDance(zero);
		if (zero.charState.attackCtrl && !player.isDead && zero.sprite.name != null && !isWildDance
			&& zero.charState.canAttack() && !zero.isSpriteInvulnerable() && !zero.isInvulnerable()
			) {
			int ZSattack = Helpers.randomRange(0, 11);
			if (!(zero.sprite.name == "zero_attack" || zero.sprite.name == "zero_attack3" || zero.sprite.name == "zero_attack2")) {
				switch (ZSattack) {
					//Randomizador
					case 0 when zero.grounded:
						zero.changeState(new ZeroSlash1State(), true);					
						break;
					case 1 when zero.grounded:
						zero.changeState(new ZeroUppercut(zero.uppercutA.type, zero.isUnderwater()), true);
						break;
					case 2 when zero.grounded:
						zero.changeState(new ZeroUppercut(zero.uppercutS.type, zero.isUnderwater()), true);
						break;				
					case 3 when zero.grounded && zero.canCrouch():
						zero.changeState(new ZeroCrouchSlashState(), true);
						break;
					case 4 when zero.charState is Dash:
						zero.changeState(new ZeroShippuugaState(), true);
						zero.slideVel = zero.xDir * zero.getDashSpeed() * 2f;
						break;		
					case 5 when zero.grounded:
						if (zero.gigaAttack.shootTime <= 0 && zero.gigaAttack.ammo >= zero.gigaAttack.getAmmoUsage(0)) {
							if (zero.gigaAttack is RekkohaWeapon) {
								zero.gigaAttack.addAmmo(-zero.gigaAttack.getAmmoUsage(0), player);
								zero.changeState(new Rekkoha(zero.gigaAttack), true);
							} else {
								zero.gigaAttack.addAmmo(-zero.gigaAttack.getAmmoUsage(0), player);
								zero.changeState(new Rakuhouha(zero.gigaAttack), true);
							}
						}
						break;		
					case 6 when zero.charState is Fall or Jump: 
					zero.changeState(new ZeroRollingSlashtate(), true);
						break;
					case 7 when zero.charState is Fall or Jump:
					zero.changeState(new ZeroAirSlashState(), true);
						break;	
					case 8 when zero.charState is Fall:
						zero.changeState(new ZeroDownthrust(zero.downThrustA.type), true);
						break;
					case 9 when zero.charState is Fall:
						zero.changeState(new ZeroDownthrust(zero.downThrustS.type), true);
						break;
					case 10 when zero.charState is Dash: 
						zero.changeState(new ZeroDashSlashState(), true);
						zero.slideVel = zero.xDir * zero.getDashSpeed() * 2f;
						break;
					case 11 when zero.grounded:
						zero.groundSpecial.attack(zero);
						break;
				}
			}
			// Hypermode attacks
			if (zero.hypermodeActive() && !player.isMainPlayer) {
				switch (Helpers.randomRange(0, 54)) {
					case 0 when !zero.isViral:
						zero.changeState(new Rakuhouha(zero.gigaAttack), true);
						break;
					case 1 when zero.isAwakened: // i would like to every hyper do Genmu, but okay
						zero.changeState(new GenmureiState(), true);
						break;
					case 2 when zero.isAwakened:
						zero.changeState(new AwakenedZeroHadangeki(), true);
						break;
				}
			}
		}
		if (isTargetInAir && zero.charState is Fall or Jump && !player.isMainPlayer) {
			zero.changeState(new ZeroUppercut(RisingType.Denjin, true), true);
		}
	}
	public void ComboAttacks(Zero zero) {
		if (!(zero.charState is HyperZeroStart or DarkHoldState or Hurt) &&
			zero.sprite.name != null && !player.isMainPlayer && !isWildDance
		) { //least insane else if chain be like:		
			if (zero.sprite.name == "zero_attack3") { //if zero is on third slash 
				switch (Helpers.randomRange(1,2)) { // A random of two scenarios
					case 1 when zero.sprite.frameIndex >= 10: //if is on 10th frame
						switch (Helpers.randomRange(1, 5)) {
							case 1:
								zero.groundSpecial.attack(zero);
								break;
							case 2:
								zero.changeState(new ZeroCrouchSlashState(), true);
								break;
							case 3:
								if (zero.gigaAttack.shootTime <= 0 && zero.gigaAttack.ammo >= zero.gigaAttack.getAmmoUsage(0)) {
									if (zero.gigaAttack is RekkohaWeapon) {
										zero.gigaAttack.addAmmo(-zero.gigaAttack.getAmmoUsage(0), player);
										zero.changeState(new Rekkoha(zero.gigaAttack), true);
									} else {
										zero.gigaAttack.addAmmo(-zero.gigaAttack.getAmmoUsage(0), player);
										zero.changeState(new Rakuhouha(zero.gigaAttack), true);
									}
								} 
								break;
							case 4:
								zero.changeState(new ZeroShippuugaState(), true);
								zero.slideVel = zero.xDir * zero.getDashSpeed() * 2f;
								break;
							case 5:
								zero.changeState(new ZeroDashSlashState(), true);
								zero.slideVel = zero.xDir * zero.getDashSpeed() * 2f;
								break;
						} 
					break;						
					case 2 when zero.sprite.frameIndex >= 7: // if is on 7th frame
						switch (Helpers.randomRange(1, 3)) {
							case 1:
								zero.changeState(new ZeroUppercut(RisingType.Denjin, true), true);
								break;
							case 2 when !zero.isUnderwater():
								zero.changeState(new ZeroUppercut(RisingType.Ryuenjin, false), true);
								break;
							case 3:
								zero.changeState(new ZeroUppercut(RisingType.RisingFang, true), true);
								break;
						} 
					break;
				}
			} 
			if (zero.sprite.name == "zero_ryuenjin" && zero.sprite.frameIndex >= 9 ||
				zero.sprite.name == "zero_eblade" && zero.sprite.frameIndex >= 11 ||
				zero.sprite.name == "zero_rising" && zero.sprite.frameIndex >= 5) {						
				switch (Helpers.randomRange(1,5)) { 
				//on the uppercuts in certain frames with a random of five cases
					case 1:
						zero.changeState(new ZeroDownthrust(ZeroDownthrustType.Hyouretsuzan), true);
						break;
					case 2:
						zero.changeState(new ZeroDownthrust(ZeroDownthrustType.Rakukojin), true);
						break;
					case 3:
						zero.changeState(new ZeroDownthrust(ZeroDownthrustType.QuakeBlazer), true); 
						break;
					case 4:
						zero.changeState(new ZeroRollingSlashtate(), true);
						break;
					case 5:
						zero.changeState(new ZeroAirSlashState(), true);
						break;
				}
			} 
			if (zero.sprite.name == "zero_raijingeki" && zero.sprite.frameIndex >= 26 || 
				zero.sprite.name == "zero_tbreaker" && zero.sprite.frameIndex >= 9 ||
				zero.sprite.name == "zero_spear" && zero.sprite.frameIndex >= 12) {
				//on the ground specials in certain frames with a random of three cases
				switch (Helpers.randomRange(1,3)) {
					case 1:
						zero.changeState(new ZeroDashSlashState(), true);
						zero.slideVel = zero.xDir * zero.getDashSpeed() * 2f;
						break;
					case 2:
						zero.changeState(new ZeroShippuugaState(), true);
						zero.slideVel = zero.xDir * zero.getDashSpeed() * 2f;
						break;
					case 3:
						zero.changeState(new FSplasherState(), true);
						break;
				}
			}
			if (zero.charState is Rakuhouha && zero.sprite.frameIndex >= 16 ||
				zero.charState is Rekkoha && zero.sprite.frameIndex >= 14) {
				//on the giga attacks in certain frames with a random of three cases
				switch (Helpers.randomRange(1,3)) {
					case 1:
						zero.changeState(new ZeroDashSlashState(), true);
						zero.slideVel = zero.xDir * zero.getDashSpeed() * 2f;
						break;
					case 2:
						zero.changeState(new ZeroShippuugaState(), true);
						zero.slideVel = zero.xDir * zero.getDashSpeed() * 2f;
						break;
					case 3:
						zero.changeState(new FSplasherState(), true);
						break;
				}
			}
			if(zero.sprite.name == "zero_attack_dash2" && zero.sprite.frameIndex >= 7) {
				//on shippuga in certain frame with a random of three cases
				switch(Helpers.randomRange(1,3)) {
					case 1:
						zero.changeState(new ZeroSlash1State(), true);
						break;
					case 2:
						switch (Helpers.randomRange(1, 3)) {
							case 1:
								zero.changeState(new ZeroUppercut(RisingType.Denjin, true), true);
								break;
							case 2 when !zero.isUnderwater():
								zero.changeState(new ZeroUppercut(RisingType.Ryuenjin, false), true);
								break;
							case 3:
								zero.changeState(new ZeroUppercut(RisingType.RisingFang, true), true);
								break;
						} 
						break;
					case 3:
						zero.changeState(new ZeroCrouchSlashState(), true);
						break;
				}
			}
		}
	}
	public void WildDance(Character zero) {
		if (character is Zero zero6) {
			if (player.health <= 4 && target != null && !player.isMainPlayer) {
				if (character.isFacing(target) && zero6.sprite.name != null && zero6.grounded) {
					WildDanceMove(zero);
					player.clearAiInput();
					isWildDance = true;
				}
			}
			if (player.health > 4) {
				isWildDance = false;
			}
		}
	}
	public void WildDanceMove(Character zero) {
		if (character is Zero zero7) {
			if (!zero7.isAttacking() && zero7.charState.canAttack() && zero7.charState.attackCtrl) {
				zero7.changeState(new ZeroShippuugaState(), true);
				zero7.slideVel = zero.xDir * zero7.getDashSpeed() * 2f;
			}
			if (zero.isAttacking()) {
				if (zero7.sprite.name == "zero_attack_dash2" && zero7.sprite.frameIndex >= 7) {
					zero7.changeState(new ZeroSlash1State(), true);
					zero7.stopMoving();				
				}
				if (zero7.sprite.name == "zero_attack3" && zero7.sprite.frameIndex >= 6) {
					zero7.changeState(new ZeroDashSlashState(), true);
					zero7.slideVel = zero.xDir * zero7.getDashSpeed() * 2f;	
				}
				if (zero7.sprite.name == "zero_attack_dash" && zero7.sprite.frameIndex >= 3) {
					zero7.playSound("gigaCrushAmmoFull");
					switch (Helpers.randomRange(1, 3)) {
						case 1:
							zero7.changeState(new ZeroUppercut(RisingType.Denjin, true), true);
							break;
						case 2 when !zero7.isUnderwater():
							zero7.changeState(new ZeroUppercut(RisingType.Ryuenjin, false), true);
							break;
						case 3:
							zero7.changeState(new ZeroUppercut(RisingType.RisingFang, true), true);
							break;
					}					
				}
			}
		}
	}
	public void zeroAIDodge(Character zero3, GameObject go) {
		var proj = go as Projectile;
				if (proj != null && proj.damager.owner.alliance != player.alliance) {
					if (player.character is Zero zero) {
						//Projectile is not 
						if (!(projId == (int)ProjIds.RollingShieldCharged || projId == (int)ProjIds.RollingShield
							|| projId == (int)ProjIds.MagnetMine || projId == (int)ProjIds.FrostShield
							|| projId == (int)ProjIds.FrostShieldAir || projId == (int)ProjIds.FrostShieldChargedPlatform)	
							) {
							// If a projectile is close to Zero
							if (character != null && proj.isFacing(character) &&
								character.withinX(proj, 100) && character.withinY(proj, 30) && 
								!player.isDead && zero.charState.canAttack() && zero.sprite.name != null &&
								(zero.charState is not HyperZeroStart or LadderClimb or DarkHoldState or Hurt
								 or GenericStun or Die or WarpIn or WarpOut or WallSlide or WallKick or SwordBlock)
							) {
								//Do i have giga attack ammo available?
								if (zero.gigaAttack.ammo >= 8f && zero.grounded) {
									//RAKUHOUHA!
									player.press(Control.Special1);
									player.press(Control.Down);
								} else if (!(projId == (int)ProjIds.SwordBlock) && zero.grounded) {
									//If he hasn't do "Block"						
									zero.turnToInput(player.input, player);
									zero.changeState(new SwordBlock());
								}
							}
						}
					}
				}	
	}
	//End of Saber Zero
	//Start of Punchy Zero
	public void KnuckleZeroAIAttack(PunchyZero pzero) {
		if (target == null || !pzero.charState.attackCtrl) {
			return;
		}		
		bool isTargetInAir = target.pos.y < character.pos.y - 50;
		bool isTargetClose = MathF.Abs(target.pos.x - character.pos.x) <= 32;
		bool canHitMaxCharge = (!isTargetInAir && pzero.getChargeLevel() >= 4);
		if (isTargetInAir && !pzero.grounded && pzero.charState is Fall or Jump) {
			pzero.changeState(new PZeroShoryuken(), true);
		}
		int ZKattack = Helpers.randomRange(0, 6);
		//Randomizer.
		switch (ZKattack) {
			// Punch.
			case 0 when pzero.grounded:
				pzero.changeState(new PZeroPunch1(), true);
				break;
			// Uppercut.
			case 1 when pzero.grounded:
				pzero.changeState(new PZeroShoryuken(), true);
				break;
			// If Zero is dashing, Spin Kick.
			case 2 when pzero.charState is Dash:
				pzero.changeState(new PZeroKick(), true);
				break;
			// If Zero is on the ground and has giga attack ammo of at least 8 to above do "Rakuhouha"
			case 3 when pzero.grounded && pzero.gigaAttack.ammo >= 16:
				pzero.changeState(new Rakuhouha(pzero.gigaAttack), true);
				pzero.gigaAttack.addAmmo(-16, player);
				break;
			// Megapunch.
			case 4 when pzero.grounded:
				pzero.changeState(new PZeroYoudantotsu(), true);
				break;
			// If the character is on fall state, Drop Kick.
			case 5 when pzero.charState is Fall:
				pzero.changeState(new PZeroDiveKickState(), true);
				break;
			// if the character is on Jump or Fall, Air Kick.
			case 6 when pzero.charState is Jump or Fall:
				pzero.changeState(new PZeroKick(), true);
				break;
		}
	}
	public void doKnuckleAI(PunchyZero pzero) {
		//if (!pzero.attackCtrl() && pzero.getChargeLevel() > 0) {
		//	pzero.increaseCharge();
		//}
	}
	public void knuckleZeroAIDodge(PunchyZero pzero) {
		foreach (GameObject gameObject in pzero.getCloseActors(64, true, false, false)) {
			if (gameObject is Projectile proj) {
				if (proj.damager.owner.alliance != player.alliance) {
					if (player.character is PunchyZero pzero1 &&
						!pzero1.isInvulnerable() && pzero.parryCooldown == 0 && pzero.charState.canAttack()) {
						if (character != null && proj.isFacing(character) && character.withinX(proj, 100) && character.withinY(proj, 30)) {
							if (pzero.gigaAttack.ammo >= 16 && pzero.grounded) {
								player.press(Control.Special1);
								player.press(Control.Down);
							} else {
								pzero1.turnToInput(player.input, player);
								pzero1.changeState(new PZeroParry(), true);
							}
						}
					}
				}
			}
		}
	}
	//End of Punchy Zero
	//Start of Buster Zero
	public void busterZeroAIAttack(BusterZero zero) {
		if (target == null) {
			zero.increaseCharge();
			return;
		}
		// Go hypermode 
		if (player.currency >= 10 && !zero.isBlackZero && !zero.isInvulnerable()
			&& zero.charState is not HyperBusterZeroStart and not WarpIn) {
			zero.changeState(new HyperBusterZeroStart(), true);
		}
		bool isTargetInAir = target.pos.y <= character.pos.y - 50;
		bool isTargetClose = MathF.Abs(target.pos.x - character.pos.x) <= 32;
		bool canHitMaxCharge = (!isTargetInAir && zero.getChargeLevel() >= 4);

		int ZBattack = Helpers.randomRange(0, 2);
		if (isTargetInAir && zero.vel.y >= 0) {
			player.press(Control.Jump);
		}
		if (!zero.isInvulnerable()) {
			switch (ZBattack) {
				// Release full charge if we have it.
				case >= 0 when canHitMaxCharge:
					player.press(Control.Shoot);
					break;
				// Saber swing when target is close.
				case 0 when isTargetClose:
					player.press(Control.Special1);
					break;
				// Another action if the enemy is on Do Jump and do SaberSwing.
				case 1 when isTargetClose:
					if (zero.vel.y >= 0) {
						player.press(Control.Jump);
					}
					player.press(Control.Special1);
					break;
				// Press Shoot to lemon.
				default:
					player.press(Control.Shoot);
					break;
			}
		}
	}
	public void doBusterZeroAI(BusterZero bzero) {
		//if (!bzero.attackCtrl()) {
		//	bzero.increaseCharge();
		//}
	}
	public void busterzeroAIDodge(BusterZero bzero) {
		foreach (GameObject gameObject in bzero.getCloseActors(64, true, false, false)) {
			if (gameObject is Projectile proj) {
				if (proj.damager.owner.alliance != player.alliance) {
					if (gameObject is not FrostShieldProj or FrostShieldProjAir
						or FrostShieldProjCharged or FrostShieldProjGround or FrostShieldProjPlatform //HOW MANY OF U EXIST
						) {
						if (player.character is BusterZero bzero1) {
							if (character != null && !bzero1.isInvulnerable() && proj.isFacing(character) &&
								character.withinX(proj, 100) && character.withinY(proj, 30)) {
								player.press(Control.Special1);
							}
						}
					}
				}
			}
		}
	}
	//End of Buster Zero
	//Start of Sigma
	public void sigmaAIAttack(Character sigma2) {
		bool isTargetInAir = target?.pos.y < character.pos.y - 50;
		bool isTargetBellowYou = target?.pos.y < character.pos.y + 10;
		bool isTargetSuperClose = target?.pos.x - 3 >= character.pos.x;
		//Sigma Start
		if (character is BaseSigma baseSigma) {
			bool once = false;
			if (baseSigma.player.weapon is MaverickWeapon mw &&
				mw.maverick == null && once == false &&
				baseSigma.canAffordMaverick(mw)
			) {
				baseSigma.buyMaverick(mw);
				if (mw.maverick != null) {
					baseSigma.changeState(new CallDownMaverick(mw.maverick, true, false), true);
				}
				mw.summon(player, baseSigma.pos.addxy(0, -112), baseSigma.pos, baseSigma.xDir);
				player.changeToSigmaSlot();
				once = true;
			}
			//Commander Sigma Start
			if (character is CmdSigma cmdSigma && character.charState is not LadderClimb) {
				int Sattack = Helpers.randomRange(0, 4);
				if (isTargetInAir) Sattack = 1;
				if (cmdSigma.charState.attackCtrl && cmdSigma?.charState?.isGrabbedState == false && !player.isDead
					&& !cmdSigma.isInvulnerable() && cmdSigma.charState.canAttack() 
					&& !(cmdSigma.charState is CallDownMaverick or SigmaSlashState)) {
					switch (Sattack) {
						case 0 when cmdSigma.saberCooldown == 0: // Beam Saber
							if (isTargetSuperClose) {
								cmdSigma.changeState(new SigmaSlashState(cmdSigma.charState), true);
								cmdSigma.saberCooldown = cmdSigma.sigmaSaberMaxCooldown;
							}
							break;
						case 1: // Machine Gun if the enemy is on the air
							if (cmdSigma.grounded && isTargetInAir) {
								cmdSigma?.changeState(new SigmaBallShoot(), forceChange: true);
							}
							break;
						case 2: // Triangle Kick
							if (cmdSigma.charState is Dash && cmdSigma.grounded) {
								cmdSigma.changeState(new SigmaWallDashState(cmdSigma.xDir, true), true);
							}
							break;
						case 3:
							if (!once) {
								cmdSigma.player.changeWeaponSlot(1);
								once = true;
							}
							break;
						case 4:
							if (!once) {
								cmdSigma.player.changeWeaponSlot(2);
								once = true;
							}
							break;
					}
				}
			}
			//Commander Sigma End

			//Neo Sigma Start
			if (character is NeoSigma neoSigma && character.charState is not LadderClimb) {
				int Neoattack = Helpers.randomRange(0, 5);
				if (isTargetInAir) Neoattack = 2;
				if (neoSigma?.charState?.isGrabbedState == false && !player.isDead && !neoSigma.isInvulnerable()
				    && neoSigma.charState.canAttack()
					&& !(neoSigma.charState is CallDownMaverick or SigmaElectricBall2State or SigmaElectricBallState)) {
					switch (Neoattack) {
						case 0 when neoSigma.saberCooldown == 0:
							neoSigma.changeState(new SigmaClawState(neoSigma.charState, neoSigma.grounded), true);
							neoSigma.saberCooldown = neoSigma.sigmaSaberMaxCooldown;
							break;
						case 1 when neoSigma.sigmaDownSlashCooldown == 0:
							if (neoSigma.grounded && isTargetInAir) {
								neoSigma.changeState(new SigmaUpDownSlashState(true), true);
								neoSigma.sigmaDownSlashCooldown = 1f;
							}
							break;
						case 2 when neoSigma.sigmaUpSlashCooldown == 0:
							if (!neoSigma.grounded && isTargetBellowYou) {
								neoSigma.changeState(new SigmaUpDownSlashState(false), true);
								neoSigma.sigmaUpSlashCooldown = 0.75f;
							}
							break;
						case 3:
							if (!once) {
								neoSigma.player.changeWeaponSlot(1);
								once = true;
							}
							break;
						case 4:
							if (!once) {
								neoSigma.player.changeWeaponSlot(2);
								once = true;
							}
							break;
						case 5:
							neoSigma.player.changeWeaponSlot(0);
							break;
						case 6:
							if (neoSigma.player.sigmaAmmo < 32) {
								neoSigma.player.sigmaAmmo -= 16;
								neoSigma.changeState(new SigmaElectricBallState(), true);
								} else {
								neoSigma.player.sigmaAmmo = 0;
								neoSigma.changeState(new SigmaElectricBall2State(), true);
								}
							break;
					}
				}
			}
			//Neo Sigma End

			//Doppma Sigma Start
			if (character is Doppma DoppmaSigma && character.charState is not LadderClimb) {
				int DoppmaSigmaAttack = Helpers.randomRange(0, 4);
				if (isTargetInAir) DoppmaSigmaAttack = 1;
				if (DoppmaSigma?.charState?.isGrabbedState == false && !player.isDead &&
				   !DoppmaSigma.isInvulnerable() && DoppmaSigma.charState.canAttack()
				   && !(DoppmaSigma.charState is CallDownMaverick or SigmaThrowShieldState or Sigma3Shoot)) {
					switch (DoppmaSigmaAttack) {
						case 0 when DoppmaSigma.sigma3FireballCooldown == 0:
							DoppmaSigma.changeState(new Sigma3Shoot(player.input.getInputDir(player)), true);
							DoppmaSigma.sigma3FireballCooldown = DoppmaSigma.maxSigma3FireballCooldown;
							break;
						case 1 when DoppmaSigma.sigma3ShieldCooldown == 0:
							if (DoppmaSigma.grounded) {
								DoppmaSigma.changeState(new SigmaThrowShieldState(), true);
								DoppmaSigma.sigma3ShieldCooldown = DoppmaSigma.maxSigma3ShieldCooldown;
							}
							break;
						case 2:
							if (!once) {
								DoppmaSigma.player.changeWeaponSlot(1);
								once = true;
							}
							break;
						case 3:
							if (!once) {
								DoppmaSigma.player.changeWeaponSlot(2);
								once = true;
							}
							break;
						case 4:
							DoppmaSigma.player.changeWeaponSlot(0);
							break;
					}
				}
			}
			//Doppma Sigma End
		}
	}
	public void doSigmaAI(Character sigma4) {
		// Sigma:
		// deactivated until fixed
		//if (character is BaseSigma baseSigma1) {
		//	
		//	if (player.canReviveSigma(out var spawnPoint)){
		//		player.reviveSigma(spawnPoint);
		//	}

		//if (jumpTime >= 0.4) {
		//	player.release(Control.Jump);
		//}
		//}	
	}
	public void sigmaAIDodge(Character sigma3) {
		foreach (GameObject gameObject in sigma3.getCloseActors(64, true, false, false)) {
			if (gameObject is Projectile proj) {
				if (proj.damager.owner.alliance != player.alliance) {
					//Start Of Sigma
					//Putting Sigma here
					if (player.character is BaseSigma baseSigma) {
						//If a projectile is close to Sigma
						if (character != null && proj.isFacing(character) &&
							character.withinX(proj, 150) && character.withinY(proj, 30)
						) {
							//Commander Sigma
							if (character is CmdSigma cmdSigma) {
								if (gameObject is not GenericMeleeProj) {
									//Do Block
									player.press(Control.Down);
								}
							}
							//Neo Sigma
							if (character is NeoSigma neoSigma) {
								// If Neo Sigma giga attack ammo is the same and higher than 16 but less than 24
								if (player.sigmaAmmo >= 16 && player.sigmaAmmo <= 24) {
									if (Global.time > 0.3f) {
										//Do "Better C-Flasher" 
										//Original name: 5 Bullet Shot (弾5発射 Dan 5 Hassha)
										player.press(Control.Special1);
									}
								}
								// If Neo Sigma giga attack ammo is 32
								else if (player.sigmaAmmo == 32) {
									if (Global.time > 0.3f) {
										//Do "I-Frames E-Spark move"
										//Original name: Electromagnetic Wave (電磁波 Denjiha) - Nightshade Electric Spark
										player.press(Control.Special1);
									}
								}
								// If Neo Sigma has giga attack ammo less than 16
								else if (player.sigmaAmmo < 16) {
									if (gameObject is not GenericMeleeProj) {
										//Do "Block"
										player.press(Control.Down);
									}
								}
							}
						}
					}
					//Doppma shouldn't get an AI to block something
					//dude literally holds the best shield.
					//End of Sigma
				}
			}
		}
	}
	//End of Sigma
	//Start of Vile
	public void vileAIAttack(Character vile2) {
		bool isTargetInAir = target?.pos.y < character.pos.y - 50;
		bool isTargetSuperClose = target?.pos.x - 3 >= character.pos.x;
		//Vile Start	
		if (character is Vile vile) {
			int Vattack = Helpers.randomRange(0, 12);
			if (vile?.charState?.isGrabbedState == false && !player.isDead && vile.charState.canAttack()
				&& !(character.charState is VileRevive or HexaInvoluteState or NecroBurstAttack
				or StraightNightmareAttack or RisingSpecterState or VileMK2GrabState)) {
				switch (Vattack) {
					case 0:
						player.press(Control.Shoot);
						break;
					case 1:
						player.weapon.vileShoot(WeaponIds.FrontRunner, vile);
						break;
					case 2:
						vile.rocketPunchWeapon.vileShoot(WeaponIds.RocketPunch, vile);
						break;
					case 3 when !vile.grounded:
						vile.grenadeWeapon.vileShoot(WeaponIds.VileBomb, vile);
						break;
					case 4:
						vile.missileWeapon.vileShoot(WeaponIds.ElectricShock, vile);
						break;
					case 5:
						vile.cutterWeapon.vileShoot(WeaponIds.VileCutter, vile);
						break;
					case 6 when vile.grounded:
						vile.napalmWeapon.vileShoot(WeaponIds.Napalm, vile);
						break;
					case 7 when vile.charState is Fall:
						vile.flamethrowerWeapon.vileShoot(WeaponIds.VileFlamethrower, vile);
						break;
					case 8 when player.vileAmmo >= 24:
						vile.laserWeapon.vileShoot(WeaponIds.VileLaser, vile);
						break;
					case 9 when vile.isVileMK5 && player.vileAmmo >= 20 && !player.isMainPlayer:
						vile?.changeState(new HexaInvoluteState(), true);
						break;
				}
			}
		}
	}
	public void doVileAI(Character vile4) {
		// Vile: Go MK2 to MKV
		// Do not remove isMainPlayer
		if (character is Vile vile1 && !player.isMainPlayer) {
			if (player.canReviveVile() && vile1.isVileMK1) {
				player.reviveVile(false);
			}
			if (vile1.isVileMK2 && player.canReviveVile()) {
				player.reviveVile(true);
			}
			/*		
			if (vile1.vileStartRideArmor == null && vile1.grounded && !player.isMainPlayer) {
				if (vile1.canAffordRideArmor()) {
					if (!(vile1.charState is Idle || vile1.charState is Run || vile1.charState is Crouch)) return;
					else {
						vile1.alreadySummonedNewMech = false;
						if (vile1.vileStartRideArmor != null) vile1.vileStartRideArmor.selfDestructTime = 1000;
						vile1.buyRideArmor();
						int raIndex = player.selectedRAIndex;
						if (vile1.isVileMK1) {
							raIndex = Helpers.randomRange(0,3);
						}
						if (vile1.isVileMK2 || vile1.isVileMK5) raIndex = 4;
						vile1.vileStartRideArmor = new RideArmor(player, vile1.pos, raIndex, 0, player.getNextActorNetId(), true, sendRpc: true);
						if (vile1.isVileMK5) {
							vile1.vileStartRideArmor.ownedByMK5 = true;
							vile1.vileStartRideArmor.zIndex = vile1.zIndex - 1;
							player.weaponSlot = 0;
							if (player.weapon is MechMenuWeapon) player.weaponSlot = 1;
						}
						vile1.changeState(new CallDownMech(vile1.vileStartRideArmor, true), true);
						vile1.alreadySummonedNewMech = true;

					}
				}
			}
			*/
		}
	}
	//End of Vile
	//Start of Axl
	public void axlAIAttack(Character axl2) {
		//Axl Start
		if (character is Axl axl) {
			if (player.axlHyperMode == 0 && player.currency >= 10 && !player.isDead && !axl.isSpriteInvulnerable() && !axl.isInvulnerable() && !axl.isWhiteAxl()
				&& !(axl.charState is Hurt or Die or GenericStun or WarpIn or HyperAxlStart or WallSlide or WallKick or DodgeRoll)) {
				axl.changeState(new HyperAxlStart(axl.grounded), true);
			}
			if (player.axlHyperMode == 1 && player.currency >= 10 && !player.isDead && !axl.isSpriteInvulnerable() && !axl.isInvulnerable() && !axl.isStealthMode()
				&& !(axl.charState is Hurt or Die or GenericStun or WarpIn or HyperAxlStart or WallSlide or WallKick or DodgeRoll)) {
				axl.stingChargeTime = 12;
			}

			int AAttack = Helpers.randomRange(0, 1);
			if (axl.charState.canShoot() && !axl.isSpriteInvulnerable() && player.weapon.ammo > 0 && player.axlWeapon != null && axl.canShoot()
				&& axl?.charState?.isGrabbedState == false && !player.isDead && axl.canChangeWeapons() && character.canChangeWeapons()
				&& !(axl.charState is Hurt or Die or GenericStun or WarpIn or HyperAxlStart or WallSlide or WallKick or LadderClimb or DodgeRoll)) {
				switch (AAttack) {
					case 0:
						player.press(Control.Shoot);
						break;
					case 1 when axl.player.weapon is not IceGattling or PlasmaGun:
						player.press(Control.Special1);
						break;
				}
			}
		}
	}
	public void doAxlAI(Character axl4) {
		// Axl: 
		if (character is Axl axl5) {
			if (player.weapon is not IceGattling) {
				player.release(Control.Shoot);
			}
			if (player.weapon is not IceGattling or PlasmaGun or RayGun) {
				player.release(Control.Special1);
			}
			if (Helpers.randomRange(0, 10) < 1) {
				player.release(Control.Jump);
			}
		}
	}
	public void axlAIDodge(Character axl3) {
		foreach (GameObject gameObject in axl3.getCloseActors(64, true, false, false)) {
			if (gameObject is Projectile proj) {
				if (proj.damager.owner.alliance != player.alliance) {
					//Start of Axl
					//Putting Axl
					if (player.character is Axl axl) {
						if (character != null && proj.isFacing(character) &&
							character.withinX(proj, 150) && character.withinY(proj, 30)) {
							//Dodge Roll if your DodgeRollCooldown is on 0 and you are not in Dodgeroll State, also if you are on ground and can dash.
							//and we count that you are not on hurt, die, frozen, crystalized, stunned, or grabbed state.. :skull:
							if (axl.grounded && axl.canDash() && axl.charState is not DodgeRoll && axl.dodgeRollCooldown == 0 &&
								(axl.player.axlWeapon?.isTwoHanded(true) == true || axl.isZooming())
								&& axl.charState.normalCtrl &&
								axl?.charState?.isGrabbedState == false
							) {
								axl.changeState(new DodgeRoll());
							}
							//Use Airblast If Axl has flameburner as his weapon and has air blast as its alt, as the projectile is not Melee and is reflectable by airblast (why this exists?), which its ammo should be higher than 0
							else if (axl?.player.weapon is FlameBurner && axl.player.axlLoadout.flameBurnerAlt == 1 && (gameObject is not GenericMeleeProj || (proj.reflectableFBurner == true)) && axl.player.weapon.ammo > 0) {
								player.press(Control.Special1);
							} else {
								doJump(0.75f);
							}
							//Or just Jump
						}
					}
				}
			}
		}
	}
	//End of Axl

} // End of AI

public class AIState {
	public bool facePlayer;
	public Character character;
	public bool shouldAttack;
	public bool shouldDodge;
	public bool randomlyChangeState;
	public bool randomlyDash;
	public bool randomlyJump;
	public bool randomlyChangeWeapon;
	public bool randomlyChargeWeapon;

	public Player player {
		get {
			return character.player;
		}
	}

	public AI ai {
		get {
			if (player.character?.ai != null) {
				return player.character.ai;
			} else if (player.limboChar?.ai != null) {
				return player.limboChar.ai;
			} else {
				return new AI(character);
			}
		}
	}

	public Actor? target {
		get {
			return ai?.target;
		}
	}

	public string getPrevNodeName() {
		if (this is FindPlayer findPlayer) {
			return findPlayer.prevNode?.name ?? "";
		}
		return "";
	}

	public string getNextNodeName() {
		if (this is FindPlayer findPlayer) {
			return findPlayer.nextNode?.name ?? "";
		}
		return "";
	}

	public string getDestNodeName() {
		if (this is FindPlayer findPlayer) {
			return findPlayer.destNode?.name ?? "";
		}
		return "";
	}

	public bool canChangeTo() {
		return character.charState is not LadderClimb && character.charState is not LadderEnd;
	}

	public AIState(Character character) {
		this.character = character;
		shouldAttack = true;
		facePlayer = true;
		shouldDodge = true;
		randomlyChangeState = true;
		randomlyDash = true;
		randomlyJump = true;
		randomlyChangeWeapon = true;
		randomlyChargeWeapon = true;
	}

	public virtual void update() {
		if (character.charState is LadderClimb && this is not FindPlayer) {
			player.press(Control.Down);
			player.press(Control.Jump);
		}
	}
}

public class MoveTowardsTarget : AIState {
	public MoveTowardsTarget(Character character) : base(character) {
		facePlayer = true;
		shouldAttack = true;
		shouldDodge = true;
		randomlyChangeState = false;
		randomlyDash = true;
		randomlyJump = true;
		randomlyChangeWeapon = false;
		randomlyChargeWeapon = true;
	}

	public override void update() {
		base.update();
		if (ai.target == null) {
			ai.changeState(new FindPlayer(character));
			return;
		}

		if (character.pos.x - ai.target.pos.x > ai.getMaxDist()) {
			player.press(Control.Left);
		} else if (character.pos.x - ai.target.pos.x < -ai.getMaxDist()) {
			player.press(Control.Right);
		} else {
			ai.changeState(new AimAtPlayer(character));
		}
	}
}

public class FindPlayer : AIState {
	public NavMeshNode? destNode;
	public NavMeshNode? nextNode;
	public NavMeshNode? prevNode;
	public NavMeshNeighbor? neighbor;
	public NodeTransition? nodeTransition;
	public List<NavMeshNode> nodePath = new();
	public float stuckTime;
	public float lastX;
	public float runIntoWallTime;
	public FindPlayer(Character character) : base(character) {
		facePlayer = true;
		shouldAttack = true;
		shouldDodge = true;
		randomlyChangeState = false;
		randomlyDash = true;
		randomlyJump = true;
		randomlyChangeWeapon = false;
		randomlyChargeWeapon = true;

		setDestNodePos();
	}

	public override void update() {
		base.update();

		if (nextNode == null) {
			ai.changeState(new FindPlayer(character));
			return;
		}

		if (nodeTransition != null) {
			nodeTransition.update();
			if (nodeTransition.failed) {
				ai.changeState(new FindPlayer(character));
				return;
			} else if (!nodeTransition.completed) {
				return;
			}
		}

		float xDist = character.pos.x - nextNode.pos.x;
		if (MathF.Abs(xDist) > 2.5f) {
			if (xDist < 0) {
				player.press(Control.Right);
			} else if (xDist > 0) {
				player.press(Control.Left);
			}
			if (character.pos.x == lastX && character.grounded) {
				runIntoWallTime += Global.spf;
				if (runIntoWallTime > 2) {
					setDestNodePos();
				}
			}
			lastX = character.pos.x;
		} else {
			// States where it's possible to move to the next node. As more special situations are added this may need to grow
			bool isValidTransitionState = character.grounded || neighbor?.isDestNodeInAir == true || character.charState is LadderClimb;

			if (Math.Abs(character.abstractedActor().pos.y - nextNode.pos.y) < 30 && isValidTransitionState) {
				goToNextNode();
			} else {
				stuckTime += Global.spf;
				if (stuckTime > 2) {
					setDestNodePos();
				}
			}
		}
	}
	public void goToNextNode() {
		if (nextNode == destNode) {
			setDestNodePos();
		} else {
			prevNode = nextNode;
			nextNode = nodePath.PopFirst();
		}
		if (nextNode != null) {
			neighbor = prevNode?.getNeighbor(nextNode);
		}
		if (neighbor != null) {
			var phases = neighbor.getNodeTransitionPhases(this);
			if (phases.Count > 0) {
				nodeTransition = new NodeTransition(phases);
			} else {
				nodeTransition = null;
			}
		}
	}

	public void setDestNodePos() {
		runIntoWallTime = 0;
		stuckTime = 0;
		if (Global.level.gameMode is Race) {
			destNode = Global.level.goalNode;
		} else if (Global.level.gameMode is CTF && player.alliance < 1) {
			if (character.flag == null) {
				Flag targetFlag = Global.level.blueFlag;
				if (player.alliance == GameMode.redAlliance) targetFlag = Global.level.blueFlag;
				else if (player.alliance == GameMode.blueAlliance) targetFlag = Global.level.redFlag;
				if (targetFlag != null) {
					destNode = Global.level.getClosestNodeInSight(targetFlag.pos);
				}
				destNode ??= Global.level.getRandomNode();
			} else {
				if (player.alliance == GameMode.blueAlliance) destNode = Global.level.blueFlagNode;
				else if (player.alliance == GameMode.redAlliance) destNode = Global.level.redFlagNode;
			}
		} else if (Global.level.gameMode is ControlPoints) {
			var cp = Global.level.getCurrentControlPoint();
			if (cp == null) {
				destNode = Global.level.getRandomNode();
			} else {
				destNode = cp.navMeshNode;
			}
		} else if (Global.level.gameMode is KingOfTheHill) {
			var cp = Global.level.hill;
			destNode = cp.navMeshNode;
		} else {
			destNode = Global.level.getRandomNode();
		}
		if (Global.level.navMeshNodes.Count == 2) {
			nextNode = destNode;
		} else {
			nextNode = Global.level.getClosestNodeInSight(character.getCenterPos());
		}
		prevNode = null;

		if (nextNode != null) {
			if (destNode != null) {
				nodePath = nextNode.getNodePath(destNode);
			}
			nodePath.Remove(nextNode);
		}
	}
}

public class MoveToPos : AIState {
	public Point dest;
	public MoveToPos(Character character, Point dest) : base(character) {
		this.dest = dest;
		facePlayer = false;
		randomlyChangeState = false;
		randomlyChargeWeapon = true;
	}

	public override void update() {
		base.update();
		var dir = 0;
		if (character.pos.x - dest.x > 5) {
			dir = -1;
			player.press(Control.Left);
		} else if (character.pos.x - dest.x < -5) {
			dir = 1;
			player.press(Control.Right);
		} else {
			ai.changeState(new AimAtPlayer(character));
		}

		if (character.sweepTest(new Point(dir * 5, 0)) != null) {
			ai.changeState(new AimAtPlayer(character));
		}
	}
}

public class AimAtPlayer : AIState {
	public float jumpDelay = 0;
	public AimAtPlayer(Character character) : base(character) {
	}

	public override void update() {
		base.update();
		if (character.grounded && jumpDelay > 0.3) {
			jumpDelay = 0;
		}

		//if (target != null && character.pos.y > target.pos.y && character.pos.y < target.pos.y + 80) {
		//	jumpDelay += Global.spf;
		//	if (jumpDelay > 0.3) {
		//		ai.doJump();
		//	}
		//} else {
		//this.changeState(new JumpToWall());
		//}
	}
}

public class InJumpZone : AIState {
	public JumpZone jumpZone;
	public int jumpZoneDir;
	public float time = 0.25f;

	public InJumpZone(Character character, JumpZone jumpZone, int jumpZoneDir) : base(character) {
		this.jumpZone = jumpZone;
		this.jumpZoneDir = jumpZoneDir;
		facePlayer = false;
		shouldAttack = false;
		shouldDodge = false;
		randomlyChangeState = false;
		randomlyDash = true;
		randomlyJump = false;
		randomlyChangeWeapon = false;
		randomlyChargeWeapon = true;
	}

	public override void update() {
		base.update();
		time += Global.spf;
		ai.doJump();
		ai.jumpZoneTime += Global.spf;

		if (jumpZoneDir == -1) {
			player.press(Control.Left);
		} else if (jumpZoneDir == 1) {
			player.press(Control.Right);
		}

		//Check if out of zone
		if (character != null && character.abstractedActor().collider != null) {
			if (!character.abstractedActor().collider.isCollidingWith(jumpZone.collider)) {
				ai.changeState(new FindPlayer(character));
			}
		}
	}
}
