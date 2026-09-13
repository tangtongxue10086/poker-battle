using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RoundJudge
{
    //牌型识别（供AI分牌和比牌使用）
    //返回值: 6=豹子, 5=同花顺, 4=顺子, 3=金花, 2=对子, 1=散牌
    public static int GetPokerHandScore(List<Card> cards)
    {
        if (cards.Count != 3) return 0;

        List<int> rankValues = new List<int>();
        int jokerCount = 0;

        foreach (Card c in cards)
        {
            if (c.isJoker)
            {
                jokerCount++;
                rankValues.Add(-1);
            }
            else
            {
                rankValues.Add((int)c.rank);
            }
        }

        List<int> suitWeights = new List<int>();
        foreach (Card c in cards)
        {
            suitWeights.Add(c.GetSuitWeight());
        }

        if (IsThreeOfAKind(rankValues, jokerCount)) return 6;
        if (IsStraightFlush(rankValues, suitWeights, jokerCount)) return 5;
        if (IsStraight(rankValues, jokerCount)) return 4;
        if (IsFlush(suitWeights, jokerCount)) return 3;
        if (IsOnePair(rankValues, jokerCount)) return 2;

        return 1;
    }

    //牌型判定函数
    private static bool IsThreeOfAKind(List<int> rankValues, int jokerCount)
    {
        List<int> normalRanks = new List<int>();
        foreach (int r in rankValues)
        {
            if (r != -1) normalRanks.Add(r);
        }

        if (jokerCount == 0)
        {
            return rankValues[0] == rankValues[1] && rankValues[1] == rankValues[2];
        }

        if (jokerCount == 1)
        {
            return normalRanks.Count == 2 && normalRanks[0] == normalRanks[1];
        }

        return true; // 2个或3个王必成豹子
    }

    private static bool IsStraightFlush(List<int> rankValues, List<int> suitWeights, int jokerCount)
    {
        return IsFlush(suitWeights, jokerCount) && IsStraight(rankValues, jokerCount);
    }

    private static bool IsStraight(List<int> rankValues, int jokerCount)
    {
        List<int> normalRanks = new List<int>();
        foreach (int r in rankValues)
        {
            if (r != -1) normalRanks.Add(r);
        }

        if (normalRanks.Count <= 1) return true;

        if (normalRanks.Count == 2)
        {
            int a = normalRanks[0];
            int b = normalRanks[1];
            int diff = Mathf.Abs(a - b);

            if (diff == 1 || diff == 2) return true;
            if ((a == 14 && b == 2) || (a == 2 && b == 14)) return true;
            if ((a == 14 && b == 13) || (a == 13 && b == 14)) return true;
            return false;
        }

        List<int> sorted = new List<int>(normalRanks);
        sorted.Sort();

        if (sorted[2] - sorted[1] == 1 && sorted[1] - sorted[0] == 1) return true;
        if (sorted[0] == 2 && sorted[1] == 3 && sorted[2] == 14) return true;
        if (sorted[0] == 12 && sorted[1] == 13 && sorted[2] == 14) return true;

        return false;
    }

    private static bool IsFlush(List<int> suitWeights, int jokerCount)
    {
        List<int> normalSuits = new List<int>();
        foreach (int s in suitWeights)
        {
            if (s != 0) normalSuits.Add(s);
        }

        if (normalSuits.Count <= 1) return true;

        int firstSuit = normalSuits[0];
        foreach (int s in normalSuits)
        {
            if (s != firstSuit) return false;
        }
        return true;
    }

    private static bool IsOnePair(List<int> rankValues, int jokerCount)
    {
        List<int> normalRanks = new List<int>();
        foreach (int r in rankValues)
        {
            if (r != -1) normalRanks.Add(r);
        }

        if (jokerCount >= 2) return true;

        if (jokerCount == 1 && normalRanks.Count == 2)
        {
            return normalRanks[0] == normalRanks[1];
        }

        for (int i = 0; i < rankValues.Count; i++)
        {
            for (int j = i + 1; j < rankValues.Count; j++)
            {
                if (rankValues[i] == rankValues[j]) return true;
            }
        }
        return false;
    }

    //异色235（只克制豹子）
    private static bool IsSpecial235(List<Card> cards)
    {
        List<int> ranks = new List<int>();
        foreach (Card c in cards)
        {
            if (c.isJoker) return false;
            ranks.Add((int)c.rank);
        }
        ranks.Sort();

        if (ranks[0] != 2 || ranks[1] != 3 || ranks[2] != 5) return false;

        int s1 = cards[0].GetSuitWeight();
        int s2 = cards[1].GetSuitWeight();
        int s3 = cards[2].GetSuitWeight();

        return !(s1 == s2 && s2 == s3);
    }

    //三张牌点数升序（作为枚举辅助使用）
    private static List<int> GetSortedRanks(List<Card> cards)
    {
        List<int> ranks = new List<int>();
        foreach (Card c in cards) ranks.Add((int)c.rank);
        ranks.Sort();
        return ranks;
    }


    //辅助: 获取对子点数（无对子时返回最大点数）
    private static int GetPairRank(List<Card> cards)
    {
        List<int> ranks = new List<int>();
        foreach (Card c in cards) ranks.Add((int)c.rank);

        for (int i = 0; i < ranks.Count; i++)
        {
            for (int j = i + 1; j < ranks.Count; j++)
            {
                if (ranks[i] == ranks[j]) return ranks[i];
            }
        }

        int max = 0;
        foreach (int r in ranks) if (r > max) max = r;
        return max;
    }

    private static int GetKickerRank(List<Card> cards, int pairRank)
    {
        foreach (Card c in cards)
        {
            int rank = (int)c.rank;
            if (rank != pairRank) return rank;
        }
        return 0;
    }

    private static int GetKickerSuit(List<Card> cards, int pairRank)
    {
        foreach (Card c in cards)
        {
            int rank = (int)c.rank;
            if (rank != pairRank) return c.GetSuitWeight();
        }
        return 0;
    }

    private static int GetMaxSuitWeight(List<Card> cards)
    {
        int max = 0;
        foreach (Card c in cards)
        {
            int suit = c.GetSuitWeight();
            if (suit > max) max = suit;
        }
        return max;
    }

    private static int CountJokers(List<Card> cards)
    {
        int count = 0;
        foreach (Card c in cards)
        {
            if (c.isJoker) count++;
        }
        return count;
    }

    // 计算顺子段位（用于顺子/同花顺比较）
    private static int GetStraightValue(List<int> sortedRanks)
    {
        // sortedRanks 已从小到大排序
        if (sortedRanks[0] == 2 && sortedRanks[1] == 3 && sortedRanks[2] == 14)
            return 3;          // A-2-3 段位为 3
        return sortedRanks[2]; // 其他顺子返回最大牌
    }

    //第三关比牌: 返回赢家, 平局返回null
    public static (Player winner, int gain, Player p235, int bounty) EvaluateRound3(List<Player> players)
    {
        List<(Player player, int score, int maxRank, int secondRank, int thirdRank,
               int maxSuit, int jokerCount, List<Card> cards, bool is235)> entries
            = new List<(Player, int, int, int, int, int, int, List<Card>, bool)>();

        foreach (Player p in players)
        {
            if (p.round3Cards.Count != 3) continue;
            List<Card> rawCards = p.round3Cards;
            List<Card> cards = ExpandJokers(rawCards);   
            int score = GetPokerHandScore(cards);
            int jokerCount = CountJokers(rawCards);      
            bool is235 = IsSpecial235(rawCards);         
            List<int> sortedRanks = GetSortedRanks(cards);   

            if (sortedRanks.Count != 3)
            {
                Debug.LogError($"玩家 {p.playerName} 的 sortedRanks 长度异常！Count={sortedRanks.Count}，手牌：");
                foreach (Card c in cards) Debug.Log(c.GetDisplayName());
                continue;   //关键：跳过该玩家
            }
            Debug.Log(p.playerName + " 第3关牌型:" + score + "（6豹子5同花顺4顺子3金花2对子1散牌）");

            entries.Add((p, score, sortedRanks[2], sortedRanks[1], sortedRanks[0],
                         GetMaxSuitWeight(cards), jokerCount, cards, is235));
        }

        if (entries.Count == 0) return (null, 0, null, 0);
        //235吃豹子
        //两家235按老规矩比最大牌
        List<(Player player, int score, int maxRank, int secondRank, int thirdRank,
              int maxSuit, int jokerCount, List<Card> cards, bool is235)> e235s
            = entries.FindAll(x => x.is235);
        Player p235 = null;
        int bestSuit = -1;
        foreach (var x in e235s)
        {
            if (x.maxSuit > bestSuit) { bestSuit = x.maxSuit; p235 = x.player; }
        }
        int bounty = 0;

        if (p235 != null)
        {
            foreach (var x in entries)
            {
                if (x.score == 6) bounty += 3;   //每只豹子付自己3张手牌
            }
            entries.RemoveAll(x => x.score == 6);
        }
        //对手全是豹子被清空：235独赢，只收自己桌上那3张（豹子的钱已走赏金）
        if (entries.Count == 0) return (p235, 3, p235, bounty);

        var winner = entries[0];
        bool isTie = false;
        for (int i = 1; i < entries.Count; i++)
        {
            var e = entries[i];

            //第1级: 牌型等级
            if (e.score > winner.score)
            {
                winner = e;
                isTie = false;
                continue;
            }
            if (e.score < winner.score) continue;

            //第2级: 核心点数（不同牌型不同规则）

            //豹子: 比点数 -> 点数相同比王数（少者胜）
            if (e.score == 6 && winner.score == 6)
            {
                int eRank = GetPairRank(e.cards);
                int wRank = GetPairRank(winner.cards);

                if (eRank > wRank)
                {
                    winner = e;
                    isTie = false;
                }
                else if (eRank == wRank)
                {
                    if (e.jokerCount < winner.jokerCount)
                    {
                        winner = e;
                        isTie = false;
                    }
                    else if (e.jokerCount == winner.jokerCount)
                    {
                        isTie = true;
                    }
                }
                continue;
            }

            //对子: 比对子点数 -> 比踢脚点数 -> 比踢脚花色
            if (e.score == 2 && winner.score == 2)
            {
                int ePair = GetPairRank(e.cards);
                int wPair = GetPairRank(winner.cards);

                if (ePair > wPair)
                {
                    winner = e;
                    isTie = false;
                }
                else if (ePair == wPair)
                {
                    int eKicker = GetKickerRank(e.cards, ePair);
                    int wKicker = GetKickerRank(winner.cards, wPair);

                    if (eKicker > wKicker)
                    {
                        winner = e;
                        isTie = false;
                    }
                    else if (eKicker == wKicker)
                    {
                        int eSuit = GetKickerSuit(e.cards, ePair);
                        int wSuit = GetKickerSuit(winner.cards, wPair);

                        if (eSuit > wSuit)
                        {
                            winner = e;
                            isTie = false;
                        }
                        else if (eSuit == wSuit)
                        {
                            isTie = true;
                        }
                    }
                }
                continue;
            }

            //同花顺/顺子: 比段位 -> 比最大牌花色
            if ((e.score == 5 || e.score == 4) && (winner.score == 5 || winner.score == 4))
            {
                int eSegment = GetStraightValue(new List<int> { e.thirdRank, e.secondRank, e.maxRank });
                int wSegment = GetStraightValue(new List<int> { winner.thirdRank, winner.secondRank, winner.maxRank });

                if (eSegment > wSegment)
                {
                    winner = e;
                    isTie = false;
                }
                else if (eSegment == wSegment)
                {
                    if (e.maxSuit > winner.maxSuit)
                    {
                        winner = e;
                        isTie = false;
                    }
                    else if (e.maxSuit == winner.maxSuit)
                    {
                        isTie = true;
                    }
                }
                continue;
            }

            //金花/散牌: 逐张比点数 -> 比最大花色
            if ((e.score == 3 || e.score == 1) && (winner.score == 3 || winner.score == 1))
            {
                if (e.maxRank > winner.maxRank)
                {
                    winner = e;
                    isTie = false;
                }
                else if (e.maxRank == winner.maxRank)
                {
                    if (e.secondRank > winner.secondRank)
                    {
                        winner = e;
                        isTie = false;
                    }
                    else if (e.secondRank == winner.secondRank)
                    {
                        if (e.thirdRank > winner.thirdRank)
                        {
                            winner = e;
                            isTie = false;
                        }
                        else if (e.thirdRank == winner.thirdRank)
                        {
                            if (e.maxSuit > winner.maxSuit)
                            {
                                winner = e;
                                isTie = false;
                            }
                            else if (e.maxSuit == winner.maxSuit)
                            {
                                isTie = true;
                            }
                        }
                    }
                }
                continue;
            }
        }
        int gain = isTie ? 0 : entries.Count * 3;   //赢家收幸存者桌上全部牌
        return isTie ? (null, 0, p235, bounty) : (winner.player, gain, p235, bounty);
    }
    //枚举遍历出有王的所有情况，从中选出最大牌型（王变化判定最终方案）
    private static List<Card> ExpandJokers(List<Card> cards)
    {
        List<Card> normal = new List<Card>();
        int jokerCount = 0;
        foreach (Card c in cards)
        {
            if (c.isJoker) jokerCount++;
            else normal.Add(c);
        }
        if (jokerCount == 0) return cards;

        List<Card> best = null;
        int bestScore = -1;
        List<int> bestRanks = null;
        int bestSuit = -1;

        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            for (int r = 2; r <= 14; r++)
            {
                Card fill1 = new Card { suit = suit, rank = (Rank)r, isJoker = false };
                if (fill1.GetSuitWeight() == 0) continue;
                if (Duplicates(normal, fill1)) continue;   //一副牌一张，不能复制已有牌

                if (jokerCount == 1)
                {
                    TryExpanded(normal, fill1, null, ref best, ref bestScore, ref bestRanks, ref bestSuit);
                }
                else
                {
                    foreach (Suit suit2 in System.Enum.GetValues(typeof(Suit)))
                    {
                        for (int r2 = 2; r2 <= 14; r2++)
                        {
                            Card fill2 = new Card { suit = suit2, rank = (Rank)r2, isJoker = false };
                            if (fill2.GetSuitWeight() == 0) continue;
                            if (Duplicates(normal, fill2)) continue;
                            if (fill2.suit == fill1.suit && fill2.rank == fill1.rank) continue;   //两张王不变成同一张牌

                            TryExpanded(normal, fill1, fill2, ref best, ref bestScore, ref bestRanks, ref bestSuit);
                        }
                    }
                }
            }
        }
        return best != null ? best : normal;
    }

    //补牌是否与已有普通牌完全相同（同花同点）
    private static bool Duplicates(List<Card> normal, Card fill)
    {
        foreach (Card c in normal)
            if (c.suit == fill.suit && c.rank == fill.rank) return true;
        return false;
    }

    //选最大牌型
    private static void TryExpanded(List<Card> normal, Card f1, Card f2,
        ref List<Card> best, ref int bestScore, ref List<int> bestRanks, ref int bestSuit)
    {
        List<Card> hand = new List<Card>(normal) { f1 };
        if (f2 != null) hand.Add(f2);

        int score = GetPokerHandScore(hand);

        List<int> ranks = new List<int>();
        foreach (Card c in hand) ranks.Add((int)c.rank);
        ranks.Sort();

        bool better = score > bestScore;
        if (!better && score == bestScore && bestRanks != null)
        {
            if (score == 4 || score == 5)
            {
                better = GetStraightValue(ranks) > GetStraightValue(bestRanks);   //A23是最小顺子
            }
            else
            {
                for (int i = 2; i >= 0; i--)
                {
                    if (ranks[i] != bestRanks[i]) { better = ranks[i] > bestRanks[i]; break; }
                }
                if (!better) better = GetMaxSuitWeight(hand) > bestSuit;
            }
        }

        if (better)
        {
            best = hand;
            bestScore = score;
            bestRanks = ranks;
            bestSuit = GetMaxSuitWeight(hand);
        }
    }
}