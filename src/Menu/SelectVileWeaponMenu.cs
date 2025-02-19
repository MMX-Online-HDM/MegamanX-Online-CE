using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class SelectVileWeaponMenu : IMainMenu {
	public List<WeaponCursor> cursors;
	public int selCursorIndex;
	public bool inGame;
	public string? error = "";

	public static (string name, Weapon[] weapons)[] vileWeaponCategories = {
			("Cannon", [
				new VileCannon(VileCannonType.None),
				new VileCannon(VileCannonType.FrontRunner),
				new VileCannon(VileCannonType.LongshotGizmo),
				new VileCannon(VileCannonType.FatBoy)
			]),
			("Vulcan", [
				new Vulcan(VulcanType.None),
				new Vulcan(VulcanType.NoneCutter),
				new Vulcan(VulcanType.NoneMissile),
				new Vulcan(VulcanType.CherryBlast),
				new Vulcan(VulcanType.DistanceNeedler),
				new Vulcan(VulcanType.BuckshotDance)
			]),
			("Missile", [
				new VileMissile(VileMissileType.None),
				new VileMissile(VileMissileType.ElectricShock),
				new VileMissile(VileMissileType.HumerusCrush),
				new VileMissile(VileMissileType.PopcornDemon)
			]),
			("R.Punch", [
				new RocketPunch(RocketPunchType.None),
				new RocketPunch(RocketPunchType.GoGetterRight),
				new RocketPunch(RocketPunchType.SpoiledBrat),
				new RocketPunch(RocketPunchType.InfinityGig),
			]),
			("Napalm", [
				new Napalm(NapalmType.None),
				new Napalm(NapalmType.NoneBall),
				new Napalm(NapalmType.RumblingBang),
				new Napalm(NapalmType.FireGrenade),
				new Napalm(NapalmType.SplashHit),
				new Napalm(NapalmType.NoneFlamethrower)
			]),
			("Grenade", [
				new VileBall(VileBallType.None),
				new VileBall(VileBallType.NoneNapalm),
				new VileBall(VileBallType.ExplosiveRound),
				new VileBall(VileBallType.SpreadShot),
				new VileBall(VileBallType.PeaceOutRoller),
				new VileBall(VileBallType.NoneFlamethrower)
			]),
			("Cutter", [
				new VileCutter(VileCutterType.None),
				new VileCutter(VileCutterType.QuickHomesick),
				new VileCutter(VileCutterType.ParasiteSword),
				new VileCutter(VileCutterType.MaroonedTomahawk)
			]),
			("Flamethrower", [
				NoneFlamethrower.netWeapon,
				WildHorseKick.netWeapon,
				SeaDragonRage.netWeapon,
				DragonsWrath.netWeapon
			]),
			("Laser", [
				new VileLaser(VileLaserType.None),
				new VileLaser(VileLaserType.RisingSpecter),
				new VileLaser(VileLaserType.NecroBurst),
				new VileLaser(VileLaserType.StraightNightmare)
			]),
		};

	public IMainMenu prevMenu;

	public SelectVileWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;

		cursors = new List<WeaponCursor>();

		cursors.Add(new WeaponCursor(vileWeaponCategories[0].weapons.FindIndex(w => w.type == Options.main.vileLoadout.cannon)));
		cursors.Add(new WeaponCursor(vileWeaponCategories[1].weapons.FindIndex(w => w.type == Options.main.vileLoadout.vulcan)));
		cursors.Add(new WeaponCursor(vileWeaponCategories[2].weapons.FindIndex(w => w.type == Options.main.vileLoadout.missile)));
		cursors.Add(new WeaponCursor(vileWeaponCategories[3].weapons.FindIndex(w => w.type == Options.main.vileLoadout.rocketPunch)));
		cursors.Add(new WeaponCursor(vileWeaponCategories[4].weapons.FindIndex(w => w.type == Options.main.vileLoadout.napalm)));
		cursors.Add(new WeaponCursor(vileWeaponCategories[5].weapons.FindIndex(w => w.type == Options.main.vileLoadout.ball)));
		cursors.Add(new WeaponCursor(vileWeaponCategories[6].weapons.FindIndex(w => w.type == Options.main.vileLoadout.cutter)));
		cursors.Add(new WeaponCursor(vileWeaponCategories[7].weapons.FindIndex(w => w.type == Options.main.vileLoadout.flamethrower)));
		cursors.Add(new WeaponCursor(vileWeaponCategories[8].weapons.FindIndex(w => w.type == Options.main.vileLoadout.laser)));
	}

	public void update() {
		if (!string.IsNullOrEmpty(error)) {
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				error = null;
			}
			return;
		}

		int maxCatCount = vileWeaponCategories[selCursorIndex].weapons.Length;

		int minIndex = 0;
		/*if (selCursorIndex == 0 || selCursorIndex == 1 || selCursorIndex == 2 || selCursorIndex == 3) {
			minIndex = 1;
		} */

		Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, minIndex, maxCatCount - 1, wrap: true, playSound: true);

		Helpers.menuUpDown(ref selCursorIndex, 0, vileWeaponCategories.Length - 1);

		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		bool selectPressed = Global.input.isPressedMenu(Control.MenuConfirm) || (backPressed && !inGame);
		if (selectPressed) {
			if (getWeightSum() > VileLoadout.maxWeight) {
				error = "Cannot exceed maximum loadout weight.";
				return;
			}

			/*if (vileWeaponCategories[0].weapons[cursors[0].index].type == -1 && vileWeaponCategories[1].weapons[cursors[1].index].type == -1) {
				error = "Must equip either a Vulcan or Cannon.";
				return;
			} */

			int[] oldArray = { Options.main.vileLoadout.cannon, Options.main.vileLoadout.vulcan, Options.main.vileLoadout.missile, Options.main.vileLoadout.rocketPunch, Options.main.vileLoadout.napalm, Options.main.vileLoadout.ball, Options.main.vileLoadout.cutter, Options.main.vileLoadout.flamethrower, Options.main.vileLoadout.laser };
			Options.main.vileLoadout.cannon = vileWeaponCategories[0].weapons[cursors[0].index].type;
			Options.main.vileLoadout.vulcan = vileWeaponCategories[1].weapons[cursors[1].index].type;
			Options.main.vileLoadout.missile = vileWeaponCategories[2].weapons[cursors[2].index].type;
			Options.main.vileLoadout.rocketPunch = vileWeaponCategories[3].weapons[cursors[3].index].type;
			Options.main.vileLoadout.napalm = vileWeaponCategories[4].weapons[cursors[4].index].type;
			Options.main.vileLoadout.ball = vileWeaponCategories[5].weapons[cursors[5].index].type;
			Options.main.vileLoadout.cutter = vileWeaponCategories[6].weapons[cursors[6].index].type;
			Options.main.vileLoadout.flamethrower = vileWeaponCategories[7].weapons[cursors[7].index].type;
			Options.main.vileLoadout.laser = vileWeaponCategories[8].weapons[cursors[8].index].type;
			int[] newArray = { Options.main.vileLoadout.cannon, Options.main.vileLoadout.vulcan, Options.main.vileLoadout.missile, Options.main.vileLoadout.rocketPunch, Options.main.vileLoadout.napalm, Options.main.vileLoadout.ball, Options.main.vileLoadout.cutter, Options.main.vileLoadout.flamethrower, Options.main.vileLoadout.laser };

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

	public int getWeightSum() {
		int total = Global.level?.mainPlayer?.getVileWeight(0) ?? 0;
		for (int i = 0; i < vileWeaponCategories.Length; i++) {
			total += vileWeaponCategories[i].weapons[cursors[i].index].vileWeight;
		}
		return total;
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["loadoutbackground"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenuload"], 0, 0);
		}

		Fonts.drawText(FontType.Yellow, "Vile Loadout", Global.screenW * 0.5f, 20, Alignment.Center);
		Fonts.drawText(
			FontType.DarkOrange, "Weight: ", Global.screenW - 60, 20, Alignment.Right
		);
		Fonts.drawText(
			getWeightSum() > VileLoadout.maxWeight ? FontType.RedishOrange : FontType.DarkPurple,
			getWeightSum() + "/" + 28,
			Global.screenW - 60, 20,
			Alignment.Left
		);
		var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
		float botOffY = inGame ? 0 : -2;

		int startY = 44;
		int startX = 20;
		int wepH = 14;

		float wepPosX = 120;
		float wepTextX = 132;

		Global.sprites["cursor"].drawToHUD(0, startX+4, startY + (selCursorIndex * wepH) - 3);
		for (int i = 0; i < vileWeaponCategories.Length; i++) {
			float yPos = startY - 6 + (i * wepH);
			Fonts.drawText(
				FontType.Blue, vileWeaponCategories[i].name + ": ",
				startX + 10, yPos, selected: selCursorIndex == i
			);
			var weapon = vileWeaponCategories[i].weapons[cursors[i].index];
			if (weapon.killFeedIndex != 0) {
				Global.sprites["hud_killfeed_weapon"].drawToHUD(weapon.killFeedIndex, wepPosX, yPos + 3);
				Fonts.drawText(FontType.Blue, weapon.displayName, wepTextX, yPos, selected: selCursorIndex == i);
			} else {
				Fonts.drawText(FontType.Blue, weapon.displayName, wepPosX - 5, yPos, selected: selCursorIndex == i);
			}

			Fonts.drawText(
				FontType.Purple, "W:" + weapon.vileWeight.ToString(),
				Global.screenW - 30, yPos, alignment: Alignment.Right,
				selected: selCursorIndex == i
			);
		}

		var wep = vileWeaponCategories[selCursorIndex].weapons[cursors[selCursorIndex].index];

		int wsy = 167;
		string inputText = "";
		if (selCursorIndex == 0) inputText = "[SHOOT]";
		if (selCursorIndex == 1) inputText = "[WeaponR]";
		if (selCursorIndex == 2) inputText = "[SPC] + On Ground";
		if (selCursorIndex == 3) inputText = "[MRIGHT] / [MLEFT] + [SPC]";
		if (selCursorIndex == 4) inputText = "[MDOWN] + [SPC] + On Ground";
		if (selCursorIndex == 5) inputText = "[SPC] + On Air";
		if (selCursorIndex == 6) inputText = "[MUP] + [SPC] + On Ground";
		if (selCursorIndex == 7) inputText = "[MDOWN] + [SPC] + On Air";
		if (selCursorIndex == 8) inputText = "Hold [SPC]";
		string damage = wep.damage;
		string rateOfFire = wep.fireRate.ToString();
		string ammousage = wep.ammousage.ToString();
		string effect = wep.effect;
		string hitcooldown = wep.hitcooldown;
		string Flinch = wep.Flinch;
		string FlinchCD = wep.FlinchCD;

		DrawWrappers.DrawRect(25, wsy - 7, Global.screenW - 30, wsy + 28, true, new Color(0, 0, 0, 100), 
		0.5f, ZIndex.HUD, false, outlineColor: outlineColor); // Big Rectangle

		//Input Description Section
		DrawWrappers.DrawRect(207, 184, 354, 160, true, new Color(0, 0, 0, 100), 
		0.5f, ZIndex.HUD, false, outlineColor: outlineColor); //Input Rectangle
		Fonts.drawTextEX(FontType.Purple, "INPUT", 281, wsy-4, Alignment.Center);
		Fonts.drawTextEX(FontType.Grey, inputText, 281, wsy + 6, Alignment.Center);

		// Damage, Flinch, Ammo Section
		DrawWrappers.DrawRect(25, 172, 114, 160, true, new Color(0, 0, 0, 100), 
		0.5f, ZIndex.HUD, false, outlineColor: outlineColor); //DMG Rectangle
		DrawWrappers.DrawRect(25, 184, 114, 172, true, new Color(0, 0, 0, 100), 
		0.5f, ZIndex.HUD, false, outlineColor: outlineColor); //Flinch Rectangle
		DrawWrappers.DrawRect(25, 195, 114, 184, true, new Color(0, 0, 0, 100), 
		0.5f, ZIndex.HUD, false, outlineColor: outlineColor); //Ammo Rectangle
		DrawWrappers.DrawRect(114, 172, 206, 160, true, new Color(0, 0, 0, 100), 
		0.5f, ZIndex.HUD, false, outlineColor: outlineColor); //Fire Rate Rectangle
		DrawWrappers.DrawRect(114, 184, 206, 172, true, new Color(0, 0, 0, 100), 
		0.5f, ZIndex.HUD, false, outlineColor: outlineColor); //Flinch CD Rectangle
		DrawWrappers.DrawRect(114, 195, 206, 184, true, new Color(0, 0, 0, 100), 
		0.5f, ZIndex.HUD, false, outlineColor: outlineColor); //Hit CD Rectangle
		Fonts.drawTextEX(FontType.Purple, "Damage:", 48, wsy-4, Alignment.Center);
		Fonts.drawTextEX(FontType.Purple, "Flinch:", 46, wsy+8, Alignment.Center);	
		Fonts.drawTextEX(FontType.Purple, "Ammo Use:", 55, wsy+19, Alignment.Center);	
		Fonts.drawTextEX(FontType.Purple, "Fire Rate:", 145, wsy-4, Alignment.Center);
		Fonts.drawTextEX(FontType.Purple, "Flinch CD:", 147, wsy+8, Alignment.Center);
		Fonts.drawTextEX(FontType.Purple, "Hit CD:", 138, wsy+19, Alignment.Center);
		Fonts.drawTextEX(FontType.Purple, damage, 86, 163, Alignment.Center);
		Fonts.drawTextEX(FontType.Purple, Flinch, 86, wsy+8, Alignment.Center);	
		Fonts.drawTextEX(FontType.Purple, ammousage, 100, wsy+19, Alignment.Center);	
		Fonts.drawTextEX(FontType.Purple, rateOfFire, 189, wsy-4, Alignment.Center);
		Fonts.drawTextEX(FontType.Purple, FlinchCD, 189, wsy+8, Alignment.Center);
		Fonts.drawTextEX(FontType.Purple, hitcooldown, 184, wsy+19, Alignment.Center);
		Fonts.drawTextEX(FontType.Purple, effect, 281, wsy+19, Alignment.Center);





		/*
		int descLine1 = wsy + 8;
		int descLine2 = wsy + 17;
		descLine1 = wsy + 13;
		descLine2 = wsy + 20;

		if (wep.description?.Length > 0) {
			Fonts.drawText(
				FontType.Grey, wep.description[0], 40, wep.description.Length == 1 ? descLine1 + 3 : descLine1
			);
		}
		if (wep.description?.Length > 1) {
			Fonts.drawText(
				FontType.Grey, wep.description[1], 40, descLine2, Alignment.Left
			);
		}
		*/
		//Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change Weapon", Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 18);
		//.drawTextStd(TCat.BotHelp, "Up/Down: Change Category", Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 18);
		//string helpText = "[BACK]: Back, [OK]: Confirm";
		//if (!inGame) helpText = "[BACK]: Save and back";
		//Helpers.drawTextStd(TCat.BotHelp, helpText, Global.screenW * 0.5f, 210 + botOffY, Alignment.Center, fontSize: 18);

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
