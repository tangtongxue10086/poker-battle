using System;
using System.Collections.Generic;
using UnityEngine;

//游戏状态枚举
public enum GameState
{
    WaitingToStart,     // 等待开始
    Dealing,            // 发牌中
    Dividing,           // 分牌中
    Round1Compare,      // 第1关比牌
    Round2Compare,      // 第2关比牌
    Round3Compare,      // 第3关比牌
    Settling,           // 结算中
    GameOver            // 游戏结束
}

public class GameManager : MonoBehaviour
{
    [Header("牌堆引用")]
    public Deck deck;

    [Header("玩家设置")]
    [Range(2, 4)]
    public int playerCount = 4;          // 玩家数量 2~4

    [Header("调试开关")]
    public bool verboseLog = true;       // 是否输出详细日志

    private List<Player> players = new List<Player>();
    private Player round1Winner;   
    private Player round2Winner;   
    private Player round3Winner;
    private bool humanConfirmed = false;   //真人是否已确认提交分配
    private GameState currentState = GameState.WaitingToStart;

    //记录上次玩家配置，用于检测变化
    private string lastPlayerConfig = "";

    void Start()
    {
        //等玩家按空格键开局
        currentState = GameState.WaitingToStart;
        stateTimer = 0f;
        Debug.Log("按【空格键】开始游戏");
    }
    //指纹生成器：指纹 = 玩家总数 + 每人的AI/真人标志，如 "4HumanAIAIAI"
    //全项目只有这一份格式定义，两条路线都调它
    //TODO(联网版): 当前指纹只认结构不认身份，联网后需把玩家唯一ID纳入
    private string BuildPlayerConfig()
    {
        string config = playerCount.ToString();
        foreach (Player p in players)
        {
            config += p.isAI ? "AI" : "Human";
        }
        return config;
    }

    //玩家管理 

    bool CreatePlayers()
    {
        //如果玩家列表为空，直接创建
        if (players.Count == 0)
        {
            int AICount = 0;
            for (int i = 0; i < playerCount; i++)
            {
                Player p = new Player();
                p.playerName = "玩家" + (i+1) ;
                p.isAI = (i!=0);      //判断是否为ai
                p.totalScore = 0;
                players.Add(p);
                if(p.isAI) AICount++;
            }
            lastPlayerConfig = BuildPlayerConfig();
            if (verboseLog)
            {
                Debug.Log("创建 " + playerCount + " 名玩家（真人 " + (playerCount - AICount) + " 名，AI " + AICount + " 名）");
            }
            return true;
        }

        //检查配置是否变化
        string currentConfig = BuildPlayerConfig();

        if (currentConfig != lastPlayerConfig)
        {
            int AICount = 0;
            //配置变化，重建玩家
            players.Clear();
            for (int i = 0; i < playerCount; i++)
            {
                Player p = new Player();
                p.playerName = "玩家" + (i + 1);
                p.isAI = (i != 0);
                p.totalScore = 0;
                players.Add(p);
                if (p.isAI) AICount++;
            }
            lastPlayerConfig = BuildPlayerConfig();
            if (verboseLog)
            {
                Debug.Log("玩家配置变化，重新创建 " + playerCount + " 名玩家（真人 " + (playerCount - AICount) + " 名，AI " + AICount + " 名）");
            }
            return true;
        }

        if (verboseLog)
        {
            Debug.Log("玩家配置未变化，复用现有玩家数据");
        }
        return false;
    }

    //牌堆管理 

    void PrepareDeck(bool configChanged)
    {
        if (configChanged)
        {
            deck.ResetDeck();
            if (verboseLog)
            {
                Debug.Log("玩家配置变化，重新洗牌");
            }
            return;
        }

        int requiredCards = playerCount * 6;
        deck.EnsureEnoughCards(requiredCards);
        if (verboseLog)
        {
            Debug.Log("牌堆剩余 " + deck.GetRemainingCount() + " 张，准备发 " + requiredCards + " 张");
        }
    }

