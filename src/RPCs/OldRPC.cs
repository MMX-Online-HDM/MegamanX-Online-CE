using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lidgren.Network;
using Newtonsoft.Json;

namespace MMXOnline;
// This file contains mostly legacy RPCs that exists only because GM19 code shanigans.
// Hopefully these should be removed in the future.

public class RPCSetHyperAxlTime : RPC {
	public RPCSetHyperAxlTime() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		int playerId = arguments[0];
		int time = arguments[1];
		int type = arguments[2];
		var player = Global.level.getPlayerById(playerId);
		if (player?.character is Axl axl) {
			if (type == 1) axl.whiteAxlTime = time;
		}

	}

	public void sendRpc(int playerId, float time, int type) {
		Global.serverClient?.rpc(this, (byte)playerId, (byte)(int)time, (byte)type);
	}
}

public class RPCAxlShoot : RPC {
	public RPCAxlShoot() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		int playerId = arguments[0];
		int projId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[3], arguments[4] }, 0);
		float x = BitConverter.ToSingle(new byte[] { arguments[5], arguments[6], arguments[7], arguments[8] }, 0);
		float y = BitConverter.ToSingle(new byte[] { arguments[9], arguments[10], arguments[11], arguments[12] }, 0);
		int xDir = Helpers.byteToDir(arguments[13]);
		float angle = Helpers.byteToDegree(arguments[14]);
		int axlBulletWeaponType = arguments.InRange(15) ? arguments[16] : 0;

		var player = Global.level.getPlayerById(playerId);
		if (player?.character == null) return;
		//this is just a mess

		switch (projId) {
			case (int)ProjIds.AxlBullet:
			case (int)ProjIds.MetteurCrash:
			case (int)ProjIds.BeastKiller:
			case (int)ProjIds.MachineBullets:
			case (int)ProjIds.RevolverBarrel:
			case (int)ProjIds.AncientGun: {
					var pos = new Point(x, y);
					var flash = new Anim(pos, "axl_pistol_flash", 1, null, true);
					flash.angle = angle;
					flash.frameSpeed = 1;
					if (projId == (int)ProjIds.AxlBullet) {
						new AxlBulletProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
						player.character.playSound("axlBullet");
					} else if (projId == (int)ProjIds.MetteurCrash) {
						//var bullet = new MettaurCrashProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
						player.character.playSound("mettaurCrash");
					} else if (projId == (int)ProjIds.BeastKiller) {
						/*var	bullet = new BeastKillerProj(new BeastKiller(), pos, player, Point.createFromAngle(angle - 45), player.getNextActorNetId(), sendRpc: true);
						var	bullet1 = new BeastKillerProj(new BeastKiller(), pos, player, Point.createFromAngle(angle - 22.5f), player.getNextActorNetId(), sendRpc: true);
						var	bullet2 = new BeastKillerProj(new BeastKiller(), pos, player, Point.createFromAngle(angle), player.getNextActorNetId(), sendRpc: true);
						var	bullet3 = new BeastKillerProj(new BeastKiller(), pos, player, Point.createFromAngle(angle + 22.5f), player.getNextActorNetId(), sendRpc: true);
						var	bullet4 = new BeastKillerProj(new BeastKiller(), pos, player, Point.createFromAngle(angle + 45), player.getNextActorNetId(), sendRpc: true);
						*/
						player.character.playSound("beastKiller");
					} else if (projId == (int)ProjIds.MachineBullets) {
						//var bullet = new MachineBulletProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
						player.character.playSound("machineBullets");
					} else if (projId == (int)ProjIds.RevolverBarrel) {
						//var bullet = new RevolverBarrelProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
						player.character.playSound("revolverBarrel");
					} else if (projId == (int)ProjIds.AncientGun) {
						//var bullet = new AncientGunProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
						player.character.playSound("ancientGun3");
					}

					break;
				}

			case (int)ProjIds.CopyShot: {
					var pos = new Point(x, y);
					var bullet = new CopyShotProj(new AxlBullet(), pos, 0, player, Point.createFromAngle(angle), netId);
					var flash = new Anim(pos, "axl_pistol_flash_charged", 1, null, true);
					flash.angle = angle;
					flash.frameSpeed = 3;
					player.character.playSound("axlBulletCharged");
					break;
				}

			case (int)ProjIds.BlastLauncherGrenadeProj:
			case (int)ProjIds.BlastLauncherMineGrenadeProj: {
					var pos = new Point(x, y);
					var bullet = new GrenadeProj(
						new BlastLauncher(0), pos, xDir, player, Point.createFromAngle(angle), null, new Point(), 0, netId
					);
					var flash = new Anim(pos, "axl_pistol_flash", 1, null, true);
					flash.angle = angle;
					flash.frameSpeed = 1;
					player.character.playSound("grenadeShoot");
					break;
				}

			case (int)ProjIds.GreenSpinner: {
					var pos = new Point(x, y);
					var bullet = new GreenSpinnerProj(
						new BlastLauncher(0), pos, xDir, player, Point.createFromAngle(angle), null, netId
					);
					var flash = new Anim(pos, "axl_pistol_flash_charged", 1, null, true);
					flash.angle = angle;
					flash.frameSpeed = 3;
					player.character.playSound("rocketShoot");
					break;
				}

			case (int)ProjIds.RayGun:
			case (int)ProjIds.RayGunChargeBeam: {
					var pos = new Point(x, y);
					Point velDir = Point.createFromAngle(angle);
					if (projId == (int)ProjIds.RayGun) {
						var bullet = new RayGunProj(new RayGun(0), pos, xDir, player, velDir, netId);
						player.character.playSound("raygun");
					} else if (projId == (int)ProjIds.RayGunChargeBeam) {
						var bullet = Global.level.getActorByNetId(netId) as RayGunAltProj;
						if (bullet == null) {
							new RayGunAltProj(new RayGun(0), pos, pos, xDir, player, netId);
						}
					}

					string fs = "axl_raygun_flash";
					if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) fs = "axl_raygun_flash2";
					var flash = new Anim(pos, fs, 1, null, true);
					flash.setzIndex(player.character.zIndex - 100);
					flash.angle = angle;
					flash.frameSpeed = 1;
					break;
				}

			case (int)ProjIds.SpiralMagnum:
			case (int)ProjIds.SpiralMagnumScoped: {
					var pos = new Point(x, y);
					var bullet = new SpiralMagnumProj(
						new SpiralMagnum(0), pos, 0, 0, player, Point.createFromAngle(angle), null, null, netId
					);
					if (projId == (int)ProjIds.SpiralMagnumScoped) {
						AssassinBulletTrailAnim trail = new AssassinBulletTrailAnim(pos, bullet);
					}
					var flash = new Anim(pos, "axl_pistol_flash", 1, null, true);
					flash.angle = angle;
					flash.frameSpeed = 1;
					player.character.playSound("spiralMagnum");
					break;
				}

			case (int)ProjIds.IceGattling:
			case (int)ProjIds.IceGattlingHyper: {
					var pos = new Point(x, y);
					var bullet = new IceGattlingProj(new IceGattling(0), pos, xDir, player, Point.createFromAngle(angle), netId);
					var flash = new Anim(pos, "axl_pistol_flash", 1, null, true);
					flash.angle = angle;
					flash.frameSpeed = 1;
					player.character.playSound("iceGattling");
					break;
				}

			case (int)ProjIds.AssassinBullet:
			case (int)ProjIds.AssassinBulletQuick: {
					var pos = new Point(x, y);
					var bullet = new AssassinBulletProj(new AssassinBullet(), pos, new Point(), xDir, player, null, null, netId);
					AssassinBulletTrailAnim trail = new AssassinBulletTrailAnim(pos, bullet);
					var flash = new Anim(pos, "axl_pistol_flash_charged", 1, null, true);
					flash.angle = angle;
					flash.frameSpeed = 3;
					player.character.playSound("assassinate");
					break;
				}
		}
	}

	public void sendRpc(int playerId, int projId, ushort netId, Point pos, int xDir, float angle) {
		var xBytes = BitConverter.GetBytes(pos.x);
		var yBytes = BitConverter.GetBytes(pos.y);
		var netIdBytes = BitConverter.GetBytes(netId);
		var projIdBytes = BitConverter.GetBytes((ushort)projId);
		Global.serverClient?.rpc(
			this, (byte)playerId, projIdBytes[0], projIdBytes[1], netIdBytes[0], netIdBytes[1],
			xBytes[0], xBytes[1], xBytes[2], xBytes[3],
			yBytes[0], yBytes[1], yBytes[2], yBytes[3],
			Helpers.dirToByte(xDir), Helpers.degreeToByte(angle)
		);
	}
}


