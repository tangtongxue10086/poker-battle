using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// UI 管理器（UI-1：计分板）
public class UIManager : MonoBehaviour
{
    [Header("游戏主逻辑引用")]
    public GameManager gameManager;

    [Header("玩家名字 Text（PlayerScore_1~4 / NameText）")]
    public Text player1Name;
    public Text player2Name;
    public Text player3Name;
    public Text player4Name;

    [Header("玩家分数 Text（PlayerScore_1~4 / ScoreText）")]
    public Text player1Score;
    public Text player2Score;
    public Text player3Score;
    public Text player4Score;

    private Text[] nameTexts;
    private Text[] scoreTexts;

    void Awake()
    {
        nameTexts = new Text[] { player1Name, player2Name, player3Name, player4Name };
        scoreTexts = new Text[] { player1Score, player2Score, player3Score, player4Score };
    }

    void Update()
    {
        RefreshScoreboard();
    }

    //刷新计分板：遍历 gameManager.GetPlayers()，刷名字与总分
    //玩家列表为空（未开局）时直接跳过，不报错
    public void RefreshScoreboard()
    {
        if (gameManager == null) return;

        List<Player> players = gameManager.GetPlayers();
        if (players == null || players.Count == 0) return;

        for (int i = 0; i < nameTexts.Length; i++)
        {
            if (i >= players.Count) break;

            if (nameTexts[i] != null)
            {
                nameTexts[i].text = players[i].playerName;
            }
            if (scoreTexts[i] != null)
            {
                scoreTexts[i].text = players[i].totalScore + "分";
            }
        }
    }
}
