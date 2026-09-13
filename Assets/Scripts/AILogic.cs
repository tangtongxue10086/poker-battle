using System.Collections.Generic;
using UnityEngine;

public static class AILogic
{
    public static void DivideCards(Player player)
    {
        if (player.handCards.Count != 6)
        {
            return;
        }

        //第1优先：从6张中选2张，使第2关（十点半）最优
        //条件：不爆点（<=10.5），且最接近10.5

        List<Card> bestRound2 = null;
        float bestDiff = 999f;

        //遍历所有 C(6,2)=15 种组合
        for (int i = 0; i < player.handCards.Count; i++)
        {
            for (int j = i + 1; j < player.handCards.Count; j++)
            {
                float point = player.handCards[i].GetSecondRoundPoint()
                             + player.handCards[j].GetSecondRoundPoint();

                float diff = 10.5f - point;
                //只选不爆点的组合，且选差距最小的
                if (diff < 0f || diff >= bestDiff)
                {
                    continue;
                }
                bestDiff = diff;
                bestRound2 = new List<Card>
                {
                    player.handCards[i],
                    player.handCards[j]
                };
            }
        }

        //如果所有组合都爆点（理论上不会，因为发牌阶段已拦截）
        //但为了安全，选爆点最少的组合
        if (bestRound2 == null)
        {
            bestDiff = -999f;
            for (int i = 0; i < player.handCards.Count; i++)
            {
                for (int j = i + 1; j < player.handCards.Count; j++)
                {
                    float point = player.handCards[i].GetSecondRoundPoint()
                                 + player.handCards[j].GetSecondRoundPoint();

                    float diff = 10.5f - point; //负数，表示爆点
                    float absDiff = Mathf.Abs(diff);

                    if (absDiff < Mathf.Abs(bestDiff))
                    {
                        bestDiff = diff;
                        bestRound2 = new List<Card>
                        {
                            player.handCards[i],
                            player.handCards[j]
                        };
                    }
                }
            }
        }

        //如果没有选中任何牌（理论上不可能），直接返回
        if (bestRound2 == null)
        {
            return;
        }

        player.round2Cards.Clear();
        player.round2Cards.AddRange(bestRound2);

        //第2优先：从剩余4张中选3张，使第3关（炸金花）牌型最大
         List<Card> remaining = new List<Card>();
        foreach (Card c in player.handCards)
        {
            if (!bestRound2.Contains(c))
            {
                remaining.Add(c);
            }
        }

        //剩余4张牌，遍历所有 C(4,3)=4 种组合
        List<Card> bestRound3 = null;
        int bestScore = -1;

        for (int i = 0; i < remaining.Count; i++)
        {
            for (int j = i + 1; j < remaining.Count; j++)
            {
                for (int k = j + 1; k < remaining.Count; k++)
                {
                    List<Card> combo = new List<Card>
                    {
                        remaining[i],
                        remaining[j],
                        remaining[k]
                    };

                    int score = RoundJudge.GetPokerHandScore(combo);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestRound3 = combo;
                    }
                }
            }
        }

        if (bestRound3 != null)
        {
            player.round3Cards.Clear();
            player.round3Cards.AddRange(bestRound3);
            //第3优先：剩下的1张归第1关
            List<Card> finalRemaining = new List<Card>();
            foreach (Card c in remaining)
            {
                if (!bestRound3.Contains(c))
                {
                    finalRemaining.Add(c);
                }
            }

            player.round1Cards.Clear();
            if (finalRemaining.Count == 1)
            {
                player.round1Cards.Add(finalRemaining[0]);
            }
        }
    }
}