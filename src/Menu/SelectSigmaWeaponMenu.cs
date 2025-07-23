using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class SigmaWeaponCursor {
	public int index;

	public SigmaWeaponCursor(int index) {
		this.index = index;
	}

	public int startOffset() {
		if (index < 9) return 0;
		else if (index >= 9 && index <= 17) return 9;
		else return 18;
	}

	public int numWeapons() {
		return 9;
	}

	public void cycleLeft() {
		if (index < 9) index = 18;
		else if (index >= 9 && index <= 17) index = 0;
		else if (index > 17) index = 9;
	}

	public void cycleRight() {
		if (index < 9) index = 9;
		else if (index >= 9 && index <= 17) index = 18;
		else if (index > 17) index = 0;
	}
}


public class SelectSigmaWeaponMenu : IMainMenu {
	public bool inGame;
	public List<SigmaWeaponCursor> cursors;
	public int selCursorIndex;
	public List<Point> weaponPositions = new List<Point>();
	public string error = "";
	public int maxRows = 1;
	public int maxCols = 9;
	public Weapon[] weapons;

	public IMainMenu prevMenu;

	public SelectSigmaWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		for (int i = 0; i < 9; i++) {
			weaponPositions.Add(new Point(80, 42 + (i * 18)));
		}

		this.inGame = inGame;

		cursors = new List<SigmaWeaponCursor>();
		cursors.Add(new SigmaWeaponCursor(Options.main.sigmaLoadout.maverick1));
		cursors.Add(new SigmaWeaponCursor(Options.main.sigmaLoadout.maverick2));
		cursors.Add(new SigmaWeaponCursor(Options.main.sigmaLoadout.sigmaForm));
		cursors.Add(new SigmaWeaponCursor(Options.main.sigmaLoadout.commandMode));

