using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class UpgradeMenu : IMainMenu {
	public static int selectArrowPosY;
	public IMainMenu prevMenu;
	public static bool onUpgradeMenu = true;
	public int subtankCost = 4;
	public List<Weapon> subtankTargets = new List<Weapon>();
	public static int subtankTargetIndex;
	public int startX = 25;
	public bool isFillingSubtank;
	public static float subtankDelay = 0;
	public const float maxSubtankDelay = 2;

	public List<Point> optionPositions = new List<Point>();

	public UpgradeMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
		optionPositions.Add(new Point(startX, 60));
		optionPositions.Add(new Point(startX, 80));
		optionPositions.Add(new Point(startX, 100));
		optionPositions.Add(new Point(startX, 120));
		optionPositions.Add(new Point(startX, 140));

		if (selectArrowPosY >= Global.level.mainPlayer.subtanks.Count + 1) {
			selectArrowPosY = Global.level.mainPlayer.subtanks.Count;
		}
	}

	public int getMaxIndex() {
		var mainPlayer = Global.level.mainPlayer;
		return Math.Clamp(2 + mainPlayer.subtanks.Count, 1, getMaxSubTanks() + 1);
	}

	public static int getHeartTankCost() {
		if (Global.level.server?.customMatchSettings != null) {
			return Global.level.server.customMatchSettings.heartTankCost;
		}
		return 2;
	}
	public static int getSubTankCost() {
		if (Global.level.server?.customMatchSettings != null) {
			return Global.level.server.customMatchSettings.subTankCost;
		}
		return 4;
	}

	public static int getMaxHeartTanks() {
		return Global.level.server?.customMatchSettings?.maxHeartTanks ?? 8;
	}

	public static int getMaxSubTanks() {
		return Global.level.server?.customMatchSettings?.maxSubTanks ?? 2;
	}

	public Player mainPlayer {
		get { return Global.level.mainPlayer; }
	}

	public bool canUseSubtankInMenu(bool canUseSubtank) {
		if (!canUseSubtank) return false;
		return subtankDelay == 0;
	}

	public void update() {
		//if (UpgradeArmorMenu.updateHyperArmorUpgrades(mainPlayer)) return;

		subtankTargets.Clear();
		if (mainPlayer.isSigma && mainPlayer.character is BaseSigma sigma) {
			if (mainPlayer.currentMaverick != null &&
				mainPlayer.currentMaverick.controlMode == MaverickModeId.TagTeam
			) {
				var currentMaverickWeapon = mainPlayer.weapons.FirstOrDefault(
					w => w is MaverickWeapon mw && mw.maverick == mainPlayer.currentMaverick
				);
				if (currentMaverickWeapon != null) {
					subtankTargets.Add(currentMaverickWeapon);
				}
			} else if (sigma.loadout.commandMode != (int)MaverickModeId.Striker) {
				subtankTargets = mainPlayer.weapons.FindAll(
					w => (w is MaverickWeapon mw && mw.maverick != null) || w is SigmaMenuWeapon
				).ToList();
			}
		}

		if (subtankTargets.Count > 0 && selectArrowPosY >= 1) {
			Helpers.menuLeftRightInc(ref subtankTargetIndex, 0, subtankTargets.Count - 1);
		}

		if (!subtankTargets.InRange(subtankTargetIndex)) subtankTargetIndex = 0;

		if (Global.input.isPressedMenu(Control.MenuLeft)) {
			if (mainPlayer.realCharNum == 0) {
				if (mainPlayer.canUpgradeXArmor()) {
					if (Options.main.oldUpgradeMenuX) {
						UpgradeArmorMenu.xGame = 3;
						Menu.change(new UpgradeArmorMenu(this));
					} else if (!Options.main.oldUpgradeMenuX) {
						UpgradeArmorMenuEX.xGame = 1;
						Menu.change(new UpgradeArmorMenuEX(this));
					}
					onUpgradeMenu = false;
					return;
				}
			}
		}

		if (Global.input.isPressedMenu(Control.MenuRight)) {
			if (mainPlayer.realCharNum == 0) {
				if (mainPlayer.canUpgradeXArmor()) {
					if (Options.main.oldUpgradeMenuX) {
						UpgradeArmorMenu.xGame = 1;
						Menu.change(new UpgradeArmorMenu(this));
					} else if (!Options.main.oldUpgradeMenuX) {
						UpgradeArmorMenuEX.xGame = 1;
						Menu.change(new UpgradeArmorMenuEX(this));
					}
					onUpgradeMenu = false;
					return;
				}
			} else if (mainPlayer.realCharNum == 2) {
				Menu.change(new SelectVileArmorMenu(prevMenu));
				onUpgradeMenu = false;
				return;
			}
		}

		Helpers.menuUpDown(ref selectArrowPosY, 0, getMaxIndex() - 1);

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectArrowPosY == 0) {
				if (mainPlayer.heartTanks < getMaxHeartTanks() && mainPlayer.currency >= getHeartTankCost()) {
					mainPlayer.currency -= getHeartTankCost();
					mainPlayer.heartTanks++;
					Global.playSound("hearthX1");
					if (mainPlayer.character != null) {
						Character chara = mainPlayer.character;
						chara.heartTanks++;
						decimal currentMaxHp = chara.maxHealth;
						chara.maxHealth = chara.getMaxHealth();
						chara.addHealth(MathInt.Ceiling(chara.maxHealth - currentMaxHp));
					} else {
						mainPlayer.maxHealth = mainPlayer.getMaxHealth();
					}
					/*
					if (mainPlayer.character?.vileStartRideArmor != null) {
						mainPlayer.character.vileStartRideArmor.addHealth(mainPlayer.getHeartTankModifier());
					}
					else if (mainPlayer?.currentMaverick != null) {
						mainPlayer.currentMaverick.addHealth(mainPlayer.getHeartTankModifier(), false);
						mainPlayer.currentMaverick.maxHealth += mainPlayer.getHeartTankModifier();
					}
					*/
				}
			} else if (selectArrowPosY >= 1) {
				if (mainPlayer.subtanks.Count < selectArrowPosY && mainPlayer.currency >= getSubTankCost()) {
					mainPlayer.currency -= getSubTankCost();
					mainPlayer.subtanks.Add(new SubTank());
					Global.playSound("upgrade");
				} else if (mainPlayer.subtanks.InRange(selectArrowPosY - 1)) {
					bool maverickUsed = false;
					if (subtankTargets.Count > 0) {
						var currentTarget = subtankTargets[subtankTargetIndex];
						if (currentTarget is MaverickWeapon mw &&
							mw.maverick != null &&
							canUseSubtankInMenu(mw.canUseSubtank(mainPlayer.subtanks[selectArrowPosY - 1]))
						) {
							mainPlayer.subtanks[selectArrowPosY - 1].use(mw.maverick);
							maverickUsed = true;
						}
					}

					if (!maverickUsed && canUseSubtankInMenu(
						mainPlayer.canUseSubtank(mainPlayer.subtanks[selectArrowPosY - 1]))
					) {
						mainPlayer.subtanks[selectArrowPosY - 1].use(mainPlayer.character);
					}
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		var mainPlayer = Global.level.mainPlayer;
		var gameMode = Global.level.gameMode;

		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);

		Global.sprites["cursor"].drawToHUD(0, optionPositions[0].x - 8, optionPositions[selectArrowPosY].y + 4);

		Fonts.drawText(FontType.Yellow, "Upgrade Menu", Global.screenW * 0.5f, 16, Alignment.Center);
		Fonts.drawText(
			FontType.Golden,
			Global.nameCoins + ": " + mainPlayer.currency,
			Global.screenW * 0.5f, 26, Alignment.Center
		);
		int maxHeartTanks = getMaxHeartTanks();
		int distance = 20;
		if (maxHeartTanks > 16) {
			distance = distance - MathInt.Floor((maxHeartTanks / 2f) * 0.6f);
		}
		int startPos = (int)Global.halfScreenW - (maxHeartTanks / 2) * distance;

		for (int i = 0; i < maxHeartTanks; i++) {
			bool isBought = mainPlayer.heartTanks > i;
			Global.sprites["menu_hearttank"].drawToHUD(
				isBought ? 0 : 2,
				startPos + (i * distance),
				37
			);
		}

		if (Global.flFrameCount % 60 < 30 && mainPlayer.realCharNum == 2) {
			Fonts.drawText(FontType.DarkPurple, ">", Global.screenW - 14, Global.halfScreenH, Alignment.Center);
			//Fonts.drawText(FontType.DarkPurple, "Armor", Global.screenW - 25, Global.halfScreenH + 15, Alignment.Center);
		} else if (Global.flFrameCount % 60 < 30 && mainPlayer.canUpgradeXArmor()) {
			Fonts.drawText(FontType.DarkPurple, "<", 14, Global.halfScreenH, Alignment.Center);
			//Fonts.drawText(FontType.DarkPurple, "X3", 12, Global.halfScreenH + 15, Alignment.Center);

			Fonts.drawText(FontType.DarkPurple, ">", Global.screenW - 14, Global.halfScreenH, Alignment.Center);
			//Fonts.drawText(FontType.DarkPurple, "X1", Global.screenW - 19, Global.halfScreenH + 15, Alignment.Center);
		}

		bool soldOut = false;
		int textX = 48;

		if (mainPlayer.heartTanks >= getMaxHeartTanks()) soldOut = true;
		string heartTanksStr = soldOut ? "SOLD OUT" : "Buy Heart Tank";
		Global.sprites["menu_hearttank"].drawToHUD(
			heartTanksStr == "SOLD OUT" ? 1 : 0, optionPositions[0].x, optionPositions[0].y - 4
		);
		Fonts.drawText(FontType.Blue, heartTanksStr, textX, optionPositions[0].y, selected: selectArrowPosY == 0);
		if (!soldOut) {
			string costStr = $" ({getHeartTankCost()} {Global.nameCoins})";
			int posOffset = Fonts.measureText(FontType.Blue, heartTanksStr);
			Fonts.drawText(FontType.Green, costStr, textX + posOffset, optionPositions[0].y);
		}

		for (int i = 0; i < getMaxSubTanks(); i++) {
			if (i > mainPlayer.subtanks.Count) continue;
			bool canUseSubtank = true;
			bool buyOrUse = mainPlayer.subtanks.Count < i + 1;
			string buyOrUseStr = buyOrUse ? "Buy Sub Tank" : "Use Sub Tank";
			var optionPos = optionPositions[i + 1];
			if (!buyOrUse) {
				var subtank = mainPlayer.subtanks[i];
				canUseSubtank = mainPlayer.canUseSubtank(subtank);
				if (mainPlayer.currentMaverick != null &&
					mainPlayer.currentMaverick.controlMode == MaverickModeId.TagTeam
				) {
					canUseSubtank = mainPlayer.currentMaverickWeapon.canUseSubtank(subtank);
				}

				Global.sprites["menu_subtank"].drawToHUD(1, optionPos.x - 2, optionPos.y - 4);
				Global.sprites["menu_subtank_bar"].drawToHUD(0, optionPos.x + 4, optionPos.y - 3);
				float yPos = 14 * (subtank.health / SubTank.maxHealth);
				DrawWrappers.DrawRect(
					optionPos.x + 4, optionPos.y - 3, optionPos.x + 9,
					optionPos.y + 11 - yPos, true, Color.Black, 1, ZIndex.HUD, isWorldPos: false
				);

				if (!canUseSubtankInMenu(canUseSubtank)) {
					if (canUseSubtank) {
						GameMode.drawWeaponSlotCooldown(
							optionPos.x + 6, optionPos.y + 4, subtankDelay / maxSubtankDelay
						);
						if (subtankTargets.Count == 0) {
							buyOrUseStr = "Cannot Use Sub Tank In Battle";
						}
					} else {
						Global.sprites["menu_subtank"].drawToHUD(2, optionPos.x - 2, optionPos.y - 4, 0.5f);
					}
				}

				if (selectArrowPosY == i + 1 && subtankTargets.Count > 0) {
					if (!subtankTargets.InRange(subtankTargetIndex)) subtankTargetIndex = 0;

					var currentTarget = subtankTargets[subtankTargetIndex];
					if (currentTarget is MaverickWeapon mw) {
						canUseSubtank = mw.canUseSubtank(subtank);
					}
					float targetXPos = 113;
					if (subtankTargets.Count > 1) {
						Global.sprites["hud_weapon_icon"].drawToHUD(currentTarget.weaponSlotIndex, optionPos.x + targetXPos, optionPos.y + 4);
						if (Global.flFrameCount % 60 < 30) {
							Fonts.drawText(FontType.DarkPurple, "<", optionPos.x + targetXPos - 12, optionPos.y - 2, Alignment.Center);
							Fonts.drawText(FontType.DarkPurple, ">", optionPos.x + targetXPos + 12, optionPos.y - 2, Alignment.Center);
						}
					}
				}
			} else {
				Global.sprites["menu_subtank"].drawToHUD(0, optionPos.x - 2, optionPos.y - 4);
			}
			if (!buyOrUse) {
				if (!canUseSubtank && subtankTargets.Count == 0) buyOrUseStr = "Cannot use Sub Tank Now";
				Fonts.drawText(
					FontType.Blue, buyOrUseStr, textX,
					optionPos.y, selected: selectArrowPosY == i + 1
				);
			} else {
				Fonts.drawText(
					FontType.Blue, buyOrUseStr, textX, optionPos.y,
					selected: selectArrowPosY == i + 1
				);
			}
			if (buyOrUse) {
				string costStr = $" ({getSubTankCost()} {Global.nameCoins})";
				int posOffset = Fonts.measureText(FontType.Blue, buyOrUseStr);
				Fonts.drawText(FontType.Green, costStr, textX + posOffset, optionPos.y);
			}
		}

		if (subtankTargets.Count > 1 && selectArrowPosY > 0) {
			Fonts.drawText(
				FontType.Grey, "Left/Right: Change Heal Target",
				Global.halfScreenW, Global.screenH - 32, Alignment.Center
			);
		}

		//UpgradeArmorMenu.drawHyperArmorUpgrades(mainPlayer, 20);

		Fonts.drawTextEX(
			FontType.Grey, "[MUP]/[MDOWN]: Select Item",
			Global.halfScreenW, Global.screenH - 28, Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Grey, "[OK]: Buy/Use, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 18, Alignment.Center
		);
	}
}
