using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyScene : MonoBehaviour
{
    public Text playerNameText;
    public Text playerScoreText;

    public Transform scoreBoardPanel;
    public Text scorePrefab;

    public Transform gameBoardPanel;
    public LobbyGameButton gameButtonPrefab;

    public InputField rowsInput;
    public InputField colsInput;

    public string gameScene;

    private Dictionary<string, LobbyGameButton> _gameButtons = new Dictionary<string, LobbyGameButton>();

    private void Start()
    {
        UpdatePlayerInfo();

        CreateScoreBoard();
        UpdateScoreBoard();

        CreateGameBoard();
    }

    private void OnEnable()
    {
        Network.instance.onScoresUpdate.AddListener(UpdateScoreBoard);
        Network.instance.onGameUpdate.AddListener(UpdateGame);
        Network.instance.onGameDestroy.AddListener(DestroyGame);
    }

    private void OnDisable()
    {
        Network.instance.onScoresUpdate.RemoveListener(UpdateScoreBoard);
        Network.instance.onGameUpdate.RemoveListener(UpdateGame);
        Network.instance.onGameDestroy.RemoveListener(DestroyGame);
    }

    private void UpdatePlayerInfo()
    {
        playerNameText.text = Network.instance.player.name;
        playerScoreText.text = Network.instance.player.score.ToString();
    }

    private void CreateScoreBoard()
    {
        for (int i = 0; i < 10; ++i)
            Instantiate(scorePrefab, scoreBoardPanel);
    }

    private void UpdateScoreBoard()
    {
        var texts = scoreBoardPanel.GetComponentsInChildren<Text>();
        int i = 0;
        foreach (var kv in Network.instance.scores.Take(10))
        {
            texts[i].text = $"#{i + 1}: {kv.Key}: {kv.Value}";
            ++i;
        }
    }

    private void CreateGameBoard()
    {
        foreach (var gameid in Network.instance.games)
        {
            var game = Network.instance.GetGameById(gameid);
            UpdateGame(game);
        }
    }

    private void UpdateGame(Network.Game game)
    {
        LobbyGameButton gameButton;
        if (_gameButtons.ContainsKey(game.id))
            gameButton = _gameButtons[game.id];
        else
        {
            gameButton = Instantiate(gameButtonPrefab, gameBoardPanel);
            gameButton.SetAction(() => JoinGame(game));
            _gameButtons[game.id] = gameButton;
        }

        gameButton.SetGame(game);
    }

    private void DestroyGame(Network.Game game)
    {
        if (_gameButtons.ContainsKey(game.id))
        {
            Destroy(_gameButtons[game.id].gameObject);
            _gameButtons.Remove(game.id);
        }
    }

    private void JoinGame(Network.Game game)
    {
        StartCoroutine(JoinGameCoroutine(game));
    }

    private IEnumerator JoinGameCoroutine(Network.Game game)
    { 
        var ret = Network.instance.JoinGame(game.id);
        yield return ret;

        if (!string.IsNullOrEmpty(ret.value))
        {
            SceneManager.LoadScene(gameScene);
        }
        else
        {
            Debug.LogWarning("Failed to create game.");
        }
    }

    public void CreateGame()
    {
        var rows = int.Parse(rowsInput.text);
        var cols = int.Parse(colsInput.text);
        if (rows > 0 && cols > 0)
            StartCoroutine(CreateGameCoroutine(rows, cols));
    }

    private IEnumerator CreateGameCoroutine(int rows, int cols)
    {
        var ret = Network.instance.CreateGame(rows, cols);
        yield return ret;

        if (!string.IsNullOrEmpty(ret.value))
        {
            SceneManager.LoadScene(gameScene);
        }
        else
        {
            Debug.LogWarning("Failed to create game.");
        }
    }
}
