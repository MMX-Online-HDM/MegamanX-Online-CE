using System;
using Lidgren.Network;

namespace MMXOnline;

public partial class RPCCreateProj : RPC {
	public RPCCreateProj() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		// Data of the array.
		bool[] dataInf = Helpers.byteToBoolArray(arguments[0]);
		// Always present values.
		var playerId = arguments[1];
		ushort projId = BitConverter.ToUInt16(arguments[2..4], 0);
		float xPos = BitConverter.ToSingle(arguments[4..8], 0);
		float yPos = BitConverter.ToSingle(arguments[8..12], 0);
		var netProjByte = BitConverter.ToUInt16(arguments[12..14], 0);
		// Angle or Dir
		int xDir = 1;
		float angle = 0;
		float byteAngle = 0;
		if (!dataInf[0]) {
			xDir = arguments[14];
			xDir -= 1;
		} else {
			byteAngle = arguments[14];
			angle = byteAngle * 1.40625f;
		}
		// Extra arguments.
		int extraDataIndex = 15;
		byte[] extraData;
		if (dataInf[1] && arguments.Length >= extraDataIndex + 1) {
			extraData = arguments[extraDataIndex..];
		} else {
			extraData = new byte[0];
		}
		Point bulletDir = Point.createFromAngle(angle);

		var player = Global.level.getPlayerById(playerId);
		if (player == null) return;

		Point pos = new Point(xPos, yPos);

		if (functs.ContainsKey(projId)) {
			ProjParameters args = new() {
				projId = projId,
				pos = pos,
				xDir = xDir,
				player = player,
				netId = netProjByte,
				angle = angle,
				byteAngle = byteAngle,
				extraData = extraData
			};
			functs[projId](args);
			return;
		}
		Projectile? proj;

