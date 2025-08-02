namespace MMXOnline;
using SFML.Graphics;
using System;
using System.Collections.Generic;

public class UpgradeArmorMenu : IMainMenu {
	public int selectArrowPosY;
	public IMainMenu prevMenu;
	public UpgradeArmorMenuGolden GoldenMenu;
	public UpgradeArmorMenuUAX UAXMenu;

	public Point optionPos1 = new Point(25, 40);
	public Point optionPos2 = new Point(25, 40 + 40);
	public Point optionPos3 = new Point(25, 40 + 40 * 2);
	public Point optionPos4 = new Point(25, 40 + 38 * 3);

	public Level level { get { return Global.level; } }
	public Player mainPlayer { get { return Global.level.mainPlayer; } }

	public static int xGame = 1;

	public UpgradeArmorMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
		GoldenMenu = new UpgradeArmorMenuGolden(prevMenu);
		UAXMenu = new UpgradeArmorMenuUAX(prevMenu);
	}

	public void update() {
		MegamanX? mmx = Global.level.mainPlayer.character as MegamanX;
		//if (updateHyperArmorUpgrades(mainPlayer)) return;

		// Should not be able to reach here but preventing upgrades just in case

		Helpers.menuUpDown(ref selectArrowPosY, 0, 3);

		if (Global.input.isPressedMenu(Control.MenuLeft)) {
			xGame--;
			if (xGame < 1) {
				xGame = 1;
				if (!Global.level.server.disableHtSt) {
					UpgradeMenu.onUpgradeMenu = true;
					Menu.change(new UpgradeMenu(prevMenu));
					return;
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuRight)) {
			xGame++;
			if (xGame > 3) {
				xGame = 3;
				if (!Global.level.server.disableHtSt) {
					UpgradeMenu.onUpgradeMenu = true;
					Menu.change(new UpgradeMenu(prevMenu));
					return;
				}
			}
		}

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
			return;
		}
		// Enable this to disable 14.0 UAX Menu
		if (mmx?.hasFullHyperMaxArmor == true) {
			Menu.change(new UpgradeArmorMenuGolden(GoldenMenu));
			return;
		}
		if (mmx?.hasUltimateArmor == true) {
			Menu.change(new UpgradeArmorMenuUAX(UAXMenu));
			return;
		}
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectArrowPosY == 0) {
				if (mainPlayer.helmetArmorNum != xGame) {
					if (!mainPlayer.isHeadArmorPurchased(xGame)) {
						if (mainPlayer.currency >= MegamanX.headArmorCost) {
							purchaseHelmetArmor(mainPlayer, xGame);
							Global.playSound("ching");
							if (mainPlayer.helmetArmorNum == 0) {
								upgradeHelmetArmor(mainPlayer, xGame);
							}
						}
					} else {
						upgradeHelmetArmor(mainPlayer, 0);
						upgradeHelmetArmor(mainPlayer, xGame);
						Global.playSound("ching");
					}
				} else if (mainPlayer.hasAllX3Armor() && mmx?.hyperHelmetActive == false) {
					mmx.hyperChestActive = false;
					mmx.hyperArmActive = false;
					mmx.hyperLegActive = false;
					mmx.hyperHelmetActive = true;
					Global.playSound("ching");
				}
			}
			if (selectArrowPosY == 1) {
				if (mainPlayer.bodyArmorNum != xGame) {
					if (!mainPlayer.isBodyArmorPurchased(xGame)) {
						if (mainPlayer.currency >= MegamanX.chestArmorCost) {
							purchaseBodyArmor(mainPlayer, xGame);
							Global.playSound("ching");
							if (mainPlayer.bodyArmorNum == 0) {
								upgradeBodyArmor(mainPlayer, xGame);
							}
						}
					} else {
						upgradeBodyArmor(mainPlayer, 0);
						upgradeBodyArmor(mainPlayer, xGame);
						Global.playSound("ching");
					}
				} else if (mainPlayer.hasAllX3Armor() && mmx?.hyperChestActive == false) {
					mmx.hyperChestActive = true;
					mmx.hyperArmActive = false;
					mmx.hyperLegActive = false;
					mmx.hyperHelmetActive = false;
					Global.playSound("ching");
				}
			}
			if (selectArrowPosY == 2) {
				if (mainPlayer.armArmorNum != xGame) {
					if (!mainPlayer.isArmArmorPurchased(xGame)) {
						if (mainPlayer.currency >= MegamanX.armArmorCost) {
							purchaseArmArmor(mainPlayer, xGame);
							Global.playSound("ching");
							if (mainPlayer.armArmorNum == 0) {
								upgradeArmArmor(mainPlayer, xGame);
							}
						}
					} else {
						upgradeArmArmor(mainPlayer, 0);
						upgradeArmArmor(mainPlayer, xGame);
						Global.playSound("ching");
					}
				} else if (mainPlayer.hasAllX3Armor() && mmx?.hyperArmActive == false) {
					mmx.hyperChestActive = false;
					mmx.hyperArmActive = true;
					mmx.hyperLegActive = false;
					mmx.hyperHelmetActive = false;
					Global.playSound("ching");
				}
			}
			if (selectArrowPosY == 3) {
				if (mainPlayer.legArmorNum != xGame) {
					if (!mainPlayer.isBootsArmorPurchased(xGame)) {
						if (mainPlayer.currency >= MegamanX.bootsArmorCost) {
							purchaseBootsArmor(mainPlayer, xGame);
							Global.playSound("ching");
							if (mainPlayer.legArmorNum == 0) {
								upgradeBootsArmor(mainPlayer, xGame);
							}
						}
					} else {
						upgradeBootsArmor(mainPlayer, 0);
						upgradeBootsArmor(mainPlayer, xGame);
						Global.playSound("ching");
					}
				} else if (mainPlayer.hasAllX3Armor() && mmx?.hyperLegActive == false) {
					mmx.hyperChestActive = false;
					mmx.hyperArmActive = false;
					mmx.hyperLegActive = true;
					mmx.hyperHelmetActive = false;
					Global.playSound("ching");
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuAlt)) {
			if (mmx != null && mmx.hasAnyHyperArmor) {
				mmx.hyperChestActive = false;
				mmx.hyperArmActive = false;
				mmx.hyperLegActive = false;
				mmx.hyperHelmetActive = false;
			}
			else if (selectArrowPosY == 0) {
				if (mainPlayer.helmetArmorNum == xGame) {
					if (mainPlayer.hasAllX3Armor()) {
						if (mmx != null) {
							mmx.hyperChestActive = false;
							mmx.hyperArmActive = false;
							mmx.hyperLegActive = false;
							mmx.hyperHelmetActive = true;
						}
					} else {
						upgradeHelmetArmor(mainPlayer, 0);
					}
				}
			}
			else if (selectArrowPosY == 1) {
				if (mainPlayer.bodyArmorNum == xGame) {
					upgradeBodyArmor(mainPlayer, 0);
				}
			}
			else if (selectArrowPosY == 2) {
				if (mainPlayer.armArmorNum == xGame) {
					upgradeArmArmor(mainPlayer, 0);
				}
			}
			else if (selectArrowPosY == 3) {
				if (mainPlayer.legArmorNum == xGame) {
					upgradeBootsArmor(mainPlayer, 0);
				}
			}
		}
	}
	
	public static void clearAllHyperArmor(MegamanX mmx) {
		mmx.hyperChestActive = false;
		mmx.hyperArmActive = false;
		mmx.hyperLegActive = false;
		mmx.hyperHelmetActive = false;
	}

	public static void upgradeHelmetArmor(Player player, int type) {
		player.helmetArmorNum = type;
		if (player.character is MegamanX mmx) {
			mmx.helmetArmor = (ArmorId)type;
			clearAllHyperArmor(mmx);
		}
	}

	public static void purchaseHelmetArmor(Player player, int type) {
		if (!player.isHeadArmorPurchased(type)) {
			player.currency -= MegamanX.headArmorCost;
			player.setHeadArmorPurchased(type);
		}
	}

	public static void upgradeBodyArmor(Player player, int type) {
		player.bodyArmorNum = type;
		if (type == 2) {
			player.addGigaCrush();
		} else {
			player.removeGigaCrush();
		}
		if (player.character is MegamanX mmx) {
			mmx.chestArmor = (ArmorId)type;
			clearAllHyperArmor(mmx);
		}
	}

	public static void purchaseBodyArmor(Player player, int type) {
		if (!player.isBodyArmorPurchased(type)) {
			player.currency -= MegamanX.chestArmorCost;
			player.setBodyArmorPurchased(type);
		}
	}

	public static void upgradeArmArmor(Player player, int type) {
		player.armArmorNum = type;
		if (type == 3) {
			player.addHyperCharge();
		} else {
			player.removeHyperCharge();
		}
		if (player.character is MegamanX mmx) {
			mmx.armArmor = (ArmorId)type;
			clearAllHyperArmor(mmx);
		}
	}

	public static void purchaseArmArmor(Player player, int type) {
		if (type != 0 && !player.isArmArmorPurchased(type)) {
			player.currency -= MegamanX.armArmorCost;
			player.setArmArmorPurchased(type);
		}
	}

	public static void upgradeBootsArmor(Player player, int type) {
		player.legArmorNum = type;
		if (player.character is MegamanX mmx) {
			mmx.legArmor = (ArmorId)type;
			clearAllHyperArmor(mmx);
		}
	}

	public static void purchaseBootsArmor(Player player, int type) {
		if (type != 0 && !player.isBootsArmorPurchased(type)) {
			player.currency -= MegamanX.bootsArmorCost;
			player.setBootsArmorPurchased(type);
		}
	}

	public void render() {
		var gameMode = level.gameMode;
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		string armorName = xGame switch {
			1 => "Light Armor",
			2 => "Giga Armor",
			3 => "Max Armor",
			_ => "ERROR"
		};
		Fonts.drawText(
			FontType.Yellow, string.Format($"Upgrade {armorName}", xGame),
			Global.screenW * 0.5f, 20, Alignment.Center
		);
		Fonts.drawText(FontType.OrangeMenu, Global.nameCoins + ": " + mainPlayer.currency, 20, 20);

		if (Global.frameCount % 60 < 30) {
			bool stEnabled = !Global.level.server.disableHtSt;
			string leftText = xGame switch {
				1 when stEnabled => "Items",
				2 => "Light",
				3 => "Giga",
				_ => ""
			};
			string rightText = xGame switch {
				1 => "Giga",
				2 => "Max",
				3 when stEnabled => "Items",
				_ => ""
			};
			if (leftText != "") {
				Fonts.drawText(
					FontType.DarkPurple, "<",
					14, Global.halfScreenH, Alignment.Center
				);
				Fonts.drawText(
					FontType.DarkPurple, "",//leftText,
					14, Global.halfScreenH + 15, Alignment.Center
				);
			}
			if (rightText != "") {
				Fonts.drawText(
					FontType.DarkPurple, ">",
					Global.screenW - 14, Global.halfScreenH, Alignment.Center
				);
				Fonts.drawText(
					FontType.DarkPurple, "",//rightText,
					Global.screenW - 14, Global.halfScreenH + 15, Alignment.Center
				);
			}
		}

		Point optionPos = new Point();
		if (selectArrowPosY == 0) optionPos = optionPos1;
		if (selectArrowPosY == 1) optionPos = optionPos2;
		if (selectArrowPosY == 2) optionPos = optionPos3;
		if (selectArrowPosY == 3) optionPos = optionPos4;
		float yOff = xGame == 3 && mainPlayer.hasAllX3Armor() ? 9 : -1;
		Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 8, optionPos.y + 4 + yOff);
		bool showChips = mainPlayer.hasAllX3Armor() && xGame == 3;

		switch (xGame) {
			case 1: case 2: case 3: Global.sprites["menu_xdefault"].drawToHUD(0, 300, 110); break;
		} 
		switch (mainPlayer.helmetArmorNum) {
			case 1: Global.sprites["menu_xhelmet"].drawToHUD(0, 300, 110); break;
			case 2: Global.sprites["menu_xhelmet2"].drawToHUD(0, 300, 110); break;
			case 3: Global.sprites["menu_xhelmet3"].drawToHUD(0, 300, 110); break;
		}
		switch (mainPlayer.bodyArmorNum) {
			case 1: Global.sprites["menu_xbody"].drawToHUD(0, 300, 110); break;
			case 2: Global.sprites["menu_xbody2"].drawToHUD(0, 300, 110); break;
			case 3: Global.sprites["menu_xbody3"].drawToHUD(0, 300, 110); break;
		}
		switch (mainPlayer.armArmorNum) {
			case 1: Global.sprites["menu_xarm"].drawToHUD(0, 300, 110); break;
			case 2: Global.sprites["menu_xarm2"].drawToHUD(0, 300, 110); break;
			case 3: Global.sprites["menu_xarm3"].drawToHUD(0, 300, 110); break;
		}
		switch (mainPlayer.legArmorNum) {
			case 1: Global.sprites["menu_xboots"].drawToHUD(0, 300, 110); break;
			case 2: Global.sprites["menu_xboots2"].drawToHUD(0, 300, 110); break;
			case 3: Global.sprites["menu_xboots3"].drawToHUD(0, 300, 110); break;
		}
		Fonts.drawText(FontType.Yellow, "Head Parts", optionPos1.x, optionPos1.y, selected: selectArrowPosY == 0 && !showChips);
		Fonts.drawText(FontType.Green, getHeadArmorMessage(), optionPos1.x + 60, optionPos1.y);
		Fonts.drawText(FontType.Yellow, "Body Parts", optionPos2.x, optionPos2.y, selected: selectArrowPosY == 1 && !showChips);
		Fonts.drawText(FontType.Green, getBodyArmorMessage(), optionPos2.x + 60, optionPos2.y);
		Fonts.drawText(FontType.Yellow, "Arm Parts", optionPos3.x, optionPos3.y, selected: selectArrowPosY == 2 && !showChips);
		Fonts.drawText(FontType.Green, getArmArmorMessage(), optionPos3.x + 60, optionPos3.y);
		Fonts.drawText(FontType.Yellow, "Foot Parts", optionPos4.x, optionPos4.y, selected: selectArrowPosY == 3 && !showChips);
		Fonts.drawText(FontType.Green, getBootsArmorMessage(), optionPos4.x + 60, optionPos4.y);
		switch (xGame) {
			case 1: //Light
				Fonts.drawText(FontType.Blue, "Grants a headbutt attack on Jump.", optionPos1.x + 5, optionPos1.y + 10);
				Fonts.drawText(FontType.Blue, "Can be combined with Upward Dash.", optionPos1.x + 5, optionPos1.y + 20);
				Fonts.drawText(FontType.Blue, "Reduces Damage by 12.5%", optionPos2.x + 5, optionPos2.y + 10);
				Fonts.drawText(FontType.Blue, "Reduces Flinch by 25.0%", optionPos2.x + 5, optionPos2.y + 20);
				Fonts.drawText(FontType.Blue, "Powers up your Spiral Crush Buster", optionPos3.x + 5, optionPos3.y + 10);
				Fonts.drawText(FontType.Blue, "by charging shots 50% Faster.", optionPos3.x + 5, optionPos3.y + 20);
				Fonts.drawText(FontType.Blue, "Ground Dash 15% faster and longer.", optionPos4.x + 5, optionPos4.y + 10);
				break;
			case 2: //Giga
				Fonts.drawText(FontType.Blue, "Trace enemy hp and positioning", optionPos1.x + 5, optionPos1.y + 10);
				Fonts.drawText(FontType.Blue, "by pressing SPECIAL button.", optionPos1.x + 5, optionPos1.y + 20);
				Fonts.drawText(FontType.Blue, "Grants the Giga Crush attack", optionPos2.x + 5, optionPos2.y + 10);
				Fonts.drawText(FontType.Blue, "Reduces Damage by 12.5%", optionPos2.x + 5, optionPos2.y + 20);
				Fonts.drawText(FontType.Blue, "Grants the Double X-Buster.", optionPos3.x + 5, optionPos3.y + 10);
				Fonts.drawText(FontType.Blue, "Store an extra charge shot.", optionPos3.x + 5, optionPos3.y + 20);
				Fonts.drawText(FontType.Blue, "Air dash 15% faster and longer.", optionPos4.x + 5, optionPos4.y + 10);
				break;
			case 3:	//Max
				if (!mainPlayer.hasAllX3Armor()) {
					Fonts.drawText(FontType.Blue, "Communicates with the", optionPos1.x + 5, optionPos1.y + 10);
					Fonts.drawText(FontType.DarkPurple,"State of the Art Space Satellite.", optionPos1.x + 5, optionPos1.y + 20);
					Fonts.drawText(FontType.Blue, "To uncover enemy position.", optionPos1.x + 5, optionPos1.y + 30);
					Fonts.drawText(FontType.Blue, "Gain a Defensive Forcefield", optionPos2.x + 5, optionPos2.y + 10);
					Fonts.drawText(FontType.Blue, "on taking Damage.", optionPos2.x + 5, optionPos2.y + 20);
					Fonts.drawText(FontType.Blue, "Forcefield Defense: 25%", optionPos2.x + 5, optionPos2.y + 30);
					Fonts.drawText(FontType.Blue, "Grants the Hyper Charge.", optionPos3.x + 5, optionPos3.y + 10);
					Fonts.drawText(FontType.Blue, "Grants the Cross Charge shot.", optionPos3.x + 5, optionPos3.y + 20);
					Fonts.drawText(FontType.Blue, "Grants an Upwards Dash", optionPos4.x + 5, optionPos4.y + 10);
				} else {
					Fonts.drawText(FontType.Pink, "ENHANCEMENT CHIP", optionPos1.x + 5, optionPos1.y + 10, selected: selectArrowPosY == 0);
					Fonts.drawText(FontType.Blue, "Slowly regenerate Health", optionPos1.x + 5, optionPos1.y + 20);
					Fonts.drawText(FontType.Blue, "after not taking Damage.", optionPos1.x + 5, optionPos1.y + 30);
					Fonts.drawText(FontType.Pink, "ENHANCEMENT CHIP", optionPos2.x + 5, optionPos2.y + 10, selected: selectArrowPosY == 1);
					Fonts.drawText(FontType.Blue, "Forcefield Defense: 50%", optionPos2.x + 5, optionPos2.y + 20);
					Fonts.drawText(FontType.Pink, "ENHANCEMENT CHIP", optionPos4.x + 5, optionPos3.y + 10, selected: selectArrowPosY == 2);
					Fonts.drawText(FontType.Blue, "Reduce ammo usage by half.", optionPos3.x + 5, optionPos3.y + 20);
					Fonts.drawText(FontType.Pink, "ENHANCEMENT CHIP", optionPos4.x + 5, optionPos4.y + 10, selected: selectArrowPosY == 3);
					Fonts.drawText(FontType.Blue, "Dash Twice in the air", optionPos4.x + 5, optionPos4.y + 20);
				}
				if (mainPlayer.character is MegamanX mmx) {
					if (mmx.hyperHelmetActive) Global.sprites["menu_chip"].drawToHUD(0, 296, optionPos1.y-16);
					if (mmx.hyperChestActive) Global.sprites["menu_chip"].drawToHUD(0, 296, optionPos2.y+4);
					if (mmx.hyperArmActive) Global.sprites["menu_chip"].drawToHUD(0, 262, optionPos3.y-8);
					if (mmx.hyperLegActive) Global.sprites["menu_chip"].drawToHUD(0, 278, optionPos4.y+6);
				}
				break;
		} 
		//drawHyperArmorUpgrades(mainPlayer, 0);

		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change Armor Set",
			26, Global.screenH - 28
		);
		Fonts.drawTextEX(
			FontType.Grey, "[OK]: Upgrade, [ALT]: Unupgrade, [BACK]: Back",
			26, Global.screenH - 18
		);
	}
	/*
	public static bool updateHyperArmorUpgrades(Player mainPlayer) {
		if (mainPlayer.character == null) return false;
		if (mainPlayer.character.charState is NovaStrikeState) return false;

		if (Global.input.isPressedMenu(Control.Special1)) {
			if (mainPlayer.canUpgradeUltimateX()) {
				if (!mainPlayer.character.boughtUltimateArmorOnce) {
					mainPlayer.currency -= Player.ultimateArmorCost;
					mainPlayer.character.boughtUltimateArmorOnce = true;
				}
				mainPlayer.setUltimateArmor(true);
				Global.playSound("chingX4");
				return true;
			} else if (mainPlayer.canUpgradeGoldenX()) {
				if (!mainPlayer.character.boughtGoldenArmorOnce) {
					mainPlayer.currency -= Player.goldenArmorCost;
					mainPlayer.character.boughtGoldenArmorOnce = true;
				}
				mainPlayer.setGoldenArmor(true);
				Global.playSound("ching");
				return true;
			}
		} else if (Global.input.isPressedMenu(Control.MenuAlt)) {
			if (mainPlayer.hasUltimateArmor()) {
				mainPlayer.setUltimateArmor(false);
				return true;
			} else if (mainPlayer.hasGoldenArmor()) {
				mainPlayer.setGoldenArmor(false);
				return true;
			}
		}
		return false;
	}
	
	public static void drawHyperArmorUpgrades(Player mainPlayer, int offY) {
		if (mainPlayer.character == null) return;
		if (mainPlayer.character.charState is NovaStrikeState) return;

		string specialText = "";
		if (mainPlayer.canUpgradeUltimateX() && mainPlayer.isX && !mainPlayer.isDisguisedAxl) {
			specialText = ("[SPC]: Ultimate Armor" +
				(mainPlayer.character.boughtUltimateArmorOnce ? "" : $" (10 {Global.nameCoins})")
			);
		} else if (mainPlayer.canUpgradeGoldenX() && mainPlayer.isX && !mainPlayer.isDisguisedAxl) {
			specialText = (
				"[SPC]: Hyper Chip" +
				(mainPlayer.character.boughtGoldenArmorOnce ? "" : $" (5 {Global.nameCoins})")
			);
		}

		if (mainPlayer.hasUltimateArmor()) {
			specialText += "\n[ALT]: Take Off Ultimate Armor";
		} else if (mainPlayer.hasGoldenArmor()) {
			specialText += "\n[ALT]: Disable Hyper Chip";
		}

		if (!string.IsNullOrEmpty(specialText)) {
			specialText = specialText.TrimStart('\n');
			float yOff = specialText.Contains('\n') ? -3 : 0;
			float yPos = Global.halfScreenH + 25;
			DrawWrappers.DrawRect(
				5, yPos + offY, Global.screenW - 5, yPos + 30 + offY,
				true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false
			);
			Fonts.drawText(
				FontType.Yellow, Helpers.controlText(specialText).ToUpperInvariant(),
				Global.halfScreenW, yPos + 11 + yOff + offY, Alignment.Center
			);
		}

	}
	*/
	public string getHeadArmorMessage() {
		if (mainPlayer.isHeadArmorPurchased(xGame)) {
			return mainPlayer.helmetArmorNum == xGame ? " (Active)" : " (Bought)";
		}
		return $" ({MegamanX.headArmorCost} {Global.nameCoins})";
	}

	public string getBodyArmorMessage() {
		if (mainPlayer.isBodyArmorPurchased(xGame)) {
			return mainPlayer.bodyArmorNum == xGame ? " (Active)" : " (Bought)";
		}
		return $" ({MegamanX.chestArmorCost} {Global.nameCoins})";
	}

	public string getArmArmorMessage() {
		if (mainPlayer.isArmArmorPurchased(xGame)) {
			return mainPlayer.armArmorNum == xGame ? " (Active)" : " (Bought)";
		}
		return $" ({MegamanX.armArmorCost} {Global.nameCoins})";
	}

	public string getBootsArmorMessage() {
		if (mainPlayer.isBootsArmorPurchased(xGame)) {
			return mainPlayer.legArmorNum == xGame ? " (Active)" : " (Bought)";
		}
		return $" ({MegamanX.bootsArmorCost} {Global.nameCoins})";
	}
}
public class UpgradeArmorMenuUAX : IMainMenu {
	public IMainMenu prevMenu;
	public Player mainPlayer { get { return Global.level.mainPlayer; } }

