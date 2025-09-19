using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class RegisterRequest { public string username; public string password; }
[System.Serializable]
public class RegisterResponse { public bool success; public string userId; public string error; }

[System.Serializable]
public class LoginRequest { public string username; public string password; }
[System.Serializable]
public class LoginResponse
{
    public bool success;
    public string token;
    public string userId;
    public string username;
    public string error;
}

public class AuthManager : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:3000";

    private void Start()
    {
        //Register("moonlite", "12345");
        //Login("sunshine", "123");
        Login("moonlite", "12345");
    }


    public void Register(string username, string password)
    {
        StartCoroutine(RegisterRoutine(username, password));
    }

    public void Login(string username, string password)
    {
        StartCoroutine(LoginRoutine(username, password));
    }

    private IEnumerator RegisterRoutine(string username, string password)
    {
        var payload = new RegisterRequest { username = username, password = password };
        string json = JsonUtility.ToJson(payload);

        using (var www = new UnityWebRequest($"{baseUrl}/auth/register", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var res = JsonUtility.FromJson<RegisterResponse>(www.downloadHandler.text);
                if (res.success)
                {
                    Debug.Log($"Registered user. ID={res.userId}");
                }
                else
                {
                    Debug.LogError($"Registration failed: {res.error}");
                }
            }
            else
            {
                Debug.LogError($"HTTP error: {www.responseCode} {www.error}");
            }
        }
    }

    private IEnumerator LoginRoutine(string username, string password)
    {
        var payload = new LoginRequest { username = username, password = password };
        string json = JsonUtility.ToJson(payload);

        using (var www = new UnityWebRequest($"{baseUrl}/auth/login", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log("Response: " + www.downloadHandler.text);

                var res = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);
                if (res.success)
                {
                    // Save both token and userId
                    PlayerPrefs.SetString("auth_token", res.token);
                    PlayerPrefs.SetString("user_id", res.userId);
                    PlayerPrefs.SetString("username", res.username);
                    PlayerPrefs.Save();

                    Debug.Log($"Login successful. UserId={res.userId}, Username={res.username}");

                    // Example: connect WS right after login
                    var wsClient = FindFirstObjectByType<AdminWsClient>();
                    if (wsClient != null)
                    {
                        wsClient.ConnectAndLogin(res.userId);
                    }
                }
                else
                {
                    Debug.LogError($"Login failed: {res.error}");
                }
            }
            else
            {
                Debug.LogError($"HTTP error: {www.responseCode} {www.error}");
            }
        }
    }
}
