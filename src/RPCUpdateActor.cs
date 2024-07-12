using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;

namespace MMXOnline;

public partial class Actor {
	public virtual List<byte>? getCustomActorNetData() {
		return null;
	}
	public virtual void updateCustomActorNetData(byte[] data) { }

	public virtual void sendActorNetData() {
		byte[] networkIdBytes = Helpers.convertToBytes((ushort)netId);
		if ((netId == 10 || netId == 11) && this is not Flag) {
			//string msg = string.Format(
			//"NetId {0} was not flag. Was {1}", netId.Value.ToString(), this.GetType().ToString()
			//);
			//Logger.logException(new Exception(msg), false);
			return;
		}

		var args = new List<byte>() { networkIdBytes[0], networkIdBytes[1] };

		ushort spriteIndex = Global.spriteIndexByName.GetValueOrCreate(sprite.name, ushort.MaxValue);

		List<bool> mask = new List<bool>();
		for (int i = 0; i < 8; i++) mask.Add(false);

		// These masks are for whether to send the following fields or not.
		mask[0] = !isStatic;                    // pos x
		mask[1] = !isStatic;                    // pos y
		mask[2] = syncScale;                    // scale data
		mask[3] = (sprite.frames.Count > 1);    // frame index data
		mask[4] = byteAngle != null;                // angle

		// The rest are just always sent and contain actual bool data
		mask[5] = visible;                      // visibility
		mask[6] = xDir == -1 ? false : true;    // xDir
		mask[7] = yDir == -1 ? false : true;    // yDir

		// add the mask
		byte maskByte = Convert.ToByte(string.Join("", mask.Select(b => b ? 1 : 0)), 2);
		args.Add(maskByte);

		// add pos x
		if (mask[0]) {
			byte[] xBytes = BitConverter.GetBytes(pos.x);
			args.AddRange(xBytes);
		}
		// add pos y
		if (mask[1]) {
			byte[] yBytes = BitConverter.GetBytes(pos.y);
			args.AddRange(yBytes);
		}
		// add the scale bytes
		if (mask[2]) {
			args.Add((byte)(int)(xScale * 20));
			args.Add((byte)(int)(yScale * 20));
		}
		// add the frame index byte
		if (mask[3]) {
			args.Add((byte)frameIndex);
		}
		// add angle
		if (mask[4]) {
			byte[] angleBytes = BitConverter.GetBytes(byteAngle ?? 0);
			args.AddRange(angleBytes);
		}

		// Sprite index (always sent)
		byte[] spriteBytes = BitConverter.GetBytes((ushort)spriteIndex);
		args.AddRange(spriteBytes);

		List<byte>? customData = getCustomActorNetData();
		if (customData != null) {
			args.AddRange(customData);
		}

		Global.serverClient?.rpc(RPC.updateActor, args.ToArray());

		lastPos = pos;
		lastSpriteIndex = spriteIndex;
		lastFrameIndex = frameIndex;
		lastXDir = xDir;
		lastYDir = yDir;
		lastAngle = byteAngle;
	}
}

public class RPCUpdateActor : RPC {
	public RPCUpdateActor() {
		netDeliveryMethod = NetDeliveryMethod.Unreliable;
	}

	public override void invoke(params byte[] arguments) {
		if (Global.level == null || !Global.level.started) return;

		int i = 0;

		ushort netId = BitConverter.ToUInt16(new byte[] { arguments[i], arguments[i + 1] }, 0);
		byte mask = arguments[i + 2];

		i += 3;

		var maskBools = Convert.ToString(mask, 2).Select(s => s.Equals('1')).ToList();
		while (maskBools.Count < 8) {
			maskBools.Insert(0, false);
		}

		float? xPos = null;
		float? yPos = null;
		float? xScale = null;
		float? yScale = null;
		int? spriteIndex = null;
		int? frameIndex = null;
		bool visible = maskBools[5];
		int? xDir = maskBools[6] ? 1 : -1;
		int? yDir = maskBools[7] ? 1 : -1;
		float? byteAngle = null;

		if (maskBools[0]) {
			xPos = BitConverter.ToSingle(
				new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0
			);
			i += 4;
		}
		if (maskBools[1]) {
			yPos = BitConverter.ToSingle(
				new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0
			);
			i += 4;
		}
		if (maskBools[2]) {
			xScale = arguments[i++] / 20f;
			yScale = arguments[i++] / 20f;
		}
		if (maskBools[3]) {
			frameIndex = arguments[i];
			i++;
		}
		if (maskBools[4]) {
			byteAngle = BitConverter.ToSingle(
				new byte[] { arguments[i], arguments[i + 1], arguments[i + 2], arguments[i + 3] }, 0
			);
			i += 4;
		}

		spriteIndex = BitConverter.ToUInt16(new byte[] { arguments[i], arguments[i + 1] }, 0);
		i += 2;

		Actor? actor = Global.level.getActorByNetId(netId, true);

		if (actor == null) {
			int? playerId = Player.getPlayerIdFromCharNetId(netId);
			if (playerId != null) {
				var player = Global.level.getPlayerById(playerId.Value);
				if (player != null) {
					Global.level.addFailedSpawn(
						playerId.Value, new Point(xPos ?? 0, yPos ?? 0), xDir ?? 1, netId
					);
				}
			}
			return;
		}

		try {
			if (actor != null && !actor.ownedByLocalPlayer) {
				// In case we are updating a local object.
				actor.forceNetUpdateNextFrame = true;
				// Update data.
				if (spriteIndex != null) actor.netSpriteIndex = (int)spriteIndex;
				if (xPos != null) actor.netXPos = (float)xPos;
				if (yPos != null) actor.netYPos = (float)yPos;
				//actor.netIncPos = actor.netPos.subtract(actor.pos).times(0.33f);
				if (frameIndex != null) actor.netFrameIndex = (int)frameIndex;
				if (xDir != null) actor.netXDir = (int)xDir;
				if (yDir != null) actor.netYDir = (int)yDir;
				if (byteAngle != null) actor.netAngle = (float)byteAngle;
				if (xScale != null) actor.xScale = xScale.Value;
				if (yScale != null) actor.yScale = yScale.Value;

				actor.visible = visible;
				actor.lastNetUpdate = Global.time;
			}
		} catch (IndexOutOfRangeException) {
			string msg = string.Format("Index out of bounds. Actor type: {0}, args len: {1}, i: {2}, netId: {3}",
				actor.GetType().ToString(), arguments.Length.ToString(), i.ToString(), netId.ToString());
			throw new Exception(msg);
		}

		if (actor != null) {
			// We send custom data here.
			if (i < arguments.Length) {
				actor.updateCustomActorNetData(arguments[i..]);
			}
		}
	}
}
