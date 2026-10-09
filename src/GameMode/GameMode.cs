﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using SFML.Graphics;
using SFML.System;

namespace MMXOnline;

public class GameMode {
	public const string Deathmatch = "Deathmatch";
	public const string TeamDeathmatch = "Team Deathmatch";
	public const string CTF = "Capture The Flag";
	public const string ControlPoint = "Control Point";
	public const string Elimination = "Elimination";
	public const string TeamElimination = "Team Elimination";
	public const string KingOfTheHill = "King Of The Hill";
	public const string Race = "Race";
	public const string ElimAlt = "Elimination Alt";
	public const string TeamElimAlt = "Team Elimination Alt";

	public static List<string> allGameModes = new List<string>() {
		Deathmatch, TeamDeathmatch, CTF, KingOfTheHill,
		ControlPoint, Elimination, TeamElimination, ElimAlt, TeamElimAlt
	};

	public const int blueAlliance = 0;
	public const int redAlliance = 1;
	public const int neutralAlliance = 50;
	public const int stageAlliance = 52;
	public const int stageEnemyAlliance = 53;
	public const int nullAlliance = 54;
	public const int freelanceAlliance = 60;

	public bool isTeamMode = false;
	public bool isElim = false;
	public float overTime = 0;
	public float secondsBeforeLeave = 7;
	public float? setupTime;
	public float? remainingTime;
	public float? startTimeLimit;
	public int playingTo;
	public bool drawingScoreboard;
	public float drawTime;
	public float drawMaxTime = 60;

	public bool noContest;

	public byte[] teamPoints = new byte[6];
	public byte[] teamAltPoints = new byte[6];
	public string[] teamNames = {
		"Blue",
		"Red",
		"Green",
		"Purple",
		"Yellow",
		"Orange"
	};
	public string[] teamInitals = {
		"B",
		"R",
		"G",
		"P",
		"Y",
		"O"
	};
	public FontType[] teamFontsSmall = {
		FontType.Blue,
		FontType.Red,
		FontType.Green,
		FontType.Purple,
		FontType.Yellow,
		FontType.Orange
	};

	public FontType[] teamFonts = {
		FontType.Blue,
		FontType.Red,
		FontType.Green,
		FontType.Purple,
		FontType.Yellow,
		FontType.Orange
	};

	public Color15b[] teamColors = {
		new Color15b(16, 25, 31),
		new Color15b(31, 14, 14), 
		new Color15b(15, 31, 15),
		new Color15b(25, 19, 31),
		new Color15b(31, 28, 0),
		new Color15b(31, 18, 0),
	};

	public VoteKick? currentVoteKick;
	public float voteKickCooldown;

	public string dpsString = "";
	public Level level;
	public float eliminationTime;
	public float localElimTimeInc;  // Used to "client side predict" the elimination time increase.
	public byte virusStarted;
	public float? finalZoneTime;
	public float finalZoneMaxTime1 = 60;
	public float finalZoneMaxTime2 = 300;
	public byte safeZoneSpawnIndex;

	bool loggedStatsOnce = true;
	float goTime;

	public Player mainPlayer { get { return level.mainPlayer; } }

	public RPCMatchOverResponse? matchOverResponse;
	public bool isOver { get { return matchOverResponse != null; } }

	public int lastTimeInt;
	public int lastSetupTimeInt;
	public float periodicHostSyncTime;
	public float syncValueTime;

	bool changedEndMenuOnce;
	bool changedEndMenuOnceHost;

	public ChatMenu chatMenu;

	public List<KillFeedEntry> killFeed = new List<KillFeedEntry>();
	public List<string> killFeedHistory = new List<string>();

	bool removedGates;
	public HostMenu? nextMatchHostMenu;

	float flashTime;
	float flashCooldown;

	public float hudErrorMsgTime;
	string hudErrorMsg = "";
	string hudDebugMsg = "";
	FontType? hudErrorOverrideFont;

	public Player? hudTopLeftPlayer;
	public Player? hudTopRightPlayer;
	public Player? hudLeftPlayer;
	public Player? hudRightPlayer;
	public Player? hudBotLeftPlayer;
	public Player? hudBotRightPlayer;

	public bool spawnOnAlly;
	public bool useTeamSpawns;
	public const int grayAmmoIndex = 30;

	bool hudPositionsAssigned;
	int currentLineH;

	public List<(Point pos, string sprite, int index)> navPoints = new();

	public enum HUDHealthPosition {
		Left,
		Right,
	}

	public Point safeZonePoint {
		get {
			return level.spawnPoints[safeZoneSpawnIndex].pos;
		}
	}
	public Rect safeZoneRect {
		get {
			if (virusStarted == 0) {
				return new Rect(0, 0, level.width, level.height);
			} else if (virusStarted == 1) {
				float t = finalZoneTime ?? 0;
				if (t < 0) t = 0;
				float timePct = 1 - (t / finalZoneMaxTime1);
				return new Rect(
					timePct * (safeZonePoint.x - 192),
					timePct * (safeZonePoint.y - 128),
					level.width - (timePct * (level.width - (safeZonePoint.x + 192))),
					level.height - (timePct * (level.height - (safeZonePoint.y + 128)))
				);
			}
			return new Rect(
				safeZonePoint.x - 192,
				safeZonePoint.y - 128,
				safeZonePoint.x + 192,
				safeZonePoint.y + 128
			);
		}
	}

	public static bool isStringTeamMode(string selectedGameMode) {
		if (selectedGameMode == CTF ||
			selectedGameMode == TeamDeathmatch ||
			selectedGameMode == ControlPoint ||
			selectedGameMode == TeamElimination ||
			selectedGameMode == TeamElimAlt ||
			selectedGameMode == KingOfTheHill ||
			selectedGameMode.StartsWith("tm_")
		) {
			return true;
		}
		return false;
	}

	public static string abbreviatedMode(string mode) {
		if (mode == TeamDeathmatch) return "tdm";
		else if (mode == CTF) return "ctf";
		else if (mode == ControlPoint) return "cp";
		else if (mode == Elimination) return "elim";
		else if (mode == TeamElimination) return "t.elim";
		else if (mode == KingOfTheHill) return "koth";
		else if (mode == Race) return "race";
		else if (mode == ElimAlt) return "a.elim";
		else if (mode == TeamElimAlt) return "ta.elim";
		else return "dm";
	}

	public static int[] getAllianceCounts(List<Player> players, int teamNum) {
		int[] teamSizes = new int[teamNum];
		foreach (Player player in players) {
			if (!player.isSpectator && player.alliance >= 0 && player.alliance < teamNum) {
				teamSizes[player.alliance]++;
			}
		}
		return teamSizes;
	}

	public static int[] getAllianceCounts(List<ServerPlayer> players, int teamNum) {
		int[] teamSizes = new int[teamNum];
		foreach (ServerPlayer serverPlayer in players) {
			if (!serverPlayer.isSpectator && serverPlayer.alliance >= 0 && serverPlayer.alliance < teamNum) {
				teamSizes[serverPlayer.alliance]++;
			}
		}
		return teamSizes;
	}

	public GameMode(Level level, int? timeLimit) {
		this.level = level;
		if (timeLimit != null) {
			remainingTime = timeLimit.Value * 60;
			startTimeLimit = remainingTime;
		}
		chatMenu = new ChatMenu();
	}

	static List<ChatEntry> getTestChatHistory() {
		var test = new List<ChatEntry>();
		for (int i = 0; i < 30; i++) {
			test.Add(new ChatEntry("chat entry " + i.ToString(), "gm19", null, false));
		}
		return test;
	}

	public void removeAllGates() {
		if (!removedGates) removedGates = true;
		else return;

		for (int i = Global.level.gates.Count - 1; i >= 0; i--) {
			Global.level.removeGameObject(Global.level.gates[i]);
			Global.level.gates.RemoveAt(i);
		}
		if (Global.level.isRace()) {
			foreach (var player in Global.level.players) {
				if (player.character != null && player.character.ownedByLocalPlayer) {
					player.character.invulnTime = 60;
				}
			}
		}
	}

