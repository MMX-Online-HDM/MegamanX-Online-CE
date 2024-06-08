using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class SelectZeroWeaponMenu : IMainMenu {
	public List<WeaponCursor> cursors;
	public int selCursorIndex;
	public bool inGame;
	public string error = "";

	public static List<Weapon> meleeWeapons = new List<Weapon>()
	{
			new ZSaber(null),
			new KKnuckleWeapon(null),
			new ZeroBuster(),
		};

	public static List<Weapon> groundSpecialWeapons = new List<Weapon>()
	{
			new RaijingekiWeapon(null),
			new SuiretsusenWeapon(null),
			new TBreakerWeapon(null)
		};

	public static List<Weapon> airSpecialWeapons = new List<Weapon>()
	{
			new KuuenzanWeapon(null),
			new FSplasherWeapon(null),
			new HyorogaWeapon(null)
		};

	public static List<Weapon> uppercutWeapons = new List<Weapon>()
	{
			new RyuenjinWeapon(null),
			new EBladeWeapon(null),
			new RisingWeapon(null)
		};

	public static List<Weapon> downThrustWeapons = new List<Weapon>()
	{
			new HyouretsuzanWeapon(null),
			new RakukojinWeapon(null),
			new QuakeBlazerWeapon(null)
		};

	public static List<Weapon> gigaAttackWeapons = new List<Weapon>() {
		new RakuhouhaWeapon(),
		new CFlasher(),
		new RekkohaWeapon()
	};

	public static List<Tuple<string, List<Weapon>>> zeroWeaponCategories = new List<Tuple<string, List<Weapon>>>()
	{
			Tuple.Create("Ground Atk", meleeWeapons),
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

		cursors = new List<WeaponCursor>();

		cursors.Add(new WeaponCursor(zeroWeaponCategories[1].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.groundSpecial)));
		cursors.Add(new WeaponCursor(zeroWeaponCategories[2].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.airSpecial)));
		cursors.Add(new WeaponCursor(zeroWeaponCategories[3].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.uppercutS)));
		cursors.Add(new WeaponCursor(zeroWeaponCategories[4].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.uppercutA)));
		cursors.Add(new WeaponCursor(zeroWeaponCategories[5].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.downThrustS)));
		cursors.Add(new WeaponCursor(zeroWeaponCategories[6].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.downThrustA)));
		cursors.Add(new WeaponCursor(zeroWeaponCategories[7].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.gigaAttack)));
		cursors.Add(new WeaponCursor(Options.main.zeroLoadout.hyperMode));
	}

	public void update() {
		if (!string.IsNullOrEmpty(error)) {
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				error = null;
			}
			return;
		}

		int maxCatCount = 3;
		if (selCursorIndex < 8) {
			maxCatCount = zeroWeaponCategories[selCursorIndex].Item2.Count;
		}

		if (!isIndexDisabled(selCursorIndex)) {
			Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, maxCatCount - 1, wrap: true, playSound: true);
		}

		Helpers.menuUpDown(ref selCursorIndex, 0, 8);

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

			Options.main.zeroLoadout.groundSpecial = zeroWeaponCategories[1].Item2[cursors[1].index].type;
			Options.main.zeroLoadout.airSpecial = zeroWeaponCategories[2].Item2[cursors[2].index].type;
			Options.main.zeroLoadout.uppercutS = zeroWeaponCategories[3].Item2[cursors[3].index].type;
			Options.main.zeroLoadout.uppercutA = zeroWeaponCategories[4].Item2[cursors[4].index].type;
			Options.main.zeroLoadout.downThrustS = zeroWeaponCategories[5].Item2[cursors[5].index].type;
			Options.main.zeroLoadout.downThrustA = zeroWeaponCategories[6].Item2[cursors[6].index].type;
			Options.main.zeroLoadout.gigaAttack = zeroWeaponCategories[7].Item2[cursors[7].index].type;
			Options.main.zeroLoadout.hyperMode = cursors[8].index;
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
		return zeroWeaponCategories[3].Item2[cursors[3].index].type == zeroWeaponCategories[4].Item2[cursors[4].index].type ||
			zeroWeaponCategories[5].Item2[cursors[5].index].type == zeroWeaponCategories[6].Item2[cursors[6].index].type;
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
		for (int i = 0; i < 8; i++) {
			color = isIndexDisabled(i) ? Helpers.Gray : Color.White;
			alpha = isIndexDisabled(i) ? 0.5f : 1f;
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

		color = isIndexDisabled(8) ? Helpers.Gray : Color.White;
		alpha = isIndexDisabled(8) ? 0.5f : 1f;

		float hyperModeYPos = startY - 6 + (wepH * 8);
		Fonts.drawText(
			FontType.Blue, "Hyper Mode:", 40, hyperModeYPos,
			selected: selCursorIndex == 8
		);
		if (cursors[8].index == 0) {
			Fonts.drawText(
				FontType.Grey, "Black Zero", wepTextX, hyperModeYPos,
				selected: selCursorIndex == 8
			);
			Global.sprites["hud_killfeed_weapon"].drawToHUD(122, wepPosX, hyperModeYPos + 3, alpha);
		} else if (cursors[8].index == 1) {
			Fonts.drawText(
				FontType.Red, "Awakened Zero", wepTextX, hyperModeYPos,
				selected: selCursorIndex == 8
			);
			Global.sprites["hud_killfeed_weapon"].drawToHUD(87, wepPosX, hyperModeYPos + 3, alpha);
		} else if (cursors[8].index == 2) {
			Fonts.drawText(
				FontType.DarkPurple, "Viral Zero", wepTextX, hyperModeYPos,
				selected: selCursorIndex == 8
			);
			Global.sprites["hud_killfeed_weapon"].drawToHUD(173, wepPosX, hyperModeYPos + 3, alpha);
		}

		int wsy = 167;
		DrawWrappers.DrawRect(
			25, wsy + 2, Global.screenW - 30, wsy + 28, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: outlineColor
		);

		if (selCursorIndex < 8) {
			var wep = zeroWeaponCategories[selCursorIndex].Item2[cursors[selCursorIndex].index];
			int posY = 6;
			foreach (string description in wep.description) {
				Fonts.drawText(
					FontType.Green, wep.description[0], 30, wsy + 6, Alignment.Left
				);
				posY += 9;
			}
		} else {
			if (cursors[8].index == 0) {
				Fonts.drawText(FontType.Green, "This hyper form increases speed and damage.", 40, wsy + 6);
				Fonts.drawText(FontType.Green, "Lasts 12 seconds.", 40, wsy + 15);
			} else if (cursors[8].index == 1) {
				Fonts.drawText(FontType.Green, "This hyper form grants powerful ranged attacks.", 40, wsy + 6);
				Fonts.drawText(FontType.Green, $"Lasts until {Global.nameCoins} are depleted.", 40, wsy + 15);
			} else if (cursors[8].index == 2) {
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

	public bool isIndexDisabled(int index) {
		if (cursors[0].index == 1) {
			return index >= 1 && index < 7;
		}
		if (cursors[0].index == 2) {
			return index >= 1 && index < 9;
		}
		return false;
	}
}
