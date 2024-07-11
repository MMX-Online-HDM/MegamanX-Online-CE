/*
using System;

namespace MMXOnline;

public enum NetCharBoolStateNum {
	One,
	Two
}

public class NetCharBoolState {
	private Character character;
	private int byteIndex;
	private Func<Character, bool> getBSValue;
	public NetCharBoolStateNum netCharStateNum;

	public NetCharBoolState(Character character, int byteIndex, NetCharBoolStateNum netCharStateNum, Func<Character, bool> getBSValue) {
		this.character = character;
		this.byteIndex = byteIndex;
		this.getBSValue = getBSValue;
		this.netCharStateNum = netCharStateNum;
	}

	public bool getValue() {
		if (character.ownedByLocalPlayer) {
			return getBSValue(character);
		}
		if (netCharStateNum == NetCharBoolStateNum.One) {
			return Helpers.getByteValue(character.netCharState1, byteIndex);
		} else {
			return Helpers.getByteValue(character.netCharState2, byteIndex);
		}
	}

	public void updateValue() {
		if (netCharStateNum == NetCharBoolStateNum.One) {
			Helpers.setByteValue(ref character.netCharState1, byteIndex, getValue());
		} else {
			Helpers.setByteValue(ref character.netCharState2, byteIndex, getValue());
		}
	}
}

public partial class Character {
	// NET CHAR STATE 1 SECTION
	public byte netCharState1;

	public NetCharBoolState isFrozenCastleActiveBS = null!;
	public NetCharBoolState isStrikeChainHookedBS = null!;
	public NetCharBoolState shouldDrawArmBS = null!;
	public NetCharBoolState isInvisibleBS = null!;
	public NetCharBoolState isHyperXBS = null!;
	public NetCharBoolState isHyperSigmaBS = null!;

	public void initNetCharState1() {
		isFrozenCastleActiveBS = new NetCharBoolState(this, 0, NetCharBoolStateNum.One, (character) => {
			if (character is not Vile vile) {
				return false;
			}
			return vile.hasFrozenCastleBarrier();
		});
		isStrikeChainHookedBS = new NetCharBoolState(this, 1, NetCharBoolStateNum.One, (character) => { return character.charState is StrikeChainHooked; });
		shouldDrawArmBS = new NetCharBoolState(this, 2, NetCharBoolStateNum.One, (character) => {
			return (character as Axl)?.shouldDrawArm() == true;
		});
		isInvisibleBS = new NetCharBoolState(this, 5, NetCharBoolStateNum.One, (character) => { return character.isInvisible(); });
		isHyperXBS = new NetCharBoolState(this, 6, NetCharBoolStateNum.One, (character) => {
			return (character as MegamanX)?.isHyperX == true;
		});
		isHyperSigmaBS = new NetCharBoolState(this, 7, NetCharBoolStateNum.One, (character) => {
			if (character is KaiserSigma) {
				return true;
			}
			return (character as BaseSigma)?.isHyperSigma == true;
		});
	}

	public byte updateAndGetNetCharState1() {
		isFrozenCastleActiveBS.updateValue();
		isStrikeChainHookedBS.updateValue();
		shouldDrawArmBS.updateValue();
		isInvisibleBS.updateValue();
		isHyperXBS.updateValue();
		isHyperSigmaBS.updateValue();
		return netCharState1;
	}

	// NET CHAR STATE 2 SECTION
	public byte netCharState2;

	public NetCharBoolState isHyperChargeActiveBS = null!;
	public NetCharBoolState isSpeedDevilActiveBS = null!;
	public NetCharBoolState isInvulnBS = null!;
	public NetCharBoolState hasUltimateArmorBS = null!;
	public NetCharBoolState isDefenderFavoredBS = null!;
	public NetCharBoolState hasSubtankCapacityBS = null!;
	public NetCharBoolState isDarkHoldBS = null!;

	public void initNetCharState2() {
		isHyperChargeActiveBS = new NetCharBoolState(this, 0, NetCharBoolStateNum.Two, (character) => { return character.player.showHyperBusterCharge(); });
		isSpeedDevilActiveBS = new NetCharBoolState(this, 1, NetCharBoolStateNum.Two, (character) => { return character.player.speedDevil; });
		isInvulnBS = new NetCharBoolState(this, 2, NetCharBoolStateNum.Two, (character) => { return character.invulnTime > 0; });
		hasUltimateArmorBS = new NetCharBoolState(this, 3, NetCharBoolStateNum.Two, (character) => { return character.player.hasUltimateArmor(); });
		isDefenderFavoredBS = new NetCharBoolState(this, 4, NetCharBoolStateNum.Two, (character) => { return character.player.isDefenderFavored; });
		hasSubtankCapacityBS = new NetCharBoolState(this, 5, NetCharBoolStateNum.Two, (character) => { return character.player.hasSubtankCapacity(); });
		isDarkHoldBS = new NetCharBoolState(this, 7, NetCharBoolStateNum.Two, (character) => { return character.charState is DarkHoldState; });
	}

	public byte updateAndGetNetCharState2() {
		isHyperChargeActiveBS.updateValue();
		isSpeedDevilActiveBS.updateValue();
		isInvulnBS.updateValue();
		hasUltimateArmorBS.updateValue();
		isDefenderFavoredBS.updateValue();
		hasSubtankCapacityBS.updateValue();
		isDarkHoldBS.updateValue();
		return netCharState2;
	}
}
*/
