using System.Collections.Generic;
using UnityEngine;

//public enum ResourceType { Wood, Brick, Sheep, Wheat, Ore, Desert, Water }

[System.Serializable]
public class TilePrefabs
{
    public GameObject woodPrefab;
    public GameObject brickPrefab;
    public GameObject sheepPrefab;
    public GameObject wheatPrefab;
    public GameObject orePrefab;
    public GameObject desertPrefab;
    public GameObject waterPrefab;
}

public class HexTile
{
    public Vector2Int axial; // q,r
    public Vector3 worldPos;
    public ResourceType type;
    public float noiseValue;
}

public class HexBoardGenerator : MonoBehaviour
{
    [Header("Grid")]
    public int radius = 4;
    public float hexSize = 1f;

    [Header("Noise / Terrain")]
    public float scale = 0.8f;
    public Vector2 noiseOffset = Vector2.zero;

    [Header("Resource Settings")]
    [Range(0f, 0.5f)] public float desertFraction = 0.1f;

    [Header("Water Settings")]
    [Range(0f, 1f)] public float waterFraction = 0.15f; // fraction of land tiles to become water
    [Range(0f, 1f)] public float waterClusterFactor = 0.6f; // higher = more clustered
    [Range(1, 5)] public int edgeWaterDepth = 2;

    [Header("Clustering / smoothing")]
    public int smoothingPasses = 3;

    [Header("Prefabs")]
    public TilePrefabs prefabs;

    private Dictionary<Vector2Int, HexTile> tiles = new Dictionary<Vector2Int, HexTile>();
    private Transform tileParent;

    void Start()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        ClearChildren();
        tiles.Clear();

