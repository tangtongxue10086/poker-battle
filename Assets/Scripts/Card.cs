using UnityEngine;
using System;

//花色枚举
//用于第1关和第3关的花色比较：黑桃 > 红桃 > 梅花 > 方片
[Serializable]
public enum Suit
{
    Diamond = 0,
    Club = 1,
    Heart = 2,
    Spade = 3
}

//点数枚举
//数值直接代表牌力大小：2最小，大王16最大
//第1关规则：大王 > 小王 > A > K > Q > J > 10 > 9 > ... > 3 > 2
public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14,
    BlackJoker = 15,
    RedJoker = 16
}

public class Card
{
    public Suit suit;
    public Rank rank;
    public bool isJoker; //是否为大小王（癞子）

    //获取牌的显示名称（UI用）
    public string GetDisplayName()
    {
        if (isJoker)
        {
            if (rank == Rank.RedJoker)
            {
                return "大王";
            }
            else
            {
                return "小王";
            }
        }

        string[] suitNames = { "方片", "梅花", "红桃", "黑桃" };
        return suitNames[(int)suit] + rank.ToString();
    }

    //第1关（单张比大小）使用的牌力值，直接返回枚举数值
    public int GetFirstRoundValue()
    {
        return (int)rank;
    }

    //第2关（十点半）使用的点数
    //J/Q/K/大小王 = 0.5点，A = 1点，数字牌按面值
    public float GetSecondRoundPoint()
    {
        if (rank == Rank.Jack || rank == Rank.Queen || rank == Rank.King || isJoker)
        {
            return 0.5f;
        }

        if (rank == Rank.Ace)
        {
            return 1.0f;
        }

        return (int)rank;
    }

    //获取花色权重，用于平局时比花色
    //黑桃(4) > 红桃(3) > 梅花(2) > 方片(1)，王牌返回0
    public int GetSuitWeight()
    {
        if (isJoker)
        {
            return 0;
        }

        if (suit == Suit.Spade)
        {
            return 4;
        }
        else if (suit == Suit.Heart)
        {
            return 3;
        }
        else if (suit == Suit.Club)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}