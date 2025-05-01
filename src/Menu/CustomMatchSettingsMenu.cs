using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

[ProtoContract]
public class CustomMatchSettings {
	[ProtoMember(1)] public bool hyperModeMatch;
	[ProtoMember(2)] public int startCurrency;
	[ProtoMember(3)] public int startHeartTanks;
	[ProtoMember(4)] public int startSubTanks;
	[ProtoMember(5)] public int healthModifier;
	[ProtoMember(5)] public int damageModifier;
	[ProtoMember(6)] public int sameCharNum;
	[ProtoMember(7)] public int redSameCharNum;
	[ProtoMember(8)] public int maxHeartTanks;
	[ProtoMember(9)] public int maxSubTanks;
	[ProtoMember(10)] public int heartTankHp;
	[ProtoMember(11)] public int heartTankCost;
	[ProtoMember(12)] public int currencyGain;
	[ProtoMember(13)] public int respawnTime;
	[ProtoMember(14)] public bool pickupItems;
	[ProtoMember(15)] public int SubtankGain;
	[ProtoMember(16)] public int AssistTime;
	[ProtoMember(16)] public bool Assistable;
	[ProtoMember(17)] public int LargeHealthPickup;
	[ProtoMember(18)] public int SmallHealthPickup;
	[ProtoMember(19)] public int LargeAmmoPickup;
	[ProtoMember(20)] public int SmallAmmoPickup;


	public CustomMatchSettings() {
	}

	public static CustomMatchSettings getDefaults() {
		return new CustomMatchSettings {
			hyperModeMatch = false,
			startCurrency = 3,
			startHeartTanks = 0,
			startSubTanks = 0,
			healthModifier = 16,
			damageModifier = 1,
			sameCharNum = -1,
			redSameCharNum = -1,
			maxHeartTanks = 8,
			maxSubTanks = 2,
			heartTankHp = 1,
			heartTankCost = 2,
			currencyGain = 1,
			respawnTime = 5,
			pickupItems = true,
			SubtankGain = 3,
			AssistTime = 2,
			Assistable = true,
			LargeHealthPickup = 8,
			SmallHealthPickup = 4,
			LargeAmmoPickup = 50,
			SmallAmmoPickup = 25,
		};
	}
}

public class CustomMatchSettingsMenu : IMainMenu {
	public int selectArrowPosY;
	public int selectArrowPosY2;
	public const int startX = 30;
	public int startY = 40;
	public const int lineH = 10;
	public const int startX2 = 30;
	public int startY2 = 40;
	public const int lineH2 = 10;
	public const uint fontSize = 24;
	public IMainMenu prevMenu;
	public bool inGame;
	public int Page = 1;
	public bool isOffline;
	public List<MenuOption> menuOptions = new List<MenuOption>();
	public List<MenuOption> menuOptions2 = new List<MenuOption>();

	SavedMatchSettings savedMatchSettings { get { return isOffline ? SavedMatchSettings.mainOffline : SavedMatchSettings.mainOnline; } }

