using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

[ProtoContract]
public class CustomMatchSettings {
	[ProtoMember(1)] public bool hyperModeMatch;
	[ProtoMember(2)] public int startCurrency;
	[ProtoMember(3)] public int startHeartTanks;
	[ProtoMember(4)] public int startSubTanks;
	[ProtoMember(5)] public float healthModifier;
	[ProtoMember(6)] public int baseHp;
	[ProtoMember(7)] public int damageModifier;
	[ProtoMember(8)] public int sameCharNum;
	[ProtoMember(9)] public int redSameCharNum;
	[ProtoMember(10)] public int maxHeartTanks;
	[ProtoMember(11)] public int maxSubTanks;
	[ProtoMember(12)] public int heartTankHp;
	[ProtoMember(13)] public int heartTankCost;
	[ProtoMember(14)] public int currencyGain;
	[ProtoMember(15)] public int respawnTime;
	[ProtoMember(16)] public bool pickupItems;
	[ProtoMember(17)] public int subtankGain;
	[ProtoMember(18)] public int assistTime;
	[ProtoMember(19)] public bool assistBanlist;
	[ProtoMember(20)] public int largeHealthPickup;
	[ProtoMember(21)] public int smallHealthPickup;
	[ProtoMember(22)] public int largeAmmoPickup;
	[ProtoMember(23)] public int smallAmmoPickup;
	[ProtoMember(24)] public int subTankCost;
	[ProtoMember(25)] public bool frostShieldNerf;
	[ProtoMember(26)] public bool frostShieldChargedNerf;
	[ProtoMember(27)] public bool axlBackwardsDebuff;
	[ProtoMember(29)] public bool axlCustomReload;
	[ProtoMember(30)] public bool oldATrans;
	[ProtoMember(31)] public bool flinchairDashReset;
	[ProtoMember(32)] public bool comboFlinch;
	[ProtoMember(33)] public bool quakeBlazerDownwards;


	public CustomMatchSettings() {
	}

	public static CustomMatchSettings getDefaults() {
		return new CustomMatchSettings {
			hyperModeMatch = false,
			startCurrency = 3,
			startHeartTanks = 0,
			startSubTanks = 0,
			healthModifier = 1,
			baseHp = 16,
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
			subtankGain = 4,
			assistTime = 2,
			assistBanlist = true,
			largeHealthPickup = 8,
			smallHealthPickup = 4,
			largeAmmoPickup = 50,
			smallAmmoPickup = 25,
			subTankCost = 4,
			oldATrans = false,
			flinchairDashReset = false,
			comboFlinch = true,
			/*
			frostShieldNerf = true,
			frostShieldChargedNerf = false,
			axlBackwardsDebuff = true,
			axlCustomReload = false,
			quakeBlazerDownwards = false,
			*/
		};
	}
}

public class CustomMatchSettingsMenu : IMainMenu {
	public int selectArrowPosY;
	public int selectArrowPosY2;
	public int selectArrowPosY3;
	public const int startX = 30;
	public int startY = 40;
	public const int lineH = 10;
	public const int startX2 = 30;
	public int startY2 = 40;
	public const int lineH2 = 10;
	public const int startX3 = 30;
	public int startY3 = 40;
	public const int lineH3 = 10;
	public const uint fontSize = 24;
	public IMainMenu prevMenu;
	public bool inGame;
	public int Page = 1;
	public bool isOffline;
	public List<MenuOption> menuOptions = new List<MenuOption>();
	public List<MenuOption> menuOptions2 = new List<MenuOption>();
	public List<MenuOption> menuOptions3 = new List<MenuOption>();


	SavedMatchSettings savedMatchSettings {
		get { return isOffline ? SavedMatchSettings.mainOffline : SavedMatchSettings.mainOnline; }
	}
	CustomMatchSettings cSettings => savedMatchSettings.customMatchSettings;

	public CustomMatchSettingsMenu(IMainMenu prevMenu, bool inGame, bool isOffline) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		int currentY = startY;
		int currentY2 = startY2;
		int currentY3 = startY3;
		this.isOffline = isOffline;

