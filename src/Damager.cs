using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Damager {
	public Player owner;
	public float damage;
	public float hitCooldown;
	public int flinch; // Number of frames to flinch
	public float knockback;

	public const float envKillDamage = 2000;
	public const float switchKillDamage = 1000;
	public const float ohkoDamage = 500;
	public const float headshotModifier = 2;

	public static readonly Dictionary<int, float> projectileFlinchCooldowns = new Dictionary<int, float>() {
		{ (int)ProjIds.ElectricSpark, 1 },
		{ (int)ProjIds.TriadThunder, 2.25f },
		{ (int)ProjIds.TriadThunderBall, 2.25f },
		{ (int)ProjIds.TriadThunderBeam, 2.25f },
		{ (int)ProjIds.PlasmaGun2, 1 },
		{ (int)ProjIds.VoltTornado, 1 },
		{ (int)ProjIds.TornadoCharged, 1 },
		//{ (int)ProjIds.KKnuckle, 1 },
		{ (int)ProjIds.PZeroPunch2, 1 },
		{ (int)ProjIds.PZeroSenpuukyaku, 1 },
		{ (int)ProjIds.PZeroAirKick, 1 },
		{ (int)ProjIds.MechPunch, 1 },
		{ (int)ProjIds.MechKangarooPunch, 1 },
		{ (int)ProjIds.MechGoliathPunch, 1 },
		{ (int)ProjIds.MechDevilBearPunch, 1 },
		{ (int)ProjIds.MechStomp, 1 },
		{ (int)ProjIds.MechChain, 1 },
		{ (int)ProjIds.TunnelFangCharged, 1 },
		{ (int)ProjIds.Headbutt, 1 },
		{ (int)ProjIds.RocketPunch, 1 },
		{ (int)ProjIds.InfinityGig, 1 },
		{ (int)ProjIds.SpoiledBrat, 1 },
		{ (int)ProjIds.SpinningBladeCharged, 1 },
		{ (int)ProjIds.Shingetsurin, 1 },
		{ (int)ProjIds.MagnetMineCharged, 1 },
		{ (int)ProjIds.Sigma2ViralBeam, 1 },
		{ (int)ProjIds.Sigma2HopperDrill, 0.9f },
		{ (int)ProjIds.WSpongeChainSpin, 1 },
		{ (int)ProjIds.MorphMCSpin, 1 },
		{ (int)ProjIds.BCrabClaw, 1 },
		{ (int)ProjIds.SpeedBurnerCharged, 0.5f },
		{ (int)ProjIds.VelGMelee, 1f },
		{ (int)ProjIds.OverdriveOMelee, 1f },
		{ (int)ProjIds.WheelGSpinWheel, 1f },
		{ (int)ProjIds.Sigma3KaiserStomp, 1f },
		{ (int)ProjIds.Sigma3KaiserBeam, 1f },
		{ (int)ProjIds.UPPunch, 1f },
		{ (int)ProjIds.CopyShot, 1f },
		{ (int)ProjIds.NeonTClawAir, 1f },
		{ (int)ProjIds.NeonTClawDash, 1f },
		{ (int)ProjIds.VoltCTriadThunder, 1f },
		{ (int)ProjIds.Rekkoha, 1f },
		{ (int)ProjIds.HexaInvolute, 1f },
		{ (int)ProjIds.ZSaber3, 1f }
	};

	public Damager(Player owner, float damage, int flinch, float hitCooldown, float knockback = 0) {
		this.owner = owner;
		this.damage = damage;
		this.flinch = flinch;
		this.hitCooldown = hitCooldown;
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
			if (chr.isCCImmune()) {
				newFlinch = 0;
				weakness = false;
			}

			if (chr.player.isAxl && newFlinch > 0) {
				if (newFlinch < 4) {
					newFlinch = 4;
				} else if (newFlinch < 12) {
					newFlinch = 12;
				} else if (newFlinch < 26) {
					newFlinch = 26;
				} else {
					newFlinch = 36;
				}
			}
		}

		return applyDamage(
			owner, newDamage, hitCooldown, newFlinch, victim as Actor,
			weakness, weapon.index, weapon.killFeedIndex, actor, projId, sendRpc
		);
	}

	public static bool applyDamage(
		Player owner, float damage, float hitCooldown, int flinch,
		Actor victim, bool weakness, int weaponIndex, int weaponKillFeedIndex,
		Actor damagingActor, int projId, bool sendRpc = true
	) {
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
		if (projId == (int)ProjIds.BlastLauncherSplash) {
			key += "_" + damagingActor?.netId?.ToString();
		}

		IDamagable? damagable = victim as IDamagable;
		Character? character = victim as Character;
		CharState? charState = character?.charState;
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
					actorNetIdBytes = BitConverter.GetBytes(gmp.owner?.character?.netId ?? 0);
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

		if (owner.isDisguisedAxl && owner.character != null) {
			owner.character.disguiseCoverBlown = true;
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

		if (damagable != null && damagable is not CrackedWall && owner != null && owner.isMainPlayer && !isDot(projId)) {
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

		if (damagable != null && owner != null) {
			DamagerMessage? damagerMessage = null;

			var proj = damagingActor as Projectile;
			if (proj != null) {
				damagerMessage = proj.onDamage(damagable, owner);
				if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
				if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;
			}

			switch (projId) {
				case (int)ProjIds.CrystalHunter:
					character?.crystalize();
					break;
				case (int)ProjIds.CSnailCrystalHunter:
					character?.crystalize();
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
				case (int)ProjIds.ElectricShock:
				case (int)ProjIds.MK2StunShot:
				case (int)ProjIds.MorphMPowder:
					character?.paralize();
					break;
			}
			if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
			if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;

			if (projId == (int)ProjIds.CrystalHunter && weakness) {
				damage = 4;
				weakness = false;
				flinch = 0;
				victim?.playSound("hurt");
			}
			if (projId == (int)ProjIds.StrikeChain && weakness) {
				damage *= 2;
				weakness = false;
				flinch = 0;
				victim?.playSound("hurt");
			}
			if (projId == (int)ProjIds.Tornado && weakness) {
				damage = 1;
				weakness = true;
				flinch = Global.defFlinch;
			}
			if (projId == (int)ProjIds.AcidBurst && weakness) {
				damage = 1;
				weakness = true;
				flinch = Global.defFlinch;
			}
			if (projId == (int)ProjIds.GravityWell && weakness) {
				damage = 2;
				weakness = true;
				flinch = Global.defFlinch;
			}
			if (projId == (int)ProjIds.CSnailMelee && character != null && character.isCrystalized) {
				damage *= 2;
			}
		}

		// Character section
		bool spiked = false;
		bool playHurtSound = false;
		if (character != null) {
			MegamanX? mmx = character as MegamanX;

			bool isStompWeapon = (
				weaponKillFeedIndex == 19 ||
				weaponKillFeedIndex == 58 ||
				weaponKillFeedIndex == 51 ||
				weaponKillFeedIndex == 59 ||
				weaponKillFeedIndex == 60
			) && projId != (int)ProjIds.MechFrogStompShockwave;
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
				playHurtSound = true;
			}
			if (character.ownedByLocalPlayer && character.charState.superArmor) {
				flinch = 0;
			}
			if ((owner?.character as Zero)?.isViral == true) {
				character.addInfectedTime(owner, damage);
			}
			if ((owner?.character as PunchyZero)?.isViral == true) {
				character.addInfectedTime(owner, damage);
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
					if (character != owner?.character)
						character.addBurnTime(owner, new SpeedBurner(null), 1);
					break;
				case (int)ProjIds.SpeedBurner:
					character.addBurnTime(owner, new SpeedBurner(null), 1);
					break;
				case (int)ProjIds.Napalm2Wall:
				case (int)ProjIds.Napalm2:
					character.addBurnTime(owner, new Napalm(NapalmType.FireGrenade), 1);;
					break;
				case (int)ProjIds.Napalm2Flame:
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
					character.addBurnTime(owner, FlameStag.getUppercutWeapon(null), 2f);
					break;
				case (int)ProjIds.DrDopplerDash:
					character.addBurnTime(owner, new Weapon(WeaponIds.DrDopplerGeneric, 156), 1f);
					break;
				case (int)ProjIds.Sigma3Fire:
					character.addBurnTime(owner, new Sigma3FireWeapon(), 0.5f);
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
				case (int)ProjIds.PlasmaGun:
					if (mmx != null && mmx.player.hasBodyArmor(3)) {
						//The main shot fires an EMP burst that causes a full flinch and 
						//destroys Rolling Shields as well as temporarily disabling X3 barriers
						//He literally made an INFINITE DEACTIVATION
						//I am putting this to 3, as i suppose is what he meant to 
						//mmx.barrierCooldown = 3;
						mmx.barrierTime = 3;
						victim?.playSound("weakness");
					}
					break;	
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
					character.addDarkHoldTime(4, owner);
					break;
				case (int)ProjIds.MagnaCTail:
					character.addInfectedTime(owner, 4f);
					break;	
				case (int)ProjIds.MechPunch:
				case (int)ProjIds.MechDevilBearPunch:
					switch (Helpers.randomRange(0, 1)) {
						case 0:
							victim?.playSound("ridepunch");
							break;
						case 1:
							victim?.playSound("ridepunch2");
							break;
					}
					break;	
				case (int)ProjIds.MechKangarooPunch:
				case (int)ProjIds.MechGoliathPunch:
					victim?.playSound("ridepunchX3");
					break;	
			}
			switch (weaponIndex) {
				case (int)WeaponIds.Boomerang:
				case (int)WeaponIds.BoomerangKBoomerang:
					if (character.player.isX) 
						character.stingChargeTime = 0;
					break;
			}

			float flinchCooldown = 0;
			if (projectileFlinchCooldowns.ContainsKey(projId)) {
				flinchCooldown = projectileFlinchCooldowns[projId];
			}
			if (mmx != null) {
				if (mmx.checkMaverickWeakness((ProjIds)projId)) {
					weakness = true;
					if (flinch == 0 && flinchCooldown == 0) {
						flinchCooldown = 1;
					}
					flinch = Global.defFlinch;
					if (damage == 0) {
						damage = 1;
					}
				}
			}

			if (!character.charState.superArmor &&
				!character.isInvulnerable(true, true) &&
				!isDot(projId) && (
				owner?.character is Zero zero && zero.isBlack ||
				owner?.character is PunchyZero pzero && pzero.isBlack
			)) {
				if (flinch <= 0) {
					flinch = Global.halfFlinch;
					flinchCooldown = 1;
				} else if (flinch < Global.halfFlinch) {
					flinch = Global.halfFlinch;
				} else if (flinch < Global.defFlinch) {
					flinch = Global.defFlinch;
				}
				damage = MathF.Ceiling(damage * 1.5f);
			}
			// Disallow flinch stack for non-BZ.
			else if (!Global.canFlinchCombo) {
				if (character != null && character.charState is Hurt hurtState &&
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

			if (flinchCooldown > 0) {
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

			if (damage > 0) {
				bool isShotgunIceAndFrozen = character.sprite.name.Contains("frozen") && weaponKillFeedIndex == 8;
				if ((flinch > 0) && !isShotgunIceAndFrozen) {
					victim?.playSound("hurt");
					int hurtDir = -character.xDir;
					if (damagingActor != null && hitFromBehind(character, damagingActor, owner, projId)) {
						hurtDir *= -1;
					}
					if (projId == (int)ProjIds.GravityWellCharged) {
						hurtDir = 0;
					}
					character.setHurt(hurtDir, flinch, spiked);

					//if (weaponKillFeedIndex == 18) {
						//character.punchFlinchCooldown = Global.spf;
					//}
				} else {
					if (playHurtSound || weaponKillFeedIndex == 18 ||
						(
							projId == (int)ProjIds.BlackArrow && damage > 1
						) || ((
							projId == (int)ProjIds.SpiralMagnum ||
							projId == (int)ProjIds.SpiralMagnumScoped) && damage > 2
						) || ((
							projId == (int)ProjIds.AssassinBullet ||
							projId == (int)ProjIds.AssassinBulletQuick) && damage > 8)
						) {
							victim?.playSound("hurt");
					} else {
						victim?.playSound("hit");
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
			if (flinch > 0 && rideArmor.ownedByLocalPlayer && owner != null) {
				tempPush = 240f * (flinch / 26f);
			}
			// Apply push only if the new push is stronger than the current one.
			if (tempPush > System.Math.Abs(rideArmor.xFlinchPushVel)) {
				float pushDirection = -victim.xDir;
				if (owner != null && owner.character != null) {
					if (victim.pos.x > owner.character.pos.x) pushDirection = 1;
					if (victim.pos.x < owner.character.pos.x) pushDirection = -1;
				}
				rideArmor.xFlinchPushVel = pushDirection * tempPush;
			}
			if (damage > 1 || flinch > 0) {
				victim?.playSound("hurt");
				rideArmor.playHurtAnim();
			} else {
				victim?.playSound("hit");
			}
		}
		// Maverick section
		else if (maverick != null) {
			if (projId == (int)ProjIds.BeastKiller || projId == (int)ProjIds.AncientGun) {
				damage *= 2;
			}
			// Weakness system.
			weakness = maverick.checkWeakness(
				(WeaponIds)weaponIndex, (ProjIds)projId, out MaverickState newState, owner?.isSigma ?? true
			);
			if (weakness && damage < 2 && projId is (int)ProjIds.CrystalHunter or (int)ProjIds.CSnailCrystalHunter) {
				damage = 2;
			}
			if (weakness && damage < 1 && (
				projId == (int)ProjIds.AcidBurst ||
				projId == (int)ProjIds.TSeahorseAcid1 ||
				projId == (int)ProjIds.TSeahorseAcid2
			)) {
				damage = 1;
			}
			if (weakness && damage < 2 && projId == (int)ProjIds.ParasiticBomb) {
				damage = 2;
			}
			// Get flinch cooldown index.
			bool isOnFlinchCooldown = false;
			float flinchCooldownTime = 0;
			int flinchKey = getFlinchKeyFromProjId(projId);
			if (projectileFlinchCooldowns.ContainsKey(projId)) {
				flinchCooldownTime = projectileFlinchCooldowns[projId];
			}
			if (!maverick.player.isTagTeam() && flinchCooldownTime < 0.75f) {
				flinchCooldownTime = 0.75f;
			}
			if (!maverick.flinchCooldown.ContainsKey(flinchKey)) {
				maverick.flinchCooldown[flinchKey] = 0;
			}
			// Weakness states and flinch weakness.
			if (newState != null && maverick.weaknessCooldown == 0) {
				maverick.weaknessCooldown = 1.75f;
				flinch = 0;
				if (maverick.ownedByLocalPlayer) {
					maverick.changeState(newState, true);
				}
			}
			// Weakness flinch.
			else if (weakness && !isOnFlinchCooldown) {
				if (flinch <= 0) {
					flinchCooldownTime = 0.75f;
					flinch = Global.miniFlinch;
				}
				else if (flinch < Global.halfFlinch) {
					flinch = Global.halfFlinch;
				}
				else if (flinch < Global.defFlinch) {
					flinch = Global.defFlinch;
				}
				else if (flinch < Global.defFlinch) {
					flinch = Global.superFlinch;
				}
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
					if (!maverick.player.isTagTeam()) {
						flinch = 0;
					} 
					if (maverick.player.isTagTeam()) {
						// Large mavericks
						if (maverick.armorClass == Maverick.ArmorClass.Heavy) {
							/*if (flinch <= Global.miniFlinch) {
								flinch = 0;
							} else {
								flinch = Global.miniFlinch;
							} */
							flinch = 0;
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
				}
				if (maverick is ArmoredArmadillo aa) {
					if ((hitFromBehind(maverick, damagingActor, owner, projId) ||
						maverick.sprite.name == "armoreda_roll"
						) && !aa.hasNoArmor() && !isArmorPiercingOrElectric(projId)
					) {
						damage = MathF.Floor(damage * 0.5f);
						if (damage == 0) {
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
				if (maverick.sprite.name == "armoreda_block" && damage > 0 && !isArmorPiercingOrElectric(projId)) {
					if (hitFromFront(maverick, damagingActor, owner, projId)) {
						if (maverick.ownedByLocalPlayer && damage > 2 &&
							damagingActor is Projectile proj && proj.shouldVortexSuck && proj.destroyOnHit
						) {
							maverick.changeState(new ArmoredAGuardChargeState(damage * 2));
						}
						flinch = 0;
						damage = 0;
						maverick.playSound("m10ding");
						if (owner.ownedByLocalPlayer &&
							owner.character is Zero zero &&
							!zero.hypermodeActive()
						) {		 //What in the..
							if ( /*projId == (int)ProjIds.ZSaber */ 
								GenericMeleeProj.isZSaberClang(projId)
							) {
								owner.character.changeState(new ZeroClang(-owner.character.xDir));
							}
						}
					}
				}
			}
			if (damage > 0) {
				if (flinch > 0 && !isOnFlinchCooldown) {
					if (weakness) {
						victim?.playSound("weakness");
					} else {
						victim?.playSound("hurt");
					}
					if (newState == null && maverick.ownedByLocalPlayer) {
						int hurtDir = -maverick.xDir;
						if (!hitFromFront(maverick, damagingActor, owner, projId)) {
							hurtDir *= -1;
						}
						maverick.changeState(new MHurt(hurtDir, flinch), true);
					}
				} else {
					victim?.playSound("hit");
				}
			}
		}
		// Misc section
		else {
			if (damage > 0) {
				victim?.playSound("hit");
			}
		}

		if (damage > 0 && character?.isDarkHoldState != true) {
			victim?.addRenderEffect(RenderEffectType.Hit, 0.05f, 0.1f);
		}

		float finalDamage = damage * owner.getDamageModifier();

		if (finalDamage > 0 && character != null &&
			character.ownedByLocalPlayer && charState is XUPParryStartState parryState &&
			parryState.canParry() && !isDot(projId)
		) {
			parryState.counterAttack(owner, damagingActor, Math.Max(finalDamage * 2, 4));
			return true;
		}
		if (finalDamage > 0 && character != null &&
			character.ownedByLocalPlayer &&
			charState is SaberParryStartState parryState2
			&& parryState2.canParry(damagingActor) &&
			!isDot(projId)
		) {
			parryState2.counterAttack(owner, damagingActor, finalDamage);
			return true;
		}
		if ((damage > 0 || finalDamage > 0) && character != null &&
			character.ownedByLocalPlayer &&
			character.specialState == (int)SpecialStateIds.PZeroParry &&
			charState is PZeroParry	zeroParryState &&
			zeroParryState.canParry(damagingActor, projId)
		) {
			zeroParryState.counterAttack(owner, damagingActor);
			return true;
		}
		damagable?.applyDamage(finalDamage, owner, damagingActor, weaponKillFeedIndex, projId);

		return true;
	}

	public static bool isArmorPiercingOrElectric(int? projId) {
		return isArmorPiercing(projId) || isElectric(projId);
	}

	public static bool isArmorPiercing(int? projId) {
		if (projId == null) return false;
		return
			projId == (int)ProjIds.SpiralMagnum ||
			projId == (int)ProjIds.AssassinBullet ||
			projId == (int)ProjIds.AssassinBulletQuick ||
			projId == (int)ProjIds.VileMK2Grab ||
			projId == (int)ProjIds.UPGrab ||
			projId == (int)ProjIds.LaunchODrain ||
			projId == (int)ProjIds.DistanceNeedler ||
			projId == (int)ProjIds.Raijingeki ||
			projId == (int)ProjIds.Raijingeki2 ||
			projId == (int)ProjIds.CFlasher ||
			projId == (int)ProjIds.AcidBurstPoison ||
			projId == (int)ProjIds.MetteurCrash;
	}

	public static bool isDot(int? projId) {
		if (projId == null) return false;
		return
			projId == (int)ProjIds.AcidBurstPoison ||
			projId == (int)ProjIds.Burn;
	}

	public static bool isElectric(int? projId) {
		return
			projId == (int)ProjIds.ElectricSpark ||
			projId == (int)ProjIds.ElectricSparkCharged ||
			projId == (int)ProjIds.TriadThunder ||
			projId == (int)ProjIds.TriadThunderBall ||
			projId == (int)ProjIds.TriadThunderBeam ||
			projId == (int)ProjIds.TriadThunderCharged ||
			projId == (int)ProjIds.Raijingeki ||
			projId == (int)ProjIds.Raijingeki2 ||
			projId == (int)ProjIds.Denjin ||
			projId == (int)ProjIds.PeaceOutRoller ||
			projId == (int)ProjIds.PlasmaGun ||
			projId == (int)ProjIds.PlasmaGun2 ||
			projId == (int)ProjIds.PlasmaGun2Hyper ||
			projId == (int)ProjIds.VoltTornado ||
			projId == (int)ProjIds.VoltTornadoHyper ||
			projId == (int)ProjIds.SparkMSpark ||
			projId == (int)ProjIds.SigmaHandElecBeam ||
			projId == (int)ProjIds.Sigma2Ball ||
			projId == (int)ProjIds.Sigma2Ball2 ||
			projId == (int)ProjIds.WSpongeLightning;
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

	public static bool hitFromFront(Actor actor, Actor damager, Player? projOwner, int projId) {
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
			projId == (int)ProjIds.Napalm ||
			projId == (int)ProjIds.Napalm2Flame ||
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

	public static bool unassistable(int? projId) {
		return projId switch {
			(int)ProjIds.Burn => true,
			(int)ProjIds.Tornado => true,
			(int)ProjIds.VoltTornado => true,
			(int)ProjIds.VoltTornadoHyper => true,
			(int)ProjIds.FlameBurner => true,
			(int)ProjIds.FlameBurner2 => true,
			(int)ProjIds.FlameBurnerHyper => true,
			(int)ProjIds.BoomerangCharged => true,
			(int)ProjIds.Napalm2Flame => true,
			(int)ProjIds.Napalm2Wall => true,
			(int)ProjIds.TunnelFang => true,
			(int)ProjIds.TunnelFang2 => true,
			(int)ProjIds.GravityWell => true,
			(int)ProjIds.SpinWheel => true,
			(int)ProjIds.DistanceNeedler => true,
			(int)ProjIds.TriadThunder => true,
			(int)ProjIds.TriadThunderBeam => true,
			(int)ProjIds.RayGun2 => true,
			(int)ProjIds.Napalm => true,
			(int)ProjIds.CircleBlaze => true,
			(int)ProjIds.CircleBlazeExplosion => true,
			(int)ProjIds.BlastLauncher => true,
			(int)ProjIds.BlastLauncherSplash => true,
			(int)ProjIds.BoundBlaster2 => true,
			(int)ProjIds.NapalmSplashHit => true,
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
		return projId switch {
			(int)ProjIds.FireWave => true,
			(int)ProjIds.FireWaveCharged => true,
			(int)ProjIds.SpeedBurner => true,
			(int)ProjIds.SpeedBurnerCharged => true,
			(int)ProjIds.Napalm2 => true,
			(int)ProjIds.Napalm2Flame => true,
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
			(int)ProjIds.SonicSlicerChargedStart => true,
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
