using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginScene : MonoBehaviour
{
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
        yield return Network.instance.WaitForConnect();

        Debug.Log($"Socket io connected");

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
