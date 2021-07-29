using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyGameButton : MonoBehaviour
{
    public Text infoText;
    public Text[] playerTexts;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void SetAction(UnityAction action)
    {
        button.onClick.AddListener(action);
    }

    public void SetGame(Network.Game game)
    {
        infoText.text = game.desc;
        playerTexts[0].text = game.players.Length > 0 ? Network.instance.GetPlayerById(game.players[0]).name : string.Empty;
        playerTexts[1].text = game.players.Length > 1 ? Network.instance.GetPlayerById(game.players[1]).name : string.Empty;

        button.interactable = game.players.Length < 2;
    }
}
