using System;

namespace MMXOnline;

public static class XWeaknesses {
	public static bool checkMaverickWeakness(Player player, ProjIds projId) {
		switch (player.weapon) {
			case HomingTorpedo:
				return projId == ProjIds.ArmoredARoll;
			case ChameleonSting:
				return projId == ProjIds.BoomerangKBoomerang;
			case RollingShield:
				return projId == ProjIds.SparkMSpark;
			case FireWave:
				return projId == ProjIds.StormETornado;
			case StormTornado:
				return projId == ProjIds.StingCSting;
			case ElectricSpark:
				return projId == ProjIds.ChillPIcePenguin || projId == ProjIds.ChillPIceShot;
			case BoomerangCutter:
				return projId == ProjIds.LaunchOMissle || projId == ProjIds.LaunchOTorpedo;
			case ShotgunIce:
				return projId == ProjIds.FlameMFireball || projId == ProjIds.FlameMOilFire;
			case CrystalHunter:
				return projId == ProjIds.MagnaCMagnetMine;
			case BubbleSplash:
				return projId == ProjIds.WheelGSpinWheel;
			case SilkShot:
				return projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash ||
					   projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail;
			case SpinWheel:
				return projId == ProjIds.WSpongeChain || projId == ProjIds.WSpongeUpChain;
			case SonicSlicer:
				return projId == ProjIds.CSnailCrystalHunter;
			case StrikeChain:
				return projId == ProjIds.OverdriveOSonicSlicer || projId == ProjIds.OverdriveOSonicSlicerUp;
			case MagnetMine:
				return projId == ProjIds.MorphMCScrap || projId == ProjIds.MorphMBeam;
			case SpeedBurner:
				return projId == ProjIds.BCrabBubbleSplash;
			case AcidBurst:
				return projId == ProjIds.BBuffaloIceProj || projId == ProjIds.BBuffaloIceProjGround;
			case ParasiticBomb:
				return projId == ProjIds.GBeetleGravityWell || projId == ProjIds.GBeetleBall;
			case TriadThunder:
				return projId == ProjIds.TunnelRTornadoFang || projId == ProjIds.TunnelRTornadoFang2 || projId == ProjIds.TunnelRTornadoFangDiag;
			case SpinningBlade:
				return projId == ProjIds.VoltCBall || projId == ProjIds.VoltCTriadThunder || projId == ProjIds.VoltCUpBeam || projId == ProjIds.VoltCUpBeam2;
			case RaySplasher:
				return projId == ProjIds.CrushCArmProj;
			case GravityWell:
				return projId == ProjIds.NeonTRaySplasher;
			case FrostShield:
				return projId == ProjIds.BHornetBee || projId == ProjIds.BHornetHomingBee;
			case TornadoFang:
				return projId == ProjIds.TSeahorseAcid1 || projId == ProjIds.TSeahorseAcid2;
		}

		return false;
	}
	public static bool checkWeakness(Player player, ProjIds projId) {
		switch (player.weapon) {
			case HomingTorpedo:
				return projId == ProjIds.RollingShield || projId == ProjIds.RollingShieldCharged;
			case ChameleonSting:
				return projId == ProjIds.Boomerang || projId == ProjIds.BoomerangCharged;
			case RollingShield:
				return projId == ProjIds.ElectricSpark || projId == ProjIds.ElectricSparkCharged ||
					   projId == ProjIds.ElectricSparkChargedStart;
			case FireWave:
				return projId == ProjIds.Tornado || projId == ProjIds.TornadoCharged;
			case StormTornado:
				return projId == ProjIds.Sting || projId == ProjIds.StingDiag;
			case ElectricSpark:
				return projId == ProjIds.ShotgunIce || projId == ProjIds.ShotgunIceSled || 
					   projId == ProjIds.ShotgunIceCharged;
			case BoomerangCutter:
				return projId == ProjIds.Torpedo || projId == ProjIds.TorpedoCharged;
			case ShotgunIce:
				return projId == ProjIds.FireWave || projId == ProjIds.FireWaveCharged ||
					   projId == ProjIds.FireWaveChargedStart;
			case CrystalHunter:
				return projId == ProjIds.MagnetMine || projId == ProjIds.MagnetMineCharged;
			case BubbleSplash:
				return projId == ProjIds.SpinWheel || projId == ProjIds.SpinWheelCharged || 
					   projId == ProjIds.SpinWheelChargedStart;
			case SilkShot:
				return projId == ProjIds.SpeedBurner || projId == ProjIds.SpeedBurnerCharged ||
					   projId == ProjIds.SpeedBurnerWater || projId == ProjIds.SpeedBurnerTrail;
			case SpinWheel:
				return projId == ProjIds.StrikeChain || projId == ProjIds.StrikeChainCharged;
			case SonicSlicer:
				return projId == ProjIds.CrystalHunter || projId == ProjIds.CrystalHunterDash;
			case StrikeChain:
				return projId == ProjIds.SonicSlicer || projId == ProjIds.SonicSlicerCharged ||
					   projId == ProjIds.SonicSlicerStart;
			case MagnetMine:
				return projId == ProjIds.SilkShot || projId == ProjIds.SilkShotCharged ||
				       projId == ProjIds.SilkShotShrapnel;
			case SpeedBurner:
				return projId == ProjIds.BubbleSplash || projId == ProjIds.BubbleSplashCharged;
			case AcidBurst:
				return projId == ProjIds.FrostShield || projId == ProjIds.FrostShieldAir ||
					   projId == ProjIds.FrostShieldCharged || projId == ProjIds.FrostShieldChargedGrounded || 
					   projId == ProjIds.FrostShieldGround || projId == ProjIds.FrostShieldPlatform;
			case ParasiticBomb:
				return projId == ProjIds.GravityWell || projId == ProjIds.GravityWellCharged;
			case TriadThunder:
				return projId == ProjIds.TornadoFang || projId == ProjIds.TornadoFang2 || 
					   projId == ProjIds.TornadoFangCharged;
			case SpinningBlade:
				return projId == ProjIds.TriadThunder || projId == ProjIds.TriadThunderBall || 
					   projId == ProjIds.TriadThunderBeam || projId == ProjIds.TriadThunderCharged ||
					   projId == ProjIds.TriadThunderQuake;
			case RaySplasher:
				return projId == ProjIds.SpinningBlade || projId == ProjIds.SpinningBladeCharged;
			case GravityWell:
				return projId == ProjIds.RaySplasher || projId == ProjIds.RaySplasherChargedProj;
			case FrostShield:
				return projId == ProjIds.ParasiticBomb || projId == ProjIds.ParasiticBombCharged ||
					   projId == ProjIds.ParasiticBombExplode;
			case TornadoFang:
				return projId == ProjIds.AcidBurst || projId == ProjIds.AcidBurstCharged || 
					   projId == ProjIds.AcidBurstSmall || projId == ProjIds.AcidBurstPoison;
		}

		return false;
	}
}
