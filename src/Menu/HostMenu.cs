using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using Lidgren.Network;
using SFML.Graphics;

namespace MMXOnline;

public class HostMenuSettings {
	public int gameModeIndex;
	public int mapSizeIndex = 0;
	public string mapName;
	[JsonIgnore] public int? mapIndex;
	public int botCount = 0;
	public int playTo = 20;
	public bool hidden;
	public int netcodeModel;
	public int netcodeModelUnderPing = Global.defaultThresholdPing;
	public bool mirrored;
	public bool useLoadout;
	public bool isCustomMapPool;
	public bool useCustomMatchSettings;
	public int team = GameMode.blueAlliance;
	public int timeLimit;
	public bool fixedCamera;
	public bool disableHtSt;
	public bool disableVehicles;
	public byte teamNum = 2;
}

public class HostMenu : IMainMenu {
	public List<MenuOption> menuOptions;
	public int selectArrowPosY;
	public static int startX = 20;
	public static int startY = 40;
	public static int lineH = 10;

	public MainMenu previous;
	public bool listenForKey = false;
	public Point mapPos = new Point(178, 43);

	public string serverName;
	public bool isOffline;
	public bool isLAN;
	public bool isP2P;
	public string localIPAddress;

	public SavedMatchSettings savedMatchSettings {
		get {
			if (isOffline) return SavedMatchSettings.mainOffline;
			return SavedMatchSettings.mainOnline;
		}
	}

	// Match settings that persist. If adding a setting here add it to HostMenuSettings class
	public int gameModeIndex {
		get { return savedMatchSettings.hostMenuSettings.gameModeIndex; }
		set { savedMatchSettings.hostMenuSettings.gameModeIndex = value; }
	}
	public int mapSizeIndex {
		get { return savedMatchSettings.hostMenuSettings.mapSizeIndex; }
		set { savedMatchSettings.hostMenuSettings.mapSizeIndex = value; }
	}
	public int mapIndex {
		get {
			return savedMatchSettings.hostMenuSettings.mapIndex.Value;
		}
		set {
			savedMatchSettings.hostMenuSettings.mapIndex = value;
			savedMatchSettings.hostMenuSettings.mapName = selectedLevel.name;
		}
	}
	public int botCount {
		get { return savedMatchSettings.hostMenuSettings.botCount; }
		set { savedMatchSettings.hostMenuSettings.botCount = value; }
	}
	public int playTo {
		get { return savedMatchSettings.hostMenuSettings.playTo; }
		set { savedMatchSettings.hostMenuSettings.playTo = value; }
	}
	public bool hidden {
		get { return savedMatchSettings.hostMenuSettings.hidden; }
		set { savedMatchSettings.hostMenuSettings.hidden = value; }
	}
	public int netcodeModel {
		get { return savedMatchSettings.hostMenuSettings.netcodeModel; }
		set { savedMatchSettings.hostMenuSettings.netcodeModel = value; }
	}
	public int netcodeModelUnderPing {
		get { return savedMatchSettings.hostMenuSettings.netcodeModelUnderPing; }
		set { savedMatchSettings.hostMenuSettings.netcodeModelUnderPing = value; }
	}
	public bool mirrored {
		get { return savedMatchSettings.hostMenuSettings.mirrored; }
		set { savedMatchSettings.hostMenuSettings.mirrored = value; }
	}
	public bool useLoadout {
		get { return savedMatchSettings.hostMenuSettings.useLoadout; }
		set { savedMatchSettings.hostMenuSettings.useLoadout = value; }
	}
	public bool isCustomMapPool {
		get { return savedMatchSettings.hostMenuSettings.isCustomMapPool; }
		set { savedMatchSettings.hostMenuSettings.isCustomMapPool = value; }
	}
	public bool useCustomMatchSettings {
		get { return savedMatchSettings.hostMenuSettings.useCustomMatchSettings; }
		set { savedMatchSettings.hostMenuSettings.useCustomMatchSettings = value; }
	}
	public int team {
		get { return savedMatchSettings.hostMenuSettings.team; }
		set { savedMatchSettings.hostMenuSettings.team = value; }
	}
	public int timeLimit {
		get { return savedMatchSettings.hostMenuSettings.timeLimit; }
		set { savedMatchSettings.hostMenuSettings.timeLimit = value; }
	}
	public bool fixedCamera {
		get { return savedMatchSettings.hostMenuSettings.fixedCamera; }
		set { savedMatchSettings.hostMenuSettings.fixedCamera = value; }
	}
	public bool disableHtSt {
		get { return savedMatchSettings.hostMenuSettings.disableHtSt; }
		set { savedMatchSettings.hostMenuSettings.disableHtSt = value; }
	}
	public bool disableVehicles {
		get { return savedMatchSettings.hostMenuSettings.disableVehicles; }
		set { savedMatchSettings.hostMenuSettings.disableVehicles = value; }
	}
	public byte teamNum {
		get { return savedMatchSettings.hostMenuSettings.teamNum; }
		set { savedMatchSettings.hostMenuSettings.teamNum = value; }
	}

	public static int prevMapSizeIndex;
	public bool playToDirty;
	public bool timeLimitDirty;
	public float Time = 1, Time2;
	public bool Confirm = false, Confirm2 = false;

	public string[] mapSizes = new string[] { "Training", "1v1", "Small", "Medium", "Large", "Collosal" };
	public string selectedMapSize {
		get {
			if (!mapSizes.InRange(mapSizeIndex)) {
				mapSizeIndex = 0;
			}
			return mapSizes[mapSizeIndex];
		}
	}