	public static int xGame = 1;

	public UpgradeArmorMenuUAX(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
	}
	public void update() {
		if (!mainPlayer.canUpgradeXArmor()) return;

		if (Global.input.isPressedMenu(Control.MenuLeft)) {
			xGame--;
			if (xGame < 1) {
				xGame = 1;
				if (!Global.level.server.disableHtSt) {
					UpgradeMenu.onUpgradeMenu = true;
					Menu.change(new UpgradeMenu(prevMenu));
					return;
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuRight)) {
			xGame++;
			if (xGame > 3) {
				xGame = 3;
				if (!Global.level.server.disableHtSt) {
					UpgradeMenu.onUpgradeMenu = true;
					Menu.change(new UpgradeMenu(prevMenu));
					return;
				}
			}
		}
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
			return;
		}
	}
	public void render() {

		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.Purple, "Ultimate Armor Active",Global.screenW * 0.5f, 20, Alignment.Center);
		Global.sprites["menu_xultimatex2"].drawToHUD(0, Global.halfScreenW, 80);
		Fonts.drawText(FontType.RedishOrange, "Uncompleted Powerful Armor",Global.halfScreenW-80, 140);
		//Fonts.drawText(FontType.Red, "Grants Parts of All armors and Chips in the entire armor", Global.halfScreenW-170, optionPos3.y + 30);
		Fonts.drawText(FontType.DarkPurple, "Enhances the Base Armor", Global.halfScreenW-70, 155);
		Fonts.drawText(FontType.DarkPurple, "Grants Nova Strike, Hover and The Plasma Shot", Global.halfScreenW-136, 170);
		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change Menu",
			Global.halfScreenW-70, 190
		);
		Fonts.drawTextEX(
			FontType.Grey, "[BACK]: Back",
			Global.halfScreenW-20, 200
		);
		if (mainPlayer.character is MegamanX mmx) {
			if (mmx.hyperLegActive) Global.sprites["menu_chip"].drawToHUD(0, Global.halfScreenW-6, 52, alpha: 0.85f);
			if (mmx.hyperChestActive) Global.sprites["menu_chip"].drawToHUD(0, Global.halfScreenW+2, 70, alpha: 0.85f);
			if (mmx.hyperHelmetActive) Global.sprites["menu_chip"].drawToHUD(0, Global.halfScreenW-20, 45, alpha: 0.85f);
			if (mmx.hyperArmActive) Global.sprites["menu_chip"].drawToHUD(0, Global.halfScreenW-22, 96, alpha: 0.85f);
		}
	}
}
public class UpgradeArmorMenuGolden : IMainMenu {
	public IMainMenu prevMenu;
	public Player mainPlayer { get { return Global.level.mainPlayer; } }
	public static int xGame = 1;
	public UpgradeArmorMenuGolden(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
	}
	public void update() {
		if (!mainPlayer.canUpgradeXArmor()) return;
		if (Global.input.isPressedMenu(Control.MenuLeft)) {
			xGame--;
			if (xGame < 1) {
				xGame = 1;
				if (!Global.level.server.disableHtSt) {
					UpgradeMenu.onUpgradeMenu = true;
					Menu.change(new UpgradeMenu(prevMenu));
					return;
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuRight)) {
			xGame++;
			if (xGame > 3) {
				xGame = 3;
				if (!Global.level.server.disableHtSt) {
					UpgradeMenu.onUpgradeMenu = true;
					Menu.change(new UpgradeMenu(prevMenu));
					return;
				}
			}
		}
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
			return;
		}
	}
	public void render() {

		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.Pink, "Hyper Chip Active",Global.screenW * 0.5f, 20, Alignment.Center);
		Global.sprites["menu_x3"].drawToHUD(2, 295, 110);
		if (Global.frameCount % 6 < 4) 
			 Global.sprites["LightX3"].drawToHUD(10, 50, 130);
		else Global.sprites["LightX3"].drawToHUD(11, 50, 130);
		DrawWrappers.DrawRect(
				210, 100 , 80 , 40,
				true, Helpers.MenuBgColor, 0, ZIndex.Default, false
		);
		Fonts.drawText(FontType.DarkBlue, "(RIGHT)",92, 46);
		Fonts.drawText(FontType.DarkBlue, "Your battles should",90, 60);
		Fonts.drawText(FontType.DarkBlue, "be easier now.",90, 70);
		Fonts.drawText(FontType.DarkBlue, "Do your Best, X.",90, 80);
		if (Global.frameCount % 60 < 30)
		Global.sprites["cursorchar"].drawToHUD(0, 94, 94);
		Fonts.drawText(FontType.RedishOrange, "Enhances The Max Armor.", 30, 150);
		Fonts.drawText(FontType.RedishOrange, "Grants the Z-Saber", 30, 160);
		Fonts.drawText(FontType.RedishOrange, "and All the Chips.", 30, 170);
		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change Menu",
			30, 190
		);
		Fonts.drawTextEX(
			FontType.Grey, "[BACK]: Back",
			30, 200
		);
	}
}
public class Skill {
	public string name = "";
	public Func<Player, bool>? isUnlocked;
	public Func<Player, bool>? canUnlock;
	public Func<Player, bool>? canLock;
	public Action<Player>? unlock;
	public Action<Player>? lockit;
	public string description = "";
	public string price = "";
}
public interface IMenuHandler {
	void handleInput(Player? player, int ud, int lr);
	void renderCursor(int ud, int lr, AnimData cursor, uint ghw, uint ghh);
	void renderDescription(Player? player, int ud, int lr, uint ghw, uint ghh);
	void renderIcons(int frame, AnimData icon, uint ghw, uint ghh, float opacity);
}
public class SNESArmorHandler : IMenuHandler {
	private Dictionary<(int ud, int lr), Skill> menu = new();
	public SNESArmorHandler() {
		#region Light
		menu[(1, 1)] = new Skill {
			name = "Helmet",
			isUnlocked = player => player.helmetArmorNum == (int)ArmorId.Light,
			canUnlock = player => player.helmetArmorNum != (int)ArmorId.Light && player.currency >= MegamanX.headArmorCost || player.headArmorsPurchased[0] == true,
			canLock = player => player.helmetArmorNum == (int)ArmorId.Light,
			unlock = (player) => {
				//Kill me
				setHelmet(player, (int)ArmorId.Light);
				player.helmetArmorNum = (int)ArmorId.Light;
				if (player.headArmorsPurchased[0] == false)
					player.currency -= MegamanX.headArmorCost;
				player.headArmorsPurchased[0] = true;
			},
			lockit = (player) => {
				setHelmet(player, (int)ArmorId.None);
				player.helmetArmorNum = (int)ArmorId.None;
				if (player.headArmorsPurchased[0] == false)
					player.currency += MegamanX.headArmorCost;
			},
			description = "Grants a headbutt attack on Jump.\nCan be combined with Upward Dash.",
			price = MegamanX.headArmorCost.ToString(),
		};
		menu[(1, 2)] = new Skill {
			name = "Body",
			isUnlocked = player => player.bodyArmorNum == (int)ArmorId.Light,
			canUnlock = player => player.bodyArmorNum != (int)ArmorId.Light && player.currency >= MegamanX.chestArmorCost || player.bodyArmorsPurchased[0] == true,
			canLock = player => player.bodyArmorNum == (int)ArmorId.Light,
			unlock = (player) => {
				setBody(player, (int)ArmorId.Light);
				player.removeGigaCrush();
				player.bodyArmorNum = (int)ArmorId.Light;
				if (player.bodyArmorsPurchased[0] == false)
					player.currency -= MegamanX.chestArmorCost;
				player.bodyArmorsPurchased[0] = true;
			},
			lockit = (player) => {
				setBody(player, (int)ArmorId.None);
				player.bodyArmorNum = (int)ArmorId.None;
				if (player.bodyArmorsPurchased[0] == false)
					player.currency += MegamanX.chestArmorCost;
			},
			description = "Reduces Damage by 12.5%\nReduces Flinch by 25.0%",
			price = MegamanX.chestArmorCost.ToString(),
		};
		menu[(1, 3)] = new Skill {
			name = "Arm",
			isUnlocked = player => player.armArmorNum == (int)ArmorId.Light,
			canUnlock = player => player.armArmorNum != (int)ArmorId.Light && player.currency >= MegamanX.armArmorCost || player.armArmorsPurchased[0] == true,
			canLock = player => player.armArmorNum == (int)ArmorId.Light,
			unlock = (player) => {
				setArm(player, (int)ArmorId.Light);
				player.removeHyperCharge();
				player.armArmorNum = (int)ArmorId.Light;
				if (player.armArmorsPurchased[0] == false)
					player.currency -= MegamanX.armArmorCost;
				player.armArmorsPurchased[0] = true;
			},
			lockit = (player) => {
				setArm(player, (int)ArmorId.None);
				player.armArmorNum = (int)ArmorId.None;
				if (player.armArmorsPurchased[0] == false)
					player.currency += MegamanX.armArmorCost;
			},
			description = "Powers up your Spiral Crush Buster.",
			price = MegamanX.armArmorCost.ToString(),
		};
		menu[(1, 4)] = new Skill {
			name = "Boots",
			isUnlocked = player => player.legArmorNum == (int)ArmorId.Light,
			canUnlock = player => player.legArmorNum != (int)ArmorId.Light && player.currency >= MegamanX.bootsArmorCost || player.bootsArmorsPurchased[0] == true,
			canLock = player => player.legArmorNum == (int)ArmorId.Light,
			unlock = (player) => {
				setLegs(player, (int)ArmorId.Light);
				player.legArmorNum = (int)ArmorId.Light;
				if (player.bootsArmorsPurchased[0] == false)
					player.currency -= MegamanX.bootsArmorCost;
				player.bootsArmorsPurchased[0] = true;
			},
			lockit = (player) => {
				setLegs(player, (int)ArmorId.None);
				player.legArmorNum = (int)ArmorId.None;
				if (player.bootsArmorsPurchased[0] == false)
					player.currency += MegamanX.bootsArmorCost;
			},
			description = "Ground Dash 15% faster and longer.",
			price = MegamanX.bootsArmorCost.ToString(),
		};
		#endregion
		#region Giga
		menu[(2, 1)] = new Skill {
			name = "Helmet",
			isUnlocked = player => player.helmetArmorNum == (int)ArmorId.Giga,
			canUnlock = player => player.helmetArmorNum != (int)ArmorId.Giga && player.currency >= MegamanX.headArmorCost || player.headArmorsPurchased[1] == true,
			canLock = player => player.helmetArmorNum == (int)ArmorId.Giga,
			unlock = (player) => {
				setHelmet(player, (int)ArmorId.Giga);
				player.helmetArmorNum = (int)ArmorId.Giga;
				if (player.headArmorsPurchased[1] == false)
					player.currency -= MegamanX.headArmorCost;
				player.headArmorsPurchased[1] = true;
			},
			lockit = (player) => {
				setHelmet(player, (int)ArmorId.None);
				player.helmetArmorNum = (int)ArmorId.None;
				if (player.headArmorsPurchased[1] == false)
					player.currency += MegamanX.headArmorCost;
				player.helmetArmorNum = (int)ArmorId.None;
			},
			description = "Trace enemy hp and positioning\nby pressing SPECIAL button.",
			price = MegamanX.headArmorCost.ToString(),
		};
		menu[(2, 2)] = new Skill {
			name = "Body",
			isUnlocked = player => player.bodyArmorNum == (int)ArmorId.Giga,
			canUnlock = player => player.bodyArmorNum != (int)ArmorId.Giga && player.currency >= MegamanX.chestArmorCost || player.bodyArmorsPurchased[1] == true,
			canLock = player => player.bodyArmorNum == (int)ArmorId.Giga,
			unlock = (player) => {
				setBody(player, (int)ArmorId.Giga);
				player.bodyArmorNum = (int)ArmorId.Giga;
				if (player.bodyArmorsPurchased[1] == false)
					player.currency -= MegamanX.chestArmorCost;
				player.addGigaCrush();
				player.bodyArmorsPurchased[1] = true;
			},
			lockit = (player) => {
				setBody(player, (int)ArmorId.None);
				player.bodyArmorNum = (int)ArmorId.None;
				if (player.bodyArmorsPurchased[1] == false)
					player.currency += MegamanX.chestArmorCost;
				player.bodyArmorNum = (int)ArmorId.None;
				player.removeGigaCrush();
			},
			description = "Grants the Giga Crush attack.\nReduces Damage by 12.5%",
			price = MegamanX.chestArmorCost.ToString(),
		};
		menu[(2, 3)] = new Skill {
			name = "Arm",
			isUnlocked = player => player.armArmorNum == (int)ArmorId.Giga,
			canUnlock = player => player.armArmorNum != (int)ArmorId.Giga && player.currency >= MegamanX.armArmorCost || player.armArmorsPurchased[1] == true,
			canLock = player => player.armArmorNum == (int)ArmorId.Giga,
			unlock = (player) => {
				setArm(player, (int)ArmorId.Giga);
				player.removeHyperCharge();
				player.armArmorNum = (int)ArmorId.Giga;
				if (player.armArmorsPurchased[1] == false)
					player.currency -= MegamanX.armArmorCost;
				player.armArmorsPurchased[1] = true;
			},
			lockit = (player) => {
				setArm(player, (int)ArmorId.None);
				player.armArmorNum = (int)ArmorId.None;
				if (player.armArmorsPurchased[1] == false)
					player.currency += MegamanX.armArmorCost;
				player.armArmorNum = (int)ArmorId.None; ;
			},
			description = "Grants the Double X-Buster.\nStore an extra charge shot.",
			price = MegamanX.armArmorCost.ToString(),
		};
		menu[(2, 4)] = new Skill {
			name = "Boots",
			isUnlocked = player => player.legArmorNum == (int)ArmorId.Giga,
			canUnlock = player => player.legArmorNum != (int)ArmorId.Giga && player.currency >= MegamanX.bootsArmorCost || player.bootsArmorsPurchased[1] == true,
			canLock = player => player.legArmorNum == (int)ArmorId.Giga,
			unlock = (player) => {
				setLegs(player, (int)ArmorId.Giga);
				player.legArmorNum = (int)ArmorId.Giga;
				if (player.bootsArmorsPurchased[1] == false)
					player.currency -= MegamanX.bootsArmorCost;
				player.bootsArmorsPurchased[1] = true;
			},
			lockit = (player) => {
				setLegs(player, (int)ArmorId.None);
				player.legArmorNum = (int)ArmorId.None;
				if (player.bootsArmorsPurchased[1] == false)
					player.currency += MegamanX.bootsArmorCost;
				player.legArmorNum = (int)ArmorId.None;
			},
			description = "Air dash 15% faster and longer.",
			price = MegamanX.bootsArmorCost.ToString(),
		};
		#endregion
		#region Max
		menu[(3, 1)] = new Skill {
			name = "Helmet",
			isUnlocked = player => player.helmetArmorNum == (int)ArmorId.Max,
			canUnlock = player => player.helmetArmorNum != (int)ArmorId.Max && player.currency >= MegamanX.headArmorCost || player.headArmorsPurchased[2] == true,
			canLock = player => player.helmetArmorNum == (int)ArmorId.Max,
			unlock = (player) => {
				setHelmet(player, (int)ArmorId.Max);
				player.helmetArmorNum = (int)ArmorId.Max;
				if (player.headArmorsPurchased[2] == false)
					player.currency -= MegamanX.headArmorCost;
				player.headArmorsPurchased[2] = true;
			},
			lockit = (player) => {
				setHelmet(player, (int)ArmorId.None);
				player.helmetArmorNum = (int)ArmorId.None;
				if (player.headArmorsPurchased[2] == false)
					player.currency += MegamanX.headArmorCost;
				player.helmetArmorNum = (int)ArmorId.None;
			},
			description = "Grants minimap to track down enemies\nthrough The State of the Art Space Satellite.",
			price = MegamanX.headArmorCost.ToString(),
		};
		menu[(3, 2)] = new Skill {
			name = "Body",
			isUnlocked = player => player.bodyArmorNum == (int)ArmorId.Max,
			canUnlock = player => player.bodyArmorNum != (int)ArmorId.Max && player.currency >= MegamanX.chestArmorCost || player.bodyArmorsPurchased[2] == true,
			canLock = player => player.bodyArmorNum == (int)ArmorId.Max,
			unlock = (player) => {
				setBody(player, (int)ArmorId.Max);
				player.bodyArmorNum = (int)ArmorId.Max;
				if (player.bodyArmorsPurchased[2] == false)
					player.currency -= MegamanX.chestArmorCost;
				player.bodyArmorsPurchased[2] = true;
				player.removeGigaCrush();
			},
			lockit = (player) => {
				setBody(player, (int)ArmorId.None);
				player.bodyArmorNum = (int)ArmorId.None;
				if (player.bodyArmorsPurchased[2] == false)
					player.currency += MegamanX.chestArmorCost;
				player.bodyArmorNum = (int)ArmorId.None;
			},
			description = "Gain a Defensive Forcefield on taking\ndamage. Forcefield Defense: 25%",
			price = MegamanX.chestArmorCost.ToString(),
		};
		menu[(3, 3)] = new Skill {
			name = "Arm",
			isUnlocked = player => player.armArmorNum == (int)ArmorId.Max,
			canUnlock = player => player.armArmorNum != (int)ArmorId.Max && player.currency >= MegamanX.armArmorCost || player.armArmorsPurchased[2] == true,
			canLock = player => player.armArmorNum == (int)ArmorId.Max,
			unlock = (player) => {
				setArm(player, (int)ArmorId.Max);
				player.armArmorNum = (int)ArmorId.Max;
				if (player.armArmorsPurchased[2] == false)
					player.currency -= MegamanX.armArmorCost;
				player.addHyperCharge();
				player.armArmorsPurchased[2] = true;
			},
			lockit = (player) => {
				setArm(player, (int)ArmorId.None);
				player.armArmorNum = (int)ArmorId.None;
				if (player.armArmorsPurchased[2] == false)
					player.currency += MegamanX.armArmorCost;
				player.armArmorNum = (int)ArmorId.None;
				player.removeHyperCharge();
			},
			description = "Grants the Hyper Charge.\nGrants the Cross Charge shot.",
			price = MegamanX.armArmorCost.ToString(),
		};
		menu[(3, 4)] = new Skill {
			name = "Boots",
			isUnlocked = player => player.legArmorNum == (int)ArmorId.Max,
			canUnlock = player => player.legArmorNum != (int)ArmorId.Max && player.currency >= MegamanX.bootsArmorCost || player.bootsArmorsPurchased[2] == true,
			canLock = player => player.legArmorNum == (int)ArmorId.Max,
			unlock = (player) => {
				setLegs(player, (int)ArmorId.Max);
				player.legArmorNum = (int)ArmorId.Max;
				if (player.bootsArmorsPurchased[2] == false)
					player.currency -= MegamanX.bootsArmorCost;
				player.bootsArmorsPurchased[2] = true;
			},
			lockit = (player) => {
				setLegs(player, (int)ArmorId.None);
				player.legArmorNum = (int)ArmorId.None;
				if (player.bootsArmorsPurchased[2] == false)
					player.currency += MegamanX.bootsArmorCost;
				player.legArmorNum = (int)ArmorId.None;
			},
			description = "Grants an Upwards Dash.",
			price = MegamanX.bootsArmorCost.ToString(),
		};
		#endregion
		#region HyperChip
		menu[(4, 1)] = new Skill {
			name = "Helmet",
			isUnlocked = player => player.character is MegamanX mmx && mmx.hyperHelmetActive, //Kill me
			canUnlock = player => player.character is MegamanX mmx && player.hasAllX3Armor() && !mmx.hasAnyHyperArmor,
			canLock = player => player.character is MegamanX mmx && mmx.hyperHelmetActive,
			unlock = (player) => { if (player.character is MegamanX mmx) mmx.hyperHelmetActive = true; },
			lockit = (player) => { if (player.character is MegamanX mmx) mmx.hyperHelmetActive = false; },
			description = "ENHANCEMENT CHIP\nSlowly regenerate Health after not taking Damage.",
			price = "0",
		};
		menu[(4, 2)] = new Skill {
			name = "Body",
			isUnlocked = player => player.character is MegamanX mmx && mmx.hyperChestActive,
			canUnlock = player => player.character is MegamanX mmx && player.hasAllX3Armor() && !mmx.hasAnyHyperArmor,
			canLock = player => player.character is MegamanX mmx && player.hasAllX3Armor() && !mmx.hyperChestActive,
			unlock = (player) => { if (player.character is MegamanX mmx) mmx.hyperChestActive = true; },
			lockit = (player) => { if (player.character is MegamanX mmx) mmx.hyperChestActive = false; },
			description = "ENHANCEMENT CHIP\nForcefield Defense: 50%",
			price = "0",
		};
		menu[(4, 3)] = new Skill {
			name = "Arm",
			isUnlocked = player => player.character is MegamanX mmx && mmx.hyperArmActive,
			canUnlock = player => player.character is MegamanX mmx && player.hasAllX3Armor() && !mmx.hasAnyHyperArmor,
			canLock = player => player.character is MegamanX mmx && mmx.hyperArmActive,
			unlock = (player) => { if (player.character is MegamanX mmx) mmx.hyperArmActive = true; },
			lockit = (player) => { if (player.character is MegamanX mmx) mmx.hyperArmActive = false; },
			description = "ENHANCEMENT CHIP\nReduce ammo usage by half.",
			price = "0",
		};
		menu[(4, 4)] = new Skill {
			name = "Boots",
			isUnlocked = player => player.character is MegamanX mmx && mmx.hyperLegActive,
			canUnlock = player => player.character is MegamanX mmx && player.hasAllX3Armor() && !mmx.hasAnyHyperArmor,
			canLock = player => player.character is MegamanX mmx && mmx.hyperLegActive,
			unlock = (player) => { if (player.character is MegamanX mmx) mmx.hyperLegActive = true; },
			lockit = (player) => { if (player.character is MegamanX mmx) mmx.hyperLegActive = false; },
			description = "ENHANCEMENT CHIP\nDash Twice in the air",
			price = MegamanX.bootsArmorCost.ToString(),
		};
		#endregion
	}
	public void renderDescription(Player? p, int ud, int lr, uint ghw, uint ghh) {
		if (!menu.TryGetValue((ud, lr), out var skill)) return;
		if (string.IsNullOrWhiteSpace(skill.description)) return;
		bool met = skill.canUnlock(p);
		FontType font = met ? FontType.Blue : FontType.Grey;
		DrawWrappers.DrawRect(20, 158, 364, 180, true, new Color(0, 0, 0, 100), 1, ZIndex.HUD, false, outlineColor: Color.White);
		Fonts.drawTextEX(font, "Price: " + skill.price, ghw + 120, ghh + 64, Alignment.Left);
		Fonts.drawTextEX(font, skill.description, ghw - 170, ghh + 54, Alignment.Left);
	}

