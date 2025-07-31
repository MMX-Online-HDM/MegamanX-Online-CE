using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using SFML.Graphics;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public enum MaverickAIBehavior {
	Defend,
	Follow,
	Control,
	Attack
}

public enum MaverickModeId {
	Summoner,
	Puppeteer,
	Striker,
	TagTeam,
}

public class SavedMaverickData {
	public bool noArmor;
	public int cloakUses;
	public Dictionary<Type, MaverickStateCooldown> stateCooldowns;
	public float ammo;

	public SavedMaverickData(Maverick maverick) {
		//if (maverick is ArmoredArmadillo aa) noArmor = aa.noArmor;
		ammo = maverick.ammo;
		stateCooldowns = maverick.stateCooldowns;
	}

	public void applySavedMaverickData(Maverick maverick, bool keepCooldowns) {
		if (maverick == null) return;
		//if (maverick is ArmoredArmadillo aa) aa.noArmor = noArmor;
		if (keepCooldowns) {
			maverick.stateCooldowns = stateCooldowns;
		}
		maverick.ammo = ammo;
	}
}

public class Maverick : Actor, IDamagable {
	// HP stuff.
	public float health;
	public float maxHealth;
	public bool alive;
	private float healAmount = 0;
	public float healTime = 0;

	// Ammo stuff.
	public float weaponHealAmount = 0;
	public float weaponHealTime = 0;
	public bool playHealSound;
	public MaverickModeId controlMode;
	public MaverickModeId trueControlMode;

	public MaverickWeapon rootWeapon;
	public Character? ownerChar;

	// New ammo variables.
	public float ammo = 32;
	public float maxAmmo = 32;
	public float grayAmmoLevel = 0;
	public bool usesAmmo = false;
	public bool canHealAmmo = false;
	public float? ammoHealScale = null;
	public bool ammoRoundDown = false;
	public (int icon, int units) barIndexes = (0, 0);

	// Movement.
	public bool useChargeJump;
	public bool canStomp;
	public float dashSpeed = 1;
	public bool canClimb;
	public bool canClimbWall;
	public bool canFly;
	public float maxFlyBar = 16;
	public float flyBar = 16;
	public (int icon, int units) flyBarIndexes = (0, 0);

	// Defense.
	public float weaknessCooldown;
	public ArmorClass armorClass = ArmorClass.Medium;
	public enum ArmorClass {
		Light,
		Medium,
		Heavy
	}
	public GameMavs gameMavs = GameMavs.X1;
	public enum GameMavs {
		X1,
		X2,
		X3
	}

	// Other vars.
	public float width;
	public float height;
	public const float maxWidth = 26;
	public MaverickState state;
	public Player player;
	public bool changedStateInFrame;
	public Dictionary<Type, MaverickStateCooldown> stateCooldowns = new();
	public Point? lastGroundedPos;
	public bool autoExit;
	public float autoExitTime;
	public float strikerTime;
	public float? maxStrikerTime;
	public int attackDir;
	public SubTank usedSubtank;
	public float netSubtankHealAmount;
	public float invulnTime = 0;

	public MaverickAIBehavior aiBehavior;
	public Actor target;
	public float aiCooldown;
	public float maxAICooldown = 75;
	public int startMoveControl = -1;

	public Weapon weapon;
	public WeaponIds awardWeaponId;
	public WeaponIds weakWeaponId;
	public WeaponIds weakMaverickWeaponId;

	// For sprite draw.
	public Sprite subtankSprite = new Sprite("menu_subtank");
	public Sprite subtankBarSprite = new Sprite("menu_subtank_bar");
	public Sprite cursorSprite = new Sprite("cursorchar");

	private Input _input;
	public Input input {
		get {
			if (aiBehavior == MaverickAIBehavior.Control && !isPuppeteerTooFar() && maverickCanControl()) {
				return player.input;
			}
			return _input;
		}
	}
	public bool isAI => aiBehavior != MaverickAIBehavior.Control && !player.isAI;

	public bool maverickCanControl() {
		if (this is StingChameleon sc && sc.isCloakTransition()) {
			return false;
		}
		if (this is MorphMothCocoon mmc && (mmc.selfDestructTime > 0 || mmc.isBurned)) {
			return false;
		}
		if (this is CrystalSnail cs && cs.sprite.name.EndsWith("shell_end")) {
			return false;
		}
		return true;
	}

	public bool isPuppeteerTooFar() {
		return (
			controlMode == MaverickModeId.Puppeteer &&
			ownerChar != null &&
			getCenterPos().distanceTo(ownerChar.pos) > 374
		);
	}

	public Maverick(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer,
		MaverickState? overrideState = null
	) : base(
		"", pos, netId, ownedByLocalPlayer, true
	) {
		this.player = player;
		this.xDir = xDir;

		spriteToCollider["enter"] = null;
		spriteToCollider["exit"] = null;

		Rect idleRect = Global.sprites[getMaverickPrefix() + "_idle"].frames[0].rect;
		width = Math.Min(idleRect.w() - 20, maxWidth);
		height = Math.Min(idleRect.h(), BaseSigma.sigmaHeight);
		if (this is MorphMothCocoon) width = 20;
		if (this is MorphMoth) width = 18;
		if (this is BlizzardBuffalo) width = 30;

		int heightInt = (int)height;

		if (ownedByLocalPlayer) {
			// Sort mavericks by their height. Unless the maverick height is >= sigma height it should go above sigma
			zIndex = ZIndex.MainPlayer - (heightInt - (int)BaseSigma.sigmaHeight);
			if (zIndex == ZIndex.MainPlayer) zIndex = ZIndex.Character - 100;
		} else {
			zIndex = ZIndex.Character - (heightInt - (int)BaseSigma.sigmaHeight);
			if (zIndex == ZIndex.Character) zIndex = ZIndex.Character - 100;
		}

		useFrameProjs = true;
		maxHealth = player.getMaverickMaxHp(controlMode);
		health = maxHealth;
		splashable = true;
		state = new MLimboState();
		state.maverick = this;
		if (ownedByLocalPlayer) {
			changeState(overrideState ?? new MEnter(destPos));
		}
		_input = new Input(true);

		if (Global.level.gameMode.isTeamMode && Global.level.mainPlayer != player) {
			RenderEffectType? allianceEffect = player.alliance switch {
				0 => RenderEffectType.BlueShadow,
				1 => RenderEffectType.RedShadow,
				2 => RenderEffectType.GreenShadow,
				3 => RenderEffectType.PurpleShadow,
				4 => RenderEffectType.YellowShadow,
				5 => RenderEffectType.OrangeShadow,
				_ => null
			};
			if (allianceEffect != null) {
				addRenderEffect(allianceEffect.Value, time: 1);
			}
		}

		Global.level.addGameObject(this);

		aiBehavior = player.currentMaverickCommand;
	}

