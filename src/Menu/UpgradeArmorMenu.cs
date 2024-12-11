namespace MMXOnline;
using SFML.Graphics;

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
		if (mmx?.hasFullHyperMaxArmor == true || mmx?.hasUltimateArmor == true) {
			return;
		}
		if (mmx?.hasFullHyperMaxArmor == true) {
			Menu.change(new UpgradeArmorMenuGolden(GoldenMenu));
		}
		if (mmx?.hasUltimateArmor == true) {
			Menu.change(new UpgradeArmorMenuUAX(UAXMenu));
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
			1 => "Ligth Armor",
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
				Fonts.drawText(FontType.Blue, "Ground Dash 15% Faster.", optionPos4.x + 5, optionPos4.y + 10);
				break;
			case 2: //Second
				Fonts.drawText(FontType.Blue, "Trace enemy hp and positioning", optionPos1.x + 5, optionPos1.y + 10);
				Fonts.drawText(FontType.Blue, "by pressing SPECIAL button.", optionPos1.x + 5, optionPos1.y + 20);
				Fonts.drawText(FontType.Blue, "Grants the Giga Crush attack", optionPos2.x + 5, optionPos2.y + 10);
				Fonts.drawText(FontType.Blue, "Reduces Damage by 12.5%", optionPos2.x + 5, optionPos2.y + 20);
				Fonts.drawText(FontType.Blue, "Grants the Double X-Buster.", optionPos3.x + 5, optionPos3.y + 10);
				Fonts.drawText(FontType.Blue, "Store an extra charge shot.", optionPos3.x + 5, optionPos3.y + 20);
				Fonts.drawText(FontType.Blue, "Air dash 15% Faster.", optionPos4.x + 5, optionPos4.y + 10);
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
				/*
				if (mainPlayer.hasChip(2)) Global.sprites["menu_x3armors"].drawToHUD(5, 313, 27);
				if (mainPlayer.hasChip(1)) Global.sprites["menu_x3armors"].drawToHUD(4, 315, 65);
				if (mainPlayer.hasChip(3)) Global.sprites["menu_x3armors"].drawToHUD(6, 331, 74);
				if (mainPlayer.hasChip(0)) Global.sprites["menu_x3armors"].drawToHUD(7, 295, 142);
				*/
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