	public LevelData selectedLevel {
		get {
			if (currentMapSizePool.Count == 0) {
				return trainingMaps[0];
			}
			if (mapIndex < 0) {
				mapIndex = currentMapSizePool.Count - 1;
			}
			if (mapIndex >= currentMapSizePool.Count) {
				mapIndex = 0;
			}
			return currentMapSizePool[mapIndex];
		}
	}

	public string selectedGameMode {
		get {
			if (!currentGameModePool.InRange(gameModeIndex)) {
				gameModeIndex = 0;
			}
			return currentGameModePool[gameModeIndex];
		}
	}

	List<LevelData> trainingMaps = new List<LevelData>();
	List<LevelData> foxOnlyMaps = new List<LevelData>();
	List<LevelData> smallMaps = new List<LevelData>();
	List<LevelData> mediumMaps = new List<LevelData>();
	List<LevelData> largeMaps = new List<LevelData>();
	List<LevelData> collosalMaps = new List<LevelData>();

	List<LevelData> trainingCustomMaps = new List<LevelData>();
	List<LevelData> foxOnlyCustomMaps = new List<LevelData>();
	List<LevelData> smallCustomMaps = new List<LevelData>();
	List<LevelData> mediumCustomMaps = new List<LevelData>();
	List<LevelData> largeCustomMaps = new List<LevelData>();
	List<LevelData> collosalCustomMaps = new List<LevelData>();

	public LevelData selectedLevelMirrored {
		get {
			string mirroredKey = selectedLevel.name + "_mirrored";
			return Global.levelDatas.GetValueOrDefault(mirroredKey);
		}
	}

	public List<LevelData> currentMapSizePool {
		get {
			if (isCustomMapPool) {
				return mapSizeIndex switch {
					0 => trainingCustomMaps,
					1 => foxOnlyCustomMaps,
					2 => smallCustomMaps,
					3 => mediumCustomMaps,
					4 => largeCustomMaps,
					_ => collosalCustomMaps
				};
			}
			return mapSizeIndex switch {
				0 => trainingMaps,
				1 => foxOnlyMaps,
				2 => smallMaps,
				3 => mediumMaps,
				4 => largeMaps,
				_ => collosalMaps
			};
		}
	}

	public List<string> currentGameModePool { get { return selectedLevel.supportedGameModes; } }

	public string errorMessage;

	public bool inGame;
	public bool isMapSelected;
	public int playerCount;
	bool isTraining { get { return selectedLevel.isTraining(); } }
	bool isRace { get { return selectedGameMode == GameMode.Race; } }
	bool is1v1 { get { return selectedLevel.is1v1(); } }
	public bool isHiddenOrLan() {
		return hidden || isLAN;
	}

	public HostMenu(
		MainMenu mainMenu, Server inGameServer,
		bool isOffline, bool isLAN, bool isP2P = false
	) {
		inGame = (inGameServer != null);
		this.isOffline = isOffline;
		this.isLAN = isLAN;
		this.isP2P = isP2P;
		if (isLAN) hidden = false;

		prevMapSizeIndex = mapSizeIndex;

		if (inGameServer != null) {
			playerCount = inGameServer.players.Count;
			netcodeModel = (int)inGameServer.netcodeModel;
			netcodeModelUnderPing = inGameServer.netcodeModelPing;
		} else {
			playerCount = 5;
		}

		foreach (var kvp in Global.levelDatas) {
			var levelData = kvp.Value;
			if (levelData.isMirrored || levelData.name.EndsWith("_inverted")) continue;

			if (!levelData.isCustomMap) {
				if (levelData.isTraining()) {
					trainingMaps.Add(levelData);
				}
				else if (levelData.isCollosal()) {
					collosalMaps.Add(levelData);
				}
				else if (levelData.isSmall()) {
					smallMaps.Add(levelData);
				}
				else if (levelData.is1v1()) {
					foxOnlyMaps.Add(levelData);
				}
				else if (levelData.isMedium()) {
					mediumMaps.Add(levelData);
				}
				else {
					largeMaps.Add(levelData);
				}
				trainingMaps.Sort(mapSortFunc);
				foxOnlyMaps.Sort(mapSortFunc);
				smallMaps.Sort(mapSortFunc);
				mediumMaps.Sort(mapSortFunc);
				largeMaps.Sort(mapSortFunc);
				collosalMaps.Sort(mapSortFunc);
			} else {
				if (levelData.isTraining()) {
					trainingCustomMaps.Add(levelData);
				}
				else if (levelData.is1v1()) {
					foxOnlyCustomMaps.Add(levelData);
				}
				else if (levelData.isSmall()) {
					smallCustomMaps.Add(levelData);
				}
				else if (levelData.isMedium()) {
					mediumCustomMaps.Add(levelData);
				}
				else if (levelData.isCollosal()) {
					collosalCustomMaps.Add(levelData);
				}
				else {
					largeCustomMaps.Add(levelData);
				}
				trainingCustomMaps.Sort(mapSortFunc);
				foxOnlyCustomMaps.Sort(mapSortFunc);
				smallCustomMaps.Sort(mapSortFunc);
				mediumCustomMaps.Sort(mapSortFunc);
				largeCustomMaps.Sort(mapSortFunc);
				collosalCustomMaps.Sort(mapSortFunc);
			}
		}

		if (!string.IsNullOrEmpty(savedMatchSettings.hostMenuSettings.mapName)) {
			savedMatchSettings.hostMenuSettings.mapIndex = currentMapSizePool.FindIndex(
				m => m.name == savedMatchSettings.hostMenuSettings.mapName
			);
			if (savedMatchSettings.hostMenuSettings.mapIndex == -1) {
				savedMatchSettings.hostMenuSettings.mapIndex = 0;
			}
		} else if (savedMatchSettings.hostMenuSettings.mapIndex == null) {
			savedMatchSettings.hostMenuSettings.mapIndex = 0;
		}

		setMenuOptions();

		previous = mainMenu;
		if (inGame) {
			serverName = inGameServer.name;
			hidden = inGameServer.hidden;
			botCount = inGameServer.botCount;
		} else {
			serverName = getRandomServerName();
		}

		isMapSelected = true;
		if (inGame) {
			isMapSelected = false;
			LevelData currentLevelData = Global.level.server.getLevelData();

			if (trainingMaps.IndexOf(currentLevelData) >= 0) {
				mapSizeIndex = 0;
				mapIndex = trainingMaps.IndexOf(currentLevelData);
			}
			else if (foxOnlyMaps.IndexOf(currentLevelData) >= 0) {
				mapSizeIndex = 1;
				mapIndex = foxOnlyMaps.IndexOf(currentLevelData);
			}
			else if (smallMaps.IndexOf(currentLevelData) >= 0) {
				mapSizeIndex = 2;
				mapIndex = smallMaps.IndexOf(currentLevelData);
			}
			else if (mediumMaps.IndexOf(currentLevelData) >= 0) {
				mapSizeIndex = 3;
				mapIndex = mediumMaps.IndexOf(currentLevelData);
			}
			else if (largeMaps.IndexOf(currentLevelData) >= 0) {
				mapSizeIndex = 4;
				mapIndex = largeMaps.IndexOf(currentLevelData);
			}
			else if (collosalMaps.IndexOf(currentLevelData) >= 0) {
				mapSizeIndex = 5;
				mapIndex = collosalMaps.IndexOf(currentLevelData);
			}
		}

		/*if (isOffline && botCount == 0) {
			botCount = is1v1 ? 1 : 7;
		}*/
	}

