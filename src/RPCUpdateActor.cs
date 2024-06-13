using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;

namespace MMXOnline;

public partial class Actor {
	public void sendActorNetData() {
		byte[] networkIdBytes = Helpers.convertToBytes((ushort)netId);
		if ((netId == 10 || netId == 11) && this is not Flag) {
			//string msg = string.Format(
			//"NetId {0} was not flag. Was {1}", netId.Value.ToString(), this.GetType().ToString()
			//);
			//Logger.logException(new Exception(msg), false);
			return;
		}

		var args = new List<byte>() { networkIdBytes[0], networkIdBytes[1] };

		ushort spriteIndex = Global.spriteIndexByName.GetValueOrCreate(sprite.name, ushort.MaxValue);

		List<bool> mask = new List<bool>();
		for (int i = 0; i < 8; i++) mask.Add(false);

		// These masks are for whether to send the following fields or not.
		mask[0] = !isStatic;                    // pos x
		mask[1] = !isStatic;                    // pos y
		mask[2] = syncScale;                    // scale data
		mask[3] = (sprite.frames.Count > 1);    // frame index data
		mask[4] = byteAngle != null;                // angle

		// The rest are just always sent and contain actual bool data
		mask[5] = visible;                      // visibility
		mask[6] = xDir == -1 ? false : true;    // xDir
		mask[7] = yDir == -1 ? false : true;    // yDir

		// add the mask
		byte maskByte = Convert.ToByte(string.Join("", mask.Select(b => b ? 1 : 0)), 2);
		args.Add(maskByte);

		// add pos x
		if (mask[0]) {
			byte[] xBytes = BitConverter.GetBytes(pos.x);
			args.AddRange(xBytes);
		}
		// add pos y
		if (mask[1]) {
			byte[] yBytes = BitConverter.GetBytes(pos.y);
			args.AddRange(yBytes);
		}
		// add the scale bytes
		if (mask[2]) {
			args.Add((byte)(int)(xScale * 20));
			args.Add((byte)(int)(yScale * 20));
		}
		// add the frame index byte
		if (mask[3]) {
			args.Add((byte)frameIndex);
		}
		// add angle
		if (mask[4]) {
			byte[] angleBytes = BitConverter.GetBytes((float)byteAngle);
			args.AddRange(angleBytes);
		}

		// Sprite index (always sent)
		byte[] spriteBytes = BitConverter.GetBytes((ushort)spriteIndex);
		args.AddRange(spriteBytes);

		// Add character stuff
		if (this is Character character) {
			// This gets parsed in RPCUpdateActor
			bool[] charMask = new bool[8];
			charMask[0] = character.acidTime > 0;
			charMask[1] = character.burnTime > 0;
			charMask[2] = character.chargeTime > 0;
			charMask[3] = character.igFreezeProgress > 0;
			charMask[4] = character.oilTime > 0;
			charMask[5] = character.infectedTime > 0;
			charMask[6] = character.vaccineTime > 0;
			charMask[7] = false;
			byte charMaskByte = Helpers.boolArrayToByte(charMask);

			int weaponIndex = character.player.weapon.index;
			if (weaponIndex == (int)WeaponIds.HyperBuster) {
				weaponIndex = character.player.weapons[character.player.hyperChargeSlot].index;
			}
			int health = MathInt.Ceiling(character.player.health);
			int maxHealth = MathInt.Ceiling(character.player.maxHealth);
			if (character.player.currentMaverick != null) {
				health = MathInt.Ceiling(character.player.currentMaverick.health);
				maxHealth = MathInt.Ceiling(character.player.currentMaverick.maxHealth);
			}

			int ammo = character.player.weapon == null ? 0 : (int)character.player.weapon.ammo;
			int charIndex = character.player.charNum;
			int alliance = character.player.alliance;
			if (character.rideArmor != null) ammo = MathInt.Ceiling(character.rideArmor.health);

			args.Add(charMaskByte);
			args.Add((byte)weaponIndex);
			args.Add((byte)health);
			args.Add((byte)maxHealth);
			args.Add((byte)ammo);
			args.Add((byte)charIndex);
			args.Add((byte)alliance);
			args.Add(character.updateAndGetNetCharState1());  // Packs bool states (BS's) into one byte
			args.Add(character.updateAndGetNetCharState2());
			args.Add((byte)character.player.currency);

			if (character is MegamanX mmx) {
				byte[] armorBytes = BitConverter.GetBytes(character.player.armorFlag);
				args.AddRange(armorBytes);
			}
			if (character is Zero zero) {
				ammo = MathInt.Ceiling(zero.gigaAttack.ammo);
			}
			if (character is PunchyZero pzero) {
				ammo = MathInt.Ceiling(pzero.gigaAttack.ammo);
			}
			if (character is Vile vile) {
				ammo = (int)character.player.vileAmmo;
				args.Add((byte)vile.cannonAimNum);
			}
			if (character is Axl axl) {
				byte axlArmAngle = Helpers.angleToByte(axl.netArmAngle);
				args.Add(axlArmAngle);
				byte[] netAxlArmSpriteIndexBytes = BitConverter.GetBytes(
					Global.spriteIndexByName.GetValueOrCreate(axl.getAxlArmSpriteName(), ushort.MaxValue)
				);
				args.AddRange(netAxlArmSpriteIndexBytes);
				args.Add((byte)character.player.axlBulletType);
			}
			if (character is BaseSigma sigma) {
				ammo = (int)character.player.sigmaAmmo;
			}
			// Status effects.
			if (charMask[0]) {
				args.Add((byte)(int)(character.acidTime * 20));
			}
			if (charMask[1]) {
				args.Add((byte)(int)(character.burnTime * 20));
			}
			if (charMask[2]) {
				args.Add((byte)(int)(character.chargeTime * 20));
			}
			if (charMask[3]) {
				args.Add((byte)(int)(character.igFreezeProgress * 20));
			}
			// We don't have room for more individual status flags. So just cram all the rarest statuses in the 7th flag
			// Gacel: What the actual hell GM19. We have room.
			if (charMask[5]) {
				args.Add((byte)(int)(character.oilTime * 20));
			}
			if (charMask[6]) {
				args.Add((byte)(int)(character.infectedTime * 20));
			}
			if (charMask[7]) {
				args.Add((byte)(int)(character.vaccineTime * 20));
			}

			byte[] raNetIdBytes = BitConverter.GetBytes(character.rideArmor?.netId ?? 0);
			args.AddRange(raNetIdBytes);

			if (Global.level.supportsRideChasers) {
				byte[] rcNetIdBytes = BitConverter.GetBytes(character.rideChaser?.netId ?? 0);
				args.AddRange(rcNetIdBytes);
			}
		} else if (this is Maverick maverick) {
			args.Add((byte)(int)(maverick.alpha * 100));
			args.Add((byte)MathF.Ceiling(maverick.health));
			args.Add((byte)(int)(maverick.invulnTime * 20));

			if (this is StingChameleon sc) {
				args.Add((byte)(sc.isInvisible ? 1 : 0));
			} else if (this is WireSponge ws) {
				byte[] chargeTime = BitConverter.GetBytes(ws.chargeTime);
				args.AddRange(chargeTime);
			} else if (this is WheelGator wg) {
				args.Add((byte)(int)(wg.damageEaten * 10));
			} else if (this is MorphMothCocoon mmc) {
				args.Add((byte)mmc.scrapAbsorbed);
				byte[] xBytes = BitConverter.GetBytes(mmc.latchPos.x);
				args.AddRange(xBytes);
				byte[] yBytes = BitConverter.GetBytes(mmc.latchPos.y);
				args.AddRange(yBytes);
			} else if (this is MagnaCentipede ms) {
				args.Add((byte)(ms.reversedGravity ? 1 : 0));
			}
		} else if (this is RideArmor ra) {
			args.Add((byte)ra.raNum);
			args.Add((byte)(ra.character != null ? 1 : 0)); // 1 means riding it, 0 means not
			args.Add((byte)(ra.neutralId));
			args.Add((byte)MathF.Ceiling(ra.health));
		} else if (this is RideChaser rc) {
			args.Add((byte)rc.drawState);
			args.Add((byte)(rc.character != null ? 1 : 0)); // 1 means riding it, 0 means not
			args.Add((byte)(rc.neutralId));
			args.Add((byte)MathF.Ceiling(rc.health));
		} else if (this is Flag flag) {
			var chrNetIdBytes = BitConverter.GetBytes(flag.chr?.netId ?? 0);
			args.AddRange(chrNetIdBytes);
			args.Add(flag.hasUpdraft() ? (byte)1 : (byte)0);
			args.Add(flag.pickedUpOnce ? (byte)1 : (byte)0);
			args.Add((byte)(int)flag.timeDropped);
		} else if (this is WSpongeSideChainProj sideChain) {
			byte[] xBytes = BitConverter.GetBytes(sideChain.netOrigin.x);
			args.AddRange(xBytes);
			byte[] yBytes = BitConverter.GetBytes(sideChain.netOrigin.y);
			args.AddRange(yBytes);
		} else if (this is WSpongeUpChainProj upChain) {
			byte[] xBytes = BitConverter.GetBytes(upChain.netOrigin.x);
			args.AddRange(xBytes);
			byte[] yBytes = BitConverter.GetBytes(upChain.netOrigin.y);
			args.AddRange(yBytes);
		} else if (this is BBuffaloBeamProj bbBeamProj) {
			byte[] xBytes = BitConverter.GetBytes(bbBeamProj.startPos.x);
			args.AddRange(xBytes);
			byte[] yBytes = BitConverter.GetBytes(bbBeamProj.startPos.y);
			args.AddRange(yBytes);
		} else if (this is MorphMBeamProj beamProj) {
			byte[] xBytes = BitConverter.GetBytes(beamProj.endPos.x);
			args.AddRange(xBytes);
			byte[] yBytes = BitConverter.GetBytes(beamProj.endPos.y);
			args.AddRange(yBytes);
		} else if (this is ViralSigmaBeamProj vsBeamProj) {
			byte[] botY = BitConverter.GetBytes(vsBeamProj.bottomY);
			args.AddRange(botY);
		} else if (this is KaiserSigmaBeamProj ksBeamProj) {
			byte[] beamAngle = BitConverter.GetBytes(ksBeamProj.beamAngle);
			byte[] beamWidth = BitConverter.GetBytes(ksBeamProj.beamWidth);
			args.AddRange(beamAngle);
			args.AddRange(beamWidth);
		} else if (this is GBeetleGravityWellProj wellProj) {
			args.Add((byte)wellProj.state);
			byte[] radiusFactor = BitConverter.GetBytes(wellProj.radiusFactor);
			args.AddRange(radiusFactor);
			byte[] maxRadius = BitConverter.GetBytes(wellProj.maxRadius);
			args.AddRange(maxRadius);

		} else if (this is VoltCSuckProj voltcSuckProj) {
			byte[] voltCatfishNetId = BitConverter.GetBytes(voltcSuckProj.vc?.netId ?? 0);
			args.AddRange(voltCatfishNetId);
		} else if (this is BHornetCursorProj cursorProj) {
			byte[] targetNetId = BitConverter.GetBytes(cursorProj.target?.netId ?? 0);
			args.AddRange(targetNetId);
		} else if (this is BHornetBeeProj beeProj) {
			byte[] targetNetId = BitConverter.GetBytes(beeProj.latchTarget?.netId ?? 0);
			args.AddRange(targetNetId);
		} else if (this is HexaInvoluteProj hiProj) {
			byte[] hiAng = BitConverter.GetBytes(hiProj.ang);
			args.AddRange(hiAng);
		}
		/*
		else if (this is ShotgunIceProjSled sips)
		{
			byte ridden = sips.ridden ? (byte)1 : (byte)0;
			args.Add(ridden);
		}
		*/

		Global.serverClient?.rpc(RPC.updateActor, args.ToArray());

		lastPos = pos;
		lastSpriteIndex = spriteIndex;
		lastFrameIndex = frameIndex;
		lastXDir = xDir;
		lastYDir = yDir;
		lastAngle = byteAngle;
	}
}

