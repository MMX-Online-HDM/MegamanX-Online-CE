using Newtonsoft.Json;

namespace MMXOnline;

public class ChatEntry {
	public string message;
	public int? alliance;
	public float time;
	public string sender;
	public bool alwaysShow;
	public bool isSpectator;

	public ChatEntry() { }

	public ChatEntry(string message, string sender, int? alliance, bool alwaysShow, bool isSpectator = false) {
		this.message = message;
		this.sender = sender;
		this.alliance = alliance;
		this.alwaysShow = alwaysShow;
		this.isSpectator = isSpectator;
	}

	public string getDisplayMessage() {
		if (string.IsNullOrEmpty(sender)) {
			return message;
		}
		string teamMsgPart = alliance != null ? "(team)" : "";
		if (isSpectator) teamMsgPart = "(spectator)";
		return sender + teamMsgPart + ": " + message;
	}

	public void sendRpc() {
		var json = JsonConvert.SerializeObject(this);
		Global.serverClient?.rpc(RPC.sendChatMessage, json);
	}
}
