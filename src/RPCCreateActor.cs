using System;
using Lidgren.Network;

namespace MMXOnline;

public enum NetActorCreateId {
	Default,
	RideArmor,
	RaySplasherTurret,
	ChillPenguin,
	SparkMandrill,
	ArmoredArmadillo,
	LaunchOctopus,
	BoomerangKuwanger,
	StingChameleon,
	StormEagle,
	FlameMammoth,
	Velguarder,
	WolfSigmaHead,
	WolfSigmaHand,
	LargeHealth,
	SmallHealth,
	LargeAmmo,
	SmallAmmo,
	MechaniloidTank,
	MechaniloidFish,
	MechaniloidHopper,
	WireSponge,
	WheelGator,
	BubbleCrab,
	FlameStag,
	MorphMoth,
	MorphMothCocoon,
	MagnaCentipede,
	CrystalSnail,
	OverdriveOstrich,
	FakeZero,
	CrystalHunterCharged,
	CrystalSnailShell,
	BlizzardBuffalo,
	ToxicSeahorse,
	TunnelRhino,
	VoltCatfish,
	CrushCrawfish,
	NeonTiger,
	GravityBeetle,
	BlastHornet,
	DrDoppler,
	RideChaser,
}

public class RPCCreateActor : RPC {
	public RPCCreateActor() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		int createId = arguments[0];
		float xPos = BitConverter.ToSingle(new byte[] { arguments[1], arguments[2], arguments[3], arguments[4] }, 0);
		float yPos = BitConverter.ToSingle(new byte[] { arguments[5], arguments[6], arguments[7], arguments[8] }, 0);
		var playerId = arguments[9];
		var netProjByte = BitConverter.ToUInt16(new byte[] { arguments[10], arguments[11] }, 0);
		int xDir = arguments[12] - 128;

		var player = Global.level.getPlayerById(playerId);
		if (player == null) return;

		Actor? actor = Global.level.getActorByNetId(netProjByte);
		if (actor != null && (int)actor.netActorCreateId == createId) return;
		if (Global.level.recentlyDestroyedNetActors.ContainsKey(netProjByte)) return;
		Point pos = new Point(xPos, yPos);

		switch (createId) {
			case (int)NetActorCreateId.RaySplasherTurret:
				new RaySplasherTurret(pos, player, 1, netProjByte, false);
				break;
			case (int)NetActorCreateId.RideArmor:
				new RideArmor(player, pos, 0, 0, netProjByte, false);
				break;
			case (int)NetActorCreateId.ChillPenguin:
				new ChillPenguin(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.SparkMandrill:
				new SparkMandrill(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.ArmoredArmadillo:
				new ArmoredArmadillo(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.LaunchOctopus:
				new LaunchOctopus(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.BoomerangKuwanger:
				new BoomerangKuwanger(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.StingChameleon:
				new StingChameleon(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.StormEagle:
				new StormEagle(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.FlameMammoth:
				new FlameMammoth(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.Velguarder:
				new Velguarder(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.WolfSigmaHead:
				new WolfSigmaHead(pos, player, netProjByte, false);
				break;
			case (int)NetActorCreateId.WolfSigmaHand:
				new WolfSigmaHand(pos, player, false, netProjByte, false);
				break;
			case (int)NetActorCreateId.LargeHealth:
				new LargeHealthPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.SmallHealth:
				new SmallHealthPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.LargeAmmo:
				new LargeAmmoPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.SmallAmmo:
				new SmallAmmoPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.WireSponge:
				new WireSponge(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.WheelGator:
				new WheelGator(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.BubbleCrab:
				new BubbleCrab(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.FlameStag:
				new FlameStag(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.MorphMoth:
				new MorphMoth(player, pos, xDir, netProjByte, false, false);
				break;
			case (int)NetActorCreateId.MorphMothCocoon:
				new MorphMothCocoon(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.MagnaCentipede:
				new MagnaCentipede(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.CrystalSnail:
				new CrystalSnail(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.OverdriveOstrich:
				new OverdriveOstrich(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.FakeZero:
				new FakeZero(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.CrystalSnailShell:
				new CrystalSnailShell(pos, xDir, null, player, netProjByte, false, false);
				break;
			case (int)NetActorCreateId.CrystalHunterCharged:
				new CrystalHunterCharged(pos, player, netProjByte, false, 2, false);
				break;
			case (int)NetActorCreateId.MechaniloidTank:
				new Mechaniloid(pos, player, xDir, new MechaniloidWeapon(player, MechaniloidType.Tank), MechaniloidType.Tank, netProjByte, false);
				break;
			case (int)NetActorCreateId.MechaniloidHopper:
				new Mechaniloid(pos, player, xDir, new MechaniloidWeapon(player, MechaniloidType.Hopper), MechaniloidType.Hopper, netProjByte, false);
				break;
			case (int)NetActorCreateId.MechaniloidFish:
				new Mechaniloid(pos, player, xDir, new MechaniloidWeapon(player, MechaniloidType.Fish), MechaniloidType.Fish, netProjByte, false);
				break;
			case (int)NetActorCreateId.BlizzardBuffalo:
				new BlizzardBuffalo(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.ToxicSeahorse:
				new ToxicSeahorse(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.TunnelRhino:
				new TunnelRhino(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.VoltCatfish:
				new VoltCatfish(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.CrushCrawfish:
				new CrushCrawfish(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.NeonTiger:
				new NeonTiger(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.GravityBeetle:
				new GravityBeetle(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.BlastHornet:
				new BlastHornet(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.DrDoppler:
				new DrDoppler(player, pos, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.RideChaser:
				new RideChaser(player, pos, 0, netProjByte, false);
				break;
		}
	}
}

