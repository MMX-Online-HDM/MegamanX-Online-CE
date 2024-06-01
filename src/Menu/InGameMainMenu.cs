namespace MMXOnline;

public class InGameMainMenu : IMainMenu {
	public static int selectY = 0;
	public int[] optionPos = {
		50,
		70,
		90,
		110,
		130,
		150,
		170
	};
	public float startX = 92;

	public InGameMainMenu() {
	}

	public Player mainPlayer { get { return Global.level.mainPlayer; } }

	public void update() {
		if (!mainPlayer.canUpgradeXArmor()) {
			UpgradeMenu.onUpgradeMenu = true;
		}

		Helpers.menuUpDown(ref selectY, 0, 6);
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectY == 0) {
				if (isSelWepDisabled()) return;
				if (Global.level.mainPlayer.realCharNum == (int)CharIds.PunchyZero) {
					Menu.change(new SelectPunchyZeroWeaponMenu(this, true));
				} else if (Global.level.mainPlayer.realCharNum == 4) {
					Menu.change(new SelectSigmaWeaponMenu(this, true));
				} else if (Global.level.mainPlayer.realCharNum == 3) {
					Menu.change(new SelectAxlWeaponMenu(this, true));
				} else if (Global.level.mainPlayer.realCharNum == 2) {
					Menu.change(new SelectVileWeaponMenu(this, true));
				} else if (Global.level.mainPlayer.realCharNum == 1) {
					Menu.change(new SelectZeroWeaponMenu(this, true));
				} else {
					Menu.change(new SelectWeaponMenu(this, true));
				}
			} else if (selectY == 1) {
				if (isSelArmorDisabled()) return;
				if (Global.level.mainPlayer.realCharNum == 0 || Global.level.mainPlayer.realCharNum == 2) {
					if (UpgradeMenu.onUpgradeMenu && !Global.level.server.disableHtSt) {
						Menu.change(new UpgradeMenu(this));
					} else if (Global.level.mainPlayer.realCharNum == 0) {
						Menu.change(new UpgradeArmorMenu(this));
					} else if (Global.level.mainPlayer.realCharNum == 2) {
						Menu.change(new SelectVileArmorMenu(this));
					}
				} else {
					if (!Global.level.server.disableHtSt) {
						Menu.change(new UpgradeMenu(this));
					}
				}
			} else if (selectY == 2) {
				if (isSelCharDisabled()) return;
				Menu.change(new SelectCharacterMenu(this, Global.level.is1v1(), Global.serverClient == null, true, false, Global.level.gameMode.isTeamMode, Global.isHost, () => { Menu.exit(); }));
			} else if (selectY == 3) {
				if (isMatchOptionsDisabled()) return;
				Menu.change(new MatchOptionsMenu(this));
			} else if (selectY == 4) {
				Menu.change(new PreControlMenu(this, true));
			} else if (selectY == 5) {
				Menu.change(new PreOptionsMenu(this, true));
			} else if (selectY == 6) {
				Menu.change(new ConfirmLeaveMenu(this, "Are you sure you want to leave?", () => {
					Global._quickStart = false;
					Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.LeftManually, null, null);
				}));
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.exit();
		}
	}

	public bool isSelWepDisabled() {
		return Global.level.is1v1() || mainPlayer.realCharNum == (int)CharIds.BusterZero;
	}

	public bool isSelArmorDisabled() {
		if (Global.level.is1v1()) return true;
		if (mainPlayer.realCharNum == 2) return false;
		if (Global.level.server.disableHtSt) {
			if (mainPlayer.realCharNum != 0) return Global.level.server.disableHtSt;
			if (mainPlayer.canUpgradeXArmor()) {
				return false;
			} else {
				return Global.level.server.disableHtSt;
			}
		}
		return false;
	}

	public bool isSelCharDisabled() {
		if (Global.level.isElimination()) return true;

		if (Global.level.server?.customMatchSettings?.redSameCharNum > -1) {
			if (Global.level.gameMode.isTeamMode && Global.level.mainPlayer.alliance == GameMode.redAlliance) {
				return true;
			}
		}
		if (Global.level.server?.customMatchSettings?.sameCharNum > -1) {
			if (!Global.level.gameMode.isTeamMode || Global.level.mainPlayer.alliance == GameMode.blueAlliance) {
				return true;
			}
		}

		return false;
	}

	public bool isMatchOptionsDisabled() {
		return false;
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.Yellow, "MENU", Global.screenW * 0.5f, 20, Alignment.Center);

		Global.sprites["cursor"].drawToHUD(0, startX - 10, optionPos[0] + 3 + (selectY * 20));

		Fonts.drawText(
			isSelWepDisabled() ? FontType.DarkBlue : FontType.Blue,
			"Edit Loadout", startX, optionPos[0], selected: selectY == 0
		);
		Fonts.drawText(
			isSelArmorDisabled() ? FontType.DarkBlue : FontType.Blue,
			"Upgrade Menu", startX, optionPos[1], selected: selectY == 1
		);
		Fonts.drawText(
			isSelCharDisabled() ? FontType.DarkBlue : FontType.Blue,
			"Switch Character", startX, optionPos[2], selected: selectY == 2
		);
		Fonts.drawText(
			isMatchOptionsDisabled() ? FontType.DarkBlue : FontType.Blue,
			"Match Options", startX, optionPos[3], selected: selectY == 3
		);
		Fonts.drawText(FontType.Blue, "Controls", startX, optionPos[4], selected: selectY == 4);
		Fonts.drawText(FontType.Blue, "Settings", startX, optionPos[5], selected: selectY == 5);
		Fonts.drawText(FontType.Blue, "Leave Match", startX, optionPos[6], selected: selectY == 6);
		Fonts.drawTextEX(FontType.Grey, "[OK]: Choose, [ESC]: Cancel", Global.halfScreenW, 198, Alignment.Center);
	}
}
