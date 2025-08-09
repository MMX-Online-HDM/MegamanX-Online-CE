using System.Globalization;
using Newtonsoft.Json;
using SFML.Graphics;

namespace MMXOnline;

public class PreJoinOrHostMenu : IMainMenu {
	public int selectY;
	public Point[] optionPos;
	public const int lineH = 17;
	public MainMenu prevMenu;
	public bool isJoin;
	public const float startX = 120;
	public int state;
	public float state1Time;
	public float Time = 1, Time2;
	public bool Confirm = false, Confirm2 = false;
	public PreJoinOrHostMenu(MainMenu prevMenu, bool isJoin) {
		this.prevMenu = prevMenu;
		this.isJoin = isJoin;
		optionPos = new Point[] {
			new Point(40, 107), //70
			new Point(40, 107 + lineH),
			new Point(40, 107 + (lineH * 2))
		};
	}

	public void update() {
		if (state == 0) {
			Helpers.menuUpDown(ref selectY, 0, 2);
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				if (selectY == 2) {
					state = 1;
				} else if (selectY == 1) {
					IMainMenu nextMenu = null;
					if (isJoin) nextMenu = new JoinMenu(true);
					else nextMenu = new HostMenu(prevMenu, null, false, true);

					Menu.change(nextMenu);
				} else if (selectY == 0) {
					IMainMenu nextMenu = null;
					if (isJoin) nextMenu = new JoinMenuP2P(true);
					// TODO: Make a menu for new host.
					else nextMenu = new HostMenu(prevMenu, null, false, false, true);
					Menu.change(nextMenu);
				}
			}
			else if (Options.main.blackFade) {
				TimeUpdate();
				if (Time2 >= 1) {
					Menu.change(prevMenu);
					prevMenu.Time = 0;
					prevMenu.Time2 = 1;
					prevMenu.Confirm = false;
					prevMenu.Confirm2 = false;
				}
			} else {
				if (Global.input.isPressedMenu(Control.MenuBack)) {
					Menu.change(prevMenu);				
				}
			}
		} else if (state == 1) {
				if (Global.regions.Count == 0) {
					state = 0;
					Menu.change(new ErrorMenu(new string[] {
						"No multiplayer regions configured.",
						"Please add a region name/ip to region.json",
						"in game or MMXOD folder, then restart the game.",
				}, this));
				} else {
					state1Time = 0;
					state = 0;
					IMainMenu nextMenu = null;
					if (isJoin) nextMenu = new JoinMenu(false);
					else nextMenu = new HostMenu(prevMenu, null, false, false);
					// Bans are useless and do not work without a main relay so are disabled.
					/*
					if (canPlayOnline(out string[] warningMessage)) {
						if (warningMessage != null) {
							Menu.change(new ErrorMenu(warningMessage, nextMenu));
						} else {
							Menu.change(nextMenu);
						}
					}
					*/
					Menu.change(nextMenu);
				}
			}
	}
	public void TimeUpdate() {
		if (Confirm == false) Time -= Global.spf * 2;
		if (Time <= 0) {
			Confirm = true;
			Time = 0;
		}
		if (Global.input.isPressedMenu(Control.MenuBack)) Confirm2 = true;
		if (Confirm2 == true) Time2 += Global.spf * 2;
	}
	private string[] getOutdatedClientMessage(decimal version, decimal serverVersion) {
		if (serverVersion == decimal.MaxValue) {
			return new string[] { "Could not connect to server.", "The region may be down.", "Try changing your region in Settings." };
		}
		return new string[]
		{
				string.Format(CultureInfo.InvariantCulture, "Your version of the game (v{0}) is outdated.", version),
				string.Format(CultureInfo.InvariantCulture, "Please update to the new version (v{0})", serverVersion),
				"for online play."
		};
	}

	private string[] getOutdatedClientMessage2(decimal version, decimal serverVersion) {
		if (serverVersion == decimal.MaxValue) {
			return new string[] { "Could not connect to server.", "The region may be down.", "Try changing your region in Settings." };
		}
		return new string[]
		{
				string.Format(CultureInfo.InvariantCulture, "Your version of the game (v{0}) is too new.", version),
				string.Format(CultureInfo.InvariantCulture, "Please revert to the version (v{0})", serverVersion),
				"for online play."
		};
	}

	public bool canPlayOnline(out string[] warningMessage) {
		warningMessage = null;

		string deviceId = Global.deviceId;
		if (string.IsNullOrEmpty(deviceId)) {
			Menu.change(new ErrorMenu(new string[] { "Error in fetching device id.", "You cannot play online." }, new MainMenu()));
			return false;
		}
		if (!Global.checkBan) {
			var response = Global.matchmakingQuerier.send(Options.main.getRegion().ip, "CheckBan:" + deviceId, "CheckBan");
			if (response != null) {
				Global.checkBan = true;
				if (response != "") Global.banEntry = JsonConvert.DeserializeObject<BanEntry>(response);
			}
		}
		if (!Global.checkBan) {
			Menu.change(new ErrorMenu(new string[] { "Unable to connect to server in region.json" }, new MainMenu()));
			return false;
		}
		if (Global.banEntry != null) {
			string banEndDateStr = "Never";
			if (Global.banEntry.bannedUntil != null) banEndDateStr = Global.banEntry.bannedUntil.ToString();

			if (Global.banEntry.banType == 0) {
				var banLines = new string[]
				{
						"You are currently banned from online play!",
						"Reason: " + Global.banEntry.reason,
						"Ban end date: " + banEndDateStr,
						"Appeal to an admin of the server."
				};
				Menu.change(new ErrorMenu(banLines, new MainMenu()));
				return false;
			} else if (Global.banEntry.banType == 1) {
				var banLines = new string[]
				{
						"ALERT: Currently banned from chat/voting.",
						"Reason: " + Global.banEntry.reason,
						"Ban end date: " + banEndDateStr,
						"Appeal to an admin of the server."
				};
				warningMessage = banLines;
			} else if (Global.banEntry.banType == 2) {
				var banLines = new string[]
				{
						"You are warned for bad online conduct.",
						"Reason: " + Global.banEntry.reason,
						"Further misconduct will result in a ban.",
						"Appeal to an admin of the server."
				};
				warningMessage = banLines;
			}
		}

		return true;
	}

	public void render() {
		float WD = Global.screenW * 0.5f;
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		//DrawWrappers.DrawTextureMenu(Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace));
		//Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * lineH));
		Fonts.drawText(
			FontType.Golden, "SELECT OPTION",WD, 20, Alignment.Center
		);

		if (state == 0) {
			Fonts.drawText(FontType.DarkBlue, "RELAY", WD+1, optionPos[2].y, Alignment.Center,
			 selected: selectY == 2, alpha: 40);
		} else {
			Fonts.drawText(FontType.DarkBlue, "LOADING...", startX, optionPos[2].y, selected: selectY == 2);
		}

		Fonts.drawText(FontType.DarkBlue, "LAN", WD, optionPos[1].y,
		Alignment.Center, selected: selectY == 1, alpha: 40);

		if (Global.flFrameCount % 60 < 30) {
		Fonts.drawText(FontType.DarkBlue, "[     ]", WD, optionPos[0].y,
		Alignment.Center,selected: selectY == 0);
		}
		Fonts.drawText(FontType.DarkBlue, "  P2P  ", WD, optionPos[0].y,
		Alignment.Center, selected: selectY == 0);
		Fonts.drawTextEX(FontType.Grey, "[OK]: Choose, [BACK]: Back", WD, 206, Alignment.Center);
		if (Options.main.blackFade) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time);
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time2);
		}
	}
	public void Guide() {
		int msgPos = 140;
		float WD = Global.screenW * 0.5f;

		DrawWrappers.DrawLine(
			10, msgPos - 5, Global.screenW - 10, msgPos - 5, Color.White, 1, ZIndex.HUD, isWorldPos: false
		);
		
		Fonts.drawText(
			FontType.DarkOrange, "NOTICE", WD,
			msgPos, Alignment.Center
		);
		Fonts.drawText(
			FontType.DarkBlue, "See link below for self hosting guide:",
			Global.halfScreenW, msgPos + 10, Alignment.Center
		);
		Fonts.drawText(
			FontType.DarkBlue, "https://gamemaker19.github.io/MMXOnlineDesktop/decom.html",
			Global.halfScreenW, msgPos + 20, Alignment.Center
		);
		DrawWrappers.DrawLine(
			10, msgPos + 32, Global.screenW - 10, msgPos + 32, Color.White, 1, ZIndex.HUD, isWorldPos: false
		);
	}
}