public class RPCUpdateActor : RPC {
	public RPCUpdateActor() {
		netDeliveryMethod = NetDeliveryMethod.Unreliable;
	}

	public override void invoke(params byte[] arguments) {
		if (Global.level == null || !Global.level.started) return;

		int i = 0;

		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[i], arguments[i + 1] }, 0);
		byte mask = arguments[i + 2];

		i += 3;

		var maskBools = Convert.ToString(mask, 2).Select(s => s.Equals('1')).ToList();
		while (maskBools.Count < 8) {
			maskBools.Insert(0, false);
		}

		float? xPos = null;
		float? yPos = null;
		float? xScale = null;
		float? yScale = null;
		int? spriteIndex = null;
		int? frameIndex = null;
		bool visible = maskBools[5];
		int? xDir = maskBools[6] ? 1 : -1;
		int? yDir = maskBools[7] ? 1 : -1;
		float? byteAngle = null;

		if (maskBools[0]) {
			xPos = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
			i += 4;
		}
		if (maskBools[1]) {
			yPos = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
			i += 4;
		}
		if (maskBools[2]) {
			xScale = arguments[i++] / 20f;
			yScale = arguments[i++] / 20f;
		}
		if (maskBools[3]) {
			frameIndex = arguments[i];
			i++;
		}
		if (maskBools[4]) {
			byteAngle = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
			i += 4;
		}

		spriteIndex = BitConverter.ToUInt16(new byte[] { arguments[i], arguments[i + 1] }, 0);
		i += 2;

		Actor actor = Global.level.getActorByNetId(netId);
		try {
			if (actor != null && !actor.ownedByLocalPlayer) {
				// In case we are updating a local object.
				actor.forceNetUpdateNextFrame = true;
				// Update data.
				if (spriteIndex != null) actor.netSpriteIndex = (int)spriteIndex;
				if (xPos != null) actor.netXPos = (float)xPos;
				if (yPos != null) actor.netYPos = (float)yPos;
				//actor.netIncPos = actor.netPos.subtract(actor.pos).times(0.33f);
				if (frameIndex != null) actor.netFrameIndex = (int)frameIndex;
				if (xDir != null) actor.netXDir = (int)xDir;
				if (yDir != null) actor.netYDir = (int)yDir;
				if (byteAngle != null) actor.netAngle = (float)byteAngle;
				if (xScale != null) actor.xScale = xScale.Value;
				if (yScale != null) actor.yScale = yScale.Value;

				actor.visible = visible;

				if (actor is Character character) {
					// Char mask section
					byte charMask = arguments[i++];
					bool[] charMaskBools = Helpers.byteToBoolArray(charMask);

					// universal section
					int weaponIndex = arguments[i++];
					int health = arguments[i++];
					int maxHealth = arguments[i++];
					int ammo = arguments[i++];
					int charIndex = arguments[i++];
					int alliance = arguments[i++];
					int netCharState1 = arguments[i++];
					int netCharState2 = arguments[i++];
					int currency = arguments[i++];

					character.player.changeWeaponFromWi(weaponIndex);
					character.player.health = health;
					character.player.maxHealth = maxHealth;
					character.player.charNum = charIndex;
					character.player.alliance = alliance;
					character.netCharState1 = (byte)netCharState1;
					character.netCharState2 = (byte)netCharState2;
					character.player.currency = currency;

					// X section.
					if (character.player.isX && character is MegamanX mmx) {
						character.player.weapon.ammo = ammo;
						byte armorByte = arguments[i++];
						byte armorByte2 = arguments[i++];

						character.player.armorFlag = BitConverter.ToUInt16(
							new byte[] { armorByte, armorByte2 }
						);
					}
					// Zero section.
					if (character.player.isZero && character is Zero zero) {
						zero.gigaAttack.ammo = ammo;
					}
					// Vile section.
					if (character.player.isVile && character is Vile vile) {
						character.player.vileAmmo = ammo;
						byte cannonByte = arguments[i++];
						vile.cannonAimNum = cannonByte;
					}
					// Axl section.
					if (character.player.isAxl && character is Axl axl) {
						int axlArmAngle = arguments[i++];
						axl.netArmAngle = axlArmAngle * 2;
						byte netAxlArmSpriteIndexByte = arguments[i++];
						byte netAxlArmSpriteIndexByte2 = arguments[i++];
						axl.netAxlArmSpriteIndex = BitConverter.ToUInt16(new byte[] {
							netAxlArmSpriteIndexByte, netAxlArmSpriteIndexByte2
						});
						int axlBulletType = arguments[i++];
						axl.player.axlBulletType = axlBulletType;
					}
					// Sigma section.
					if (character.player.isSigma && character is BaseSigma sigma) {
						character.player.sigmaAmmo = ammo;
					}

					// acid section
					if (charMaskBools[0]) {
						byte acidTime = arguments[i++];
						character.acidTime = acidTime / 20f;
					} else {
						character.acidTime = 0;
					}
					// burn section
					if (charMaskBools[1]) {
						byte burnTime = arguments[i++];
						character.burnTime = burnTime / 20f;
					} else {
						character.burnTime = 0;
					}
					// charge section
					if (charMaskBools[2]) {
						byte chargeTime = arguments[i++];
						character.chargeTime = chargeTime / 20f;
					} else {
						character.chargeTime = 0;
					}
					if (charMaskBools[3]) {
						byte igFreezeProgress = arguments[i++];
						character.igFreezeProgress = igFreezeProgress / 20f;
					} else {
						character.igFreezeProgress = 0;
					}
					// all other statuses section
					if (charMaskBools[4]) {
						byte oilTime = arguments[i++];
						character.oilTime = oilTime / 20f;
					} else {
						character.oilTime = 0;
					}
					if (charMaskBools[5]) {
						byte infectedTime = arguments[i++];
						character.infectedTime = infectedTime / 20f;
					} else {
						character.infectedTime = 0;
					}
					if (charMaskBools[6]) {
						byte vaccineTime = arguments[i++];
						character.vaccineTime = vaccineTime / 20f;
					} else {
						character.vaccineTime = 0;
					}

					// ride armor section
					byte raNetIdByte = arguments[i++];
					byte raNetIdByte2 = arguments[i++];

					ushort raNetId = BitConverter.ToUInt16(new byte[] { raNetIdByte, raNetIdByte2 });
					var rideArmor = Global.level.getActorByNetId(raNetId) as RideArmor;

					character.rideArmor = rideArmor;
					if (character.rideArmor != null) character.rideArmor.health = ammo;

					// ride chaser section
					if (Global.level.supportsRideChasers) {
						byte rcNetIdByte = arguments[i++];
						byte rcNetIdByte2 = arguments[i++];

						ushort rcNetId = BitConverter.ToUInt16(new byte[] { rcNetIdByte, rcNetIdByte2 });
						var rideChaser = Global.level.getActorByNetId(rcNetId) as RideChaser;

						character.rideChaser = rideChaser;
						if (character.rideChaser != null) {
							character.rideChaser.zIndex = character.zIndex - 10;
							character.rideChaser.health = ammo;
						}
					}
				} else if (actor is Maverick maverick) {
					byte alpha = arguments[i++];
					byte health = arguments[i++];
					byte invulnTime = arguments[i++];

					maverick.alpha = alpha / 100f;
					maverick.health = health;
					maverick.invulnTime = invulnTime / 20f;

					if (actor is StingChameleon sc) {
						byte isInvisible = arguments[i++];
						sc.isInvisible = (isInvisible == 1);
					} else if (actor is WireSponge ws) {
						float chargeTime = BitConverter.ToSingle(
							new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0
						);
						i += 4;
						ws.chargeTime = chargeTime;
					} else if (actor is WheelGator wg) {
						int damageEaten = arguments[i++];
						wg.damageEaten = damageEaten / 10f;
					} else if (actor is MorphMothCocoon mmc) {
						int scrapAbsorbed = arguments[i++];
						mmc.scrapAbsorbed = scrapAbsorbed;

						float latchPosX = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
						i += 4;
						float latchPosY = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
						i += 4;

						mmc.latchPos = new Point(latchPosX, latchPosY);
					} else if (actor is MagnaCentipede ms) {
						byte reversedGravityByte = arguments[i++];
						bool reversedGravity = (reversedGravityByte == 1);
						if (reversedGravity != ms.reversedGravity) {
							ms.reverseGravity();
						}
					}
				} else if (actor is RideArmor ra) {
					int raNum = arguments[i++];
					ra.setRaNum(raNum);

					int isOwnerRiding = arguments[i++];
					if (isOwnerRiding == 0) {
						ra.character = null;
					} else if (isOwnerRiding == 1) {
						ra.character = ra.netOwner?.character;
					}

					int neutralId = arguments[i++];
					ra.neutralId = neutralId;

					int health = arguments[i++];
					ra.health = health;

					ra.setColorShaders();
				} else if (actor is RideChaser rc) {
					int drawState = arguments[i++];
					rc.drawState = drawState;

					int isOwnerRiding = arguments[i++];
					if (isOwnerRiding == 0) {
						rc.character = null;
					} else if (isOwnerRiding == 1) {
						rc.character = rc.netOwner?.character;
					}

					int neutralId = arguments[i++];
					rc.neutralId = neutralId;

					int health = arguments[i++];
					rc.health = health;
				} else if (actor is Flag flag) {
					byte chrNetIdByte1 = arguments[i++];
					byte chrNetIdByte2 = arguments[i++];
					byte hasUpdraftByte = arguments[i++];
					byte pickedUpOnce = arguments[i++];
					int timeDropped = arguments[i++];

					ushort chrNetId = BitConverter.ToUInt16(new byte[] { chrNetIdByte1, chrNetIdByte2 });
					if (chrNetId != 0) {
						var chr = Global.level.getActorByNetId(chrNetId) as Character;
						if (chr != null && chr.flag == null) {
							chr.onFlagPickup(flag);
						}
					} else {
						foreach (var player in Global.level.players) {
							if (player?.character != null && player.character.flag == flag) {
								player.character.flag = null;
							}
						}
					}
					flag.nonOwnerHasUpdraft = hasUpdraftByte == 1;
					flag.pickedUpOnce = pickedUpOnce == 1;
					flag.timeDropped = timeDropped;
				} else if (actor is WSpongeSideChainProj sideChain) {
					float originX = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					float originY = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					sideChain.netOrigin = new Point(originX, originY);
				} else if (actor is WSpongeUpChainProj upChain) {
					float originX = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					float originY = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					upChain.netOrigin = new Point(originX, originY);
				} else if (actor is BBuffaloBeamProj bbBeamProj) {
					float x = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					float y = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					bbBeamProj.setStartPos(new Point(x, y));
				} else if (actor is MorphMBeamProj beamProj) {
					float x = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					float y = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					beamProj.setEndPos(new Point(x, y));
				} else if (actor is ViralSigmaBeamProj vsBeamProj) {
					float botY = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					vsBeamProj.bottomY = botY;
				} else if (actor is KaiserSigmaBeamProj ksBeamProj) {
					float beamAngle = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					float beamWidth = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					ksBeamProj.beamAngle = beamAngle;
					ksBeamProj.beamWidth = beamWidth;
				} else if (actor is GBeetleGravityWellProj wellProj) {
					int state = arguments[i++];
					wellProj.state = state;
					float radiusFactor = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					wellProj.radiusFactor = radiusFactor;
					float maxRadius = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					i += 4;
					wellProj.maxRadius = maxRadius;
				} else if (actor is VoltCSuckProj voltcSuckProj) {
					ushort voltCatfishNetId = BitConverter.ToUInt16(
						new byte[] { arguments[i], arguments[i + 1] }, 0
					);
					i += 2;
					voltcSuckProj.vc = Global.level.getActorByNetId(voltCatfishNetId) as VoltCatfish;
				} else if (actor is BHornetCursorProj cursorProj) {
					ushort targetNetId = BitConverter.ToUInt16(new byte[] { arguments[i], arguments[i + 1] }, 0);
					i += 2;
					cursorProj.target = Global.level.getActorByNetId(targetNetId);
				} else if (actor is BHornetBeeProj beeProj) {
					ushort targetNetId = BitConverter.ToUInt16(new byte[] { arguments[i], arguments[i + 1] }, 0);
					i += 2;
					beeProj.latchTarget = Global.level.getActorByNetId(targetNetId);
				} else if (actor is HexaInvoluteProj hiProj) {
					float hiAng = BitConverter.ToSingle(new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0);
					hiProj.ang = hiAng;
				}
				/*
				else if (actor is ShotgunIceProjSled sips)
				{
					bool ridden = arguments[i] == 1 ? true : false;
					i += 1;
					sips.ridden = ridden;
				}
				*/

				actor.lastNetUpdate = Global.time;
			}
		} catch (IndexOutOfRangeException) {
			string msg = string.Format("Index out of bounds. Actor type: {0}, args len: {1}, i: {2}, netId: {3}",
				actor.GetType().ToString(), arguments.Length.ToString(), i.ToString(), netId.ToString());
			throw new Exception(msg);
		}

		if (actor == null) {
			int? playerId = Player.getPlayerIdFromCharNetId(netId);
			if (playerId != null) {
				var player = Global.level.getPlayerById(playerId.Value);
				if (player != null) {
					Global.level.addFailedSpawn(playerId.Value, new Point(xPos ?? 0, yPos ?? 0), xDir ?? 1, netId);
				}
			}
		}
	}
}