	public void handleInput(Player? player, int ud, int lr) {
		if (!menu.TryGetValue((ud, lr), out var skill)) return;
		if (Global.input.isPressedMenu(Control.MenuConfirm) && skill.canUnlock(player)) {
			skill.unlock(player);
			Global.playSound("ching");
		} else if (Global.input.isPressedMenu(Control.MenuAlt) && skill.canLock(player)) {
			skill.lockit(player);
			Global.playSound("busterX3");
		}
	}

	public void renderCursor(int ud, int lr, AnimData cursor, uint ghw, uint ghh) {
		Dictionary<(int, int), (float x, float y)> positions = new() {
			{(1, 1), (-150, -50)},
			{(1, 2), (-130, -50)},
			{(1, 3), (-110, -50)},
			{(1, 4), (-90, -50)},
			{(2, 1), (-150, -30)},
			{(2, 2), (-130, -30)},
			{(2, 3), (-110, -30)},
			{(2, 4), (-90, -30)},
			{(3, 1), (-150, -10)},
			{(3, 2), (-130, -10)},
			{(3, 3), (-110, -10)},
			{(3, 4), (-90, -10)},
			{(4, 1), (-150, 10)},
			{(4, 2), (-130, 10)},
			{(4, 3), (-110, 10)},
			{(4, 4), (-90, 10)},
		};
		if (positions.TryGetValue((ud, lr), out var pos))
			cursor.drawToHUD(0, ghw + pos.x, ghh + pos.y);
	}
	public void renderIcons(int frame, AnimData icon, uint ghw, uint ghh, float opacity) {
		Dictionary<int, Point> iconsD = new()
		{
			{ 4, new Point(-150, -50) }, //Helmet
			{ 7, new Point(-90, -50) }, //Foot
			{ 6, new Point(-110, -50) }, //Arm
			{ 5, new Point(-130, -50) }, //Body
			{ 8, new Point(-150, -30) },   //Helmet
			{ 9, new Point(-130, -30) }, //Body	
			{ 10, new Point(-110, -30) },  //Arm
			{ 11, new Point(-90, -30) },  //Foot
			{ 12, new Point(-150, -10) }, //Helmet
			{ 13, new Point(-130, -10) }, //Body
			{ 14, new Point(-110, -10) }, //Arm
			{ 15, new Point(-90, -10) }, //Foot
			{ 0, new Point(-150, 10) }, //Helmet
			{ 1, new Point(-130, 10) }, //Body
			{ 2, new Point(-110, 10) }, //Arm
			{ 3, new Point(-90, 10) }, //Foot
        };
		foreach (var icons in iconsD) {
			icon.drawToHUD(icons.Key, ghw + icons.Value.x, ghh + icons.Value.y, opacity);
		}
	}
	//Kill me
	public void setHelmet(Player player, int armorID) {
		if (player.character is MegamanX mmx && mmx != null)
			mmx.helmetArmor = (ArmorId)armorID;
	}
	public void setBody(Player player, int armorID) {
		if (player.character is MegamanX mmx && mmx != null)
			mmx.chestArmor = (ArmorId)armorID;
	}
	public void setArm(Player player, int armorID) {
		if (player.character is MegamanX mmx && mmx != null)
			mmx.armArmor = (ArmorId)armorID;
	}
	public void setLegs(Player player, int armorID) {
		if (player.character is MegamanX mmx && mmx != null)
			mmx.legArmor = (ArmorId)armorID;
	}
}
public class UpgradeArmorMenuEX : IMainMenu {
	public IMainMenu prevMenu;
	public UpgradeArmorMenuEX(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
		menu = Global.sprites["menu_xdefault"];
		cursor = Global.sprites["axl_cursor"]; ;
		icon = Global.sprites["hud_upgradearmor"];
	}
	public int frame;
	public static int xGame = 1;
	public static int slot = 1;
	public int snesLR = 1, snesUD = 1;
	SNESArmorHandler snesArmorHandler = new();
	AnimData cursor, menu, icon;
	Player mainP => Global.level.mainPlayer;
	uint ghw => Global.halfScreenW;
	uint ghh => Global.halfScreenH;
	