	float ammoRechargeTime;
	public void rechargeAmmo(float amountPerSecond) {
		float ammoRechargeCooldown = 1 / amountPerSecond;
		ammoRechargeTime -= Global.spf;
		if (ammoRechargeTime <= 0) {
			ammoRechargeTime = ammoRechargeCooldown;
			ammo++;
			if (ammo > 32) ammo = 32;
		}
	}

	float ammoDrainTime;
	public void drainAmmo(float amountPerSecond) {
		float ammoDrainCooldown = 1 / amountPerSecond;
		ammoDrainTime -= Global.spf;
		if (ammoDrainTime <= 0) {
			ammoDrainTime = ammoDrainCooldown;
			ammo--;
			if (ammo < 0) ammo = 0;
		}
	}

	public void addHealth(float amount, bool fillSubtank) {
		if (health >= maxHealth && fillSubtank) {
			player.fillSubtank(amount);
		}
		healAmount += amount;
	}

	public virtual void setHealth(float lastHealth) {
		health = lastHealth;
	}

	public void addAmmo(float amount) {
		weaponHealAmount += amount;
	}

	public void deductAmmo(int v) {
		ammo -= v;
		if (ammo < 0) ammo = 0;
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();

		if (!ownedByLocalPlayer) {
			return;
		}

		Helpers.decrementTime(ref invulnTime);
		Helpers.decrementFrames(ref weaknessCooldown);

		foreach (var key in stateCooldowns.Keys) {
			Helpers.decrementFrames(ref stateCooldowns[key].cooldown);
		}
		if (grounded) {
			lastGroundedPos = pos;
		}

		useChargeJump = controlMode == MaverickModeId.Puppeteer;

		// Striker auto-exit for some states.
		if (maxStrikerTime != null) {
			if (strikerTime >= maxStrikerTime && state is not MExit) {
				maxStrikerTime = null;
				changeState(new MExit(pos, true));
			} else {
				strikerTime += speedMul;
			}
		}
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) {
			return;
		}

		if (grounded) {
			lastGroundedPos = pos;
			if (canFly && flyBar < maxFlyBar) {
				flyBar += 1 * Global.speedMul;
				if (flyBar > maxFlyBar) { flyBar = maxFlyBar; }
			}
		}

		if (ammo >= maxAmmo) {
			weaponHealAmount = 0;
		}
		if (weaponHealAmount > 0 && health > 0) {
			weaponHealTime += speedMul;
			if (weaponHealTime > 3) {
				weaponHealTime = 0;
				weaponHealAmount--;
				ammo = Helpers.clampMax(ammo + 1, maxAmmo);
				if (ammo >= maxAmmo) {
					weaponHealTime = 0;
					weaponHealAmount = 0;
				}
				if (gameMavs == Maverick.GameMavs.X1) {
					playSound("heal", forcePlay: true, sendRpc: true);
				} else if (gameMavs == Maverick.GameMavs.X2) {
					playSound("healX2", forcePlay: true, sendRpc: true);
				} else if (gameMavs == Maverick.GameMavs.X3) {
					playSound("healX3", forcePlay: true, sendRpc: true);
				}
			}
		}

		if (health >= maxHealth || !alive) {
			healAmount = 0;
			usedSubtank = null;
		}
		if (healAmount > 0 && alive) {
			healTime += Global.spf;
			if (healTime > 0.05) {
				healTime = 0;
				healAmount--;
				if (usedSubtank != null) {
					usedSubtank.health--;
				}
				health = Helpers.clampMax(health + 1, maxHealth);
				if (player == Global.level.mainPlayer || playHealSound) {
					if (gameMavs == Maverick.GameMavs.X1) {
						playSound("heal", forcePlay: true, sendRpc: true);
					} else if (gameMavs == Maverick.GameMavs.X1) {
						playSound("healX2", forcePlay: true, sendRpc: true);
					} else if (gameMavs == Maverick.GameMavs.X1) {
						playSound("healX3", forcePlay: true, sendRpc: true);
					}
				}
			}
		}

		if (usedSubtank != null && usedSubtank.health <= 0) {
			usedSubtank = null;
		}

		if (pos.y > Global.level.killY && state is not MEnter && state is not MExit) {
			incPos(new Point(0, 50));
			applyDamage(Damager.envKillDamage, player, this, null, null);
		}

