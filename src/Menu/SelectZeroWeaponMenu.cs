using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class SelectZeroWeaponMenu : IMainMenu {
	public WeaponCursor[] cursors;
	public int selCursorIndex;
	public bool inGame;
	public string error = "";

	public static List<Weapon> groundSpecialWeapons = new List<Weapon>() {
		RaijingekiWeapon.staticWeapon,
		SuiretsusenWeapon.staticWeapon,
		TBreakerWeapon.staticWeapon,
	};

	public static List<Weapon> airSpecialWeapons = new List<Weapon>() {
		KuuenzanWeapon.staticWeapon,
		FSplasherWeapon.staticWeapon,
		HyorogaWeapon.staticWeapon,
	};

	public static List<Weapon> uppercutWeapons = new List<Weapon>() {
		RyuenjinWeapon.staticWeapon,
		DenjinWeapon.staticWeapon,
		RisingFangWeapon.staticWeapon
	};

	public static List<Weapon> downThrustWeapons = new List<Weapon>() {
		HyouretsuzanWeapon.staticWeapon,
		RakukojinWeapon.staticWeapon,
		DanchienWeapon.staticWeapon
	};

	public static List<Weapon> gigaAttackWeapons = new List<Weapon>() {
		new RakuhouhaWeapon(),
		new CFlasher(),
		new RekkohaWeapon()
	};

	public static List<Tuple<string, List<Weapon>>> zeroWeaponCategories = new List<Tuple<string, List<Weapon>>>() {
		Tuple.Create("Ground Spc", groundSpecialWeapons),
		Tuple.Create("Air Spc", airSpecialWeapons),
		Tuple.Create("Uppercut(Spc)", uppercutWeapons),
		Tuple.Create("Uppercut(Atk)", uppercutWeapons),
		Tuple.Create("Down thrust(Spc)", downThrustWeapons),
		Tuple.Create("Down thrust(Atk)", downThrustWeapons),
		Tuple.Create("Giga attack", gigaAttackWeapons),
	};

	public IMainMenu prevMenu;

	public SelectZeroWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;

		cursors = new WeaponCursor[] {
			new WeaponCursor(zeroWeaponCategories[0].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.groundSpecial)),
			new WeaponCursor(zeroWeaponCategories[1].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.airSpecial)),
			new WeaponCursor(zeroWeaponCategories[2].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.uppercutS)),
			new WeaponCursor(zeroWeaponCategories[3].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.uppercutA)),
			new WeaponCursor(zeroWeaponCategories[4].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.downThrustS)),
			new WeaponCursor(zeroWeaponCategories[5].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.downThrustA)),
			new WeaponCursor(zeroWeaponCategories[6].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.gigaAttack)),
			new WeaponCursor(Options.main.zeroLoadout.hyperMode)
		};
	}

	public void update() {
		if (!string.IsNullOrEmpty(error)) {
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				error = null;
			}
			return;
		}

		int maxCatCount = 3;
		if (selCursorIndex < 7) {
			maxCatCount = zeroWeaponCategories[selCursorIndex].Item2.Count;
		}

		Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, maxCatCount - 1, wrap: true, playSound: true);
		Helpers.menuUpDown(ref selCursorIndex, 0, cursors.Length - 1);

		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		bool selectPressed = Global.input.isPressedMenu(Control.MenuConfirm) || (backPressed && !inGame);
		if (selectPressed) {
			if (duplicateTechniques()) {
				error = "Cannot select same technique in two slots!";
				return;
			}

			int[] oldArray = {
				Options.main.zeroLoadout.uppercutS,
				Options.main.zeroLoadout.uppercutA,
				Options.main.zeroLoadout.downThrustS,
				Options.main.zeroLoadout.downThrustA,
				Options.main.zeroLoadout.gigaAttack,
				Options.main.zeroLoadout.hyperMode,
				Options.main.zeroLoadout.groundSpecial,
				Options.main.zeroLoadout.airSpecial
			};

			Options.main.zeroLoadout.groundSpecial = zeroWeaponCategories[0].Item2[cursors[0].index].type;
			Options.main.zeroLoadout.airSpecial = zeroWeaponCategories[1].Item2[cursors[1].index].type;
			Options.main.zeroLoadout.uppercutS = zeroWeaponCategories[2].Item2[cursors[2].index].type;
			Options.main.zeroLoadout.uppercutA = zeroWeaponCategories[3].Item2[cursors[3].index].type;
			Options.main.zeroLoadout.downThrustS = zeroWeaponCategories[4].Item2[cursors[4].index].type;
			Options.main.zeroLoadout.downThrustA = zeroWeaponCategories[5].Item2[cursors[5].index].type;
			Options.main.zeroLoadout.gigaAttack = zeroWeaponCategories[6].Item2[cursors[6].index].type;
			Options.main.zeroLoadout.hyperMode = cursors[7].index;
			int[] newArray = {
				Options.main.zeroLoadout.uppercutS,
				Options.main.zeroLoadout.uppercutA,
				Options.main.zeroLoadout.downThrustS,
				Options.main.zeroLoadout.downThrustA,
				Options.main.zeroLoadout.gigaAttack,
				Options.main.zeroLoadout.hyperMode,
				Options.main.zeroLoadout.groundSpecial,
				Options.main.zeroLoadout.airSpecial
			};

			if (!Enumerable.SequenceEqual(oldArray, newArray)) {
				Options.main.saveToFile();
				if (inGame) {
					if (Options.main.killOnLoadoutChange) {
						Global.level.mainPlayer.forceKill();
					} else if (!Global.level.mainPlayer.isDead) {
						Global.level.gameMode.setHUDErrorMessage(Global.level.mainPlayer, "Change will apply on next death", playSound: false);
					}
				}
			}

			if (inGame) Menu.exit();
			else Menu.change(prevMenu);
		} else if (backPressed) {
			Menu.change(prevMenu);
		}
	}

	public bool duplicateTechniques() {
		return zeroWeaponCategories[3].Item2[cursors[2].index].type == zeroWeaponCategories[3].Item2[cursors[3].index].type ||
			zeroWeaponCategories[4].Item2[cursors[4].index].type == zeroWeaponCategories[5].Item2[cursors[5].index].type;
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["loadoutbackground"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenuload"], 0, 0);
		}

		Fonts.drawText(FontType.Yellow, "Zero Loadout", Global.screenW * 0.5f, 20, Alignment.Center);
		var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
		float botOffY = inGame ? 0 : -2;

		int startY = 40;
		int startX = 30;
		int wepH = 15;

		float wepPosX = 195;
		float wepTextX = 207;

		Global.sprites["cursor"].drawToHUD(0, startX, startY + (selCursorIndex * wepH) - 2);
		Color color;
		float alpha;
		for (int i = 0; i < cursors.Length - 1; i++) {
			color = Color.White;
			alpha = 1f;
			float yPos = startY - 6 + (i * wepH);
			Fonts.drawText(
				FontType.Blue, zeroWeaponCategories[i].Item1 + ":", 40,
				yPos, selected: selCursorIndex == i
			);
			var weapon = zeroWeaponCategories[i].Item2[cursors[i].index];
			Fonts.drawText(
				FontType.Purple, weapon.displayName, wepTextX, yPos,
				selected: selCursorIndex == i
			);
			Global.sprites["hud_killfeed_weapon"].drawToHUD(weapon.killFeedIndex, wepPosX, yPos + 3, alpha);
		}

		color = Color.White;
		alpha = 1f;

		float hyperModeYPos = startY - 6 + (wepH * 7);
		Fonts.drawText(
			FontType.Blue, "Hyper Mode:", 40, hyperModeYPos,
			selected: selCursorIndex == 7
		);
		if (cursors[7].index == 0) {
			Fonts.drawText(
				FontType.Grey, "Black Zero", wepTextX, hyperModeYPos,
				selected: selCursorIndex == 7
			);
			Global.sprites["hud_killfeed_weapon"].drawToHUD(122, wepPosX, hyperModeYPos + 3, alpha);
		} else if (cursors[7].index == 1) {
			Fonts.drawText(
				FontType.Red, "Awakened Zero", wepTextX, hyperModeYPos,
				selected: selCursorIndex == 7
			);
			Global.sprites["hud_killfeed_weapon"].drawToHUD(87, wepPosX, hyperModeYPos + 3, alpha);
		} else if (cursors[7].index == 2) {
			Fonts.drawText(
				FontType.DarkPurple, "Viral Zero", wepTextX, hyperModeYPos,
				selected: selCursorIndex == 7
			);
			Global.sprites["hud_killfeed_weapon"].drawToHUD(173, wepPosX, hyperModeYPos + 3, alpha);
		}

		int wsy = 167;
		DrawWrappers.DrawRect(
			25, wsy + 2, Global.screenW - 30, wsy + 28, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: outlineColor
		);

		if (selCursorIndex < 7) {
			var wep = zeroWeaponCategories[selCursorIndex].Item2[cursors[selCursorIndex].index];
			int posY = 6;
			foreach (string description in wep.description) {
				Fonts.drawText(
					FontType.Green, wep.description[0], 30, wsy + 6, Alignment.Left
				);
				posY += 9;
			}
		} else {
			if (cursors[7].index == 0) {
				Fonts.drawText(FontType.Green, "This hyper form increases speed and damage.", 40, wsy + 6);
				Fonts.drawText(FontType.Green, "Lasts 12 seconds.", 40, wsy + 15);
			} else if (cursors[7].index == 1) {
				Fonts.drawText(FontType.Green, "This hyper form grants powerful ranged attacks.", 40, wsy + 6);
				Fonts.drawText(FontType.Green, $"Lasts until {Global.nameCoins} are depleted.", 40, wsy + 15);
			} else if (cursors[7].index == 2) {
				Fonts.drawText(FontType.Green, "This hyper form infects and disrupts foes on each hit.", 40, wsy + 6);
				Fonts.drawText(FontType.Green, "Lasts until death.", 40, wsy + 15);
			}
		}
		/*
		Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change Technique", Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 18);
		Helpers.drawTextStd(TCat.BotHelp, "Up/Down: Change Category", Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 18);
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
}
