using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;
using System;
using System.Linq;
using UnityEngine.Events;

[RequireComponent(typeof(SocketIOComponent))]
public class Network : MonoBehaviour
{
    public class Player
    {
        public string id { get; private set; }
        public string name { get; private set; }
        public int score { get; private set; }
        public string gameid { get; private set; }

        public void ParseJson(JSONObject json)
        {
            id = json["id"].str;
            name = json["name"].str;
            score = (int)json["score"].n;
            gameid = json["gameid"].str;
        }
    }

    private string _playerid;

    public Player player => _players[_playerid];

    private Dictionary<string, Player> _players = new Dictionary<string, Player>();

    public IEnumerable<string> players => _players.Keys;

    public Player GetPlayerById(string id) => _players.ContainsKey(id) ? _players[id] : null;

    public class PlayerEvent : UnityEvent<Player> { }
    public PlayerEvent onPlayerUpdate = new PlayerEvent();
    public PlayerEvent onPlayerDestroy = new PlayerEvent();

    public class Game
    {
        private Network network;

        public string type { get; private set; }
        public string id { get; private set; }
        public string owner { get; private set; }

        public JSONObject data { get; private set; }

        public string[] players { get; private set; }

        public void ParseJson(JSONObject json)
        {
            type = json["type"].str;
            id = json["id"].str;
            owner = json["owner"].str;
            data = json["data"];
            players = json["players"].list.Select(i => i.str).ToArray();
        }
    }
    private Dictionary<string, Game> _games = new Dictionary<string, Game>();

    public IEnumerable<string> games => _games.Keys;

    public Game GetGameById(string id) => _games.ContainsKey(id) ? _games[id] : null;

    public class GameEvent : UnityEvent<Game> { }
    public GameEvent onGameUpdate = new GameEvent();
    public GameEvent onGameDestroy = new GameEvent();

    public Game playerGame => string.IsNullOrEmpty(player.gameid) ? null : _games[player.gameid];

    private Dictionary<string, int> _scores = new Dictionary<string, int>();
    public UnityEvent onScoresUpdate = new UnityEvent();

    public IEnumerable<KeyValuePair<string, int>> scores => _scores.OrderByDescending(i => i.Value);

    public static Network instance { get; private set; }

    public SocketIOComponent io { get; private set; }

    private bool _connected;

    private void Awake()
    {
        if (instance != null)
        {
            DestroyImmediate(gameObject);
            return;
        }

        instance = this;

        io = GetComponent<SocketIOComponent>();
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        io.On("connect", OnConnect);
        io.On("error", OnError);

        io.On("updateScore", OnUpdateScore);

        io.On("updatePlayer", OnUpdatePlayer);
        io.On("playerDestroyed", OnPlayerDestroyed);

        io.On("updateGame", OnUpdateGame);
        io.On("gameDestroyed", OnGameDestroyed);
    }

    public CustomYieldInstruction WaitForConnect()
    {
        return new WaitUntil(() => _connected);
    }

    public FutureValue<string> Login(string username)
    {
        var ret = new FutureValue<string>();

        io.Emit("login", new JSONObject((obj) =>
        {
            obj.AddField("username", username);
        }), (result) =>
        {
            result = result[0];

            if (result["result"].b)
            {
                _playerid = result["playerid"].str;
                ret.SetValue(_playerid);
            }
            else
            {
                ret.SetValue(null);
            }
        });

        return ret;
    }

    public FutureValue<string> CreateGame(string type, JSONObject data)
    {
        var ret = new FutureValue<string>();

        io.Emit("createGame", new JSONObject((obj) =>
        {
            obj.AddField("type", type);
            obj.AddField("data", data);
        }), (result) =>
        {
            result = result[0];

            if (result["result"].b)
            {
                var gameid = result["gameid"].str;
                ret.SetValue(gameid);
            }
            else
            {
                ret.SetValue(null);
            }
        });

        return ret;
    }

    public FutureValue<string> JoinGame(string gameid)
    {
        var ret = new FutureValue<string>();

        io.Emit("joinGame", new JSONObject((obj) =>
        {
            obj.AddField("gameid", gameid);
        }), (result) =>
        {
            result = result[0];

            if (result["result"].b)
            {
                gameid = result["gameid"].str;
                ret.SetValue(gameid);
            }
            else
            {
                ret.SetValue(null);
            }
        });

        return ret;
    }

    public void LeaveGame()
    {
        io.Emit("leaveGame");
    }

    private void OnConnect(SocketIOEvent e)
    {
        _connected = true;

        Debug.Log("SocketIO connected");
    }

    private void OnError(SocketIOEvent e)
    {
        Debug.LogWarning($"SocketIO error");
    }

    private void OnUpdateScore(SocketIOEvent e)
    {
        Debug.Log("SERVER -> updateScore: " + e.data);
        _scores = e.data["scores"].list.ToDictionary(p=>p["name"].str, p => (int)p["score"].n);

        onScoresUpdate.Invoke();
    }

    private void OnUpdatePlayer(SocketIOEvent e)
    {
        Debug.Log("SERVER -> updatePlayer: " + e.data);
        var playerId = e.data["id"].str;
        Player player;
        if (_players.ContainsKey(playerId))
            player = _players[playerId];
        else
        {
            player = new Player();
            _players[playerId] = player;
        }
        player.ParseJson(e.data);
        onPlayerUpdate.Invoke(player);
    }

    private void OnPlayerDestroyed(SocketIOEvent e)
    {
        Debug.Log("SERVER -> playerDestroyed: " + e.data);
        var playerId = e.data["id"].str;
        if (_players.ContainsKey(playerId))
        {
            var player = _players[playerId];
            _players.Remove(playerId);
            onPlayerDestroy.Invoke(player);
        }
    }

    private void OnUpdateGame(SocketIOEvent e)
    {
        Debug.Log("SERVER -> updateGame: " + e.data);
        var gameId = e.data["id"].str;
        Game game;
        if (_games.ContainsKey(gameId))
            game = _games[gameId];
        else
        {
            game = new Game();
            _games[gameId] = game;
        }
        game.ParseJson(e.data);

        onGameUpdate.Invoke(game);
    }

    private void OnGameDestroyed(SocketIOEvent e)
    {
        Debug.Log("SERVER -> gameDestroyed: " + e.data);
        var gameId = e.data["id"].str;
        if (_games.ContainsKey(gameId))
        {
            var game = _games[gameId];
            _games.Remove(gameId);
            onGameDestroy.Invoke(game);
        }
    }
}
