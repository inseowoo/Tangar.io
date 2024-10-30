using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using TMPro;

public class RankInfo
{
    public string nickname;
    public int score;
}
public class GameScorePanel : NetworkBehaviour
{
    Dictionary<PlayerRef, RankInfo> _rankDic = new Dictionary<PlayerRef, RankInfo>();
    List<RankInfo> _rankInfos = new List<RankInfo>();

    [SerializeField] private TMP_Text _rankText;
    [SerializeField] private TMP_Text _nickText;
    [SerializeField] private TMP_Text _scoreText;

    // Add new player info entry
    // sort : option for sort after modification
    public void AddEntry(PlayerRef playerRef, PlayerDataNetworked playerData, bool sort)
    {
        if (playerData == null) return;

        RankInfo rankInfo = new RankInfo
        {
            nickname = playerData.NickName.ToString(),
            score = playerData.Score
        };

        _rankDic.Add(playerRef, rankInfo);
        _rankInfos.Add(rankInfo);

        if (sort) SortAndDisplayRankInfo();
    }

    // Update every player info
    public void UpdateRankInfo(PlayerDataNetworked playerData)
    {
        _rankInfos.Clear();

        foreach (var playerRef in Runner.ActivePlayers)
        {
            UpdateRankInfo(playerRef, playerData, false);
        }

        SortAndDisplayRankInfo();
    }

    // Update single player info
    // sort : option for sort after modification
    public void UpdateRankInfo(PlayerRef playerRef, PlayerDataNetworked playerData, bool sort)
    {
        if (playerData == null) return;

        if (_rankDic.ContainsKey(playerRef))
        {
            _rankDic[playerRef].nickname = playerData.NickName.ToString();
            _rankDic[playerRef].score = playerData.Score;
        }
        else
        {
            AddEntry(playerRef, playerData, sort);
        }

        if (sort) SortAndDisplayRankInfo();
    }

    // Sort rank list by score in descending order and display
    private void SortAndDisplayRankInfo()
    {
        _rankInfos.Sort((A, B) => B.score.CompareTo(A.score));

        DisplayRankInfo();
    }

    private void DisplayRankInfo()
    {
        _rankText.text = "";
        _nickText.text = "";
        _scoreText.text = "";

        for (int i = 0; i < _rankInfos.Count; i++)
        {
            _rankText.text +=  (i + 1).ToString() + "\n";
            _nickText.text += _rankInfos[i].nickname + "\n";
            _scoreText.text += _rankInfos[i].score.ToString() + "\n";
        }
    }
}
