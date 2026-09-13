using UnityEngine;
using System;
using System.Collections.Generic;

//玩家数据类（不继承 MonoBehaviour，纯数据）
[Serializable]
public class Player
{
    public string playerName;          //玩家名字（如“玩家1”、“电脑2”）
    public bool isAI;                 //是否为电脑控制
    public int totalScore;            //总分（三关结束后累计）

    //当前回合的6张手牌
    public List<Card> handCards = new List<Card>();

    //第1关：1张牌
    public List<Card> round1Cards = new List<Card>();
    //第2关：2张牌
    public List<Card> round2Cards = new List<Card>();
    //第3关：3张牌
    public List<Card> round3Cards = new List<Card>();

    //判断当前玩家是否已经完成了分牌（三组都分好了）
    public bool IsReady()
    {
        if (round1Cards.Count != 1)
        {
            return false;
        }
        if (round2Cards.Count != 2)
        {
            return false;
        }
        if (round3Cards.Count != 3)
        {
            return false;
        }
        return true;
    }

    //清空手牌（用于新一局重置）
    public void ClearHand()
    {
        handCards.Clear();
        round1Cards.Clear();
        round2Cards.Clear();
        round3Cards.Clear();
    }

    //给玩家发牌（把发到的6张牌存入手牌）
    public void DealCards(List<Card> cards)
    {
        handCards.Clear();      //先清空旧牌
        handCards.AddRange(cards); //把新牌加进来
    }
}