		List<Weapon> tempWp = Weapon.getAllSigmaWeapons(null);
		tempWp.RemoveAt(0);
		weapons = tempWp.ToArray();
	}

	public void update() {
		if (!string.IsNullOrEmpty(error)) {
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				error = "";
			}
			return;
		}

		if (selCursorIndex < 2) {
			//Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, 8, playSound: true);
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				cursors[selCursorIndex].index--;
				if (cursors[selCursorIndex].index == -1) cursors[selCursorIndex].index = 26;
				else if (cursors[selCursorIndex].index == 8) cursors[selCursorIndex].index = 8;
				else if (cursors[selCursorIndex].index == 17) cursors[selCursorIndex].index = 17;
				Global.playSound("menuX2");
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				cursors[selCursorIndex].index++;
				if (cursors[selCursorIndex].index == 9) cursors[selCursorIndex].index = 9;
				else if (cursors[selCursorIndex].index == 18) cursors[selCursorIndex].index = 18;
				else if (cursors[selCursorIndex].index == 27) cursors[selCursorIndex].index = 0;
				Global.playSound("menuX2");
			}
			if (Global.input.isPressedMenu(Control.WeaponLeft)) {
				cursors[selCursorIndex].cycleLeft();
			} else if (Global.input.isPressedMenu(Control.WeaponRight)) {
				cursors[selCursorIndex].cycleRight();
			}
		} else if (selCursorIndex == 2) {
			Helpers.menuLeftRightInc(ref cursors[2].index, 0, 2, playSound: true);
		} else if (selCursorIndex == 3) {
			Helpers.menuLeftRightInc(ref cursors[3].index, 0, 3, playSound: true);
		}

		Helpers.menuUpDown(ref selCursorIndex, 0, 3);

		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		bool selectPressed = Global.input.isPressedMenu(Control.MenuConfirm) || (backPressed && !inGame);
		if (selectPressed) {
			if (cursors[0].index == cursors[1].index) {
				error = "Cannot select same maverick more than once!";
				return;
			}

			if (cursors[0].index != Options.main.sigmaLoadout.maverick1 ||
				cursors[1].index != Options.main.sigmaLoadout.maverick2 ||
				cursors[2].index != Options.main.sigmaLoadout.sigmaForm ||
				cursors[3].index != Options.main.sigmaLoadout.commandMode) {
				Options.main.sigmaLoadout.maverick1 = cursors[0].index;
				Options.main.sigmaLoadout.maverick2 = cursors[1].index;
				Options.main.sigmaLoadout.sigmaForm = cursors[2].index;
				Options.main.sigmaLoadout.commandMode = cursors[3].index;
				Options.main.saveToFile();
				if (inGame) {
					if (Options.main.killOnLoadoutChange) {
						Global.level.mainPlayer.forceKill();
					} else if (!Global.level.mainPlayer.isDead) {
						Global.level.gameMode.setHUDErrorMessage(
							Global.level.mainPlayer, "Change will apply on next death", playSound: false
						);
					}
				}
			}

			if (inGame) Menu.exit();
			else Menu.change(prevMenu);
		} else if (backPressed) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["loadoutbackground"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenuload"], 0, 0);
		}

		Fonts.drawText(FontType.Yellow, "Sigma Loadout", Global.screenW * 0.5f, 24, Alignment.Center);

		var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
		float botOffY = inGame ? 0 : -2;

		int startY = 48;
		int startX = 40;
		int startX2 = 128;
		int startY2 = 88;
		int wepW = 18;
		int wepH = 24;

		float leftArrowPos = startX2 - 16;
		float rightArrowPos = startX2 + (wepW * 9) - 8;
		if (selCursorIndex < 2) {
			Global.sprites["cursor"].drawToHUD(0, startX - 6, startY + (selCursorIndex * wepH) - 1);
		} else {
			float curPos2 = startY2 + ((selCursorIndex - 2) * 16);
			Global.sprites["cursor"].drawToHUD(0, startX - 6, curPos2 + 3);
		}
		for (int i = 0; i < 4; i++) {
			float yPos = startY - 4 + (i * wepH);
			float yPos2 = startY2 + ((i - 2) * 16);

			if (i == 2) {
				Fonts.drawText(FontType.Blue, "Main Body: ", 40, yPos2, selected: selCursorIndex == i);
				string form = "Commander Sigma";
				if (cursors[i].index == 1) { form = "Neo Sigma"; }
				if (cursors[i].index == 2) { form = "Dopple Sigma"; }
				if (cursors[i].index == 3) { form = "Dr. Doppler"; }
				Fonts.drawText(FontType.Blue, form, startX2 - 6, yPos2, selected: selCursorIndex == i);
				continue;
			}

			if (i == 3) {
				Fonts.drawText(FontType.Blue, "Command Mode: ", 40, yPos2, selected: selCursorIndex == i);
				string commandModeStr = "Summoner";
				if (cursors[i].index == 1) commandModeStr = "Puppeteer";
				if (cursors[i].index == 2) commandModeStr = "Striker";
				if (cursors[i].index == 3) commandModeStr = "Tag Team";
				Fonts.drawText(FontType.Blue, commandModeStr, startX2 + 8, yPos2, selected: selCursorIndex == i);
				continue;
			}

			Fonts.drawText(FontType.Blue, "Maverick " + (i + 1), 40, yPos, selected: selCursorIndex == i);

			for (int j = 0; j < cursors[i].numWeapons(); j++) {
				int jIndex = j + cursors[i].startOffset();
				Global.sprites["hud_weapon_icon"].drawToHUD(
					weapons[jIndex].weaponSlotIndex,
					startX2 + (j * wepW), startY + (i * wepH)
				);
				if (cursors[i].index != jIndex) {
					DrawWrappers.DrawRectWH(
						startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14, true,
						Helpers.FadedIconColor, 1, ZIndex.HUD, false
					);
				}
			}

			if (Global.frameCount % 60 < 30) {
				Fonts.drawText(FontType.Blue, ">", rightArrowPos, yPos, selected: selCursorIndex == i);
				Fonts.drawText(FontType.Blue, "<", leftArrowPos, yPos, selected: selCursorIndex == i);
			}

		}

		int wsx = 32;
		int wsy = 128;
		if (selCursorIndex < 2) {
			int wi = cursors[selCursorIndex].index;
			int weaponIcon = getMaverickIcon(wi);
			Weapon[] strongAgainstIndices = getStrongAgainstFrameIndices(wi);
			Weapon[] weakAgainstIndices = getWeakAgainstFrameIndices(wi);

			DrawWrappers.DrawRect(
				wsx, wsy - 8, Global.screenW - wsx, wsy + 48 + 8, true,
				new Color(0, 0, 0, 100), 1, ZIndex.HUD, false, outlineColor: outlineColor
			);
			Global.sprites["hud_maverick"].drawToHUD(weaponIcon, startX, wsy+3);

			Fonts.drawText(FontType.Orange, getMaverickName(wi), startX + 48 + 8, wsy+1);
			Fonts.drawText(FontType.Purple, getMaverickTitle(wi), startX + 48 + 8, wsy + 13);
			Fonts.drawText(FontType.Green, "Strong aganist:", startX + 48 + 8, wsy + 27);

			for (int i = 0; i < strongAgainstIndices.Length; i++) {
				int drawIndex = strongAgainstIndices[i].weaponSlotIndex;
				if (drawIndex == 0) {
					drawIndex = 118;
				}
				Global.sprites["hud_weapon_icon"].drawToHUD(
					drawIndex,
					startX + 152 + i * 16 + 8,
					wsy + 30
				);
			}
			int weakOffset = Fonts.measureText(FontType.Green, "Strong aganist:");
			Fonts.drawText(
				FontType.Green, "Weak against:",
				startX + weakOffset + 45, wsy + 42, Alignment.Right
			);
			for (int i = 0; i < weakAgainstIndices.Length; i++) {
				int drawIndex = weakAgainstIndices[i].weaponSlotIndex;
				if (drawIndex == 0) {
					drawIndex = 118;
				}
				Global.sprites["hud_weapon_icon"].drawToHUD(
					drawIndex,
					startX + 152 + i * 16 + 8,
					wsy + 46
				);
			}
		} else if (selCursorIndex == 2) {
			DrawWrappers.DrawRect(
				wsx, wsy - 8, Global.screenW - wsx, wsy + 48 + 8, true,
				new Color(0, 0, 0, 100), 1, ZIndex.HUD, false, outlineColor: outlineColor
			);
			string title = cursors[2].index switch {
				0 => "Commander Sigma",
				1 => "Neo Sigma",
				2 => "Dopple Sigma",
				_ => "ERROR"
			};
			string text = cursors[2].index switch {
				0 => "Sigma original body created by Dr. Cain.\nBalanced in melee/range and offense/defense.",
				1 => "Agile body create by Serges.\nHighly offensive with short range.",
				2 => "Battle body created by Dr. Doppler.\nHighly defensive with long range.",
				_ => "ERROR"
			};
			string hypermode = cursors[2].index switch {
				0 => "Hyper Mode: Wolf Sigma",
				1 => "Hyper Mode: Viral Sigma",
				2 or 3 => "Hyper Mode: Kaiser Sigma",
				_ => "ERROR"
			};
			Fonts.drawText(FontType.Purple, title, startX, wsy);
			Fonts.drawText(FontType.Green, text, startX, wsy + 16);
			Fonts.drawText(FontType.Orange, hypermode, startX, wsy + 40);
		} else if (selCursorIndex == 3) {
			DrawWrappers.DrawRect(
				wsx, wsy - 8, Global.screenW - wsx, wsy + 48 + 8, true,
				new Color(0, 0, 0, 100), 1, ZIndex.HUD, false, outlineColor: outlineColor
			);
			string cost = cursors[3].index switch {
				0 => $"3 {Global.nameCoins}",
				1 => $"3 {Global.nameCoins}",
				2 => "Free",
				3 => $"5 {Global.nameCoins}",
				_ => "ERROR"
			};
			string title = cursors[3].index switch {
				0 => "Summoner",
				1 => "Puppteer",
				2 => "Striker",
				3 => "Tag Team",
				_ => "ERROR"
			};
			string text = cursors[3].index switch {
				0 => "Mavericks will attack on their own.",
				1 => "Mavericks need be controlled manually.",
				2 => "Mavericks will do one attack, then leave.",
				3 => "Mavericks will swap out with Sigma.",
				_ => ""
			};

			Fonts.drawText(FontType.Purple, title, startX, wsy);
			Fonts.drawText(FontType.Green, text, startX, wsy + 16);
			Fonts.drawText(FontType.Orange, "Maverick cost: " + cost, startX, wsy + 40);
			Fonts.drawText(FontType.Orange, "Maverick cost: " + cost, startX, wsy + 40);
		}

		//Helpers.drawTextStd(Helpers.menuControlText("Left/Right: Change Weapon/Mode"), Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 18);
		//Helpers.drawTextStd(Helpers.menuControlText("Up/Down: Change Slot"), Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 18);
		//Helpers.drawTextStd(Helpers.menuControlText("WeaponL/WeaponR: Quick cycle X1/X2/X3 weapons"), Global.screenW * 0.5f, 205, Alignment.Center, fontSize: 18);
		//string helpText = Helpers.menuControlText("[BACK]: Back, [OK]: Confirm");
		//if (!inGame) helpText = Helpers.menuControlText("[BACK]: Save and back");
		//Helpers.drawTextStd(helpText, Global.screenW * 0.5f, 210 + botOffY, Alignment.Center, fontSize: 18);

		if (!string.IsNullOrEmpty(error)) {
			float top = Global.screenH * 0.4f;
			DrawWrappers.DrawRect(
				17, 17, Global.screenW - 17, Global.screenH - 17, true,
				new Color(0, 0, 0, 224), 0, ZIndex.HUD, false
			);
			Fonts.drawText(FontType.Red, "ERROR", Global.screenW / 2, top - 20, alignment: Alignment.Center);
			Fonts.drawText(FontType.RedishOrange, error, Global.screenW / 2, top, alignment: Alignment.Center);
			Fonts.drawTextEX(
				FontType.Grey, Helpers.controlText("Press [OK] to continue"),
				Global.screenW / 2, 20 + top, alignment: Alignment.Center
			);
		}
	}

	private int getMaverickIcon(int wi) {
		return wi switch {
			7 => 0,
			5 => 1,
			2 => 2,
			0 => 3,
			6 => 4,
			1 => 5,
			4 => 6,
			3 => 7,
			8 => 8,
			14 => 9,
			12 => 10,
			10 => 11,
			16 => 12,
			11 => 13,
			15 => 14,
			9 => 15,
			13 => 16,
			17 => 17,
			25 => 18,
			18 => 19,
			24 => 20,
			20 => 21,
			21 => 22,
			22 => 23,
			23 => 24,
			19 => 25,
			26 => 26,
			_ => -1
		};
	}

	private string getMaverickName(int wi) {
		return wi switch {
			7 => "Chill Penguin",
			5 => "Spark Mandrill",
			2 => "Armored Armadillo",
			0 => "Launch Octopus",
			6 => "Boomerang Kuwanger",
			1 => "Sting Chameleon",
			4 => "Storm Eagle",
			3 => "Flame Mammoth",
			8 => "Velguarder",
			14 => "Wire Sponge",
			12 => "Wheel Gator",
			10 => "Bubble Crab",
			16 => "Flame Stag",
			11 => "Morph Moth",
			15 => "Magna Centipede",
			9 => "Crystal Snail",
			13 => "Overdrive Ostrich",
			17 => "Dark Zero",
			25 => "Blizzard Buffalo",
			18 => "Toxic Seahorse",
			24 => "Tunnel Rhino",
			20 => "Volt Catfish",
			21 => "Crush Crawfish",
			22 => "Neon Tiger",
			23 => "Gravity Beetle",
			19 => "Blast Hornet",
			26 => "Dr. Doppler",
			_ => "ERROR"
		};
	}
	private string getMaverickTitle(int wi) {
		return wi switch {
			7 => "Glacial Emperor",
			5 => "Quick-Fisted King of Lightning",
			2 => "Armored Warrior",
			0 => "General of The Deep Sea",
			6 => "Blade Demon of Space and Time",
			1 => "Frightening Forest's Strike",
			4 => "Nobleman of The Skies",
			3 => "Blazing Oil Tank", 
			8 => "Guardian of the underworld",
			14 => "Little Forest Demon",
			12 => "Fanged Heavy Tank",
			10 => "Shredder of The Deep",
			16 => "Heat Knuckle Champion",
			11 => "Fallen Angel from The Islands of Dreams",
			15 => "Crimson Assassin",
			9 => "Crystal Ball Magician",
			13 => "Swift Runner of The Sands",
			17 => "Shadow doppelganger",
			25 => "Silver Snowman",
			18 => "President of The Water Dragons",
			24 => "Subterranean Barbarian",
			20 => "Rescue Power Plant",
			21 => "Destroyer of The Seven Seas",
			22 => "Protector of The Jungle",
			23 => "Steel Revenger",
			19 => "Flying Shadow Ninja",
			26 => "Dark Ambitionist",
			_ => "ERROR"
		};
	}

	private Weapon[] getWeakAgainstFrameIndices(int wi) {
		return wi switch {
			7 => [FlameMammothWeapon.netWeapon, FireWave.netWeapon],
			5 => [ChillPenguinWeapon.netWeapon, ShotgunIce.netWeapon],
			2 => [SparkMandrillWeapon.netWeapon, ElectricSpark.netWeapon],
			0 => [ArmoredArmadilloWeapon.netWeapon, RollingShield.netWeapon],
			6 => [LaunchOctopusWeapon.netWeapon, HomingTorpedo.netWeapon],
			1 => [BoomerangKuwangerWeapon.netWeapon, BoomerangCutter.netWeapon],
			4 => [StingChameleonWeapon.netWeapon, ChameleonSting.netWeapon],
			3 => [StormEagleWeapon.netWeapon, StormTornado.netWeapon],
			8 => [ChillPenguinWeapon.netWeapon, ShotgunIce.netWeapon],
			14 => [OverdriveOstrichWeapon.netWeapon, SonicSlicer.netWeapon],
			12 => [WireSpongeWeapon.netWeapon, StrikeChain.netWeapon],
			10 => [WheelGatorWeapon.netWeapon, SpinWheel.netWeapon],
			16 => [BubbleCrabWeapon.netWeapon, BubbleSplash.netWeapon],
			11 => [FlameStagWeapon.netWeapon, SpeedBurner.netWeapon],
			15 => [MorphMothWeapon.netWeapon, SilkShot.netWeapon],
			9 => [MagnaCentipedeWeapon.netWeapon, MagnetMine.netWeapon],
			13 => [CrystalSnailWeapon.netWeapon, CrystalHunter.netWeapon],
			17 => [FlameStagWeapon.netWeapon, SpeedBurner.netWeapon],
			25 => [BlastHornetWeapon.netWeapon, ParasiticBomb.netWeapon],
			18 => [BlizzardBuffaloWeapon.netWeapon, FrostShield.netWeapon],
			24 => [ToxicSeahorseWeapon.netWeapon, AcidBurst.netWeapon],
			20 => [TunnelRhinoWeapon.netWeapon, TornadoFang.netWeapon],
			21 => [VoltCatfishWeapon.netWeapon, TriadThunder.netWeapon],
			22 => [CrushCrawfishWeapon.netWeapon, SpinningBlade.netWeapon],
			23 => [NeonTigerWeapon.netWeapon, RaySplasher.netWeapon],
			19 => [GravityBeetleWeapon.netWeapon, GravityWell.netWeapon],
			26 => [ToxicSeahorseWeapon.netWeapon, AcidBurst.netWeapon],
			_ => [Weapon.baseNetWeapon]
		};
	}

	private Weapon[] getStrongAgainstFrameIndices(int wi) {
		return wi switch {
			7 => [SparkMandrillWeapon.netWeapon, ElectricSpark.netWeapon, VelguarderWeapon.netWeapon],
			5 => [ArmoredArmadilloWeapon.netWeapon, RollingShield.netWeapon],
			2 => [LaunchOctopusWeapon.netWeapon, HomingTorpedo.netWeapon],
			0 => [BoomerangKuwangerWeapon.netWeapon, BoomerangCutter.netWeapon],
			6 => [StingChameleonWeapon.netWeapon, ChameleonSting.netWeapon],
			1 => [StormEagleWeapon.netWeapon, StormTornado.netWeapon],
			4 => [FlameMammothWeapon.netWeapon, FireWave.netWeapon],
			3 => [ChillPenguinWeapon.netWeapon, ShotgunIce.netWeapon],
			8 => [Weapon.baseNetWeapon],
			14 => [WheelGatorWeapon.netWeapon, SpinWheel.netWeapon],
			12 => [BubbleCrabWeapon.netWeapon, BubbleSplash.netWeapon],
			10 => [FlameStagWeapon.netWeapon, SpeedBurner.netWeapon],
			16 => [MorphMothWeapon.netWeapon, SilkShot.netWeapon, FakeZeroWeapon.netWeapon],
			11 => [MagnaCentipedeWeapon.netWeapon, MagnetMine.netWeapon],
			15 => [CrystalSnailWeapon.netWeapon, CrystalHunter.netWeapon],
			9 => [OverdriveOstrichWeapon.netWeapon, SonicSlicer.netWeapon],
			13 => [WireSpongeWeapon.netWeapon, StrikeChain.netWeapon],
			17 => [Weapon.baseNetWeapon],
			25 => [ToxicSeahorseWeapon.netWeapon, AcidBurst.netWeapon],
			18 => [TunnelRhinoWeapon.netWeapon, TornadoFang.netWeapon, DrDopplerWeapon.netWeapon],
			24 => [VoltCatfishWeapon.netWeapon, TriadThunder.netWeapon],
			20 => [CrushCrawfishWeapon.netWeapon, SpinningBlade.netWeapon],
			21 => [NeonTigerWeapon.netWeapon, RaySplasher.netWeapon],
			22 => [GravityBeetleWeapon.netWeapon, GravityWell.netWeapon],
			23 => [BlastHornetWeapon.netWeapon, ParasiticBomb.netWeapon],
			19 => [BlizzardBuffaloWeapon.netWeapon, FrostShield.netWeapon],
			26 => [Weapon.baseNetWeapon],
			_ => [Weapon.baseNetWeapon]
		};
	}
}
