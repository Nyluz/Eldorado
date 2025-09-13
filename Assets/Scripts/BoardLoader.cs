using UnityEngine;

public enum ResourceType { Wood, Brick, Sheep, Wheat, Ore, Desert, Water }

[System.Serializable]
public class TileData
{
    public int q;
    public int r;
    public string type;
}

[System.Serializable]
public class GameData
{
    public TileData[] board;
}

public class BoardLoader : MonoBehaviour
{
    private float hexSize = 1f;

    [Header("Prefabs")]
    public GameObject woodPrefab;
    public GameObject brickPrefab;
    public GameObject sheepPrefab;
    public GameObject wheatPrefab;
    public GameObject orePrefab;
    public GameObject desertPrefab;
    public GameObject waterPrefab;

    private Transform tileParent;

    [ContextMenu("Load Board")]

    // Extract just the board array from the server JSON
    string ExtractBoardJson(string fullJson)
    {
        // crude but effective way to extract state.board
        int start = fullJson.IndexOf("\"board\":");
        if (start < 0) return "[]";
        start += 8;
        int end = fullJson.IndexOf(']', start);
        return fullJson.Substring(start, end - start + 1);
    }

    [System.Serializable]
    private class GameWrapper
    {
        public TileData[] board;
    }

    public void BuildBoard(string json)
    {
        var wrapper = JsonUtility.FromJson<GameWrapper>("{\"board\":" + ExtractBoardJson(json) + "}");
        TileData[] tiles = wrapper.board;

        if (tileParent == null)
        {
            tileParent = new GameObject("_HexTiles").transform;
            tileParent.parent = this.transform;
        }

        foreach (Transform child in tileParent)
            Destroy(child.gameObject);

        foreach (var t in tiles)
        {
            Vector3 pos = AxialToWorld(t.q, t.r);
            GameObject prefab = GetPrefab(t.type);
            if (prefab == null) continue;

            var go = Instantiate(prefab, pos, Quaternion.identity, tileParent);
        }
    }

    Vector3 AxialToWorld(int q, int r)
    {
        float x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
        float z = hexSize * (3f / 2f * r);
        return new Vector3(x, 0f, z);
    }

    GameObject GetPrefab(string type)
    {
        switch (type)
        {
            case "Wood": return woodPrefab;
            case "Brick": return brickPrefab;
            case "Sheep": return sheepPrefab;
            case "Wheat": return wheatPrefab;
            case "Ore": return orePrefab;
            case "Desert": return desertPrefab;
            case "Water": return waterPrefab;
            default: return null;
        }
    }
}
