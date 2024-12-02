using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMXOnline;

namespace MMXOnline;

public class XLoadoutSetup {
	public static List<Weapon> getLoadout(Player player) {
		List<Weapon> weapons = new();
		// 1v1/Training loadout.
		if (Global.level.isTraining() && !Global.level.server.useLoadout || Global.level.is1v1()) {
			bool enableX1Weapons = player.xArmor1v1 == 1 || !Global.level.server.useLoadout;
			bool enableX2Weapons = player.xArmor1v1 == 2 || !Global.level.server.useLoadout;
			bool enableX3Weapons = player.xArmor1v1 == 3 || !Global.level.server.useLoadout;
			weapons.Add(new XBuster());

			if (enableX1Weapons) {
				weapons.Add(new HomingTorpedo());
				weapons.Add(new ChameleonSting());
				weapons.Add(new RollingShield());
				weapons.Add(new FireWave());
				weapons.Add(new StormTornado());
				weapons.Add(new ElectricSpark());
				weapons.Add(new BoomerangCutter());
				weapons.Add(new ShotgunIce());
			}
			if (enableX2Weapons) {
				weapons.Add(new CrystalHunter());
				weapons.Add(new BubbleSplash());
				weapons.Add(new SilkShot());
				weapons.Add(new SpinWheel());
				weapons.Add(new SonicSlicer());
				weapons.Add(new StrikeChain());
				weapons.Add(new MagnetMine());
				weapons.Add(new SpeedBurner());
			}
			if (enableX3Weapons) {
				weapons.Add(new AcidBurst());
				weapons.Add(new ParasiticBomb());
				weapons.Add(new TriadThunder());
				weapons.Add(new SpinningBlade());
				weapons.Add(new RaySplasher());
				weapons.Add(new GravityWell());
				weapons.Add(new FrostShield());
				weapons.Add(new TornadoFang());
			}

			if (player.hasArmArmor(3) || player.xArmor1v1 == 2) weapons.Add(new HyperCharge());
			if (player.hasBodyArmor(2) || player.xArmor1v1 == 3) weapons.Add(new GigaCrush());
		}
		// Regular Loadout.
		else {
			weapons = player.loadout.xLoadout.getWeaponsFromLoadout(player);
		}

		return weapons;
	}
}
