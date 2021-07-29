using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginScene : MonoBehaviour
{
    public InputField serverAddressText;
    public InputField userNameText;

    public GameObject connectingObject;
    public GameObject loginErrorObject;

    public string lobbyScene;

    public void Login()
    {
        StartCoroutine(LoginCoroutine());
    }

    private IEnumerator LoginCoroutine()
    {
        var serverAddress = serverAddressText.text;
        if (string.IsNullOrEmpty(serverAddress))
            serverAddress = "127.0.0.1";

        if (Network.instance.io.IsConnected)
        {
            Network.instance.io.Close();
            yield return Network.instance.WaitForDisconnect();
        }

        Network.instance.io.url = $"ws://{serverAddress}/socket.io/?EIO=4&transport=websocket";
        Network.instance.io.Connect();
        yield return Network.instance.WaitForConnect();

        connectingObject.SetActive(true);
        loginErrorObject.SetActive(false);

        var username = userNameText.text;

        if (!string.IsNullOrEmpty(username))
        {
            var login = Network.instance.Login(username);
            yield return login;

            connectingObject.SetActive(false);

            if (string.IsNullOrEmpty(login.value))
            {
                loginErrorObject.SetActive(true);
                Debug.Log($"Login failed");
            }
            else
            {
                Debug.Log($"Login success");
                SceneManager.LoadScene(lobbyScene);
            }
        }
        else
        {
            connectingObject.SetActive(false);
        }
    }
}