    //发牌逻辑

    void DealCards()
    {
        if (verboseLog)
        {
            Debug.Log("发牌");
        }

        foreach (Player p in players)
        {
            //清空玩家旧数据
            p.ClearHand();

            //获取一组有效手牌（必爆死局前置拦截）
            List<Card> hand = GetValidHand();

            if (hand.Count == 6)
            {
                p.DealCards(hand);
                if (verboseLog)
                {
                    string cardsStr = "";
                    foreach (Card c in p.handCards)
                    {
                        cardsStr += c.GetDisplayName() + " ";
                    }
                    Debug.Log(p.playerName + " 手牌: " + cardsStr);
                }
            }
            else
            {
                //3次重发失败，标记该玩家第2关为0分
                Debug.LogError(p.playerName + " 发牌失败，第2关将得0分");
                p.DealCards(new List<Card>());
            }
        }
        if (OnHandDealt != null) OnHandDealt();  
    }

    //为单个玩家获取有效手牌（必爆死局前置拦截）
    List<Card> GetValidHand()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            List<Card> hand = deck.DrawCards(6);

            //检查是否存在至少一种两两组合不爆点（<=10.5）
            bool hasValidPair = false;
            for (int i = 0; i < hand.Count; i++)
            {
                for (int j = i + 1; j < hand.Count; j++)
                {
                    float point = hand[i].GetSecondRoundPoint() + hand[j].GetSecondRoundPoint();
                    if (point <= 10.5f)
                    {
                        hasValidPair = true;
                        break;
                    }
                }
                if (hasValidPair) break;
            }

            if (hasValidPair)
            {
                return hand;
            }

