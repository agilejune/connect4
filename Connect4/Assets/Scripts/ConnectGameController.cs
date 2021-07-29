using DG.Tweening;
using SocketIO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectGameController : MonoBehaviour
{
    public Transform blackBoardRoot;
    public Transform playerBoardRoot;

    public float intervalX = 0.1f;
    public float intervalY = 0.1f;

    public Transform blackDiskPrefab;
    public Transform[] playerDiskPrefabs;

    public Text[] playerTexts;

    public GameObject readyButton;
    public GameObject winPanel;
    public GameObject losePanel;

    private SocketIOComponent io;
    private Network.Player player;
    private Network.Game game;

    private int rows, cols;

    private Transform[] blackDisks;
    private List<Transform> playerDisks = new List<Transform>();

    private void Start()
    {
        io = Network.instance.io;
        player = Network.instance.player;
        game = Network.instance.playerGame;

        rows = (int)game.data["rows"].n;
        cols = (int)game.data["cols"].n;

        io.On("_ready", OnReady);
        io.On("_start", OnStart);
        io.On("_drop", OnDrop);
        io.On("_end", OnEnd);

        io.On("_join", OnPlayerJoin);
        io.On("_leave", OnPlayerLeave);

        io.Emit("playerReady");

        CreateBoardSprites();
        UpdatePlayerTexts();
    }

    private void OnDisable()
    {
        io.Off("_ready", OnReady);
        io.Off("_ready", OnStart);
        io.Off("_drop", OnDrop);
        io.Off("_end", OnEnd);

        io.Off("_join", OnPlayerJoin);
        io.Off("_leave", OnPlayerLeave);
    }

    private void CreateBoardSprites()
    {
        blackDisks = new Transform[cols * rows];

        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < cols; ++c)
            {
                var black = Instantiate(blackDiskPrefab, blackBoardRoot);
                black.localPosition = GetDiskWorldPosition(r, c);
                SetBlackDisk(r, c, black);
            }
        }
    }

    private void UpdatePlayerTexts()
    {
        int i = 0;
        foreach (var text in playerTexts)
        {
            if (i < game.players.Length)
            {
                var player = Network.instance.GetPlayerById(game.players[i]);
                text.text = $"{player.name}: {player.score}";
            }
            else
                text.text = string.Empty;

            ++i;
        }
    }

    private Vector3 GetDiskWorldPosition(int r, int c)
    {
        var width = intervalX * (cols - 1);
        return new Vector3(c * intervalX - width * .5f, r * intervalY);
    }

    private void SetBlackDisk(int r, int c, Transform disk)
    {
        blackDisks[r * cols + c] = disk;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryDrop();
        }
    }

    private void TryDrop()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var diskPos = GetDiskAt(ray);
        if (diskPos != null)
        {
            print($"Drop: {diskPos.Item2}");

            io.Emit("_drop", new JSONObject(diskPos.Item2));
        }
    }

    private Tuple<int,int> GetDiskAt(Ray ray)
    {
        var hit = Physics2D.GetRayIntersection(ray);
        if (hit.transform == null)
            return null;
        int index = Array.IndexOf(blackDisks, hit.transform);
        return new Tuple<int, int>(index / cols, index % cols);
    }

    private void DropDisk(int playerIndex, int row, int col)
    {
        var topPos = GetDiskWorldPosition(rows - 1, col);
        var bottomPos = GetDiskWorldPosition(row, col);

        var disk = Instantiate(playerDiskPrefabs[playerIndex], playerBoardRoot);
        disk.localPosition = topPos;

        playerDisks.Add(disk);

        disk.DOLocalMove(bottomPos, .5f).SetEase(Ease.OutBounce);
    }

    private void OnPlayerJoin(SocketIOEvent obj)
    {
        foreach (var disk in playerDisks)
            Destroy(disk.gameObject);
        playerDisks.Clear();

        winPanel.SetActive(false);
        losePanel.SetActive(false);
        readyButton.SetActive(false);

        UpdatePlayerTexts();
    }

    private void OnPlayerLeave(SocketIOEvent obj)
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        readyButton.SetActive(false);

        UpdatePlayerTexts();
    }

    public void Ready()
    {
        io.Emit("_ready");
    }

    private void OnReady(SocketIOEvent e)
    {
        Debug.Log("SERVER -> ready: " + e.data);
        readyButton.SetActive(true);
    }

    private void OnStart(SocketIOEvent e)
    {
        Debug.Log("SERVER -> start: " + e.data);
        readyButton.SetActive(false);
    }

    private void OnDrop(SocketIOEvent e)
    {
        Debug.Log("SERVER -> drop: " + e.data);

        var playerIndex = Array.IndexOf(game.players, e.data["playerid"].str);
        if (playerIndex < 0)
            return;

        DropDisk(playerIndex, (int)e.data["row"].n, (int)e.data["col"].n);
    }

    private void OnEnd(SocketIOEvent e)
    {
        Debug.Log("SERVER -> end: " + e.data);

        var playerid = e.data["winner"].str;
        if (playerid == player.id)
        {
            winPanel.SetActive(true);
        }
        else
        {
            losePanel.SetActive(true);
        }

        UpdatePlayerTexts();
        StartCoroutine(Restart());
    }

    private IEnumerator Restart()
    {
        var expire = Time.time + 4;
        yield return new WaitUntil(() => Time.time > expire || Input.GetMouseButtonDown(0));

        io.Emit("playerReady");
    }
}