	public void update() {
		if (mainP.character is MegamanX mmx && mmx != null) {
			if (mmx.hasAnyHyperArmor && mmx.fullArmor != ArmorId.Max) {
				mmx.hyperHelmetActive = false;
				mmx.hyperChestActive = false;
				mmx.hyperArmActive = false;
				mmx.hyperLegActive = false;
			}
		}
		if (xGame >= 1) {
			switch (slot) {
				case 1:
					Helpers.menuLeftRightInc(ref snesLR, 1, 4, true, true);
					Helpers.menuUpDown(ref snesUD, 1, 4, true, true);
					snesArmorHandler.handleInput(mainP, snesUD, snesLR);
					break;
			}
		}
		slotLogic();
		xGameV();
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		DrawWrappers.DrawRect(364, 40, 20, 180, true, new Color(0, 0, 0, 100), 1,
		ZIndex.HUD, false, outlineColor: Color.White);
		menu.drawToHUD(0, ghw, ghh, 0.5f);
		menuX();
		DrawTextStuff();
		LowerMenuText();
		if (xGame >= 1) {
			snesArmorHandler.renderIcons(frame, icon, ghw, ghh, 1);
			snesArmorHandler.renderCursor(snesUD, snesLR, cursor, ghw, ghh);
			snesArmorHandler.renderDescription(mainP, snesUD, snesLR, ghw, ghh);
		}
		Fonts.drawText(FontType.Blue, "Metals: " + mainP.currency.ToString(), 294, 160, Alignment.Left);
	}
	public void xGameV() {
		if (Global.input.isPressedMenu(Control.WeaponLeft)) {
			xGame--;
			if (xGame < 1) {
				xGame = 1;
				if (!Global.level.server.disableHtSt) {
					UpgradeMenu.onUpgradeMenu = true;
					Menu.change(new UpgradeMenu(prevMenu));
					return;
				}
			}
		} else if (Global.input.isPressedMenu(Control.WeaponRight)) {
			xGame++;
			if (xGame > 1) {
				xGame = 2;
				if (!Global.level.server.disableHtSt) {
					UpgradeMenu.onUpgradeMenu = true;
					Menu.change(new UpgradeMenu(prevMenu));
					return;
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}
	public void DrawTextStuff() {
		string upgrades = xGame switch {
			1 => "SNES",
			2 => "PS1",
			3 => "PS2",
			_ => "ERROR"
		};
		Fonts.drawText(
			FontType.Pink, string.Format($"Upgrade {upgrades}", xGame),
			Global.screenW * 0.5f, 20, Alignment.Center
		);
		if (Global.frameCount % 60 < 30) {
			bool stEnabled = !Global.level.server.disableHtSt;
			string leftText = xGame switch {
				1 when stEnabled => "Items",
				2 => "SNES",
				3 => "PS2",
				_ => ""
			};
			string rightText = xGame switch {
				1 => "PS1",
				2 => "PS2",
				3 when stEnabled => "Items",
				_ => ""
			};
			if (leftText != "") {
				Fonts.drawText(
					FontType.DarkPurple, "<",
					14, ghh, Alignment.Center
				);
				Fonts.drawText(
					FontType.DarkPurple, "",//leftText,
					14, ghh + 15, Alignment.Center
				);
			}
			if (rightText != "") {
				Fonts.drawText(
					FontType.DarkPurple, ">",
					Global.screenW - 14, ghh, Alignment.Center
				);
				Fonts.drawText(
					FontType.DarkPurple, "",//rightText,
					Global.screenW - 14, ghh + 15, Alignment.Center
				);
			}
		}
	}
	public void LowerMenuText() {
		//LowerMenu
		Fonts.drawTextEX(FontType.RedishOrange, "[CMD]: Change Row", 10, 184);
		Fonts.drawTextEX(FontType.RedishOrange, "[WeaponL]/[WeaponR]: Change Menu", 10, 198);
		Fonts.drawTextEX(FontType.Grey,
			"[OK]:Unlock [ALT]:Disable", 263, 184
		);
		Fonts.drawTextEX(FontType.Grey,
			"[MLEFT],[MRIGHT],[MDOWN],[MUP]: Travel", 216, 198, Alignment.Left
		);
	}
	public void menuX() {
		switch (mainP.helmetArmorNum) {
			case 1: Global.sprites["menu_xhelmet"].drawToHUD(0, ghw, ghh, 0.5f); break;
			case 2: Global.sprites["menu_xhelmet2"].drawToHUD(0, ghw, ghh, 0.5f); break;
			case 3: Global.sprites["menu_xhelmet3"].drawToHUD(0, ghw, ghh, 0.5f); break;
		}
		switch (mainP.bodyArmorNum) {
			case 1: Global.sprites["menu_xbody"].drawToHUD(0, ghw, ghh, 0.5f); break;
			case 2: Global.sprites["menu_xbody2"].drawToHUD(0, ghw, ghh, 0.5f); break;
			case 3: Global.sprites["menu_xbody3"].drawToHUD(0, ghw, ghh, 0.5f); break;
		}
		switch (mainP.armArmorNum) {
			case 1: Global.sprites["menu_xarm"].drawToHUD(0, ghw, ghh, 0.5f); break;
			case 2: Global.sprites["menu_xarm2"].drawToHUD(0, ghw, ghh, 0.5f); break;
			case 3: Global.sprites["menu_xarm3"].drawToHUD(0, ghw, ghh, 0.5f); break;
		}
		switch (mainP.legArmorNum) {
			case 1: Global.sprites["menu_xboots"].drawToHUD(0, ghw, ghh, 0.5f); break;
			case 2: Global.sprites["menu_xboots2"].drawToHUD(0, ghw, ghh, 0.5f); break;
			case 3: Global.sprites["menu_xboots3"].drawToHUD(0, ghw, ghh, 0.5f); break;
		}
	}

	public void slotLogic() {
		if (Global.input.isPressedMenu(Control.Special2)) {
			slot++;
			Global.playSound("menuX3");
			if (slot > 1) slot = 1;
			snesLR = 1;
			snesUD = 1;
		}
	}
}