	public CustomMatchSettingsMenu(IMainMenu prevMenu, bool inGame, bool isOffline) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		int currentY = startY;
		int currentY2 = startY2;
		this.isOffline = isOffline;
		#region  Page 1
		menuOptions.Add(
			new MenuOption(
				startX, currentY,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startCurrency, 0, 9999, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Start " + Global.nameCoins + ": " +
						savedMatchSettings.customMatchSettings.startCurrency.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 0
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.healthModifier, 8, 32);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Base Health: " +
						(savedMatchSettings.customMatchSettings.healthModifier).ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 1
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startHeartTanks, 0, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Start heart tanks: " +
						savedMatchSettings.customMatchSettings.startHeartTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 2
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxHeartTanks, 0, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Max heart tanks: " +
						savedMatchSettings.customMatchSettings.maxHeartTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 3
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.heartTankHp, 1, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Heart tank HP: " +
						savedMatchSettings.customMatchSettings.heartTankHp.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 4
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.heartTankCost, 0, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Heart tanks cost: " +
						savedMatchSettings.customMatchSettings.heartTankCost.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 5
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startSubTanks, 0, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Start subtanks: " +
						savedMatchSettings.customMatchSettings.startSubTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 6
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxSubTanks, 0, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Max subtanks: " +
						savedMatchSettings.customMatchSettings.maxSubTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 7
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.damageModifier, 1, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Damage modifier: " +
						(savedMatchSettings.customMatchSettings.damageModifier * 100).ToString() + "%",
						pos.x, pos.y, selected: selectArrowPosY == 8
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.sameCharNum, -1, 4);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Mono character: " +
						getSameCharString(savedMatchSettings.customMatchSettings.sameCharNum),
						pos.x, pos.y, selected: selectArrowPosY == 9
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.redSameCharNum, -1, 4);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Red mono character: " +
						getSameCharString(savedMatchSettings.customMatchSettings.redSameCharNum),
						pos.x, pos.y, selected: selectArrowPosY == 10
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.hyperModeMatch);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"1v1 or Hypermode Match : " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.hyperModeMatch),
						pos.x, pos.y, selected: selectArrowPosY == 11
					);
				}
			)
		);

		#endregion
		#region  Page 2
		//Currency Gain Custom Setting
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.currencyGain, 1, 10, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Currency Gain modifier: " +
						savedMatchSettings.customMatchSettings.currencyGain.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 0
					);
				}
			)
		);
		//Respawn Time Custom Setting
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.respawnTime, 1, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Respawn Time modifier: " +
						savedMatchSettings.customMatchSettings.respawnTime.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 1
					);
				}
			)
		);
		//
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.pickupItems);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Pick Up Items: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.pickupItems),
						pos.x, pos.y, selected: selectArrowPosY2 == 2
					);
				}
			)
		);
		menuOptions2.Add(
				new MenuOption(
					startX2, currentY2 += lineH2,
					() => {
						Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.SubtankGain, 1, 4, true);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue,
							"SubTank Gain: " +
							savedMatchSettings.customMatchSettings.SubtankGain.ToString(),
							pos.x, pos.y, selected: selectArrowPosY2 == 3
						);
					}
				)
			);
		menuOptions2.Add(
				new MenuOption(
					startX2, currentY2 += lineH2,
					() => {
						Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.AssistTime, 0, 5, true);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue,
							"Assist Time: " +
							savedMatchSettings.customMatchSettings.AssistTime.ToString(),
							pos.x, pos.y, selected: selectArrowPosY2 == 4
						);
					}
				)
			);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.Assistable);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Unassistable List: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.Assistable),
						pos.x, pos.y, selected: selectArrowPosY2 == 5
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.LargeHealthPickup, 0, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Large Health Recovery: " +
						savedMatchSettings.customMatchSettings.LargeHealthPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 6
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.SmallHealthPickup, 0, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Small Health Recovery: " +
						savedMatchSettings.customMatchSettings.SmallHealthPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 7
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.LargeAmmoPickup, 0, 100, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Large Ammo Recovery: " +
						savedMatchSettings.customMatchSettings.LargeAmmoPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 8
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.SmallAmmoPickup, 0, 100, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Small Ammo Recovery: " +
						savedMatchSettings.customMatchSettings.SmallAmmoPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 9
					);
				}
			)
		);
		#endregion
	}

	public string getSameCharString(int charNum) {
		if (charNum == -1) return "No";
		return Character.charDisplayNames[charNum];
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.Special1)) {
			Page++;
			if (Page > 2) Page = 1;
		}
		if (Page == 1) {
			menuOptions[selectArrowPosY].update();
			Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);
		} else if (Page == 2) {
			menuOptions2[selectArrowPosY2].update();
			Helpers.menuUpDown(ref selectArrowPosY2, 0, menuOptions2.Count - 1);
		}

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (savedMatchSettings.customMatchSettings.maxHeartTanks < savedMatchSettings.customMatchSettings.startHeartTanks) {
				Menu.change(new ErrorMenu(new string[] { "Error: Max heart tanks can't be", "less than start heart tanks." }, this));
				return;
			}

			if (savedMatchSettings.customMatchSettings.maxSubTanks < savedMatchSettings.customMatchSettings.startSubTanks) {
				Menu.change(new ErrorMenu(new string[] { "Error: Max sub tanks can't be", "less than start sub tanks." }, this));
				return;
			}

			Menu.change(prevMenu);
		}
	}

	public void render() {
		Cursor();
		drawText();
		int i = 0;
		if (Page == 1)
		foreach (var menuOption in menuOptions) {
			menuOption.render(menuOption.pos, i);
			i++;
		}
		if (Page == 2)
		foreach (var menuOption2 in menuOptions2) {
			menuOption2.render(menuOption2.pos, i);
			i++;
		}
	}
	public void drawText() {
		Fonts.drawText(
			FontType.Yellow, "Custom Match Options",
			Global.halfScreenW, 20, alignment: Alignment.Center
		);
		Fonts.drawText(
			FontType.Yellow, "Page: " + Page,
			Global.halfScreenW+150, 20, alignment: Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change setting, [SPC]: Change Page, [BACK]: Back",
			Global.halfScreenW-6, Global.screenH - 26, Alignment.Center
		);
	}
	public void Cursor() {
		if (Page == 1) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions[selectArrowPosY].pos.x - 8,
					menuOptions[selectArrowPosY].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions[selectArrowPosY].pos.x - 8, menuOptions[selectArrowPosY].pos.y + 5
				);
			}
		} else if (Page == 2) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions2[selectArrowPosY2].pos.x - 8,
					menuOptions2[selectArrowPosY2].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions2[selectArrowPosY2].pos.x - 8, menuOptions2[selectArrowPosY2].pos.y + 5
				);
			}
		}
	}
}
