using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

[ProtoContract]
public class ExtraCpuCharData {
	[ProtoMember(1)] public List<PlayerCharData> cpuDatas;

	public ExtraCpuCharData() {
		cpuDatas = new List<PlayerCharData>();
	}
}

public class ConfigureCPUMenu : IMainMenu {
	public IMainMenu prevMenu;
	public List<MenuOption> menuOptions = new List<MenuOption>();
	public int selectArrowPosY;
	public const int startX = 30;
	public int startY = 30;
	public const int lineH = 10;
	public const uint fontSize = 24;

	public bool is1v1;
	public bool isOffline;
	public bool isInGame;
	public bool isInGameEndSelect;
	public bool isTeamMode;
	public bool isHost;

	public List<CharSelection> charSelections;

	public ConfigureCPUMenu(IMainMenu prevMenu, int cpuCount, bool is1v1, bool isOffline, bool isInGame, bool isInGameEndSelect, bool isTeamMode, bool isHost) {
		this.prevMenu = prevMenu;
		this.is1v1 = is1v1;
		this.isOffline = isOffline;
		this.isInGame = isInGame;
		this.isInGameEndSelect = isInGameEndSelect;
		this.isTeamMode = isTeamMode;
		this.isHost = isHost;

		int currentY = startY;
		if (cpuCount >= 9 && isTeamMode) {
			currentY -= 10;
		}

		SavedMatchSettings savedMatchSettings = isOffline ? SavedMatchSettings.mainOffline : SavedMatchSettings.mainOnline;

		while (savedMatchSettings.extraCpuCharData.cpuDatas.Count < cpuCount) {
			savedMatchSettings.extraCpuCharData.cpuDatas.Add(new PlayerCharData());
		}
		while (savedMatchSettings.extraCpuCharData.cpuDatas.Count > cpuCount) {
			savedMatchSettings.extraCpuCharData.cpuDatas.Pop();
		}

		charSelections = is1v1 ? CharSelection.selections1v1 : CharSelection.selections;
		charSelections = new List<CharSelection>(charSelections);
		charSelections.Insert(0, new CharSelection("Random", 0, 1, 0, "", 0));

		for (int i = 0; i < savedMatchSettings.extraCpuCharData.cpuDatas.Count; i++) {
			var cpuData = savedMatchSettings.extraCpuCharData.cpuDatas[i];
			cpuData.uiSelectedCharIndex = Helpers.clamp(cpuData.uiSelectedCharIndex, 0, charSelections.Count - 1);

			bool forceEnable = (isOffline && i == 0);
			int iCopy = i;

			// CPU Character
			menuOptions.Add(
				new MenuOption(60, currentY += lineH,
					() => {
						Helpers.menuLeftRightInc(ref cpuData.uiSelectedCharIndex, 0, charSelections.Count - 1);
						cpuData.charNum = charSelections[cpuData.uiSelectedCharIndex].mappedCharNum;
						cpuData.armorSet = charSelections[cpuData.uiSelectedCharIndex].mappedCharArmor;
						cpuData.isRandom = charSelections[cpuData.uiSelectedCharIndex].name == "Random";
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "CPU" + (iCopy + 1).ToString(),
							pos.x - 32, pos.y
						);
						Fonts.drawText(
							FontType.Blue, "Character: " + charSelections[cpuData.uiSelectedCharIndex].name,
							pos.x + 14, pos.y, selected: index == selectArrowPosY
						);
					})
				);

			if (isTeamMode) {
				// Team
				menuOptions.Add(
					new MenuOption(startX + 30, currentY += lineH,
						() => {
							Helpers.menuLeftRightInc(ref cpuData.alliance, -1, Global.level.teamNum);
						},
						(Point pos, int index) => {
							string allianceStr = "auto";
							if (cpuData.alliance >= 0) {
								allianceStr = GameMode.getTeamName(cpuData.alliance);
							}
							Fonts.drawText(
								FontType.Blue, "Team: " + allianceStr, pos.x, pos.y,
								selected: index == selectArrowPosY
							);
						})
					);
			}
		}
	}

	public void update() {
		Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);

		menuOptions[selectArrowPosY].update();

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
			return;
		}
	}

	public void render() {
		if (!isInGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		}

		Fonts.drawText(
			FontType.Yellow, "Configure CPU players",
			Global.halfScreenW, 20, alignment: Alignment.Center
		);
		DrawWrappers.DrawTextureHUD(
			Global.textures["cursor"], menuOptions[0].pos.x + 6, menuOptions[(int)selectArrowPosY].pos.y - 2
		);

		int i = 0;
		foreach (var menuOption in menuOptions) {
			menuOption.render(menuOption.pos, i);
			i++;
		}

		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change, [BACK]: Back",
			Global.screenW * 0.5f, Global.screenH - 28, Alignment.Center
		);
	}
}
