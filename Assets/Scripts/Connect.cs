using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class Connect : MonoBehaviour
{
    string baseUrl = "http://localhost:3000";

    void Start()
    {
        StartCoroutine(ConnectToBackend());
        StartCoroutine(CreateGame());
    }

    IEnumerator ConnectToBackend()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Server says: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + www.error);
            }
        }
    }

    [System.Serializable]
    public class CreateGameResponse
    {
        public string gameId;
    }

    IEnumerator CreateGame()
    {
        using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/game", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log("Server says: " + json);

                // Parse JSON to get gameId
                CreateGameResponse response = JsonUtility.FromJson<CreateGameResponse>(json);
                string gameId = response.gameId;

                // Now you can use gameId
                StartCoroutine(JoinGame(gameId, "Niels"));
            }
            else
            {
                Debug.LogError("Error: " + www.error);
            }
        }
    }

    [System.Serializable]
    public class JoinRequest
    {
        public string playerName;
    }

    IEnumerator JoinGame(string gameId, string playerName)
    {
        JoinRequest data = new JoinRequest { playerName = playerName };
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/game/" + gameId + "/join", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("Join response: " + www.downloadHandler.text);
            else
                Debug.LogError("Error: " + www.error);
        }
    }

    [System.Serializable]
    public class PlayerData
    {
        public string id;
        public string name;
        public int score;
    }

    IEnumerator SendPlayerData()
    {
        PlayerData data = new PlayerData { id = "123", name = "Niels", score = 100 };
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/player", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("Response: " + www.downloadHandler.text);
            else
                Debug.LogError("Error: " + www.error);
        }
    }
}