	public void setMenuOptions() {
		menuOptions = new List<MenuOption>();
		// Match name
		if (!isOffline && !inGame) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuAlt)) {
							Menu.change(new EnterTextMenu("Enter a custom server name", 10, (string text) => {
								serverName = text;
								Menu.change(this);
							}));
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Match name: " + serverName, pos.x, pos.y,
							selected: index == selectArrowPosY
						);
					},
					"Change Name"
				)
			);
		}
		// Custom map pool
		menuOptions.Add(
			new MenuOption(startX, startY,
				() => {
					if (Global.input.isPressedMenu(Control.MenuLeft)) {
						if (isCustomMapPool) {
							isCustomMapPool = false;
							mapIndex = 0;
							onMapChange();
						}
					} else if (Global.input.isPressedMenu(Control.MenuRight)) {
						if (!isCustomMapPool) {
							isCustomMapPool = true;
							mapIndex = 0;
							onMapChange();
						}
					}
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue, "Map pool: " + (isCustomMapPool ? "Custom" : "Official"),
						pos.x, pos.y, selected: index == selectArrowPosY
					);
				}
			)
		);
		// Map size
		menuOptions.Add(
			new MenuOption(startX, startY,
				() => {
					if (Global.input.isPressedMenu(Control.MenuLeft) && mapSizeIndex > 0) {
						onMapSizeChange(mapSizeIndex, --mapSizeIndex);
					} else if (Global.input.isPressedMenu(Control.MenuRight) && mapSizeIndex < mapSizes.Length - 1) {
						onMapSizeChange(mapSizeIndex, ++mapSizeIndex);
					}
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue, "Map size: " + selectedMapSize, pos.x, pos.y,
						selected: index == selectArrowPosY
					);
				}
			)
		);
		// Map
		menuOptions.Add(
			new MenuOption(startX, startY,
				() => {
					if (currentMapSizePool.Count == 0) {
						return;
					}
					if (Global.input.isPressedMenu(Control.MenuLeft)) {
						mapIndex--;
						if (mapIndex < 0) mapIndex = currentMapSizePool.Count - 1;
						onMapChange();
					} else if (Global.input.isPressedMenu(Control.MenuRight)) {
						mapIndex++;
						if (mapIndex >= currentMapSizePool.Count) mapIndex = 0;
						onMapChange();
					} else if (Global.input.isPressedMenu(Control.MenuAlt)) {
						mapIndex = Helpers.randomRange(0, currentMapSizePool.Count - 1);
						onMapChange();
					}
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue, "Map: " + (isMapSelected ? selectedLevel.displayName : "[Select]"),
						pos.x, pos.y, selected: index == selectArrowPosY
					);
				},
				"Random Map"
			)
		);
		if (isTraining) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							useLoadout = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							useLoadout = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Use loadout: " + Helpers.boolYesNo(useLoadout),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Mode
		if (!isTraining) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft) && gameModeIndex > 0) {
							gameModeIndex--;
							onModeChange();
						} else if (
							Global.input.isPressedMenu(Control.MenuRight) &&
							gameModeIndex < currentGameModePool.Count - 1
						) {
							gameModeIndex++;
							onModeChange();
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Mode: " + selectedGameMode, pos.x, pos.y,
							selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Team number.
		if (GameMode.isStringTeamMode(selectedGameMode)) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (Global.input.isPressedOrHeldMenu(Control.MenuLeft) && teamNum > 2) {
							teamNum--;
						} else if (Global.input.isPressedOrHeldMenu(Control.MenuRight) && teamNum < 6) {
							teamNum++;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Teams: " + teamNum,
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Team
		if (changeTeamEnabled()) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (changeTeamEnabled()) {
							if (Global.input.isPressedOrHeldMenu(Control.MenuLeft) && team > 0) {
								team--;
							} else if (Global.input.isPressedOrHeldMenu(Control.MenuRight) && team < teamNum) {
								team++;
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Team: " + GameMode.getTeamName(team),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		if (playToSupported()) {
			// Play to
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (playToSupported()) {
							if (Global.input.isPressedOrHeldMenu(Control.MenuLeft) && playTo > 1) {
								playTo--;
								playToDirty = true;
							} else if (Global.input.isPressedOrHeldMenu(Control.MenuRight) && playTo < 100) {
								playTo++;
								playToDirty = true;
							}
						}
					},
					(Point pos, int index) => {
						string playToStr = "Play to: ";
						if (selectedGameMode == GameMode.Elimination ||
							selectedGameMode == GameMode.TeamElimination
						) {
							playToStr = "Lives: ";
						}
						Fonts.drawText(
							FontType.Blue,
							playToStr + playTo, pos.x, pos.y,
							selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// CPU Count
		menuOptions.Add(
			new MenuOption(startX, startY,
				() => {
					if (Global.input.isPressedOrHeldMenu(Control.MenuLeft) && botCount > 0) {
						botCount--;
						resetCpuDataTeams();
					} else if (
						Global.input.isPressedOrHeldMenu(Control.MenuRight) &&
						botCount < Math.Max(Server.maxPlayerCap - 1, 3)
					) {
						botCount++;
						resetCpuDataTeams();
					}
					if (Global.input.isPressedMenu(Control.MenuAlt) && botCount > 0) {
						Menu.change(
							new ConfigureCPUMenu(
								this, botCount, is1v1, isOffline, inGame, inGame,
								GameMode.isStringTeamMode(selectedGameMode), true
							)
						);
					}
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue, "CPU count: " + botCount, pos.x, pos.y,
						selected: index == selectArrowPosY
					);
				},
				"Configure CPUs"
			)
		);
		// Time limit
		if (!isTraining && !isRace) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (Global.input.isPressedOrHeldMenu(Control.MenuLeft)) {
							timeLimitDirty = true;
							timeLimit--;
							int minimumTimeLimit = 0;
							if (selectedGameMode == GameMode.ControlPoint || selectedGameMode == GameMode.KingOfTheHill || selectedGameMode == GameMode.Elimination || selectedGameMode == GameMode.TeamElimination) {
								if (!is1v1) {
									minimumTimeLimit = 1;
								}
							}
							if (timeLimit < minimumTimeLimit) timeLimit = minimumTimeLimit;
						} else if (Global.input.isPressedOrHeldMenu(Control.MenuRight)) {
							timeLimitDirty = true;
							timeLimit++;
							if (timeLimit > 30) timeLimit = 30;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue,
							"Time limit: " + (timeLimit > 0 ? timeLimit.ToString() + " minutes" : "None"),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Mirrored
		if (selectedLevel.canChangeMirror(selectedGameMode)) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (selectedLevel.canChangeMirror(selectedGameMode)) {
							if (Global.input.isPressedMenu(Control.MenuLeft)) {
								mirrored = false;
							} else if (Global.input.isPressedMenu(Control.MenuRight)) {
								mirrored = true;
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Mirrored: " + Helpers.boolYesNo(mirrored),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Large camera
		if (selectedLevel.supportsLargeCam) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (selectedLevel.supportsLargeCam) {
							if (Global.input.isPressedMenu(Control.MenuLeft)) {
								fixedCamera = false;
							} else if (Global.input.isPressedMenu(Control.MenuRight)) {
								fixedCamera = true;
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Large camera: " + (fixedCamera ? "Yes" : "No"),
							pos.x, pos.y,
							selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Disable HT/ST
		if (!is1v1) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							disableHtSt = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							disableHtSt = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Heart/Sub tanks: " + (disableHtSt ? "Disabled" : "Enabled"),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Disable vehicles
		if (selectedLevel.supportsVehicles) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (Global.input.isPressedMenu(Control.MenuLeft)) {
							disableVehicles = false;
						} else if (Global.input.isPressedMenu(Control.MenuRight)) {
							disableVehicles = true;
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Vehicles: " + (disableVehicles ? "Disabled" : "Enabled"),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Private match
		if (!isOffline && !isLAN) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (!isOffline) {
							if (Global.input.isPressedMenu(Control.MenuLeft)) {
								hidden = false;
								setMenuOptions();
							} else if (Global.input.isPressedMenu(Control.MenuRight)) {
								hidden = true;
								setMenuOptions();
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Private match: " + (hidden ? "Yes" : "No"),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Netcode model
		if (!isOffline) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (!isOffline) {
							if (Global.input.isPressedMenu(Control.MenuLeft)) {
								netcodeModel--;
								if (netcodeModel < 0) netcodeModel = 0;
								else {
									setMenuOptions();
								}
							} else if (Global.input.isPressedMenu(Control.MenuRight)) {
								netcodeModel++;
								if (netcodeModel > 1) netcodeModel = 1;
								else {
									setMenuOptions();
								}
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Netcode: " + Helpers.getNetcodeModelString((NetcodeModel)netcodeModel),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Netcode model ping
		if (netcodePingEnabled()) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						if (netcodePingEnabled()) {
							if (Global.input.isPressedOrHeldMenu(Control.MenuLeft)) {
								netcodeModelUnderPing -= 10;
								if (netcodeModelUnderPing < 10) netcodeModelUnderPing = 10;
							} else if (Global.input.isPressedOrHeldMenu(Control.MenuRight)) {
								netcodeModelUnderPing += 10;
								if (netcodeModelUnderPing > 500) netcodeModelUnderPing = 500;
							}
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Favor below ping: " + netcodeModelUnderPing.ToString(),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					}
				)
			);
		}
		// Custom match settings
		if (isOffline || isP2P || isHiddenOrLan()) {
			menuOptions.Add(
				new MenuOption(startX, startY,
					() => {
						Helpers.menuLeftRightBool(ref savedMatchSettings.hostMenuSettings.useCustomMatchSettings);
						if (Global.input.isPressedMenu(Control.MenuAlt)) {
							Menu.change(new CustomMatchSettingsMenu(this, inGame, isOffline));
						}
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue, "Custom settings: " + Helpers.boolYesNo(useCustomMatchSettings),
							pos.x, pos.y, selected: index == selectArrowPosY
						);
					},
					"Configure Custom Settings"
				)
			);
		}
		for (int i = 0; i < menuOptions.Count; i++) {
			menuOptions[i].pos.y = startY + lineH * i;
		}
	}

	public string[] mapSortOrder = new string[] {
		// Medium maps.
		"factory_md", "airport_md", "maverickfactory_md", "desertbase_md", "weathercontrol_md",
		// Small maps.
		 "training",
		"centralcomputer_1v1", "zerovirus_1v1", "sigma4_1v1", "dopplerlab_1v1", "sigma1_1v1",
		"airport_1v1", "factory_1v1", "hunterbase_1v1", "forest_1v1", "highway", "highway2",
		"bossroom", "powerplant", "factory", "gallery", "tower", "mountain", "ocean", "forest",
		"forest2", "airport", "sigma", "sigma2", "japetribute_1v1",
		// Large maps.
		"maverickfactory", "weathercontrol", "dinosaurtank", "deepseabase", "volcaniczone",
		"robotjunkyard", "desertbase", "desertbase2", "crystalmine", "centralcomputer",
		"xhunter1", "xhunter2", "hunterbase", "highway2", "giantdam", "giantdam2",
		"weaponsfactory", "frozentown", "aircraftcarrier", "powercenter", "shipyard",
		"quarry", "safaripark", "dopplerlab", "hunterbase2", "nodetest",
	};
	public int mapSortFunc(LevelData a, LevelData b) {
		int aIndex = -1;
		int bIndex = -1;
		for (int i = 0; i < mapSortOrder.Length; i++) {
			if (a.name.StartsWith(mapSortOrder[i], StringComparison.OrdinalIgnoreCase)) {
				aIndex = i;
				break;
			}
		}
		for (int i = 0; i < mapSortOrder.Length; i++) {
			if (b.name.StartsWith(mapSortOrder[i], StringComparison.OrdinalIgnoreCase)) {
				bIndex = i;
				break;
			}
		}
		int compareTo = aIndex.CompareTo(bIndex);
		if (compareTo == 0) {
			return a.name.CompareTo(b.name);
		}
		return compareTo;
	}

	public string getRandomServerName() {
		return "match" + Helpers.randomRange(1, 999).ToString();
	}
	public void update() {
		if (Global.leaveMatchSignal != null) return;

		if (inGame) {
			botCount = Helpers.clamp(botCount, 0, 10 - Global.level.players.Count(p => !p.isBot));
		}

		if (string.IsNullOrEmpty(errorMessage)) {
			Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);
		}

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (!string.IsNullOrEmpty(errorMessage)) {
				errorMessage = null;
				return;
			}

			if (inGame && !isMapSelected) {
				errorMessage = "Please select a map.";
				return;
			}
			if (selectedLevel.isTraining() && !isOffline && !isHiddenOrLan()) {
				errorMessage = "Can't select training in public matches.";
				return;
			}
			/*if (!selectedLevel.isCustomMap && !isOffline && !isHiddenOrLan() && selectedGameMode == GameMode.Race) {
				errorMessage = "Race only in private match or custom maps.";
				return;
			}*/

			if (isLAN) {
				localIPAddress = LANIPHelper.GetLocalIPAddress();
				if (string.IsNullOrEmpty(localIPAddress)) {
					errorMessage = "Couldn't get LAN IP address.";
					return;
				}
			}

			if (inGame && !Global.level.is1v1()) {
				completeAction();
			} else {
				Menu.change(new SelectCharacterMenu(this, is1v1, isOffline, inGame, inGame, GameMode.isStringTeamMode(selectedGameMode), true, completeAction));
			}

			return;
		}

		if (string.IsNullOrEmpty(errorMessage)) {
			menuOptions[selectArrowPosY].update();
			/*
			if (Global.input.isPressedMenu(Control.MenuBack) && !inGame) {
				Global.serverClient = null;
				Menu.change(previous);
			} */
			if (Options.main.blackFade) {
				TimeUpdate();
				if (Time2 >= 1 && !inGame) {
					Menu.change(previous);
					Global.serverClient = null;
					previous.Time = 0;
					previous.Time2 = 1;
					previous.Confirm = false;
					previous.Confirm2 = false;
				}
			} else {
				if (Global.input.isPressedMenu(Control.MenuBack) && !inGame) {
					Menu.change(previous);
					Global.serverClient = null;
				}
			}
			/*
			else if (Global.input.isPressedMenu(Control.MenuEnter) && inGame)
			{
				Menu.change(new ConfirmLeaveMenu(this, "Are you sure you want to leave?", () =>
				{
					Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.MatchOver, null, null, null);
				}));
			}
			*/
		}
	}

	public CustomMatchSettings getCustomMatchSettings() {
		if (useCustomMatchSettings) {
			return savedMatchSettings.customMatchSettings;
		}
		return null;
	}

	public void completeAction() {
		if (inGame) {
			var oldServer = Global.level.server;
			var server = new Server(
				oldServer.gameVersion, oldServer.region,
				serverName, selectedLevel.name,
				selectedLevel.shortName, selectedGameMode,
				playTo, botCount, oldServer.maxPlayers,
				timeLimit, fixedCamera, hidden,
				(NetcodeModel)netcodeModel, netcodeModelUnderPing,
				isLAN, mirrored, useLoadout, Global.checksum,
				selectedLevel.checksum, selectedLevel.customMapUrl,
				savedMatchSettings.extraCpuCharData, getCustomMatchSettings(),
				disableHtSt, disableVehicles, teamNum
			);
			server.uniqueID = oldServer.uniqueID;
			server.isP2P = oldServer.isP2P;
			if (server.isP2P) {
				Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.RecreateMS, server, null);
			} else {
				Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.Recreate, server, null);
			}
		} else if (isP2P) {
			createP2PMatch();
		} else if (!isOffline) {
			Region region;
			if (isLAN) {
				region = new Region("LAN", localIPAddress);
			} else {
				region = Options.main.getRegion();
			}
			// TODO: Unhardcode max players.
			var serverData = new Server(
				Global.version, region, serverName,
				selectedLevel.name, selectedLevel.shortName,
				selectedGameMode, playTo, botCount, Server.maxPlayerCap,
				timeLimit, fixedCamera, hidden, (NetcodeModel)netcodeModel,
				netcodeModelUnderPing, isLAN, mirrored, useLoadout,
				Global.checksum, selectedLevel.checksum, selectedLevel.customMapUrl,
				savedMatchSettings.extraCpuCharData, getCustomMatchSettings(),
				disableHtSt, disableVehicles, teamNum
			);
			createServer(
				SelectCharacterMenu.playerData.charNum, serverData, team, false, previous, out errorMessage
			);
		} else {
			//Global.playSound("menuConfirm");
			createOfflineMatch();
		}

		if (!string.IsNullOrEmpty(errorMessage)) {
			Menu.change(new ErrorMenu(errorMessage, this, inGame));
			errorMessage = null;
		}
	}

	public static void createServer(
		int charNum, Server serverData,
		int? hostTeam, bool isRecreate,
		IMainMenu menu, out string errorMessage
	) {
		serverData.name = serverData.name.Trim();
		errorMessage = null;
		if (string.IsNullOrWhiteSpace(serverData.name)) {
			errorMessage = "Error: Empty match name.";
			return;
		}

		var playerName = Options.main.playerName;

		var response = Global.matchmakingQuerier.createServer(serverData);
		if (response.server != null) {
			var inputServerPlayer = new ServerPlayer(
				playerName, -1, true, charNum, hostTeam, Global.deviceId, null, serverData.region.getPing()
				);
			Global.serverClient = ServerClient.Create(
				serverData.region.ip, serverData.name, response.server.port, inputServerPlayer, out JoinServerResponse joinServerResponse, out string error
			);
			if (Global.serverClient == null) {
				Menu.change(new ErrorMenu(new string[] { error, "Please try recreating match." }, new HostMenu(new MainMenu(), null, false, serverData.isLAN)));
				return;
			}
			Menu.change(new WaitMenu(menu, joinServerResponse.server, isRecreate));
		} else {
			errorMessage = response.failReason;
			if (string.IsNullOrEmpty(errorMessage) && serverData.region.name == "LAN") {
				errorMessage = "Could not connect to LAN Relay Server.";
			}
		}
	}

	public void createOfflineMatch() {
		var me = new ServerPlayer(
			Options.main.playerName, 0, true,
			SelectCharacterMenu.playerData.charNum, team, Global.deviceId, null, 0
		);
		if (GameMode.isStringTeamMode(selectedGameMode)) me.alliance = team;

		string gameMode = selectedGameMode;
		if (selectedLevel.isTraining()) {
			playTo = 9999;
			gameMode = GameMode.Deathmatch;
		}

		var localServer = new Server(
			Global.version, null, null, selectedLevel.name, selectedLevel.shortName,
			gameMode, playTo, botCount, selectedLevel.maxPlayers, timeLimit, fixedCamera,
			false, (NetcodeModel)netcodeModel, netcodeModelUnderPing,
			isLAN, mirrored, useLoadout, Global.checksum, selectedLevel.checksum,
			selectedLevel.customMapUrl, savedMatchSettings.extraCpuCharData, getCustomMatchSettings(),
			disableHtSt, disableVehicles, teamNum
		);
		localServer.players = new List<ServerPlayer>() { me };

		Global.level = new Level(
			localServer.getLevelData(), SelectCharacterMenu.playerData, localServer.extraCpuCharData, false
		);
		Global.level.teamNum = teamNum;
		Global.level.startLevel(localServer, false);
	}

	public void createP2PMatch() {
		serverName = serverName.Trim();
		if (string.IsNullOrWhiteSpace(serverName)) {
			errorMessage = "Error: Empty match name.";
			return;
		}
		string gameMode = selectedGameMode;
		if (selectedLevel.isTraining()) {
			playTo = 9999;
			gameMode = GameMode.Deathmatch;
		}
		if (Global.localServer != null && Global.localServer.s_server.Status == NetPeerStatus.Running) {
			Global.localServer.shutdown("Host left the match.");
			for (int i = 0; i < 10; i++) {
				if (Global.localServer.s_server.Status != NetPeerStatus.NotRunning) {
					Thread.Sleep(20);
				} else {
					break;
				}
			}
		}
		var localServer = new Server(
			Global.version, null, serverName, selectedLevel.name,
			selectedLevel.shortName, gameMode,
			playTo, botCount, selectedLevel.maxPlayers,
			timeLimit, fixedCamera, false,
			(NetcodeModel)netcodeModel, netcodeModelUnderPing,
			false, mirrored, useLoadout,
			Global.checksum, selectedLevel.checksum,
			selectedLevel.customMapUrl, savedMatchSettings.extraCpuCharData,
			getCustomMatchSettings(), disableHtSt, disableVehicles, teamNum
		);
		localServer.isP2P = true;
		Global.localServer = localServer;
		localServer.start();
		Thread.Sleep(100);
		int waitLoops = 0;
		while ((localServer.s_server?.Status != NetPeerStatus.Running || localServer.uniqueID == 0) &&
			waitLoops <= 10
		) {
			Thread.Sleep(100);
		}
		var me = new ServerPlayer(
			Options.main.playerName, 0, true,
			SelectCharacterMenu.playerData.charNum, team, Global.deviceId, null, 0
		);
		if (GameMode.isStringTeamMode(selectedGameMode)) {
			me.alliance = team;
		}
		System.Threading.Thread.Sleep(50);
		/*
		Global.serverClient = ServerClient.CreateDirect(
			"127.0.0.1", 65535, me,
			out JoinServerResponse joinServerResponse, out string error
		);
		*/
		Global.serverClient = ServerClient.CreateDirect(
			"127.0.0.1", localServer.port, me,
			out JoinServerResponse? joinServerResponse, out string error
		);

		if (joinServerResponse != null && error == "") {
			Menu.change(new WaitMenu(new MainMenu(), localServer, false));
		} else {
			errorMessage = error;
			if (errorMessage == "") {
				errorMessage = "Could not connect to self.";
			}
		}
		Program.setLastUpdateTimeAsNow();
	}

	public static void reCreateP2PMatch(
		int charNum, Server localServer,
		IMainMenu menu, out string errorMessage
	) {
		errorMessage = "";

		if (Global.serverClient != null) {
			Global.serverClient.disconnect("RecreateMS");
		}
		if (Global.localServer.s_server.Status == NetPeerStatus.Running) {
			Global.localServer.shutdown("RecreateMS");
			for (int i = 0; i < 10; i++) {
				if (Global.localServer.s_server.Status != NetPeerStatus.NotRunning) {
					Thread.Sleep(20);
				} else {
					break;
				}
			}
		}
		localServer.isP2P = true;
		localServer.uniqueID = Global.localServer.uniqueID; // Reuse the old ID.
		Global.localServer = localServer;
		localServer.start();
		Thread.Sleep(100);
		int waitLoops = 0;
		while (localServer.s_server?.Status != NetPeerStatus.Running && waitLoops <= 10) {
			Thread.Sleep(100);
		}
		var me = new ServerPlayer(
			Options.main.playerName, 0, true,
			SelectCharacterMenu.playerData.charNum, null, Global.deviceId, null, 0
		);
		Global.serverClient = ServerClient.CreateDirect(
			"127.0.0.1", localServer.port, me,
			out JoinServerResponse? joinServerResponse, out string error
		);
		if (joinServerResponse != null && error == "") {
			Menu.change(new WaitMenu(new MainMenu(), localServer, false));
		} else {
			errorMessage = error;
			if (string.IsNullOrEmpty(errorMessage)) {
				errorMessage = "Could not connect to self.";
			}
		}
		Program.setLastUpdateTimeAsNow();
	}

	public void onMapSizeChange(int prevMapSizeIndex, int newMapSizeIndex) {
		playToDirty = false;
		timeLimitDirty = false;
		mapIndex = 0;
		gameModeIndex = 0;
		if (prevMapSizeIndex == 1 && newMapSizeIndex != 1) {
			removeMaverickCpuDatas();
		}
		if (!isOffline && is1v1) {
			botCount = 0;
		}
		onMapChange();
	}

	public void onMapChange() {
		isMapSelected = true;
		string oldSelectedGameMode = selectedGameMode;
		gameModeIndex = currentGameModePool.IndexOf(selectedGameMode);
		if (gameModeIndex == -1) gameModeIndex = 0;
		if (selectedGameMode != oldSelectedGameMode) {
			onModeChange();
		}

		if (selectedLevel.isTraining()) {
			timeLimit = 0;
			removeMaverickCpuDatas();
		}

		if (!selectedLevel.supportsLargeCam) {
			fixedCamera = false;
		} /*else if (selectedLevel.defaultLargeCam) {
			fixedCamera = true;
		}*/ else {
			fixedCamera = false;
		}

		if (isOffline) {
			if (selectedLevel.is1v1() || selectedLevel.isTraining()) {
				botCount = 1;
			} else if (selectedLevel.isMedium()) {
				botCount = 3;
			} else {
				botCount = 9;
			}
		}

		setPlayToAndTimeLimitBasedOnGameMode();
		setMirroredBasedOnMapAndGameMode();
		setMirroredBasedOnMap();
		setMenuOptions();
	}

	private void removeMaverickCpuDatas() {
		foreach (var cpuData in savedMatchSettings.extraCpuCharData.cpuDatas) {
			if (cpuData.charNum >= 210) cpuData.charNum = 4;
		}
	}

	private void resetCpuDataTeams() {
		foreach (var cpuData in savedMatchSettings.extraCpuCharData.cpuDatas) {
			cpuData.alliance = -1;
		}
	}

	public void onModeChange() {
		playToDirty = false;
		timeLimitDirty = false;

		setPlayToAndTimeLimitBasedOnGameMode();
		setMirroredBasedOnMapAndGameMode();
		setMirroredBasedOnMap();
		setMenuOptions();
	}

	public void setPlayToAndTimeLimitBasedOnGameMode() {
		if (selectedGameMode == GameMode.Deathmatch) {
			if (!playToDirty) {
				if (playerCount <= 2) playTo = 10;
				else if (playerCount == 3) playTo = 15;
				else if (playerCount == 4) playTo = 20;
				else if (playerCount == 5) playTo = 25;
				else if (playerCount == 6) playTo = 25;
				else if (playerCount == 7) playTo = 30;
				else if (playerCount == 8) playTo = 30;
				else if (playerCount == 9) playTo = 40;
				else if (playerCount >= 10) playTo = 40;
			}
			timeLimit = 0;
		} else if (selectedGameMode == GameMode.TeamDeathmatch) {
			if (!playToDirty) {
				if (playerCount <= 3) playTo = 15;
				else if (playerCount == 4) playTo = 25;
				else if (playerCount == 5) playTo = 30;
				else if (playerCount == 6) playTo = 50;
				else if (playerCount == 7) playTo = 50;
				else if (playerCount == 8) playTo = 75;
				else if (playerCount == 9) playTo = 75;
				else if (playerCount >= 10) playTo = 75;
			}
			timeLimit = 0;
		} else if (selectedGameMode == GameMode.Elimination) {
			if (!playToDirty) {
				if (is1v1) playTo = 1;
				else playTo = 3;
			}
			timeLimit = 7;
		} else if (selectedGameMode == GameMode.TeamElimination) {
			if (!playToDirty) {
				if (is1v1) playTo = 1;
				else playTo = 3;
			}
			timeLimit = 7;
		} else if (selectedGameMode == GameMode.CTF) {
			if (!playToDirty) {
				playTo = 3;
			}
			timeLimit = 10;
		} else if (selectedGameMode == GameMode.ControlPoint) {
			timeLimit = 7;
		} else if (selectedGameMode == GameMode.KingOfTheHill) {
			timeLimit = 5;
		}
	}

	public void setMirroredBasedOnMapAndGameMode() {
		if (selectedGameMode == GameMode.CTF || selectedGameMode == GameMode.KingOfTheHill) {
			if (selectedLevel.supportedGameModesToMirrorSupport[selectedGameMode].mirrored) {
				mirrored = true;
			} else {
				mirrored = false;
			}
		} else {
			if (selectedLevel.supportedGameModesToMirrorSupport[selectedGameMode].nonMirrored) {
				mirrored = false;
			} else {
				mirrored = true;
			}
		}
	}

	public void setMirroredBasedOnMap() {
		if (selectedLevel.mirroredOnly) {
			mirrored = true;
		} else if (!selectedLevel.supportsMirrored) {
			mirrored = false;
		}
	}

	public void setBotCounts() {
		//if (isOffline) botCount = Server.maxPlayerCap - 1;
	}

	public bool changeTeamEnabled() {
		return isOffline && GameMode.isStringTeamMode(selectedGameMode) && !inGame;
	}

	bool netcodePingEnabled() {
		return !isOffline && netcodeModel == (int)NetcodeModel.FavorAttacker;
	}

	bool playToSupported() {
		return selectedGameMode != GameMode.ControlPoint && selectedGameMode != GameMode.KingOfTheHill && !isTraining && !isRace;
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
			DrawWrappers.DrawTextureHUD(
				Global.textures["cursor"],
				menuOptions[selectArrowPosY].pos.x,
				menuOptions[selectArrowPosY].pos.y - 2
			);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			Global.sprites["cursor"].drawToHUD(
				0, menuOptions[selectArrowPosY].pos.x,
				menuOptions[selectArrowPosY].pos.y - 2
			);
		}
		DrawWrappers.DrawTextureHUD(selectedLevel.getMapThumbnail(), 254, 38);

		string titleText = "";
		if (!inGame) {
			if (isOffline) titleText = "VS. CPU";
			else {
				if (isLAN) titleText = "HOST LAN";
				else titleText = "CREATE MATCH";
			}
		} else {
			titleText = "Create Mext Match";
		}

		Fonts.drawText(FontType.Yellow, titleText, Global.halfScreenW, 20, Alignment.Center);

		int i = 0;
		foreach (MenuOption menuOption in menuOptions) {
			menuOption.render(menuOption.pos.addxy(8, 0), i++);
		}

		string msg;
		string extraMsg = "";
		if (!string.IsNullOrEmpty(menuOptions[selectArrowPosY].configureMessage)) {
			extraMsg = ", [ALT]: " + menuOptions[selectArrowPosY].configureMessage;
		}
		msg = "[OK]: Next, [BACK]: Back" + extraMsg;
		Fonts.drawTextEX(
			FontType.Grey, msg + "\nLeft/Right: Change setting",
			Global.screenW * 0.5f, 178, Alignment.Center
		);

		if (errorMessage != null) {
			float top = Global.screenH * 0.4f;

			//DrawWrappers.DrawRect(5, top - 20, Global.screenW - 5, top + 60, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
			DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
			Fonts.drawText(
				FontType.Red, errorMessage, Global.screenW / 2, top,
				alignment: Alignment.Center
			);
			Fonts.drawTextEX(
				FontType.Red, "Press [OK] to continue",
				Global.screenW * 0.5f, 20 + top, alignment: Alignment.Center
			);
		}
		if (Options.main.blackFade) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time);
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time2);
		}	

	}
	public void TimeUpdate() {
		if (!inGame) {
			if (Confirm == false) Time -= Global.spf * 2;
			if (Time <= 0) {
				Confirm = true;
				Time = 0;
			}
			if (Global.input.isPressedMenu(Control.MenuBack)) Confirm2 = true;
			if (Confirm2 == true) Time2 += Global.spf * 2;
		}
		if (inGame) {
			Time = 0;
			Time2 = 0;
		}
	}
}