	public virtual void update() {
		Helpers.decrementTime(ref hudErrorMsgTime);

		if (finalZoneTime != null && finalZoneTime.Value <= 0) {
			if (virusStarted < 3) {
				virusStarted++;
				if (virusStarted == 1) {
					finalZoneTime = finalZoneMaxTime1;
				}
				else if (virusStarted == 2) {
					finalZoneTime = finalZoneMaxTime2;
				}
			}
		}

		if (currentVoteKick != null) {
			currentVoteKick.update();
		}
		if (voteKickCooldown > 0) {
			voteKickCooldown -= Global.spf;
			if (voteKickCooldown < 0) voteKickCooldown = 0;
		}

		if ((level.mainPlayer.isSpectator || level.mainPlayer.altSpectator) && !Menu.inMenu) {
			if (Global.input.isPressedMenu(Control.Left)) {
				level.specPlayer = level.getNextSpecPlayer(-1);
			} else if (Global.input.isPressedMenu(Control.Right)) {
				level.specPlayer = level.getNextSpecPlayer(1);
			}
		}

		for (var i = killFeed.Count - 1; i >= 0; i--) {
			var killFeed = this.killFeed[i];
			killFeed.time += 1;
			if (killFeed.time > 60 * 3) {
				this.killFeed.Remove(killFeed);
			}
		}

		checkIfWin();

		if (Global.isHost && Global.serverClient != null) {
			periodicHostSyncTime += Global.spf;
			if (periodicHostSyncTime >= 0.5f) {
				periodicHostSyncTime = 0;
				RPC.periodicHostSync.sendRpc();
			}

			if (Global.level.movingPlatforms.Count > 0) {
				syncValueTime += Global.spf;
				if (syncValueTime > 0.06f) {
					syncValueTime = 0;
					RPC.syncValue.sendRpc(Global.level.syncValue);
				}
			}
		}

		if ((Global.level.mainPlayer.isAxl ||
			Global.level.mainPlayer.isDisguisedAxl) &&
			Options.main.useMouseAim && overTime < secondsBeforeLeave &&
			!Menu.inMenu && !Global.level.mainPlayer.isSpectator
		) {
			Global.window.SetMouseCursorVisible(false);
			Global.window.SetMouseCursorGrabbed(true);
			Global.isMouseLocked = true;
		} else {
			Global.window.SetMouseCursorVisible(true);
			Global.window.SetMouseCursorGrabbed(false);
			Global.isMouseLocked = false;
		}

		if (!isOver) {
			if (setupTime == 0 && Global.isHost) {
				// Just in case packets were dropped, keep syncing "0" time
				if (Global.flFrameCount % 30 == 0) {
					Global.serverClient?.rpc(RPC.syncSetupTime, 0, 0);
				}
			}
			if (setupTime > 0 && Global.isHost) {
				int time = MathInt.Round(setupTime.Value);
				byte[] timeBytes = BitConverter.GetBytes((ushort)time);
				if (setupTime > 0) {
					setupTime -= Global.spf;
					if (setupTime <= 0) {
						setupTime = 0;
						removeAllGates();
					}
				}
				if (setupTime.Value < lastSetupTimeInt) {
					Global.serverClient?.rpc(RPC.syncSetupTime, timeBytes);
				}
				lastSetupTimeInt = MathInt.Floor(setupTime.Value);
				return;
			}
			if (remainingTime != null && Global.isHost) {
				int time = MathInt.Round(remainingTime.Value);
				byte[] timeBytes = BitConverter.GetBytes((ushort)time);
				int elimTime = MathInt.Round(eliminationTime);
				byte[] elimTimeBytes = BitConverter.GetBytes((ushort)elimTime);

				if (remainingTime > 0) {
					remainingTime -= Global.spf;
					if (remainingTime <= 0) {
						remainingTime = 0;
						if (elimTime > 0) {
							Global.serverClient?.rpc(
								RPC.syncGameTime,
								0, 0, elimTimeBytes[0], elimTimeBytes[1]
							);
						}
						else {
							Global.serverClient?.rpc(RPC.syncGameTime, 0, 0);
						}
					}
				}

				if (remainingTime.Value < lastTimeInt) {
					if (remainingTime.Value <= 10) Global.playSound("text");
					if (elimTime > 0) {
						Global.serverClient?.rpc(
							RPC.syncGameTime, timeBytes[0], timeBytes[1],
							elimTimeBytes[0], elimTimeBytes[1]
						);
					}
					else {
						Global.serverClient?.rpc(RPC.syncGameTime, timeBytes[0], timeBytes[1]);
					}
				}

				lastTimeInt = MathInt.Floor(remainingTime.Value);
			}

			if (finalZoneTime != null && Global.isHost) {
				if (finalZoneTime > 0) {
					eliminationTime += Global.spf;
					finalZoneTime -= Global.spf;
					if (finalZoneTime < 0) { finalZoneTime = 0; }
				}
			}

			if (!Global.isHost) {
				remainingTime -= Global.spf;
				finalZoneTime -= Global.spf;
			}
		}

		bool isWarpIn = level.mainPlayer.character != null && level.mainPlayer.character.isWarpIn();

		Helpers.decrementTime(ref UpgradeMenu.subtankDelay);

		if (!isOver) {
			if (!Menu.inMenu && (
				(level.mainPlayer.warpedInOnce && !isWarpIn) ||
				Global.level.mainPlayer.isSpectator) &&
				Global.input.isPressedMenu(Control.MenuPause) &&
				!chatMenu.recentlyExited
			) {
				Menu.change(
					new InGameMainMenu(
				));
			}
			else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuPause) && !isBindingControl()) {
				Menu.exit();
			}
		} else if (Global.serverClient != null) {
			if (!Global.isHost && !level.is1v1()) {
				if (!Menu.inMenu && Global.input.isPressedMenu(Control.MenuPause) && !chatMenu.recentlyExited) {
					if (mainPlayer.character is Axl axl) {
						axl.resetToggle();
					}
					Menu.change(new InGameMainMenu());
				} else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuPause) && !isBindingControl()) {
					Menu.exit();
				}
			}

			if (overTime <= secondsBeforeLeave) {

			} else {
				if (Global.isHost) {
					if ((Menu.mainMenu is HostMenu || Menu.mainMenu is SelectCharacterMenu) && Global.input.isPressedMenu(Control.MenuPause) && !chatMenu.recentlyExited) {
						if (mainPlayer.character is Axl axl) {
							axl.resetToggle();
						}
						Menu.change(new InGameMainMenu());
					} else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuPause) && !chatMenu.recentlyExited) {
						if (nextMatchHostMenu != null) Menu.change(nextMatchHostMenu);
					}
					if (!Menu.inMenu) {
						if (nextMatchHostMenu != null) Menu.change(nextMatchHostMenu);
					}
				} else {
					if (!Menu.inMenu && level.is1v1()) {
						Menu.change(
							new SelectCharacterMenu(
								null, level.is1v1(), false, true, true,
								level.gameMode.isTeamMode, Global.isHost, () => { })
							);
					}
				}
			}
		}
	}

	private bool isBindingControl() {
		if (Menu.mainMenu is ControlMenu cm) {
			return cm.isBindingControl();
		}
		return false;
	}

	public void checkIfWin() {
		if (!isOver) {
			if (Global.isHost) {
				checkIfWinLogic();

				if (noContest) {
					matchOverResponse = new RPCMatchOverResponse() {
						winningAlliances = [nullAlliance],
						winMessage = "No contest!",
						loseMessage = "No contest!",
						loseMessage2 = "Host ended match."
					};
				}

				if (isOver) {
					onMatchOver();
					Global.serverClient?.rpc(RPC.matchOver, JsonConvert.SerializeObject(matchOverResponse));
				}
			}
		} else {
			overTime += Global.spf;
			if (overTime > secondsBeforeLeave) {
				if (Global.serverClient != null) {
					if (Global.isHost) {
						if (!changedEndMenuOnceHost) {
							changedEndMenuOnceHost = true;
							nextMatchHostMenu = new HostMenu(
								null, level.server, false,
								level.server.isLAN,
								level.server.isP2P
							);
							Menu.change(nextMatchHostMenu);
						}
					} else {
						if (!changedEndMenuOnce) {
							changedEndMenuOnce = true;
							if (level.is1v1()) {
								Menu.change(new SelectCharacterMenu(null, level.is1v1(), false, true, true, level.gameMode.isTeamMode, Global.isHost, () => { }));
							}
						}
					}
				} else {
					if (Global.input.isPressedMenu(Control.MenuPause)) {
						Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.MatchOver, null, null);
					}
				}
			}
		}
	}

	public virtual void checkIfWinLogic() {
	}

	public virtual void reportKill(bool isAssist, Player killer, Player victim, bool isSummon = false) { }

	public virtual void reportDeath(bool isMain, Player killer, Player victim, bool isSummon = false) { }

	public void killToScore(Player killer, Player? victim) {
		if (Global.serverClient == null) {
			killer.score++;
		} else if (Global.canControlKillscore) {
			killer.score++;
			if (!killer.charNumToKills.ContainsKey(killer.realCharNum)) {
				killer.charNumToKills[killer.realCharNum] = 0;
			}
			killer.charNumToKills[killer.realCharNum]++;
			RPC.updatePlayer.sendRpc(killer);
			if (victim != null) { RPC.updatePlayer.sendRpc(victim); }
		}
	}

	public void deathToScore(Player? killer, Player victim) {
		if (Global.serverClient == null) {
			victim.score++;
		} else if (Global.canControlKillscore) {
			victim.score++;
			RPC.updatePlayer.sendRpc(victim);
			if (killer != null) { RPC.updatePlayer.sendRpc(killer); }
		}
	}

	public void checkIfWinLogicDM() {
		List<Player> winningPlayers = [];
		// Timeout condition.
		if (remainingTime <= 0) {
			int maxKills = 0;
			foreach (Player player in level.players) {
				if (player.score >= maxKills) {
					maxKills = player.score;
				}
			}
			// Get everyone with max kills.
			// This will be a draw if more than one.
			if (maxKills > 0) {
				foreach (var player in level.players) {
					if (player.score >= playingTo) {
						winningPlayers.Add(player);
						break;
					}
				}
			} else {
				// Make the stage win to show lose message to everyone.
				winningPlayers.Add(Player.stagePlayer);
			}
		}
		// Regular wincon.
		else {
			foreach (Player player in level.players) {
				if (player.score >= playingTo && !winningPlayers.Contains(player)) {
					winningPlayers.Add(player);
					break;
				}
			}
		}
		if (winningPlayers.Count == 0) {
			return;
		}
		if (winningPlayers.Count == 1 && winningPlayers[0] != Player.stagePlayer) {
			string winMessage = "You won!";
			string loseMessage = "You lost!";
			string loseMessage2 = winningPlayers[0].name + " wins";

			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = [winningPlayers[0].alliance],
				winMessage = winMessage,
				loseMessage = loseMessage,
				loseMessage2 = loseMessage2
			};
			return;
		}
		string drawMessage = remainingTime <= 0 ? "Stalemate!" : "Draw!";
		matchOverResponse = new RPCMatchOverResponse() {
			winningAlliances = [nullAlliance],
			winMessage = drawMessage,
			loseMessage = drawMessage,
		};
	}

	public void checkIfWinLogicTeams() {
		int winningAlliance = -1;
		for (int i = 0; i < Global.level.teamNum; i++) {
			if (teamPoints[i] >= playingTo) {
				if (winningAlliance == -1) {
					winningAlliance = i;
				} else {
					winningAlliance = -3;
				}
			}
		}
		if (winningAlliance == -1 && remainingTime <= 0) {
			byte[] orderedPoints = teamPoints.Order().ToArray();
			int score1st = orderedPoints[^1];
			int score2nd = orderedPoints[^2];
			bool closeMatch = false;

			if (score1st - 1 == score2nd) {
				closeMatch = true;
				winningAlliance = -2;
			} else if (score1st == score2nd) {
				winningAlliance = -3;
			} else {
				for (int i = 0; i < Global.level.teamNum; i++) {
					if (teamPoints[i] == score1st) {
						winningAlliance = i;
						closeMatch = false;
					}
				}
			}
			if (this is CTF && closeMatch) {
				if (level.redFlag.pickedUpOnce) {
					return;
				}
				if (level.blueFlag.pickedUpOnce) {
					return;
				}
			}
			if (this is TeamElimAlt && closeMatch) {
				return;
			}
		}
		if (winningAlliance == -3) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = [],
				winMessage = "Draw!",
				loseMessage = "Draw!"
			};
			return;
		} else if (winningAlliance == -2) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = [],
				winMessage = "Stalemate!",
				loseMessage = "Stalemate!"
			};
		} else if (winningAlliance >= 0) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = [winningAlliance],
				winMessage = "Victory!",
				winMessage2 = $"{teamNames[winningAlliance]} team wins",
				loseMessage = "You lost!",
				loseMessage2 = $"{teamNames[winningAlliance]} team wins"
			};
		}
	}

	public virtual void render() {
		if (level.mainPlayer == null) return;
		if (DevConsole.showConsole) {
			return;
		}
		navPoints.Clear();
		if (!Global.level.is1v1()) {
			drawKillFeed();
		}
		//if (Global.level.isTraining()) {
		//	drawDpsIfSet(5);
		//} else {
			drawTopHUD();
		//}
		if (isOver) {
			drawWinScreen();
		} else {
			drawRespawnHUD();
		}

		drawingScoreboard = false;
		if (!Menu.inControlMenu && level.mainPlayer.input.isHeldMenu(Control.Scoreboard)) {
			drawingScoreboard = true;
			drawScoreboard();
		}
		if (level.isAfkWarn()) {
			Fonts.drawText(
				FontType.RedishOrange, "Warning: Time before AFK Kick: " + Global.level.afkWarnTimeAmount(),
				Global.halfScreenW, 50, Alignment.Center
			);
		} else if (Global.serverClient != null && Global.serverClient.isLagging() && hudErrorMsgTime == 0) {
			Fonts.drawText(
				FontType.RedishOrange, Helpers.controlText("Connectivity issues detected."),
				Global.halfScreenW, 50, Alignment.Center
			);
		} else if (mainPlayer?.character is ViralSigma viralSigma && viralSigma.possessTarget != null) {
			Fonts.drawText(
				FontType.BlueMenu, Helpers.controlText(
				$"Hold [JUMP] to possess {viralSigma.possessTarget.player.name}"),
				Global.halfScreenW, 50, Alignment.Center
			);
		} else if (hudErrorMsgTime > 0) {
			Fonts.drawText(
				hudErrorOverrideFont ?? FontType.BlueMenu, hudErrorMsg,
				Global.halfScreenW, 50, Alignment.Center
			);
		} else if (hudDebugMsg != "") {
			Fonts.drawText(
				FontType.BlueMenu, hudDebugMsg,
				Global.halfScreenW, 50, Alignment.Center
			);
			hudDebugMsg = "";
		} else if (mainPlayer?.character is KaiserSigma kaiser) {
			string msg = "";
			if (kaiser.isVirus && KaiserSigma.canKaiserSpawn(mainPlayer.character, out _)) {
				msg += "[DASH]: Relocate";
			}
			if (msg != "") {
				Fonts.drawText(
					FontType.BlueMenu, Helpers.controlText(msg),
					Global.halfScreenW, 50, Alignment.Center
				);
			} else if (mainPlayer?.character?.charState is ViralSigmaPossess vsp && vsp.target != null) {
				Fonts.drawText(
					FontType.BlueMenu, $"Controlling possessed player {vsp.target.player.name}",
					Global.halfScreenW, 50, Alignment.Center
				);
			} else if (mainPlayer?.isPossessed() == true && mainPlayer.possesser != null) {
				Fonts.drawText(
					FontType.BlueMenu, $"{mainPlayer.possesser.name} is possessing you!",
					Global.halfScreenW - 2, 50, Alignment.Center
				);
			}
		}

		if (currentVoteKick != null) {
			currentVoteKick.render();
		} else if (level.mainPlayer.isSpectator && !Menu.inMenu) {
			if (level.specPlayer == null) {
				Fonts.drawText(
					FontType.BlueMenu, "Now spectating: (No player to spectate)",
					 Global.halfScreenW, 190, Alignment.Center
				);
			} else {
				string deadMsg = level.specPlayer.character == null ? " (Dead)" : "";
				Fonts.drawText(
					FontType.BlueMenu, "Now spectating: " + level.specPlayer.name + deadMsg,
					Global.halfScreenW, 180, Alignment.Center
				);
				Fonts.drawText(
					FontType.BlueMenu, "Left/Right: Change Spectated Player",
					Global.halfScreenW, 190, Alignment.Center
				);
			}
		} else if (level.mainPlayer.aiTakeover) {
			Fonts.drawText(
				FontType.OrangeMenu, "AI Takeover active. Press F12 to stop.",
				Global.halfScreenW, 190, Alignment.Center
			);
		} else if (
			level.mainPlayer.isDisguisedAxl &&
			level.mainPlayer.character?.disguiseCoverBlown != true
		) {
			Fonts.drawText(
				FontType.RedishOrange, "Disguised as " + level.mainPlayer.disguise?.targetName,
				Global.halfScreenW, 8, Alignment.Center
			);
		} else if (
			level.mainPlayer.currentMaverick?.controlMode == MaverickModeId.Puppeteer &&
			level.mainPlayer.weapon is MaverickWeapon mw
		) {
			if (level.mainPlayer.currentMaverick.isPuppeteerTooFar()) {
				Fonts.drawText(
					FontType.RedishOrange, mw.displayName + " too far to control",
					Global.halfScreenW, 186, Alignment.Center);
			}
		}
		drawDiagnostics();

		if (level.isNon1v1Elimination() && virusStarted > 0) {
			addMapNavpoint(safeZonePoint);
		}
	}

	public void setHUDErrorMessage(Player player, string message, bool playSound = true, bool resetCooldown = false, FontType? overrideFont = null) {
		if (player != level.mainPlayer) return;
		if (resetCooldown) hudErrorMsgTime = 0;
		if (hudErrorMsgTime == 0) {
			hudErrorMsg = message;
			hudErrorMsgTime = 2;
			hudErrorOverrideFont = overrideFont;
			if (playSound) {
				Global.playSound("error");
			}
		}
	}

	public void forceHUDErrorMessage(string message, FontType? overrideFont = null) {
		hudErrorMsg = message;
		hudErrorMsgTime = 2;
		hudErrorOverrideFont = overrideFont;
	}

	public void setHUDDebugWarning(string message, FontType? overrideFont = null) {
		hudDebugMsg += message + "\n";
		hudErrorOverrideFont = overrideFont;
	}

	public static List<Player> getOrderedPlayerList() {
		List<Player> playerList = Global.level.players.Where(p => !p.isSpectator).ToList();
		playerList.Sort((a, b) => {
			if (a.score > b.score) {
				return -1;
			}
			if (a.score < b.score) {
				return 1;
			}
			if (a.score == b.score) {
				if (a.kills < b.kills) {
					return -1;
				}
				if (a.deaths < b.deaths) {
					return 0;
				}
				return 1;
			}
			return 0;
		});
		return playerList;
	}

	public void assignPlayerHUDPositions() {
		var nonSpecPlayers = level.players.FindAll(p => p.is1v1Combatant && p != mainPlayer);
		if (mainPlayer != null) {
			nonSpecPlayers.Insert(0, mainPlayer);
		}

		// Two player case: just arrange left and right trivially
		if (nonSpecPlayers.Count <= 2) {
			hudLeftPlayer = nonSpecPlayers.ElementAtOrDefault(0);
			hudRightPlayer = nonSpecPlayers.ElementAtOrDefault(1);
		}
		// Three player case with mainPlayer
		else if (nonSpecPlayers.Count == 3 && mainPlayer != null) {
			// Not a team mode: put main player on left, others on right
			if (!isTeamMode) {
				hudLeftPlayer = nonSpecPlayers[0];
				hudTopRightPlayer = nonSpecPlayers[1];
				hudBotRightPlayer = nonSpecPlayers[2];
			}
			// If team mode, group main player on left with first ally.
			else {
				int mainPlayerAlliance = mainPlayer.alliance;
				var mainPlayerAllies = nonSpecPlayers.FindAll(p => p != mainPlayer && p.alliance == mainPlayer.alliance);
				if (mainPlayerAllies.Count == 0) {
					hudLeftPlayer = nonSpecPlayers[0];
					hudTopRightPlayer = nonSpecPlayers[1];
					hudBotRightPlayer = nonSpecPlayers[2];
				} else {
					hudTopLeftPlayer = nonSpecPlayers[0];
					hudBotLeftPlayer = mainPlayerAllies[0];
					hudRightPlayer = nonSpecPlayers.FirstOrDefault(p => p != nonSpecPlayers[0] && p != mainPlayerAllies[0]);
				}
			}
		} else {
			// Four players with main player and team mode: group main player with any allies on left if they exist
			if (nonSpecPlayers.Count == 4 && mainPlayer != null && isTeamMode) {
				int allyIndex = nonSpecPlayers.FindIndex(p => p != mainPlayer && p.alliance == mainPlayer.alliance);
				if (allyIndex != -1) {
					var temp = nonSpecPlayers[2];
					nonSpecPlayers[2] = nonSpecPlayers[allyIndex];
					nonSpecPlayers[allyIndex] = temp;
				}
			}

			hudTopLeftPlayer = nonSpecPlayers.ElementAtOrDefault(0);
			hudTopRightPlayer = nonSpecPlayers.ElementAtOrDefault(1);
			hudBotLeftPlayer = nonSpecPlayers.ElementAtOrDefault(2);
			hudBotRightPlayer = nonSpecPlayers.ElementAtOrDefault(3);
		}
	}

	public static Point getHUDHealthPosition(HUDHealthPosition position, bool isHealth) {
		float x = 0;
		if (position is HUDHealthPosition.Left) {
			x = isHealth ? 12 : 12 + 16;
		} else {
			x = isHealth ? Global.screenW - 17 : Global.screenW - 33;
		}
		return new Point(x, 126 + 8);
	}

	public static Point getHUDBuffPosition(HUDHealthPosition position) {
		int offset = 2;
		if (position == GameMode.HUDHealthPosition.Right) {
			offset = -1;
		}
		return getHUDHealthPosition(position, true).addxy(offset, 9);
	}

	public static void renderAmmo(
		float baseX, float baseY, int baseIndex,
		int barIndex, float ammo, float grayAmmo = 0, float maxAmmo = 32,
		bool allowSmall = true, string barSprite = "hud_weapon_full",
		string baseSprite = "hud_weapon_base",
		int eeStacks = 0, float ammoDisplayScale = 1
	) {
		if (baseIndex >= 0) {
			Global.sprites[baseSprite].drawToHUD(baseIndex, baseX, baseY);
		}
		// Puppeteer small energy bars.
		bool forceSmallBarsOff = false;
		if (!Options.main.smallBarsEx || !allowSmall && maxAmmo > 16) {
			forceSmallBarsOff = true;
		}
		// Small Bars option.
		if (Options.main.enableSmallBars && !forceSmallBarsOff) {
			ammoDisplayScale *= 0.5f;
		}
		// Main draw step.
		baseY -= 16;
		float maxAP = MathF.Ceiling(maxAmmo / ammoDisplayScale);
		float curAP = MathF.Floor(ammo / ammoDisplayScale);
		if (ammo == maxAmmo) { curAP = maxAP; }
		float ceilCurAP = MathF.Ceiling(ammo / ammoDisplayScale);
		float floatCurAP = ammo / ammoDisplayScale;
		float fhpAlpha = floatCurAP - curAP;

		for (int i = 0; i < maxAP; i++) {
			if (i < curAP) {
				if (ammo < grayAmmo) {
					Global.sprites["hud_weapon_full"].drawToHUD(grayAmmoIndex, baseX, baseY);
				} else {
					Global.sprites[barSprite].drawToHUD(barIndex, baseX, baseY);
				}
			} else {
				Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
				if (i < ceilCurAP) {
					Global.sprites[barSprite].drawToHUD(barIndex, baseX, baseY, fhpAlpha);
				}
			}
			baseY -= 2;
		}
		if (baseIndex == -3) {
			Global.sprites["hud_energy_top"].drawToHUD(eeStacks, baseX, baseY);
		} else {
			Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);
		}
	}

	public void addKillFeedEntry(KillFeedEntry killFeed, bool sendRpc = false) {
		killFeedHistory.Add(killFeed.rawString());
		this.killFeed.Insert(0, killFeed);
		if (this.killFeed.Count > 10) this.killFeed.Pop();
		if (sendRpc) {
			killFeed.sendRpc();
		}
	}

	public float killFeedOffset() {
		return 57;
	}

	public FontType getKillFeedTeamFonts(int team) {
		return team switch {
			0 => FontType.Blue,
			1 => FontType.Red,
			2 => FontType.Green,
			3 => FontType.Purple,
			4 => FontType.Yellow,
			5 => FontType.Orange,
			_ => FontType.LightGrey
		};
	}

	public void drawKillFeed() {
		var fromRight = Global.screenW - 10;
		var fromTop = 10;
		var yDist = 12;
		for (var i = 0; i < this.killFeed.Count && i < 3; i++) {
			var killFeed = this.killFeed[i];

			string victimName = killFeed.victim?.name ?? "";
			if (killFeed.maverickKillFeedIndex != null) {
				victimName = " (" + victimName + ")";
			}

			var msg = "";
			var killersMsg = "";
			if (killFeed.killer != null) {
				var killerMessage = "";
				if (killFeed.killer != killFeed.victim) {
					killerMessage = killFeed.killer.name;
				}
				var assisterMsg = "";
				if (killFeed.assister != null && killFeed.assister != killFeed.victim) {
					assisterMsg = killFeed.assister.name;
				}

				var killerAndAssister = new List<string>();
				if (!string.IsNullOrEmpty(killerMessage)) killerAndAssister.Add(killerMessage);
				if (!string.IsNullOrEmpty(assisterMsg)) killerAndAssister.Add(assisterMsg);

				killersMsg = string.Join(" & ", killerAndAssister) + "    ";

				msg = killersMsg + victimName;

			} else if (killFeed.victim != null && killFeed.customMessage == null) {
				if (killFeed.maverickKillFeedIndex != null) {
					msg = killFeed.victim.name + "'s Maverick died";
				} else {
					msg = victimName + " died";
				}
			} else {
				msg = killFeed.customMessage;
			}

			if (killFeed.killer == level.mainPlayer || killFeed.victim == level.mainPlayer || killFeed.assister == level.mainPlayer) {
				int msgLen = Fonts.measureText(FontType.FBlue, msg);
				int msgHeight = 10;
				DrawWrappers.DrawRect(
					fromRight - msgLen - 2, fromTop - 2 + (i * yDist) - msgHeight / 2,
					fromRight + 2, fromTop - 1 + msgHeight / 2 + (i * yDist),
					true, new Color(0, 0, 0, 128), 1, ZIndex.HUD,
					isWorldPos: false, outlineColor: Color.White
				);
			}

			FontType killerColor = FontType.Blue;
			if (killFeed.killer != null && killFeed.killer.alliance == redAlliance && isTeamMode) {
				killerColor = FontType.Red;
			}
			FontType victimColor = FontType.Blue;
			if (killFeed.victim != null && killFeed.victim.alliance == redAlliance && isTeamMode) {
				victimColor = FontType.Red;
			}

			if (killFeed.killer != null) {
				int nameLen = Fonts.measureText(killerColor, victimName);
				Fonts.drawText(
					killerColor, victimName, fromRight, fromTop + (i * yDist) - 5, Alignment.Right
				);
				Fonts.drawText(
					victimColor, killersMsg, fromRight - nameLen, fromTop + (i * yDist) - 5, Alignment.Right
				);
				int weaponIndex = killFeed.weaponIndex ?? 0;
				weaponIndex = (
					weaponIndex < Global.sprites["hud_killfeed_weapon"].frames.Length ? weaponIndex : 0
				);
				Global.sprites["hud_killfeed_weapon"].drawToHUD(
					weaponIndex, fromRight - nameLen - 14, fromTop + (i * yDist) - 2
				);
				if (killFeed.maverickKillFeedIndex != null) {
					Global.sprites["hud_killfeed_weapon"].drawToHUD(
						killFeed.maverickKillFeedIndex.Value, fromRight - nameLen + 3, fromTop + (i * yDist) - 2
					);
				}
			} else {
				FontType fontColor = killFeed.customMessageAlliance switch {
					GameMode.blueAlliance => FontType.Blue,
					GameMode.redAlliance => FontType.Red,
					_ => FontType.Grey
				};
				Fonts.drawText(
					fontColor, msg, fromRight, fromTop + (i * yDist) - 5,
					Alignment.Right
				);
			}
		}
	}

	public void drawSpectators() {
		var spectatorNames = level.players.Where(p => p.isSpectator).Select((p) => {
			bool isHost = p.serverPlayer?.isHost ?? false;
			return p.name + (isHost ? " (Host)" : "");
		});
		string spectatorStr = string.Join(",", spectatorNames);
		if (!string.IsNullOrEmpty(spectatorStr)) {
			Fonts.drawText(FontType.BlueMenu, "Spectators: " + spectatorStr, 15, 200);
		}
	}

	public bool shouldDrawRadar() {
		if (Global.level.isRace()) return true;
		if (level.is1v1()) return false;
		if (level.mainPlayer == null) return false;
		if (level.mainPlayer.isX && level.mainPlayer.hasHelmetArmor(3)) {
			return true;
		}
		if (level.mainPlayer.isAxl && level.boundBlasterAltProjs.Any(b => b.state == 1)) {
			return true;
		}
		if (level.mainPlayer.currentMaverick != null) {
			if (level.mainPlayer.currentMaverick.controlMode is
				MaverickModeId.Puppeteer or MaverickModeId.Summoner
			) {
				return level.mainPlayer.mavericks.Count > 0;
			}
		}
		if (level.mainPlayer.currentMaverick != null) {
			if (level.mainPlayer.currentMaverick.controlMode == MaverickModeId.Puppeteer) {
				return level.mainPlayer.character?.alive == true;
			} else if (level.mainPlayer.currentMaverick.controlMode == MaverickModeId.Summoner) {
				return level.mainPlayer.mavericks.Count > 1;
			}
		}
		return false;
	}

	public void drawDiagnostics() {
		if (Global.showDiagnostics) {
			double? downloadedBytes = 0;
			double? uploadedBytes = 0;

			if (Global.serverClient?.client?.ServerConnection?.Statistics != null) {
				downloadedBytes = Global.serverClient.client.ServerConnection.Statistics.ReceivedBytes;
				uploadedBytes = Global.serverClient.client.ServerConnection.Statistics.SentBytes;
			}

			int topLeftX = 10;
			int topLeftY = 24;
			int w = 120;
			int lineHeight = 9;

			DrawWrappers.DrawRect(
				topLeftX - 2,
				topLeftY + lineHeight - 2,
				topLeftX + w,
				topLeftY + currentLineH + lineHeight - 1,
				true, Helpers.MenuBgColor, 1, ZIndex.HUD - 10, isWorldPos: false
			);

			currentLineH = 0;

			bool showNetStats = Global.debug;
			if (showNetStats) {
				if (downloadedBytes != null) {
					string downloadMb = (downloadedBytes.Value / 1000000.0).ToString("0.00");
					string downloadKb = (downloadedBytes.Value / 1000.0).ToString("0.00");
					Fonts.drawText(
						FontType.Grey,
						"Bytes received: " + downloadMb + " mb" + " (" + downloadKb + " kb)",
						topLeftX, topLeftY + (currentLineH += lineHeight)
					);
				}
				if (uploadedBytes != null) {
					string uploadMb = (uploadedBytes.Value / 1000000.0).ToString("0.00");
					string uploadKb = (uploadedBytes.Value / 1000.0).ToString("0.00");
					Fonts.drawText(
						FontType.Grey,
						"Bytes sent: " + uploadMb + " mb" + " (" + uploadKb + " kb)",
						topLeftX, topLeftY + (currentLineH += lineHeight)
					);
				}

				double avgPacketIncrease = Global.lastFramePacketIncreases.Count == 0 ? 0 : Global.lastFramePacketIncreases.Average();
				Fonts.drawText(
					FontType.Grey,
					"Packet rate: " + (avgPacketIncrease * 60f).ToString("0") + " bytes/second", topLeftX, topLeftY + (currentLineH += lineHeight)
				);
				Fonts.drawText(
					FontType.Grey,
					"Packet rate: " + avgPacketIncrease.ToString("0") + " bytes/frame", topLeftX, topLeftY + (currentLineH += lineHeight)
				);
			}

			double avgPacketsReceived = Global.last10SecondsPacketsReceived.Count == 0 ? 0 : Global.last10SecondsPacketsReceived.Average();
			Fonts.drawText(
				FontType.Grey,
				"Ping Packets / sec: " + avgPacketsReceived.ToString("0.0"),
				topLeftX, topLeftY + (currentLineH += lineHeight)
			);
			Fonts.drawText(
				FontType.Grey,
				"Start GameObject Count: " + level.startGoCount, topLeftX, topLeftY + (currentLineH += lineHeight)
			);
			Fonts.drawText(
				FontType.Grey,
				"Current GameObject Count: " + level.gameObjects.Count, topLeftX, topLeftY + (currentLineH += lineHeight)
			);

			Fonts.drawText(
				FontType.Grey,
				"GridItem Count: " +
				level.startGridCount + "-" + level.getGridCount(),
				topLeftX, topLeftY + (currentLineH += lineHeight)
			);
			Fonts.drawText(
				FontType.Grey,
				"TGridItem Count: " +
				level.startTGridCount + "-" + level.getTGridCount(),
				topLeftX, topLeftY + (currentLineH += lineHeight)
			);

			Fonts.drawText(
				FontType.Grey,
				"Sound Count: " + Global.sounds.Count, topLeftX, topLeftY + (currentLineH += lineHeight)
			);

			/*Fonts.drawText(
				FontType.Grey,
				"List Counts: " + Global.level.getListCounts(), topLeftX, topLeftY + (currentLineH += lineHeight)
			);

			float avgFrameProcessTime = Global.lastFrameProcessTimes.Count == 0 ? 0 : Global.lastFrameProcessTimes.Average();

			Fonts.drawText(
				FontType.Grey,
				"Avg frame process time: " + avgFrameProcessTime.ToString("0.00") + " ms", topLeftX, topLeftY + (currentLineH += lineHeight)
			);
			*/
			//float graphYHeight = 20;
			//drawDiagnosticsGraph(Global.lastFrameProcessTimes, topLeftX, topLeftY + (currentLineH += lineHeight) + graphYHeight, 1);

		}
	}

	public void drawDiagnosticsGraph(List<float> values, float startX, float startY, float yScale) {
		for (int i = 1; i < values.Count; i++) {
			DrawWrappers.DrawLine(startX + i - 1, startY + (values[i - 1] * yScale), startX + i, startY + (values[i] * yScale), Color.Green, 0.5f, ZIndex.HUD, false);
		}
	}

	public void drawWeaponSwitchHUD(Player player) {
		if (player.isSelectingRA()) {
			drawRideArmorIcons();
		}


		if (player.character?.rideArmor != null || player.character?.rideChaser != null) {
			return;
		}

		var iconW = 8;
		var iconH = 8;
		var width = 15;

		var startX = getWeaponSlotStartX(player, ref iconW, ref iconH, ref width);
		var startY = Global.screenH - 12;

		int gigaWeaponX = 11;

		if (player.isX && Options.main.gigaCrushSpecial) {
			Weapon? gigaCrush = player.weapons.FirstOrDefault((Weapon w) => w is GigaCrush);
			if (gigaCrush != null) {
				drawWeaponSlot(gigaCrush, gigaWeaponX, 159);
				gigaWeaponX += 18;
			}
		}
		if (player.isX && Options.main.novaStrikeSpecial) {
			Weapon? novaStrike = player.weapons.FirstOrDefault((Weapon w) => w is HyperNovaStrike);
			if (novaStrike != null) {
				drawWeaponSlot(novaStrike, gigaWeaponX, 159);
				gigaWeaponX += 18;
			}
		}
		if (player.character is MegamanX mmx && mmx.hasFgMoveEquipped() && mmx.canAffordFgMove()) {
			if (mmx.hasHadoukenEquipped()) {
				int x = gigaWeaponX;
				int y = 159;
				Global.sprites["hud_weapon_icon"].drawToHUD(112, x, y);
				float cooldown = Helpers.progress(player.hadoukenAmmo, 1920f);
				drawWeaponSlotCooldown(x, y, cooldown);
				gigaWeaponX += 18;
			}
			if (mmx.hasShoryukenEquipped()) {
				int x = gigaWeaponX;
				int y = 159;
				Global.sprites["hud_weapon_icon"].drawToHUD(113, x, y);
				float cooldown = Helpers.progress(player.shoryukenAmmo, 1920f);
				drawWeaponSlotCooldown(x, y, cooldown);
				gigaWeaponX += 18;
			}
		}

		if (player.isAxl && player.weapons[0].type > 0) {
			int x = 10, y = 156;
			int index = 0;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.MetteurCrash) index = 0;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.BeastKiller) index = 1;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.MachineBullets) index = 2;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.DoubleBullets) index = 3;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.RevolverBarrel) index = 4;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.AncientGun) index = 5;
			Global.sprites["hud_axl_ammo"].drawToHUD(index, x, y);
			int currentAmmo = MathInt.Ceiling(player.weapons[0].ammo);
			int totalAmmo = MathInt.Ceiling(player.axlBulletTypeAmmo[player.weapons[0].type]);
			Fonts.drawText(
				FontType.Grey, totalAmmo.ToString(), x + 10, y - 4
			);
		}

		if (player.isGridModeEnabled()) {
			if (player.gridModeHeld == true) {
				var gridPoints = player.gridModePoints();
				for (var i = 0; i < player.weapons.Count && i < 9; i++) {
					Point pos = gridPoints[i];
					var weapon = player.weapons[i];
					var x = Global.halfScreenW + (pos.x * 20);
					var y = Global.screenH - 30 + pos.y * 20;

					drawWeaponSlot(weapon, x, y);
				}
			}

			/*
			// Draw giga crush/hyper buster
			if (player.weapons.Count == 10)
			{
				int x = 10, y = 146;
				Weapon weapon = player.weapons[9];

				drawWeaponSlot(weapon, x, y);

				//Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, x, y);
				//DrawWrappers.DrawRectWH(
					//x - 8, y - 8, 16, 16 - MathF.Floor(16 * (weapon.ammo / weapon.maxAmmo)),
					//true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false
				//);
			}
			*/
			return;
		}

		for (var i = 0; i < player.weapons.Count; i++) {
			var weapon = player.weapons[i];
			var x = startX + (i * width);
			var y = startY;
			if (weapon is HyperCharge hb) {
				bool canShootHyperBuster = hb.canShootIncludeCooldown(player);
				Color lineColor = canShootHyperBuster ? Color.White : Helpers.Gray;

				float slotPosX = startX + (player.hyperChargeSlot * width);
				int yOff = -1;

				// Stretch black
				DrawWrappers.DrawRect(
					slotPosX, y - 9 + yOff, x, y - 12 + yOff,
					true, Color.Black, 1, ZIndex.HUD, false
				);
				// Right
				DrawWrappers.DrawRect(
					x - 1, y - 7, x + 2, y - 12 + yOff,
					true, Color.Black, 1, ZIndex.HUD, false
				);
				DrawWrappers.DrawRect(
					x, y - 8, x + 1, y - 11 + yOff,
					true, lineColor, 1, ZIndex.HUD, false
				);
				// Left
				DrawWrappers.DrawRect(
					slotPosX - 1, y - 7, slotPosX + 2, y - 12 + yOff,
					true, Color.Black, 1, ZIndex.HUD, false
				);
				DrawWrappers.DrawRect(
					slotPosX, y - 8, slotPosX + 1, y - 11 + yOff,
					true, lineColor, 1, ZIndex.HUD, false
				);
				// Stretch white
				DrawWrappers.DrawRect(
					slotPosX, y - 10 + yOff, x, y - 11 + yOff,
					true, lineColor, 1, ZIndex.HUD, false
				);
				break;
			}
		}
		int offsetX = 0;
		for (var i = 0; i < player.weapons.Count; i++) {
			var weapon = player.weapons[i];
			var x = startX + (i * width) + offsetX;
			var y = startY;
			if (player.isX && Options.main.gigaCrushSpecial && weapon is GigaCrush) {
				offsetX -= width;
				continue;
			}
			if (player.isX && Options.main.novaStrikeSpecial && weapon is HyperNovaStrike) {
				offsetX -= width;
				continue;
			}
			if (level.mainPlayer.weapon == weapon && !level.mainPlayer.isSelectingCommand()) {
				DrawWrappers.DrawRectWH(
					x - 7, y - 8, 14, 15, false,
					Color.Black, 1, ZIndex.HUD, false
				);
				drawWeaponSlot(weapon, x, y - 1, true);
			} else {
				drawWeaponSlot(weapon, x, y);
			}
		}

		if (player == mainPlayer && mainPlayer.isSelectingCommand()) {
			drawMaverickCommandIcons();
		}
	}

	public void drawZeroGigaCooldown(Weapon weapon, int x = 11, int y = 159) {
		// This runs once per character.
		if (weapon == null || weapon.shootCooldown <= 0) {
			return;
		}
		float cooldown = Helpers.progress(weapon.shootCooldown, weapon.fireRate);
		drawGigaWeaponCooldown(weapon.weaponSlotIndex, 1 - cooldown, x, y);
	}

	public void drawGigaWeaponCooldown(int slotIndex, float cooldown, int x = 11, int y = 159, bool isKillFeed = false, int xKF = 11, int yKF = 159, float alpha = 1) {
		if (isKillFeed) {
			Global.sprites["hud_weapon_icon"].drawToHUD(118, x, y, alpha);
			Global.sprites["hud_killfeed_weapon"].drawToHUD(slotIndex, xKF, yKF, alpha);
		} else {
			Global.sprites["hud_weapon_icon"].drawToHUD(slotIndex, x, y, alpha);
		}
		drawWeaponSlotCooldown(x, y, cooldown);
	}

	public void drawWeaponSlot(Weapon weapon, float x, float y, bool selected = false) {
		if (weapon is MechMenuWeapon && !mainPlayer.isSpectator && level.mainPlayer.character?.linkedRideArmor != null) {
			int index = 37 + level.mainPlayer.character.linkedRideArmor.raNum;
			if (index == 42) index = 119;
			Global.sprites["hud_weapon_icon"].drawToHUD(index, x, y);
		} else if (weapon is MechMenuWeapon && level.mainPlayer.isSelectingRA()) {
			return;
		} else if (weapon is not AbsorbWeapon) {
			Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, x, y);
		}
		bool canShoot = weapon.canShoot(0, mainPlayer);
		if (!canShoot && (selected || weapon.ammo > 0)) {
			drawWeaponStateOverlay(x, y, 2);
		} else if (canShoot && weapon.shootCooldown > 0 && weapon.fireRate > 10 && weapon.drawCooldown) {
			drawWeaponStateOverlay(x, y, 1);
		} else if (selected) {
			drawWeaponStateOverlay(x, y, 0);
		}
		if (weapon.ammo < weapon.maxAmmo && weapon.drawAmmo) {
			drawWeaponSlotAmmo(x, y, weapon.ammo / weapon.maxAmmo);
		}

		if (weapon is MechaniloidWeapon mew) {
			if (mew.mechaniloidType == MechaniloidType.Tank && level.mainPlayer.tankMechaniloidCount() > 0) {
				drawWeaponText(x, y, level.mainPlayer.tankMechaniloidCount().ToString());
			} else if (mew.mechaniloidType == MechaniloidType.Hopper && level.mainPlayer.hopperMechaniloidCount() > 0) {
				drawWeaponText(x, y, level.mainPlayer.hopperMechaniloidCount().ToString());
			} else if (mew.mechaniloidType == MechaniloidType.Bird && level.mainPlayer.birdMechaniloidCount() > 0) {
				drawWeaponText(x, y, level.mainPlayer.birdMechaniloidCount().ToString());
			} else if (mew.mechaniloidType == MechaniloidType.Fish && level.mainPlayer.fishMechaniloidCount() > 0) {
				drawWeaponText(x, y, level.mainPlayer.fishMechaniloidCount().ToString());
			}
		}
		if (!mainPlayer.isSpectator && mainPlayer.character is MegamanX mmx) {
			if (weapon is MagnetMine && mmx.magnetMines.Count > 0) {
				drawWeaponText(x, y, mmx.magnetMines.Count.ToString());
			}
			if (weapon is HyperCharge hc) {
				if (level.mainPlayer.hyperChargeSlot >= 0 &&
					mainPlayer.weapons[level.mainPlayer.hyperChargeSlot].ammo == 0
				) {
					drawWeaponSlotAmmo(x, y, 0);
				} else {
					drawWeaponSlotCooldown(x, y, hc.shootCooldown / hc.fireRate);
				}
			} else if (weapon is HyperNovaStrike ns) {
				drawWeaponSlotCooldown(x, y, ns.shootCooldown / ns.fireRate);
			}
		}


		if (weapon is BlastLauncher &&
			level.mainPlayer.character is Axl { loadout.blastLauncherAlt: 1 } &&
			level.mainPlayer.grenades.Count > 0
		) {
			drawWeaponText(x, y, level.mainPlayer.grenades.Count.ToString());
		}

		if (weapon is DNACore dnaCore && level.mainPlayer.weapon == weapon && level.mainPlayer.input.isHeld(Control.Special1, level.mainPlayer)) {
			drawTransformPreviewInfo(dnaCore, x, y);
		}

		if (weapon is SigmaMenuWeapon) {
			drawWeaponSlotCooldown(x, y, weapon.shootCooldown / 4);
		}

		if (weapon is AxlWeapon aw2 && weapon is not DNACore) {
			drawWeaponSlotCooldownBar(x, y, aw2.shootCooldown / aw2.fireRate);
			drawWeaponSlotCooldownBar(x, y, aw2.altShotCooldown / aw2.altFireCooldown, true);
		}

		MaverickWeapon? mw = weapon as MaverickWeapon;
		if (mw != null) {
			float maxHealth = level.mainPlayer.getMaverickMaxHp(mw.controlMode);
			if (mw.controlMode == MaverickModeId.Summoner) {
				float mHealth = mw.maverick?.health ?? mw.lastHealth;
				float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
				if (!mw.summonedOnce) mHealth = 0;
				drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
				drawWeaponSlotCooldown(x, y, mw.shootCooldown / MaverickWeapon.summonerCooldown);
			} else if (mw.controlMode == MaverickModeId.Puppeteer) {
				float mHealth = mw.maverick?.health ?? mw.lastHealth;
				float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
				if (!mw.summonedOnce) mHealth = 0;
				drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
			} else if (mw.controlMode == MaverickModeId.Striker) {
				float mHealth = mw.maverick?.health ?? mw.lastHealth;
				float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
				drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
				drawWeaponSlotCooldown(x, y, mw.cooldown / MaverickWeapon.strikerCooldown);
			} else if (mw.controlMode == MaverickModeId.TagTeam) {
				float mHealth = mw.maverick?.health ?? mw.lastHealth;
				float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
				if (!mw.summonedOnce) mHealth = 0;
				drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
				drawWeaponSlotCooldown(x, y, mw.cooldown / MaverickWeapon.tagTeamCooldown);
			}

			if (mw is ChillPenguinWeapon) {
				for (int i = 0; i < mainPlayer.iceStatues.Count; i++) {
					Global.sprites["hud_ice_statue"].drawToHUD(0, x - 3 + (i * 6), y + 10);
				}
			}

			if (mw is DrDopplerWeapon ddw && ddw.ballType == 1) {
				Global.sprites["hud_doppler_weapon"].drawToHUD(ddw.ballType, x + 4, y + 4);
			}

			if (mw is WireSpongeWeapon && level.mainPlayer.seeds.Count > 0) {
				drawWeaponText(x, y, level.mainPlayer.seeds.Count.ToString());
			}

			if (mw is BubbleCrabWeapon && mw.maverick is BubbleCrab bc && bc.crabs.Count > 0) {
				drawWeaponText(x, y, bc.crabs.Count.ToString());
			}
		}

		/*if (level.mainPlayer.weapon == weapon && !level.mainPlayer.isSelectingCommand()) {
			drawWeaponSlotSelected(x, y);
		}*/

		if (weapon is AxlWeapon && Options.main.axlLoadout.altFireArray[Weapon.wiToFi(weapon.index)] == 1) {
			Helpers.drawWeaponSlotSymbol(x - 8, y - 8, "²");
		}

		if (weapon is SigmaMenuWeapon && level.mainPlayer.character is BaseSigma baseSigma) {
			if (baseSigma.currentMaverickCommand == MaverickAIBehavior.Follow &&
				level.mainPlayer.weapons.First(
					wp => wp is MaverickWeapon mw && mw.controlMode is MaverickModeId.Summoner or MaverickModeId.Puppeteer
				) != null
			) {
				Helpers.drawWeaponSlotSymbol(x - 8, y - 8, "ª");
			}
			/*
			string commandModeSymbol = null;
			//if (level.mainPlayer.isSummoner()) commandModeSymbol = "SUM";
			if (level.mainPlayer.isPuppeteer()) commandModeSymbol = "PUP";
			if (level.mainPlayer.isStriker()) commandModeSymbol = "STK";
			if (level.mainPlayer.isTagTeam()) commandModeSymbol = "TAG";
			if (commandModeSymbol != null)
			{
				Helpers.drawTextStd(commandModeSymbol, x - 7, y + 4, Alignment.Left, fontSize: 12);
			}
			*/
		}


		if (mw != null) {
			if (mw.currencyHUDAnimTime > 0) {
				float animProgress = mw.currencyHUDAnimTime / MaverickWeapon.currencyHUDMaxAnimTime;
				float yOff = animProgress * 20;
				float alpha = Helpers.clamp01(1 - animProgress);
				Global.sprites["hud_scrap"].drawToHUD(0, x - 6, y - yOff - 10, alpha);
				//DrawWrappers.DrawText("+1", x - 6, y - yOff - 10, Alignment.Center, )
				if (Global.level.isHyperMatch()) {
					Fonts.drawText(FontType.RedishOrange, "+5", x - 4, y - yOff - 15, Alignment.Left);
				} else
					Fonts.drawText(FontType.RedishOrange, "+1", x - 4, y - yOff - 15, Alignment.Left);
			}

			if (mw.trueControlMode == MaverickModeId.Summoner && mw.maverick != null) {
				if (mw.maverick.aiBehavior == MaverickAIBehavior.Attack) {
					Helpers.drawWeaponSlotSymbol(x - 8, y - 8, "²", FontType.Pink);
				} else if (mw.maverick.aiBehavior == MaverickAIBehavior.Follow) {
					Helpers.drawWeaponSlotSymbol(x - 8, y - 8, "ª");
				}
			}
		}

		if (weapon is AbsorbWeapon aw) {
			var sprite = Global.sprites[aw.absorbedProj.sprite.name];

			float w = sprite.frames[0].rect.w();
			float h = sprite.frames[0].rect.h();

			float scaleX = Helpers.clampMax(10f / w, 1);
			float scaleY = Helpers.clampMax(10f / h, 1);

			Global.sprites["hud_weapon_icon"].draw(weapon.weaponSlotIndex, Global.level.camX + x, Global.level.camY + y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
			Global.sprites[aw.absorbedProj.sprite.name].draw(0, Global.level.camX + x, Global.level.camY + y, 1, 1, null, 1, scaleX, scaleY, ZIndex.HUD);
		}
	}

	private void drawWeaponStateOverlay(float x, float y, int type) {
		Color cooldownColour = type switch {
			1 => new Color(255, 212, 128, 128),
			2 => new Color(255, 128, 128, 128),
			_ => new Color(128, 255, 128, 128),
		};
		DrawWrappers.DrawRectWH(
			x - 6f, y - 6f,
			12f, 12f, filled: false,
			cooldownColour, 1,
			1000000L, isWorldPos: false
		);
	}

	private void drawWeaponText(float x, float y, string text) {
		Fonts.drawText(
			FontType.Yellow, text, x + 1, y + 8, Alignment.Center
		);
	}

	private void drawWeaponSlotSelected(float x, float y) {
		DrawWrappers.DrawRectWH(x - 7, y - 7, 14, 14, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
	}

	public static void drawWeaponSlotAmmo(float x, float y, float val) {
		DrawWrappers.DrawRectWH(x - 8, y - 8, 16, 16 - MathF.Floor(16 * val), true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
	}

	public static void drawWeaponSlotCooldownBar(float x, float y, float val, bool isAlt = false) {
		if (val <= 0) return;
		val = Helpers.clamp01(val);

		float yPos = -8.5f;
		if (isAlt) yPos = 8.5f;
		DrawWrappers.DrawLine(x - 8, y + yPos, x + 8, y + yPos, Color.Black, 1, ZIndex.HUD, false);
		DrawWrappers.DrawLine(x - 8, y + yPos, x - 8 + (val * 16), y + yPos, Color.Yellow, 1, ZIndex.HUD, false);
	}

	public static void drawWeaponSlotCooldown(float x, float y, float val, byte trans = 128) {
		if (val <= 0) return;
		val = Helpers.clamp01(val);

		int sliceStep = 1;
		if (Options.main.particleQuality == 0) sliceStep = 4;
		if (Options.main.particleQuality == 1) sliceStep = 2;

		int gridLen = 16 / sliceStep;
		List<Point> points = new List<Point>(gridLen * 4);

		int startX = 0;
		int startY = -8;

		int xDir = -1;
		int yDir = 0;

		for (int i = 0; i < gridLen * 4; i++) {
			points.Add(new Point(x + startX, y + startY));
			startX += sliceStep * xDir;
			startY += sliceStep * yDir;

			if (xDir == -1 && startX == -8) {
				xDir = 0;
				yDir = 1;
			}
			if (yDir == 1 && startY == 8) {
				yDir = 0;
				xDir = 1;
			}
			if (xDir == 1 && startX == 8) {
				xDir = 0;
				yDir = -1;
			}
			if (yDir == -1 && startY == -8) {
				xDir = -1;
				yDir = 0;
			}
		}

		var slices = new List<List<Point>>(points.Count);
		for (int i = 0; i < points.Count; i++) {
			Point nextPoint = i + 1 >= points.Count ? points[0] : points[i + 1];
			slices.Add(new List<Point>() { new Point(x, y), points[i], nextPoint });
		}

		for (int i = 0; i < (int)(val * slices.Count); i++) {
			DrawWrappers.DrawPolygon(slices[i], new Color(0, 0, 0, trans), true, ZIndex.HUD, false);
		}
	}

	public static void drawWeaponSlotCooldownR(float x, float y, float val, byte trans = 128) {
		val = Helpers.clamp01(val);
		val = 1 - val;

		int sliceStep = Options.main.particleQuality switch {
			0 => 4,
			1 => 2,
			_ => 1
		};
		int gridLen = 16 / sliceStep;
		List<Point> points = new List<Point>(gridLen * 4);
		int startX = 0;
		int startY = -8;
		int xDir = 1;
		int yDir = 0;

		for (int i = 0; i < gridLen * 4; i++) {
			points.Add(new Point(x + startX, y + startY));
			startX += sliceStep * xDir;
			startY += sliceStep * yDir;
			if (xDir == 1 && startX == 8) {
				xDir = 0;
				yDir = 1;
			}
			if (yDir == 1 && startY == 8) {
				yDir = 0;
				xDir = -1;
			}
			if (xDir == -1 && startX == -8) {
				xDir = 0;
				yDir = -1;
			}
			if (yDir == -1 && startY == -8) {
				xDir = 1;
				yDir = 0;
			}
		}

		List<List<Point>> slices = new List<List<Point>>(points.Count);
		for (int i = 0; i < points.Count; i++) {
			Point nextPoint = i + 1 >= points.Count ? points[0] : points[i + 1];
			slices.Add(new List<Point>() { new Point(x, y), points[i], nextPoint });
		}

		for (int i = 0; i < (int)(val * slices.Count); i++) {
			DrawWrappers.DrawPolygon(slices[i], new Color(0, 0, 0, trans), true, ZIndex.HUD, false);
		}
	}

	public void drawTransformPreviewInfo(DNACore dnaCore, float x, float y) {
		float sx = x - 50;
		float sy = y - 100;

		float leftX = sx + 15;

		DrawWrappers.DrawRect(sx, sy, x + 50, y - 18, true, new Color(0, 0, 0, 224), 1, ZIndex.HUD, false);
		Global.sprites["cursorchar"].drawToHUD(0, x, y - 13);
		int sigmaForm = dnaCore.loadout?.sigmaLoadout?.sigmaForm ?? 0;

		sy += 5;
		Fonts.drawText(FontType.RedishOrange, dnaCore.name, x, sy);
		sy += 30;
		if (dnaCore.charNum == 0) {
			if (dnaCore.ultimateArmor) {
				Global.sprites["menu_megaman"].drawToHUD(5, x, sy + 4);
			} else if (dnaCore.armorFlag == ushort.MaxValue) {
				Global.sprites["menu_megaman"].drawToHUD(4, x, sy + 4);
			} else {
				Global.sprites["menu_megaman_armors"].drawToHUD(0, x, sy + 4);
				int[] armorVals = MegamanX.getArmorVals(dnaCore.armorFlag);
				int boots = armorVals[2];
				int body = armorVals[0];
				int helmet = armorVals[3];
				int arm = armorVals[1];

				if (helmet == 1) Global.sprites["menu_megaman_armors"].drawToHUD(1, x, sy + 4);
				if (helmet == 2) Global.sprites["menu_megaman_armors"].drawToHUD(2, x, sy + 4);
				if (helmet >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(3, x, sy + 4);

				if (body == 1) Global.sprites["menu_megaman_armors"].drawToHUD(4, x, sy + 4);
				if (body == 2) Global.sprites["menu_megaman_armors"].drawToHUD(5, x, sy + 4);
				if (body >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(6, x, sy + 4);

				if (arm == 1) Global.sprites["menu_megaman_armors"].drawToHUD(7, x, sy + 4);
				if (arm == 2) Global.sprites["menu_megaman_armors"].drawToHUD(8, x, sy + 4);
				if (arm >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(9, x, sy + 4);

				if (boots == 1) Global.sprites["menu_megaman_armors"].drawToHUD(10, x, sy + 4);
				if (boots == 2) Global.sprites["menu_megaman_armors"].drawToHUD(11, x, sy + 4);
				if (boots >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(12, x, sy + 4);

				if (helmet == 15) Global.sprites["menu_chip"].drawToHUD(0, x, sy - 16 + 4);
				if (body == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 2, sy - 5 + 4);
				if (arm == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 9, sy - 2 + 4);
				if (boots == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 12, sy + 10);
			}
		} else if (dnaCore.charNum == 1) {
			int index = 0;
			if (dnaCore.hyperMode == DNACoreHyperMode.BlackZero) index = 1;
			if (dnaCore.hyperMode == DNACoreHyperMode.AwakenedZero) index = 2;
			if (dnaCore.hyperMode == DNACoreHyperMode.NightmareZero) index = 3;
			Global.sprites["menu_zero"].drawToHUD(index, x, sy + 1);
		} else if (dnaCore.charNum == 2) {
			int index = 0;
			if (dnaCore.hyperMode == DNACoreHyperMode.VileMK2) index = 1;
			if (dnaCore.hyperMode == DNACoreHyperMode.VileMK5) index = 2;
			Global.sprites["menu_vile"].drawToHUD(index, x, sy + 2);
			if (dnaCore.frozenCastle) {
				Fonts.drawText(FontType.DarkBlue, "F", x - 25, sy);
			}
			if (dnaCore.speedDevil) {
				Fonts.drawText(FontType.DarkPurple, "S", x + 20, sy);
			}
		} else if (dnaCore.charNum == 3) {
			Global.sprites["menu_axl"].drawToHUD(dnaCore.hyperMode == DNACoreHyperMode.WhiteAxl ? 1 : 0, x, sy + 4);
		} else if (dnaCore.charNum == 4) {
			Global.sprites["menu_sigma"].drawToHUD(sigmaForm, x, sy + 10);
		}

		sy += 35;

		var weapons = new List<Weapon>();
		for (int i = 0; i < dnaCore.weapons.Count && i < 6; i++) {
			weapons.Add(dnaCore.weapons[i]);
		}
		if (dnaCore.charNum == (int)CharIds.Zero) {
			if (dnaCore.hyperMode == DNACoreHyperMode.NightmareZero) {
				weapons.Add(new DarkHoldWeapon() { ammo = dnaCore.altCharAmmo });
			} else {
				weapons.Add(RakuhouhaWeapon.getWeaponFromIndex(dnaCore.loadout?.zeroLoadout.gigaAttack ?? 0));
			}
		}
		if (dnaCore.charNum == (int)CharIds.Sigma) {
			if (sigmaForm == 0) weapons.Add(new Weapon() {
				weaponSlotIndex = 111,
				ammo = dnaCore.altCharAmmo,
				maxAmmo = 20,
			});
			if (sigmaForm == 1) weapons.Add(new Weapon() {
				weaponSlotIndex = 110,
				ammo = dnaCore.altCharAmmo,
				maxAmmo = 28,
			});
		}
		int counter = 0;
		float wx = 1 + x - ((weapons.Count - 1) * 8);
		foreach (var weapon in weapons) {
			float slotX = wx + (counter * 15);
			float slotY = sy;
			Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, slotX, slotY);
			float ammo = weapon.ammo;
			if (weapon is RakuhouhaWeapon || weapon is RekkohaWeapon ||
				weapon is Messenkou || weapon is DarkHoldWeapon
			) {
				ammo = dnaCore.altCharAmmo;
			}
			if (weapon is not MechMenuWeapon) {
				DrawWrappers.DrawRectWH(
					slotX - 8, slotY - 8, 16, 16 - MathF.Floor(16 * (ammo / weapon.maxAmmo)
				), true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
			}
			counter++;
		}
	}

	public float getWeaponSlotStartX(Player player, ref int iconW, ref int iconH, ref int width) {
		int weaponCountOff = player.weapons.Count - 1;
		if (mainPlayer.isX && Options.main.gigaCrushSpecial) {
			weaponCountOff = player.weapons.Count((Weapon w) => w is not GigaCrush) - 1;
		}
		int weaponOffset = 0;
		float halfSize = width / 2f;
		if (weaponCountOff + 1 >= 20) {
			width -= weaponCountOff + 1 - 20;
			halfSize = width / 2f;
		}
		if (weaponCountOff > 0) {
			weaponOffset = MathInt.Floor(halfSize * weaponCountOff);
		}

		return Global.halfScreenW - weaponOffset;
	}

	public void drawMaverickCommandIcons() {
		int mwIndex = level.mainPlayer.weapons.IndexOf(level.mainPlayer.weapon);
		float height = 15;
		int width = 20;
		var iconW = 8;
		var iconH = 8;

		float startX = getWeaponSlotStartX(mainPlayer, ref iconW, ref iconH, ref width) + (mwIndex * 20);
		float startY = Global.screenH - 12;

		for (int i = 0; i < MaverickWeapon.maxCommandIndex; i++) {
			float x = startX;
			float y = startY - ((i + 1) * height);
			int index = i;
			Global.sprites["hud_maverick_command"].drawToHUD(index, x, y);
			/*
			if (i == 1)
			{
				Global.sprites["hud_maverick_command"].drawToHUD(3, x - height, y);
				Global.sprites["hud_maverick_command"].drawToHUD(4, x + height, y);
			}
			*/
		}

		for (int i = 0; i < MaverickWeapon.maxCommandIndex + 1; i++) {
			float x = startX;
			float y = startY - (i * height);
			if (level.mainPlayer.maverickWeapon.selCommandIndex == i && level.mainPlayer.maverickWeapon.selCommandIndexX == 1) {
				DrawWrappers.DrawRectWH(x - iconW, y - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
			}
		}

		/*
		if (level.mainPlayer.maverickWeapon.selCommandIndexX == 0)
		{
			DrawWrappers.DrawRectWH(startX - height - iconW, startY - (height * 2) - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
		}
		if (level.mainPlayer.maverickWeapon.selCommandIndexX == 2)
		{
			DrawWrappers.DrawRectWH(startX + height - iconW, startY - (height * 2) - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
		}
		*/
	}

	public void drawRideArmorIcons() {
		int raIndex = mainPlayer.weapons.FindIndex(w => w is MechMenuWeapon);


		float startX = 168;
		if (raIndex == 0) startX = 148;
		if (raIndex == 1) startX = 158;
		if (raIndex == -1) {
			startX = 11;
		}

		float startY = Global.screenH - 12;
		float height = 15;
		Vile vile = level.mainPlayer?.character as Vile;
		bool isMK2 = vile?.isVileMK2 == true;
		bool isMK5 = vile?.isVileMK5 == true;
		bool isMK2Or5 = isMK2 || isMK5;
		int maxIndex = isMK2Or5 ? 5 : 4;

		for (int i = 0; i < maxIndex; i++) {
			float x = startX;
			float y = startY - (i * height);
			int iconIndex = 37 + i;
			if (i == 4 && isMK5) iconIndex = 119;
			Global.sprites["hud_weapon_icon"].drawToHUD(iconIndex, x, y);
		}

		for (int i = 0; i < maxIndex; i++) {
			float x = startX;
			float y = startY - (i * height);
			if (i == 4 && (!isMK2Or5 || level.mainPlayer.currency < 10)) {
				DrawWrappers.DrawRectWH(x - 8, y - 8, 16, 16, true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
			}
		}

		for (int i = 0; i < maxIndex; i++) {
			float x = startX;
			float y = startY - (i * height);
			if (level.mainPlayer.selectedRAIndex == i) {
				DrawWrappers.DrawRectWH(x - 7, y - 7, 14, 14, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
			}
		}
	}

	public Color getPingColor(Player player) {
		Color pingColor = Helpers.getPingColor(player.getPingOrStartPing(), level.server.netcodeModel == NetcodeModel.FavorAttacker ? level.server.netcodeModelPing : Global.defaultThresholdPing);
		if (pingColor == Color.Green) pingColor = Color.White;
		return pingColor;
	}

	public void drawNetcodeData() {
		int top2 = -3;
		string netcodePingStr = "";
		//int iconXPos = 280;
		if (level.server.netcodeModel == NetcodeModel.FavorAttacker) {
			netcodePingStr = " < " + level.server.netcodeModelPing.ToString();
			//if (level.server.netcodeModelPing < 100) iconXPos = 260;
			//else iconXPos = 253;
		}

		if (!Global.level.server.isP2P) {
			Fonts.drawText(
				FontType.DarkPurple, Global.level.server.region?.name ?? "???",
				Global.screenW - 12, top2 + 12, Alignment.Right
			);
		} else {
			Fonts.drawText(
				FontType.DarkPurple,
				"P2P Server on" + Global.level.server.port + netcodePingStr,
				261, top2 + 14, Alignment.Left
			);
		}
		Global.sprites["hud_netcode"].drawToHUD((int)level.server.netcodeModel, 364, top2 + 30);
		if (Global.level.server.isLAN) {
			Fonts.drawText(
				FontType.DarkPurple, "IP: " + Global.level.server.ip,
				Global.screenW - 12, top2 + 32, Alignment.Right
			);
		}
	}

	public virtual void drawScoreboard() {
		int padding = 16;
		int top = 16;
		int col1x = padding;
		int col2x = (int)Math.Floor(Global.screenW * 0.33);
		int col3x = (int)Math.Floor(Global.screenW * 0.475);
		int col4x = (int)Math.Floor(Global.screenW * 0.65);
		int col5x = (int)Math.Floor(Global.screenW * 0.85);
		int lineY = padding + 20;
		var labelTextY = lineY + 2;
		int line2Y = lineY + 12;
		int topPlayerY = line2Y + 2;
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);

		string modeText = this switch {
			MMXOnline.FFADeathMatch => $"FFA (to {playingTo})",
			MMXOnline.Elimination => "Elimination",
			MMXOnline.Race => "Race",
			_ => Global.level.server.gameMode
		};
		Fonts.drawText(FontType.BlueMenu, modeText, padding, top);
		drawMapName(padding, top + 10);
		if (Global.serverClient != null) {
			Fonts.drawText(
				FontType.BlueMenu, "Match: " + Global.level.server.name, padding + 245, top + 10
			);
			drawNetcodeData();
		}

		DrawWrappers.DrawLine(
			padding - 2, lineY, Global.screenW - padding + 2, lineY, new Color(232, 232, 232, 224), 1, ZIndex.HUD, false
		);
		Fonts.drawText(FontType.OrangeMenu, "Player", col1x, labelTextY, Alignment.Left);
		Fonts.drawText(FontType.OrangeMenu, "Char", col2x, labelTextY, Alignment.Left);
		Fonts.drawText(FontType.OrangeMenu, "Kills", col3x, labelTextY, Alignment.Left);
		Fonts.drawText(FontType.OrangeMenu, this is Elimination ? "Lives" : "Deaths", col4x, labelTextY, Alignment.Left);

		if (Global.serverClient != null) {
			Fonts.drawText(FontType.OrangeMenu, "Ping", col5x, labelTextY, Alignment.Left);
		}
		DrawWrappers.DrawLine(
			padding - 2, line2Y, Global.screenW - padding + 2, line2Y, Color.White, 1, ZIndex.HUD, false
		);
		var rowH = 10;
		var players = getOrderedPlayerList();
		if (this is Race race) {
			players = race.getSortedPlayers();
		}
		for (var i = 0; i < players.Count && i <= 14; i++) {
			var player = players[i];
			var color = getCharFont(player);

			if (Global.serverClient != null && player.serverPlayer.isHost) {
				Fonts.drawText(
					FontType.Yellow, "H", col1x - 8, 1 + topPlayerY + i * rowH
				);
			} else if (Global.serverClient != null && player.serverPlayer.isBot) {
				Fonts.drawText(
					FontType.Grey, "B", col1x - 8, 3 + topPlayerY + i * rowH
				);
			}

			Fonts.drawText(color, player.name, col1x, topPlayerY + (i) * rowH, Alignment.Left);
			Fonts.drawText(FontType.Blue, player.kills.ToString(), col3x, topPlayerY + (i) * rowH, Alignment.Left);
			Fonts.drawText(
				FontType.Blue, player.getDeathScore().ToString(),
				col4x, topPlayerY + (i) * rowH, Alignment.Left
			);

			if (Global.serverClient != null) {
				Fonts.drawText(FontType.Blue, player.getDisplayPing(), col5x, topPlayerY + (i) * rowH, Alignment.Left);
			}
			if (player.charNum == (int)CharIds.PunchyZero || player.charNum == (int)CharIds.BusterZero) {
				Global.sprites[getCharIcon(player)].drawToHUD(1, col2x + 4, topPlayerY + i * rowH);
			} else Global.sprites[getCharIcon(player)].drawToHUD(player.realCharNum, col2x + 4, topPlayerY + i * rowH);
		}
		drawSpectators();
	}

	public void drawTeamScoreboard() {
		int padding = 16;
		int top = 16;
		var hPadding = padding + 5;
		var col1x = padding + 5;
		var playerNameX = padding + 15;
		var col2x = col1x - 11;
		var col3x = Global.screenW * 0.28f;
		var col4x = Global.screenW * 0.35f;
		var col5x = Global.screenW * 0.4225f;
		var teamLabelY = padding + 35;
		var lineY = teamLabelY + 10;
		var labelY = lineY + 5;
		var line2Y = labelY + 10;
		var topPlayerY = line2Y + 5;
		var halfwayX = Global.halfScreenW - 2;
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);

		string textMode = this switch {
			MMXOnline.CTF => $" CTF (to {playingTo})",
			MMXOnline.TeamDeathMatch => $"Team Deathmatch (to {playingTo})",
			MMXOnline.TeamElimination => "Team Elimination",
			_ => Global.level.server.gameMode
		};
		Fonts.drawText(FontType.BlueMenu, textMode, padding, top);
		drawMapName(padding, top + 10);

		if (Global.serverClient != null) {
			Fonts.drawText(
				FontType.BlueMenu, "Match: " + Global.level.server.name, padding + 245, top + 10
			);
			drawNetcodeData();
		}

		int redPlayersStillAlive = 0;
		int bluePlayersStillAlive = 0;
		if (this is TeamElimination) {
			redPlayersStillAlive = level.players.Where(
				p => !p.isSpectator && p.deaths < playingTo && p.alliance == redAlliance
			).Count();
			bluePlayersStillAlive = level.players.Where(
				p => !p.isSpectator && p.deaths < playingTo && p.alliance == blueAlliance
			).Count();
		}

		//Blue
		string blueText = this switch {
			ControlPoints => "Blue: Attack",
			MMXOnline.KingOfTheHill => "Blue",
			MMXOnline.TeamElimination => $"Alive: {bluePlayersStillAlive}",
			_ => $"Blue: {Global.level.gameMode.teamPoints[0]}"
		};
		string redText = this switch {
			ControlPoints => "Red: Defend",
			MMXOnline.KingOfTheHill => "Red",
			MMXOnline.TeamElimination => $"Alive: {bluePlayersStillAlive}",
			_ => $"Red: {Global.level.gameMode.teamPoints[1]}"
		};
		(int x, int y)[] positions = {
			(padding - 6, top + 32),
			(padding + 238, top + 32),
			(padding + 116, top + 32),
			(padding - 6, top + 116),
			(padding + 238, top + 116),
			(padding + 116, top + 116),
		};
		drawTeamMiniScore(positions[0], 0, FontType.Blue, blueText);
		drawTeamMiniScore(positions[1], 1, FontType.Red, redText);
		for (int i = 2; i < Global.level.teamNum; i++) {
			drawTeamMiniScore(
				positions[i], i,
				teamFonts[i], $"{teamNames[i]}: {Global.level.gameMode.teamPoints[i]}"
			);
		}
		drawSpectators();
	}

	public void drawTeamMiniScore((int x, int y) pos, int alliance, FontType color, string title) {
		int playersStillAlive = 0;
		bool isTE = false;
		if (this is TeamElimination) {
			isTE = true;
			playersStillAlive = level.players.Where(
				p => !p.isSpectator && p.deaths < playingTo && p.alliance == alliance
			).Count();
		}
		int[] rows = new int[] { pos.y, pos.y + 10, pos.y + 24 };
		int[] cols = new int[] { pos.x, pos.x + 72, pos.x + 88, pos.x + 104 };
		DrawWrappers.DrawRect(
			pos.x - 1, pos.y + 19, pos.x + 120, pos.y + 20, true,
			new Color(255, 255, 255, 128), 0, ZIndex.HUD, false
		);

		Fonts.drawText(color, title, cols[0] + 12, rows[0]);
		Fonts.drawText(FontType.Orange, "Player", cols[0] + 12, rows[1]);
		Fonts.drawText(FontType.Orange, "K", cols[1], rows[1]);
		Fonts.drawText(FontType.Orange, isTE ? "L" : "D", cols[2], rows[1]);
		if (Global.serverClient != null) {
			Fonts.drawText(FontType.Orange, "P", cols[3], rows[1]);
		}
		// Player draw
		Player[] players = level.players.Where(p => p.alliance == alliance && !p.isSpectator).ToArray();
		for (var i = 0; i < players.Length && i <= 14; i++) {
			Player player = players[i];
			int posY = rows[2] + i * 10;
			FontType charColor = getCharFont(player);

			if (Global.serverClient != null && player.serverPlayer.isHost) {
				Fonts.drawText(FontType.Yellow, "H", cols[0] - 8, posY);
			} else if (Global.serverClient != null && player.serverPlayer.isBot) {
				Fonts.drawText(FontType.Grey, "B", cols[0] - 8, posY);
			}

			Global.sprites[getCharIcon(player)].drawToHUD(player.realCharNum, cols[0] + 5, posY - 2);
			Fonts.drawText(charColor, player.name, cols[0] + 12, posY);
			Fonts.drawText(FontType.Blue, player.kills.ToString(), cols[1], posY);
			Fonts.drawText(FontType.Red, player.getDeathScore().ToString(), cols[2], posY);

			if (Global.serverClient != null) {
				Fonts.drawText(FontType.Grey, player.getTeamDisplayPing(), cols[3], posY);
			}
		}
	}

	private void drawMapName(int x, int y) {
		string displayName = "Map: " + level.levelData.displayName.Replace("_mirrored", "");
		Fonts.drawText(FontType.BlueMenu, displayName, x, 27, Alignment.Left);
		if (level.levelData.isMirrored) {
			int size = Fonts.measureText(Fonts.getFontSrt(FontType.BlueMenu), displayName);
			Global.sprites["hud_mirror_icon"].drawToHUD(0, x + size + 9, y + 5);
		}
	}

	public Color getCharColor(Player player) {
		if (player.isDead && !isOver) {
			return new Color(132, 132, 132);
		} else if (player.eliminated()) {
			return new Color(132, 132, 132);
		} else if (player.character is MegamanX) {
			return new Color(132, 206, 255);
		} else if (player.character is Zero) {
			return new Color(255, 115, 115);
		} else if (player.character is Axl) {
			return new Color(206, 156, 255);
		}
		return Color.White;
	}

	public float getCharAlpha(Player player) {
		if (player.isDead && !isOver) {
			return 0.5f;
		} else if (player.eliminated()) {
			return 0.5f;
		}
		return 1;
	}

	public FontType getCharFont(Player player) {
		if (player.eliminated()) {
			return FontType.DarkOrange;
		}
		return (CharIds)player.charNum switch {
			CharIds.X => FontType.Blue,
			CharIds.Zero => FontType.Red,
			CharIds.Axl => FontType.Yellow,
			CharIds.Vile => FontType.Pink,
			CharIds.Sigma => FontType.Green,
			_ => FontType.Grey
		};
	}

	public string getCharIcon(Player player) {
		return "char_icon";
		//if (isOver) return "char_icon";
		//return player.isDead ? "char_icon_dead" : "char_icon";
	}

	public static string getTeamName(int alliance) {
		return alliance switch {
			0 => "Blue",
			1 => "Red",
			2 => "Green",
			3 => "Purple",
			4 => "Yellow",
			5 => "Orange",
			_ => "Error"
		};
	}

	public void drawTimeIfSet(int yPos, float? altTime = null) {
		altTime ??= finalZoneTime ?? remainingTime;
		FontType fontColor = FontType.Grey;
		string timeStr = "";
		if (setupTime > 0) {
			var timespan = new TimeSpan(0, 0, MathInt.Ceiling(setupTime.Value));
			timeStr = timespan.ToString(@"m\:ss");
			fontColor = FontType.OrangeMenu;
		} else if (setupTime == 0 && goTime < 1) {
			goTime += Global.spf;
			timeStr = "GO!";
			fontColor = FontType.RedishOrange;
		} else if (altTime != null) {
			if (altTime <= 10) {
				fontColor = FontType.OrangeMenu;
			}
			var timespan = new TimeSpan(0, 0, MathInt.Ceiling(altTime.Value));
			timeStr = timespan.ToString(@"m\:ss");
			if (virusStarted >= 2) {
				timeStr += " Left";
			}
			if (isOvertime()) {
				timeStr = "Overtime!";
				fontColor = FontType.Red;
			}
		} else {
			return;
		}
		Fonts.drawText(fontColor, timeStr, 5, yPos, Alignment.Left);
	}

	public bool isOvertime() {
		return (this is ControlPoints || this is KingOfTheHill || this is CTF) && remainingTime != null && remainingTime.Value == 0 && !isOver;
	}

	public void drawDpsIfSet(int yPos) {
		if (!string.IsNullOrEmpty(dpsString)) {
			Fonts.drawText(
				FontType.Blue, dpsString, 44, yPos
			);
		}
	}

	public void drawVirusTime(int yPos) {
		var timespan = new TimeSpan(0, 0, MathInt.Ceiling(finalZoneTime ?? 0));
		string timeStr = "Nightmare Virus: " + timespan.ToString(@"m\:ss");
		Fonts.drawText(FontType.Purple, timeStr, 5, yPos, Alignment.Left);
	}

	public void drawWinScreen() {
		string text = "";
		string subtitle = "";
		if (matchOverResponse == null) {
			return;
		}
		if (playerWon(level.mainPlayer)) {
			text = matchOverResponse.winMessage;
			subtitle = matchOverResponse.winMessage2;
		} else {
			text = matchOverResponse.loseMessage;
			subtitle = matchOverResponse.loseMessage2;
		}

		// Title
		float titleY = Global.halfScreenH;
		// Subtitle
		float subtitleY = titleY + 16;
		// Offsets.
		float hh = 8;
		float hh2 = 16;
		if (string.IsNullOrEmpty(subtitle)) {
			subtitleY = titleY;
		}
		int offset = MathInt.Floor(((subtitleY + hh2) - (titleY - hh)) / 2);
		titleY -= offset;
		subtitleY -= offset;

		// Box
		DrawWrappers.DrawRect(
			0, titleY - hh,
			Global.screenW, subtitleY + hh2,
			true, new Color(0, 0, 0, 192), 1, ZIndex.HUD,
			isWorldPos: false, outlineColor: Color.White
		);

		// Title
		Fonts.drawText(
			FontType.Grey, text.ToUpperInvariant(),
			Global.halfScreenW, titleY, Alignment.Center
		);

		// Subtitle
		Fonts.drawText(
			FontType.Grey, subtitle,
			Global.halfScreenW, subtitleY, Alignment.Center
		);

		if (overTime >= secondsBeforeLeave) {
			if (Global.serverClient == null) {
				Fonts.drawText(
					FontType.OrangeMenu, Helpers.controlText("Press [ESC] to return to menu"),
					Global.halfScreenW, subtitleY + hh2 + 16, Alignment.Center
				);
			}
		}
	}

	public virtual void drawTopHUD() {

	}

	public void drawRespawnHUD() {
		string respawnStr = (
			(level.mainPlayer.respawnTime > 0) ?
			"Respawn in " + Math.Round(level.mainPlayer.respawnTime).ToString() :
			Helpers.controlText("Press [OK] to respawn")
		);
		if (level.mainPlayer.character != null && level.mainPlayer.readyTextOver) {
			if (level.mainPlayer.canReviveX()) {
				Fonts.drawTextEX(
					FontType.Blue, Helpers.controlText("[CMD]: Activate Raging Charge"),
					Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center
				);
			}
			#region  ReviveVile HUD
			if (level.mainPlayer.canReviveVile()) {
				if (level.mainPlayer.lastDeathWasVileMK2) {
					string reviveText = Helpers.controlText(
						$"[SPC]: Revive as Vile V (5 {Global.nameCoins})"
					);
					Fonts.drawText(
						FontType.Green, reviveText,
						Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center
					);
				} else {
					string reviveText = Helpers.controlText(
						$"[SPC]: Revive as MK-II (5 {Global.nameCoins})"
					);
					Fonts.drawText(
						FontType.DarkBlue, reviveText,
						Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center
					);
					string reviveText2 = Helpers.controlText(
						$"[CMD]: Revive as Vile V (5 {Global.nameCoins})"
					);
					Fonts.drawText(
						FontType.Green, reviveText2,
						Global.screenW / 2, 22 + Global.screenH / 2, Alignment.Center
					);
				}
			#endregion
				#region  ReviveSigma HUD
			} else if (level.mainPlayer.canReviveSigma(out _, 2)) {
				string hyperType = "Kaiser";
				string reviveText = (
					$"[CMD]: Revive as {hyperType} Sigma ({Player.reviveSigmaCost.ToString()} {Global.nameCoins})"
				);
				Fonts.drawTextEX(
					FontType.Green, reviveText,
					Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center
				);
				#endregion
			}
		}

		if (level.mainPlayer.randomTip == null) return;
		if (level.mainPlayer.isSpectator) return;

		bool drawTip = true;
		bool drawMainBg = false;
		FontType respawnFont = FontType.BlueMenu;

		if (!canRespawn()) {
			if (level.mainPlayer.respawnTime > 0) {
				respawnStr = "You Died";
				respawnFont = FontType.Red;
				drawMainBg = true;
			} else {
				respawnStr = "";
				drawTip = false;
			}
		}

		if (level.mainPlayer.character == null && level.mainPlayer.readyTextOver) {
			if (level.mainPlayer.eliminated()) {
				Fonts.drawText(
					FontType.Red, "You were eliminated!",
					Global.screenW / 2, -15 + Global.screenH / 2, Alignment.Center
				);
				Fonts.drawText(
					FontType.BlueMenu, "Spectating in " + Math.Round(level.mainPlayer.respawnTime),
					Global.screenW / 2, Global.screenH / 2, Alignment.Center
				);
			}

			if (drawMainBg) {
				DrawWrappers.DrawRect(
					-20, Global.halfScreenH - 14,
					Global.screenW + 20, Global.halfScreenH + 1,
					true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false, new Color(255, 255, 255, 128)
				);
			}

			if (respawnStr != "") {
				Fonts.drawText(
					respawnFont, respawnStr,
					Global.screenW / 2, -10 + Global.screenH / 2, Alignment.Center
				);
			}
			if (!Menu.inMenu && drawTip) {
				DrawWrappers.DrawRect(
					0, Global.halfScreenH + 40, Global.screenW,
					Global.halfScreenH + 40 + (14 * level.mainPlayer.randomTip.Length),
					true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false
				);
				for (int i = 0; i < level.mainPlayer.randomTip.Length; i++) {
					var line = level.mainPlayer.randomTip[i];
					if (i == 0) line = "Tip: " + line;
					Fonts.drawText(
						FontType.BlueMenu, line,
						Global.screenW / 2, (Global.screenH / 2) + 45 + (12 * i), Alignment.Center
					);
				}
			}
		}
	}

	public bool playerWon(Player player) {
		if (!isOver ||
			matchOverResponse?.winningAlliances == null
		) {
			return false;
		}
		return matchOverResponse.winningAlliances.Contains(player.alliance);
	}

	public void onMatchOver() {
		if (level.mainPlayer != null && playerWon(level.mainPlayer)) {
			Global.changeMusic(Global.level.levelData.getWinTheme());
		} else if (level.mainPlayer != null && !playerWon(level.mainPlayer)) {
			Global.changeMusic(Global.level.levelData.getLooseTheme());
		}
		if (Menu.inMenu) {
			Menu.exit();
		}
		logStats();
	}

	public void matchOverRpc(RPCMatchOverResponse matchOverResponse) {
		if (this.matchOverResponse == null) {
			this.matchOverResponse = matchOverResponse;
			onMatchOver();
		}
	}

	public void logStats() {
		loggedStatsOnce = true;
		if (loggedStatsOnce) return;

		if (Global.serverClient == null) {
			return;
		}
		if (level.isTraining()) {
			return;
		}
		bool is1v1 = level.is1v1();
		var nonSpecPlayers = Global.level.nonSpecPlayers();
		int botCount = nonSpecPlayers.Count(p => p.isBot);
		int nonBotCount = nonSpecPlayers.Count(p => !p.isBot);
		if (botCount >= nonBotCount) return;
		Player mainPlayer = level.mainPlayer;
		string mainPlayerCharName = getLoggingCharNum(mainPlayer, is1v1);

		if (this is FFADeathMatch && !mainPlayer.isSpectator && isFairDeathmatch(mainPlayer)) {
			long val = playerWon(mainPlayer) ? 100 : 0;
			Logger.logEvent(
				"dm_win_rate", mainPlayerCharName, val, forceLog: true
			);
			Logger.logEvent(
				"dm_unique_win_rate_" + mainPlayerCharName,
				Global.deviceId + "_" + mainPlayer.name, val, forceLog: true
			);
		}

		if (is1v1 && !mainPlayer.isSpectator && !isMirrorMatchup()) {
			long val = playerWon(mainPlayer) ? 100 : 0;
			Logger.logEvent("1v1_win_rate", mainPlayerCharName, val, forceLog: true);
		}

		if (!is1v1 && (mainPlayer.kills > 0 || mainPlayer.deaths > 0 || mainPlayer.assists > 0)) {
			Logger.logEvent(
				"kill_stats_v2", mainPlayerCharName +
				":kills", mainPlayer.kills, forceLog: true
			);
			Logger.logEvent(
				"kill_stats_v2", mainPlayerCharName +
				":deaths", mainPlayer.deaths, forceLog: true
			);
			Logger.logEvent(
				"kill_stats_v2", mainPlayerCharName +
				":assists", mainPlayer.assists, forceLog: true
			);
		}

		if (!is1v1 && Global.isHost && matchOverResponse != null) {
			if (isTeamMode && !level.levelData.isMirrored && (this is CTF || this is ControlPoints)) {
				long val;
				if (matchOverResponse.winningAlliances.Contains(blueAlliance)) val = 100;
				else if (matchOverResponse.winningAlliances.Contains(redAlliance)) val = 0;
				else {
					Logger.logEvent(
						"map_stalemate_rates",
						level.levelData.name + ":" + level.server.gameMode,
						100, false, true
					);
					return;
				}
				Logger.logEvent(
					"map_win_rates",
					level.levelData.name + ":" + level.server.gameMode,
					val, false, true
				);
				Logger.logEvent(
					"map_stalemate_rates", level.levelData.name + ":" +
					level.server.gameMode, 0, false, true
				);
			}
		}
	}

	public bool isMirrorMatchup() {
		var nonSpecPlayers = Global.level.nonSpecPlayers();
		if (nonSpecPlayers.Count != 2) return false;
		if (nonSpecPlayers[0].charNum != nonSpecPlayers[1].charNum) {
			return true;
		} else {
			if (nonSpecPlayers[0].charNum == 0 && nonSpecPlayers[0].armorFlag != nonSpecPlayers[1].armorFlag) {
				return true;
			}
			return false;
		}
	}

	public bool isFairDeathmatch(Player mainPlayer) {
		int kills = mainPlayer.charNumToKills.GetValueOrDefault(mainPlayer.realCharNum);
		if (kills < mainPlayer.kills / 2) return false;
		if (kills < 10) return false;
		return true;
	}

	public string getLoggingCharNum(Player player, bool is1v1) {
		int charNum = player.realCharNum;
		string charName;
		if (charNum == 0) {
			charName = "X";
			if (is1v1) {
				if (player.legArmorNum == 1) charName += "1";
				else if (player.legArmorNum == 2) charName += "2";
				else if (player.legArmorNum == 3) charName += "3";
			}
		} else if (charNum == 1) charName = "Zero";
		else if (charNum == 2) charName = "Vile";
		else if (charNum == 3) {
			if (Options.main.axlAimMode == 2) charName = "AxlCursor";
			else if (Options.main.axlAimMode == 1) charName = "AxlAngular";
			else charName = "AxlDirectional";
		} else if (charNum == 4) {
			if (Options.main.sigmaLoadout.commandMode == 0) charName = "SigmaSummoner";
			else if (Options.main.sigmaLoadout.commandMode == 1) charName = "SigmaPuppeteer";
			else if (Options.main.sigmaLoadout.commandMode == 2) charName = "SigmaStriker";
			else charName = "SigmaTagTeam";
		} else charName = null;

		return charName;
	}

	public void drawTeamTopHUD() {
		int teamSide = Global.level.mainPlayer.teamAlliance ?? -1;
		if (teamSide < 0 || Global.level.mainPlayer.isSpectator) {
			drawAllTeamsHUD();
			return;
		}
		int maxTeams = Global.level.teamNum;

		string teamText = $"{teamNames[teamSide]}: {teamPoints[teamSide]}";
		Fonts.drawText(teamFonts[teamSide], teamText, 5, 5);

		int leaderTeam = 0;
		int leaderScore = -1;
		bool moreThanOneLeader = false;
		for (int i = 0; i < Global.level.teamNum; i++) {
			if (teamPoints[0] >= leaderScore) {
				leaderTeam = i;
				if (leaderScore == teamPoints[i]) {
					moreThanOneLeader = true;
				}
				leaderScore = teamPoints[i];
			}
		}
		if (!moreThanOneLeader) {
			Fonts.drawText(teamFonts[leaderTeam], $"Leader: {leaderScore}", 5, 15);
		} else {
			Fonts.drawText(FontType.Grey, $"Leader: {leaderScore}", 5, 15);
		}
		drawTimeIfSet(25);
	}

	public void drawAllTeamsHUD() {
		for (int i = 0; i < Global.level.teamNum; i++) {
			Fonts.drawText(teamFonts[i], $"{teamNames[i]}: {teamPoints[i]}", 5, 5 + i * 10);
		}
		drawTimeIfSet(5 + 10 * (Global.level.teamNum + 1));
	}

	public void addMapNavpoint(Point objPos, (string name, int index)? spr = null) {
		navPoints.Add((objPos.round(), spr?.name ?? "hud_minimap_arrow", spr?.index ?? 0));
	}

	public void drawObjectiveNavpoint(string label, Point objPos) {
		if (level.mainPlayer.character == null) return;
		if (!string.IsNullOrEmpty(label)) label += ":";

		Point playerPos = level.mainPlayer.character.pos;

		var line = new Line(playerPos, objPos);
		var camRect = new Rect(
			level.camX, level.camY, level.camX + Global.viewScreenW, level.camY + Global.viewScreenH
		);

		var intersectionPoints = camRect.getShape().getLineIntersectCollisions(line);
		Point? intersectPointNull = null;
		if (intersectionPoints.Count > 0) {
			intersectPointNull = intersectionPoints[0].hitData?.hitPoint.GetValueOrDefault();
		}
		if (intersectPointNull == null) {
			return;
		}
		Point intersectPoint = intersectPointNull.Value;
		Point dirTo = playerPos.directionTo(objPos).normalize();

		//a = arrow, l = length, m = minus
		float al = 10 / Global.viewSize;
		float alm1 = 9 / Global.viewSize;
		float alm2 = 8 / Global.viewSize;
		float alm3 = 7 / Global.viewSize;
		float alm4 = 5 / Global.viewSize;

		intersectPoint.inc(dirTo.times(-10));
		var posX = intersectPoint.x - Global.level.camX;
		var posY = intersectPoint.y - Global.level.camY;

		posX /= Global.viewSize;
		posY /= Global.viewSize;

		DrawWrappers.DrawLine(
			posX, posY,
			posX + dirTo.x * al, posY + dirTo.y * al,
			Helpers.getAllianceColor(), 1, ZIndex.HUD, false
		);
		DrawWrappers.DrawLine(
			posX + dirTo.x * alm4, posY + dirTo.y * alm4,
			posX + dirTo.x * alm3, posY + dirTo.y * alm3,
			Helpers.getAllianceColor(), 4, ZIndex.HUD, false
		);
		DrawWrappers.DrawLine(
			posX + dirTo.x * alm3, posY + dirTo.y * alm3,
			posX + dirTo.x * alm2, posY + dirTo.y * alm2,
			Helpers.getAllianceColor(), 3, ZIndex.HUD, false
		);
		DrawWrappers.DrawLine(
			posX + dirTo.x * alm2, posY + dirTo.y * alm2,
			posX + dirTo.x * alm1, posY + dirTo.y * alm1,
			Helpers.getAllianceColor(), 2, ZIndex.HUD, false
		);

		decimal distInMeters = Math.Floor(Math.Abs((decimal)objPos.distanceTo(playerPos) / 24m));
		bool isLeft = posX < Global.viewScreenW / 2;
		Fonts.drawText(
			FontType.BlueMenu, $"{label} {distInMeters}m",
			posX, posY - 4, isLeft ? Alignment.Left : Alignment.Right
		);
	}

	public void syncTeamScores() {
		if (Global.isHost) {
			Global.serverClient?.rpc(RPC.syncTeamScores, teamPoints);
		}
	}

	public virtual bool canRespawn() {
		return true;
	}

	public virtual bool forceRespawn() {
		return false;
	}
}
