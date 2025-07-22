using System;

namespace MMXOnline;

public class MaverickWeapon : Weapon {
	public Player? player;
	public MaverickModeId controlMode;
	public MaverickModeId trueControlMode;

	public bool isMenuOpened;
	public float cooldown;
	public bool summonedOnce;
	public float lastHealth;
	public const float summonerCooldown = 2;
	public const float tagTeamCooldown = 4;
	public const float strikerCooldown = 4;
	public SavedMaverickData? smd;
	protected Maverick? _maverick;
	public Maverick? maverick {
		get {
			if (_maverick != null && _maverick.destroyed) {
				cooldown = _maverick.controlMode == MaverickModeId.TagTeam ? tagTeamCooldown : strikerCooldown;
				lastHealth = _maverick.health;
				if (_maverick.health <= 0) smd = null;
				else smd = new SavedMaverickData(_maverick);
				_maverick = null;
			}
			return _maverick;
		}
		set {
			_maverick = value;
		}
	}
	public int selCommandIndex = 2;
	public int selCommandIndexX = 1;
	public const int maxCommandIndex = 4;
	public float currencyHUDAnimTime;
	public const float currencyHUDMaxAnimTime = 0.75f;
	public float currencyGainCooldown;
	public float currencyGainMaxCooldown {
		get {
			return 10 + player?.currency ?? 0;
		}
	}

	public bool isMoth;

	public MaverickWeapon(Player? player, int controlMode) {
		this.controlMode = (MaverickModeId)controlMode;
		lastHealth = player?.getMaverickMaxHp(this.controlMode) ?? 32;
		this.player = player;
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref cooldown);

		if (player != null && !summonedOnce && player.character != null) {
			if (currencyGainCooldown < currencyGainMaxCooldown) {
				currencyGainCooldown += Global.spf;
				if (currencyGainCooldown >= currencyGainMaxCooldown) {
					currencyGainCooldown = 0;
					currencyHUDAnimTime = Global.spf;
					player.currency++;
				}
			}
		}