		switch (projId) {
			case (int)ProjIds.ItemTracer:
				proj = new ItemTracerProj(new ItemTracer(), pos, xDir, player, null, netProjByte);
				break;
			case (int)ProjIds.ZSaberProj:
				proj = new ZSaberProj(pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.XSaberProj:
				proj = new XSaberProj(pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.Buster3:
				proj = new Buster3Proj(new XBuster(), pos, xDir, extraData[0], player, netProjByte);
				break;
			case (int)ProjIds.BusterX3Proj2:
				proj = new BusterX3Proj2(new XBuster(), pos, xDir, extraData[0], player, netProjByte);
				break;
			case (int)ProjIds.BusterX3Plasma:
				proj = new BusterPlasmaProj(new XBuster(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.BusterX3PlasmaHit:
				proj = new BusterPlasmaHitProj(new XBuster(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.ZBuster:
				proj = new BusterProj(new ZeroBuster(), pos, xDir, 1, player, netProjByte);
				break;
			case (int)ProjIds.ZBuster2:
				proj = new ZBuster2Proj(pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.ZBuster3:
				proj = new ZBuster3Proj(pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.ZBuster4:
				proj = new ZBuster4Proj(pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.Sting:
			case (int)ProjIds.StingDiag:
				proj = new StingProj(new ChameleonSting(), pos, xDir, player, extraData[0], netProjByte);
				break;
			case (int)ProjIds.FireWaveCharged:
				proj = new FireWaveProjCharged(new FireWave(), pos, xDir, player, 0, netProjByte, 0);
				break;
			case (int)ProjIds.ElectricSpark:
				proj = new ElectricSparkProj(new ElectricSpark(), pos, xDir, player, extraData[0], netProjByte);
				break;
			case (int)ProjIds.ElectricSparkCharged:
				proj = new ElectricSparkProjCharged(new ElectricSpark(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.ShotgunIce:
				proj = new ShotgunIceProj(new ShotgunIce(), pos, xDir, player, 1, netProjByte);
				break;
			case (int)ProjIds.ShotgunIceCharged:
				proj = new ShotgunIceProjCharged(new ShotgunIce(), pos, xDir, player, 1, false, netProjByte);
				break;
			case (int)ProjIds.ChillPIceBlow:
				proj = new ShotgunIceProjCharged(new ShotgunIce(), pos, xDir, player, 1, true, netProjByte);
				break;
			case (int)ProjIds.Rakuhouha: {
				proj = new RakuhouhaProj(
					RakuhouhaWeapon.netWeapon, pos, false,
					byteAngle, player, netProjByte 
				);
				break;
			}
			case (int)ProjIds.CFlasher: {
				proj = new RakuhouhaProj(
					RakuhouhaWeapon.netWeapon, pos, true,
					byteAngle, player, netProjByte 
				);
				break;
			}
			case (int)ProjIds.Hadouken:
				proj = new HadoukenProj(new HadoukenWeapon(player), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.ElectricShock:
				proj = new StunShotProj(new VileMissile(VileMissileType.ElectricShock), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.MK2StunShot:
				proj = new VileMK2StunShotProj(new VileMK2StunShot(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.VileBomb:
				proj = new VileBombProj(new VileBall(VileBallType.ExplosiveRound), pos, xDir, player, 0, netProjByte);
				break;
			case (int)ProjIds.VileBombSplit:
				proj = new VileBombProj(new VileBall(VileBallType.ExplosiveRound), pos, xDir, player, 1, netProjByte);
				break;
			case (int)ProjIds.MechMissile:
				proj = new MechMissileProj(new MechMissileWeapon(player), pos, xDir, false, player, netProjByte);
				break;
			case (int)ProjIds.MechTorpedo:
				proj = new TorpedoProj(new MechTorpedoWeapon(player), pos, xDir, player, 2, netProjByte);
				break;
			case (int)ProjIds.MechChain:
				proj = new MechChainProj(new MechChainWeapon(player), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.MechBuster:
				proj = new MechBusterProj(new MechBusterWeapon(player), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.MechBuster2:
				proj = new MechBusterProj2(new MechBusterWeapon(player), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.BlastLauncherGrenadeSplash:
				proj = new GrenadeExplosionProj(new Weapon(), pos, xDir, player, 0, null, 0, netProjByte);
				break;
			case (int)ProjIds.GreenSpinnerSplash:
				proj = new GreenSpinnerExplosionProj(new Weapon(), pos, xDir, player, 0, null, 1, netProjByte);
				break;
			case (int)ProjIds.RumblingBangGrenade:
				proj = new NapalmGrenadeProj(new Weapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.RumblingBangProj:
				proj = new NapalmPartProj(new Weapon(), pos, xDir, player, netProjByte, extraData[0]);
				break;
			case (int)ProjIds.FlameRoundGrenade:
				proj = new MK2NapalmGrenadeProj(new Napalm(NapalmType.FireGrenade), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FlameRoundProj:
				proj = new MK2NapalmProj(new Napalm(NapalmType.FireGrenade), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FlameRoundWallProj:
				proj = new MK2NapalmWallProj(new Napalm(NapalmType.FireGrenade), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FlameRoundFlameProj:
				proj = new MK2NapalmFlame(new Napalm(NapalmType.FireGrenade), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.RocketPunch:
				proj = new RocketPunchProj(new RocketPunch(RocketPunchType.GoGetterRight), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.SpoiledBrat:
				proj = new RocketPunchProj(new RocketPunch(RocketPunchType.SpoiledBrat), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.InfinityGig:
				proj = new RocketPunchProj(new RocketPunch(RocketPunchType.InfinityGig), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.Vulcan:
				proj = new VulcanProj(new Vulcan(VulcanType.CherryBlast), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.DistanceNeedler:
				proj = new VulcanProj(new Vulcan(VulcanType.DistanceNeedler), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.BuckshotDance:
				proj = new VulcanProj(new Vulcan(VulcanType.BuckshotDance), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.SilkShotShrapnel:
				proj = new SilkShotProjShrapnel(
					SilkShot.netWeapon, pos, xDir, player, extraData[1], extraData[0], netProjByte
				);
				break;
			case (int)ProjIds.SpinWheelCharged:
				proj = new SpinWheelProjCharged(new SpinWheel(), pos, xDir, player, extraData[0], netProjByte);
				break;
			case (int)ProjIds.SonicSlicer:
				proj = new SonicSlicerProj(new SonicSlicer(), pos, xDir, extraData[0], player, netProjByte);
				break;
			/* case (int)ProjIds.StrikeChain:
				proj = new StrikeChainProj(
					new StrikeChain(), pos, xDir, arguments[extraDataIndex],
					arguments[extraDataIndex + 1] - 128, player, netProjByte
				); */
				break;
			case (int)ProjIds.SpeedBurnerTrail:
				proj = new SpeedBurnerProjGround(SpeedBurner.netWeapon, pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.GigaCrush:
				proj = new GigaCrushProj(new GigaCrush(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.Rekkoha:
				proj = new RekkohaProj(RekkohaWeapon.netWeapon, pos, player, netProjByte);
				break;
			case (int)ProjIds.AcidBurstSmall:
				proj = new AcidBurstProjSmall(new AcidBurst(), pos, xDir, new Point(), (ProjIds)projId, player, netProjByte);
				break;
			case (int)ProjIds.ParasiticBombCharged:
				proj = new ParasiticBombProjCharged(new ParasiticBomb(), pos, xDir, player, netProjByte, null);
				break;
			case (int)ProjIds.TriadThunderBeam:
				proj = new TriadThunderBeamPiece(new TriadThunder(), pos, xDir, 1, player, 0, netProjByte);
				break;
			case (int)ProjIds.SparkMSpark:
				proj = new TriadThunderProjCharged(new TriadThunder(), pos, xDir, 1, player, netProjByte);
				break;
			case (int)ProjIds.GravityWellCharged:
				proj = new GravityWellProjCharged(new GravityWell(), pos, xDir, 1, player, netProjByte);
				break;
			case (int)ProjIds.FrostShieldAir:
				proj = new FrostShieldProjAir(new FrostShield(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.FrostShieldGround:
				proj = new FrostShieldProjGround(new FrostShield(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FrostShieldChargedGrounded:
				proj = new FrostShieldProjChargedGround(new FrostShield(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FrostShieldChargedPlatform:
				proj = new FrostShieldProjPlatform(new FrostShield(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.TornadoFang or (int)ProjIds.TornadoFang2:
				proj = new TornadoFangProj(new TornadoFang(), pos, xDir, extraData[0], player, netProjByte);
				break;
			case (int)ProjIds.SplashLaser:
				proj = new SplashLaserProj(new RayGun(0), pos, player, bulletDir, netProjByte);
				break;
			case (int)ProjIds.BlackArrow:
				proj = new BlackArrowProj(new BlackArrow(0), pos, player, bulletDir, 0, netProjByte);
				break;
			case (int)ProjIds.WindCutter:
				proj = new WindCutterProj(new BlackArrow(0), pos, player, bulletDir, netProjByte);
				break;
			case (int)ProjIds.SniperMissile:
				proj = new SniperMissileProj(new SpiralMagnum(0), pos, player, bulletDir, netProjByte);
				break;
			case (int)ProjIds.SniperMissileBlast:
				proj = new SniperMissileExplosionProj(new SpiralMagnum(0), pos, xDir, 1, player, netProjByte);
				break;
			case (int)ProjIds.BoundBlaster:
				proj = new BoundBlasterProj(new BoundBlaster(0), pos, angle, player, netProjByte);
				break;
			case (int)ProjIds.BoundBlasterRadar:
				proj = new BoundBlasterAltProj(new BoundBlaster(0), pos, xDir, player, bulletDir, netProjByte);
				break;
			case (int)ProjIds.MovingWheel:
				proj = new MovingWheelProj(new BoundBlaster(0), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.PlasmaGunProj:
				proj = new PlasmaGunProj(new PlasmaGun(0), pos, xDir, player, bulletDir, netProjByte);
				break;
			case (int)ProjIds.PlasmaGunBeamProj or (int)ProjIds.PlasmaGunBeamProjHyper:
				proj = new PlasmaGunAltProj(new PlasmaGun(0), pos, pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.VoltTornado or (int)ProjIds.VoltTornadoHyper:
				proj = new VoltTornadoProj(new PlasmaGun(0), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.GaeaShield:
				proj = new GaeaShieldProj(new IceGattling(0), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FlameBurner or (int)ProjIds.FlameBurnerHyper:
				proj = new FlameBurnerProj(new FlameBurner(0), pos, xDir, player, bulletDir, netProjByte);
				break;
			case (int)ProjIds.AirBlastProj:
				proj = new FlameBurnerAltProj(new FlameBurner(0), pos, xDir, player, bulletDir, netProjByte);
				break;
			case (int)ProjIds.CircleBlaze:
				proj = new CircleBlazeProj(new FlameBurner(0), pos, xDir, player, bulletDir, netProjByte);
				break;
			case (int)ProjIds.CircleBlazeExplosion:
				proj = new CircleBlazeExplosionProj(new FlameBurner(0), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.QuakeBlazer:
				proj = new DanchienExplosionProj(pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.QuakeBlazerFlame:
				proj = new QuakeBlazerFlamePart(pos, xDir, (int)extraData[0] - 1, player, netProjByte);
				break;
			case (int)ProjIds.MechFrogStompShockwave:
				proj = new MechFrogStompShockwave(new MechFrogStompWeapon(null), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.SplashHitGrenade:
				proj = new SplashHitGrenadeProj(new Napalm(NapalmType.SplashHit), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.SplashHitProj:
				proj = new SplashHitProj(new Napalm(NapalmType.SplashHit), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.ShinMessenkou:
				proj = new ShinMessenkouProj(new ShinMessenkou(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.Shingetsurin:
				proj = new ShingetsurinProj(pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.VileMissile:
				proj = new VileMissileProj(new VileMissile(VileMissileType.HumerusCrush), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.PopcornDemon:
				proj = new VileMissileProj(new VileMissile(VileMissileType.PopcornDemon), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.PopcornDemonSplit:
				proj = new VileMissileProj(
					new VileMissile(VileMissileType.PopcornDemon),
					pos, xDir, 1, player, netProjByte, vel: new Point()
				);
				break;
			case (int)ProjIds.Gemnu:
				proj = new GenmuProj(pos, xDir, extraData[0], player, netProjByte);
				break;
			case (int)ProjIds.PeaceOutRoller:
				proj = new PeaceOutRollerProj(new VileBall(VileBallType.PeaceOutRoller), pos, xDir, player, 0, netProjByte);
				break;
			case (int)ProjIds.NecroBurst:
				proj = new NecroBurstProj(new VileLaser(VileLaserType.NecroBurst), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.NecroBurstShrapnel: {
					ushort spriteIndex = BitConverter.ToUInt16(
						new byte[] { arguments[extraDataIndex], arguments[extraDataIndex + 1] }, 0
					);
					string spriteName = Global.spriteNameByIndex[spriteIndex];
					byte hasRaColorShaderByte = arguments[extraDataIndex + 2];
					proj = new RAShrapnelProj(
						new VileLaser(VileLaserType.NecroBurst),
						pos, spriteName, xDir, hasRaColorShaderByte == 1 ? true : false, player, netProjByte
					);
					break;
				}

			case (int)ProjIds.ChillPIceShot:
				proj = new ChillPIceProj(new ChillPIceShotWeapon(), pos, xDir, player, extraData[0], netProjByte);
				break;
			case (int)ProjIds.ChillPIcePenguin:
				proj = new ChillPIceStatueProj(new ChillPIceStatueWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.ChillPBlizzard:
				proj = new ChillPBlizzardProj(new ChillPBlizzardWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.ArmoredAProj:
				proj = new ArmoredAProj(new ArmoredAProjWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.ArmoredAChargeRelease:
				proj = new ArmoredAChargeReleaseProj(
					new ArmoredAChargeReleaseWeapon(), pos, xDir, new Point(), 6, player, netProjByte
				);
				break;
			case (int)ProjIds.LaunchOMissle:
				proj = new LaunchOMissile(new LaunchOMissileWeapon(), pos, xDir, player, extraData[0], netProjByte);
				break;
			case (int)ProjIds.LaunchOWhirlpool:
				proj = new LaunchOWhirlpoolProj(new LaunchOWhirlpoolWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.LaunchOTorpedo:
				proj = new TorpedoProj(new LaunchOHomingTorpedoWeapon(), pos, xDir, player, 3, netProjByte);
				break;
			case (int)ProjIds.BoomerangKBoomerang:
				proj = new BoomerangKBoomerangProj(new BoomerangKBoomerangWeapon(), pos, xDir, null, 0, player, netProjByte);
				break;
			case (int)ProjIds.StingCSting:
				proj = new StingCStingProj(new StingCStingWeapon(), pos, xDir, extraData[0], player, netProjByte);
				break;
			case (int)ProjIds.StingCSpike:
				proj = new StingCSpikeProj(new StingCSpikeWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.StormEEgg:
				proj = new StormEEggProj(new StormEEggWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.StormEGust:
				proj = new StormEGustProj(new StormEGustWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.StormEBird:
				proj = new StormEBirdProj(new StormEBirdWeapon(), pos, xDir, new Point(), new Point(), player, netProjByte);
				break;
			case (int)ProjIds.StormETornado:
				proj = new TornadoProj(new StormETornadoWeapon(), pos, xDir, true, player, netProjByte);
				break;
			case (int)ProjIds.FlameMFireball:
				proj = new FlameMFireballProj(new FlameMFireballWeapon(), pos, xDir, false, player, netProjByte);
				break;
			case (int)ProjIds.FlameMOil:
				proj = new FlameMOilProj(new FlameMOilWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FlameMOilSpill:
				proj = new FlameMOilSpillProj(new FlameMOilWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.FlameMOilFire:
				proj = new FlameMBigFireProj(new FlameMOilFireWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.FlameMStompShockwave:
				proj = new FlameMStompShockwave(new FlameMStompWeapon(player), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.VelGFire:
				proj = new VelGFireProj(new VelGFireWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.VelGIce:
				proj = new VelGIceProj(new VelGIceWeapon(), pos, xDir, new Point(), player, netProjByte);
				break;
			case (int)ProjIds.SigmaSlash:
				proj = new SigmaSlashProj(new SigmaSlashWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.SigmaBall:
				proj = new SigmaBallProj(new SigmaBallWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.SigmaHandElecBeam:
				proj = new WolfSigmaBeam(new WolfSigmaBeamWeapon(), pos, xDir, 1, 0, player, netProjByte);
				break;
			case (int)ProjIds.WSpongeLightning:
				proj = new WolfSigmaBeam(WireSponge.getWeapon(), pos, xDir, 1, 1, player, netProjByte);
				break;
			case (int)ProjIds.SigmaWolfHeadBallProj:
				proj = new WolfSigmaBall(new WolfSigmaHeadWeapon(), pos, new Point(), player, netProjByte);
				break;
			case (int)ProjIds.SigmaWolfHeadFlameProj:
				proj = new WolfSigmaFlame(new WolfSigmaHeadWeapon(), pos, new Point(), player, netProjByte);
				break;
			case (int)ProjIds.FSplasher:
				proj = new FSplasherProj(pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.HyorogaProj:
				proj = new HyorogaProj(pos, (int)extraData[0] - 128, player, netProjByte);
				break;
			case (int)ProjIds.TBreakerProj:
				proj = new TBreakerProj(pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.QuickHomesick:
				proj = new VileCutterProj(new VileCutter(VileCutterType.QuickHomesick), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.ParasiteSword:
				proj = new VileCutterProj(new VileCutter(VileCutterType.ParasiteSword), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.MaroonedTomahawk:
				proj = new VileCutterProj(new VileCutter(VileCutterType.MaroonedTomahawk), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.WildHorseKick:
				proj = new FlamethrowerProj(
					WildHorseKick.netWeapon,
					pos, xDir, extraData[0] == 1, player, netProjByte
				);
				break;
			case (int)ProjIds.SeaDragonRage:
				proj = new FlamethrowerProj(
					SeaDragonRage.netWeapon,
					pos, xDir, extraData[0] == 1, player, netProjByte
				);
				break;
			case (int)ProjIds.DragonsWrath:
				proj = new FlamethrowerProj(
					DragonsWrath.netWeapon,
					pos, xDir, extraData[0] == 1, player, netProjByte
				);
				break;
			case (int)ProjIds.RisingSpecter:
				proj = new RisingSpecterProj(new VileLaser(VileLaserType.RisingSpecter), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.StraightNightmare:
				proj = new StraightNightmareProj(
					new VileLaser(VileLaserType.StraightNightmare),
					pos, xDir, player, netProjByte
				);
				break;
			case (int)ProjIds.MetteurCrash:
				proj = new MettaurCrashProj(
					new AxlBullet(AxlBulletWeaponType.MetteurCrash),
					pos, player, Point.zero, netProjByte
				);
				break;
			case (int)ProjIds.BeastKiller:
				proj = new BeastKillerProj(new AxlBullet(AxlBulletWeaponType.BeastKiller), pos, player, Point.zero, netProjByte);
				break;
			case (int)ProjIds.MachineBullets:
				proj = new MachineBulletProj(new AxlBullet(AxlBulletWeaponType.MachineBullets), pos, player, Point.zero, netProjByte);
				break;
			case (int)ProjIds.RevolverBarrel:
				proj = new RevolverBarrelProj(new AxlBullet(AxlBulletWeaponType.RevolverBarrel), pos, player, Point.zero, netProjByte);
				break;
			case (int)ProjIds.AncientGun:
				proj = new AncientGunProj(new AxlBullet(AxlBulletWeaponType.AncientGun), pos, player, Point.zero, netProjByte);
				break;
			case (int)ProjIds.WSpongeChain:
				proj = new WSpongeSideChainProj(WireSponge.getChainWeapon(player), pos, xDir, null, 0, player, netProjByte);
				break;
			case (int)ProjIds.WSpongeUpChain:
				proj = new WSpongeUpChainProj(WireSponge.getChainWeapon(player), pos, xDir, null, 0, player, netProjByte);
				break;
			case (int)ProjIds.WSpongeChainSpin:
				proj = new WSpongeChainSpinProj(WireSponge.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.WSpongeSeed:
				proj = new WSpongeSeedProj(WireSponge.getWeapon(), pos, xDir, Point.zero, player, netProjByte);
				break;
			case (int)ProjIds.WSpongeSpike:
				proj = new WSpongeSpike(WireSponge.getWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.WheelGSpinWheel:
				proj = new WheelGSpinWheelProj(WheelGator.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.WheelGSpit:
				proj = new WheelGSpitProj(WheelGator.getWeapon(), pos, xDir, Point.zero, player, netProjByte);
				break;
			case (int)ProjIds.BCrabBubbleSplash:
				proj = new BCrabBubbleSplashProj(BubbleCrab.getWeapon(), pos, xDir, 0, 0, player, netProjByte);
				break;
			case (int)ProjIds.BCrabBubbleShield:
				proj = new BCrabShieldProj(BubbleCrab.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.BCrabCrabling:
				proj = new BCrabSummonCrabProj(BubbleCrab.getWeapon(), pos, Point.zero, null, player, netProjByte);
				break;
			case (int)ProjIds.BCrabCrablingBubble:
				proj = new BCrabSummonBubbleProj(BubbleCrab.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FStagFireball:
				proj = new FStagFireballProj(FlameStag.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FStagDashTrail:
				proj = new FStagTrailProj(FlameStag.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FStagDashCharge:
				proj = new FStagDashChargeProj(FlameStag.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FStagDash:
				proj = new FStagDashProj(FlameStag.getWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.MorphMCScrap:
				proj = new MorphMCScrapProj(
					MorphMothCocoon.getWeapon(),
					pos, xDir, Point.zero, float.MaxValue, false, null, player, netProjByte
				);
				break;
			case (int)ProjIds.MorphMCThread:
				proj = new MorphMCThreadProj(MorphMothCocoon.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.MorphMBeam:
				proj = new MorphMBeamProj(MorphMoth.getWeapon(), pos, pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.MorphMPowder:
				proj = new MorphMPowderProj(MorphMoth.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.MagnaCShuriken:
				proj = new MagnaCShurikenProj(MagnaCentipede.getWeapon(), pos, xDir, Point.zero, player, netProjByte);
				break;
			case (int)ProjIds.MagnaCMagnetMine:
				proj = new MagnaCMagnetMineProj(MagnaCentipede.getWeapon(), pos, Point.zero, 0, player, netProjByte);
				break;
			case (int)ProjIds.MagnaCMagnetPull:
				proj = new MagnaCMagnetPullProj(MagnaCentipede.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.CSnailCrystalHunter:
				proj = new CSnailCrystalHunterProj(
						CrystalSnail.getWeapon(), pos, xDir,
						new Point(xDir, (extraData[0] - 128) / 2f), player, netProjByte
					);
				break;
			case (int)ProjIds.OverdriveOSonicSlicer:
				proj = new OverdriveOSonicSlicerProj(OverdriveOstrich.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.OverdriveOSonicSlicerUp:
				proj = new OverdriveOSonicSlicerUpProj(OverdriveOstrich.getWeapon(), pos, extraData[0], player, netProjByte);
				break;
			case (int)ProjIds.FakeZeroBuster:
				proj = new FakeZeroBusterProj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FakeZeroBuster2:
				proj = new FakeZeroBuster2Proj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FakeZeroSwordBeam:
				proj = new FakeZeroSwordBeamProj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FakeZeroMelee:
				proj = new FakeZeroMeleeProj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.FakeZeroGroundPunch:
				proj = new FakeZeroRockProj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.Sigma2Ball:
				proj = new SigmaElectricBallProj(new SigmaElectricBallWeapon(), pos, 0, player, netProjByte);
				break;
			case (int)ProjIds.Sigma2Ball2:
				proj = new SigmaElectricBall2Proj(new SigmaElectricBallWeapon(), pos, 0, player, netProjByte);
				break;
			case (int)ProjIds.Sigma2ViralProj:
				proj = new ViralSigmaShootProj(null, pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.Sigma2ViralBeam:
				proj = new ViralSigmaBeamProj(new ViralSigmaBeamWeapon(), pos, player, netProjByte);
				break;
			case (int)ProjIds.Sigma2BirdProj:
				proj = new BirdMechaniloidProj(
					new MechaniloidWeapon(player, MechaniloidType.Bird),
					pos, xDir, player, netProjByte
				);
				break;
			case (int)ProjIds.Sigma2TankProj:
				proj = new TankMechaniloidProj(
					new MechaniloidWeapon(player, MechaniloidType.Tank),
					pos, 0, player, netProjByte
				);
				break;
			case (int)ProjIds.Sigma3Shield:
				proj = new SigmaShieldProj(pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.Sigma3Fire:
				proj = new Sigma3FireProj(pos, 0, 0, player, netProjByte);
				break;
			case (int)ProjIds.Sigma3KaiserMine:
				proj = new KaiserSigmaMineProj(new KaiserMineWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.Sigma3KaiserMissile:
				proj = new KaiserSigmaMissileProj(new KaiserMissileWeapon(), pos, player, netProjByte);
				break;
			case (int)ProjIds.Sigma3KaiserBeam:
				proj = new KaiserSigmaBeamProj(new KaiserBeamWeapon(), pos, xDir, extraData[0] == 1, player, netProjByte);
				break;
			case (int)ProjIds.TSeahorseAcid1:
				proj = new TSeahorseAcidProj(ToxicSeahorse.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.TSeahorseAcid2:
				proj = new TSeahorseAcid2Proj(ToxicSeahorse.getWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.TunnelRTornadoFang:
			case (int)ProjIds.TunnelRTornadoFang2:
				proj = new TunnelRTornadoFang(TunnelRhino.getWeapon(), pos, xDir, extraData[0], player, netProjByte);
				break;
			case (int)ProjIds.TunnelRTornadoFangDiag:
				proj = new TunnelRTornadoFangDiag(TunnelRhino.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.VoltCBall:
				proj = new TriadThunderProjCharged(new TriadThunder(), pos, xDir, 2, player, netProjByte);
				break;
			case (int)ProjIds.VoltCBarrier:
				proj = new VoltCBarrierProj(VoltCatfish.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.VoltCCharge:
				proj = new VoltCChargeProj(VoltCatfish.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.VoltCSparkle:
				proj = new VoltCSparkleProj(VoltCatfish.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.VoltCTriadThunder:
				proj = new VoltCTriadThunderProj(
					VoltCatfish.getWeapon(), pos, xDir, 0,
					new Point(xDir, 0.5f), null, player, netProjByte
				);
				break;
			case (int)ProjIds.VoltCUpBeam:
				proj = new VoltCUpBeamProj(VoltCatfish.getWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.VoltCUpBeam2:
				proj = new VoltCUpBeamProj(VoltCatfish.getWeapon(), pos, xDir, 1, player, netProjByte);
				break;
			case (int)ProjIds.CrushCProj:
				proj = new CrushCProj(CrushCrawfish.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.CrushCArmProj:
				proj = new CrushCArmProj(CrushCrawfish.getWeapon(), pos, xDir, Point.zero, null, player, netProjByte);
				break;
			case (int)ProjIds.NeonTRaySplasher:
				proj = new NeonTRaySplasherProj(NeonTiger.getWeapon(), pos, xDir, 0, false, player, netProjByte);
				break;
			case (int)ProjIds.GBeetleBall:
				proj = new GBeetleBallProj(GravityBeetle.getWeapon(), pos, xDir, false, player, netProjByte);
				break;
			case (int)ProjIds.GBeetleGravityWell:
				proj = new GBeetleGravityWellProj(GravityBeetle.getWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.DrDopplerBall:
				proj = new DrDopplerBallProj(DrDoppler.getWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.DrDopplerBall2:
				proj = new DrDopplerBallProj(DrDoppler.getWeapon(), pos, xDir, 1, player, netProjByte);
				break;
			case (int)ProjIds.RideChaserProj:
				proj = new RCProj(RideChaser.getGunWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.Buster:
				proj = new RCProj(RideChaser.getGunWeapon(), pos, xDir, 0, player, netProjByte);
				break;
			case (int)ProjIds.BBuffaloCrash:
				proj = new BBuffaloCrashProj(BlizzardBuffalo.getWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.BBuffaloIceProj:
				proj = new BBuffaloIceProj(BlizzardBuffalo.getWeapon(), pos, xDir, Point.zero, 0, player, netProjByte);
				break;
			case (int)ProjIds.BBuffaloIceProjGround:
				proj = new BBuffaloIceProjGround(BlizzardBuffalo.getWeapon(), pos, 0, player, netProjByte);
				break;
			case (int)ProjIds.BBuffaloBeam:
				ushort bbNetIdBytes = BitConverter.ToUInt16(extraData[0..2], 0);
				BlizzardBuffalo? bb = Global.level.getActorByNetId(bbNetIdBytes) as BlizzardBuffalo;
				proj = new BBuffaloBeamProj(BlizzardBuffalo.getWeapon(), pos, xDir, bb, player, netProjByte);
				break;
			case (int)ProjIds.BHornetBee:
				proj = new BHornetBeeProj(
					BlastHornet.netWeapon, pos, xDir, Point.zero, player, netProjByte
				);
				break;
			case (int)ProjIds.BHornetHomingBee:
				proj = new BHornetHomingBeeProj(
						BlastHornet.netWeapon, pos, xDir, null, player, netProjByte
					);
				break;
			case (int)ProjIds.BHornetCursor:
				proj = new BHornetCursorProj(
						BlastHornet.netWeapon, pos, xDir, Point.zero, null, player, netProjByte
					);
				break;
			case (int)ProjIds.DarkHold:
				proj = new DarkHoldProj(new DarkHoldWeapon(), pos, xDir, player, netProjByte);
				break;
			case (int)ProjIds.HexaInvolute:
				proj = new HexaInvoluteProj(new HexaInvoluteWeapon(), pos, xDir, player, netProjByte);
				break;
			default:
				proj = null;
				break;
		}

		/*
		// Template
		else if (projId == (int)ProjIds.PROJID)
		{
			proj = new PROJ(new WEP(), pos, xDir, player, netProjByte);
		}
		*/
		/*
		if (proj.damager != null) {
			proj.damager.damage = damage;
			proj.damager.flinch = flinch;
		}
		*/
	}
}