        // Randomize noise offset so each map is unique
        noiseOffset = new Vector2(
            Random.Range(-100000f, 100000f),
            Random.Range(-100000f, 100000f)
        );

        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Mathf.Max(-radius, -q - radius);
            int r2 = Mathf.Min(radius, -q + radius);
            for (int r = r1; r <= r2; r++)
            {
                HexTile t = new HexTile();
                t.axial = new Vector2Int(q, r);
                t.worldPos = AxialToWorld(q, r);
                tiles.Add(t.axial, t);
            }
        }

        foreach (var kv in tiles)
        {
            var t = kv.Value;
            float nx = (t.worldPos.x + noiseOffset.x) * scale;
            float ny = (t.worldPos.z + noiseOffset.y) * scale;
            t.noiseValue = Mathf.PerlinNoise(nx, ny);

            // Force edge tiles to water by setting noiseValue very low
            if (Mathf.Abs(t.axial.x) == radius || Mathf.Abs(t.axial.y) == radius || Mathf.Abs(t.axial.x + t.axial.y) == radius)
            {
                t.noiseValue = 0f;
            }
        }

        AssignWater();
        AssignResources();
        for (int i = 0; i < smoothingPasses; i++) SmoothResources();
        InstantiatePrefabs();
    }

    void ClearChildren()
    {
        if (tileParent == null) tileParent = new GameObject("_HexTiles").transform;
        for (int i = tileParent.childCount - 1; i >= 0; i--)
        {
            Destroy(tileParent.GetChild(i).gameObject);
        }
        tileParent.parent = this.transform;
        tileParent.localPosition = Vector3.zero;
    }

    Vector3 AxialToWorld(int q, int r)
    {
        float x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
        float z = hexSize * (3f / 2f * r);
        return new Vector3(x, 0f, z);
    }

    void AssignWater()
    {
        List<HexTile> landTiles = new List<HexTile>();
        foreach (var kv in tiles)
        {
            var t = kv.Value;

            // Force multiple edge rows to water
            if (Mathf.Abs(t.axial.x) >= radius - edgeWaterDepth + 1 ||
                Mathf.Abs(t.axial.y) >= radius - edgeWaterDepth + 1 ||
                Mathf.Abs(t.axial.x + t.axial.y) >= radius - edgeWaterDepth + 1)
            {
                t.type = ResourceType.Water;
                continue;
            }

            if (t.type != ResourceType.Water) landTiles.Add(t);
        }

        int waterCount = Mathf.RoundToInt(landTiles.Count * waterFraction);

        // Shuffle candidate tiles
        for (int i = 0; i < landTiles.Count; i++)
        {
            var tmp = landTiles[i];
            int j = Random.Range(i, landTiles.Count);
            landTiles[i] = landTiles[j];
            landTiles[j] = tmp;
        }

        List<HexTile> waterTiles = new List<HexTile>();

        for (int i = 0; i < waterCount; i++)
        {
            HexTile t;
            if (waterTiles.Count > 0 && Random.value < waterClusterFactor)
            {
                // Pick a neighbor of an existing water tile
                var neighbors = new List<HexTile>();
                foreach (var w in waterTiles)
                {
                    foreach (var nPos in GetNeighbors(w.axial))
                    {
                        if (tiles.ContainsKey(nPos))
                        {
                            var nTile = tiles[nPos];
                            if (nTile.type != ResourceType.Water && !neighbors.Contains(nTile))
                                neighbors.Add(nTile);
                        }
                    }
                }
                if (neighbors.Count > 0)
                {
                    t = neighbors[Random.Range(0, neighbors.Count)];
                }
                else
                {
                    t = landTiles[i];
                }
            }
            else
            {
                t = landTiles[i];
            }

            t.type = ResourceType.Water;
            waterTiles.Add(t);
        }
    }

    void AssignResources()
    {
        List<HexTile> landTiles = new List<HexTile>();
        foreach (var kv in tiles)
            if (kv.Value.type != ResourceType.Water)
                landTiles.Add(kv.Value);

        // Shuffle to randomize
        for (int i = 0; i < landTiles.Count; i++)
        {
            var tmp = landTiles[i];
            int j = Random.Range(i, landTiles.Count);
            landTiles[i] = landTiles[j];
            landTiles[j] = tmp;
        }

        int desertCount = Mathf.RoundToInt(landTiles.Count * desertFraction);
        int resourceCount = landTiles.Count - desertCount;
        int perResource = resourceCount / 5; // 5 main resources

        for (int i = 0; i < landTiles.Count; i++)
        {
            if (i < perResource) landTiles[i].type = ResourceType.Wood;
            else if (i < perResource * 2) landTiles[i].type = ResourceType.Brick;
            else if (i < perResource * 3) landTiles[i].type = ResourceType.Sheep;
            else if (i < perResource * 4) landTiles[i].type = ResourceType.Wheat;
            else if (i < perResource * 5) landTiles[i].type = ResourceType.Ore;
            else landTiles[i].type = ResourceType.Desert;
        }
    }

    void SmoothResources()
    {
        var newTypes = new Dictionary<Vector2Int, ResourceType>();
        foreach (var kv in tiles)
        {
            var key = kv.Key;
            var t = kv.Value;
            if (t.type == ResourceType.Water) { newTypes[key] = ResourceType.Water; continue; }

            var tally = new Dictionary<ResourceType, int>();
            void Inc(ResourceType rt)
            {
                if (tally.ContainsKey(rt)) tally[rt]++; else tally[rt] = 1;
            }

            Inc(t.type);
            foreach (var n in GetNeighbors(key)) if (tiles.ContainsKey(n)) Inc(tiles[n].type);

            ResourceType best = t.type;
            int bestCount = -1;
            foreach (var kv2 in tally)
            {
                if (kv2.Key == ResourceType.Water) continue;
                if (kv2.Value > bestCount) { best = kv2.Key; bestCount = kv2.Value; }
            }
            newTypes[key] = best;
        }

        foreach (var kv in newTypes) tiles[kv.Key].type = kv.Value;
    }

    List<Vector2Int> GetNeighbors(Vector2Int axial)
    {
        var dirs = new Vector2Int[] {
            new Vector2Int(+1, 0), new Vector2Int(+1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, +1), new Vector2Int(0, +1)
        };
        var outList = new List<Vector2Int>();
        foreach (var d in dirs) outList.Add(axial + d);
        return outList;
    }

    void InstantiatePrefabs()
    {
        if (tileParent == null) tileParent = new GameObject("_HexTiles").transform;
        tileParent.parent = this.transform;
        tileParent.localPosition = Vector3.zero;

        foreach (var kv in tiles)
        {
            var t = kv.Value;
            GameObject prefab = null;
            switch (t.type)
            {
                case ResourceType.Wood: prefab = prefabs.woodPrefab; break;
                case ResourceType.Brick: prefab = prefabs.brickPrefab; break;
                case ResourceType.Sheep: prefab = prefabs.sheepPrefab; break;
                case ResourceType.Wheat: prefab = prefabs.wheatPrefab; break;
                case ResourceType.Ore: prefab = prefabs.orePrefab; break;
                case ResourceType.Desert: prefab = prefabs.desertPrefab; break;
                case ResourceType.Water: prefab = prefabs.waterPrefab; break;
            }

            if (prefab == null) continue;

            var go = Instantiate(prefab);
            go.transform.position = t.worldPos;
            go.transform.parent = tileParent;
        }
    }
}
