using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class RideArmor : Actor, IDamagable {
	public float health = 32;
	public float maxHealth = 32;
	public float goliathHealth = 0;
	public float healAmount = 0;
	public float healTime = 0;
	public bool playHealSound;

	public RideArmorState? rideArmorState;
	public Character? mk5Rider;
	public Character? character;
	public Character? grabbedCharacter;
	public bool changedStateInFrame;
	public bool isDashing;
	//WARNING: this gets the player of the character riding the armor. For the owner, use netOwner
	public Player? player { get { return character?.player; } }
	public bool isExploding;
	public float enterCooldown;
	public float goliathTime;
	public float selfDestructTime;
	public const float maxSelfDestructTime = 10;
	public int raNum;
	public bool isSwimming;
	public bool isNeutral { get { return neutralId != 0; } }
	public int neutralId;
	public bool claimed;
	public ShaderWrapper? colorShader;
	public bool ownedByMK5;
	public int hawkBombCount;
	public int maxHawkBombCount;

	public float consecutiveJumpTimeout;
	public int consecutiveJump;
	public bool manualDisabled = false;

	public static ShaderWrapper? paletteEG01 = Helpers.cloneGenericPaletteShader("paletteEG01");
	public static ShaderWrapper? paletteKangaroo = Helpers.cloneGenericPaletteShader("paletteKangaroo");
	public static ShaderWrapper? paletteHawk = Helpers.cloneGenericPaletteShader("paletteHawk");
	public static ShaderWrapper? paletteFrog = Helpers.cloneGenericPaletteShader("paletteFrog");

	bool netColorShadersSet;

	public RideArmor(
		Player owner, Point pos, int raNum, int neutralId, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base("", pos, netId, ownedByLocalPlayer, true
	) {
		netOwner = owner;
		setRaNum(raNum);
		health = maxHealth;

		this.neutralId = neutralId;
		changeState(new RADeactive(transitionSprite: ""));

		spriteFrameToSounds["ridearmor_run/2"] = "ridewalk";
		spriteFrameToSounds["ridearmor_run/6"] = "ridewalk2";
		spriteFrameToSounds["goliath_run/2"] = "ridewalkX3";
		spriteFrameToSounds["goliath_run/6"] = "ridewalk2X3";
		spriteFrameToSounds["hawk_run/4"] = "ridewalkX3";
		spriteFrameToSounds["hawk_run/9"] = "ridewalk2X3";
		spriteFrameToSounds["kangaroo_run/4"] = "ridewalkX3";
		spriteFrameToSounds["kangaroo_run/9"] = "ridewalk2X3";
		spriteFrameToSounds["neutralra_run/2"] = "ridewalk";
		spriteFrameToSounds["neutralra_run/6"] = "ridewalk2";
		spriteFrameToSounds["devilbear_run/2"] = "ridewalk";
		spriteFrameToSounds["devilbear_run/7"] = "ridewalk2";

		if (ownedByLocalPlayer) {
			setColorShaders();
		}

		useFrameProjs = true;
		splashable = true;
		Global.level.addGameObject(this);

		spriteToCollider["deact*"] = getLowCollider();
		spriteToCollider["warp_beam"] = null;

		maxHawkBombCount = 3;

		if (ownedByLocalPlayer && raNum == 2 && owner.character is Vile vile) {
			if (vile.napalmWeapon.type == 1) {
				maxHawkBombCount = 2;
			} else if (vile.napalmWeapon.type == 2) {
				maxHawkBombCount = 2;
			} 
		}
		hawkBombCount = maxHawkBombCount;

		netActorCreateId = NetActorCreateId.RideArmor;
		if (sendRpc) {
			createActorRpc(owner.id);
		}
	}

	bool colorShadersSet;
	public void setColorShaders() {
		if (!colorShadersSet) colorShadersSet = true;
		else return;

		// If adding a palette here it must be added to RPCActorToggleType.ChangeRAPieceColor check to sync
		if (raNum == 0 && isNeutral) {
			if (neutralId % 2 == 1) {
				colorShader = paletteEG01;
				colorShader?.SetUniform("palette", 1);
			}
		} else if (raNum == 1 && isNeutral) {
			colorShader = paletteKangaroo;
			colorShader?.SetUniform("palette", 1);
		} else if (raNum == 2 && !isNeutral) {
			colorShader = paletteHawk;
			colorShader?.SetUniform("palette", 1);
		} else if (raNum == 3 && !isNeutral) {
			colorShader = paletteFrog;
			colorShader?.SetUniform("palette", 1);
		}
	}

	public void setMaxHealth() {
		if (raNum == 2) maxHealth = 24; // + Helpers.clampMax(netOwner.heartTanks * netOwner.getHeartTankModifier(), 8);
		else if (raNum == 3) maxHealth = 24; // + Helpers.clampMax(netOwner.heartTanks * netOwner.getHeartTankModifier(), 8);
		else maxHealth = 32;
		if (raNum == 4) goliathHealth = 32;
		maxHealth = Player.getModifiedHealth(maxHealth);
	}

	public void setRaNum(int raNum) {
		this.raNum = raNum;
		setMaxHealth();
	}

	float dashOffsetTime;
	int dashOffset;
	public override void render(float x, float y) {
		base.render(x, y);

		renderDamageText(35);

		if (player != null && player.isMainPlayer) {
			if (flyTime > 0) {
				float healthPct = Helpers.clamp01((6 - flyTime) / 6);
				float sy = -40;
				float sx = 0;
				if (xDir == -1) sx = 90;
				if (raNum == 3) {
					sx = 10;
					if (xDir == -1) sx = 80;
				}
				drawFuelMeter(healthPct, sx, sy);
			}
		}

		bool isVileMk5 = ownedByMK5 && character != null && character is Vile vile && vile.isVileMK5;

		// Health bar
		if (ownedByLocalPlayer && isVileMk5 && !isInvincible(null, null)) {
			float healthBarInnerWidth = 30;
			Color color = new Color();

			float healthPct = health / maxHealth;
			float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
			if (healthPct > 0.66) color = Color.Green;
			else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
			else if (healthPct < 0.33) color = Color.Red;

			float botY = pos.y - 60;
			DrawWrappers.DrawRect(pos.x - 16, botY - 5, pos.x + 16, botY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
			DrawWrappers.DrawRect(pos.x - 15, botY - 4, pos.x - 15 + width, botY - 1, true, color, 0, ZIndex.HUD - 1);
		}

		// Tether
		if (isVileMk5 && character != null) {
			Point charPos = character.getCenterPos().addxy(0, -10);
			Point raPos = getCenterPos().addxy(0, -10);
			Point dirTo = charPos.directionTo(raPos);
			float dist = charPos.distanceTo(raPos);

			float dashLen = 4;
			dashOffsetTime += Global.spf;
			if (dashOffsetTime > 0) {
				dashOffsetTime = 0;
				dashOffset++;
				if (dashOffset >= dashLen * 2) {
					dashOffset = 0;
				}
			}

			for (int i = 0; i < MathF.Floor(dist / dashLen); i++) {
				var dash = dirTo.normalize().times(dashLen);
				var offset = dirTo.normalize().times(dashOffset);

				if (i % 2 == 0) {
					//DrawWrappers.DrawLine(charPos.x + offset.x, charPos.y + offset.y, charPos.x + dash.x + offset.x, charPos.y + dash.y + offset.y, new Color(255, 0, 0, 128), 2, ZIndex.HUD);
					//DrawWrappers.DrawLine(charPos.x + offset.x, charPos.y + offset.y, charPos.x + dash.x + offset.x, charPos.y + dash.y + offset.y, Color.Red, 1, ZIndex.HUD);
					DrawWrappers.DrawLine(charPos.x + offset.x, charPos.y + offset.y, charPos.x + dash.x + offset.x, charPos.y + dash.y + offset.y, new Color(255, 0, 0, 128), 1, ZIndex.HUD);
				}

				charPos.x += dash.x;
				charPos.y += dash.y;
			}
		}
	}

	public override void preUpdate() {
		base.preUpdate();
		changedStateInFrame = false;
	}

	public override void update() {
		base.update();

		if (raNum == 5) {
			if (!ownedByLocalPlayer) {
				zIndex = ZIndex.Character - 100;
			} else if (!isNeutral) {
				if (netOwner?.character != null) zIndex = netOwner.character.zIndex - 1;
				else zIndex = ZIndex.Character - 100;
			}
		}

		if (raNum == 4 && character != null && musicSource == null) {
			addMusicSource("vile_X3", getCenterPos(), true);
		}
		if (character == null && musicSource != null) {
			destroyMusicSource();
		}

		if (ownedByLocalPlayer && ownedByMK5) {
			var mk5Pos = getMK5Pos();
			var hitsAbove = Global.level.getTriggerList(this, 0, -2);
			foreach (var hit in hitsAbove) {
				if (ownedByMK5 &&
					enterCooldown == 0 &&
					hit.gameObject is Character chr &&
					chr.player == netOwner &&
					chr.pos.y <= mk5Pos.y + 10 &&
					chr.canLandOnRideArmor()
				) {
					mk5Rider = chr;
					chr.rideArmorPlatform = this;
					if (rideArmorState is RADeactive) {
						changeState(new RAIdle("ridearmor_activating"), true);
					}
					break;
				}
			}
			if (mk5Rider != null) {
				mk5Rider.changePos(mk5Pos.addxy(0, 1));
			}
			if (mk5Rider?.rideArmorPlatform == null) {
				mk5Rider = null;
			} else {
				character = mk5Rider;
			}
		}
	}

	public Point getMK5Pos() {
		return getFirstPOIOrDefault().addxy(0, 0);
	}

	public bool isVileNArmor() {
		return raNum == 0 && !isNeutral;
	}

	//float soundTime;
	float punchCooldown;
	float missileCooldown;
	public float chainSoundTime;
	public float chainTotalSoundTime;
	public Anim? hawkElec;

	public override void postUpdate() {
		Player? player = this.player ?? netOwner;
		MegamanX? mmx = character as MegamanX;

		base.postUpdate();

		updateProjectileCooldown();

		if (grounded && flyTime > 0) {
			flyTime -= Global.spf * 6;
			if (flyTime < 0) flyTime = 0;
		}

		if (!isUnderwater()) {
			isSwimming = false;
		}

		if (selfDestructTime > 0) {
			int flashFrequency = 30;
			if (selfDestructTime < 3) flashFrequency = 60;
			else if (selfDestructTime >= 3 && selfDestructTime < 6) flashFrequency = 30;
			else if (selfDestructTime >= 6 && selfDestructTime < 8) flashFrequency = 15;
			else if (selfDestructTime >= 8) flashFrequency = 5;

			if (Global.frameCount % flashFrequency == 0) {
				addRenderEffect(RenderEffectType.Hit);
			} else {
				removeRenderEffect(RenderEffectType.Hit);
			}

			selfDestructTime += Global.spf;
			if (selfDestructTime >= maxSelfDestructTime) {
				explode();
				return;
			}
		}

		if (raNum == 2 && isUnderwater() && character != null && character.isUnderwater() && selfDestructTime == 0) {
			if (hawkElec == null) {
				hawkElec = new Anim(pos.addxy(0, -20), "hawk_elec", 1, null, false);
			}
			selfDestructTime = Global.spf;
		}

		if (hawkElec != null) {
			hawkElec.changePos(pos.addxy(0, -30));
		}

		// Cutoff point
		if (!ownedByLocalPlayer) {
			return;
		}

		/*
		if (!(rideArmorState is RADropIn) && character != null)
		{
			var cds = Global.level.checkCollisionsActor(this, 0, -1);
			if (cds.Any(c => c.gameObject is Wall))
			{
				globalCollider = getLowCollider();
			}
		}
		*/

		if (pos.y > Global.level.killY) {
			incPos(new Point(0, 50));
			applyDamage(Damager.envKillDamage, null, null, null, null);
		}
		if (rideArmorState != null) {
			if (grabbedCharacter != null && !string.IsNullOrEmpty(rideArmorState.carrySprite) && !isAttacking()) {
				changeSprite(rideArmorState.carrySprite, true);
			} else if (rideArmorState.inTransition() && isAnimOver() && !Global.level.gameMode.isOver) {
				rideArmorState.sprite = rideArmorState.defaultSprite;
				changeSprite(rideArmorState.sprite, true);
			}
			
			if (grabbedCharacter != null && (!Global.level.gameObjects.Contains(grabbedCharacter) || !(grabbedCharacter.charState is VileMK2Grabbed))) {
				grabbedCharacter = null;
				rideArmorState.sprite = rideArmorState.defaultSprite;
				changeSprite(rideArmorState.sprite, true);
			}
		}
		if (punchCooldown > 0) {
			punchCooldown -= Global.spf;
			if (mmx?.isHyperX == true) punchCooldown -= Global.spf;
			if (punchCooldown < 0) punchCooldown = 0;
		}

		if (enterCooldown >= 0) {
			enterCooldown -= Global.spf;
			if (enterCooldown < 0) enterCooldown = 0;
		}

		if (isExploding) return;

		if (ownedByMK5 && (netOwner?.character == null || netOwner.character.charState is Die)) {
			changeState(new RADeactive(), true);
		}

		if (character != null) {
			if (health >= maxHealth) {
				healAmount = 0;
			}
			if (healAmount > 0 && health > 0) {
				healTime += Global.spf;
				if (healTime > 0.05) {
					healTime = 0;
					healAmount--;
					health = Helpers.clampMax(health + 1, maxHealth);
					if (player == Global.level.mainPlayer || playHealSound) {
						if(raNum == 0 || raNum == 5) {
						playSound("heal", forcePlay: true, sendRpc: true);
						}
						else {
						playSound("healX3", forcePlay: true, sendRpc: true);
						}
					}
				}
			}
		}

		Helpers.decrementTime(ref missileCooldown);
		if (mmx?.isHyperX == true) Helpers.decrementTime(ref missileCooldown);

		if (consecutiveJumpTimeout > 0) {
			consecutiveJumpTimeout -= Global.spf;
			if (consecutiveJumpTimeout <= 0) {
				consecutiveJumpTimeout = 0;
				consecutiveJump = 0;
			}
		}

		if (sprite.name.Contains("attack") && (raNum == 2 || raNum == 3) && sprite.frameIndex == 1 && missileCooldown == 0) {
			bool isBombDrop = sprite.name.EndsWith("down2");
			if (raNum == 2) {
				missileCooldown = 0.75f;
				for (int i = 0; i < currentFrame.POIs.Length; i++) {
					Point poi = currentFrame.POIs[i];
					string poiTag = currentFrame.POITags[i];
					if (poiTag == "b" && player != null) {
						Point shootPos = pos.addxy(poi.x * xDir, poi.y);
						if (!isBombDrop) {
							new MechMissileProj(new MechMissileWeapon(player), shootPos, xDir, sprite.name.Contains("down"), player, player.getNextActorNetId(), rpc: true);
							new Anim(shootPos, "dust", 1, player.getNextActorNetId(), true, true, true) { vel = new Point(0, -100) };
						} else {
							if (character is Vile vile) {
								int xDirMod = -1;
								if (i == 1) xDirMod = 1;
								Projectile grenade;
								if (vile.napalmWeapon.type == (int)NapalmType.SplashHit) {
									grenade = new SplashHitGrenadeProj(vile.napalmWeapon, shootPos, xDir * xDirMod, player, player.getNextActorNetId(), rpc: true);
								} else if (vile.napalmWeapon.type == (int)NapalmType.FireGrenade) {
									grenade = new MK2NapalmGrenadeProj(vile.napalmWeapon, shootPos, xDir * xDirMod, player, player.getNextActorNetId(), rpc: true);
								} else {
									grenade = new NapalmGrenadeProj(new Napalm(NapalmType.RumblingBang), shootPos, xDir * xDirMod, player, player.getNextActorNetId(), rpc: true);
								}
								grenade.vel = new Point();
							}
						}
					}
				}

				if (!isBombDrop) {
					playSound("hawkShootX3", forcePlay: false, sendRpc: true);
				}
			} else if (raNum == 3 && rideArmorState is not RAGroundPound && rideArmorState is not RAGroundPoundStart) {
				missileCooldown = 1f;
				for (int i = 0; i < currentFrame.POIs.Length; i++) {
					Point poi = currentFrame.POIs[i];
					string poiTag = currentFrame.POITags[i];
					if (poiTag == "b" && player != null) {
						Point shootPos = pos.addxy(poi.x * xDir, poi.y);
						new TorpedoProj(new MechTorpedoWeapon(player), shootPos, xDir, player, 2, player.getNextActorNetId(), rpc: true);
					}
				}
				playSound("frogShootX3", forcePlay: false, sendRpc: true);
			}
		}
			bool attackConditionMet = character != null && canAttack() && character.player.input.isPressed(Control.Shoot, character.player) && punchCooldown == 0;
			if (raNum == 5) attackConditionMet = canAttack() && character != null && character.player.input.isHeld(Control.Shoot, character.player);
			if (attackConditionMet) {
				if (rideArmorState is RARun) changeState(new RAIdle(), true);
				if (rideArmorState is RAJump) changeState(new RAFall(), true);
				if (raNum == 1) punchCooldown = 0.8f;
				else punchCooldown = 0.56f;
				string? attackSprite = rideArmorState?.attackSprite;
				if (raNum == 2 && character != null && rideArmorState != null && character.player.input.isHeld(Control.Down, character.player) && !rideArmorState.inTransition()) {
					attackSprite = "hawk_attack_air_down";
				}
				changeSprite(attackSprite, false);
			} else if ( rideArmorState != null && player != null &&
				hawkBombCount > 0 &&
				canAttack() &&
				character is Vile vile &&
				punchCooldown == 0 &&
				raNum == 2 &&
				vile.napalmWeapon.shootTime == 0 &&
				player.input.isPressed(Control.Special1, player) &&
				player.input.isHeld(Control.Down, player) &&
				!rideArmorState.inTransition()
			) {
				hawkBombCount--;
				var targetCooldownWeapon = vile.napalmWeapon;
				if (targetCooldownWeapon.type == (int)NapalmType.NoneFlamethrower || targetCooldownWeapon.type == (int)NapalmType.NoneBall) {
					targetCooldownWeapon = new Napalm(NapalmType.RumblingBang);
				}
				vile.setVileShootTime(vile.napalmWeapon, 2, targetCooldownWeapon);
				punchCooldown = 0.56f;
				changeSprite("hawk_attack_air_down2", false);
			}

			if (isAttacking()) {
				if (isAnimOver()) {
					changeSprite(rideArmorState?.defaultSprite, true);
				}
			}

			if (character != null &&
				!ownedByMK5 &&
				(character.destroyed || character.charState is not InRideArmor)
			) {
				enterCooldown = 0.5f;
				character.rideArmor = null;
				removeCharacter();
				changeState(new RADeactive(), true);
			} else if (character != null &&
				ownedByMK5 &&
				(character.destroyed || character is Vile vile && !vile.canLinkMK5()
			)) {
				enterCooldown = 0.5f;
				unlinkMK5();
			} else {
				rideArmorState?.update();
			}

			if (character != null && !ownedByMK5) {
				var charPos = character.getCharRideArmorPos();
				character.xDir = xDir;
				character.changePos(pos.add(charPos));
			}
		
	}

	public void addCharacter(Character character) {
		this.character = character;
	}

	public void removeCharacter() {
		character = null;
		claimed = false;
	}

	public void addHealth(float amount) {
		healAmount += amount;
	}

	private bool canGrab() {
		return false;
		//return Global.serverClient == null && rideArmorState is RAIdle && !rideArmorState.inTransition() && character != null && !character.isInvulnerable();
	}

	public bool canAttack() {
		if (character == null) return false;
		bool ignoreRideArmorHide = true;
		if (raNum == 2 || raNum == 3) ignoreRideArmorHide = false;
		if (missileCooldown > 0) return false;
		return !string.IsNullOrEmpty(rideArmorState?.attackSprite) && !character.isInvulnerable(ignoreRideArmorHide, true) && !sprite.name.Contains("attack");
	}

	public bool isSpawning() {
		if (character != null && character.isSpawning()) return true;
		if (rideArmorState is RADropIn) return true;
		return false;
	}

	public void changeState(RideArmorState? newState, bool forceChange = false) {
		if (rideArmorState != null && newState != null && rideArmorState.GetType() == newState.GetType()) return;
		if (changedStateInFrame && !forceChange) return;
		if (newState != null) {
			changedStateInFrame = true;
			newState.rideArmor = this;

			changeSprite(newState.sprite, true);

			var oldState = rideArmorState;
			if (oldState != null) oldState.onExit(newState);
			rideArmorState = newState;
			newState.onEnter(oldState);
		}
	}

	//canEnterRideArmor
	public bool canBeEntered() {
		return !ownedByMK5 &&
			rideArmorState is RADeactive &&
			!rideArmorState.inTransition() &&
			character == null;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (isExploding) return;
		if (enterCooldown > 0) return;

		// Move zone movement.
		if (other.gameObject is MoveZone moveZone) {
			if (moveZone.moveVel.x != 0) {
				xPushVel = moveZone.moveVel.x;
			}
			if (moveZone.moveVel.y != 0) {
				yPushVel = moveZone.moveVel.y;
			}
		}

		if (canBeEntered()) {
			var chr = other.otherCollider.actor as Character;
			if (chr != null && chr.canEnterRideArmor() && chr.charState is Fall fallState && MathF.Abs(chr.pos.x - pos.x) < 10) {
				if (Global.serverClient != null) {
					if (isNeutral) {
						if (claimed) {
							return;
						} else if (!ownedByLocalPlayer && chr.ownedByLocalPlayer) {
							fallState.setLimboVehicleCheck(this);
							return;
						} else if (!(ownedByLocalPlayer && chr.ownedByLocalPlayer)) {
							return;
						}
					} else if (chr?.startRideArmor != this || selfDestructTime > 0) {
						return;
					}
				} else {
					// Non-neutral ride armors: don't allow other characters to take
					if (!isNeutral && chr.player != netOwner) {
						return;
					}
				}

				putCharInRideArmor(chr);
			}
		}
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		if (hitbox == null || player == null || sprite?.name == null) {
			return null;
		}
		// whats the center point value?
		Projectile? proj = null;

		if (sprite.name.Contains("attack")) {
			switch (raNum) {
				case 0:
					proj = new GenericMeleeProj(new MechPunchWeapon(player),
					 centerPoint, ProjIds.MechPunch, player);
					break;
				case 1:
					proj = new GenericMeleeProj(new MechKangarooPunchWeapon(player),
					 centerPoint, ProjIds.MechKangarooPunch, player);
					break;
				case 4:
					proj = new GenericMeleeProj(new MechGoliathPunchWeapon(player),
					 centerPoint, ProjIds.MechGoliathPunch, player);
					break;
				case 5:
					proj = new GenericMeleeProj(new MechDevilBearPunchWeapon(player),
					 centerPoint, ProjIds.MechDevilBearPunch, player);
					break;
			}
		}
		else if (sprite.name.Contains("charge")) {
			proj = new GenericMeleeProj(new MechChainChargeWeapon(player), centerPoint, ProjIds.MechChain, player);
		}
		else if (hitbox.name == "stomp" && deltaPos.y > 150 * Global.spf && character != null) {
			bool canDamage = deltaPos.y > 150 * Global.spf;
			float? overrideDamage = sprite.name.EndsWith("groundpound") ? 4 : null;
			if (!canDamage) overrideDamage = 0;
			ProjIds overrideProjId = sprite.name.EndsWith("groundpound") ? ProjIds.MechFrogGroundPound : ProjIds.MechStomp;
			switch (raNum) {
				case 0:
					proj = new GenericMeleeProj(new MechStompWeapon(player),
					 centerPoint, ProjIds.MechStomp, player, damage: !canDamage ? 0 : null);
					break;
				case 1:
					proj = new GenericMeleeProj(new MechKangarooStompWeapon(player),
					 centerPoint, ProjIds.MechStomp, player, damage: !canDamage ? 0 : null);
					break;
				case 2:
					proj = new GenericMeleeProj(new MechHawkStompWeapon(player),
					 centerPoint, ProjIds.MechStomp, player, damage: !canDamage ? 0 : null);
					break;
				case 3:
					proj = new GenericMeleeProj(new MechFrogStompWeapon(player),
					 centerPoint, overrideProjId, player, damage: overrideDamage);
					break;
				case 4:
					proj = new GenericMeleeProj(new MechGoliathStompWeapon(player),
					 centerPoint, ProjIds.MechStomp, player, damage: !canDamage ? 0 : null);
					break;
				case 5:
					proj = new GenericMeleeProj(new MechDevilBearStompWeapon(player),
					 centerPoint, ProjIds.MechStomp, player, damage: !canDamage ? 0 : null);
					break;
			}
		}

		return proj;
	}

	bool healedOnEnter;
	public void putCharInRideArmor(Character chr) {
		addCharacter(chr);
		chr.rideArmor = this;
		chr.changeState(new InRideArmor(), true);
		changeState(new RAIdle("ridearmor_activating"), true);
		if (character != null) {
			if (!healedOnEnter && raNum == 4 && character.ownedByLocalPlayer && character.startRideArmor == this) {
				healedOnEnter = true;
				character.fillHealthToMax();
			}
		}
	}

	public void linkMK5(Character character) {
		addCharacter(character);
		changeState(new RAIdle("ridearmor_activating"), true);
	}

	public void unlinkMK5() {
		removeCharacter();
		if (rideArmorState is not RACalldown) {
			changeState(new RADeactive(), true);
		}
	}

	public void addDamageText(string text, FontType color) {
		int xOff = 0;
		int yOff = 0;

		for (int i = damageTexts.Count - 1; i >= 0; i--) {
			if (damageTexts[i].time < 6) {
				yOff -= (6 - (int)damageTexts[i].time);
			}
		}
		damageTexts.Add(new DamageText(text, 0, pos, new Point(xOff, yOff), (int)color));
	}

	public void playHurtAnim() {
		if (ownedByLocalPlayer) {
			new Anim(pos.addxy(0, -25), "ra_elec", 1, player?.getNextActorNetId(), true, sendRpc: true, host: this);
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;

		if (damage > 0 && owner != null) {
			addDamageTextHelper(owner, damage, maxHealth, true);
		}

		if (goliathHealth > 0) {
			goliathHealth -= damage;
			if (goliathHealth < 0) goliathHealth = 0;
		} else {
			health -= damage;
		}

		if (owner != null && weaponIndex != null) {
			damageHistory.Add(new DamageEvent(owner, weaponIndex.Value, projId, false, Global.time));
		}

		if (health <= 0) {
			health = 0;
		}
		if (health <= 0) {
			if (character != null && !ownedByMK5 && character.startRideArmor == this) {
				character.invulnTime = 1;
			}

			int? assisterProjId = null;
			int? assisterWeaponId = null;
			Player? killer = null;
			Player? assister = null;
			getKillerAndAssister(player, ref killer, ref assister, ref weaponIndex, ref assisterProjId, ref assisterWeaponId);
			creditKill(killer, assister, weaponIndex);

			explode();
		}
	}

	public void explode(bool shrapnel = false) {
		if (!ownedByLocalPlayer) return;

		if (!isExploding) {
			isExploding = true;
			sprite.frameSpeed = 0;

			var centerPos = pos.addxy(0, -30);
			explodeAnim(centerPos, shrapnel);

			rideArmorState?.onExit(null);
			destroySelf();
		}
	}

	public void explodeAnim(Point centerPos, bool shrapnel = false) {
		if (!ownedByLocalPlayer) return;

		string raName = getRaTypeName();
		if (netOwner != null) {
		Global.level.addEffect(new ExplodeDieEffect(netOwner, centerPos, pos, raName + "_idle", 1, zIndex, false, 15, 0.5f, false));
			string piecesSpriteName = raName + "_pieces";
			if (raNum == 4) piecesSpriteName = "goliath_pieces";
			int frameCount = Global.sprites[piecesSpriteName].frames.Length;
			var shrapnelVels = getShrapnelVels(frameCount);
			bool hasRaColorShader = colorShader != null;
			for (int i = 0; i < frameCount; i++) {
				Actor piece;
				if (!shrapnel) {
					piece = new Anim(centerPos.addxy(Helpers.randomRange(-20, 20), Helpers.randomRange(-20, 20)), piecesSpriteName, 1, netOwner.getNextActorNetId(), false, sendRpc: true, hasRaColorShader: hasRaColorShader);
					piece.useGravity = true;
					piece.vel = new Point(Helpers.randomRange(-300, 300), Helpers.randomRange(-300, 25));
				} else {
					piece = new RAShrapnelProj(new VileLaser(VileLaserType.NecroBurst), centerPos, piecesSpriteName, 1, hasRaColorShader, netOwner, netOwner.getNextActorNetId(), rpc: true);
					piece.vel = shrapnelVels[i];
				}
				piece.frameIndex = i;
				piece.frameSpeed = 0;
			}
		}
	}

	public List<Point> getShrapnelVels(int frameCount) {
		var shrapnelVels = new List<Point>();
		float angle = 0;
		for (int i = 0; i < frameCount; i++) {
			shrapnelVels.Add(Point.createFromAngle(angle).times(450));
			angle -= 360 / frameCount;
		}
		return shrapnelVels;
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		if (Global.level.isRace()) return false;
		if (character == null) return false;
		return !character.isInvulnerable(true) && character.player.alliance != damagerAlliance;
	}

	public bool isInvincible(Player? attacker, int? projId) {
		if (raNum == 5) return true;
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		if (character == null) return false;
		return character.player.alliance == healerAlliance && health > 0 && health < maxHealth;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		if (!allowStacking && this.healAmount > 0) return;
		if (health < maxHealth) {
			playHealSound = true;
		}
		commonHealLogic(healer, healAmount, health, maxHealth, drawHealText);
		this.healAmount = healAmount;
	}

	public override Collider? getTerrainCollider() {
		if (physicsCollider == null) {
			return null;
		}
		return new Collider(
			new Rect(0f, 0f, 26, 46).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0, 0)
		);
	}

	public override Collider? getGlobalCollider() {
		int yHeight = 50;
		if (raNum == 5) yHeight = 60;
		if (raNum == 0 || raNum == 4) yHeight = 52;
		var rect = new Rect(0, 0, 26, yHeight);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public Collider? getLowCollider() {
		var rect = new Rect(0, 0, 26, 40);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public bool isAttacking() {
		return sprite.name.Contains("attack") || sprite.name.Contains("grab") || sprite.name.Contains("grab_punch") || sprite.name.Contains("chain") || sprite.name.Contains("charge");
	}

	public float getDashSpeed() {
		if (isDashing && !(raNum == 3 && (isUnderwater() || isWading()))) {
			return raNum == 3 ? 1.5f : 2.5f;
		}
		return 1;
	}

	public float getRunSpeed() {
		// Slow down the ride on push.
		if (xFlinchPushVel != 0) {
			return 15;
		}
		if (rideArmorState is RAFall raFall && raFall.hovering && raNum != 2) {
			return 30;
		}
		if (raNum == 3) {
			if (isSwimming) {
				return 150;
			}
			return 120;
		} else if (raNum == 2) {
			return 80;
		} else if (raNum == 1) {
			if (isNeutral) return 70;
			return 54;
		} else {
			if (isNeutral) return 80;
			return 60;
		}
	}

	public float getJumpPower() {
		if (isNeutral) {
			if (raNum == 0) return Physics.JumpSpeed * 1.15f;
			if (raNum == 1) return Physics.JumpSpeed * 1.075f;
		}
		return Physics.JumpSpeed;
	}

	public bool canDash() {
		return !isAttacking() && character?.flag == null && raNum != 3;
	}

	public Point? getCarryPos() {
		var cf = sprite.getCurrentFrame();
		if (cf == null || cf.POIs.Length < 2) return null;
		var offset = cf.POIs[1];
		offset.x *= xDir;
		return pos.add(offset);
	}

	public override void changeSprite(string? spriteName, bool resetFrame) {
		if (spriteName != null) {
			spriteName = spriteName.Replace("ridearmor_", getRaTypeName() + "_");

			var oldSpriteName = sprite?.name ?? "";
			base.changeSprite(spriteName, true);
			if (oldSpriteName == spriteName) return;

			if (ownedByLocalPlayer) return;

			spriteName = spriteName ?? "";

			if (spriteName.Contains("dash") && !oldSpriteName.Contains("dash")) {
				new Anim(pos, "dash_sparks", xDir, null, true);
			}
			if (spriteName.Contains("jump") && !oldSpriteName.Contains("jump")) {
			}
			if (spriteName.Contains("land") && !oldSpriteName.Contains("land")) {
			}
		}
	}

	float lastCrashSoundTime;
	public float flyTime;

	public void playCrashSound() {
		if (Global.time - lastCrashSoundTime > 0.33f) {
			playSound("crash", forcePlay: true, sendRpc: true);
		}
		lastCrashSoundTime = Global.time;
	}

	public string getRaTypeName() {
		if (raNum == 1) {
			return "kangaroo";
		} else if (raNum == 2) return "hawk";
		else if (raNum == 3) return "frog";
		else if (raNum == 4) return "goliath";
		else if (raNum == 5) return "devilbear";
		else {
			if (!isNeutral) return "ridearmor";
			else return "neutralra";
		}
	}

	public string getRaTypeFriendlyName() {
		if (raNum == 1) {
			return "Kangaroo Armor";
		} else if (raNum == 2) return "Hawk Armor";
		else if (raNum == 3) return "Frog Armor";
		else if (raNum == 4) return "Goliath Armor";
		else if (raNum == 5) return "Devil Bear";
		else {
			return "Black Bear";
		}
	}

	public override void destroySelf(string spriteName = "", string fadeSound = "", bool rpc = false, bool doRpcEvenIfNotOwned = false, bool favorDefenderProjDestroy = false) {
		if (rideArmorState is RAFall) {
			(rideArmorState as RAFall)?.hoverExhaust?.destroySelf();
		}

		if (hawkElec != null) {
			hawkElec.destroySelf();
		}

		if (character != null && !(character.charState is Die)) {
			if (!ownedByMK5) {
				character.changeState(new Fall(), true);
				character.changePos(pos.addxy(0, -10));
			}

			removeCharacter();
		}

		base.destroySelf(spriteName, fadeSound, rpc, doRpcEvenIfNotOwned);
	}

	public override List<ShaderWrapper> getShaders() {
		var shaders = base.getShaders() ?? new List<ShaderWrapper>();
		if (colorShader != null) {
			shaders.Add(colorShader);
		}
		return shaders;
	}

	public void creditKill(Player? killer, Player? assister, int? weaponIndex) {
		if (killer != null && killer != player) {
			/*
			killer.addKill();
			if (Global.level.gameMode is TeamDeathMatch)
			{
				if (Global.isHost)
				{
					if (player.alliance == GameMode.redAlliance) Global.level.gameMode.bluePoints++;
					if (player.alliance == GameMode.blueAlliance) Global.level.gameMode.redPoints++;
					Global.level.gameMode.syncTeamScores();
				}
			}
			*/

			killer.awardCurrency();
		}

		if (assister != null && assister != player) {
			//assister.addAssist();
			//assister.addKill();
			assister.awardCurrency();
		}

		if (ownedByLocalPlayer) {
			RPC.creditPlayerKillVehicle.sendRpc(killer, assister, this, weaponIndex);
		}
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();
		customData.Add((byte)raNum);
		// 1 means riding it, 0 means not.
		customData.Add((byte)(character != null ? 1 : 0)); 
		customData.Add((byte)neutralId);
		customData.Add((byte)MathF.Ceiling(health));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		raNum = data[0];

		int isOwnerRiding = data[1];
		if (isOwnerRiding == 0) {
			character = null;
		} else if (isOwnerRiding == 1) {
			character = netOwner?.character;
		}

		neutralId = data[2];
		health = data[3];

		if (!netColorShadersSet) {
			setColorShaders();
			netColorShadersSet = true;
		}
	}
}

public class RideArmorState {
	public string? sprite;
	public string? defaultSprite;
	public string? attackSprite;
	public string? carrySprite;
	public string? transitionSprite;
	public Point? busterOffset;
	public RideArmor rideArmor = null!;
	public Character? character { get { return rideArmor?.character; } }
	public Collider? lastLeftWall;
	public Collider? lastRightWall;
	public float stateTime;
	public string? enterSound;
	public float framesJumpNotHeld = 0;

	public RideArmorState(string? sprite, string? attackSprite = null, string? carrySprite = null, string? transitionSprite = null) {
		this.sprite = string.IsNullOrEmpty(transitionSprite) ? sprite : transitionSprite;
		this.transitionSprite = transitionSprite;
		defaultSprite = sprite;
		this.attackSprite = attackSprite;
		this.carrySprite = carrySprite;
		stateTime = 0;
	}

	public bool canAttack() {
		return !string.IsNullOrEmpty(attackSprite) && character != null && !character.isInvulnerable(factorHyperMode: true);
	}

	public Player? player {
		get {
			Player? charPlayer = character?.player;
			if (charPlayer != null) {
				return charPlayer;
			} else if (rideArmor.ownedByMK5) {
				return rideArmor.netOwner;
			}
			return null;
		}
	}

	public virtual void onExit(RideArmorState? newState) {
		//Stop the dash speed on transition to any frame except jump/fall (dash lingers in air) or dash itself
		if (!(newState is RAJump) && !(newState is RAFall) && !(newState is RADash) && !(newState is RAGoliathShoot)) {
			rideArmor.isDashing = false;
		}
	}

	public virtual void onEnter(RideArmorState? oldState) {
		if (!string.IsNullOrEmpty(enterSound)) rideArmor?.playSound(enterSound);
	}

	public bool inTransition() {
		if (Global.level.rideArmorFlight) {
			return false;
		}

		var realTransitionSprite = transitionSprite?.Replace("ridearmor", rideArmor?.getRaTypeName() ?? "") ?? transitionSprite;
		return !string.IsNullOrEmpty(transitionSprite) && realTransitionSprite != null 
		&& sprite == transitionSprite && rideArmor?.sprite?.name != null 
		&& rideArmor.sprite.name.Contains(realTransitionSprite);
	}

	public virtual void update() {
		stateTime += Global.spf;
	}

	public void airCode() {
		if (player == null) return;

		if (rideArmor != null && rideArmor.grounded) {
			switch (rideArmor.raNum) {
				case 0:
				case 5:
					if (Helpers.randomRange(0, 1) != 1) {
						rideArmor.playSound("ridewalk", forcePlay: false, sendRpc: true);
					} else {
						rideArmor.playSound("ridewalk2", forcePlay: false, sendRpc: true);
					}
					break;
				case 1:
				case 2:
				case 3:
					if (Helpers.randomRange(0, 1) != 1) {
						rideArmor.playSound("ridewalkX3", forcePlay: false, sendRpc: true);
					} else {
					rideArmor.playSound("ridewalk2X3", forcePlay: false, sendRpc: true);
					}
					break;
				case 4:
					rideArmor.playSound("crashX3", false, true);
					rideArmor.shakeCamera(sendRpc: true);
					break;
			}
			string ts = "ridearmor_land".Replace("ridearmor", rideArmor.getRaTypeName());
			if (!rideArmor.isAttacking() || rideArmor.raNum == 2 || rideArmor.raNum == 3) {
				rideArmor.changeState(new RAIdle(ts));
			} else {
				int oldFrameIndex = rideArmor.sprite.frameIndex;
				float oldFrameTime = rideArmor.sprite.frameTime;
				rideArmor.changeState(new RAIdle(ts));
				string? attackSprite = rideArmor.rideArmorState?.attackSprite;
				rideArmor.changeSprite(attackSprite, false);
				rideArmor.frameIndex = oldFrameIndex;
				rideArmor.frameTime = oldFrameTime;
			}
			if (rideArmor.consecutiveJump < 2) rideArmor.consecutiveJumpTimeout = 0.25f;
			else rideArmor.consecutiveJump = 0;
			return;
		}

		var raJump = rideArmor?.rideArmorState as RAJump;
		if (rideArmor?.gravityWellModifier > 0 && (raJump == null || !raJump.isLeapFrog)) {
			if (!player.input.isHeld(Control.Jump, player) && rideArmor.vel.y < 0) {
				framesJumpNotHeld++;
				if (framesJumpNotHeld > 3) {
					framesJumpNotHeld = 0;
					rideArmor.vel.y = 0;
				}
			}
			if (player.input.isHeld(Control.Jump, player)) {
				framesJumpNotHeld = 0;
			}
		}

		if (Global.level.checkTerrainCollisionOnce(rideArmor, 0, -1) != null && rideArmor?.vel.y < 0) {
			rideArmor.vel.y = 0;
		}

		var move = new Point(0, 0);
		if (rideArmor != null) {
			if (player.input.isHeld(Control.Left, player)) {
				move.x = -rideArmor.getRunSpeed() * rideArmor.getDashSpeed();
				rideArmor.xDir = -1;
			} else if (player.input.isHeld(Control.Right, player)) {
				move.x = rideArmor.getRunSpeed() * rideArmor.getDashSpeed();
				rideArmor.xDir = 1;
			}

			if (move.magnitude > 0) {
				rideArmor.move(move);
			}

			if (rideArmor.raNum == 3 && player.input.isPressed(Control.Dash, player) && player.input.isHeld(Control.Down, player)) {
				rideArmor.changeState(new RAGroundPoundStart(), true);
			}
		}
	}

	public float shootHeldTime;

	public void commonGroundCode() {
		if (!rideArmor.grounded) {
			rideArmor.changeState(new RAFall());
			return;
		}

		rideArmor.isSwimming = false;
	}

	public bool jumpPressed() {
		return player != null && player.input.isPressed(Control.Jump, player) && !player.input.isHeld(Control.Up, player);
	}

	public void groundCode() {
		commonGroundCode();
		if (rideArmor.isAttacking()) {
			return;
		}

		if (jumpPressed()) {
			rideArmor.vel.y = -rideArmor.getJumpPower();
			rideArmor.changeState(new RAJump());
			if (rideArmor.raNum >= 1 && rideArmor.raNum <= 4) {
				rideArmor.playSound("ridejumpX3", sendRpc: true);
			} else {
				rideArmor.playSound("ridejump", sendRpc: true);
			}
		} else if (player != null && player.input.isHeld(Control.Down, player) && !rideArmor.isDashing) {
		} else if (player != null && player.input.isPressed(Control.Taunt, player)) {
			rideArmor.changeState(new RATaunt());
		}
	}
}

public class RADropIn : RideArmorState {
	public bool warpSoundPlayed;
	public Point spawnPos;
	public float yVel;
	public RADropIn() : base("ridearmor_idle") {
	}

	public override void update() {
		if (character != null) {
			if (!Global.level.mainPlayer.readyTextOver) {
				character.visible = false;
				rideArmor.visible = false;
				rideArmor.frameSpeed = 0;
				return;
			} else {
				character.visible = true;
				rideArmor.visible = true;
				rideArmor.frameSpeed = 1;
			}
		}
		yVel += Global.speedMul * rideArmor.getGravity();
		float yInc = Global.spf * yVel;
		rideArmor.incPos(new Point(0, yInc));
		if (rideArmor.pos.y >= spawnPos.y) {
			rideArmor.shakeCamera(sendRpc: true);
			rideArmor.changePos(new Point(rideArmor.pos.x, spawnPos.y));
			rideArmor.changeState(new RAIdle("ridearmor_land"), true);
		}
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		rideArmor.useGravity = false;
		spawnPos = rideArmor.pos;
		yVel = 0;
		rideArmor.incPos(new Point(0, -150));
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
		if (character != null && player != null) {
			character.invulnTime = player.warpedInOnce ? 2 : 0;
			player.warpedInOnce = true;
		}
		rideArmor.useGravity = true;
		rideArmor.visible = true;
	}
}

public class RADeactive : RideArmorState {
	public RADeactive(string transitionSprite = "ridearmor_deactivating") : base("ridearmor_deactive", "", "", transitionSprite) {
	}

	public override void update() {
		base.update();
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		if (rideArmor.character != null) {
			rideArmor.character.changeState(new Fall(), true);
			rideArmor.removeCharacter();
		}
		rideArmor.consecutiveJumpTimeout = 0;
		rideArmor.consecutiveJump = 0;
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
		rideArmor.manualDisabled = false;
	}
}

public class RAIdle : RideArmorState {
	public RAIdle(string transitionSprite = "") : base("ridearmor_idle", "ridearmor_attack", "ridearmor_carry", transitionSprite) {
	}

	float attackCooldown = 0;
	public override void update() {
		base.update();

		if (inTransition()) {
			if (!(transitionSprite == "frog_land" && rideArmor.frameIndex > 1)) {
				return;
			}
		}

		if (character == null || (character.charState is not InRideArmor && !rideArmor.ownedByMK5)) {
			return;
		}

		Helpers.decrementTime(ref attackCooldown);

		if (player != null && rideArmor.raNum == 1 && player.input.isHeld(Control.Shoot, player) && !rideArmor.isAttacking()) {
			shootHeldTime += Global.spf;
			if (shootHeldTime > 0.5f) {
				shootHeldTime = 0;
				rideArmor.chainSoundTime = 1;
				rideArmor.changeState(new RAChainCharge(), true);
			}
		}
		if (rideArmor.isAttacking()) shootHeldTime = 0;

		if (character.flag == null) {
			if (player != null && player.isVile && player.input.isHeld(Control.Down, player)) {
				(character.charState as InRideArmor)?.setHiding(true);
				if (!rideArmor.isAttacking()) {
					if (player.input.isHeld(Control.Left, player)) rideArmor.xDir = -1;
					if (player.input.isHeld(Control.Right, player)) rideArmor.xDir = 1;
				}
				commonGroundCode();
				return;
			} else {
				(character.charState as InRideArmor)?.setHiding(false);
			}
		}
		if (player != null) {
			bool jumpHeld = player.input.isHeld(Control.Jump, player);
			bool dashHeld = player.input.isHeld(Control.Dash, player) && character.flag == null;
			if (jumpHeld && rideArmor.raNum == 3 && character.flag == null) {
				if (rideArmor.consecutiveJumpTimeout > 0 && character.flag == null && !rideArmor.isUnderwater()) {
					rideArmor.consecutiveJumpTimeout = 0;
					rideArmor.consecutiveJump++;
				}

				string sound = "frogjumpX3";
				float modifier = 1;
				if (rideArmor.consecutiveJump == 1) {
					sound = "frogjump2X3";
					modifier = 1.25f;
				} else if (rideArmor.consecutiveJump == 2) {
					sound = "frogjump3X3";
					modifier = 1.5f;
				}

				if (dashHeld) modifier *= 0.75f;

				rideArmor.playSound(sound, forcePlay: true, sendRpc: true);
				rideArmor.vel.y = -rideArmor.getJumpPower() * 1.5f * modifier;
				rideArmor.changeState(new RAJump(isDash: dashHeld));
				return;
			}

			if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
				if (!rideArmor.isAttacking()) {
					if (player.input.isHeld(Control.Left, player)) rideArmor.xDir = -1;
					if (player.input.isHeld(Control.Right, player)) rideArmor.xDir = 1;
					if (rideArmor.raNum != 3) {
						rideArmor.changeState(new RARun());
					} else {
						rideArmor.playSound("ridejumpX3");
						dashHeld = player.input.isHeld(Control.Dash, player) && character.flag == null;
						float modifier = dashHeld ? 0.75f : 1f;
						rideArmor.vel.y = -rideArmor.getJumpPower() * modifier;
						rideArmor.changeState(new RAJump(isLeapFrog: true, isDash: dashHeld));
						return;
					}
				}
			}
			groundCode();
			if (player.input.isPressed(Control.Dash, player)) {
				if (rideArmor.canDash()) rideArmor.changeState(new RADash());
			}

			if (Global.level.gameMode.isOver && player != null && character != null) {
				if (Global.level.gameMode.playerWon(player)) {
					if (player.isVile && character.rideArmor != null) {
						var inRideArmor = character.charState as InRideArmor;
						if (inRideArmor != null) inRideArmor.setHiding(false);
						rideArmor.changeState(new RATaunt());
					}
				}
			}
		}
	}
}

public class RAGrab : RideArmorState {
	public RAGrab(string transitionSprite = "") : base("ridearmor_grab", "", "", transitionSprite) {
	}

	public override void update() {
		base.update();
		if (rideArmor.isAnimOver()) {
			rideArmor.changeState(new RAIdle());
		}
	}
}

public class RATaunt : RideArmorState {
	public RATaunt() : base("ridearmor_taunt", "", "", "") {
	}

	public override void update() {
		base.update();
		if (rideArmor.loopCount > 3) {
			if (Global.level.gameMode.isOver && player != null && character != null) {
				if (Global.level.gameMode.playerWon(player)) {
					rideArmor.frameIndex = 0;
					rideArmor.frameSpeed = 0;
					return;
				}
			}
			rideArmor.changeState(new RAIdle());
		}
	}
}

public class RARun : RideArmorState {
	public RARun() : base("ridearmor_run", "ridearmor_attack_dash", "ridearmor_carry_run") {
	}

	public override void update() {
		if (character == null) {
			rideArmor.changeState(new RAIdle());
			return;
		}

		base.update();
		var move = new Point(0, 0);
		if (player != null) {
			if (player.input.isHeld(Control.Left, player)) {
				rideArmor.xDir = -1;
				move.x = -rideArmor.getRunSpeed();
			} else if (player.input.isHeld(Control.Right, player)) {
				rideArmor.xDir = 1;
				move.x = rideArmor.getRunSpeed();
			}
			if (move.magnitude > 0) {
				rideArmor.move(move);
			} else {
				rideArmor.changeState(new RAIdle());
			}
			groundCode();
			if (player.input.isPressed(Control.Dash, player) && rideArmor.canDash()) {
				rideArmor.changeState(new RADash());
			}
		}
	}

}

public class RAJump : RideArmorState {
	public bool isLeapFrog;
	public bool isDash;
	public RAJump(bool isLeapFrog = false, bool isDash = false) : base("ridearmor_jump", "ridearmor_attack_air", "ridearmor_carry_air") {
		enterSound = "";
		this.isLeapFrog = isLeapFrog;
		this.isDash = isDash;
	}

	public override void update() {
		base.update();
		if (rideArmor.vel.y > 0) {
			rideArmor.changeState(new RAFall());
			return;
		}
		airCode();
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		if (isDash) {
			rideArmor.isDashing = true;
		}
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
	}
}

public class RAFall : RideArmorState {
	public bool hovering;
	bool hoveredOnce;
	float hoverTime;
	public Anim? hoverExhaust;
	public RAFall() : base("ridearmor_fall", "ridearmor_attack_air", "ridearmor_carry_air") {
	}

	public override void update() {
		base.update();
		if (player == null) return;

		airCode();
		if (rideArmor != null) {
			if (rideArmor.isVileNArmor() || rideArmor.raNum == 4) defaultHoverCode();
			if (rideArmor.raNum == 2) hawkHoverCode();
			else if (rideArmor.raNum == 3) frogHoverCode();
		}
	}

	private float getHoverVelY(float velY) {
		if (rideArmor?.gravityWellModifier > 1) {
			velY = 53;
		}
		return velY;
	}

	private void defaultHoverCode() {
		updateHoverExhaustPos();
		if (player != null) {
			if (player.input.isHeld(Control.Jump, player)) {
				if (!hoveredOnce) {
					hovering = true;
					hoveredOnce = true;
				}
			} else {
				hovering = false;
			}
		}
		if (hoverExhaust != null) {
			if (hovering) {
				hoverExhaust.visible = true;
				hoverTime += Global.spf;
				rideArmor.vel.y = getHoverVelY(-53);
				if (hoverTime > 1f) {
					hovering = false;
				}
			} else {
				hoverExhaust.visible = false;
			}
		}
	}

	private void hawkHoverCode() {
		if (player != null) {
			if (player.input.isHeld(Control.Jump, player) && character?.flag == null) {
				if (rideArmor.pos.y > -5) {
					hovering = true;
				} else {
					rideArmor.flyTime += Global.spf;
					hovering = false;
				}
			} else {
				hovering = false;
			}
		}
		if (hovering) {
			rideArmor.flyTime += Global.spf;
			if (rideArmor.flyTime > 6) {
				hovering = false;
			}
		}

		if (hovering) {
			if (!rideArmor.sprite.name.Contains("hover") && !rideArmor.sprite.name.Contains("attack")) {
				rideArmor.changeSprite("hawk_hover", true);
			}

			rideArmor.vel.y = getHoverVelY(-106);
		} else {
			if (rideArmor.raNum == 3 && rideArmor.sprite.name.Contains("hover")) {
				rideArmor.changeSprite("hawk_fall", true);
			}
		}
	}

	float swimTime;
	bool lastFrameWasUnderwater;
	private void frogHoverCode() {
		if (player != null && player.input.isHeld(Control.Jump, player) && rideArmor.isUnderwater() && character?.flag == null) {
			hovering = true;
			rideArmor.isSwimming = true;
		} else {
			hovering = false;
		}

		if (hovering) {
			if (swimTime < 1) swimTime += Global.spf;
			else swimTime = 1;
			rideArmor.flyTime += Global.spf;
			if (rideArmor.flyTime > 6) {
				hovering = false;
			}
		} else {
			if (swimTime > 0) swimTime -= Global.spf;
			else swimTime = 0;
		}

		if (rideArmor.sprite.name.Contains("swim")) {
			rideArmor.sprite.frameSpeed = ((swimTime * 0.75f) + 0.25f);
		}

		if (hovering) {
			if (!rideArmor.sprite.name.Contains("swim") && !rideArmor.sprite.name.Contains("attack")) {
				rideArmor.changeSprite("frog_swim", true);
			}

			rideArmor.vel.y = -106;
		} else {
			if (rideArmor.raNum == 3 && rideArmor.sprite.name.Contains("swim") && swimTime == 0) {
				rideArmor.changeSprite("frog_fall", true);
			}
		}

		if (!rideArmor.isUnderwater() && player != null) {
			if (lastFrameWasUnderwater && player.input.isHeld(Control.Jump, player) && player.input.isHeld(Control.Up, player)) {
				rideArmor.vel.y = -425;
			}
		}
		lastFrameWasUnderwater = rideArmor.isUnderwater();
	}

	public void updateHoverExhaustPos() {
		if (hoverExhaust != null) {
			hoverExhaust.xDir = rideArmor.xDir;
			float xOff = -25;
			if (rideArmor.sprite.name.Contains("attack") || rideArmor.sprite.name.Contains("carry")) xOff = -20;
			hoverExhaust.pos = rideArmor.pos.addxy(xOff * rideArmor.xDir, -32);
		}
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		hoverExhaust = new Anim(rideArmor.pos, "ridearmor_hover", rideArmor.xDir, null, false);
		hoverExhaust.visible = false;
		hoverExhaust.setzIndex(ZIndex.Character - 1);
		updateHoverExhaustPos();
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
		hoverExhaust?.destroySelf();
	}

}

public class RAGroundPoundStart : RideArmorState {
	public RAGroundPoundStart(string transitionSprite = "") : base("frog_groundpound_start") {
	}

	public override void update() {
		base.update();

		rideArmor.incPos(new Point(0, -200 * Global.spf));

		if (rideArmor.isAnimOver()) {
			rideArmor.changeState(new RAGroundPound());
		}
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		rideArmor.vel = new Point();
		rideArmor.useGravity = false;
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
		rideArmor.useGravity = true;
	}
}

public class RAGroundPound : RideArmorState {
	public RAGroundPound(string transitionSprite = "") : base("frog_groundpound") {
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (rideArmor.grounded) {
			rideArmor.changeState(new RAGroundPoundLand(), true);
		}
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		rideArmor.vel = new Point(0, 400);
	}
}

public class RAGroundPoundLand : RideArmorState {
	public RAGroundPoundLand(string transitionSprite = "") : base("frog_groundpound_land") {
	}

	public override void update() {
		base.update();

		if (rideArmor.isAnimOver()) {
			rideArmor.changeState(new RAIdle(), true);
		}
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		if (player == null) return;
		rideArmor.playSound("crash", sendRpc: true);
		new MechFrogStompShockwave(new MechFrogStompWeapon(player), rideArmor.pos.addxy(6 * rideArmor.xDir, 0), rideArmor.xDir, player, player.getNextActorNetId(), rpc: true);
		if (rideArmor.consecutiveJump < 2) rideArmor.consecutiveJumpTimeout = 0.25f;
		else rideArmor.consecutiveJump = 0;
	}
}

public class RADash : RideArmorState {
	public float dashTime = 0;
	float dashAttackTime = 0;
	public Character? draggedChar;

	public RADash() : base("ridearmor_dash", "ridearmor_attack_dash", "ridearmor_carry_dash") {
		enterSound = "";
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		if (rideArmor.raNum == 0 || rideArmor.raNum == 5) {
			rideArmor.playSound("ridedash", false, true);
		}
		if (rideArmor.raNum == 1 || rideArmor.raNum == 2 || rideArmor.raNum == 3 || rideArmor.raNum == 4) {
			rideArmor.playSound("ridedashX3", false, true);
		}
		rideArmor.isDashing = true;
		new Anim(rideArmor.pos.addxy(rideArmor.xDir * -15, 0), "dash_sparks", rideArmor.xDir, null, true);
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
	}

	public override void update() {
		base.update();
		groundCode();
		var move = getDashVel();
		rideArmor.move(move);
		if (player != null) {
			bool isHeldDashAttack = rideArmor.raNum == 4 && player.input.isHeld(Control.Shoot, player);
			if (isHeldDashAttack) {
				dashTime = 0;
				dashAttackTime += Global.spf;
				if (dashAttackTime > 2) {
					rideArmor.changeState(new RAIdle());
					return;
				}
				if (rideArmor.sprite.name.Contains("attack") && rideArmor.frameIndex == 2) {
					rideArmor.frameTime = 0;
				}
				var hitWall = Global.level.checkTerrainCollisionOnce(rideArmor, move.x * Global.spf * 2, 0, null);
				if (hitWall?.isSideWallHit() == true) {
					rideArmor.playSound("crashX3", forcePlay: false, sendRpc: true);
					rideArmor.shakeCamera(sendRpc: true);
					rideArmor.changeState(new RAIdle());
					if (draggedChar != null) {
						var mgpw = new MechGoliathPunchWeapon(player);
						mgpw.applyDamage(draggedChar, false, rideArmor, (int)ProjIds.MechPunch);
					}
					return;
				}
			} else if (!player.input.isHeld(Control.Dash, player)) {
				rideArmor.changeState(new RAIdle());
				return;
			}
		}

		dashTime += Global.spf;
		if (dashTime > 0.6) {
			rideArmor.changeState(new RAIdle());
			return;
		}
		if (stateTime > 0.1) {
			stateTime = 0;
			new Anim(rideArmor.pos.addxy(rideArmor.xDir * -15, -4), "dust", rideArmor.xDir, null, true);
		}
	}

	public Point getDashVel() {
		return new Point(rideArmor.getRunSpeed() * rideArmor.getDashSpeed() * rideArmor.xDir, 0);
	}
}

public class RACalldown : RideArmorState {
	const float warpHeight = 150;
	float origYPos;
	int phase = 0;
	Point summonPos;
	bool isNew;
	public RACalldown(Point summonPos, bool isNew) : base("vile_warp_beam") {
		this.summonPos = summonPos;
		this.isNew = isNew;
		enterSound = "warpIn";
	}

	public override void update() {
		base.update();
		if (phase == 0) {
			rideArmor.incPos(new Point(0, -Global.spf * 450));
			if (rideArmor.pos.y < origYPos - warpHeight) {
				rideArmor.changePos(summonPos.addxy(0, -warpHeight));
				phase = 1;
			}
		} else if (phase == 1) {
			rideArmor.incPos(new Point(0, Global.spf * 450));
			if (rideArmor.pos.y >= summonPos.y) {
				if (!rideArmor.ownedByMK5) {
					rideArmor.changeState(new RADeactive(), true);
				} else {
					rideArmor.changeState(new RAIdle(), true);
				}
			}
		}
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		rideArmor.vel = Point.zero;
		rideArmor.xIceVel = 0;
		rideArmor.xPushVel = 0;
		rideArmor.xFlinchPushVel = 0;
		rideArmor.useGravity = false;
		origYPos = rideArmor.pos.y;
		if (rideArmor.character != null && !rideArmor.ownedByMK5 && (rideArmor.character as Vile)?.isVileMK5 != true) {
			rideArmor.character.changeState(new Fall(), false);
			rideArmor.character.changePos(rideArmor.pos.addxy(0, -10));
			rideArmor.removeCharacter();
		}
		if (isNew) {
			rideArmor.changePos(summonPos.addxy(0, -warpHeight));
			phase = 1;
		}
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
		rideArmor.useGravity = true;
	}
}

public class RAChainCharge : RideArmorState {
	public RAChainCharge() : base("ridearmor_charge", "", "") {
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
		if (!(newState is RAChainChargeDash)) {
			rideArmor.chainTotalSoundTime = 0;
		}
	}

	public override void update() {
		base.update();
		if (!rideArmor.ownedByLocalPlayer) return;
	if (player != null) {
			if (!player.input.isHeld(Control.Shoot, player)) {
				rideArmor.changeState(new RAChainAttack(), true);
				return;
			}

			if (player.input.isHeld(Control.Left, player)) {
				rideArmor.xDir = -1;
			} else if (player.input.isHeld(Control.Right, player)) {
				rideArmor.xDir = 1;
			}

			if (player.dashPressed(out string dashControl)) {
				bool isHiding = (character?.charState as InRideArmor)?.isHiding ?? false;
				rideArmor.changeState(new RAChainChargeDash(dashControl, isHiding), true);
				return;
			}
	}
		rideArmor.chainSoundTime += Global.spf;
		rideArmor.chainTotalSoundTime += Global.spf;
		if (rideArmor.chainSoundTime > 60f/60f && rideArmor.chainTotalSoundTime < 4) {
			rideArmor.playSound("kangarooDrillX3", sendRpc: true);
			rideArmor.chainSoundTime = 0;
		}
	}
}

public class RAChainChargeDash : RideArmorState {
	string dashControl;
	public float dashTime;
	public bool isSlow;
	public RAChainChargeDash(string dashControl, bool isSlow) : base("ridearmor_charge_dash", "", "") {
		this.dashControl = dashControl;
		this.isSlow = isSlow;
		enterSound = "dash";
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
		rideArmor.playSound("ridedashX3", false, true);	
		rideArmor.isDashing = true;
		new Anim(rideArmor.pos, "dash_sparks", rideArmor.xDir, null, true);
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
		if (!(newState is RAChainCharge)) {
			rideArmor.chainTotalSoundTime = 0;
		}
	}

	public override void update() {
		base.update();
		if (!rideArmor.ownedByLocalPlayer) return;

		if (!rideArmor.grounded) {
			rideArmor.changeState(new RAFall());
			return;
		}
		float modifier = (isSlow ? 0.5f : 0.75f);
		rideArmor.move(new Point(rideArmor.xDir * rideArmor.getRunSpeed() * rideArmor.getDashSpeed() * modifier, 0));
		if (player != null) {
			if (!player.input.isHeld(dashControl, player)) {
				rideArmor.changeState(new RAChainCharge(), true);
				return;
			} else if (!player.input.isHeld(Control.Shoot, player)) {
				rideArmor.changeState(new RAChainAttack(), true);
				return;
			}
		}
		if (stateTime > 0.1) {
			stateTime = 0;
			new Anim(rideArmor.pos.addxy(0, -4), "dust", rideArmor.xDir, null, true);
		}
		dashTime += Global.spf;
		if (dashTime > 0.6f) {
			dashTime = 0;
			rideArmor.changeState(new RAChainCharge());
		}
		rideArmor.chainSoundTime += Global.spf;
		rideArmor.chainTotalSoundTime += Global.spf;
		if (rideArmor.chainSoundTime > 60f/60f && rideArmor.chainTotalSoundTime < 4) {
			rideArmor.playSound("kangarooDrillX3", sendRpc: true);
			rideArmor.chainSoundTime = 0;
		}
	}
}

public class RAChainAttack : RideArmorState {
	float frame5Time;
	MechChainProj? mcp;
	int once;
	public RAChainAttack() : base("ridearmor_chain", "", "") {
	}

	public Point chainOrigin() {
		return rideArmor.pos.addxy(47 * rideArmor.xDir, -25);
	}

	public override void update() {
		base.update();
		if (!rideArmor.ownedByLocalPlayer) return;
		if (player == null) {
			rideArmor.changeState(new RAIdle(), true);
			return;
		}

		if (player.input.isPressed(Control.Shoot, player)) {
			if (rideArmor.frameIndex < 8) {
				//rideArmor.frameIndex = 15 - rideArmor.frameIndex;
			} else if (rideArmor.frameIndex == 8) {
				frame5Time = 1;
			}
		}

		if (rideArmor.frameIndex == 1 && once == 0 && rideArmor.player != null) {
			once = 1;
			mcp = new MechChainProj(new MechChainWeapon(player), chainOrigin(), rideArmor.xDir, rideArmor.player, player.getNextActorNetId(), rpc: true);
		}
		if (rideArmor.frameIndex == 14 && once == 1) {
			once = 2;
			mcp?.destroySelf();
			mcp = null;
		}
		if (mcp != null) {
			float xOff = (rideArmor.frameIndex - 2) * 8;
			if (rideArmor.frameIndex > 8) xOff = (14 - rideArmor.frameIndex) * 8;
			mcp.changePos(chainOrigin().addxy(xOff * rideArmor.xDir, 0));
		}

		if (rideArmor.frameIndex == 8) {
			if (frame5Time < 30f/60f) {
				rideArmor.frameTime = 0;
			}
			frame5Time += Global.spf;
		}

		if (rideArmor.frameIndex == 16) {
			rideArmor.changeState(new RAIdle(), true);
		}
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
		mcp?.destroySelf();
	}
}

public class RAGoliathShoot : RideArmorState {
	bool grounded;
	bool once;
	public RAGoliathShoot(bool grounded) : base(grounded ? "ridearmor_shoot" : "ridearmor_jump_shoot", "", "") {
		this.grounded = grounded;
	}

	public override void onEnter(RideArmorState? oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(RideArmorState? newState) {
		base.onExit(newState);
	}

	public override void update() {
		base.update();
		if (grounded) {
			groundCode();
		} else {
			airCode();
		}

		if (rideArmor.frameIndex == 3 && !once) {
			once = true;
			//mechBusterCooldown = 1f;
			if (player != null) {
				var mbw = new MechBusterWeapon(player);
				rideArmor.playSound("buster2X3", forcePlay: false, sendRpc: true);
				new MechBusterProj2(mbw, rideArmor.pos.addxy(15 * rideArmor.xDir, -36), rideArmor.xDir, 0, player, player.getNextActorNetId(), rpc: true);
				new MechBusterProj(mbw, rideArmor.pos.addxy(15 * rideArmor.xDir, -36), rideArmor.xDir, player, player.getNextActorNetId(), rpc: true);
				new MechBusterProj2(mbw, rideArmor.pos.addxy(15 * rideArmor.xDir, -36), rideArmor.xDir, 1, player, player.getNextActorNetId(), rpc: true);
			}
		}

		if (rideArmor.isAnimOver()) {
			rideArmor.changeState(grounded ? new RAIdle() : new RAFall());
		}
	}
}
/*
public class RASample : RideArmorState
{
	public RASample() : base("ridearmor_sample", "", "")
	{
	}

	public override void onEnter(RideArmorState oldState)
	{
		base.onEnter(oldState);
	}

	public override void onExit(RideArmorState newState)
	{
		base.onExit(newState);
	}

	public override void update()
	{
		base.update();
		groundCode();
		airCode();
	}
}
*/

public class InRideArmor : CharState {
	public bool isHiding;
	public float frozenTime;
	public float stunTime;
	public float crystalizeTime;
	public Anim? stunAnim;
	public Anim? freezeAnim;
	public bool winTaunt;
	public float innerCooldown;
	public InRideArmor(string transitionSprite = "") : base("ra_idle", "", "", transitionSprite) {
	}

	public override void update() {
		base.update();

		if (!character.ownedByLocalPlayer) return;

		Helpers.decrementTime(ref innerCooldown);

		float healthPercent = player.health / player.maxHealth;
		if (frozenTime > 0) {
			if (freezeAnim == null) freezeAnim = new Anim(character.pos, "frozen_block_head", character.xDir, character.player.getNextActorNetId(), false, sendRpc: true);
			freezeAnim.pos = character.pos;
			frozenTime -= (Global.spf + (healthPercent * player.input.mashCount * 0.25f));
			if (frozenTime <= 0) {
				frozenTime = 0;
				character.freezeInvulnTime = 2;
				character.breakFreeze(player, sendRpc: true);
				if (freezeAnim != null) {
					freezeAnim.destroySelf();
					freezeAnim = null;
				}
			}
		}

		if (stunTime > 0) {
			stunTime -= (Global.spf + (healthPercent * player.input.mashCount * 0.25f));
			if (stunTime <= 0) {
				character.stunInvulnTime = 2;
				stunTime = 0;
			}
		}

		if (crystalizeTime > 0) {
			crystalizeTime -= (Global.spf + (healthPercent * player.input.mashCount * 0.25f));
			checkCrystalizeTime();
		}

		if (stunTime > 0) {
			if (stunAnim == null) stunAnim = new Anim(character.getCenterPos(), "vile_stun_static", 1, character.player.getNextActorNetId(), false, sendRpc: true);
			stunAnim.pos = character.getCenterPos();
		} else if (stunAnim != null) {
			stunAnim.destroySelf();
			stunAnim = null;
		}

		if (!isHiding) {
			if (character.rideArmor != null && character.rideArmor.isAttacking()) {
				character.changeSpriteFromName("ra_attack", true);
				character.frameSpeed = 0;
				var mapping = new List<int>() { 0, 1, 1, 1, 0 };

				//if (mapping.Count >= character.rideArmor.sprite.frameIndex) character.frameIndex = mapping[4];
				//else
				{
					if (mapping.InRange(character.rideArmor.sprite.frameIndex)) {
						character.frameIndex = mapping[character.rideArmor.sprite.frameIndex];
					}
				}
			} else if (!character.sprite.name.Contains("ra_show") || character.sprite.isAnimOver()) {
				character.changeSpriteFromName("ra_idle", true);
			}
		} else {
			if (character is Vile vile && player.input.isPressed(Control.Special1, player)) {
				tossGrenade(vile);
			}
		}

		bool ejectInput = character.player.input.isHeld(Control.Up, player) && character.player.input.isPressed(Control.Jump, player);
		if (ejectInput) {
			if (character.canEjectFromRideArmor()) {
				character.vel.y = -character.getJumpPower();
				character.changeState(new Jump(), true);
			}
		}
	}

	public void tossGrenade(Vile vile) {
		Projectile? grenade = null;
		if (vile.napalmWeapon.shootTime > 0) {
			return;
		}
		if (vile.napalmWeapon.type == (int)NapalmType.SplashHit) {
			vile.setVileShootTime(vile.napalmWeapon);
			grenade = new SplashHitGrenadeProj(
				vile.napalmWeapon, character.pos.addxy(0, -3),
				character.xDir, character.player, character.player.getNextActorNetId(), rpc: true
			);
		} else if (vile.napalmWeapon.type == (int)NapalmType.FireGrenade) {
			vile.setVileShootTime(vile.napalmWeapon);
			grenade = new MK2NapalmGrenadeProj(
				vile.napalmWeapon, character.pos.addxy(0, -3), character.xDir,
				character.player, character.player.getNextActorNetId(), rpc: true
			);
		} else {
			vile.setVileShootTime(vile.napalmWeapon, targetCooldownWeapon: new Napalm(NapalmType.RumblingBang));
			grenade = new NapalmGrenadeProj(
				new Napalm(NapalmType.RumblingBang), character.pos.addxy(0, -3),
				character.xDir, character.player, character.player.getNextActorNetId(), rpc: true
			);
		}
		/*
		else if (player.vileNapalmWeapon.type == (int)NapalmType.NoneBall)
		{
			if (player.vileBallWeapon.type == (int)VileBallType.ExplosiveRound)
			{
				if (player.vileBallWeapon.shootTime == 0) character.setVileShootTime(player.vileBallWeapon);
				else return;
				grenade = new VileBombProj(player.vileBallWeapon, character.pos.addxy(0, -3), character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
				grenade.maxTime = 1f;
			}
			else if (player.vileBallWeapon.type == (int)VileBallType.SpreadShot)
			{
				if (player.vileBallWeapon.shootTime == 0) character.setVileShootTime(player.vileBallWeapon);
				else return;
				Point vel = Point.createFromAngle(-45).times(150);
				if (character.xDir == -1) vel.x *= -1;
				new StunShotProj(player.vileBallWeapon, character.pos.addxy(0, -3), character.xDir, 1, character.player, character.player.getNextActorNetId(), vel, rpc: true);
				Global.level.delayedActions.Add(new DelayedAction(() =>
				{
					Point vel = Point.createFromAngle(-22.5f).times(150);
					if (character.xDir == -1) vel.x *= -1;
					new StunShotProj(player.vileBallWeapon, character.pos.addxy(0, -3), character.xDir, 1, character.player, character.player.getNextActorNetId(), vel, rpc: true);
				}, 0.15f));
				Global.level.delayedActions.Add(new DelayedAction(() =>
				{
					Point vel = Point.createFromAngle(0).times(150);
					if (character.xDir == -1) vel.x *= -1;
					new StunShotProj(player.vileBallWeapon, character.pos.addxy(0, -3), character.xDir, 1, character.player, character.player.getNextActorNetId(), vel, rpc: true);
				}, 0.3f));
			}
			else if (player.vileBallWeapon.type == (int)VileBallType.PeaceOutRoller)
			{
				if (player.vileBallWeapon.shootTime == 0) character.setVileShootTime(player.vileBallWeapon);
				else return;
				grenade = new PeaceOutRollerProj(player.vileBallWeapon, character.pos.addxy(0, -3), character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
				grenade.maxTime = 1;
				grenade.gravityModifier = 1;
			}
		}
		*/

		if (grenade != null) {
			grenade.vel.x = 75 * (character.xDir);
			grenade.vel.y = -200;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.frameSpeed = 0;
		character.setGlobalColliderTrigger(true);
		var mechWeapon = player.weapons.FirstOrDefault(m => m is MechMenuWeapon) as MechMenuWeapon;
		if (mechWeapon != null) mechWeapon.isMenuOpened = false;
		if (character.isCharging() && !player.isVile) {
			character.stopCharge();
		}
	}

	public override void onExit(CharState newState) {
		character.rideArmor = null;
		character.useGravity = true;
		character.frameSpeed = 1;
		character.setGlobalColliderTrigger(false);
		if (stunAnim != null) stunAnim.destroySelf();
		if (freezeAnim != null) freezeAnim.destroySelf();
		if (character.player.isCamPlayer) {
			//Global.level.snapCamPos(character.getCamCenterPos());
		}
		if (character.isCrystalized) {
			character.crystalizeEnd();
			Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (byte)RPCToggleType.StopCrystalize);
		}
		character.incPos(new Point(0, 30));

		base.onExit(newState);
	}

	public void setHiding(bool isHiding) {
		if (character.sprite.name.Contains("ra_show")) return;
		if (this.isHiding == isHiding) return;
		this.isHiding = isHiding;
		if (isHiding) {
			character.changeSpriteFromName("ra_hide", true);
		} else {
			character.changeSpriteFromName("ra_show", true);
		}
	}

	public void freeze(float freezeTime) {
		if (freezeTime > frozenTime) {
			frozenTime = freezeTime;
		}
	}

	public void stun(float timeToStun) {
		if (timeToStun > stunTime) {
			stunTime = timeToStun;
		}
	}

	public void crystalize(float timeToCrystalize) {
		if (timeToCrystalize > crystalizeTime) {
			crystalizeTime = timeToCrystalize;
			character.crystalizeStart();
			Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (byte)RPCToggleType.StartCrystalize);
		}
	}

	public void checkCrystalizeTime() {
		if (crystalizeTime <= 0) {
			character.crystalizeInvulnTime = 2;
			crystalizeTime = 0;
			character.crystalizeEnd();
			Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (byte)RPCToggleType.StopCrystalize);
		}
	}
}
