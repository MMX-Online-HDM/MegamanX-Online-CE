using System.Collections.Generic;

namespace MMXOnline;

public partial class RPCCreateProj : RPC {
	public static Dictionary<int, ProjCreate> functs = new Dictionary<int, ProjCreate> {
		// X Stuff.
		//BUSTERS
		{ (int)ProjIds.Buster, BusterProj.rpcInvoke },
		{ (int)ProjIds.Buster2, Buster2Proj.rpcInvoke },
		{ (int)ProjIds.BusterUnpo, BusterUnpoProj.rpcInvoke },
		{ (int)ProjIds.Buster3, Buster3Proj.rpcInvoke },
		{ (int)ProjIds.Buster4, Buster4Proj.rpcInvoke },
		{ (int)ProjIds.BusterX3Proj2, BusterX3Proj2.rpcInvoke },
		{ (int)ProjIds.BusterX3Plasma, BusterPlasmaProj.rpcInvoke },
		{ (int)ProjIds.BusterX3PlasmaHit, BusterPlasmaHitProj.rpcInvoke },

		//X1 PROJS
		{ (int)ProjIds.Torpedo, TorpedoProj.rpcInvoke },
		{ (int)ProjIds.TorpedoCharged, TorpedoProj.rpcInvoke },
		{ (int)ProjIds.Sting, StingProj.rpcInvoke },
		{ (int)ProjIds.StingDiag, StingProj.rpcInvoke },
		{ (int)ProjIds.RollingShield, RollingShieldProj.rpcInvoke },
		{ (int)ProjIds.RollingShieldCharged, RollingShieldProjCharged.rpcInvoke },
		{ (int)ProjIds.FireWave, FireWaveProj.rpcInvoke },
		{ (int)ProjIds.FireWaveChargedStart, FireWaveProjChargedStart.rpcInvoke },
		{ (int)ProjIds.FireWaveCharged, FireWaveProjCharged.rpcInvoke },
		{ (int)ProjIds.Tornado, TornadoProj.rpcInvoke },
		{ (int)ProjIds.TornadoCharged, TornadoProjCharged.rpcInvoke },
		{ (int)ProjIds.ElectricSpark, ElectricSparkProj.rpcInvoke },
		{ (int)ProjIds.ElectricSparkChargedStart, ElectricSparkProjChargedStart.rpcInvoke },
		{ (int)ProjIds.ElectricSparkCharged, ElectricSparkProjCharged.rpcInvoke },
		{ (int)ProjIds.Boomerang, BoomerangProj.rpcInvoke },
		{ (int)ProjIds.BoomerangCharged, BoomerangProjCharged.rpcInvoke },
		{ (int)ProjIds.ShotgunIce, ShotgunIceProj.rpcInvoke },
		{ (int)ProjIds.ShotgunIceCharged, ShotgunIceProjCharged.rpcInvoke },
		{ (int)ProjIds.ShotgunIceSled, ShotgunIceProjSled.rpcInvoke },
		{ (int)ProjIds.Hadouken, HadoukenProj.rpcInvoke} ,

		//X2 PROJS
		{ (int)ProjIds.CrystalHunter, CrystalHunterProj.rpcInvoke },
		{ (int)ProjIds.BubbleSplash, BubbleSplashProj.rpcInvoke },
		{ (int)ProjIds.BubbleSplashCharged, BubbleSplashProjCharged.rpcInvoke },
		{ (int)ProjIds.SilkShot, SilkShotProj.rpcInvoke },
		{ (int)ProjIds.SilkShotShrapnel, SilkShotProjShrapnel.rpcInvoke },
		{ (int)ProjIds.SilkShotChargedLv2, SilkShotProjLv2.rpcInvoke },
		{ (int)ProjIds.SilkShotCharged, SilkShotProjCharged.rpcInvoke },
		{ (int)ProjIds.SpinWheel, SpinWheelProj.rpcInvoke },
		{ (int)ProjIds.SpinWheelChargedStart, SpinWheelProjChargedStart.rpcInvoke },
		{ (int)ProjIds.SpinWheelCharged, SpinWheelProjCharged.rpcInvoke },
		{ (int)ProjIds.SonicSlicerStart, SonicSlicerStart.rpcInvoke },
		{ (int)ProjIds.SonicSlicer, SonicSlicerProj.rpcInvoke },
		{ (int)ProjIds.SonicSlicerCharged, SonicSlicerProjCharged.rpcInvoke },
		{ (int)ProjIds.StrikeChain, StrikeChainProj.rpcInvoke },
		{ (int)ProjIds.StrikeChainCharged, StrikeChainProjCharged.rpcInvoke },
		{ (int)ProjIds.MagnetMine, MagnetMineProj.rpcInvoke },
		{ (int)ProjIds.MagnetMineCharged, MagnetMineProjCharged.rpcInvoke },
		{ (int)ProjIds.SpeedBurner, SpeedBurnerProj.rpcInvoke },
		{ (int)ProjIds.SpeedBurnerWater, SpeedBurnerProjWater.rpcInvoke },
		{ (int)ProjIds.ItemTracer, ItemTracerProj.rpcInvoke },

		//X3 PROJS
		{ (int)ProjIds.AcidBurst, AcidBurstProj.rpcInvoke },
		{ (int)ProjIds.AcidBurstSmall, AcidBurstProjSmall.rpcInvoke },
		{ (int)ProjIds.AcidBurstCharged, AcidBurstProjCharged.rpcInvoke },
		{ (int)ProjIds.ParasiticBomb, ParasiticBombProj.rpcInvoke },
		{ (int)ProjIds.ParasiticBombCharged, ParasiticBombProjCharged.rpcInvoke },
		{ (int)ProjIds.TriadThunder, TriadThunderProj.rpcInvoke },
		{ (int)ProjIds.TriadThunderQuake, TriadThunderQuake.rpcInvoke },
		{ (int)ProjIds.TriadThunderCharged, TriadThunderProjCharged.rpcInvoke },
		{ (int)ProjIds.SpinningBlade, SpinningBladeProj.rpcInvoke },
		{ (int)ProjIds.SpinningBladeCharged, SpinningBladeProjCharged.rpcInvoke },
		{ (int)ProjIds.RaySplasher, RaySplasherProj.rpcInvoke },
		{ (int)ProjIds.RaySplasherChargedProj, RaySplasherTurretProj.rpcInvoke },
		{ (int)ProjIds.GravityWell, GravityWellProj.rpcInvoke },
		{ (int)ProjIds.GravityWellCharged, GravityWellProjCharged.rpcInvoke },
		{ (int)ProjIds.FrostShield, FrostShieldProj.rpcInvoke },
		{ (int)ProjIds.FrostShieldAir, FrostShieldProjAir.rpcInvoke },
		{ (int)ProjIds.FrostShieldGround, FrostShieldProjGround.rpcInvoke },
		{ (int)ProjIds.FrostShieldCharged, FrostShieldProjCharged.rpcInvoke },
		{ (int)ProjIds.FrostShieldChargedGrounded, FrostShieldProjChargedGround.rpcInvoke },
		{ (int)ProjIds.FrostShieldPlatform, FrostShieldProjPlatform.rpcInvoke },
		{ (int)ProjIds.TunnelFang, TunnelFangProj.rpcInvoke },
		{ (int)ProjIds.TunnelFang2, TunnelFangProj.rpcInvoke },
		{ (int)ProjIds.TunnelFangCharged, TunnelFangProjCharged.rpcInvoke },
		{ (int)ProjIds.XSaberProj, XSaberProj.rpcInvoke },

		//EXTRA
		{ (int)ProjIds.UPParryMelee, UPParryMeleeProj.rpcInvoke },
		{ (int)ProjIds.UPParryProj, UPParryRangedProj.rpcInvoke },
		
		// Vile stuff.
		{ (int)ProjIds.FrontRunner, VileCannonProj.rpcInvoke },
		{ (int)ProjIds.FatBoy, VileCannonProj.rpcInvoke },
		{ (int)ProjIds.LongshotGizmo, VileCannonProj.rpcInvoke },
		// Buster Zero
		{ (int)ProjIds.DZBuster, DZBusterProj.rpcInvoke },
		{ (int)ProjIds.DZBuster2, DZBuster2Proj.rpcInvoke },
		{ (int)ProjIds.DZBuster3, DZBuster3Proj.rpcInvoke },
		// Mavericks
		{ (int)ProjIds.VoltCSuck, VoltCSuckProj.rpcInvoke },
	};

}

public struct ProjParameters {
	public int projId;
	public Point pos;
	public int xDir;
	public Player player;
	public ushort netId;
	public byte[] extraData;
	public float angle;
	public float byteAngle;
}

public delegate Projectile ProjCreate(ProjParameters arg);
