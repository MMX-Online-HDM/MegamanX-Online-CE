using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;

namespace MMXOnline;

public partial class Actor {
	public virtual List<byte> getCustomActorNetData() { return []; }
	public virtual void updateCustomActorNetData(byte[] data) { }

	public void sendActorNetData() {
		if (netId == null) {
			if (this is Character) {
				throw new Exception($"Error character {getActorTypeName()} has a null net id");
			}
			return;
		}
		if (netId < Level.firstNormalNetId && this is not Flag and not ControlPoint) {
			string msg = $"NetId {netId.Value} was not flag or system object. Was {getActorTypeName()}";
			throw new Exception(msg);
		}
		byte[] networkIdBytes = Helpers.convertToBytes(netId.Value);
		var args = new List<byte>() { networkIdBytes[0], networkIdBytes[1] };

		ushort spriteIndex = Global.spriteIndexByName.GetValueOrCreate(sprite.name, ushort.MaxValue);

		List<bool> mask = new List<bool>();
		for (int i = 0; i < 8; i++) mask.Add(false);

		// These masks are for whether to send the following fields or not.
		mask[0] = !isStatic; // Pos
		mask[1] = syncScale; // Scale data
		mask[2] = sprite.totalFrameNum != 0; // Frame index data
		mask[3] = angleSet; // Angle

		// The rest are just always sent and contain actual bool data
		mask[5] = visible; // Visibility
		mask[6] = xDir > -1; // xDir
		mask[7] = yDir > -1; // yDir

		// add the mask
		byte maskByte = Convert.ToByte(string.Join("", mask.Select(b => b ? 1 : 0)), 2);
		args.Add(maskByte);

		// Pos.
		if (mask[0]) {
			byte[] xBytes = BitConverter.GetBytes(pos.x);
			args.AddRange(xBytes);
			byte[] yBytes = BitConverter.GetBytes(pos.y);
			args.AddRange(yBytes);
		}
		// Scale.
		if (mask[1]) {
			args.Add((byte)(int)(xScale * 20));
			args.Add((byte)(int)(yScale * 20));
		}
		// Frame index byte
		if (mask[2]) {
			args.Add((byte)frameIndex);
		}
		// Angle
		if (mask[3]) {
			int angleToSend = MathInt.Round(byteAngle) % 256;
			args.Add((byte)angleToSend);
		}

		// Sprite index (always sent)
		byte[] spriteBytes = BitConverter.GetBytes(spriteIndex);
		args.AddRange(spriteBytes);

		List<byte>? customData = getCustomActorNetData();
		if (customData != null) {
			args.AddRange(customData);
		}

		Global.serverClient?.rpc(RPC.updateActor, args.ToArray());
	}
}

public class RPCUpdateActor : RPC {
	public RPCUpdateActor() {
		netDeliveryMethod = NetDeliveryMethod.Unreliable;
		isPreUpdate = true;
	}

	public override void invoke(params byte[] arguments) {
		if (Global.level == null || !Global.level.started) return;

		// Actor ID. Return if does not exist or we own it.
		ushort netId = BitConverter.ToUInt16(arguments.AsSpan()[0..2]);
		Actor? actor = Global.level.getActorByNetId(netId, true);
		if (actor == null || actor.ownedByLocalPlayer) {
			return;
		};
		int i = 2;
		byte maskByte = arguments[i];
		bool[] mask = Helpers.byteToBoolArray(arguments[i]);
		i++;

		// General flags.
		if (mask[0]) {
			actor.netXPos = BitConverter.ToSingle(arguments.AsSpan()[i..(i+4)]);
			i += 4;
			actor.netYPos = BitConverter.ToSingle(arguments.AsSpan()[i..(i+4)]);
			i += 4;
			//actor.netIncPos = actor.netPos.subtract(actor.pos).times(0.33f);
		}
		if (mask[1]) {
			actor.xScale = arguments[i++] / 20f;
			actor.yScale = arguments[i++] / 20f;
		}
		if (mask[2]) {
			actor.netFrameIndex = arguments[i++];
		}
		if (mask[3]) {
			actor.netAngle = arguments[i++];
		}
		// Spriteindex.
		actor.netSpriteIndex = BitConverter.ToUInt16(arguments.AsSpan()[i..(i+2)]);
		i += 2;
		// Other flags.
		actor.visible = mask[5];
		actor.netXDir = mask[6] ? 1 : -1;
		actor.netYDir = mask[7] ? 1 : -1;
		actor.lastNetUpdate = Global.time;
		actor.lastNetFrame = Global.frameCount;

		try {
			// We send custom data here.
			if (i < arguments.Length) {
				actor.updateCustomActorNetData(arguments[i..]);
			}
		} catch {
			string playerName = "null";
			if (actor is Character character) {
				playerName = character.player.name;
			}
			else if (actor.netOwner?.name != null) {
				playerName = actor.netOwner.name;
			}
			Program.exceptionExtraData = (
				"Index out of bounds.\n" + 
				$"actor type: {actor.getActorTypeName()}, " +
				$"player: {playerName}, " +
				$"args len: {arguments.Length}, " +
				$"extraArgs pos/len: {i} {arguments.Length - i}, " + 
				$"netId: {netId}, " +
				$"maskBool: {Convert.ToString(maskByte, 2).PadLeft(8, '0')}"
			);
			if (actor is Character chara) {
				Program.exceptionExtraData += $", playerData len {arguments[i]}, " +
					$"playerData len {arguments[i]}, " +
					$"charId {(CharIds)arguments[i + 7]}, " +
					$"isMainChar {chara.player.character == chara}, " +
					$"playerCharId {chara.player.charNum}"
				;
			}

			throw;
		}
	}
}
