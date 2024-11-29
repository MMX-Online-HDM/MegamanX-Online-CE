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
				Global.sprites["hud_weapon_icon"].drawToHUD(66 + jIndex, startX2 + (j * wepW), startY + (i * wepH));
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
			int[] strongAgainstIndices = getStrongAgainstFrameIndices(wi);
			int[] weakAgainstIndices = getWeakAgainstFrameIndices(wi);

			DrawWrappers.DrawRect(
				wsx, wsy - 8, Global.screenW - wsx, wsy + 48 + 8, true,
				new Color(0, 0, 0, 100), 1, ZIndex.HUD, false, outlineColor: outlineColor
			);
			Global.sprites["hud_maverick"].drawToHUD(wi, startX, wsy);

			Fonts.drawText(FontType.Purple, getMaverickName(wi), startX + 48 + 8, wsy);
			Fonts.drawText(FontType.Green, "Strong aganist:", startX + 48 + 8, wsy + 16);

			for (int i = 0; i < strongAgainstIndices.Length; i++) {
				int drawIndex = strongAgainstIndices[i];
				if (strongAgainstIndices[i] == 0) {
					drawIndex = 118;
				}
				Global.sprites["hud_weapon_icon"].drawToHUD(
					drawIndex,
					startX + 152 + i * 16 + 8,
					wsy + 18
				);
			}
			int weakOffset = Fonts.measureText(FontType.Green, "Strong aganist:");
			Fonts.drawText(
				FontType.Green, "Weak against:",
				startX + weakOffset + 48 + 8, wsy + 32, Alignment.Right
			);
			for (int i = 0; i < weakAgainstIndices.Length; i++) {
				int drawIndex = weakAgainstIndices[i];
				if (weakAgainstIndices[i] == 0) {
					drawIndex = 118;
				}
				Global.sprites["hud_weapon_icon"].drawToHUD(
					drawIndex,
					startX + 152 + i * 16 + 8,
					wsy + 34
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

	private string getMaverickName(int wi) {
		return wi switch {
			0 => "Chill Penguin",
			1 => "Spark Mandrill",
			2 => "Armored Armadillo",
			3 => "Launch Octopus",
			4 => "Boomerang Kuwanger",
			5 => "Sting Chameleon",
			6 => "Storm Eagle",
			7 => "Flame Mammoth",
			8 => "Velguarder",
			9 => "Wire Sponge",
			10 => "Wheel Gator",
			11 => "Bubble Crab",
			12 => "Flame Stag",
			13 => "Morph Moth",
			14 => "Magna Centipede",
			15 => "Crystal Snail",
			16 => "Overdrive Ostrich",
			17 => "Fake Zero",
			18 => "Blizzard Buffalo",
			19 => "Toxic Seahorse",
			20 => "Tunnel Rhino",
			21 => "Volt Catfish",
			22 => "Crush Crawfish",
			23 => "Neon Tiger",
			24 => "Gravity Beetle",
			25 => "Blast Hornet",
			26 => "Dr. Doppler",
			_ => ""
		};
	}

	private int[] getWeakAgainstFrameIndices(int wi) {
		return wi switch {
			0 => new int[] { new FlameMammothWeapon(null).weaponSlotIndex, new FireWave().weaponSlotIndex },
			1 => new int[] { new ChillPenguinWeapon(null).weaponSlotIndex, new ShotgunIce().weaponSlotIndex },
			2 => new int[] { new SparkMandrillWeapon(null).weaponSlotIndex, new ElectricSpark().weaponSlotIndex },
			3 => new int[] { new ArmoredArmadilloWeapon(null).weaponSlotIndex, new RollingShield().weaponSlotIndex },
			4 => new int[] { new LaunchOctopusWeapon(null).weaponSlotIndex, new HomingTorpedo().weaponSlotIndex },
			5 => new int[] { new BoomerangKuwangerWeapon(null).weaponSlotIndex, new BoomerangCutter().weaponSlotIndex },
			6 => new int[] { new StingChameleonWeapon(null).weaponSlotIndex, new ChameleonSting().weaponSlotIndex },
			7 => new int[] { new StormEagleWeapon(null).weaponSlotIndex, new StormTornado().weaponSlotIndex },
			8 => new int[] { new ChillPenguinWeapon(null).weaponSlotIndex, new ShotgunIce().weaponSlotIndex },
			9 => new int[] { new OverdriveOstrichWeapon(null).weaponSlotIndex, new SonicSlicer().weaponSlotIndex },
			10 => new int[] { new WireSpongeWeapon(null).weaponSlotIndex, new StrikeChain().weaponSlotIndex },
			11 => new int[] { new WheelGatorWeapon(null).weaponSlotIndex, new SpinWheel().weaponSlotIndex },
			12 => new int[] { new BubbleCrabWeapon(null).weaponSlotIndex, new BubbleSplash().weaponSlotIndex },
			13 => new int[] { new FlameStagWeapon(null).weaponSlotIndex, new SpeedBurner().weaponSlotIndex },
			14 => new int[] { new MorphMothWeapon(null).weaponSlotIndex, new SilkShot().weaponSlotIndex },
			15 => new int[] { new MagnaCentipedeWeapon(null).weaponSlotIndex, new MagnetMine().weaponSlotIndex },
			16 => new int[] { new CrystalSnailWeapon(null).weaponSlotIndex, new CrystalHunter().weaponSlotIndex },
			17 => new int[] { new FlameStagWeapon(null).weaponSlotIndex, new SpeedBurner().weaponSlotIndex },
			18 => new int[] { new BlastHornetWeapon(null).weaponSlotIndex, new ParasiticBomb().weaponSlotIndex },
			19 => new int[] { new BlizzardBuffaloWeapon(null).weaponSlotIndex, new FrostShield().weaponSlotIndex },
			20 => new int[] { new ToxicSeahorseWeapon(null).weaponSlotIndex, new AcidBurst().weaponSlotIndex },
			21 => new int[] { new TunnelRhinoWeapon(null).weaponSlotIndex, new TornadoFang().weaponSlotIndex },
			22 => new int[] { new VoltCatfishWeapon(null).weaponSlotIndex, new TriadThunder().weaponSlotIndex },
			23 => new int[] { new CrushCrawfishWeapon(null).weaponSlotIndex, new SpinningBlade().weaponSlotIndex },
			24 => new int[] { new NeonTigerWeapon(null).weaponSlotIndex, new RaySplasher().weaponSlotIndex },
			25 => new int[] { new GravityBeetleWeapon(null).weaponSlotIndex, new GravityWell().weaponSlotIndex },
			26 => new int[] { new ToxicSeahorseWeapon(null).weaponSlotIndex, new AcidBurst().weaponSlotIndex },
			_ => new int[] { 0 }
		};
	}

	private int[] getStrongAgainstFrameIndices(int wi) {
		return wi switch {
			0 => new int[] {
				new SparkMandrillWeapon(null).weaponSlotIndex,
				new ElectricSpark().weaponSlotIndex, new VelguarderWeapon(null).weaponSlotIndex
			},
			1 => new int[] {
				new ArmoredArmadilloWeapon(null).weaponSlotIndex, new RollingShield().weaponSlotIndex
			},
			2 => new int[] {
				new LaunchOctopusWeapon(null).weaponSlotIndex, new HomingTorpedo().weaponSlotIndex
			},
			3 => new int[] { new BoomerangKuwangerWeapon(null).weaponSlotIndex, new BoomerangCutter().weaponSlotIndex },
			4 => new int[] { new StingChameleonWeapon(null).weaponSlotIndex, new ChameleonSting().weaponSlotIndex },
			5 => new int[] { new StormEagleWeapon(null).weaponSlotIndex, new StormTornado().weaponSlotIndex },
			6 => new int[] { new FlameMammothWeapon(null).weaponSlotIndex, new FireWave().weaponSlotIndex },
			7 => new int[] { new ChillPenguinWeapon(null).weaponSlotIndex, new ShotgunIce().weaponSlotIndex },
			9 => new int[] { new WheelGatorWeapon(null).weaponSlotIndex, new SpinWheel().weaponSlotIndex },
			10 => new int[] { new BubbleCrabWeapon(null).weaponSlotIndex, new BubbleSplash().weaponSlotIndex },
			11 => new int[] { new FlameStagWeapon(null).weaponSlotIndex, new SpeedBurner().weaponSlotIndex },
			12 => new int[] {
				new MorphMothWeapon(null).weaponSlotIndex, new SilkShot().weaponSlotIndex,
				new FakeZeroWeapon(null).weaponSlotIndex
			},
			13 => new int[] { new MagnaCentipedeWeapon(null).weaponSlotIndex, new MagnetMine().weaponSlotIndex },
			14 => new int[] { new CrystalSnailWeapon(null).weaponSlotIndex, new CrystalHunter().weaponSlotIndex },
			15 => new int[] { new OverdriveOstrichWeapon(null).weaponSlotIndex, new SonicSlicer().weaponSlotIndex },
			16 => new int[] { new WireSpongeWeapon(null).weaponSlotIndex, new StrikeChain().weaponSlotIndex },
			18 => new int[] { new ToxicSeahorseWeapon(null).weaponSlotIndex, new AcidBurst().weaponSlotIndex },
			19 => new int[] {
				new TunnelRhinoWeapon(null).weaponSlotIndex,
				new TornadoFang().weaponSlotIndex, new DrDopplerWeapon(null).weaponSlotIndex
			},
			20 => new int[] { new VoltCatfishWeapon(null).weaponSlotIndex, new TriadThunder().weaponSlotIndex },
			21 => new int[] { new CrushCrawfishWeapon(null).weaponSlotIndex, new SpinningBlade().weaponSlotIndex },
			22 => new int[] { new NeonTigerWeapon(null).weaponSlotIndex, new RaySplasher().weaponSlotIndex },
			23 => new int[] { new GravityBeetleWeapon(null).weaponSlotIndex, new GravityWell().weaponSlotIndex },
			24 => new int[] { new BlastHornetWeapon(null).weaponSlotIndex, new ParasiticBomb().weaponSlotIndex },
			25 => new int[] { new BlizzardBuffaloWeapon(null).weaponSlotIndex, new FrostShield().weaponSlotIndex },
			_ => new int[] { 0 }
		};
	}
}
