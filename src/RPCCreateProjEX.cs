using Lidgren.Network;
using System;
using System.Collections.Generic;

namespace MMXOnline;

public partial class RPCCreateProj : RPC {
	public static Dictionary<int, ProjCreate> functs = new Dictionary<int, ProjCreate> {
		{ (int)ProjIds.Boomerang, BoomerangProj.projCreate },
		{ (int)ProjIds.ShotgunIce, ShotgunIceProj.projCreate },
		{ (int)ProjIds.TriadThunder, TriadThunderProj.projCreate },
		{ (int)ProjIds.TriadThunderQuake, TriadThunderQuake.projCreate },
		{ (int)ProjIds.TriadThunderCharged, TriadThunderProjCharged.projCreate },
		{ (int)ProjIds.RaySplasher, RaySplasherProj.projCreate },
		{ (int)ProjIds.RaySplasherChargedProj, RaySplasherProj.projCreate },
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
	}

	public delegate Projectile ProjCreate(ProjParameters arg);