public class RPCSyncAxlBulletPos : RPC {
	public RPCSyncAxlBulletPos() {
		netDeliveryMethod = NetDeliveryMethod.Unreliable;
	}

	public override void invoke(params byte[] arguments) {
		int playerId = arguments[0];

		short xPos = BitConverter.ToInt16(new byte[] { arguments[1], arguments[2] }, 0);
		short yPos = BitConverter.ToInt16(new byte[] { arguments[3], arguments[4] }, 0);

		var player = Global.level.getPlayerById(playerId);
		Axl? axl = player?.character as Axl;
		if (axl == null) { return; }

		axl.nonOwnerAxlBulletPos = new Point(xPos, yPos);
	}

	public void sendRpc(int playerId, Point bulletPos) {
		byte[] xBytes = BitConverter.GetBytes((short)MathF.Round(bulletPos.x));
		byte[] yBytes = BitConverter.GetBytes((short)MathF.Round(bulletPos.y));
		Global.serverClient?.rpc(this, (byte)playerId, xBytes[0], xBytes[1], yBytes[0], yBytes[1]);
	}
}

public class RPCSyncAxlScopePos : RPC {
	public RPCSyncAxlScopePos() {
		netDeliveryMethod = NetDeliveryMethod.Unreliable;
	}

	public override void invoke(params byte[] arguments) {
		int playerId = arguments[0];

		var player = Global.level.getPlayerById(playerId);
		Axl? axl = player?.character as Axl;
		if (axl == null) {
			return;
		}
		bool isZooming = arguments[1] == 1 ? true : false;

		axl.isNonOwnerZoom = isZooming;

		short sxPos = BitConverter.ToInt16(new byte[] { arguments[2], arguments[3] }, 0);
		short syPos = BitConverter.ToInt16(new byte[] { arguments[4], arguments[5] }, 0);

		short exPos = BitConverter.ToInt16(new byte[] { arguments[6], arguments[7] }, 0);
		short eyPos = BitConverter.ToInt16(new byte[] { arguments[8], arguments[9] }, 0);

		axl.nonOwnerScopeStartPos = new Point(sxPos, syPos);
		axl.netNonOwnerScopeEndPos = new Point(exPos, eyPos);
	}