		if (currencyHUDAnimTime > 0) {
			currencyHUDAnimTime += Global.spf;
			if (currencyHUDAnimTime > currencyHUDMaxAnimTime) {
				currencyHUDAnimTime = 0;
			}
		}
	}

	public Maverick summon(Player player, Point pos, Point destPos, int xDir, bool isMothHatch = false) {
		// X1
		if (this is ChillPenguinWeapon) {
			maverick = new ChillPenguin(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is SparkMandrillWeapon) {
			maverick = new SparkMandrill(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is ArmoredArmadilloWeapon) {
			maverick = new ArmoredArmadillo(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is LaunchOctopusWeapon) {
			maverick = new LaunchOctopus(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is BoomerangKuwangerWeapon) {
			maverick = new BoomerangKuwanger(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is StingChameleonWeapon) {
			maverick = new StingChameleon(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is StormEagleWeapon) {
			maverick = new StormEagle(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is FlameMammothWeapon) {
			maverick = new FlameMammoth(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is VelguarderWeapon) {
			maverick = new Velguarder(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		}
		  // X2
		  else if (this is WireSpongeWeapon) {
			maverick = new WireSponge(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is WheelGatorWeapon) {
			maverick = new WheelGator(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is BubbleCrabWeapon) {
			maverick = new BubbleCrab(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is FlameStagWeapon) {
			maverick = new FlameStag(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is MorphMothWeapon mmw) {
			if (mmw.isMoth) {
				maverick = new MorphMoth(player, pos, destPos, xDir, player.getNextActorNetId(), true, isMothHatch, sendRpc: true);
			} else {
				maverick = new MorphMothCocoon(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
			}
		} else if (this is MagnaCentipedeWeapon) {
			maverick = new MagnaCentipede(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is CrystalSnailWeapon) {
			maverick = new CrystalSnail(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is OverdriveOstrichWeapon) {
			maverick = new OverdriveOstrich(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is FakeZeroWeapon) {
			maverick = new FakeZero(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		}
		  // X3
		  else if (this is BlizzardBuffaloWeapon) {
			maverick = new BlizzardBuffalo(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is ToxicSeahorseWeapon) {
			maverick = new ToxicSeahorse(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is TunnelRhinoWeapon) {
			maverick = new TunnelRhino(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is VoltCatfishWeapon) {
			maverick = new VoltCatfish(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is CrushCrawfishWeapon) {
			maverick = new CrushCrawfish(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is NeonTigerWeapon) {
			maverick = new NeonTiger(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is GravityBeetleWeapon) {
			maverick = new GravityBeetle(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is BlastHornetWeapon) {
			maverick = new BlastHornet(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
		} else if (this is DrDopplerWeapon ddw) {
			var drDoppler = new DrDoppler(
				player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true
			);
			drDoppler.ballType = ddw.ballType;
			maverick = drDoppler;
		}

		if (maverick == null) {
			throw new Exception("Error summoning maverick on maverick weapon " + this.GetType().ToString());
		}
		maverick.controlMode = controlMode;
		maverick.trueControlMode = trueControlMode;
		maverick.rootWeapon = this;
		maverick.maxHealth = player.getMaverickMaxHp(controlMode);
		maverick.health = maverick.maxHealth;

		if (summonedOnce) {
			maverick.setHealth(lastHealth);
		} else {
			lastHealth = maverick.maxHealth;
		}
		smd?.applySavedMaverickData(maverick, controlMode == MaverickModeId.Puppeteer);
		if (controlMode == MaverickModeId.Striker) {
			maverick.ammo = maverick.maxAmmo;
		}
		summonedOnce = true;
		return maverick;
	}

	public bool canUseSubtank(SubTank subtank) {
		return maverick != null && maverick.health < maverick.maxHealth;
	}

	public bool canIssueOrders() {
		return controlMode is MaverickModeId.Summoner or MaverickModeId.Puppeteer;
	}

	public bool canIssueAttack() {
		return controlMode is MaverickModeId.Summoner;
	}
}

public class SigmaMenuWeapon : Weapon {
	public SigmaMenuWeapon() {
		index = (int)WeaponIds.Sigma;
		weaponSlotIndex = 65;
		displayName = "Sigma";
		fireRate = 60 * 4;
		drawAmmo = false;
		drawCooldown = false;
	}
}

public class ChillPenguinWeapon : MaverickWeapon {
	public static ChillPenguinWeapon netWeapon = new();

	public ChillPenguinWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.ChillPenguin;
		weaponSlotIndex = 66;
		displayName = "Chill Penguin";
	}
}

public class SparkMandrillWeapon : MaverickWeapon {
	public static SparkMandrillWeapon netWeapon = new();

	public SparkMandrillWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.SparkMandrill;
		weaponSlotIndex = 67;
		displayName = "Spark Mandrill";
	}
}

public class ArmoredArmadilloWeapon : MaverickWeapon {
	public static ArmoredArmadilloWeapon netWeapon = new();

	public ArmoredArmadilloWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.ArmoredArmadillo;
		weaponSlotIndex = 68;
		displayName = "Armored Armadillo";
	}
}

public class LaunchOctopusWeapon : MaverickWeapon {
	public static LaunchOctopusWeapon netWeapon = new();

	public LaunchOctopusWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.LaunchOctopus;
		weaponSlotIndex = 69;
		displayName = "Launch Octopus";
	}
}

public class BoomerangKuwangerWeapon : MaverickWeapon {
	public static BoomerangKuwangerWeapon netWeapon = new();

	public BoomerangKuwangerWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.BoomerangKuwanger;
		weaponSlotIndex = 70;
		displayName = "Boomerang Kuwanger";
	}
}

public class StingChameleonWeapon : MaverickWeapon {
	public static StingChameleonWeapon netWeapon = new();

	public StingChameleonWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.StingChameleon;
		weaponSlotIndex = 71;
		displayName = "Sting Chameleon";
	}
}

public class StormEagleWeapon : MaverickWeapon {
	public static StormEagleWeapon netWeapon = new();

	public StormEagleWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.StormEagle;
		weaponSlotIndex = 72;
		displayName = "Storm Eagle";
	}
}

public class FlameMammothWeapon : MaverickWeapon {
	public static FlameMammothWeapon netWeapon = new();

	public FlameMammothWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.FlameMammoth;
		weaponSlotIndex = 73;
		displayName = "Flame Mammoth";
	}
}

public class VelguarderWeapon : MaverickWeapon {
	public static VelguarderWeapon netWeapon = new();

	public VelguarderWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.Velguarder;
		weaponSlotIndex = 74;
		displayName = "Velguarder";
	}
}

public class WireSpongeWeapon : MaverickWeapon {
	public static WireSpongeWeapon netWeapon = new();

	public WireSpongeWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.WireSponge;
		weaponSlotIndex = 75;
		displayName = "Wire Sponge";
	}
}

public class WheelGatorWeapon : MaverickWeapon {
	public static WheelGatorWeapon netWeapon = new();

	public WheelGatorWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.WheelGator;
		weaponSlotIndex = 76;
		displayName = "Wheel Gator";
	}
}

public class BubbleCrabWeapon : MaverickWeapon {
	public static BubbleCrabWeapon netWeapon = new();

	public BubbleCrabWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.BubbleCrab;
		weaponSlotIndex = 77;
		displayName = "Bubble Crab";
	}
}

public class FlameStagWeapon : MaverickWeapon {
	public static FlameStagWeapon netWeapon = new();

	public FlameStagWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.FlameStag;
		weaponSlotIndex = 78;
		displayName = "Flame Stag";
	}
}

public class MorphMothWeapon : MaverickWeapon {
	public static MorphMothWeapon netWeapon = new();

	public MorphMothWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.MorphMoth;
		weaponSlotIndex = 79;
		displayName = "Morph Moth";
	}

	public override void update() {
		base.update();
		if (!isMoth) weaponSlotIndex = 109;
		else weaponSlotIndex = 79;
	}
}

public class MagnaCentipedeWeapon : MaverickWeapon {
	public static MagnaCentipedeWeapon netWeapon = new();

	public MagnaCentipedeWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.MagnaCentipede;
		weaponSlotIndex = 80;
		displayName = "Magna Centipede";
	}
}

public class CrystalSnailWeapon : MaverickWeapon {
	public static CrystalSnailWeapon netWeapon = new();

	public CrystalSnailWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.CrystalSnail;
		weaponSlotIndex = 81;
		displayName = "Crystal Snail";
	}
}

public class OverdriveOstrichWeapon : MaverickWeapon {
	public static OverdriveOstrichWeapon netWeapon = new();

	public OverdriveOstrichWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.OverdriveOstrich;
		weaponSlotIndex = 82;
		displayName = "Overdrive Ostrich";
	}
}

public class FakeZeroWeapon : MaverickWeapon {
	public static FakeZeroWeapon netWeapon = new();

	public FakeZeroWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.FakeZero;
		weaponSlotIndex = 83;
		displayName = "Fake Zero";
	}
}

public class BlizzardBuffaloWeapon : MaverickWeapon {
	public static BlizzardBuffaloWeapon netWeapon = new();

	public BlizzardBuffaloWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.BlizzardBuffalo;
		weaponSlotIndex = 84;
		displayName = "Blizzard Buffalo";
	}
}

public class ToxicSeahorseWeapon : MaverickWeapon {
	public static ToxicSeahorseWeapon netWeapon = new();

	public ToxicSeahorseWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.ToxicSeahorse;
		weaponSlotIndex = 85;
		displayName = "Toxic Seahorse";
	}
}

public class TunnelRhinoWeapon : MaverickWeapon {
	public static TunnelRhinoWeapon netWeapon = new();

	public TunnelRhinoWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.TunnelRhino;
		weaponSlotIndex = 86;
		displayName = "Tunnel Rhino";
	}
}

public class VoltCatfishWeapon : MaverickWeapon {
	public static VoltCatfishWeapon netWeapon = new();

	public VoltCatfishWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.VoltCatfish;
		weaponSlotIndex = 87;
		displayName = "Volt Catfish";
	}
}

public class CrushCrawfishWeapon : MaverickWeapon {
	public static CrushCrawfishWeapon netWeapon = new();

	public CrushCrawfishWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.CrushCrawfish;
		weaponSlotIndex = 88;
		displayName = "Crush Crawfish";
	}
}

public class NeonTigerWeapon : MaverickWeapon {
	public static NeonTigerWeapon netWeapon = new();

	public NeonTigerWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.NeonTiger;
		weaponSlotIndex = 89;
		displayName = "Neon Tiger";
	}
}

public class GravityBeetleWeapon : MaverickWeapon {
	public static GravityBeetleWeapon netWeapon = new();

	public GravityBeetleWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.GravityBeetle;
		weaponSlotIndex = 90;
		displayName = "Gravity Beetle";
	}
}

public class BlastHornetWeapon : MaverickWeapon {
	public static BlastHornetWeapon netWeapon = new();

	public BlastHornetWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.BlastHornet;
		weaponSlotIndex = 91;
		displayName = "Blast Hornet";
	}
}

public class DrDopplerWeapon : MaverickWeapon {
	public static DrDopplerWeapon netWeapon = new();

	public int ballType; // 0 = shock gun, 1 = vaccine
	public DrDopplerWeapon(Player? player = null, int controlMode = 0) : base(player, controlMode) {
		index = (int)WeaponIds.DrDoppler;
		weaponSlotIndex = 92;
		displayName = "Dr. Doppler";
	}
}
