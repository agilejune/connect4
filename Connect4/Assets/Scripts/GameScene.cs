using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameScene : MonoBehaviour
{
    public string lobbyScene;

    public void Leave()
    {
        Network.instance.LeaveGame();

        SceneManager.LoadScene(lobbyScene);
    }
}