	public void sendRpc(int playerId, bool isZooming, Point startScopePos, Point endScopePos) {
		byte[] sxBytes = BitConverter.GetBytes((short)MathF.Round(startScopePos.x));
		byte[] syBytes = BitConverter.GetBytes((short)MathF.Round(startScopePos.y));

		byte[] exBytes = BitConverter.GetBytes((short)MathF.Round(endScopePos.x));
		byte[] eyBytes = BitConverter.GetBytes((short)MathF.Round(endScopePos.y));

		Global.serverClient?.rpc(this, (byte)playerId, isZooming ? (byte)1 : (byte)0, sxBytes[0], sxBytes[1], syBytes[0], syBytes[1], exBytes[0], exBytes[1], eyBytes[0], eyBytes[1]);
	}
}

public class RPCBoundBlasterStick : RPC {
	public RPCBoundBlasterStick() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		ushort beaconNetId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
		ushort stuckActorNetId = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);

		short xPos = BitConverter.ToInt16(new byte[] { arguments[4], arguments[5] }, 0);
		short yPos = BitConverter.ToInt16(new byte[] { arguments[6], arguments[7] }, 0);

		BoundBlasterAltProj? beaconActor = Global.level.getActorByNetId(beaconNetId) as BoundBlasterAltProj;
		Actor? stuckActor = Global.level.getActorByNetId(stuckActorNetId);

		if (beaconActor == null || stuckActor == null) {
			return;
		}
		beaconActor.isActorStuck = true;
		beaconActor.stuckActor = stuckActor;
		beaconActor.stopSyncingNetPos = true;
		beaconActor.changePos(new Point(xPos, yPos));
	}

	public void sendRpc(ushort? beaconNetId, ushort? stuckActorNetId, Point hitPos) {
		if (beaconNetId == null) return;
		if (stuckActorNetId == null) return;

		byte[] beaconNetIdBytes = BitConverter.GetBytes(beaconNetId.Value);
		byte[] stuckActorNetIdBytes = BitConverter.GetBytes(stuckActorNetId.Value);

		byte[] sxBytes = BitConverter.GetBytes((short)MathF.Round(hitPos.x));
		byte[] syBytes = BitConverter.GetBytes((short)MathF.Round(hitPos.y));

		Global.serverClient?.rpc(this, beaconNetIdBytes[0], beaconNetIdBytes[1], stuckActorNetIdBytes[0], stuckActorNetIdBytes[1],
			sxBytes[0], sxBytes[1], syBytes[0], syBytes[1]);
	}
}

