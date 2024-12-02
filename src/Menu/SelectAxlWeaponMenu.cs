using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class AltFireData {
	public int index;
	public string alt1Name;
	public string alt2Name;
	public AltFireData(int index, string alt1Name, string alt2Name) {
		this.index = index;
		this.alt1Name = alt1Name;
		this.alt2Name = alt2Name;
	}
}

public class SelectAxlWeaponMenu : IMainMenu {
	public List<WeaponCursor> cursors;
	public int selCursorIndex;
	public List<Point> weaponPositions = new List<Point>();
	public string error = "";
	public int maxRows = 1;
	public int maxCols = 9;
	public bool inGame;
	public static List<string> weaponNames = new List<string>()
	{
			"Axl Bullets",
			"Ray Gun",
			"Blast Launcher",
			"Black Arrow",
			"Spiral Magnum",
			"Bound Blaster",
			"Plasma Gun",
			"Ice Gattling",
			"Flame Burner"
		};
	public static List<AltFireData> altFireDatas = new List<AltFireData>()
	{
			new AltFireData(0, "Copy Shot", "N/A"),
			new AltFireData(1, "Splash Laser", "Charge Beam"),
			new AltFireData(2, "Shockwave", "Detonate"),
			new AltFireData(3, "Wind Cutter", "Triple Arrow"),
			new AltFireData(4, "Sniper Missile", "Zoom Scope"),
			new AltFireData(5, "Moving Wheel", "Sonar Beacon"),
			new AltFireData(6, "Volt Tornado", "Plasma Beam"),
			new AltFireData(7, "A. Gaea Shield", "Rev Minigun"),
			new AltFireData(8, "Circle Blaze", "Air Blast"),
		};
	public static string getAltFireDesc(int index, int altFireNum) {
		if (altFireNum == 0) return altFireDatas[index].alt1Name;
		return altFireDatas[index].alt2Name;
	}

	public List<Weapon> craftableWeapons;
	public static List<List<string>> craftingRecipes = new List<List<string>>()
	{
			null,
			new List<string>() { "S3", "03", "02S1", "01S2" },
			new List<string>() { "S3", "13", "12S1", "11S2" },
			new List<string>() { "S3", "23", "22S1", "21S2" },
			new List<string>() { "S3", "33", "32S1", "31S2" },
			new List<string>() { "S3", "43", "42S1", "41S2" },
			new List<string>() { "S:", "0111213141" },
		};
	public static List<int> craftingRecipeSelections = new List<int>()
	{
			0,
			0,
			0,
			0,
			0,
			0,
			0
		};
	public Player mainPlayer {
		get {
			return Global.level?.mainPlayer;
		}
	}

	public bool altCustomizeDirty;
	public List<int> altCustomizeArray;
	public List<int> selectedWeaponIndices;
	public IMainMenu prevMenu;

	public SelectAxlWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		for (int i = 0; i < 9; i++) {
			weaponPositions.Add(new Point(80, 42 + (i * 18)));
		}

		selectedWeaponIndices = Options.main.axlLoadout.getAxlWeaponFIs();


		if (Global.level?.mainPlayer == null) {
			craftableWeapons = new List<Weapon>()
			{
					new AxlBullet(AxlBulletWeaponType.AxlBullets),
				};
		} else {
			craftableWeapons = new List<Weapon>()
			{
					new AxlBullet(AxlBulletWeaponType.AxlBullets),
					new MettaurCrash(),
					new BeastKiller(),
					new MachineBullets(),
					new DoubleBullet(),
					new RevolverBarrel(),
					new AncientGun(),
				};
			selectedWeaponIndices[0] = mainPlayer.axlBulletType;
		}

		cursors = new List<WeaponCursor>();
		foreach (var selectedWeaponIndex in selectedWeaponIndices) {
			cursors.Add(new WeaponCursor(selectedWeaponIndex));
		}
		cursors.Add(new WeaponCursor(Options.main.axlLoadout.hyperMode));

