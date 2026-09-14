using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameUI : MonoBehaviour
{
    public GameManager gameManager;   //游戏主逻辑
    public GameObject cardTemplate;   //卡牌模板
    public Transform handPanel;       //手牌区
    public Text statusText;           //状态文字
    public Text messageText;          //消息文字
    public GameObject settlePanel;    //结算面板
    public Text rankText;             //排名文字
    private List<GameObject> shownCards = new List<GameObject>();

    void Start()
    {
        cardTemplate.SetActive(false);
        settlePanel.SetActive(false);
        if (gameManager != null)
        {
            gameManager.OnHandDealt += RefreshHandNow;
        }
    }


    void Update()
    {
        if (gameManager == null) return;
        List<Player> players = gameManager.GetPlayers();
        if (players == null || players.Count == 0) return;
        Player human = players.Find(p => !p.isAI);
        //消息栏跟随游戏状态变化
        switch (gameManager.CurrentState)
        {
            case GameState.WaitingToStart:
                messageText.text = "按【空格】开始游戏";
                break;
            case GameState.Dividing:
                messageText.text = "数字键1~6=选牌  Enter=确认  R=重选";
                break;
            case GameState.Round1Compare:
                messageText.text = ">>> 第1关比牌中（单张比大小）…  " + gameManager.latestResult;
                break;
            case GameState.Round2Compare:
                messageText.text = ">>> 第2关比牌中（十点半）…  " + gameManager.latestResult;
                break;
            case GameState.Round3Compare:
                messageText.text = ">>> 第3关比牌中（炸金花）…  " + gameManager.latestResult;
                break;
            case GameState.Settling:
                messageText.text = ">>> 结算中…";
                break;
            case GameState.GameOver:
                messageText.text = "本局结束！按【空格】再来一局（分数累计）";
                break;
        }


        //手牌显示
        if (human != null && human.handCards.Count != shownCards.Count)
        {
            RefreshHand(human);
        }

        //结算面板
        if (gameManager.IsGameOver())
        {
            if (!settlePanel.activeSelf) ShowRanking(players);
        }
        else if (settlePanel.activeSelf)
        {
            settlePanel.SetActive(false);
            messageText.gameObject.SetActive(true);  //退出结算，恢复全局消息条
        }
    }
    //放入真实扑克图片
    Sprite LoadCardSprite(Card c)
    {
        if (c.isJoker)
        {
            string jokerPath = (c.rank == Rank.RedJoker)
                ? "Cards/red_joker"
                : "Cards/black_joker";
            return Resources.Load<Sprite>(jokerPath);
        }
        string suit;
        switch (c.suit)
        {
            case Suit.Spade: suit = "spades"; break;
            case Suit.Heart: suit = "hearts"; break;
            case Suit.Diamond: suit = "diamonds"; break;
            case Suit.Club: suit = "clubs"; break;
            default:
                Debug.LogWarning("未知花色: " + c.suit);
                return null;
        }
        string rank;
        switch (c.rank)
        {
            case Rank.Ace: rank = "ace"; break;
            case Rank.Jack: rank = "jack"; break;
            case Rank.Queen: rank = "queen"; break;
            case Rank.King: rank = "king"; break;
            default: rank = ((int)c.rank).ToString(); break;
        }

        string path = "Cards/" + rank + "_of_" + suit;
        Sprite s = Resources.Load<Sprite>(path);
        if (s == null) Debug.LogWarning("找不到贴图: " + path);
        return s;
    }
    void RefreshHandNow()
    {
        if (gameManager == null) return;
        List<Player> players = gameManager.GetPlayers();
        if (players == null || players.Count == 0) return;

        Player human = players.Find(p => !p.isAI);
        if (human != null) RefreshHand(human);
    }


    void RefreshHand(Player human)
    {
        Font chineseFont = statusText.font;
        foreach (GameObject g in shownCards) Destroy(g);
        shownCards.Clear();
        for (int i = 0; i < human.handCards.Count; i++)
        {
            Card c = human.handCards[i];
            GameObject card = Instantiate(cardTemplate, handPanel);
            card.SetActive(true);
            Image cardImage = card.GetComponent<Image>();
            cardImage.sprite = LoadCardSprite(c);
            cardImage.color = Color.white;
            card.name = "Card_" + (i + 1);
            card.GetComponent<RectTransform>().localPosition = new Vector3(i * 130 - 260, 0, 0);
            shownCards.Add(card);
        }
    }


    void ShowRanking(List<Player> players)
    {
        List<Player> ranked = new List<Player>(players);
        ranked.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
        string s = "最终排名\n";
        for (int i = 0; i < ranked.Count; i++)
        {
            s += "第" + (i + 1) + "名 " + ranked[i].playerName + "  " + ranked[i].totalScore + "分\n";
        }
        //注意：循环里每行已带\n，这里直接拼接即为第6行，不要再加空行
        //（之前多加了一个"\n"空行，7行 x ~32px ≈ 224px，超出文本框220px，末行被Truncate裁掉）
        s += "本局结束！按【空格】再来一局（分数累计）";
        rankText.text = s;
        rankText.color = Color.white;  //面板是86%黑底，原黑字几乎不可读
        //代码加高面板：6行 x ~32px(24号字MSYH行高) ≈ 192px + 40px内边距，260太紧，加高到320留足余量
        settlePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 320);
        messageText.gameObject.SetActive(false);  //隐藏全局消息条（重叠来源）
        settlePanel.SetActive(true);
    }
}
