using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class WeaponCursor {
	public int index;

	public WeaponCursor(int index) {
		this.index = index;
	}
}

public class XWeaponCursor {
	public int index;

	public XWeaponCursor(int index) {
		this.index = index;
	}

	public int startOffset() {
		if (index < 9) {
			return 0;
		}
		return MathInt.Floor((index - 1) / 8f) * 8 + 1;
	}

	public int numWeapons() {
		if (index < 9) return 9;
		return 8;
	}

	public void cycleLeft() {
		if (index < 9) index = 17;
		else if (index >= 9 && index <= 16) index = 0;
		else if (index > 16) index = 9;
	}

	public void cycleRight() {
		if (index < 9) index = 9;
		else if (index >= 9 && index <= 16) index = 17;
		else if (index > 16) index = 0;
	}
}

public class SelectWeaponMenu : IMainMenu {
	public bool inGame;
	public XWeaponCursor[] cursors;
	public int selCursorIndex;
	public Point[] weaponPositions = new Point[9];
	public Weapon[] weaponList = Weapon.getAllXWeapons().ToArray();
	public int[] selectedWeaponsInts;
	public int[] selectedWeaponsIntsRound = new int[3];
	public int[] selectedWeaponsOffset = new int[3];
	public int[] oldWeaponsInts;
	public string error = "";
	public IMainMenu prevMenu;

	public SelectWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		for (int i = 0; i < 9; i++) {
			weaponPositions[i] = new Point(80, 42 + (i * 18));
		}

		selectedWeaponsInts = Options.main.xLoadout.getXWeaponIndices().ToArray();
		oldWeaponsInts = Options.main.xLoadout.getXWeaponIndices().ToArray();
		this.inGame = inGame;

		cursors = new XWeaponCursor[selectedWeaponsInts.Length + 1];
		for (int i = 0; i < selectedWeaponsInts.Length; i++) {
			cursors[i] = new XWeaponCursor(selectedWeaponsInts[i]); ;
		}
		cursors[^1] = new XWeaponCursor(Options.main.xLoadout.melee);