public class RPCBroadcastLoadout : RPC {
	public RPCBroadcastLoadout() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		LoadoutData loadout = Helpers.deserialize<LoadoutData>(arguments);
		var player = Global.level?.getPlayerById(loadout.playerId);
		if (player == null) return;

		player.loadout = loadout;
		player.loadoutSet = true;
	}

	public void sendRpc(Player player) {
		byte[] loadoutBytes = Helpers.serialize(player.loadout);
		Global.serverClient?.rpc(this, loadoutBytes);
	}
}

public class RPCLogWeaponKills : RPC {
	public RPCLogWeaponKills() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
		isServerMessage = true;
	}

	public void sendRpc() {
		Global.serverClient?.rpc(logWeaponKills);
	}
}


public class RPCFeedWheelGator : RPC {
	public RPCFeedWheelGator() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
		float damage = BitConverter.ToSingle(new byte[] { arguments[2], arguments[3], arguments[4], arguments[5] }, 0);

		var wheelGator = Global.level.getActorByNetId(netId) as WheelGator;
		if (wheelGator != null) {
			wheelGator.feedWheelGator(damage);
		}
	}

	public void sendRpc(WheelGator wheelGator, float damage) {
		if (wheelGator?.netId == null || Global.serverClient == null) return;

		byte[] netIdBytes = BitConverter.GetBytes(wheelGator.netId.Value);
		byte[] damageBytes = BitConverter.GetBytes(damage);

		Global.serverClient.rpc(RPC.feedWheelGator, netIdBytes[0], netIdBytes[1],
			damageBytes[0], damageBytes[1], damageBytes[2], damageBytes[3]);
	}
}

public class RPCHealDoppler : RPC {
	public RPCHealDoppler() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
		float damage = BitConverter.ToSingle(new byte[] { arguments[2], arguments[3], arguments[4], arguments[5] }, 0);
		int attackerPlayerId = arguments[6];

		var player = Global.level.getPlayerById(attackerPlayerId);
		var drDoppler = Global.level.getActorByNetId(netId) as DrDoppler;
		if (drDoppler != null) {
			drDoppler.healDrDoppler(player, damage);
		}
	}

	public void sendRpc(DrDoppler drDoppler, float damage, Player attacker) {
		if (drDoppler.netId == null || Global.serverClient == null) return;

		byte[] netIdBytes = BitConverter.GetBytes(drDoppler.netId.Value);
		byte[] damageBytes = BitConverter.GetBytes(damage);

		Global.serverClient.rpc(RPC.healDoppler, netIdBytes[0], netIdBytes[1],
			damageBytes[0], damageBytes[1], damageBytes[2], damageBytes[3], (byte)attacker.id);
	}
}

public class RPCDecShieldAmmo : RPC {
	public RPCDecShieldAmmo() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		int playerId = arguments[0];
		float decAmmoAmount = BitConverter.ToSingle(arguments.AsSpan()[1..5]);

		Player? player = Global.level.getPlayerById(playerId);

		(player?.character as MegamanX)?.chargedRollingShieldProj?.decAmmo(decAmmoAmount);
	}
}

