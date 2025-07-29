using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Damager {
	public Player owner;
	public float damage;
	public float hitCooldownSeconds {
		set => hitCooldown = MathF.Ceiling(value * 60f);
		get => hitCooldown / 60f;
	}
	public float hitCooldown;
	public int flinch; // Number of frames to flinch
	public float knockback;

	public const float envKillDamage = 2000;
	public const float switchKillDamage = 1000;
	public const float ohkoDamage = 500;
	public const float forceKillDamage = 999;
	public const float headshotModifier = 2;

	public static readonly Dictionary<int, float> projectileFlinchCooldowns = new Dictionary<int, float>() {
		{ (int)ProjIds.ElectricSpark, 60 },
		{ (int)ProjIds.TriadThunder, 135 },
		{ (int)ProjIds.TriadThunderBall, 135 },
		{ (int)ProjIds.TriadThunderBeam, 135 },
		{ (int)ProjIds.PlasmaGunBeamProj, 60 },
		{ (int)ProjIds.VoltTornado, 60 },
		{ (int)ProjIds.TornadoCharged, 60 },
		//{ (int)ProjIds.KKnuckle, 60 },
		{ (int)ProjIds.PZeroPunch2, 60 },
		{ (int)ProjIds.PZeroSenpuukyaku, 60 },
		{ (int)ProjIds.PZeroAirKick, 60 },
		{ (int)ProjIds.MechPunch, 60 },
		{ (int)ProjIds.MechKangarooPunch, 60 },
		{ (int)ProjIds.MechGoliathPunch, 60 },
		{ (int)ProjIds.MechDevilBearPunch, 60 },
		{ (int)ProjIds.MechStomp, 60 },
		{ (int)ProjIds.MechChain, 60 },
		{ (int)ProjIds.TornadoFangCharged, 60 },
		{ (int)ProjIds.Headbutt, 60 },
		{ (int)ProjIds.RocketPunch, 60 },
		{ (int)ProjIds.InfinityGig, 60 },
		{ (int)ProjIds.SpoiledBrat, 60 },
		{ (int)ProjIds.SpinningBladeCharged, 60 },
		{ (int)ProjIds.Shingetsurin, 60 },
		{ (int)ProjIds.MagnetMineCharged, 60 },
		{ (int)ProjIds.Sigma2ViralBeam, 60 },
		{ (int)ProjIds.Sigma2HopperDrill, 54 },
		{ (int)ProjIds.WSpongeChainSpin, 60 },
		{ (int)ProjIds.MorphMCSpin, 60 },
		{ (int)ProjIds.BCrabClaw, 60 },
		{ (int)ProjIds.SpeedBurnerCharged, 30 },
		{ (int)ProjIds.VelGMelee, 60 },
		{ (int)ProjIds.OverdriveOMelee, 60 },
		{ (int)ProjIds.WheelGSpinWheel, 60 },
		{ (int)ProjIds.Sigma3KaiserStomp, 60 },
		{ (int)ProjIds.Sigma3KaiserBeam, 60 },
		{ (int)ProjIds.UPPunch, 60 },
		{ (int)ProjIds.CopyShot, 60 },
		{ (int)ProjIds.NeonTClawAir, 60 },
		{ (int)ProjIds.NeonTClawDash, 60 },
		{ (int)ProjIds.VoltCTriadThunder, 60 },
		{ (int)ProjIds.Rekkoha, 60 },
		{ (int)ProjIds.HexaInvolute, 60 },
		{ (int)ProjIds.ZSaber3, 60 },
	};

	public Damager(Player owner, float damage, int flinch, float hitCooldown, float knockback = 0) {
		this.owner = owner;
		this.damage = damage;
		this.flinch = flinch;
		this.hitCooldownSeconds = hitCooldown;
		this.knockback = knockback;
	}

	// Normally, sendRpc would default to false, but literally over 20 places need it to true
	// and one place needs it false so in this case, we invert the convention
	public bool applyDamage(
		IDamagable victim, bool weakness, Weapon weapon, Actor actor,
		int projId, float? overrideDamage = null, int? overrideFlinch = null, bool sendRpc = true
	) {
		if (weapon == null) return false;
		if (weapon is ItemTracer) return false;
		if (projId == (int)ProjIds.GravityWellCharged) return false;

		var newDamage = (overrideDamage != null ? (float)overrideDamage : damage);
		var newFlinch = (overrideFlinch != null ? (int)overrideFlinch : flinch);

		var chr = victim as Character;

		if (chr != null) {
			if (chr.isStatusImmune()) {
				newFlinch = 0;
				weakness = false;
			}

			if (chr.player.isAxl && newFlinch > 0) {
				if (newFlinch < Global.halfFlinch) {
					newFlinch = Global.halfFlinch;
				} else if (newFlinch < Global.defFlinch) {
					newFlinch = Global.defFlinch;
				} else {
					newFlinch = Global.superFlinch;
				}
			}
			// Tough Guy.
			if (chr.player.isSigma || chr.isToughGuyHyperMode()) {
				if (newFlinch >= Global.superFlinch) {
					newFlinch = Global.halfFlinch;
				} else if (newFlinch > Global.miniFlinch) {
					newFlinch = Global.miniFlinch;
				} else if (newFlinch <= Global.miniFlinch) {
					newFlinch = 0;
				}
			}
		}

		return applyDamage(
			owner, newDamage, hitCooldown, newFlinch, victim.actor(),
			weakness, weapon.index, weapon.killFeedIndex, actor, projId, sendRpc
		);
	}

	public static bool applyDamage(
		Player owner, float damage, float hitCooldown, int flinch,
		Actor victim, bool weakness, int weaponIndex, int weaponKillFeedIndex,
		Actor? damagingActor, int projId, bool sendRpc = true
	) {
		if (owner == null) {
			throw new Exception("Null damage player source. Use stage or self if not from another player.");
		}
		if (victim is Character chr && chr.invulnTime > 0) {
			return false;
		}
		if (projId == (int)ProjIds.TriadThunderQuake &&
			victim.ownedByLocalPlayer && isVictimImmuneToQuake(victim)
		) {
			return false;
		}
		if (damagingActor is GenericMeleeProj tgmp &&
			tgmp.owningActor is Character { isDarkHoldState: true }
		) {
			return false;
		}
		string key = projId.ToString() + "_" + owner.id.ToString();

		// Key adjustment overrides for more fine tuned balance cases
		if (projId == (int)ProjIds.Hyouretsuzan2) {
			key = ((int)ProjIds.Hyouretsuzan).ToString() + "_" + owner.id.ToString();
		}
		if (projId == (int)ProjIds.BlastLauncherGrenadeSplash) {
			key += "_" + damagingActor?.netId?.ToString();
		}

		IDamagable? damagable = victim as IDamagable;
		Character? preCharacter = victim as Character;
		CharState? charState = preCharacter?.charState;
		RideArmor? rideArmor = victim as RideArmor;
		Maverick? maverick = victim as Maverick;

		if (damagable == null) return false;
		if (!damagable.projectileCooldown.ContainsKey(key)) {
			damagable.projectileCooldown[key] = 0;
		}

		if (damagable.projectileCooldown[key] != 0) {
			return false;
		}

		damagable.projectileCooldown[key] = hitCooldown;

		// Run the RPC on all clients first, before it can modify the parameters, so clients can act accordingly
		if (sendRpc && victim.netId != null && Global.serverClient?.isLagging() == false) {
			byte[] damageBytes = BitConverter.GetBytes(damage);
			byte[] hitCooldownBytes = BitConverter.GetBytes(hitCooldown);
			byte[] victimNetIdBytes = BitConverter.GetBytes((ushort)victim.netId);
			byte[] actorNetIdBytes = BitConverter.GetBytes(damagingActor?.netId ?? 0);
			var projIdBytes = BitConverter.GetBytes(projId);
			byte linkedMeleeId = byte.MaxValue;

			if (damagingActor is GenericMeleeProj gmp &&
				(gmp.netId == null || gmp.netId == 0) &&
				gmp.meleeId != -1
			) {
				linkedMeleeId = (byte)gmp.meleeId;
				if (gmp.owningActor?.netId != null) {
					actorNetIdBytes = BitConverter.GetBytes(gmp.owningActor?.netId ?? 0);
				} else {
					actorNetIdBytes = BitConverter.GetBytes(gmp.owner.character?.netId ?? 0);
				}
			}
			var byteParams = new List<byte> {
				(byte)owner.id,
				damageBytes[0],
				damageBytes[1],
				damageBytes[2],
				damageBytes[3],
				hitCooldownBytes[0],
				hitCooldownBytes[1],
				hitCooldownBytes[2],
				hitCooldownBytes[3],
				(byte)flinch,
				victimNetIdBytes[0],
				victimNetIdBytes[1],
				weakness ? (byte)1 : (byte)0,
				(byte)weaponIndex,
				(byte)weaponKillFeedIndex,
				actorNetIdBytes[0],
				actorNetIdBytes[1],
				projIdBytes[0],
				projIdBytes[1],
				linkedMeleeId,
			};
			RPC.applyDamage.sendRpc(byteParams.ToArray());
		}

		if (owner.character != null && owner.character.isATrans) {
			owner.character.disguiseCoverBlown = true;
		}
		if (damagingActor is Projectile tempProj &&
			tempProj.owningActor is Character atChar &&
			atChar.isATrans
		) {
			atChar.disguiseCoverBlown = true;
		}

		if (damagable.isInvincible(owner, projId) && damage > 0) {
			victim.playSound("m10ding");
			if (Helpers.randomRange(0, 50) == 10) {
				victim.addDamageText("Bloqueo! Por 48 horas!", 1);
			}
			return true;
		}

		// Would only get reached due to lag. Otherwise, the owner that initiates the applyDamage call would have already considered it and avoided entering the method
		// This allows dodge abilities to "favor the defender"
		if (!damagable.canBeDamaged(owner.alliance, owner.id, projId)) {
			return true;
		}

		if (damagable != null && damagable is not CrackedWall && owner.isMainPlayer && !isDot(projId)) {
			owner.delaySubtank();
		}

		if (damagable is CrackedWall cw) {
			float? overrideDamage = CrackedWall.canDamageCrackedWall(projId, cw);
			if (overrideDamage != null && overrideDamage == 0 && damage > 0) {
				cw.playSound("m10ding");
				return true;
			}
			damage = overrideDamage ?? damage;
		}

		if (damagable != null) {
			DamagerMessage? damagerMessage = null;

			var proj = damagingActor as Projectile;
			if (proj != null) {
				damagerMessage = proj.onDamage(damagable, owner);
				if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
				if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;
			}

			switch (projId) {
				case (int)ProjIds.CrystalHunter:
					preCharacter?.crystalize();
					break;
				case (int)ProjIds.CSnailCrystalHunter:
					preCharacter?.crystalize();
					break;
				case (int)ProjIds.AcidBurst:
					damagerMessage = onAcidDamage(damagable, owner, 2);
					break;
				case (int)ProjIds.AcidBurstSmall:
					damagerMessage = onAcidDamage(damagable, owner, 1);
					break;
				case (int)ProjIds.AcidBurstCharged:
					damagerMessage = onAcidDamage(damagable, owner, 3);
					break;
				case (int)ProjIds.TSeahorseAcid3:
				case (int)ProjIds.TSeahorseAcid1:
					damagerMessage = onAcidDamage(damagable, owner, 2);
					break;
				case (int)ProjIds.TSeahorseAcid2:
					damagerMessage = onAcidDamage(damagable, owner, 2);
					break;
				/*
				case (int)ProjIds.TSeahorsePuddle:
					damagerMessage = onAcidDamage(damagable, owner, 1);
					break;
				case (int)ProjIds.TSeahorseEmerge:
					damagerMessage = onAcidDamage(damagable, owner, 2);
					break;
				*/
				case (int)ProjIds.ParasiticBomb:
					damagerMessage = onParasiticBombDamage(damagable, owner);
					break;
				case (int)ProjIds.SpreadShot:
				case (int)ProjIds.ElectricShock:
				case (int)ProjIds.MK2StunShot:
				case (int)ProjIds.MorphMPowder:
					preCharacter?.paralize();
					break;
			}
			if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
			if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;

			if (projId == (int)ProjIds.CrystalHunter && weakness) {
				damage = 2;
				weakness = false;
				flinch = 0;
				victim.playSound("weakness");
			}
			if (projId == (int)ProjIds.CSnailMelee && preCharacter != null && preCharacter.isCrystalized) {
				damage += 1;
			}
		}

		// Character section
		bool spiked = false;
		if (victim is Character character) {
			MegamanX? mmx = character as MegamanX;

			bool isStompWeapon = projId == (int)ProjIds.MechStomp;
			if (projId == (int)ProjIds.FlameMStomp || projId == (int)ProjIds.TBreaker ||
				projId == (int)ProjIds.SparkMStomp || projId == (int)ProjIds.WheelGStomp ||
				projId == (int)ProjIds.GBeetleStomp || projId == (int)ProjIds.TunnelRStomp ||
				projId == (int)ProjIds.Sigma3KaiserStomp || projId == (int)ProjIds.BBuffaloStomp
			) {
				isStompWeapon = true;
			}
			// Ride armor stomp
			if (isStompWeapon) {
				character.flattenedTime = 0.5f;
			}

			if (character.charState is SwordBlock || character.charState is SigmaBlock) {
				weakness = false;
			}

			if (character.isAlwaysHeadshot() && (projId == (int)ProjIds.RevolverBarrel || projId == (int)ProjIds.AncientGun)) {
				damage *= 1.5f;
			}
			if (character.ownedByLocalPlayer && character.isFlinchImmune()) {
				flinch = 0;
			}
			if ((owner.character as Zero)?.isViral == true) {
				character.addVirusTime(owner, damage);
			}
			if ((owner.character as PunchyZero)?.isViral == true) {
				character.addVirusTime(owner, damage);
			}

			switch (projId) {
				//burn [to the ground] section
				case (int)ProjIds.FireWave:
					character.addBurnTime(owner, new FireWave(), 0.5f);
					break;
				case (int)ProjIds.FireWaveCharged:
					character.addBurnTime(owner, new FireWave(), 2f);
					break;
				case (int)ProjIds.SpeedBurnerCharged:
					if (character != owner.character)
						character.addBurnTime(owner, SpeedBurner.netWeapon, 1);
					break;
				case (int)ProjIds.SpeedBurner:
					character.addBurnTime(owner, SpeedBurner.netWeapon, 1);
					break;
				case (int)ProjIds.FlameRoundWallProj:
				case (int)ProjIds.FlameRoundProj:
					character.addBurnTime(owner, new Napalm(NapalmType.FireGrenade), 1); ;
					break;
				case (int)ProjIds.FlameRoundFlameProj:
					character.addBurnTime(owner, new Napalm(NapalmType.FireGrenade), 0.5f);
					break;
				case (int)ProjIds.Ryuenjin:
					character.addBurnTime(owner, RyuenjinWeapon.staticWeapon, 2);
					break;
				case (int)ProjIds.FlameBurner:
					character.addBurnTime(owner, new FlameBurner(0), 0.5f);
					break;
				case (int)ProjIds.FlameBurnerHyper:
					character.addBurnTime(owner, new FlameBurner(0), 1);
					break;
				case (int)ProjIds.CircleBlazeExplosion:
					character.addBurnTime(owner, new FlameBurner(0), 2);
					break;
				case (int)ProjIds.QuakeBlazer:
					character.addBurnTime(owner, DanchienWeapon.staticWeapon, 0.5f);
					break;
				case (int)ProjIds.QuakeBlazerFlame:
					character.addBurnTime(owner, DanchienWeapon.staticWeapon, 0.5f);
					break;
				case (int)ProjIds.FlameMFireball:
					character.addBurnTime(owner, new FlameMFireballWeapon(), 1);
					break;
				case (int)ProjIds.FlameMOilFire:
					character.addBurnTime(owner, new FlameMOilFireWeapon(), 8);
					break;
				case (int)ProjIds.VelGFire:
					character.addBurnTime(owner, new VelGFireWeapon(), 0.5f);
					break;
				case (int)ProjIds.SigmaWolfHeadFlameProj:
					character.addBurnTime(owner, new WolfSigmaHeadWeapon(), 3);
					break;
				case (int)ProjIds.WildHorseKick:
					character.addBurnTime(owner, WildHorseKick.netWeapon, 0.5f);
					break;
				case (int)ProjIds.FStagFireball:
					character.addBurnTime(owner, FlameStag.getWeapon(), 1f);
					break;
				case (int)ProjIds.FStagDash:
					character.addBurnTime(owner, FlameStag.staticUppercutWeapon, 2f);
					break;
				case (int)ProjIds.DrDopplerDash:
					character.addBurnTime(owner, new Weapon(WeaponIds.DrDopplerGeneric, 156), 1f);
					break;
				case (int)ProjIds.Sigma3Fire:
					character.addBurnTime(owner, new Sigma3FireWeapon(), 1f);
					break;
				//Freeze effects	
				case (int)ProjIds.IceGattling:
					character.addIgFreezeProgress(1);
					break;
				case (int)ProjIds.IceGattlingHeadshot:
					character.addIgFreezeProgress(2);
					break;
				case (int)ProjIds.IceGattlingHyper:
					character.addIgFreezeProgress(2);
					break;
				case (int)ProjIds.Hyouretsuzan:
					character.addIgFreezeProgress(3);
					break;
				case (int)ProjIds.Hyouretsuzan2:
					character.addIgFreezeProgress(4);
					flinch = 0;
					break;
				case (int)ProjIds.VelGIce:
					character.addIgFreezeProgress(2, 2 * 60);
					break;
				case (int)ProjIds.BBuffaloBeam:
					character.addIgFreezeProgress(4);
					break;
				case (int)ProjIds.ShotgunIceCharged:
					character.addIgFreezeProgress(4, 5 * 60);
					break;
				case (int)ProjIds.ChillPIceBlow:
					character.addIgFreezeProgress(4);
					break;
				case (int)ProjIds.HyorogaProj:
					character.addIgFreezeProgress(1.5f);
					break;
				case (int)ProjIds.HyorogaSwing:
					character.addIgFreezeProgress(4);
					break;
				case (int)ProjIds.SeaDragonRage:
					character.addIgFreezeProgress(1);
					break;
				//Other effects
				case (int)ProjIds.SplashLaser:
					if (damagingActor != null) {
						character.splashLaserKnockback(damagingActor.deltaPos);
					}
					break;
				case (int)ProjIds.MechFrogStompShockwave:
				case (int)ProjIds.FlameMStompShockwave:
				case (int)ProjIds.TBreakerProj:
					if (character.grounded && character.ownedByLocalPlayer) {
						character.changeState(new KnockedDown(character.pos.x < damagingActor?.pos.x ? -1 : 1), true);
					}
					break;
				case (int)ProjIds.MechFrogGroundPound:
					if (!character.grounded) {
						character.vel.y += 300;
						spiked = true;
					}
					break;
				case (int)ProjIds.FlameMOil:
					character.addOilTime(owner, 8);
					character.playSound("flamemOil");
					break;
				case (int)ProjIds.DarkHold:
					character.addDarkHoldTime(DarkHoldState.totalStunTime, owner);
					break;
				case (int)ProjIds.MagnaCTail:
					character.addVirusTime(owner, 4f);
					break;
				case (int)ProjIds.MechPunch:
				case (int)ProjIds.MechDevilBearPunch:
					switch (Helpers.randomRange(0, 1)) {
						case 0:
							victim.playSound("ridepunch");
							break;
						case 1:
							victim.playSound("ridepunch2");
							break;
					}
					break;
				case (int)ProjIds.MechKangarooPunch:
				case (int)ProjIds.MechGoliathPunch:
					victim.playSound("ridepunchX3");
					break;
			}
			switch (weaponIndex) {
				case (int)WeaponIds.BoomerangCutter:
				case (int)WeaponIds.BoomerangKBoomerang:
					if (mmx != null) {
						mmx.stingActiveTime = 0;
					}
					break;
			}

			float flinchCooldown = 0;
			if (projectileFlinchCooldowns.ContainsKey(projId)) {
				flinchCooldown = projectileFlinchCooldowns[projId];
			}
			if (mmx != null) {
				if (XWeaknesses.checkMaverickWeakness(mmx.player, (ProjIds)projId)) {
					weakness = true;
					if (flinch <= 0 && flinchCooldown <= 0) {
						flinchCooldown = 60;
					}
					flinch = Global.defFlinch;
					if (damage == 0) {
						damage = 1;
					}
				}
				if (XWeaknesses.checkWeakness(mmx.player, (ProjIds)projId)) {
					weakness = true;
				}
			}

			if (!character.isFlinchImmune() &&
				!character.isInvulnerable(false, true) &&
				!isDot(projId) && (
				owner.character is Zero zero && zero.isBlack ||
				owner.character is PunchyZero pzero && pzero.isBlack
			)) {
				if (flinch <= 0) {
					flinch = Global.halfFlinch;
				} else if (flinch < Global.halfFlinch) {
					flinch = Global.halfFlinch;
				} else if (flinch < Global.defFlinch) {
					flinch = Global.defFlinch;
				} else {
					flinch = Global.superFlinch;
				}
				flinchCooldown = 60;
				damage = MathF.Ceiling(damage * 1.5f);
			}
			// Disallow flinch stack for non-BZ.
			else if (!Global.canFlinchCombo) {
				if (character.charState is Hurt hurtState &&
					hurtState.stateFrames < hurtState.flinchTime - 4
				) {
					flinchCooldown = 0;
				}
				if (maverick != null && maverick.state is MHurt mHurtState &&
					mHurtState.stateFrame < mHurtState.flinchTime - 4
				) {
					flinchCooldown = 0;
				}
			}

			if (flinchCooldown > 0 && flinch > 0) {
				int flinchKey = getFlinchKeyFromProjId(projId);
				if (!character.flinchCooldown.ContainsKey(flinchKey)) {
					character.flinchCooldown[flinchKey] = 0;
				}
				if (character.flinchCooldown[flinchKey] > 0) {
					flinch = 0;
				} else {
					character.flinchCooldown[flinchKey] = flinchCooldown;
				}
			}

			if ((character as Vile)?.isVileMK2 == true && damage > 0 && !isArmorPiercing(projId)) {
				if (hitFromBehind(character, damagingActor, owner, projId)) {
					damage--;

					if (damage < 1) {
						damage = 0;
						character.playSound("m10ding");
					}
				}
			}
			//Damage above 0
			if (damage > 0) {
				//bool if the character is frozen
				bool isShotgunIceAndFrozen = (
					character.sprite.name.Contains("frozen") == true && weaponKillFeedIndex == 8
				);
				int hurtDir = -character.xDir; //Hurt Direction
				if (damagingActor != null && hitFromBehind(character, damagingActor, owner, projId)) {
					hurtDir *= -1;
				}
				if (projId == (int)ProjIds.GravityWellCharged) {
					hurtDir = 0;
				}
				// Flinch above 0 and is not weakness
				if (flinch > 0 && !weakness) {
					character.playAltSound("hurt", altParams: "carmor");
					character.setHurt(hurtDir, flinch, spiked);
				}
				// Weakness is true and character is not frozen in Shotgun Ice.
				else if (weakness && !isShotgunIceAndFrozen && mmx?.weaknessCooldown <= 0) {
					victim.playSound("weakness");
					if (!character.isFlinchImmune()) {
						// Put a cooldown of 0.75s minimum.
						if (flinchCooldown * 60 < 45) {
							mmx.weaknessCooldown = 45;
						}
						// Set weakness cooldown to the same time as flinch cooldown.
						else {
							mmx.weaknessCooldown = MathF.Ceiling(flinchCooldown * 60);
						}
						if (flinch < Global.halfFlinch) {
							flinch = Global.halfFlinch;
						}
						else if (flinch < Global.defFlinch) {
							flinch = Global.defFlinch;
						}
						if (character.ownedByLocalPlayer) {
							character.setHurt(hurtDir, flinch, spiked);
						}
					}
				} else {			  
					if (character.altSoundId == Character.AltSoundIds.X1) {
						victim.playSound("hit");
					} else if (character.altSoundId == Character.AltSoundIds.X2) {
						victim.playSound("hitX2");
					} else if (character.altSoundId == Character.AltSoundIds.X3) {
						victim.playSound("hitX3");
					}
					if (mmx?.chestArmor == ArmorId.Light || mmx?.chestArmor == ArmorId.None) {
						victim.playSound("hit");
					} else if (mmx?.chestArmor == ArmorId.Giga) {
						victim.playSound("hitX2");
					} else if (mmx?.chestArmor == ArmorId.Max) {
						victim.playSound("hitX3");
					}
				}
			}
		}
		// Ride armor section
		else if (rideArmor != null) {
			// Beast Killer damage amp
			if (projId == (int)ProjIds.BeastKiller || projId == (int)ProjIds.AncientGun) {
				damage *= 2;
			}
			// Ride armor flinch push system.
			float tempPush = 0;
			if (flinch > 0 && rideArmor.ownedByLocalPlayer) {
				tempPush = 240f * (flinch / 26f);
			}
			// Apply push only if the new push is stronger than the current one.
			if (tempPush > System.Math.Abs(rideArmor.xFlinchPushVel)) {
				float pushDirection = -victim.xDir;
				if (owner.character != null) {
					if (victim.pos.x > owner.character.pos.x) pushDirection = 1;
					if (victim.pos.x < owner.character.pos.x) pushDirection = -1;
				}
				rideArmor.xFlinchPushVel = pushDirection * tempPush;
			}
			if (damage > 1 || flinch > 0) {
				victim.playSound("hurt");
				rideArmor.playHurtAnim();
			} else {
				victim.playSound("hit");
			}
		}
		// Maverick section
		else if (maverick != null) {
			if (projId == (int)ProjIds.BeastKiller || projId == (int)ProjIds.AncientGun) {
				damage *= 1.25f;
			}
			// Weakness system.
			weakness = maverick.checkWeakness(
				(WeaponIds)weaponIndex, (ProjIds)projId, out MaverickState? newState, owner.isSigma
			);
			if (weakness && damage < 1 && projId is (int)ProjIds.CrystalHunter or (int)ProjIds.CSnailCrystalHunter) {
				damage = 1;
			}
			if (weakness && damage < 1 && (
				projId == (int)ProjIds.AcidBurst ||
				projId == (int)ProjIds.TSeahorseAcid1 ||
				projId == (int)ProjIds.TSeahorseAcid2
			)) {
				damage = 1;
			}
			if (weakness && damage < 1 && projId == (int)ProjIds.ParasiticBomb) {
				damage = 1;
			}
			// Get flinch cooldown index.
			bool isOnFlinchCooldown = false;
			float flinchCooldownTime = 0;
			int flinchKey = getFlinchKeyFromProjId(projId);
			if (projectileFlinchCooldowns.ContainsKey(projId)) {
				flinchCooldownTime = projectileFlinchCooldowns[projId];
			}
			if (maverick.controlMode != MaverickModeId.TagTeam && flinchCooldownTime < 45 && !weakness) {
				flinchCooldownTime = 45;
			}
			if (!maverick.flinchCooldown.ContainsKey(flinchKey)) {
				maverick.flinchCooldown[flinchKey] = 0;
			}
			// Weakness flinch.
			if (weakness) {
				if (flinch <= 0) {
					flinchCooldownTime = 45;
					flinch = Global.miniFlinch;
				} else if (flinch < Global.halfFlinch) {
					flinch = Global.halfFlinch;
				} else if (flinch < Global.defFlinch) {
					flinch = Global.defFlinch;
				} else if (flinch < Global.defFlinch) {
					flinch = Global.superFlinch;
				}
			}
			// Weakness states and flinch weakness.
			if (newState != null && maverick.weaknessCooldown == 0) {
				maverick.weaknessCooldown = 60 * 3;
				flinch = 0;
				if (maverick.ownedByLocalPlayer) {
					maverick.changeState(newState, true);
				}
			} else {
				newState = null;
			}
			// Superarmor.
			if (maverick.state.superArmor) {
				flinch = 0;
			}
			// Set flinch cooldown if it exists.
			if (flinch > 0) {
				if (maverick.flinchCooldown[flinchKey] > 0) {
					isOnFlinchCooldown = true;
				} else {
					maverick.flinchCooldown[flinchKey] = flinchCooldownTime;
				}
			}
			// For backshield and similar stuff.
			if (!weakness) {
				// Flinch reduction.
				if (flinch > 0) {
					if (maverick.controlMode != MaverickModeId.TagTeam) {
						flinch = 0;
					}
					// Large mavericks
					if (maverick.armorClass == Maverick.ArmorClass.Heavy) {
						if (flinch <= Global.miniFlinch) {
							flinch = 0;
						} else {
							flinch = Global.miniFlinch;
						}
					}
					// Medium mavericks
					else if (maverick.armorClass == Maverick.ArmorClass.Medium) {
						if (flinch <= Global.miniFlinch) {
							flinch = 0;
						} else if (flinch <= Global.halfFlinch) {
							flinch = Global.miniFlinch;
						} else {
							flinch = Global.halfFlinch;
						}
					}
				}
				if (maverick is ArmoredArmadillo aa) {
					if ((hitFromBehind(maverick, damagingActor, owner, projId) ||
						maverick.sprite.name == "armoreda_roll"
						) && !aa.hasNoArmor() && !isArmorPiercing(projId)
					) {
						damage = MathF.Floor(damage * 0.5f);
						if (damage == 0) {
							flinch = 0;
							maverick.playSound("m10ding");
						}
					}
				}
				/*
				if (maverick is CrystalSnail cs) {
					if ((hitFromBehind(maverick, damagingActor, owner)) && !cs.noShell) {
						damage = 0;
						maverick.playSound("m10ding");
					}
				}
				*/
				if (maverick.sprite.name == "armoreda_block" && damage > 0 && !isArmorPiercing(projId)) {
					if (hitFromFront(maverick, damagingActor, owner, projId)) {
						if (maverick.ownedByLocalPlayer && damage > 2 &&
							damagingActor is Projectile proj && proj.shouldVortexSuck && proj.destroyOnHit
						) {
							maverick.changeState(new ArmoredAGuardChargeState(damage * 2));
						}
						flinch = 0;
						damage = 0;
						maverick.playSound("m10ding");
						if (owner.ownedByLocalPlayer == true &&
							owner.character is Zero zero &&
							!zero.hypermodeActive()
						) {
							//What in the..
							if (damagingActor is Projectile proj1 && proj1.isZSaberClang) {
								owner.character.changeState(new ZeroClang(-owner.character.xDir));
							}
						}
					}
				}
			}
			if (damage > 0) {
				if (flinch > 0 && !isOnFlinchCooldown) {
					if (weakness) {
						victim.playSound("weakness");
					} else {
						if (maverick.gameMavs == Maverick.GameMavs.X1) {
							victim.playSound("hurt");
						} else if (maverick.gameMavs == Maverick.GameMavs.X2) {
							victim.playSound("hurtX2");
						} else if (maverick.gameMavs == Maverick.GameMavs.X3) {
							victim.playSound("hurtX3");
						}
					}
					if (newState == null && maverick.ownedByLocalPlayer) {
						int hurtDir = -maverick.xDir;
						if (!hitFromFront(maverick, damagingActor, owner, projId)) {
							hurtDir *= -1;
						}
						maverick.changeState(new MHurt(hurtDir, flinch), true);
					}
				} else {
					if (maverick.gameMavs == Maverick.GameMavs.X1) {
						victim.playSound("hit");
					} else if (maverick.gameMavs == Maverick.GameMavs.X2) {
						victim.playSound("hitX2");
					} else if (maverick.gameMavs == Maverick.GameMavs.X3) {
						victim.playSound("hitx3");
					}
				}
			}
		}
		// Misc section
		else {
			if (damage > 0) {
				victim.playSound("hit");
			}
		}

		if (damage > 0 && preCharacter?.isDarkHoldState != true) {
			victim.addRenderEffect(RenderEffectType.Hit, 3, 6);
		}

		float finalDamage = damage;
		if (weakness && damage > 0) {
			damage += 1;
		}
		finalDamage *= owner.getDamageModifier();

		if (finalDamage > 0 && preCharacter != null &&
			preCharacter.ownedByLocalPlayer && charState is XUPParryStartState parryState &&
			parryState.canParry() && !isDot(projId)
		) {
			parryState.counterAttack(owner, damagingActor, Math.Max(finalDamage * 2, 4));
			return true;
		}
		if (finalDamage > 0 && preCharacter != null &&
			preCharacter.ownedByLocalPlayer &&
			charState is SaberParryStartState parryState2
			&& parryState2.canParry(damagingActor) &&
			!isDot(projId)
		) {
			parryState2.counterAttack(owner, damagingActor, finalDamage);
			return true;
		}
		if ((damage > 0 || finalDamage > 0) && preCharacter != null &&
			preCharacter.ownedByLocalPlayer &&
			preCharacter.charState.specialId == SpecialStateIds.PZeroParry &&
			charState is PZeroParry zeroParryState &&
			zeroParryState.canParry(damagingActor, projId)
		) {
			zeroParryState.counterAttack(owner, damagingActor);
			return true;
		}
		damagable?.applyDamage(finalDamage, owner, damagingActor, weaponKillFeedIndex, projId);

		return true;
	}

	public static bool isArmorPiercing(int? projId) {
		if (projId == null) return false;
		return projId switch {
			(int)ProjIds.PlasmaGunProj => true,
			(int)ProjIds.SpiralMagnum => true,
			(int)ProjIds.AssassinBullet => true,
			(int)ProjIds.AssassinBulletQuick => true,
			(int)ProjIds.VileMK2Grab => true,
			(int)ProjIds.UPGrab => true,
			(int)ProjIds.LaunchODrain => true,
			(int)ProjIds.DistanceNeedler => true,
			(int)ProjIds.Raijingeki => true,
			(int)ProjIds.Raijingeki2 => true,
			(int)ProjIds.CFlasher => true,
			(int)ProjIds.MetteurCrash => true,
			(int)ProjIds.AcidBurstPoison => true,
			_ => false
		};
	}

	public static bool isDot(int? projId) {
		if (projId == null) return false;
		return projId switch {
			(int)ProjIds.AcidBurstPoison => true,
			(int)ProjIds.Burn => true,
			_ => false
		};
	}
	public static bool isElectric(int? projId) {
		return projId switch {
			(int)ProjIds.ElectricSpark => true,
			(int)ProjIds.ElectricSparkCharged => true,
			(int)ProjIds.TriadThunder => true,
			(int)ProjIds.TriadThunderBall => true,
			(int)ProjIds.TriadThunderBeam => true,
			(int)ProjIds.TriadThunderCharged => true,
			(int)ProjIds.Raijingeki => true,
			(int)ProjIds.Raijingeki2 => true,
			(int)ProjIds.Denjin => true,
			(int)ProjIds.PeaceOutRoller => true,
			(int)ProjIds.PlasmaGunProj => true,
			(int)ProjIds.PlasmaGunBeamProj => true,
			(int)ProjIds.PlasmaGunBeamProjHyper => true,
			(int)ProjIds.VoltTornado => true,
			(int)ProjIds.VoltTornadoHyper => true,
			(int)ProjIds.SparkMSpark => true,
			(int)ProjIds.SigmaHandElecBeam => true,
			(int)ProjIds.Sigma2Ball => true,
			(int)ProjIds.Sigma2Ball2 => true,
			(int)ProjIds.WSpongeLightning => true,
			_ => false
		};
	}

	private static int getFlinchKeyFromProjId(int projId) {
		if (projId == (int)ProjIds.TriadThunder || projId == (int)ProjIds.TriadThunderBall || projId == (int)ProjIds.TriadThunderBeam) {
			projId = (int)ProjIds.TriadThunder;
		}
		return 1000 + projId;
	}

	public static bool hitFromBehind(Actor actor, Actor? damager, Player? projOwner, int projId) {
		return hitFromSub(
			actor, damager,
			projOwner, projId,
			delegate (Actor actor, float deltaX) {
				if (deltaX != 0 && (
					actor.xDir == -1 && deltaX < 0 ||
					actor.xDir == 1 && deltaX > 0
				)) {
					return true;
				}
				return false;
			},
			delegate (Actor actor, Point damagePos) {
				if (actor.pos.x == damagePos.x ||
					actor.pos.x < damagePos.x + 2 && actor.xDir == -1 ||
					actor.pos.x > damagePos.x - 2 && actor.xDir == 1
				) {
					return true;
				}
				return false;
			}
		);
	}

	public static bool hitFromFront(Actor actor, Actor? damager, Player? projOwner, int projId) {
		return hitFromSub(
			actor, damager,
			projOwner, projId,
			delegate (Actor actor, float deltaX) {
				if (deltaX != 0f && (
					actor.xDir == -1 && deltaX > 0f ||
					actor.xDir == 1 && deltaX < 0f
				)) {
					return true;
				}
				return false;
			},
			delegate (Actor actor, Point damagePos) {
				if (actor.pos.x == damagePos.x ||
					actor.pos.x > damagePos.x - 2 && actor.xDir == -1 ||
					actor.pos.x < damagePos.x + 2 && actor.xDir == 1
				) {
					return true;
				}
				return false;
			}
		);
	}

	private static bool hitFromSub(
		Actor actor, Actor? damager, Player? projOwner, int projId,
		Func<Actor, float, bool> checkDelta,
		Func<Actor, Point, bool> checkPos
	) {
		if (damager == null) {
			return false;
		}
		if (projId >= 0 && (
			projId == (int)ProjIds.Burn ||
			projId == (int)ProjIds.SelfDmg ||
			projId == (int)ProjIds.RumblingBangProj ||
			projId == (int)ProjIds.FlameRoundFlameProj ||
			projId == (int)ProjIds.MaroonedTomahawk ||
			projId == (int)ProjIds.AcidBurstPoison
		)) {
			return false;
		}
		if (damager is not Projectile { isMelee: false }) {
			if (damager.deltaPos.x != 0) {
				if (checkDelta(actor, damager.deltaPos.x)) {
					return true;
				} else {
					return false;
				}
			}
		}
		// Calculate based on other values if speed is 0.
		Point damagePos = damager.pos;

		if (damager is Projectile proj) {
			if (proj.isMelee || proj.isOwnerLinked) {
				if (proj.owningActor != null) {
					damagePos = proj.owningActor.pos;
				} else if (projOwner?.character != null) {
					damagePos = projOwner.character.pos;
				}
			}
		}

		// Call function if pos is not null.
		return checkPos(actor, damagePos);
	}

	private static bool isVictimImmuneToQuake(Actor victim) {
		if (victim is CrackedWall) return false;
		if (!victim.grounded) return true;
		if (victim is Character chr && chr.charState is WallSlide) return true;
		return false;
	}

	public static DamagerMessage? onAcidDamage(IDamagable damagable, Player attacker, float acidTime) {
		(damagable as Character)?.addAcidTime(attacker, acidTime);
		return null;
	}

	// Count for kills and assist even if it does 0 damage.
	public static bool alwaysAssist(int? projId) {
		if (projId == null) {
			return false;
		}
		return (ProjIds)projId switch {
			ProjIds.AcidBurst => true,
			ProjIds.AcidBurstCharged => true,
			ProjIds.CrystalHunter => true,
			ProjIds.ElectricShock => true,
			//ProjIds.AirBlastProj => true,
			_ => false
		};
	}

	public static bool unassistable(int? projId) {
		if (projId == null) {
			return false;
		}
		if (Global.level.server?.customMatchSettings?.assistable == false) {
			return false;		
		}
		// Never assist in any mode as they are DOT or self-damage. (Also Volt Tornado)
		bool alwaysNotAssist = (ProjIds)projId switch {
			ProjIds.Burn => true,
			ProjIds.AcidBurstPoison => true,
			ProjIds.SelfDmg => true,
			ProjIds.FlameRoundFlameProj => true,
			ProjIds.BoundBlasterRadar => true, 
			ProjIds.RayGunChargeBeam => true,
			ProjIds.PlasmaGunBeamProj => true,
			ProjIds.PlasmaGunBeamProjHyper => true,
			ProjIds.VoltTornado => true,
			ProjIds.VoltTornadoHyper => true,
			ProjIds.FlameBurner => true,
			ProjIds.FlameBurnerHyper => true,
			_ => false
		};
		if (alwaysNotAssist) {
			return true;
		}
		// The GM19 list now only counts for FFA mode.
		if (Global.level.gameMode is not FFADeathMatch) {
			return false;
		}
		return projId switch {
			(int)ProjIds.Tornado => true,
			(int)ProjIds.BoomerangCharged => true,
			(int)ProjIds.TornadoFang => true,
			(int)ProjIds.TornadoFang2 => true,
			(int)ProjIds.GravityWell => true,
			(int)ProjIds.SpinWheel => true,
			(int)ProjIds.TriadThunder => true,
			(int)ProjIds.TriadThunderBeam => true,
			(int)ProjIds.DistanceNeedler => true,
			(int)ProjIds.RumblingBangProj => true,
			(int)ProjIds.FlameRoundWallProj => true,
			(int)ProjIds.SplashHitProj => true,
			(int)ProjIds.CircleBlaze => true,
			(int)ProjIds.CircleBlazeExplosion => true,
			(int)ProjIds.BlastLauncherGrenadeSplash => true,
			(int)ProjIds.BlastLauncherMineGrenadeProj => true, 
			_ => false
		};
	}

	public static DamagerMessage? onParasiticBombDamage(IDamagable damagable, Player attacker) {
		var chr = damagable as Character;
		if (chr != null && chr.ownedByLocalPlayer && !chr.hasParasite) {
			chr.addParasite(attacker);
			chr.playSound("parasiteBombLatch", sendRpc: true);
		}

		return null;
	}


	public static bool canDamageFrostShield(int projId) {
		if (CrackedWall.canDamageCrackedWall(projId, null) != 0) {
			return true;
		}
		if (Global.level.server.customMatchSettings?.frostShieldNerf != false) {
			return true;
		}
		return projId switch {
			(int)ProjIds.FireWave => true,
			(int)ProjIds.FireWaveCharged => true,
			(int)ProjIds.SpeedBurner => true,
			(int)ProjIds.SpeedBurnerCharged => true,
			(int)ProjIds.FlameRoundProj => true,
			(int)ProjIds.FlameRoundFlameProj => true,
			(int)ProjIds.Ryuenjin => true,
			(int)ProjIds.FlameBurner => true,
			(int)ProjIds.FlameBurnerHyper => true,
			(int)ProjIds.CircleBlazeExplosion => true,
			(int)ProjIds.QuakeBlazer => true,
			(int)ProjIds.QuakeBlazerFlame => true,
			(int)ProjIds.FlameMFireball => true,
			(int)ProjIds.FlameMOilFire => true,
			(int)ProjIds.VelGFire => true,
			(int)ProjIds.SigmaWolfHeadFlameProj => true,
			(int)ProjIds.WildHorseKick => true,
			(int)ProjIds.Sigma3Fire => true,
			(int)ProjIds.FStagDashCharge => true,
			(int)ProjIds.FStagDash => true,
			(int)ProjIds.FStagFireball => true,
			_ => false
		};
	}

	public static bool isBoomerang(int? projId) {
		if (projId == null) return false;
		return projId switch {
			(int)ProjIds.Boomerang => true,
			(int)ProjIds.BoomerangCharged => true,
			(int)ProjIds.BoomerangKBoomerang => true,
			_ => false
		};
	}

	public static bool isSonicSlicer(int? projId) {
		if (projId == null) return false;
		return projId switch {
			(int)ProjIds.SonicSlicer => true,
			(int)ProjIds.SonicSlicerCharged => true,
			(int)ProjIds.SonicSlicerStart => true,
			(int)ProjIds.OverdriveOSonicSlicer => true,
			(int)ProjIds.OverdriveOSonicSlicerUp => true,
			_ => false
		};
	}
}
public class DamagerMessage {
	public int? flinch;
	public float? damage;
}