            //必爆死局，这6张牌弃掉，重试
            if (verboseLog)
            {
                Debug.LogWarning("第 " + (attempt + 1) + " 次发牌为必爆死局，重新发牌");
            }
        }

        //3次全部失败
        Debug.LogError("3次发牌均为必爆死局，该玩家第2关0分");
        return new List<Card>();
    }

    //AI自动分牌
    void AutoDivideForAI()
    {
        if (verboseLog)
        {
            Debug.Log("AI分牌");
        }

        foreach (Player p in players)
        {
            if (p.isAI)
            {
                //调用AI分牌逻辑（目前占位，后续完善）
                AILogic.DivideCards(p);

                if (verboseLog)
                {
                    string r1 = p.round1Cards.Count > 0 ? p.round1Cards[0].GetDisplayName() : "无";
                    string r2 = p.round2Cards.Count == 2 ?
                        p.round2Cards[0].GetDisplayName() + " + " + p.round2Cards[1].GetDisplayName() : "无";
                    string r3 = p.round3Cards.Count == 3 ?
                        p.round3Cards[0].GetDisplayName() + " + " + p.round3Cards[1].GetDisplayName() + " + " + p.round3Cards[2].GetDisplayName() : "无";

                    Debug.Log(p.playerName + " 分牌: 第1关[" + r1 + "] 第2关[" + r2 + "] 第3关[" + r3 + "]");
                }
            }
            else
            {
                //真人玩家后续由UI交互完成分牌，这里暂时跳过
                if (verboseLog)
                {
                    //Debug.Log(p.playerName + " 是真人玩家，等待UI操作");
                }
            }
        }
    }


    //第一关比牌
    void DoCompareRound1()
    {
        if (verboseLog) Debug.Log("【第1关】单张比大小");

        List<(Player player, Card card, int value, int suitWeight)> entries = new List<(Player, Card, int, int)>();

        foreach (Player p in players)
        {
            if (p.round1Cards.Count == 0) continue;

            Card card = p.round1Cards[0];
            int value = card.GetFirstRoundValue();
            int suitWeight = card.GetSuitWeight();

            if (verboseLog)
                Debug.Log(p.playerName + " 出牌: " + card.GetDisplayName() + " (牌力:" + value + " 花色:" + suitWeight + ")");

            entries.Add((p, card, value, suitWeight));
        }

        if (entries.Count == 0)
        {
            Debug.Log("第1关 无有效牌");
            return;
        }

        //只有一个玩家出牌：没人可赢，不计分
        if (entries.Count == 1)
        {
            Debug.Log("第1关 仅 " + entries[0].player.playerName + " 一家有牌，无人可赢，+0分");
            return;
        }

        //找最大
        Player winner = entries[0].player;
        int maxValue = entries[0].value;
        int maxSuit = entries[0].suitWeight;
        bool isTie = false;

        for (int i = 1; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.value > maxValue || (e.value == maxValue && e.suitWeight > maxSuit))
            {
                winner = e.player;
                maxValue = e.value;
                maxSuit = e.suitWeight;
                isTie = false;
            }
            else if (e.value == maxValue && e.suitWeight == maxSuit)
            {
                isTie = true;
            }
        }

        if (isTie)
        { 
            Debug.Log("第1关 平局！无人得分");
            latestResult = "第1关 平局，无人得分";
        }
        else if (winner != null)
        {
            winner.totalScore += entries.Count;   //收本局桌上全部牌：出了牌的人数×每关1张
            round1Winner = winner;
            Debug.Log("第1关 获胜者: " + winner.playerName + " +" + entries.Count + "分！总分:" + winner.totalScore);
            latestResult = "第1关 " + winner.playerName + " 胜，+" + entries.Count + "分";
        }
    }

    void DoCompareRound2()
    { 
        if (verboseLog)
        {
            Debug.Log("【第2关】十点半");
        }

        //收集数据
        List<(Player player, float total, float maxSingle, int maxSingleSuit, bool isValid)> entries
            = new List<(Player, float, float, int, bool)>();

        foreach (Player p in players)
        {
            if (p.round2Cards.Count != 2)
            {
                //没有第2关牌（必爆死局失败的情况），标记无效
                entries.Add((p, -999f, -999f, 0, false));
                continue;
            }

            float total = p.round2Cards[0].GetSecondRoundPoint() + p.round2Cards[1].GetSecondRoundPoint();
            if (verboseLog)
            {
                Debug.Log(p.playerName + " 调试: 牌1点数=" + p.round2Cards[0].GetSecondRoundPoint()
              + " 牌2点数=" + p.round2Cards[1].GetSecondRoundPoint()
              + " 合计=" + total);
            }
            //计算最大单张点数（第二关计分点数）
            float maxSingle = 0f;
            int maxSingleSuit = 0;
            foreach (Card c in p.round2Cards)
            {
                float point = c.GetSecondRoundPoint();
                if (point > maxSingle)
                {
                    maxSingle = point;
                    maxSingleSuit = c.GetSuitWeight();
                }
                else if (point == maxSingle) 
                {
                    if (c.GetSuitWeight() > maxSingleSuit)
                    {
                        maxSingleSuit = c.GetSuitWeight();
                    }
                }
            }

            bool isValid = (total <= 10.5f);

            if (verboseLog)
            {
                string status = isValid ? "" : " 💥爆点!";
                Debug.Log(p.playerName + " 出牌: " +
                    p.round2Cards[0].GetDisplayName() + " + " + p.round2Cards[1].GetDisplayName() +
                    " 点数:" + total + " 最大单张:" + maxSingle + status);
            }

            entries.Add((p, total, maxSingle, maxSingleSuit, isValid));
        }

        //过滤无效（爆点或没有牌）
        List<(Player player, float total, float maxSingle, int maxSingleSuit)> valid =
            new List<(Player, float, float, int)>();

        foreach (var e in entries)
        {
            if (e.isValid)
            {
                valid.Add((e.player, e.total, e.maxSingle, e.maxSingleSuit));
            }
        }

        if (valid.Count == 0)
        {
            Debug.Log("第2关 全员爆点或无效！无人得分");
            return;
        }

        // 找赢家
        var winner = valid[0];
        bool isTie = false;

        for (int i = 1; i < valid.Count; i++)
        {
            var e = valid[i];
            if (e.total > winner.total)
            {
                winner = e;
                isTie = false;
            }
            else if (Mathf.Approximately(e.total, winner.total))
            {
                if (e.maxSingle > winner.maxSingle)
                {
                    winner = e;
                    isTie = false;
                }
                else if (Mathf.Approximately(e.maxSingle, winner.maxSingle))
                {
                    if (e.maxSingleSuit > winner.maxSingleSuit)
                    {
                        winner = e;
                        isTie = false;
                    }
                    else if (e.maxSingleSuit == winner.maxSingleSuit)
                    {
                        isTie = true;
                    }
                }
            }
        }

        if (isTie)
            Debug.Log("第2关 平局！无人得分");
        else
        {
            winner.player.totalScore += valid.Count * 2;   //收本局桌上全部牌：有效人数×每关2张
            round2Winner = winner.player;
            Debug.Log("第2关 获胜者: " + winner.player.playerName + " +" + (valid.Count * 2) + "分！总分:" + winner.player.totalScore);
            latestResult = "第2关 " + winner.player.playerName + " 胜，+" + (valid.Count * 2) + "分";
        }
    }

    void DoCompareRound3()
    {
        if (verboseLog)
        {
            Debug.Log("【第3关】炸金花");
        }

        var res = RoundJudge.EvaluateRound3(players);

        //每只豹子付3张给235（不进赢家口袋，平局也照付）
        if (res.p235 != null && res.bounty > 0)
        {
            res.p235.totalScore += res.bounty;
            Debug.Log("💀 异色235克豹子！赏金 +" + res.bounty + " → " + res.p235.playerName + " 总分:" + res.p235.totalScore);
        }

        if (res.winner != null)
        {
            res.winner.totalScore += res.gain;
            round3Winner = res.winner;
            Debug.Log("第3关 获胜者: " + res.winner.playerName + " +" + res.gain + "分！总分:" + res.winner.totalScore);
            latestResult = "第3关 " + res.winner.playerName + " 胜，+" + res.gain + "分";
        }
        else
        {
            Debug.Log("第3关 平局或无人得分（赏金照付）");
        }
    }


    //结算逻辑

    void SettleGame()
    {
        latestResult = "";
        if (verboseLog)
        {
            Debug.Log("游戏结算");
        }
        //全金奖励：三关被同一个人赢下
        if (round1Winner != null && round1Winner == round2Winner && round2Winner == round3Winner)
        {
            round1Winner.totalScore += 2;
            Debug.Log("🌟 " + round1Winner.playerName + " 三关全胜！全金奖励 +2分，总分:" + round1Winner.totalScore);
        }
        //按总分排序
        List<Player> ranked = new List<Player>(players);
        ranked.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
        Debug.Log("排名:");
        for (int i = 0; i < ranked.Count; i++)
        {
            string medal = (i == 0) ? "🏆 " : "";
            Debug.Log("第" + (i + 1) + "名: " + medal + ranked[i].playerName + " " + ranked[i].totalScore + "分");
        }

        Debug.Log("游戏结束");
    }
    //获取玩家列表
    public List<Player> GetPlayers()
    {
        return players;
    }
    //供UI查询游戏是否结束
    public bool IsGameOver()
    {
        return currentState == GameState.GameOver;
    }
    //供UI查询当前状态
    public GameState CurrentState
    {
        get { return currentState; }
    }

    public string latestResult = "";
    public event Action OnHandDealt;
    private float stateTimer = 0f;
    private const float ShowResultDuration = 1.5f;

    //整个游戏流程的推进全部由这里的 switch 驱动
    void Update()
    {
        stateTimer += Time.deltaTime;  //秒表一直走

        switch (currentState)
        {
            case GameState.WaitingToStart:
                //等待状态：只响应空格键，其他什么都不做
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StartNewGame();
                }
                break;

            case GameState.Dividing:
                //分牌状态：每帧检查真人是否分完了
                UpdateDividing();
                break;

            case GameState.Round1Compare:
                //停够1.5秒才执行第1关比牌，比完自动进入下一关状态
                if (stateTimer >= ShowResultDuration)
                {
                    DoCompareRound1();
                    EnterNextState(GameState.Round2Compare);
                }
                break;

            case GameState.Round2Compare:
                if (stateTimer >= ShowResultDuration)
                {
                    DoCompareRound2();
                    EnterNextState(GameState.Round3Compare);
                }
                break;

            case GameState.Round3Compare:
                if (stateTimer >= ShowResultDuration)
                {
                    DoCompareRound3();
                    EnterNextState(GameState.Settling);
                }
                break;

            case GameState.Settling:
                if (stateTimer >= ShowResultDuration)
                {
                    SettleGame();
                    EnterNextState(GameState.GameOver);
                }
                break;

            case GameState.GameOver:
                //结束状态：按空格再来一局（保留累计分数）
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    RestartGame();
                }
                break;
        }
    }

    //开新的一局：做"准备工作"（建玩家、发牌、AI分牌），然后停在 Dividing 等真人
    private void StartNewGame()
    {
        round1Winner = null;
        round2Winner = null;
        round3Winner = null;
        humanConfirmed = false;
        Debug.Log("新一局开始");

        bool configChanged = CreatePlayers();  //建或复用玩家
        PrepareDeck(configChanged);            //准备牌堆
        DealCards();                           //发牌（含必爆死局拦截）
        AutoDivideForAI();                     //AI立刻分完它们的牌

        PrintHumanHand();                      //把真人的手牌带序号打印出来
        Debug.Log("请按数字键【1~6】选牌分配：第1张→第1关，第2~3张→第2关，第4~6张→第3关。按 R 重新分配。");

        EnterNextState(GameState.Dividing);    //进入分牌状态，开始等真人操作
    }

    //分牌状态的每帧逻辑
    private void UpdateDividing()
    {
        HandleHumanInput();  //响应真人的键盘输入

        //检查是否所有人都分完了
        if (IsAllDivided())
        {
            Debug.Log("全部分配完毕，开始比牌！");
            EnterNextState(GameState.Round1Compare);
        }
    }

    //判断"所有人都分好了"
    private bool IsAllDivided()
    {
        foreach (Player p in players)
        {
            //发牌失败的玩家手牌是空的（3次死局重发都失败），
            //这种玩家跳过检查，否则游戏会永远卡在分牌状态
            if (p.handCards.Count == 0) continue;
            if (!p.IsReady()) return false;  //还有人没分完 → 还没好
        }
        //真人没有手牌（发牌失败旁观）时无需确认；否则必须按过 Enter
        Player human = players.Find(p => !p.isAI);
        if (human == null || human.handCards.Count == 0) return true;
        return humanConfirmed;
    }

    //处理真人玩家的键盘输入
    private void HandleHumanInput()
    {
        Player human = players.Find(p => !p.isAI);
        if (human == null || human.handCards.Count == 0) return;

        //数字键1~6 分别对应手牌列表的第0~5张
        for (int i = 0; i < human.handCards.Count && i < 6; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                TryAssignCard(human.handCards[i]);
            }
        }

        //Enter键：6张分完后确认提交
        if (Input.GetKeyDown(KeyCode.Return) && human.IsReady())
        {
            humanConfirmed = true;
            Debug.Log("✅ 分配已确认提交！");
        }

        //R键：推倒重来
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetHumanDivide();
        }
    }

    //把一张牌按顺序分配进三关（这就是以后UI按钮要调用的方法！）
    public void TryAssignCard(Card card)
    {
        Player human = players.Find(p => !p.isAI);
        if (human == null || card == null) return;

        //防重复：这张牌如果已经分进任何一关，忽略本次操作
        if (human.round1Cards.Contains(card) ||
            human.round2Cards.Contains(card) ||
            human.round3Cards.Contains(card))
        {
            return;
        }

        //按顺序填充：先填满第1关(1张)，再第2关(2张)，再第3关(3张)
        if (human.round1Cards.Count < 1)
        {
            human.round1Cards.Add(card);
        }
        else if (human.round2Cards.Count < 2)
        {
            //第2关出牌门：放第二张时检查合计，爆点拒收
            if (human.round2Cards.Count == 1)
            {
                float wouldTotal = human.round2Cards[0].GetSecondRoundPoint() + card.GetSecondRoundPoint();
                if (wouldTotal > 10.5f)
                {
                    Debug.Log("❌ " + human.round2Cards[0].GetDisplayName() + " + " + card.GetDisplayName()
                        + " = " + wouldTotal + " > 10.5，会爆点！这张没收，请选其他牌。实在无解按【R】重排");
                    return;   //拒收：这张牌留在手里，玩家仍在"选第2关第二张"的状态
                }
            }
            human.round2Cards.Add(card);
        }

        else if (human.round3Cards.Count < 3)
        {
            human.round3Cards.Add(card);
        }
        PrintHumanStatus();
    }

    //真人重选：清空三组，重新分配
    private void ResetHumanDivide()
    {
        Player human = players.Find(p => !p.isAI);
        if (human == null) return;
        human.round1Cards.Clear();
        human.round2Cards.Clear();
        human.round3Cards.Clear();
        humanConfirmed = false;
        Debug.Log("已清空分配，请重新选牌。");
        PrintHumanHand();
    }

    //再来一局：清空手牌但保留累计总分
    private void RestartGame()
    {
        foreach (Player p in players)
        {
            p.ClearHand();  //只清牌，不清totalScore
        }
        StartNewGame();
    }

    //状态切换的统一入口：改状态 + 秒表清零 + 打日志
    private void EnterNextState(GameState next)
    {
        currentState = next;
        stateTimer = 0f;  //秒表归零，新状态重新计时
        Debug.Log($">>> 状态切换 → {next}");
    }

    //打印真人的手牌（带序号，供键盘选择）
    private void PrintHumanHand()
    {
        Player human = players.Find(p => !p.isAI);
        if (human == null || human.handCards.Count == 0)
        {
            Debug.Log("本局你没有手牌（发牌失败），旁观 AI 对战");
            return;
        }

        string s = "你的手牌：";
        for (int i = 0; i < human.handCards.Count; i++)
        {
            s += $"[{i + 1}]{human.handCards[i].GetDisplayName()}  ";
        }
        Debug.Log(s);
    }

    //打印真人当前的分配进度
    private void PrintHumanStatus()
    {
        Player human = players.Find(p => !p.isAI);
        if (human == null) return;

        string r1 = human.round1Cards.Count > 0 ? human.round1Cards[0].GetDisplayName() : "待选";
        string r2 = human.round2Cards.Count > 0
            ? human.round2Cards[0].GetDisplayName() + (human.round2Cards.Count > 1 ? "+" + human.round2Cards[1].GetDisplayName() : "")
            : "待选";
        string r3 = human.round3Cards.Count > 0
            ? human.round3Cards[0].GetDisplayName() + (human.round3Cards.Count > 1 ? "+" + human.round3Cards[1].GetDisplayName() : "") + (human.round3Cards.Count > 2 ? "+" + human.round3Cards[2].GetDisplayName() : "")
            : "待选";

        Debug.Log($"【分配进度】第1关(1张): {r1} | 第2关(2张): {r2} | 第3关(3张): {r3}");
    }
}
