using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class Connect : MonoBehaviour
{
    public BoardLoader boardLoader;
    public string serverUrl = "http://localhost:3000";
    public string gameId;
    public string playerName;

    private void Awake()
    {
        boardLoader = FindAnyObjectByType<BoardLoader>();
    }

    public void CreateGameRoutine()
    {
        StartCoroutine(CreateGame());
    }

    public void JoinGameRoutine()
    {
        StartCoroutine(JoinGame(gameId, playerName));
    }

    [System.Serializable]
    public class CreateGameResponse
    {
        public string gameId;
    }

    IEnumerator CreateGame()
    {
        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/game", "POST"))
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
                gameId = response.gameId;

                // Now you can use gameId
                StartCoroutine(JoinGame(gameId, playerName));
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

        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/game/" + gameId + "/join", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Join response: " + www.downloadHandler.text);
                StartCoroutine(LoadBoardRoutine());
            }
            else
            {
                Debug.LogError("Error: " + www.error);
            }
        }
    }

    IEnumerator LoadBoardRoutine()
    {
        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogError("No gameId set.");
            yield break;
        }

        string url = $"{serverUrl}/game/{gameId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load board: " + www.error);
                yield break;
            }

            // Parse JSON
            var json = www.downloadHandler.text;

            boardLoader.BuildBoard(json);
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

        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/player", "POST"))
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