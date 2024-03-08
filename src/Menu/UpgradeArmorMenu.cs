namespace MMXOnline;

public class UpgradeArmorMenu : IMainMenu {
	public int selectArrowPosY;
	public IMainMenu prevMenu;

	public Point optionPos1 = new Point(25, 40);
	public Point optionPos2 = new Point(25, 40 + 36);
	public Point optionPos3 = new Point(25, 40 + 36 * 2);
	public Point optionPos4 = new Point(25, 40 + 36 * 3);

	public Level level { get { return Global.level; } }
	public Player mainPlayer { get { return Global.level.mainPlayer; } }

	public static int xGame = 1;

	public UpgradeArmorMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
	}

	public void update() {
		if (updateHyperArmorUpgrades(mainPlayer)) return;

		// Should not be able to reach here but preventing upgrades just in case
		if (!mainPlayer.canUpgradeXArmor()) return;

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

		if (mainPlayer.hasGoldenArmor() || mainPlayer.hasUltimateArmor()) {
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
				} else if (mainPlayer.hasAllX3Armor() && !mainPlayer.hasChip(2)) {
					mainPlayer.setChipNum(2, false);
					Global.playSound("ching");
				}
			}
			if (selectArrowPosY == 1) {
				if (mainPlayer.bodyArmorNum != xGame) {
					if (!mainPlayer.isBodyArmorPurchased(xGame)) {
						if (mainPlayer.currency >= MegamanX.bodyArmorCost) {
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
				} else if (mainPlayer.hasAllX3Armor() && !mainPlayer.hasChip(1)) {
					mainPlayer.setChipNum(1, false);
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
				} else if (mainPlayer.hasAllX3Armor() && !mainPlayer.hasChip(3)) {
					mainPlayer.setChipNum(3, false);
					Global.playSound("ching");
				}
			}
			if (selectArrowPosY == 3) {
				if (mainPlayer.bootsArmorNum != xGame) {
					if (!mainPlayer.isBootsArmorPurchased(xGame)) {
						if (mainPlayer.currency >= MegamanX.bootsArmorCost) {
							purchaseBootsArmor(mainPlayer, xGame);
							Global.playSound("ching");
							if (mainPlayer.bootsArmorNum == 0) {
								upgradeBootsArmor(mainPlayer, xGame);
							}
						}
					} else {
						upgradeBootsArmor(mainPlayer, 0);
						upgradeBootsArmor(mainPlayer, xGame);
						Global.playSound("ching");
					}
				} else if (mainPlayer.hasAllX3Armor() && !mainPlayer.hasChip(0)) {
					mainPlayer.setChipNum(0, false);
					Global.playSound("ching");
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuAlt)) {
			if (selectArrowPosY == 0) {
				if (mainPlayer.helmetArmorNum == xGame) {
					if (mainPlayer.hasAllX3Armor() && mainPlayer.hasChip(2)) {
						mainPlayer.setChipNum(2, true);
					} else {
						upgradeHelmetArmor(mainPlayer, 0);
					}
				}
			}
			if (selectArrowPosY == 1) {
				if (mainPlayer.bodyArmorNum == xGame) {
					if (mainPlayer.hasAllX3Armor() && mainPlayer.hasChip(1)) {
						mainPlayer.setChipNum(1, true);
					} else {
						upgradeBodyArmor(mainPlayer, 0);
					}
				}
			}
			if (selectArrowPosY == 2) {
				if (mainPlayer.armArmorNum == xGame) {
					if (mainPlayer.hasAllX3Armor() && mainPlayer.hasChip(3)) {
						mainPlayer.setChipNum(2, true);
					} else {
						upgradeArmArmor(mainPlayer, 0);
					}
				}
			}
			if (selectArrowPosY == 3) {
				if (mainPlayer.bootsArmorNum == xGame) {
					if (mainPlayer.hasAllX3Armor() && mainPlayer.hasChip(0)) {
						mainPlayer.setChipNum(0, true);
					} else {
						upgradeBootsArmor(mainPlayer, 0);
					}
				}
			}
		}
	}

	public static void upgradeHelmetArmor(Player player, int type) {
		player.helmetArmorNum = type;
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
		}

		if (type == 0) {
			player.removeGigaCrush();
		}
	}

	public static void purchaseBodyArmor(Player player, int type) {
		if (!player.isBodyArmorPurchased(type)) {
			player.currency -= MegamanX.bodyArmorCost;
			player.setBodyArmorPurchased(type);
		}
	}

	public static void upgradeArmArmor(Player player, int type) {
		player.armArmorNum = type;
		if (type == 3) {
			player.addHyperCharge();
		}
		if (type == 0) {
			player.removeHyperCharge();
		}
	}

	public static void purchaseArmArmor(Player player, int type) {
		if (type != 0 && !player.isArmArmorPurchased(type)) {
			player.currency -= MegamanX.armArmorCost;
			player.setArmArmorPurchased(type);
		}
	}

	public static void upgradeBootsArmor(Player player, int type) {
		player.bootsArmorNum = type;
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
			1 => "Light",
			2 => "Giga",
			3 => "Max",
			_ => "ERROR"
		};
		Fonts.drawText(
			FontType.Yellow, string.Format($"Upgrade {armorName} Armor", xGame),
			Global.screenW * 0.5f, 20, Alignment.Center
		);
		Fonts.drawText(FontType.Golden, Global.nameCoins + ": " + mainPlayer.currency, 20, 20);

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

		Global.sprites["menu_xdefault"].drawToHUD(0, 300, 110);

		if (mainPlayer.hasUltimateArmor()) Global.sprites["menu_xultimate"].drawToHUD(0, 300, 110);
		else if (mainPlayer.hasGoldenArmor()) Global.sprites["menu_xgolden"].drawToHUD(0, 300, 110);
		else {
			if (mainPlayer.helmetArmorNum == 1) Global.sprites["menu_xhelmet"].drawToHUD(0, 300, 110);
			if (mainPlayer.bodyArmorNum == 1) Global.sprites["menu_xbody"].drawToHUD(0, 300, 110);
			if (mainPlayer.armArmorNum == 1) Global.sprites["menu_xarm"].drawToHUD(0, 300, 110);
			if (mainPlayer.bootsArmorNum == 1) Global.sprites["menu_xboots"].drawToHUD(0, 300, 110);

			if (mainPlayer.helmetArmorNum == 2) Global.sprites["menu_xhelmet2"].drawToHUD(0, 300, 110);
			if (mainPlayer.bodyArmorNum == 2) Global.sprites["menu_xbody2"].drawToHUD(0, 300, 110);
			if (mainPlayer.armArmorNum == 2) Global.sprites["menu_xarm2"].drawToHUD(0, 300, 110);
			if (mainPlayer.bootsArmorNum == 2) Global.sprites["menu_xboots2"].drawToHUD(0, 300, 110);

			if (mainPlayer.helmetArmorNum >= 3) Global.sprites["menu_xhelmet3"].drawToHUD(0, 300, 110);
			if (mainPlayer.bodyArmorNum >= 3) Global.sprites["menu_xbody3"].drawToHUD(0, 300, 110);
			if (mainPlayer.armArmorNum >= 3) Global.sprites["menu_xarm3"].drawToHUD(0, 300, 110);
			if (mainPlayer.bootsArmorNum >= 3) Global.sprites["menu_xboots3"].drawToHUD(0, 300, 110);
		}

		Point optionPos = new Point();
		if (selectArrowPosY == 0) optionPos = optionPos1;
		if (selectArrowPosY == 1) optionPos = optionPos2;
		if (selectArrowPosY == 2) optionPos = optionPos3;
		if (selectArrowPosY == 3) optionPos = optionPos4;

		float yOff = xGame == 3 && mainPlayer.hasAllX3Armor() ? 9 : -1;
		Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 8, optionPos.y + 4 + yOff);

		bool showChips = mainPlayer.hasAllX3Armor() && xGame == 3;

		// Head section
		Fonts.drawText(FontType.Yellow, "Head Parts", optionPos1.x, optionPos1.y, selected: selectArrowPosY == 0 && !showChips);
		Fonts.drawText(FontType.Green, getHeadArmorMessage(), optionPos1.x + 60, optionPos1.y);
		if (xGame == 1) {
			Fonts.drawText(FontType.Blue, "Grants a headbutt attack on jump.", optionPos1.x + 5, optionPos1.y + 10);
		}
		if (xGame == 2) {
			Fonts.drawText(FontType.Blue, "Scan and tag enemies with SPECIAL.", optionPos1.x + 5, optionPos1.y + 10);
		}
		if (xGame == 3) {
			if (mainPlayer.hasAllX3Armor()) {
				Fonts.drawText(FontType.Golden, "ENHANCEMENT CHIP", optionPos1.x + 5, optionPos1.y + 10, selected: selectArrowPosY == 0);
				Fonts.drawText(FontType.Blue, "Slowly regenerate health.", optionPos1.x + 5, optionPos1.y + 20);
			} else {
				Fonts.drawText(FontType.Blue, "Gain a radar to detect enemies.", optionPos1.x + 5, optionPos1.y + 10);
			}
		}

		// Body section
		Fonts.drawText(FontType.Yellow, "Body Parts", optionPos2.x, optionPos2.y, selected: selectArrowPosY == 1 && !showChips);
		Fonts.drawText(FontType.Green, getBodyArmorMessage(), optionPos2.x + 60, optionPos2.y);
		if (xGame == 1) {
			Fonts.drawText(FontType.Blue, string.Format("Reduces damage and flinch received."), optionPos2.x + 5, optionPos2.y + 10);
		}
		if (xGame == 2) {
			Fonts.drawText(FontType.Blue, "Grants the Giga Crush attack", optionPos2.x + 5, optionPos2.y + 10);
			Fonts.drawText(FontType.Blue, "and reduces damage received.", optionPos2.x + 5, optionPos2.y + 20);
		}
		if (xGame == 3) {
			if (mainPlayer.hasAllX3Armor()) {
				Fonts.drawText(FontType.Golden, "ENHANCEMENT CHIP", optionPos2.x + 5, optionPos2.y + 10, selected: selectArrowPosY == 1);
				Fonts.drawText(FontType.Blue, "Improves barrier defense.", optionPos2.x + 5, optionPos2.y + 20);
			} else {
				Fonts.drawText(FontType.Blue, "Gain a barrier on taking damage.", optionPos2.x + 5, optionPos2.y + 10);
			}
		}

		// Arm section
		Fonts.drawText(FontType.Yellow, "Arm Parts", optionPos3.x, optionPos3.y, selected: selectArrowPosY == 2 && !showChips);
		Fonts.drawText(FontType.Green, getArmArmorMessage(), optionPos3.x + 60, optionPos3.y);
		if (xGame == 1) Fonts.drawText(FontType.Blue, "Charge shots 50% faster.", optionPos3.x + 5, optionPos3.y + 10);
		if (xGame == 2) Fonts.drawText(FontType.Blue, "Store an extra charge shot.", optionPos3.x + 5, optionPos3.y + 10);
		if (xGame == 3) {
			if (mainPlayer.hasAllX3Armor()) {
				Fonts.drawText(FontType.Golden, "ENHANCEMENT CHIP", optionPos3.x + 5, optionPos3.y + 10);
				Fonts.drawText(FontType.Blue, "Reduce ammo usage by half.", optionPos3.x + 5, optionPos3.y + 20);
			} else {
				Fonts.drawText(FontType.Blue, "Grants the Hyper Charge", optionPos3.x + 5, optionPos3.y + 10);
				Fonts.drawText(FontType.Blue, "and Cross Shot abilities.", optionPos3.x + 5, optionPos3.y + 20);
			}
		}

		// Foot section
		Fonts.drawText(FontType.Yellow, "Foot Parts", optionPos4.x, optionPos4.y, selected: selectArrowPosY == 3 && !showChips);
		Fonts.drawText(FontType.Green, getBootsArmorMessage(), optionPos4.x + 60, optionPos4.y);
		if (xGame == 1) {
			Fonts.drawText(FontType.Blue, "Ground dash 15% faster.", optionPos4.x + 5, optionPos4.y + 10);
		}
		if (xGame == 2) {
			Fonts.drawText(FontType.Blue, "Air dash 15% faster.", optionPos4.x + 5, optionPos4.y + 10);
		}
		if (xGame == 3) {
			if (mainPlayer.hasAllX3Armor()) {
				Fonts.drawText(FontType.Golden, "ENHANCEMENT CHIP", optionPos4.x + 5, optionPos4.y + 10, selected: selectArrowPosY == 3);
				Fonts.drawText(FontType.Blue, "Dash twice in the air.", optionPos4.x + 5, optionPos4.y + 20);
			} else {
				Fonts.drawText(FontType.Blue, "Gain a midair upward dash.", optionPos4.x + 5, optionPos4.y + 10);
			}
		}

		if (mainPlayer.hasChip(2)) Global.sprites["menu_chip"].drawToHUD(0, 220 - 4, optionPos1.y);
		if (mainPlayer.hasChip(1)) Global.sprites["menu_chip"].drawToHUD(0, 220 - 4, optionPos2.y);
		if (mainPlayer.hasChip(3)) Global.sprites["menu_chip"].drawToHUD(0, 220 - 38, optionPos3.y);
		if (mainPlayer.hasChip(0)) Global.sprites["menu_chip"].drawToHUD(0, 220 - 28, optionPos4.y);

		//drawHyperArmorUpgrades(mainPlayer, 0);

		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change Armor Set",
			40, Global.screenH - 28
		);
		Fonts.drawTextEX(
			FontType.Grey, "[OK]: Upgrade, [ALT]: Unupgrade, [BACK]: Back",
			40, Global.screenH - 18
		);
	}

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
		return $" ({MegamanX.bodyArmorCost} {Global.nameCoins})";
	}

	public string getArmArmorMessage() {
		if (mainPlayer.isArmArmorPurchased(xGame)) {
			return mainPlayer.armArmorNum == xGame ? " (Active)" : " (Bought)";
		}
		return $" ({MegamanX.armArmorCost} {Global.nameCoins})";
	}

	public string getBootsArmorMessage() {
		if (mainPlayer.isBootsArmorPurchased(xGame)) {
			return mainPlayer.bootsArmorNum == xGame ? " (Active)" : " (Bought)";
		}
		return $" ({MegamanX.bootsArmorCost} {Global.nameCoins})";
	}
}
