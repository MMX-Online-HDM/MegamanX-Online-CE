using Lidgren.Network;
using System;
using System.Collections.Generic;

namespace MMXOnline;

public partial class RPCCreateProj : RPC {
	public static Dictionary<int, ProjCreate> functs = new Dictionary<int, ProjCreate> {
		{ (int)ProjIds.TriadThunder, TriadThunderProj.projCreate }
	};
}

public struct ProjParameters {
		public int projId;
		public Point pos;
		public int xDir;
		public Player player;
		public ushort netID;
		public byte[] extraData;
		public float angle;
	}

	public delegate Projectile ProjCreate(ProjParameters arg);
