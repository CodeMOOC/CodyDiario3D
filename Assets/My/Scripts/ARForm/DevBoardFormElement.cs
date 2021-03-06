﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ARFormOptions
{

public class DevBoardFormElement : ARFormElement {

	public const int cardsNumber = 3;
	public enum CardType {
		Left,
		Forward,
		Right,
		Null
	}

	public int[] CardRows = new int[cardsNumber];

	public int colStarting = 6;
	public int colDistance = 2;

	public int rowLeftCard = 7;
	public int rowForwardCard = 5;
	public int rowRightCard = 3;

	public int boadLength = 25;

	[Range(0f, 1f)]
	public float minColorIntensityDifference = 0.06f;

	public Text uiPreviewValues;
	public Text uiSavedValues;

	//public CodingGrid codingGrid;

	float bestColorIntensity;

	CardType bestCard;
	CardType[] bestCards;

	public event Action<string> OnSendToCodingGrid;
	
	private string GetCardChar(CardType card) {
		switch (card) {
			case CardType.Left: return "S";
			case CardType.Forward: return "A";
			case CardType.Right: return "D";
		}
		return "_";
	}


    ARFormElementValue<CardType>[] cardsValues;

	public void Awake() {
		CardRows[(int)CardType.Left] = rowLeftCard;
		CardRows[(int)CardType.Forward] = rowForwardCard;
		CardRows[(int)CardType.Right] = rowRightCard;

		bestCards = new CardType[boadLength];
		cardsValues = new ARFormElementValue<CardType>[boadLength];

		for (int i = 0; i < boadLength; i++) {
			cardsValues[i] = new ARFormElementValue<CardType>(CardType.Null);
		}
	}

	public override void CheckValues() {

        float bestColorIntensity;
        float avgColorIntensity;

		// @tmp:
		StringBuilder sb = new StringBuilder();

		int i;
        for (i = 0; i < boadLength; i++) {
			bestCard = CardType.Null;
			bestColorIntensity = 1f;
			avgColorIntensity = 0f;
			for (var j = 0; j < cardsNumber; j++) {
				float c = formContainer.GetAvgGrayscale(new Vec2(i * colDistance + colStarting, (int)CardRows[j]));
				avgColorIntensity += c;
				if (IsBetter(c, bestColorIntensity)) {
					bestColorIntensity = c;
					bestCard = (CardType)j;
				}
			}
			avgColorIntensity /= cardsNumber;

			// /*
			bool hasBestValue = false;
			CardType bestAvgCard = CardType.Null;
			if (cardsValues[i].TryGetValue(out bestAvgCard) && bestAvgCard != CardType.Null) {
				sb.Append(GetCardChar(bestAvgCard));
				sb.Append(" ");
				hasBestValue = true;
			}
			// */
			if (bestCard != CardType.Null && minColorIntensityDifference <= (avgColorIntensity - bestColorIntensity)) {
				bestCards[i] = bestCard;
				cardsValues[i].SetValue(bestCard);
				if (!hasBestValue) {
					sb.Append(GetCardChar(bestCard));
					sb.Append(" ");
				}
			} else {
				cardsValues[i].SetValue(CardType.Null);
				break;
			}
        }
		uiPreviewValues.text = sb.ToString();

	}

	public override void SubmitElement() {

		// @tmp
		StringBuilder sb = new StringBuilder();

		for (int i = 0; i < boadLength; i++) {
			CardType card;
			if (cardsValues[i].TryGetValue(out card) && card != CardType.Null) {
				sb.Append(GetCardChar(card));
				sb.Append(" ");
			} else {
				break;
			}
		}
		uiSavedValues.text = sb.ToString();
	}

	public string GetCardLetters() {

		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < boadLength; i++) {
			CardType card;
			if (cardsValues[i].TryGetValue(out card) && card != CardType.Null) {
				sb.Append(GetCardChar(card));
			} else {
				break;
			}
		}
		return sb.ToString();
	}

	public void SendToCodingGrid() {

		var code = GetCardLetters();
		if (code != "" && OnSendToCodingGrid != null) {
			OnSendToCodingGrid(code);
		}
	}

}

}