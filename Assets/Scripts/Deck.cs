using UnityEngine;
using System.Collections.Generic;

public class Deck : MonoBehaviour
{
    public List<Card> cards = new List<Card>();

    //生成一副完整的54张牌（2~A + 大小王）
    public void CreateDeck()
    {
        cards.Clear();

        //遍历4种花色
        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            //点数从2到14（对应枚举中的 Two 到 Ace）
            for (int rankValue = 2; rankValue <= 14; rankValue++)
            {
                Card card = new Card();
                card.suit = suit;
                card.rank = (Rank)rankValue;
                card.isJoker = false;
                cards.Add(card);
            }
        }

        //添加小王
        Card blackJoker = new Card();
        blackJoker.rank = Rank.BlackJoker;
        blackJoker.isJoker = true;
        cards.Add(blackJoker);

        //添加大王
        Card redJoker = new Card();
        redJoker.rank = Rank.RedJoker;
        redJoker.isJoker = true;
        cards.Add(redJoker);
    }

    //洗牌（随机交换）
    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            //交换两张牌的位置
            Card temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }

    //从牌堆顶部取走 count 张牌（发牌用）
    public List<Card> DrawCards(int count)
    {
        //如果牌不够，返回空列表并打印警告
        if (cards.Count < count)
        {
            Debug.LogWarning("牌堆剩余牌不足 " + count + " 张");
            return new List<Card>();
        }

        List<Card> drawnCards = new List<Card>();

        //从列表末尾依次取牌（列表尾部视为牌堆顶部）
        for (int i = 0; i < count; i++)
        {
            int lastIndex = cards.Count - 1;
            Card card = cards[lastIndex];
            drawnCards.Add(card);
            cards.RemoveAt(lastIndex);
        }

        return drawnCards;
    }
    //获取牌堆剩余牌数
    public int GetRemainingCount()
    {
        return cards.Count;
    }

    //检查剩余牌数是否足够发牌，不够则自动重置并洗牌
    public void EnsureEnoughCards(int requiredCount)
    {
        if (cards.Count < requiredCount)
        {
            ResetDeck();
        }
    }

    //重置牌堆（新一局调用）
    public void ResetDeck()
    {
        CreateDeck();
        Shuffle();
    }
}