		altCustomizeArray = new List<int>(Options.main.axlLoadout.altFireArray);
	}

	public bool duplicateWeapons() {
		return selectedWeaponIndices[1] == selectedWeaponIndices[2];
	}

	public bool areWeaponArrSame(List<int> wepArr1, List<int> wepArr2) {
		for (int i = 1; i < wepArr1.Count; i++) {
			if (wepArr1[i] != wepArr2[i]) return false;
		}

		return true;
	}

	public void recipeManager(
		string recipe, bool buyIfCanAfford, out bool canAfford,
		out int missingCurrency, out int[] missingCores
	) {
		canAfford = true;
		missingCurrency = 0;
		int missingXCores = 0;
		int missingZeroCores = 0;
		int missingVileCores = 0;
		int missingAxlCores = 0;
		int missingSigmaCores = 0;

		int currencyRequired = 0;
		int xCoresRequired = 0;
		int zeroCoresRequired = 0;
		int vileCoresRequired = 0;
		int axlCoresRequired = 0;
		int sigmaCoresRequired = 0;

		for (int i = 0; i < recipe.Length - 1; i += 2) {
			if (recipe[i] == 'S') currencyRequired = recipe[i + 1] - '0';
			if (recipe[i] == '0') xCoresRequired = recipe[i + 1] - '0';
			if (recipe[i] == '1') zeroCoresRequired = recipe[i + 1] - '0';
			if (recipe[i] == '2') vileCoresRequired = recipe[i + 1] - '0';
			if (recipe[i] == '3') axlCoresRequired = recipe[i + 1] - '0';
			if (recipe[i] == '4') sigmaCoresRequired = recipe[i + 1] - '0';
		}

		if (mainPlayer.currency < currencyRequired) {
			canAfford = false;
			missingCurrency = currencyRequired - mainPlayer.currency;
		}

		int xDnaCount = mainPlayer.weapons.Count(w => w is DNACore dnaCore && dnaCore.charNum == 0);
		if (xDnaCount < xCoresRequired) {
			canAfford = false;
			missingXCores = xCoresRequired - xDnaCount;
		}

		int zeroDnaCount = mainPlayer.weapons.Count(w => w is DNACore dnaCore && dnaCore.charNum == 1);
		if (zeroDnaCount < zeroCoresRequired) {
			canAfford = false;
			missingZeroCores = xCoresRequired - zeroDnaCount;
		}

		int vileDnaCount = mainPlayer.weapons.Count(w => w is DNACore dnaCore && dnaCore.charNum == 2);
		if (vileDnaCount < vileCoresRequired) {
			canAfford = false;
			missingVileCores = xCoresRequired - vileDnaCount;
		}

		int axlDnaCount = mainPlayer.weapons.Count(w => w is DNACore dnaCore && dnaCore.charNum == 3);
		if (axlDnaCount < axlCoresRequired) {
			canAfford = false;
			missingAxlCores = axlCoresRequired - axlDnaCount;
		}

		int sigmaDnaCount = mainPlayer.weapons.Count(w => w is DNACore dnaCore && dnaCore.charNum == 4);
		if (sigmaDnaCount < sigmaCoresRequired) {
			canAfford = false;
			missingSigmaCores = sigmaCoresRequired - sigmaDnaCount;
		}

		if (buyIfCanAfford && canAfford) {
			mainPlayer.currency -= currencyRequired;
			for (int i = 0; i < xCoresRequired; i++) {
				mainPlayer.weapons.RemoveAt(mainPlayer.weapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 0));
				int removeIndex = mainPlayer.savedDNACoreWeapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 0);
				if (removeIndex >= 0) mainPlayer.savedDNACoreWeapons.RemoveAt(removeIndex);
			}
			for (int i = 0; i < zeroCoresRequired; i++) {
				mainPlayer.weapons.RemoveAt(mainPlayer.weapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 1));
				int removeIndex = mainPlayer.savedDNACoreWeapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 1);
				if (removeIndex >= 0) mainPlayer.savedDNACoreWeapons.RemoveAt(removeIndex);
			}
			for (int i = 0; i < vileCoresRequired; i++) {
				mainPlayer.weapons.RemoveAt(mainPlayer.weapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 2));
				int removeIndex = mainPlayer.savedDNACoreWeapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 2);
				if (removeIndex >= 0) mainPlayer.savedDNACoreWeapons.RemoveAt(removeIndex);
			}
			for (int i = 0; i < axlCoresRequired; i++) {
				mainPlayer.weapons.RemoveAt(mainPlayer.weapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 3));
				int removeIndex = mainPlayer.savedDNACoreWeapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 3);
				if (removeIndex >= 0) mainPlayer.savedDNACoreWeapons.RemoveAt(removeIndex);
			}
			for (int i = 0; i < sigmaCoresRequired; i++) {
				mainPlayer.weapons.RemoveAt(mainPlayer.weapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 4));
				int removeIndex = mainPlayer.savedDNACoreWeapons.FindIndex(w => w is DNACore dnaCore && dnaCore.charNum == 4);
				if (removeIndex >= 0) mainPlayer.savedDNACoreWeapons.RemoveAt(removeIndex);
			}
		}

		missingCores = new int[] { missingXCores, missingZeroCores, missingVileCores, missingAxlCores, missingSigmaCores };
	}

	public void update() {
		if (!string.IsNullOrEmpty(error)) {
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				error = null;
			}
			return;
		}

		if (selCursorIndex == 0) {
			Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, craftableWeapons.Count - 1, wrap: true, playSound: true);

			int index = selectedWeaponIndices[0];
			if (index > 0) {
				if (Global.input.isPressedMenu(Control.WeaponLeft)) {
					craftingRecipeSelections[index]--;
					if (craftingRecipeSelections[index] < 0) craftingRecipeSelections[index] = 0;
				} else if (Global.input.isPressedMenu(Control.WeaponRight)) {
					craftingRecipeSelections[index]++;
					if (craftingRecipeSelections[index] >= craftingRecipes[index].Count) craftingRecipeSelections[index] = craftingRecipes[index].Count - 1;
				}
				if (Global.input.isPressedMenu(Control.MenuAlt)) {
					int crsIndex = craftingRecipeSelections[index];
					string recipe = craftingRecipes[index][crsIndex];
					recipeManager(recipe, true, out bool canAfford, out int _, out int[] _);
					if (canAfford) {
						if (mainPlayer.axlBulletTypeBought[index] == false) {
							mainPlayer.axlBulletTypeBought[index] = true;
							mainPlayer.axlBulletTypeAmmo[index] = 32;
						} else {
							mainPlayer.axlBulletTypeAmmo[index] += 32;
						}
						Global.playSound("ching");
					} else {
						Global.playSound("error");
					}
				}
			}
		} else if (selCursorIndex > 0 && selCursorIndex < 3) {
			Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 1, 8, wrap: true, playSound: true);

			if (Global.input.isPressedMenu(Control.MenuAlt)) {
				int wi = cursors[selCursorIndex].index;
				if (selCursorIndex > 0 && wi > 0) {
					altCustomizeArray[wi] = (altCustomizeArray[wi] == 0 ? 1 : 0);
					altCustomizeDirty = true;
				}
			}
			if (Global.input.isPressedMenu(Control.WeaponLeft)) {
				int wi = cursors[selCursorIndex].index;
				if (selCursorIndex > 0 && wi > 0 && altCustomizeArray[wi] == 1) {
					altCustomizeArray[wi] = 0;
					altCustomizeDirty = true;
				}
			} else if (Global.input.isPressedMenu(Control.WeaponRight)) {
				int wi = cursors[selCursorIndex].index;
				if (selCursorIndex > 0 && wi > 0 && altCustomizeArray[wi] == 0) {
					altCustomizeArray[wi] = 1;
					altCustomizeDirty = true;
				}
			}
		} else if (selCursorIndex == 3) {
			Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, 1, playSound: true);
		}

		Helpers.menuUpDown(ref selCursorIndex, 0, 3);

		for (int i = 0; i < 3; i++) {
			selectedWeaponIndices[i] = cursors[i].index;
		}

		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		bool selectPressed = Global.input.isPressedMenu(Control.MenuConfirm) || (backPressed && !inGame);
		if (selectPressed) {
			Axl axl = mainPlayer?.character as Axl;

			if (duplicateWeapons()) {
				error = "Cannot select same weapon more than once!";
				return;
			}

			if (mainPlayer != null && mainPlayer.axlBulletTypeBought[selectedWeaponIndices[0]] == false) {
				error = "Must craft this upgrade first.";
				return;
			}

			if (mainPlayer != null && (mainPlayer.character?.charState is HyperAxlStart || axl?.isWhiteAxl() == true) && selectedWeaponIndices[0] > 0) {
				error = "Cannot use craftable guns as White Axl.";
				return;
			}

			if (mainPlayer != null && mainPlayer.axlBulletType != selectedWeaponIndices[0]) {
				mainPlayer.axlBulletType = selectedWeaponIndices[0];
				if (axl != null) {
					axl.ammoUsages.Clear();
				}
				float oldAmmo = mainPlayer.weapons[0].ammo;

				mainPlayer.weapons[0] = mainPlayer.getAxlBulletWeapon(selectedWeaponIndices[0]);
				if (mainPlayer.oldWeapons.Count > 0) mainPlayer.oldWeapons[0] = mainPlayer.getAxlBulletWeapon(selectedWeaponIndices[0]);

				//mainPlayer.weapons[0].ammo = oldAmmo;
				mainPlayer.weapons[0].ammo = mainPlayer.axlBulletTypeLastAmmo[mainPlayer.weapons[0].type];
			}

			if (!areWeaponArrSame(selectedWeaponIndices, Options.main.axlLoadout.getAxlWeaponFIs()) || altCustomizeDirty || Options.main.axlLoadout.hyperMode != cursors[3].index) {
				if (altCustomizeDirty) {
					Options.main.axlLoadout.setAltFireArray(altCustomizeArray);
				}

				if (mainPlayer != null) {
					mainPlayer.axlBulletType = selectedWeaponIndices[0];
					if (axl != null) {
						axl.ammoUsages.Clear();
					}
				}
				Options.main.axlLoadout.weapon2 = selectedWeaponIndices[1];
				Options.main.axlLoadout.weapon3 = selectedWeaponIndices[2];
				selectedWeaponIndices = selectedWeaponIndices.Select(i => Weapon.fiToAxlWep(i).index).ToList();
				Options.main.axlLoadout.hyperMode = cursors[3].index;
				Options.main.saveToFile();
				if (inGame) {
					if (Options.main.killOnLoadoutChange) {
						mainPlayer.forceKill();
					} else if (!mainPlayer.isDead) {
						Global.level.gameMode.setHUDErrorMessage(mainPlayer, "Change will apply on next death", playSound: false);
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

		Fonts.drawText(FontType.Yellow, "Axl Loadout", Global.screenW * 0.5f, 24, Alignment.Center);

		var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
		float botOffY = inGame ? 0 : -1;

		int startY = 45;
		int startX = 30;
		int startX2 = 120;
		int wepW = 18;
		int wepH = 20;

		float leftArrowPos = startX2 - 15;

		Global.sprites["cursor"].drawToHUD(0, startX, startY + (selCursorIndex * wepH));
		for (int i = 0; i < 3; i++) {
			float yPos = startY - 6 + (i * wepH);
			if (i == 0) {
				Fonts.drawText(FontType.Blue, "Sidearm", 40, yPos + 2, selected: selCursorIndex == i);
			} else {
				Fonts.drawText(FontType.Blue, "Main " + (i).ToString(), 40, yPos + 2, selected: selCursorIndex == i);
			}
			if (i == 0) {
				for (int j = 0; j < craftableWeapons.Count; j++) {
					Global.sprites["hud_weapon_icon"].drawToHUD(
						craftableWeapons[j].weaponSlotIndex, startX2 + (j * wepW), startY + (i * wepH)
					);
					if (Global.level?.mainPlayer != null && mainPlayer.axlBulletTypeBought[j] == false) {
						//DrawWrappers.DrawRectWH(startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14, true, new Color(0, 0, 0, 128), 0, ZIndex.HUD, false);
						Global.sprites["hud_weapon_locked"].drawToHUD(0, startX2 + (j * wepW), startY + (i * wepH));
					}
					if (selectedWeaponIndices[i] != j) {
						DrawWrappers.DrawRectWH(
							startX2 + (j * wepW) - 7, startY + (i * wepH) - 7,
							14, 14, true, Helpers.FadedIconColor, 1, ZIndex.HUD, false
						);
					}
				}
				continue;
			}

			for (int j = 0; j < 8; j++) {
				Global.sprites["hud_weapon_icon"].drawToHUD(
					Weapon.fiToAxlWep(j + 1).weaponSlotIndex, startX2 + (j * wepW), startY + (i * wepH)
				);
				if (selectedWeaponIndices[i] != j + 1) {
					DrawWrappers.DrawRectWH(
						startX2 + (j * wepW) - 7, startY + (i * wepH) - 7,
						14, 14, true, Helpers.FadedIconColor, 1, ZIndex.HUD, false
					);
				}
				if (selectedWeaponIndices[i] == j + 1) {
					if (altCustomizeArray[j + 1] == 1) {
						Helpers.drawWeaponSlotSymbol(startX2 + (j * wepW) - 8, startY + (i * wepH) - 8, "²");
					} else {
						Helpers.drawWeaponSlotSymbol(startX2 + (j * wepW) - 8, startY + (i * wepH) - 8, "¹");
					}
				}
			}
		}

		Fonts.drawText(FontType.Blue, "Hyper Mode", 40, startY - 4 + (wepH * 3), selected: selCursorIndex == 3);
		//Helpers.drawTextStd((cursors[3].index == 0 ? "White Axl" : "Stealth Mode"), 112, startY - 6 + (wepH * 3), color: Color.White);
		for (int j = 0; j < 2; j++) {
			Global.sprites["hud_weapon_icon"].drawToHUD(103 + j, startX2 + (j * wepW), startY + (wepH * 3));
			if (cursors[3].index != j) {
				DrawWrappers.DrawRectWH(
					startX2 + (j * wepW) - 7, startY - 7 + (wepH * 3),
					14, 14, true, Helpers.FadedIconColor, 1, ZIndex.HUD, false
				);
			}
		}

		int wsy = 162;
		DrawWrappers.DrawRect(
			25, wsy - 42, Global.screenW - 25, wsy + 30, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: outlineColor
		);
		DrawWrappers.DrawRect(
			25, wsy - 42, Global.screenW - 25, wsy - 27, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: outlineColor
		);

		float titleY1 = 124;
		float titleY2 = 140;
		float row1Y = 153;
		float row2Y = 181;

		string description = "";
		if (selCursorIndex == 0) {
			var weapon = craftableWeapons[selectedWeaponIndices[0]];
			Fonts.drawText(FontType.Purple, "Sidearm", Global.halfScreenW, titleY1, Alignment.Center);

			if (selectedWeaponIndices[0] == 0) {
				Fonts.drawText(FontType.Orange, "Axl Bullet", Global.halfScreenW, titleY2, Alignment.Center);
				description = (
					"Fully automatic pistol with a self-regenerating\n" +
					"ammo chamber and reliable bullet trajectory."
				);
				Fonts.drawText(FontType.Green, description, Global.halfScreenW, row1Y, Alignment.Center);
				Fonts.drawText(
					FontType.Blue, "Can charge the alt fire for more damage.",
					Global.halfScreenW, row2Y, Alignment.Center
				);
			} else {
				if (selectedWeaponIndices[0] == 1) description = "Pierces enemies, walls and defenses.";
				if (selectedWeaponIndices[0] == 2) description = "2x damage vs Mavericks and Ride Armors.";
				if (selectedWeaponIndices[0] == 3) description = "2 bullets per shot, but inaccurate.";
				if (selectedWeaponIndices[0] == 4) description = "2x fire rate, but 2x ammo usage.";
				if (selectedWeaponIndices[0] == 5) description = "Can headshot, but 1/2 damage on body.";
				if (selectedWeaponIndices[0] == 6) description = "Has all bonuses in one.";

				Fonts.drawText(
					FontType.Orange, weapon.displayName,
					Global.halfScreenW, titleY2, Alignment.Center
				);
				Fonts.drawText(
					FontType.Green, description,
					Global.halfScreenW, row1Y, Alignment.Center
				);
				string ammoString = "Not purchased.";
				if (mainPlayer.axlBulletTypeBought[selectedWeaponIndices[0]] == true) {
					float ammo = mainPlayer.axlBulletTypeAmmo[selectedWeaponIndices[0]];
					ammoString = "Remaining Ammo: " + MathF.Ceiling(ammo);
				}
				Fonts.drawText(
					FontType.DarkPurple, ammoString,
					Global.halfScreenW, row1Y + 10, Alignment.Center
				);
			}

			if (selectedWeaponIndices[0] > 0) {
				drawCraftingRecipes(selectedWeaponIndices[0]);
			}
		} else if (selCursorIndex < 3) {
			int friendlyWi = selectedWeaponIndices[selCursorIndex];
			string title = selCursorIndex == 1 ? "1st main" : "2nd main";

			Fonts.drawText(
				FontType.Purple, title + " weapon",
				Global.halfScreenW, titleY1, Alignment.Center
			);
			Fonts.drawText(FontType.Orange, weaponNames[friendlyWi], Global.halfScreenW, titleY2, Alignment.Center);

			if (friendlyWi == 1) description = (
				"Eapid-fire energy weapon with long range."
			);
			if (friendlyWi == 2) description = (
				"Pump-action grenade launcher with AOE\nand blast knockback."
			);
			if (friendlyWi == 3) description = (
				"Ballista with arrows that can embed into walls\nand homing to the enemy head. Can headshot."
			);
			if (friendlyWi == 4) description = (
				"Long range revolver with piercing bullets that\nignore defense, go through walls and headshot."
			);
			if (friendlyWi == 5) description = (
				"Semi-automatic pistol with specialized ammo\nmade to ricochet off of walls."
			);
			if (friendlyWi == 6) description = (
				"Electroshock shotgun whose shots ignore defense,\n flinches the enemy, and disables barriers."
			);
			if (friendlyWi == 7) description = (
				"Nitogen-based rotatory machinegun that fires\nshots that can freeze targets."
			);
			if (friendlyWi == 8) description = (
				"Portable flamethrower that burns foes."
			);
			string altFireDesc = (
				altCustomizeArray[friendlyWi] == 0 ? altFireDatas[friendlyWi].alt1Name : altFireDatas[friendlyWi].alt2Name
			);
			Fonts.drawText(
				FontType.Green, description,
				Global.halfScreenW, row1Y, Alignment.Center
			);

			float alt1X = Global.screenW * 0.3f;
			float alt2X = Global.screenW * 0.7f;
			//Helpers.drawTextStd(
			//	"Customize Alt Fire:", Global.halfScreenW, row1Y - 2, Alignment.Center,
			//	style: Text.Styles.Italic, fontSize: 24
			//);
			Fonts.drawText(
				FontType.DarkPurple, altFireDatas[friendlyWi].alt1Name,
				alt1X, row2Y, Alignment.Center, selected: altCustomizeArray[friendlyWi] == 0,
				selectedFont: FontType.Yellow
			);
			Fonts.drawText(
				FontType.DarkPurple, altFireDatas[friendlyWi].alt2Name,
				alt2X, row2Y, Alignment.Center, selected: altCustomizeArray[friendlyWi] == 1,
				selectedFont: FontType.Yellow
			);
			DrawWrappers.DrawLine(
				Global.halfScreenW, 176,
				Global.halfScreenW, 192,
				outlineColor, 1, ZIndex.HUD, false
			);
			DrawWrappers.DrawLine(
				25, 176,
				Global.screenW - 25, 176,
				outlineColor, 1, ZIndex.HUD, false
			);
		} else {
			Fonts.drawText(
				FontType.Purple, "Hyper Mode", Global.halfScreenW, titleY1, Alignment.Center
			);
			Fonts.drawText(
				FontType.Orange, (cursors[3].index == 0 ? "White Axl" : "Stealth Axl"),
				Global.halfScreenW, titleY2, Alignment.Center
			);

			if (cursors[3].index == 0) {
				Fonts.drawText(
					FontType.Green, "Grants infinite hover\nand powered up weapons.",
					Global.halfScreenW, row1Y, Alignment.Center
				);
			} else {
				Fonts.drawText(
					FontType.Green, "Turns Axl invisible and\ninvincible while still allowing attacks.",
					Global.halfScreenW, row1Y, Alignment.Center
				);
			}
			Fonts.drawText(
				FontType.Blue, "Cost: 10 Metal",
				Global.halfScreenW, row2Y, Alignment.Center
			);
		}
		/*
		if (selCursorIndex == 0) {
			if (mainPlayer != null && selectedWeaponIndices[0] > 0 && mainPlayer.axlBulletTypeBought[selectedWeaponIndices[0]] == false) {
				Helpers.drawTextStd(TCat.BotHelp, "WeaponL/R: Change Recipe", Global.screenW * 0.5f, 195 + botOffY, Alignment.Center, fontSize: 18);
			}

			Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change Weapon", Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 18);
			Helpers.drawTextStd(TCat.BotHelp, "Up/Down: Change Slot", Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 18);
		} else {
			if (selCursorIndex == 1 || selCursorIndex == 2) {
				Helpers.drawTextStd(TCat.BotHelp, "WeaponL/R: Change alt fire", Global.screenW * 0.5f, 195 + botOffY, Alignment.Center, fontSize: 18);
			}
			Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change Weapon", Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 18);
			Helpers.drawTextStd(TCat.BotHelp, "Up/Down: Change Slot", Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 18);
		}

		string helpText = "[BACK]: Back, [OK]: Confirm";
		if (!inGame) helpText = "[BACK]: Save and back";
		Helpers.drawTextStd(TCat.BotHelp, helpText, Global.screenW * 0.5f, 210 + botOffY, Alignment.Center, fontSize: 18);
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

	private void drawCraftingRecipes(int index) {
		var recipes = craftingRecipes[index];
		int posY = 176;
		DrawWrappers.DrawLine(
			25, posY,
			Global.screenW - 25, posY,
			Color.White, 1, ZIndex.HUD, false
		);
		var rects = new List<Rect>();
		for (int i = 0; i < recipes.Count; i++) {
			DrawWrappers.DrawLine(
				25 + MathInt.Floor(83.5f * i), posY,
				25 + MathInt.Floor(83.5f * i), 192,
				Color.White, 1, ZIndex.HUD, false
			);
			int offset = 0;
			if (recipes.Count == 2 && i == 1) {
				offset = 167;
			}
			rects.Add(new Rect(
				25 + MathInt.Floor(83.5f * i),
				posY,
				25 + MathInt.Floor(83.5f * (i + 1)) + offset,
				192
			));

		}
		for (int i = 0; i < recipes.Count; i++) {
			var recipe = recipes[i];
			float rx = rects[i].x1 + 8 + (recipe.Length == 2 ? 20 : 8);
			if (recipes.Count == 2 && i == 1) {
				rx += 35;
			}
			float ry = rects[i].y1 + 3;
			recipeManager(recipe, false, out bool canAfford, out int missingCurrency, out int[] missingCores);
			for (int j = 0; j < recipe.Length - 1; j += 2) {
				char curChar = recipe[j];
				char nextChar = recipe[j + 1];
				int count = nextChar - '0';
				if (curChar == 'S') {
					Global.sprites["hud_scrap"].drawToHUD(0, rx, ry, alpha: !canAfford ? 0.5f : 1);
				} else if (curChar == '0' || curChar == '1' || curChar == '2' || curChar == '3' || curChar == '4') {
					int charToInt = curChar - '0';
					Global.sprites["char_icon"].drawToHUD(charToInt, rx + 6, 1 + ry, alpha: !canAfford ? 0.5f : 1);
				}
				Fonts.drawText(
					canAfford ? FontType.Blue : FontType.DarkBlue, "x" + count.ToString(),
					rx + 12, 3 + ry
				);
				rx += 25;
			}
			int selCraftRecipeIndex = craftingRecipeSelections[index];
			if (selCraftRecipeIndex == i) {
				DrawWrappers.DrawRect(rects[i].x1, rects[i].y1, rects[i].x2, rects[i].y2, false, Color.Green, 1, ZIndex.HUD, false);
			}
		}
	}
}