		#region  Page 1
		menuOptions.Add(
			new MenuOption(
				startX, currentY,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.hyperModeMatch);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.RedishOrange,
						"Hypermode Match : " +
						Helpers.boolYesNo(cSettings.hyperModeMatch),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.startCurrency, 0, 9999, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Green,
						"Start " + Global.nameCoins + ": " +
						cSettings.startCurrency.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		//Currency Gain Custom Setting
		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.currencyGain, 1, 10, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Green,
						Global.nameCoins + " gain modifier: " +
						cSettings.currencyGain.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					int dir = Global.input.getMenuXDir();
					if (dir == 0) { return; }
					cSettings.healthModifier += 0.25f * dir;
					cSettings.healthModifier = (
						Helpers.clamp(cSettings.healthModifier, 0.25f, 4)
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Red,
						"Health Modifier: " +
						$"{cSettings.healthModifier * 100}%",
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref cSettings.baseHp, 4, 16, false
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Red,
						"Base Health: " +
						(cSettings.baseHp).ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref cSettings.heartTankCost, 1, 4, false
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Heart Tanks Cost: " +
						cSettings.heartTankCost.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref cSettings.maxHeartTanks, 0, 8, false
					);
					if (cSettings.startHeartTanks > cSettings.maxHeartTanks) {
						cSettings.startHeartTanks = cSettings.maxHeartTanks;
					}
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Max Heart Tanks: " +
						cSettings.maxHeartTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref cSettings.startHeartTanks, 0,
						cSettings.maxHeartTanks, true
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Start Heart Tanks: " +
						cSettings.startHeartTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref cSettings.heartTankHp, 1, 2, true
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Heart Tank HP: " +
						cSettings.heartTankHp.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.subTankCost, 0, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Sub Tanks Cost: " +
						cSettings.subTankCost.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.maxSubTanks, 0, 4, false);
					if (cSettings.startSubTanks > cSettings.maxSubTanks) {
						cSettings.startSubTanks = cSettings.maxSubTanks;
					}
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Max Sub Tanks: " +
						cSettings.maxSubTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref cSettings.startSubTanks, 0, cSettings.maxSubTanks, true
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Start Sub Tanks: " +
						cSettings.startSubTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.subtankGain, 1, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Sub Tank Gain: " +
						cSettings.subtankGain.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		/*
			menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.damageModifier, 1, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Damage modifier: " +
						(cSettings.damageModifier * 100).ToString() + "%",
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.sameCharNum, -1, 4);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Mono character: " +
						getSameCharString(cSettings.sameCharNum),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.redSameCharNum, -1, 4);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Red mono character: " +
						getSameCharString(cSettings.redSameCharNum),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		*/
#endregion
			#region  Page 2
			menuOptions2.Add(
			new MenuOption(
				startX2, currentY2,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.assistBanlist);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Unassistable List (FFA): " +
						Helpers.boolYesNo(cSettings.assistBanlist),
						pos.x, pos.y, selected: selectArrowPosY2 == index
					);
				}
			)
		);
			//Respawn Time Custom Setting
			menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.respawnTime, 1, 12, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Respawn Time modifier: " +
						cSettings.respawnTime.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == index
					);
				}
			)
		);
			menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.assistTime, 0, 6, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Assist Time: " +
						cSettings.assistTime.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == index
					);
				}
			)
		);
			menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.pickupItems);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Pick Up Items: " +
						Helpers.boolYesNo(cSettings.pickupItems),
						pos.x, pos.y, selected: selectArrowPosY2 == index
					);
				}
			)
		);
			menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.largeHealthPickup, 4, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Large Health Recovery: " +
						cSettings.largeHealthPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == index
					);
				}
			)
		);
			menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.smallHealthPickup, 2, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Small Health Recovery: " +
						cSettings.smallHealthPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == index
					);
				}
			)
		);
			menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.largeAmmoPickup, 0, 100, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Large Ammo Recovery: " +
						cSettings.largeAmmoPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == index
					);
				}
			)
		);
		menuOptions2.Add(
		new MenuOption(
			startX2, currentY2 += lineH2,
			() => {
				Helpers.menuLeftRightInc(ref cSettings.smallAmmoPickup, 0, 100, true);
			},
			(Point pos, int index) => {
				Fonts.drawText(
					FontType.Blue,
					"Small Ammo Recovery: " +
					cSettings.smallAmmoPickup.ToString(),
					pos.x, pos.y, selected: selectArrowPosY2 == index
						);
					}
				)
			);
			#endregion
				menuOptions3.Add(
				new MenuOption(
					startX3, currentY3 += lineH3,
					() => {
						Helpers.menuLeftRightBool(ref cSettings.flinchairDashReset, true);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Purple,
							"Flinch resets Air Dash: " +
							Helpers.boolYesNo(cSettings.flinchairDashReset),
							pos.x, pos.y, selected: selectArrowPosY3 == index
						);
					}
				)
			);
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.comboFlinch, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Flinch stack: " +
						Helpers.boolYesNo(cSettings.comboFlinch),
						pos.x, pos.y, selected: selectArrowPosY3 == index
						);
					}
				)
			);
			menuOptions3.Add(
				new MenuOption(
					startX3, currentY3 += lineH3,
					() => {
						Helpers.menuLeftRightBool(ref cSettings.oldATrans, true);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Purple,
							"Axl Vanilla DNA: " +
							Helpers.boolYesNo(cSettings.oldATrans),
							pos.x, pos.y, selected: selectArrowPosY3 == index
						);
					}
				)
			);
			/*
			#region Page 3
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.frostShieldNerf, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Frost Shield Uncharged 'Shield' Nerf: " +
						Helpers.boolYesNo(cSettings.frostShieldNerf),
						pos.x, pos.y, selected: selectArrowPosY3 == index
					);
				}
			)
		);
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.frostShieldChargedNerf, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Frost Shield Charged 'Shield' Nerf: " +
						Helpers.boolYesNo(cSettings.frostShieldChargedNerf),
						pos.x, pos.y, selected: selectArrowPosY3 == index
					);
				}
			)
		);
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.axlBackwardsDebuff, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Axl Shooting Backwards Debuff: " +
						Helpers.boolYesNo(cSettings.axlBackwardsDebuff),
						pos.x, pos.y, selected: selectArrowPosY3 == index
					);
				}
			)
		);
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.axlCustomReload, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Axl Weapons Capable to Reload: " +
						Helpers.boolYesNo(cSettings.axlCustomReload),
						pos.x, pos.y, selected: selectArrowPosY3 == index
					);
				}
			)
		);
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.oldATrans, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Axl Vanilla DNA: " +
						Helpers.boolYesNo(cSettings.oldATrans),
						pos.x, pos.y, selected: selectArrowPosY3 == index
					);
				}
			)
		);
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.flinchairDashReset, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Flinch resets Air Dash: " +
						Helpers.boolYesNo(cSettings.flinchairDashReset),
						pos.x, pos.y, selected: selectArrowPosY3 == index
					);
				}
			)
		);
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.comboFlinch, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Flinch stack: " +
						Helpers.boolYesNo(cSettings.comboFlinch),
						pos.x, pos.y, selected: selectArrowPosY3 == index
					);
				}
			)
		);
			menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.quakeBlazerDownwards, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Quake Blazer knocks downwards: " +
						Helpers.boolYesNo(cSettings.quakeBlazerDownwards),
						pos.x, pos.y, selected: selectArrowPosY3 == index
					);
				}
			)
		);
			#endregion
			*/
		}

	public string getSameCharString(int charNum) {
		if (charNum == -1) return "No";
		return Character.charDisplayNames[charNum];
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.Special1)) {
			Page++;
			if (Page > 3) Page = 1;
		}
		if (Page == 1) {
			menuOptions[selectArrowPosY].update();
			Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);
		} else if (Page == 2) {
			menuOptions2[selectArrowPosY2].update();
			Helpers.menuUpDown(ref selectArrowPosY2, 0, menuOptions2.Count - 1);
		}
		else if (Page == 3) {
			menuOptions3[selectArrowPosY3].update();
			Helpers.menuUpDown(ref selectArrowPosY3, 0, menuOptions3.Count - 1);
		}

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (cSettings.maxHeartTanks < cSettings.startHeartTanks) {
				Menu.change(new ErrorMenu(new string[] { "Error: Max heart tanks can't be", "less than start heart tanks." }, this));
				return;
			}

			if (cSettings.maxSubTanks < cSettings.startSubTanks) {
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
		if (Page == 3)
		foreach (var menuOption3 in menuOptions3) {
			menuOption3.render(menuOption3.pos, i);
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
		else if (Page == 3) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions3[selectArrowPosY3].pos.x - 8,
					menuOptions3[selectArrowPosY3].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions3[selectArrowPosY3].pos.x - 8, menuOptions3[selectArrowPosY3].pos.y + 5
				);
			}
		}
	}
}