		if (autoExit) {
			autoExitTime += speedMul;
			if (autoExitTime > 10 && state is not MExit) {
				changeState(new MExit(pos, true));
			}
			return;
		}
		if (aiBehavior != MaverickAIBehavior.Control) {
			aiUpdate();
		}
		updateCtrl();
	}

	public virtual bool updateCtrl() {
		if (state.exitOnLanding && grounded) {
			state.landingCode();
			return true;
		}
		if (state.exitOnAirborne && !grounded) {
			changeState(new MFall());
			return true;
		}
		if (!useChargeJump &&
			state.canStopJump &&
			!state.stoppedJump &&
			!grounded && vel.y < 0 &&
			!player.input.isHeld(Control.Jump, player)
		) {
			vel.y *= 0.21875f;
			state.stoppedJump = true;
		}
		if (state.normalCtrl && canClimb) {
			state.climbIfCheckClimbTrue();
		}
		if (state.normalCtrl && canClimbWall) {
			state.wallClimbCode();
		}
		if (state.canJump && grounded && player.input.isPressed(Control.Jump, player)) {
			grounded = false;
			if (state.canStopJump) {
				state.stoppedJump = false;
			}
			vel.y = -getJumpPower();
			playSound("jump", sendRpc: true);
		}
		if (state.normalCtrl) {
			normalCtrl();
		}
		if (state.attackCtrl && invulnTime <= 0) {
			return attackCtrl();
		}
		return false;
	}

	public virtual bool normalCtrl() {
		if (input.isPressed(Control.Up, player) && canFly && state is not MFly && !state.wasFlying) {
			stopMovingWeak();
			incPos(new Point(0, -4));
			changeState(new MFly());
			return true;
		}
		if (grounded) {
			if (input.isPressed(Control.Taunt, player)) {
				changeState(new MTaunt());
				return true;
			}
			if (input.isPressed(Control.Jump, player)) {
				changeState(new MJumpStart());
				return true;
			}
			if (player.input.isPressed(Control.Down, player)) {
				checkLadderDown = true;
				List<CollideData> ladders = Global.level.getTerrainTriggerList(
					this, new Point(0, 1), typeof(Ladder)
				);
				if (ladders.Count > 0) {
					Rect rect = ladders[0].otherCollider.shape.getRect();
					float snapX = (rect.x1 + rect.x2) / 2;
					float xDist = snapX - pos.x;
					if (MathF.Abs(xDist) < 10 &&
						Global.level.checkTerrainCollisionOnce(this, xDist, 30) == null
					) {
						if (!canClimb) {
							move(new Point(xDist, 1), false);
						} else {
							changeState(new StingCClimb());
							move(new Point(0, 30), false);
							player.character.stopCamUpdate = true;
							changePos(new Point(snapX, pos.y));
							if (player == Global.level.mainPlayer) {
								Global.level.lerpCamTime = 0.25f;
							}
						}
					}
				}
				checkLadderDown = false;
			}
			return false;
		}
		if (state.airMove) {
			int inputDir =  input.getXDir(player);
			if (inputDir != 0 && (state is not MWallKick wallKick || wallKick.kickSpeed == 0)) {
				xDir = inputDir;
				move(new Point(inputDir * getRunSpeed() * getDashSpeed() * getAirSpeed(), 0));
			}
		}
		return false;
	}

	public virtual bool attackCtrl() {
		return false;
	}
	
	public override void statePreUpdate() {
		state.stateFrame += speedMul;
		state.preUpdate();
	}


	public override void stateUpdate() {
		state.update();
	}
	
	public override void statePostUpdate() {
		state.postUpdate();
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.myCollider?.flag == (int)HitboxFlag.Hitbox || other.myCollider?.flag == (int)HitboxFlag.None) return;

		// Move zone movement.
		if (other.gameObject is MoveZone moveZone) {
			if (moveZone.moveVel.x != 0) {
				xPushVel = moveZone.moveVel.x;
			}
			if (moveZone.moveVel.y != 0) {
				yPushVel = moveZone.moveVel.y;
			}
		}

		var killZone = other.gameObject as KillZone;
		if (killZone != null) {
			if (!killZone.killInvuln && this is StingChameleon sc && sc.isInvisible) return;
			if (state is not MEnter && state is not MExit) {
				killZone.applyDamage(this);
			}
		}
	}

	public override Point getCenterPos() {
		return pos.addxy(0, -height / 2f);
	}

	public int getMaverickKillFeedIndex() {
		if (this is ChillPenguin) return 93;
		if (this is SparkMandrill) return 94;
		if (this is ArmoredArmadillo) return 95;
		if (this is LaunchOctopus) return 96;
		if (this is BoomerangKuwanger) return 97;
		if (this is StingChameleon) return 98;
		if (this is StormEagle) return 99;
		if (this is FlameMammoth) return 100;
		if (this is Velguarder) return 101;

		if (this is WireSponge) return 141;
		if (this is WheelGator) return 142;
		if (this is BubbleCrab) return 143;
		if (this is FlameStag) return 144;
		if (this is MorphMothCocoon) return 145;
		if (this is MorphMoth) return 146;
		if (this is MagnaCentipede) return 147;
		if (this is CrystalSnail) return 148;
		if (this is OverdriveOstrich) return 149;
		if (this is FakeZero) return 150;

		if (this is BlizzardBuffalo) return 151;
		if (this is ToxicSeahorse) return 152;
		if (this is TunnelRhino) return 153;
		if (this is VoltCatfish) return 154;
		if (this is CrushCrawfish) return 155;
		if (this is NeonTiger) return 156;
		if (this is GravityBeetle) return 157;
		if (this is BlastHornet) return 158;
		if (this is DrDoppler) return 159;

		return 0;
	}

	public virtual void aiUpdate() {
		input.keyPressed.Clear();
		input.keyHeld.Clear();

		Helpers.decrementFrames(ref aiCooldown);

		bool isSummonerOrStrikerDoppler = (
			controlMode is MaverickModeId.Striker or MaverickModeId.Summoner
		) && this is DrDoppler;
		bool isSummonerCocoon = controlMode == MaverickModeId.Summoner && this is MorphMothCocoon;
		bool isStrikerCocoon = controlMode == MaverickModeId.Striker && this is MorphMothCocoon;
		var mmc = this as MorphMothCocoon;
		var doppler = this as DrDoppler;

		if (isSummonerOrStrikerDoppler) {
			var hit = Global.level.raycast(getCenterPos(), getCenterPos().addxy(xDir * 100, 0), new List<Type>() { typeof(Character) });
			if (hit?.gameObject is Character chr && chr.player.alliance == player.alliance) {
				target = chr;
				doppler.ballType = 1;
			} else {
				target = Global.level.getClosestTarget(getCenterPos(), player.alliance, true, isRequesterAI: true);
				doppler.ballType = 0;
			}
		} else if (isSummonerCocoon || isStrikerCocoon) {
			target = mmc.getHealTarget();
		} else if (controlMode is not MaverickModeId.Puppeteer and not MaverickModeId.TagTeam) {
			target = Global.level.getClosestTarget(
				getCenterPos(), player.alliance, true, isRequesterAI: true,
				aMaxDist: 300
			);
		}

		bool isAIState = state.attackCtrl;
		if (canFly) isAIState = isAIState || state is MFly;

		if (target != null && (isAIState || state is MShoot)) {
			turnToPos(target.getCenterPos());
		}

		bool doStartMoveControlIfNoTarget = startMoveControl >= 0;
		if ((target != null || doStartMoveControlIfNoTarget) &&
			isAIState && controlMode != MaverickModeId.Puppeteer
		) {
			if (isSummonerCocoon) {
				if (target != null) {
					mmc?.changeState(new MorphMCSpinState());
				}
			}
			else if (aiCooldown == 0 && isAIState) {
				MaverickState mState = getRandomAttackState();
				if (isSummonerOrStrikerDoppler && doppler.ballType == 1) {
					mState = strikerStates()[0];
				} else if (startMoveControl >= 0) {
					MaverickState[] aiAttackStateArray = strikerStates();
					while (startMoveControl >= aiAttackStateArray.Length) {
						startMoveControl = 0;
					}
					mState = aiAttackStateArray[startMoveControl];
					startMoveControl = -1;
				}

				if (mState != null) {
					changeState(mState);
				}
			}
		} else if (aiBehavior == MaverickAIBehavior.Follow && controlMode != MaverickModeId.Striker) {
			if (player.character != null && state.normalCtrl) {
				Character chr = player.character;
				float dist = chr.pos.x - pos.x;
				float assignedDist = 40;

				for (int i = 0; i < player.mavericks.Count; i++) {
					if (player.mavericks[i] == this) {
						assignedDist = 40 * (i + 1);
					}
				}
				if (!grounded) {
					assignedDist = 4;
				}
				int walkDir = dist < 0 ? -1 : 1;
				bool doWalk = MathF.Abs(dist) > assignedDist && chr.grounded;

				// For while we are jumping.
				if (!grounded) {
					// Start falling and on top of grounded Sigma.
					if (chr.grounded && MathF.Abs(chr.pos.x - pos.x) <= 4) {
						doWalk = false;
					}
					// If on top of death. NEVER STOP.
					else if (Global.level.getGroundPosNoKillzone(pos, 128) == null) {
						walkDir = xDir;
						doWalk = true;
					}
					// Check if pit on front and falling. If so, stop.
					else if (
						vel.y >= 0 &&
						Global.level.getGroundPosNoKillzone(
							pos.addxy(xDir * 32, 0), 128
						) == null
					) {
						doWalk = false;
					}
				}
				if (doWalk) {
					if (walkDir == -1) {
						press(Control.Left);
					} else {
						press(Control.Right);
					}
					var jumpZones = Global.level.getTerrainTriggerList(
						this, Point.zero, typeof(JumpZone)
					);
					if (jumpZones.Count > 0) {
						press(Control.Jump);
					}
				}
			}
		} else if (aiBehavior == MaverickAIBehavior.Attack && state.normalCtrl) {
			// For air-walk.
			bool doWalk = false;
			// For while we are jumping.
			if (!grounded) {
				// If on top of death. NEVER STOP.
				if (Global.level.getGroundPosNoKillzone(pos, 128) == null) {
					doWalk = true;
				}
				// Check if pit on front and falling. If so, stop.
				else if (
					vel.y >= 0 &&
					Global.level.getGroundPosNoKillzone(
						pos.addxy(xDir * 32, 0), 128
					) == null
				) {
					doWalk = false;
				}
			}
			// Jump on jump zones.
			var jumpZones = Global.level.getTerrainTriggerList(
				this, Point.zero, typeof(JumpZone)
			);
			if (grounded && jumpZones.Count > 0) {
				press(Control.Jump);
			}
			// Air walk. Keep moving in the same direction.
			if (doWalk) {
				if (xDir == -1) {
					press(Control.Left);
				} else {
					press(Control.Right);
				}
			} else {
				float raycastDist = (width / 2) + 5;
				List<CollideData> hit = Global.level.raycastAll(
					getCenterPos(), getCenterPos().addxy(attackDir * raycastDist, 0), [typeof(Wall)]
				);
				if (hit.Count == 0) {
					if (attackDir < 0) {
						press(Control.Left);
					} else {
						press(Control.Right);
					}
				}
			}
		}
	}

	public bool shootPressed() {
		return input.isPressed(Control.Shoot, player) && player.weapon is MaverickWeapon mw && mw.maverick == this;
	}

	public bool specialPressed() {
		return input.isPressed(Control.Special1, player) && player.weapon is MaverickWeapon mw && mw.maverick == this;
	}

	private void press(string inputMapping) {
		string keyboard = "keyboard";
		int? control = Control.controllerNameToMapping[keyboard].GetValueOrDefault(inputMapping);
		if (control == null) return;
		Key key = (Key)control;
		input.keyPressed[key] = !input.keyHeld.ContainsKey(key) || !input.keyHeld[key];
		input.keyHeld[key] = true;
	}

	public virtual string getMaverickPrefix() {
		return "";
	}

	public virtual MaverickState getRandomAttackState() {
		// Get all posible states.
		MaverickState[] targetStates = aiAttackStates();
		// Skip states on cooldown.
		if (targetStates.Length != 0) {
			targetStates = targetStates.Where(
				tState => !(stateCooldowns.GetValueOrDefault(tState.GetType())?.cooldown > 0)
			).ToArray();
		}
		// If the total state count is 0. Then we just taunt.
		if (targetStates.Length == 0) {
			return new MTaunt();
		}
		// Otherwise, return all usable states.
		return targetStates.GetRandomItem();
	}

	public virtual MaverickState[] aiAttackStates() {
		return [new MTaunt()];
	}

	public virtual MaverickState[] strikerStates() {
		return [new MTaunt()];
	}

	// For terrain collision.
	public override Collider getTerrainCollider() {
		if (physicsCollider == null) {
			return null;
		}
		float hSize = Math.Min(height, 30);
		float wSize = width;
		Rect? physicsRect = physicsCollider?.shape.getRect();
		if (physicsRect != null) {
			hSize = Math.Min(hSize, physicsRect.Value.h());
		} else {
			return null;
		}

		if (reversedGravity && this is MagnaCentipede) {
			return new Collider(
				new Rect(0f, 0f, wSize, hSize).getPoints(),
				false, this, false, false,
				HitboxFlag.Hurtbox, new Point(0f, -50 + hSize)
			);
		}
		return new Collider(
			new Rect(0f, 0f, wSize, hSize).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0f, 0f)
		);
	}

	public override Collider getGlobalCollider() {
		var rect = new Rect(0, 0, width, height);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public Collider getDashCollider(float widthPercent = 1f, float heightPercent = 0.75f) {
		var rect = new Rect(0, 0, width * widthPercent, height * heightPercent);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public bool isAttacking() {
		return sprite.name.Contains("attack");
	}

	public override Dictionary<int, Func<Projectile>> getGlobalProjs() {
		var retProjs = new Dictionary<int, Func<Projectile>>();

		if (globalCollider != null && Global.level.is1v1() && player.maverick1v1 != null && (sprite.name.Contains("_jump") || sprite.name.Contains("_fall"))) {
			retProjs[(int)ProjIds.MaverickContactDamage] = () => {
				Point centerPoint = globalCollider.shape.getRect().center();
				float damage = 3;
				int flinch = 0;
				Projectile proj = new GenericMeleeProj(weapon, centerPoint, ProjIds.MaverickContactDamage, player, damage, flinch);
				proj.globalCollider = globalCollider.clone();
				return proj;
			};
		}

		return retProjs;
	}

	public void maxOutGlobalCooldowns(float maxAllowedCooldown) {
		foreach (var stateCooldown in stateCooldowns.Values) {
			if (stateCooldown.isGlobal && stateCooldown.cooldown < maxAllowedCooldown) {
				stateCooldown.cooldown = Math.Min(stateCooldown.maxCooldown, maxAllowedCooldown);
			}
		}
	}

	public void changeState(MaverickState newState, bool ignoreCooldown = false) {
		if (!newState.canEnterSelf && state.GetType() == newState.GetType()) {
			return;
		}
		if (state is MDie) {
			return;
		}
		if (!newState.canEnter(this)) {
			return;
		}
		MaverickStateCooldown? oldStateCooldown = stateCooldowns.GetValueOrDefault(state.GetType());
		MaverickStateCooldown? newStateCooldown = stateCooldowns.GetValueOrDefault(newState.GetType());

		if (newStateCooldown != null && !ignoreCooldown) {
			if (newStateCooldown.cooldown > 0) return;
		}

		changedStateInFrame = true;
		newState.maverick = this;

		if (Global.sprites.ContainsKey($"{getMaverickPrefix()}_{newState.sprite}")) {
			changeSpriteFromName(newState.sprite, true);
		}
		else if (
			newState.sprite == newState.transitionSprite &&
			Global.sprites.ContainsKey($"{getMaverickPrefix()}_{newState.defaultSprite}")
		) {
			newState.sprite = newState.defaultSprite;
			changeSpriteFromName(newState.defaultSprite, true);
		}

		var oldState = state;
		if (oldState != null) {
			oldState.onExit(newState);
			if (oldStateCooldown != null && !oldStateCooldown.startOnEnter) {
				oldStateCooldown.cooldown = oldStateCooldown.maxCooldown;
				if (oldStateCooldown.isGlobal) {
					maxOutGlobalCooldowns(oldStateCooldown.maxCooldown);
				}
			}
		}
		state = newState;
		newState.onEnter(oldState);
		if (newStateCooldown != null && newStateCooldown.startOnEnter) {
			newStateCooldown.cooldown = newStateCooldown.maxCooldown;
			if (newStateCooldown.isGlobal) {
				maxOutGlobalCooldowns(newStateCooldown.maxCooldown);
			}
		}
	}

	public void changeSpriteFromName(string spriteBaseName, bool resetFrame) {
		string spriteName = getMaverickPrefix() + "_" + spriteBaseName;
		if (this is BoomerangKuwanger bk && bk.bald) {
			string newSpriteName = getMaverickPrefix() + "_bald_" + spriteBaseName;
			if (Global.sprites.ContainsKey(newSpriteName)) {
				spriteName = newSpriteName;
			}
		} else if (this is ArmoredArmadillo aa && aa.noArmor) {
			string newSpriteName = getMaverickPrefix() + "_na_" + spriteBaseName;
			if (Global.sprites.ContainsKey(newSpriteName)) {
				spriteName = newSpriteName;
			}
		} else if (this is MagnaCentipede ms && ms.noTail) {
			string newSpriteName = getMaverickPrefix() + "_notail_" + spriteBaseName;
			if (Global.sprites.ContainsKey(newSpriteName)) {
				spriteName = newSpriteName;
			}
		} else if (this is CrystalSnail cs && cs.noShell) {
			string newSpriteName = getMaverickPrefix() + "_noshell_" + spriteBaseName;
			if (Global.sprites.ContainsKey(newSpriteName)) {
				spriteName = newSpriteName;
			}
		}
		changeSprite(spriteName, resetFrame);
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (this is FakeZero fz && fz.state is FakeZeroGuardState) {
			ammo += damage;
			if (ammo > 32) ammo = 32;
			damage *= 0.75f;
		}

		health -= damage;

		if ((damage > 0 || Damager.alwaysAssist(projId)) && owner != null && weaponIndex != null) {
			damageHistory.Add(new DamageEvent(owner, weaponIndex.Value, projId, false, Global.time));
		}

		if (ownedByLocalPlayer && damage > 0 && owner != null) {
			netOwner.delaySubtank();
			addDamageTextHelper(owner, damage, maxHealth, true);
		}

		if (health <= 0 && ownedByLocalPlayer) {
			health = 0;
			if (state is not MDie) {
				changeState(new MDie(damage == Damager.envKillDamage));
				int? assisterProjId = null;
				int? assisterWeaponId = null;
				Player killer = null;
				Player assister = null;
				getKillerAndAssister(player, ref killer, ref assister, ref weaponIndex, ref assisterProjId, ref assisterWeaponId);
				creditMaverickKill(killer, assister, weaponIndex);
			}
		}
	}

	public void creditMaverickKill(Player killer, Player assister, int? weaponIndex) {
		if (killer != null && killer != player) {
			killer.addKill();
			if (Global.level.gameMode is TeamDeathMatch) {
				if (Global.isHost) {
					if (killer.alliance != player.alliance) {
						Global.level.gameMode.teamPoints[killer.alliance]++;
						Global.level.gameMode.syncTeamScores();
					}
					Global.level.gameMode.syncTeamScores();
				}
			}

			killer.awardCurrency();
			awardXWeapon(killer);
		}

		if (assister != null && assister != player) {
			assister.addAssist();
			assister.addKill();
			assister.awardCurrency();
			awardXWeapon(killer);
		}

		int maverickKillFeedIndex = getMaverickKillFeedIndex();
		Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(killer, assister, player, weaponIndex, maverickKillFeedIndex: maverickKillFeedIndex));

		if (ownedByLocalPlayer) {
			RPC.creditPlayerKillMaverick.sendRpc(killer, assister, this, weaponIndex);
		}
	}

	public void awardXWeapon(Player player) {
		if (player.character is not MegamanX) {
			return;
		}
		Weapon? weaponToAdd = awardWeaponId switch {
			WeaponIds.Buster => new XBuster(),
			WeaponIds.ShotgunIce => new ShotgunIce(),
			WeaponIds.ElectricSpark => new ElectricSpark(),
			WeaponIds.RollingShield => new RollingShield(),
			WeaponIds.HomingTorpedo => new HomingTorpedo(),
			WeaponIds.BoomerangCutter => new BoomerangCutter(),
			WeaponIds.ChameleonSting => new ChameleonSting(),
			WeaponIds.StormTornado => new StormTornado(),
			WeaponIds.FireWave => new FireWave(),
			WeaponIds.CrystalHunter => new CrystalHunter(),
			WeaponIds.BubbleSplash => new BubbleSplash(),
			WeaponIds.SilkShot => new SilkShot(),
			WeaponIds.SpinWheel => new SpinWheel(),
			WeaponIds.SonicSlicer => new SonicSlicer(),
			WeaponIds.StrikeChain => new StrikeChain(),
			WeaponIds.MagnetMine => new MagnetMine(),
			WeaponIds.SpeedBurner => new SpeedBurner(),
			WeaponIds.AcidBurst => new AcidBurst(),
			WeaponIds.ParasiticBomb => new ParasiticBomb(),
			WeaponIds.TriadThunder => new TriadThunder(),
			WeaponIds.SpinningBlade => new SpinningBlade(),
			WeaponIds.RaySplasher => new RaySplasher(),
			WeaponIds.GravityWell => new GravityWell(),
			WeaponIds.FrostShield => new FrostShield(),
			WeaponIds.TornadoFang => new TornadoFang(),
			_ => null,
		};
		if (weaponToAdd == null) {
			return;
		}
		Weapon? matchingW = player.weapons.FirstOrDefault(w => w.index == weaponToAdd.index);
		if (matchingW != null) {
			matchingW.ammo = matchingW.maxAmmo;
		}
		else if (player.weapons.Count >= 3 && player.weapons.Count < 9) {
			player.weapons.Insert(3, weaponToAdd);
		}
	}

	public bool checkWeakness(WeaponIds weaponId, ProjIds projId, out MaverickState? newState, bool isAttackerMaverick) {
		newState = null;
		if (player.maverick1v1 != null && isAttackerMaverick) {
			return false;
		}

		if ((weaponId == WeaponIds.FireWave || projId == ProjIds.FlameMFireball || projId == ProjIds.FlameMOilFire) && this is ChillPenguin) {
			newState = new ChillPBurnState();
			return true;
		}
		if ((weaponId == WeaponIds.ShotgunIce || projId == ProjIds.ChillPIceShot || projId == ProjIds.ChillPIceBlow || projId == ProjIds.ChillPIcePenguin) && this is SparkMandrill) {
			newState = new SparkMFrozenState();
			return true;
		}
		if ((weaponId == WeaponIds.ElectricSpark || projId == ProjIds.SparkMSpark) && this is ArmoredArmadillo aa) {
			if (!aa.noArmor) {
				newState = new ArmoredAZappedState();
			}
			return true;
		}
		if ((weaponId == WeaponIds.RollingShield || projId == ProjIds.ArmoredARoll) && this is LaunchOctopus lo) {
			return true;
		}
		if ((weaponId == WeaponIds.HomingTorpedo || projId == ProjIds.LaunchOMissle || projId == ProjIds.LaunchOTorpedo) && this is BoomerangKuwanger bk) {
			return true;
		}
		if ((weaponId == WeaponIds.BoomerangCutter || projId == ProjIds.BoomerangKBoomerang) && this is StingChameleon sc) {
			if (sc.isInvisible && sc.ownedByLocalPlayer) {
				sc.decloak();
			}
			return true;
		}
		if ((weaponId == WeaponIds.ChameleonSting || projId == ProjIds.StingCSting) && this is StormEagle se) {
			return true;
		}
		if ((weaponId == WeaponIds.StormTornado || projId == ProjIds.StormETornado) && this is FlameMammoth fm) {
			return true;
		}
		if ((weaponId == WeaponIds.ShotgunIce || projId == ProjIds.ChillPIceShot || projId == ProjIds.ChillPIceBlow || projId == ProjIds.ChillPIcePenguin) && this is Velguarder vg) {
			return true;
		}

		// X2
		if ((weaponId == WeaponIds.SonicSlicer || projId == ProjIds.OverdriveOSonicSlicer || projId == ProjIds.OverdriveOSonicSlicerUp) && this is WireSponge) {
			return true;
		}
		if ((weaponId == WeaponIds.StrikeChain || projId == ProjIds.WSpongeChain || projId == ProjIds.WSpongeUpChain) && this is WheelGator) {
			return true;
		}
		if ((weaponId == WeaponIds.SpinWheel || projId == ProjIds.WheelGSpinWheel) && this is BubbleCrab) {
			return true;
		}
		if ((weaponId == WeaponIds.BubbleSplash || projId == ProjIds.BCrabBubbleSplash) && this is FlameStag) {
			return true;
		}
		if ((weaponId == WeaponIds.SpeedBurner || projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash || projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail) && this is MorphMothCocoon mmc) {
			if (ownedByLocalPlayer) {
				newState = mmc.burn();
			}
			return true;
		}
		if ((weaponId == WeaponIds.SpeedBurner || projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash || projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail) && this is MorphMoth) {
			return true;
		}
		if ((weaponId == WeaponIds.SilkShot || projId == ProjIds.MorphMCScrap || projId == ProjIds.MorphMBeam) && this is MagnaCentipede ms) {
			if (ownedByLocalPlayer && !ms.noTail) {
				ms.removeTail();
			}
			return true;
		}
		if ((weaponId == WeaponIds.MagnetMine || projId == ProjIds.MagnaCMagnetMine) && this is CrystalSnail cs) {
			if (ownedByLocalPlayer && !cs.noShell) {
				newState = new CSnailWeaknessState(true);
			}
			return true;
		}
		if ((weaponId == WeaponIds.CrystalHunter || projId == ProjIds.CSnailCrystalHunter) && this is OverdriveOstrich oo) {
			newState = new OverdriveOCrystalizedState();
			return true;
		}
		if ((weaponId == WeaponIds.SpeedBurner || projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash || projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail) && this is FakeZero) {
			return true;
		}

		// x3
		if ((weaponId == WeaponIds.ParasiticBomb || projId == ProjIds.BHornetBee || projId == ProjIds.BHornetHomingBee) && this is BlizzardBuffalo) {
			return true;
		}
		if ((weaponId == WeaponIds.FrostShield || projId == ProjIds.BBuffaloIceProj || projId == ProjIds.BBuffaloIceProjGround) && this is ToxicSeahorse) {
			return true;
		}
		if ((weaponId == WeaponIds.AcidBurst || projId == ProjIds.TSeahorseAcid1 || projId == ProjIds.TSeahorseAcid2) && this is TunnelRhino) {
			return true;
		}
		if ((weaponId == WeaponIds.TornadoFang || projId == ProjIds.TunnelRTornadoFang || projId == ProjIds.TunnelRTornadoFang2 || projId == ProjIds.TunnelRTornadoFangDiag) && this is VoltCatfish) {
			return true;
		}
		if ((weaponId == WeaponIds.TriadThunder || projId == ProjIds.VoltCBall || projId == ProjIds.VoltCTriadThunder || projId == ProjIds.VoltCUpBeam || projId == ProjIds.VoltCUpBeam2) && this is CrushCrawfish) {
			return true;
		}
		if ((weaponId == WeaponIds.SpinningBlade || projId == ProjIds.CrushCArmProj) && this is NeonTiger) {
			return true;
		}
		if ((weaponId == WeaponIds.RaySplasher || projId == ProjIds.NeonTRaySplasher) && this is GravityBeetle) {
			return true;
		}
		if ((weaponId == WeaponIds.GravityWell || projId == ProjIds.GBeetleGravityWell || projId == ProjIds.GBeetleBall) && this is BlastHornet) {
			return true;
		}
		if ((weaponId == WeaponIds.AcidBurst || projId == ProjIds.TSeahorseAcid1 || projId == ProjIds.TSeahorseAcid2) && this is DrDoppler) {
			return true;
		}

		return false;
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		if (Global.level.isRace()) return false;

		if (this is BoomerangKuwanger bk && bk.sprite.name.Contains("teleport")) {
			return false;
		}
		if (this is StingChameleon sc && sc.isInvisible && !Damager.isBoomerang(projId)) {
			return false;
		}
		if (this is MorphMothCocoon mmc && mmc.selfDestructTime > 0) {
			return false;
		}
		if (sprite.name.EndsWith("enter") || sprite.name.EndsWith("exit")) {
			return false;
		}
		if (this is MagnaCentipede ms && ms.sprite.name.Contains("teleport")) {
			return false;
		}
		if (this is ToxicSeahorse ts && ts.sprite.name.Contains("teleport")) {
			return false;
		}
		if (sprite.name == "drdoppler_uncoat") {
			return false;
		}
		if (invulnTime > 0) {
			return false;
		}

		return damagerAlliance != player.alliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return sprite.name == "armoreda_charge" || sprite.name.Contains("_shell") || sprite.name.EndsWith("eat_loop");
	}

	public bool canBeHealed(int healerAlliance) {
		return healerAlliance == player.alliance && health > 0 && health < maxHealth;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		if (!allowStacking && this.healAmount > 0) return;
		if (health < maxHealth) {
			playHealSound = true;
		}
		commonHealLogic(healer, healAmount, health, maxHealth, drawHealText);
		this.healAmount = healAmount;
	}

	public bool isPlayableDamagable() {
		return true;
	}

	public virtual float getAirSpeed() {
		return 1;
	}

	public virtual float getDashSpeed() {
		return dashSpeed;
	}

	public virtual float getRunSpeed() {
		return 100;
	}

	public virtual float getJumpPower() {
		return Physics.JumpSpeed;
	}

	public bool canDash() {
		return !isAttacking();
	}

	public float getLabelOffY() {
		if (this is MorphMothCocoon && state is MorphMCHangState) {
			return height * 0.5f;
		}
		if (this is BlastHornet) {
			return 60;
		}
		return height;
	}

	public void drawHealthBar() {
		float healthBarInnerWidth = 30;
		Color color = new Color();

		float healthPct = health / maxHealth;
		float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
		if (healthPct > 0.66) color = Color.Green;
		else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
		else if (healthPct < 0.33) color = Color.Red;

		float botY = pos.y + currentLabelY;
		DrawWrappers.DrawRect(pos.x - 16, botY - 5, pos.x + 16, botY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
		DrawWrappers.DrawRect(pos.x - 15, botY - 4, pos.x - 15 + width, botY - 1, true, color, 0, ZIndex.HUD - 1);

		deductLabelY(labelHealthOffY);
	}

	public void drawName(string overrideName = "", FontType? overrideColor = null) {
		string playerName = player.name;
		FontType playerColor = FontType.Grey;
		if (Global.level.gameMode.isTeamMode && player.alliance < Global.level.teamNum) {
			playerColor = Global.level.gameMode.teamFonts[player.alliance];
		}

		if (!string.IsNullOrEmpty(overrideName)) playerName = overrideName;
		if (overrideColor != null) playerColor = overrideColor.Value;

		float textPosX = pos.x;
		float textPosY = pos.y + currentLabelY - 8;

		Fonts.drawText(
			playerColor, playerName, textPosX, textPosY,
			Alignment.Center, true, depth: ZIndex.HUD
		);

		deductLabelY(labelNameOffY);
	}

	public bool drawSubtankHealing() {
		if (ownedByLocalPlayer) {
			if (usedSubtank != null) {
				drawSubtankHealingInner(usedSubtank.health);
				return true;
			}
		} else {
			if (netSubtankHealAmount > 0) {
				drawSubtankHealingInner(netSubtankHealAmount);
				netSubtankHealAmount -= Global.spf * 20;
				if (netSubtankHealAmount <= 0) netSubtankHealAmount = 0;
				return true;
			}
		}

		return false;
	}

	public void drawSubtankHealingInner(float health) {
		Point topLeft = new Point(pos.x - 8, pos.y - 15 + currentLabelY);
		Point topLeftBar = new Point(pos.x - 2, topLeft.y + 1);
		Point botRightBar = new Point(pos.x + 2, topLeft.y + 15);

		subtankSprite.draw(1, topLeft.x, topLeft.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
		subtankBarSprite.draw(0, topLeftBar.x, topLeftBar.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
		float yPos = 14 * (health / SubTank.maxHealth);
		DrawWrappers.DrawRect(topLeftBar.x, topLeftBar.y, botRightBar.x, botRightBar.y - yPos, true, Color.Black, 1, ZIndex.HUD);

		deductLabelY(labelSubtankOffY);
	}

	public override bool shouldDraw() {
		if (invulnTime > 0) {
			if (Global.level.frameCount % 4 < 2) return false;
		}
		return base.shouldDraw();
	}

	public override void render(float x, float y) {
		base.render(x, y);
		currentLabelY = -getLabelOffY();

		if (ownerChar?.currentMaverick != this &&
			!sprite.name.EndsWith("exit") &&
			!sprite.name.EndsWith("enter") &&
			controlMode != MaverickModeId.Striker &&
			controlMode != MaverickModeId.TagTeam
		) {
			if (player == Global.level.mainPlayer && ownerChar?.currentMaverick != this && health > 0) {
				drawHealthBar();
			}

			if (player != Global.level.mainPlayer && player.alliance == Global.level.mainPlayer.alliance) {
				drawHealthBar();
				drawName();
			}
		}

		drawSubtankHealing();

		renderDamageText(35);

		if (showCursor()) {
			cursorSprite.draw(0, pos.x + x, pos.y + y + currentLabelY, 1, 1, null, 1, 1, 1, zIndex + 1);
			deductLabelY(labelCursorOffY);
		}
	}

	public bool showCursor() {
		if (this is StingChameleon sc && sc.isInvisible && ownedByLocalPlayer) {
			return true;
		}
		return false;
	}

	public void changeToIdleOrFall(string transitionSprite = "") {
		if (grounded) {
			if (state is not MIdle) changeState(new MIdle(transitionSprite));
		} else {
			if (state is not MFall) changeState(new MFall(transitionSprite));
		}
	}

	public void changeToIdleRunOrFall() {
		if (grounded) {
			if (input.getInputDir(player).x != 0) {
				changeState(new MRun());
			}
			if (state is not MIdle) changeState(new MIdle());
		} else {
			if (state is not MFall) changeState(new MFall());
		}
	}

	public void changeToIdleFallOrFly(string transitionSprite = "") {
		if (grounded) {
			if (state is not MIdle) changeState(new MIdle(transitionSprite));
		} else {
			if (state?.wasFlying == true) changeState(new MFly(transitionSprite));
			else if (state is not MFall) changeState(new MFall(transitionSprite));
		}
	}

	public override List<ShaderWrapper>? getShaders() {
		if (timeStopTime > timeStopThreshold) {
			if (!Global.level.darkHoldProjs.Any(
				dhp => dhp.screenShader != null && dhp.inRange(this))
			) {
				return [Player.darkHoldShader];
			}
		}
		return null;
	}

	public const int CustomNetDataLength = 3;

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		customData.Add((byte)MathF.Ceiling(alpha * 100));
		customData.Add((byte)MathF.Ceiling(health));
		customData.Add((byte)MathF.Ceiling(invulnTime * 20));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		alpha = data[0] / 100f;
		health = data[1];
		invulnTime = data[2] / 20f;
	}
}