		for (int i = 0; i < 3; i++) {
			selectedWeaponsOffset[i] = (selectedWeaponsInts[i] - 1) % weaponList.Length;
			selectedWeaponsIntsRound[i] = selectedWeaponsInts[i];
		}
	}

	public bool duplicateWeapons() {
		return (
			selectedWeaponsInts[0] == selectedWeaponsInts[1] ||
			selectedWeaponsInts[1] == selectedWeaponsInts[2] ||
			selectedWeaponsInts[0] == selectedWeaponsInts[2]
		);
	}

	public bool areWeaponArrSame(int[] wepArr1, int[] wepArr2) {
		for (int i = 0; i < wepArr1.Length; i++) {
			if (wepArr1[i] != wepArr2[i]) {
				return false;
			}
		}
		return true;
	}

	public void update() {
		if (!string.IsNullOrEmpty(error)) {
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				error = "";
			}
			return;
		}

		if (selCursorIndex < 3) {
			int maxCursorIndex = weaponList.Length - 1;
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				cursors[selCursorIndex].index--;
				selectedWeaponsIntsRound[selCursorIndex]--;
				if (cursors[selCursorIndex].index == -1) {
					cursors[selCursorIndex].index = maxCursorIndex;
				}
				Global.playSound("menuX2");
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				cursors[selCursorIndex].index++;
				selectedWeaponsIntsRound[selCursorIndex]++;
				if (cursors[selCursorIndex].index == maxCursorIndex + 1) {
					cursors[selCursorIndex].index = 0;
				}
				Global.playSound("menuX2");
			}
		} else {
			Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, 1, playSound: true);
		}

		Helpers.menuUpDown(ref selCursorIndex, 0, 3);

		for (int i = 0; i < 3; i++) {
			selectedWeaponsInts[i] = cursors[i].index;
		}

		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		bool selectPressed = Global.input.isPressedMenu(Control.MenuConfirm) || (backPressed && !inGame);
		if (selectPressed) {
			if (duplicateWeapons()) {
				error = "Cannot select same weapon more than once!";
				return;
			}

			bool shouldSave = false;
			if (cursors[3].index != Options.main.xLoadout.melee) {
				Options.main.xLoadout.melee = cursors[3].index;
				if (Global.level?.mainPlayer != null) {
					Global.level.mainPlayer.loadout.xLoadout.melee = cursors[3].index;
					Global.level.mainPlayer.syncLoadout();
				}
				shouldSave = true;
			}

			if (!areWeaponArrSame(selectedWeaponsInts, oldWeaponsInts)) {
				Options.main.xLoadout.weapon1 = selectedWeaponsInts[0];
				Options.main.xLoadout.weapon2 = selectedWeaponsInts[1];
				Options.main.xLoadout.weapon3 = selectedWeaponsInts[2];
				shouldSave = true;
				if (inGame && Global.level != null) {
					if (Options.main.killOnLoadoutChange) {
						Global.level.mainPlayer.forceKill();
					} else if (!Global.level.mainPlayer.isDead) {
						Global.level.gameMode.setHUDErrorMessage(
							Global.level.mainPlayer, "Change will apply on next death", playSound: false
						);
					}
				}
			}

			if (shouldSave) {
				Options.main.saveToFile();
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

		Fonts.drawText(FontType.Yellow, "X Loadout", Global.screenW * 0.5f, 24, Alignment.Center);
		var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
		float botOffY = inGame ? 0 : -1;

		int startY = 45;
		int startX = 30;
		int startX2 = 64;
		int wepW = 18;
		int wepH = 20;

		float rightArrowPos = 224;
		float leftArrowPos = startX2 - 15;

		Global.sprites["cursor"].drawToHUD(0, startX - 6, startY + (selCursorIndex * wepH));
		for (int i = 0; i < 4; i++) {
			float yPos = startY - 6 + (i * wepH);

			if (i == 3) {
				Fonts.drawText(FontType.Blue, "S", 30, yPos + 2, selected: selCursorIndex == i);

				for (int j = 0; j < 2; j++) {
					if (j == 0) {
						Global.sprites["hud_weapon_icon"].drawToHUD(0, startX2 + (j * wepW), startY + (i * wepH));
					} else if (j == 1) {
						Global.sprites["hud_weapon_icon"].drawToHUD(102, startX2 + (j * wepW), startY + (i * wepH));
					}

					if (cursors[3].index != j) {
						DrawWrappers.DrawRectWH(
							startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14,
							true, Helpers.FadedIconColor, 1, ZIndex.HUD, false
						);
					}
				}
				Helpers.drawWeaponSlotSymbol(
					startX2 - 8, startY + (i * wepH) - 8, "²"
				);
				if (cursors[3].index != 0) {
					DrawWrappers.DrawRectWH(
						startX2 + 4, startY + (i * wepH) + 3, 4, 5,
						true, Helpers.FadedIconColor, 1, ZIndex.HUD, false
					);
				}
				break;
			}

			Fonts.drawText(FontType.Blue, (i + 1).ToString(), 30, yPos + 2, selected: selCursorIndex == i);

			if (Global.frameCount % 60 < 30) {
				Fonts.drawText(
					FontType.Blue, ">", cursors[i].index < 9 ? rightArrowPos : rightArrowPos - 20, yPos + 2,
					Alignment.Center, selected: selCursorIndex == i
				);
				Fonts.drawText(
					FontType.Blue, "<", leftArrowPos, yPos + 2, Alignment.Center, selected: selCursorIndex == i
				);
			}

			for (int j = 0; j < 9; j++) {
				if (selectedWeaponsIntsRound[i] <= selectedWeaponsOffset[i]) {
					selectedWeaponsOffset[i] = selectedWeaponsOffset[i] - 1;
				}
				if (selectedWeaponsIntsRound[i] >= selectedWeaponsOffset[i] + 8) {
					selectedWeaponsOffset[i] = selectedWeaponsOffset[i] + 1;
				}
				int jIndex = selectedWeaponsOffset[i] + j;
				while (jIndex < 0) {
					jIndex += weaponList.Length;
				}
				while (jIndex >= weaponList.Length) {
					jIndex -= weaponList.Length;
				}
				int displayIndex = weaponList[jIndex].weaponSlotIndex;
				Global.sprites["hud_weapon_icon"].drawToHUD(displayIndex, startX2 + (j * wepW), startY + (i * wepH));
				if (selectedWeaponsInts[i] != jIndex) {
					DrawWrappers.DrawRectWH(
						startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14, true,
						Helpers.FadedIconColor, 1, ZIndex.HUD, false
					);
				}
			}
		}

		int wsy = 162;

		DrawWrappers.DrawRect(
			25, wsy - 46, Global.screenW - 25, wsy + 30, true, new Color(0, 0, 0, 100),
			1, ZIndex.HUD, false, outlineColor: outlineColor
		); // bottom rect
		DrawWrappers.DrawRect(
			25, wsy - 46, Global.screenW - 25, wsy - 30, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: outlineColor
		); //slot 1 weapon rect
		DrawWrappers.DrawRect(
			240, 38, 359, 92, true, new Color(0, 0, 0, 100),
			1, ZIndex.HUD, false, outlineColor: outlineColor
		); // Up Right Rect
		DrawWrappers.DrawRect(
			240, 38, 359, 52, true, new Color(0, 0, 0, 100),
			1, ZIndex.HUD, false, outlineColor: outlineColor
		); // Up Right Rect
		if (selCursorIndex >= 3) {
			Fonts.drawText(
				FontType.Purple, "Special Key",
				Global.halfScreenW, 120, Alignment.Center
			);
			if (cursors[3].index == 0) {
				Fonts.drawText(FontType.Blue, "X-Buster", Global.halfScreenW, 146, Alignment.Center);
				Fonts.drawText(
					FontType.Green, "If no armor is equipped,\nSPECIAL will fire the X-Buster.",
					Global.halfScreenW, wsy, Alignment.Center
				);
			}
			if (cursors[3].index == 1) {
				Fonts.drawText(FontType.Green, "Z-Saber", Global.halfScreenW, 146, Alignment.Center);
				Fonts.drawText(
					FontType.Blue, "If no armor is equipped,\nSPECIAL will swing the Z-Saber.",
					Global.halfScreenW, wsy, Alignment.Center
				);
			}
		} else {
			int wi = selectedWeaponsInts[selCursorIndex];
			int strongAgainstIndex = weaponList.FindIndex(w => w.weaknessIndex == wi);
			var weapon = weaponList[wi];
			int weakAgainstIndex = weapon.weaknessIndex;
			int[] strongAgainstMaverickIndices = getStrongAgainstMaverickFrameIndex(wi);
			int weakAgainstMaverickIndex = getWeakAgainstMaverickFrameIndex(wi);
			string damage = weapon.damage;
			string rateOfFire = weapon.fireRate.ToString();
			string maxAmmo = weapon.maxAmmo.ToString();
			string effect = weapon.effect;
			string hitcooldown = weapon.hitcooldown;
			string flinch = weapon.flinch;
			string flinchCD = weapon.flinchCD;


			Fonts.drawText(
				FontType.Orange, weaponList[selectedWeaponsInts[selCursorIndex]].displayName,
				Global.halfScreenW, 121, Alignment.Center
			);
			Fonts.drawText(
				FontType.Orange, "Element Chart",
				303, 42, Alignment.Center
			);
			Fonts.drawText(FontType.Purple, "Counters: ", 305, 62, Alignment.Right);
			if (strongAgainstIndex > 0) {
				Global.sprites["hud_weapon_icon"].drawToHUD(strongAgainstIndex, 308, 62);
			} else {
				Fonts.drawText(FontType.Grey, "None", 300, 62);
			}
			for (int i = 0; i < strongAgainstMaverickIndices.Length; i++) {
				if (strongAgainstMaverickIndices[0] == 0) {
					continue;
				}
				Global.sprites["hud_weapon_icon"].drawToHUD(strongAgainstMaverickIndices[i], 325 + i * 17, 62);
			}
			Fonts.drawText(FontType.Red, "Weakness: ", 305, 82, Alignment.Right);
			if (weakAgainstIndex > 0) {
				Global.sprites["hud_weapon_icon"].drawToHUD(weakAgainstIndex, 308, 82);
			} else {
				Fonts.drawText(FontType.Grey, "None", 300, 82);
			}
			if (weakAgainstMaverickIndex > 0) {
				Global.sprites["hud_weapon_icon"].drawToHUD(weakAgainstMaverickIndex, 325, 82);
			}
			int basePos = 192;
			int yOffSet = 11;

			
			DrawWrappers.DrawRect(
				25, basePos - yOffSet * 2, 359, basePos, true, new Color(0,0,0,100), 1, ZIndex.HUD, false
			);
			DrawWrappers.DrawRect(25, basePos - yOffSet, 137, basePos, false, outlineColor, 1, ZIndex.HUD, false);
			DrawWrappers.DrawRect(138, basePos - yOffSet, 260, basePos, false, outlineColor, 1, ZIndex.HUD, false);
			DrawWrappers.DrawRect(261, basePos - yOffSet, 359, basePos, false, outlineColor, 1, ZIndex.HUD, false);
			basePos -= yOffSet + 1;
			DrawWrappers.DrawRect(25, basePos - yOffSet, 106, basePos, false, outlineColor, 1, ZIndex.HUD, false);
			DrawWrappers.DrawRect(107, basePos - yOffSet, 209, basePos, false, outlineColor, 1, ZIndex.HUD, false);
			DrawWrappers.DrawRect(210, basePos - yOffSet, 359, basePos, false, outlineColor, 1, ZIndex.HUD, false);

			Fonts.drawText(FontType.RedishOrange, "Damage: " + damage, 27, 183);
			Fonts.drawText(FontType.Yellow, "Flinch: " + flinch, 139, 183);
			Fonts.drawText(FontType.Orange, "FCD: " + flinchCD, 262, 183);

			Fonts.drawText(FontType.Blue, "Ammo: " + maxAmmo, 27, 171);
			Fonts.drawText(FontType.Purple, "Fire Rate: " + rateOfFire, 108, 171);
			Fonts.drawText(FontType.Pink, "Hit CD: " + hitcooldown, 212, 171);

			Fonts.drawText(FontType.Green, effect, 27, 136);
			if (weapon is XBuster) {
				effect = (ArmorId)(Global.level?.mainPlayer.armArmorNum ?? 0) switch {
					ArmorId.Light => "Mega Buster Mark 17 with Spiral Shot.",
					ArmorId.Giga => "Mega Buster Mark 17 with Double Shot.",
					ArmorId.Max => "Mega Buster Mark 17 with Cross Shot.",
					_ => "Mega Buster Mark 17 baseline form.",
				};
				if (Global.level?.mainPlayer.character is MegamanX mmx && mmx.hasUltimateArmor == true) {
					effect = "Mega Buster Mark 17 with Plasma Shot.";
				}
				Fonts.drawText(FontType.Green, effect, 27, 136);
			}
		}

		/*
		Helpers.drawTextStd(Helpers.menuControlText("Left/Right: Change Weapon"), Global.screenW * 0.5f, 195 + botOffY, Alignment.Center, fontSize: 16);
		Helpers.drawTextStd(Helpers.menuControlText("Up/Down: Change Slot"), Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 16);
		Helpers.drawTextStd(Helpers.menuControlText("WeaponL/WeaponR: Quick cycle X1/X2/X3 weapons"), Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 16);
		string helpText = Helpers.menuControlText("[BACK]: Back, [OK]: Confirm");
		if (!inGame) helpText = Helpers.menuControlText("[BACK]: Save and back");
		Helpers.drawTextStd(helpText, Global.screenW * 0.5f, 210 + botOffY, Alignment.Center, fontSize: 16);
		*/
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

	private int getWeakAgainstMaverickFrameIndex(int wi) {
		switch (wi) {
			case (int)WeaponIds.HomingTorpedo:
				return new ArmoredArmadilloWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.ChameleonSting:
				return new BoomerangKuwangerWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.RollingShield:
				return new SparkMandrillWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.FireWave:
				return new StormEagleWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.StormTornado:
				return new StingChameleonWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.ElectricSpark:
				return new ChillPenguinWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.BoomerangCutter:
				return new LaunchOctopusWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.ShotgunIce:
				return new FlameMammothWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.StrikeChain:
				return new OverdriveOstrichWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.SpinWheel:
				return new WireSpongeWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.BubbleSplash:
				return new WheelGatorWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.SpeedBurner:
				return new BubbleCrabWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.SilkShot:
				return new FlameStagWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.MagnetMine:
				return new MorphMothWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.CrystalHunter:
				return new MagnaCentipedeWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.SonicSlicer:
				return new CrystalSnailWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.AcidBurst:
				return new BlizzardBuffaloWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.ParasiticBomb:
				return new GravityBeetleWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.TriadThunder:
				return new TunnelRhinoWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.SpinningBlade:
				return new VoltCatfishWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.RaySplasher:
				return new CrushCrawfishWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.GravityWell:
				return new NeonTigerWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.FrostShield:
				return new BlastHornetWeapon(null).weaponSlotIndex;
			case (int)WeaponIds.TornadoFang:
				return new ToxicSeahorseWeapon(null).weaponSlotIndex;
			default:
				return 0;
		}
	}

	private int[] getStrongAgainstMaverickFrameIndex(int weaponIndex) {
		return weaponIndex switch {
			(int)WeaponIds.HomingTorpedo => new int[] { new BoomerangKuwangerWeapon(null).weaponSlotIndex },
			(int)WeaponIds.ChameleonSting => new int[] { new StormEagleWeapon(null).weaponSlotIndex },
			(int)WeaponIds.RollingShield => new int[] { new LaunchOctopusWeapon(null).weaponSlotIndex },
			(int)WeaponIds.FireWave => new int[] { new ChillPenguinWeapon(null).weaponSlotIndex },
			(int)WeaponIds.StormTornado => new int[] { new FlameMammothWeapon(null).weaponSlotIndex },
			(int)WeaponIds.ElectricSpark => new int[] { new ArmoredArmadilloWeapon(null).weaponSlotIndex },
			(int)WeaponIds.BoomerangCutter => new int[] { new StingChameleonWeapon(null).weaponSlotIndex },
			(int)WeaponIds.ShotgunIce => new int[] {
				new SparkMandrillWeapon(null).weaponSlotIndex,
				new VelguarderWeapon(null).weaponSlotIndex
			},
			(int)WeaponIds.StrikeChain => new int[] { new WheelGatorWeapon(null).weaponSlotIndex },
			(int)WeaponIds.SpinWheel => new int[] { new BubbleCrabWeapon(null).weaponSlotIndex },
			(int)WeaponIds.BubbleSplash => new int[] { new FlameStagWeapon(null).weaponSlotIndex },
			(int)WeaponIds.SpeedBurner => new int[] {
				new MorphMothWeapon(null).weaponSlotIndex,
				new FakeZeroWeapon(null).weaponSlotIndex
			},
			(int)WeaponIds.SilkShot => new int[] { new MagnaCentipedeWeapon(null).weaponSlotIndex },
			(int)WeaponIds.MagnetMine => new int[] { new CrystalSnailWeapon(null).weaponSlotIndex },
			(int)WeaponIds.CrystalHunter => new int[] { new OverdriveOstrichWeapon(null).weaponSlotIndex },
			(int)WeaponIds.SonicSlicer => new int[] { new WireSpongeWeapon(null).weaponSlotIndex },
			(int)WeaponIds.AcidBurst => new int[] {
				new TunnelRhinoWeapon(null).weaponSlotIndex,
				new DrDopplerWeapon(null).weaponSlotIndex
			},
			(int)WeaponIds.ParasiticBomb => new int[] { new BlizzardBuffaloWeapon(null).weaponSlotIndex },
			(int)WeaponIds.TriadThunder => new int[] { new CrushCrawfishWeapon(null).weaponSlotIndex },
			(int)WeaponIds.SpinningBlade => new int[] { new NeonTigerWeapon(null).weaponSlotIndex },
			(int)WeaponIds.RaySplasher => new int[] { new GravityBeetleWeapon(null).weaponSlotIndex },
			(int)WeaponIds.GravityWell => new int[] { new BlastHornetWeapon(null).weaponSlotIndex },
			(int)WeaponIds.FrostShield => new int[] { new ToxicSeahorseWeapon(null).weaponSlotIndex },
			(int)WeaponIds.TornadoFang => new int[] { new VoltCatfishWeapon(null).weaponSlotIndex },
			_ => new int[] { }
		};
	}
}
