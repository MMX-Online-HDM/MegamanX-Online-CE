using ProtoBuf;
using SFML.Graphics;

namespace MMXOnline;

[ProtoContract]
public class PZeroLoadout {
	[ProtoMember(1)]
	public int gigaAttack;
	[ProtoMember(2)]
	public int hyperMode;

	public static PZeroLoadout createRandom() {
		return new PZeroLoadout() {
			gigaAttack = Helpers.randomRange(0, 2),
			hyperMode = Helpers.randomRange(0, 2)
		};
	}
}

public class SelectPunchyZeroWeaponMenu : IMainMenu {
	// Menu controls.
	public IMainMenu prevMenu;
	public int cursorRow;
	bool inGame;

	// Loadout items.
	public int gigaAttack;
	public int hyperMode;

	public int[][] weaponIcons = {
		new int[] {51, 63, 64},
		new int[] {118, 118, 122}
	};
	public int[][] weaponIconsL2 = {
		new int[] {-1, -1, -1},
		new int[] {125, 86, -1}
	};

	public SelectPunchyZeroWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
	}

	public void update() {
		bool okPressed = Global.input.isPressedMenu(Control.MenuConfirm);
		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);

		Helpers.menuUpDown(ref cursorRow, 0, 1);

		if (cursorRow == 0) {
			Helpers.menuLeftRightInc(ref gigaAttack, 0, 2, playSound: true);
		}
		else if (cursorRow == 1) {
			Helpers.menuLeftRightInc(ref hyperMode, 0, 2, playSound: true);
		}

		if (okPressed || backPressed && !inGame) {
			Options.main.pzeroLoadout.gigaAttack = gigaAttack;
			Options.main.pzeroLoadout.hyperMode = hyperMode;

			if (inGame && Global.level != null && Options.main.killOnLoadoutChange) {
				Global.level.mainPlayer.forceKill();
			}
			Menu.change(prevMenu);
			return;
		}
		if (backPressed) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["loadoutbackground"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenuload"], 0, 0);
		}
		Fonts.drawText(FontType.Yellow, "Knuckle Zero Loadout", Global.screenW * 0.5f, 24, Alignment.Center);

		int startY = 55;
		int startX = 30;
		int startX2 = 128;
		int wepW = 18;
		int wepH = 20;
		Global.sprites["cursor"].drawToHUD(0, startX, startY + cursorRow * wepH);

		for (int i = 0; i < 2; i++) {
			float yPos = startY - 6 + (i * wepH);
			int selectVar = 0;
			if (i == 0) {
				Fonts.drawText(FontType.Blue, "Giga Attack", 40, yPos + 2, selected: cursorRow == i);
				selectVar = gigaAttack;
			} else {
				Fonts.drawText(FontType.Blue, "Hyper Mode", 40, yPos + 2, selected: cursorRow == i);
				selectVar = hyperMode;
			}
			for (int j = 0; j < weaponIcons[i].Length; j++) {
				Global.sprites["hud_weapon_icon"].drawToHUD(
					weaponIcons[i][j], startX2 + (j * wepW), startY + (i * wepH)
				);
				if (weaponIconsL2[i][j] != -1) {
					Global.sprites["hud_killfeed_weapon"].drawToHUD(
						weaponIconsL2[i][j], startX2 + (j * wepW), startY + (i * wepH)
					);
					DrawWrappers.DrawRectWH(
						startX2 + (j * wepW) - 6, startY + (i * wepH) - 6,
						12, 12, false, Color.White, 1, ZIndex.HUD, false
					);
					DrawWrappers.DrawRectWH(
						startX2 + (j * wepW) - 5, startY + (i * wepH) - 5,
						10, 10, false, new Color(0, 0, 0, 192), 1, ZIndex.HUD, false
					);
				}
				if (selectVar != j) {
					DrawWrappers.DrawRectWH(
						startX2 + (j * wepW) - 7, startY + (i * wepH) - 7,
						14, 14, true, Helpers.FadedIconColor, 1, ZIndex.HUD, false
					);
				}
			}
		}

		string menuTitle = "";
		string weaponTitle = "";
		string weaponDescription = "";
		string weaponSubDescription = "";
		if (cursorRow == 0) {
			menuTitle = "Giga Attack";
			weaponTitle = gigaAttack switch {
				0 => "Rakuhouha",
				1 => "Messenkou",
				2 => "Rekkouha",
				_ => "ERROR"
			};
			weaponDescription = gigaAttack switch {
				0 => "Cannels stored energy.\nCan flinch enemies.",
				1 => "Energy blast with pierce properties.\nIgnores enemy defense.",
				2 => "Summon eleven beams of light.\nFull-screen range.",
				_ => "ERROR"
			};
			weaponSubDescription = gigaAttack switch {
				0 => "Ammo use: 16",
				1 => "Ammo use: 8",
				2 => "Ammo use: 32",
				_ => "ERROR"
			};
		} else {
			menuTitle = "Hyper Mode";
			weaponTitle = hyperMode switch {
				0 => "Black Zero",
				1 => "Awakened Zero",
				2 => "Viral Zero",
				_ => "ERROR"
			};
			weaponDescription = hyperMode switch {
				0 => "Increases speed by 15%, increases damage by 50%" +
					"\nand increases the flinch of all attacks.",
				1 => "Gives a damaging contact aura and upgrades attacks." +
					"\nAfter 30 seconds enters Genmu state",
				2 => "Applies virus to enemies on hit\nand grants the Dark Hold giga attack.",
				_ => "ERROR"
			};
			weaponSubDescription = hyperMode switch {
				0 => "Duration: 15 seconds",
				1 => "Duration: Scrap-based",
				2 => "Duration: Unlimited",
				_ => "ERROR"
			};
		}
		
		int wsy = 124;
		DrawWrappers.DrawRect(
			25, wsy - 4, Global.screenW - 25, wsy + 68, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: Helpers.LoadoutBorderColor
		);
		DrawWrappers.DrawRect(
			25, wsy - 4, Global.screenW - 25, wsy + 11, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor:  Helpers.LoadoutBorderColor
		);
		float titleY1 = 124;
		float titleY2 = 140;
		float row1Y = 153;
		float row2Y = 181;
		Fonts.drawText(
			FontType.Purple, menuTitle,
			Global.halfScreenW, titleY1, Alignment.Center
		);
		Fonts.drawText(
			FontType.Orange, weaponTitle,
			Global.halfScreenW, titleY2, Alignment.Center
		);
		Fonts.drawText(
			FontType.Green, weaponDescription,
			Global.halfScreenW, row1Y, Alignment.Center
		);
		Fonts.drawText(
			FontType.Blue, weaponSubDescription,
			Global.halfScreenW, row2Y, Alignment.Center
		);
	}
}
