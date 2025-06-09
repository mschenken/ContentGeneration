using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StreetPatternGenerator : MonoBehaviour {
    public int gridSize = 1;
    public int randomSeed = 0;
    public Material[] streetMaterials;
    public GameObject[] buildingPrefabs;
    private const float CELL_SIZE = 10f;
    private const float ROAD_WIDTH = 1f;
    private enum StreetType {
        Empty,
        Straight,
        Turn,
        Intersection,
        TJunction,
        DeadEnd
    }

    private StreetType[,] grid;

    [Header("Ground Settings")]
    public int textureWidth = 512;
    public int textureHeight = 512;
    public float noiseScale = 20f;
    public Vector3 groundOffset = new Vector3(0, -0.1f, 0);
    private GameObject groundObject;
    private void Start() {
        Random.InitState(randomSeed);
        grid = new StreetType[gridSize, gridSize];
        GenerateStreetPattern();
        CreateTexturedGround();
    }

    private void GenerateStreetPattern() {
        // grid with empty cells
        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                grid[x, z] = StreetType.Empty;
            }
        }
        GenerateMainStreets();
        GenerateSecondaryStreets();
        EnsureProperConnections();
        CreateStreetMeshes();
        PlaceBuildings();
    }

    private void GenerateMainStreets() {
        // main street hoizontal
        int mainStreetZ = Random.Range(1, gridSize - 1);
        for (int x = 0; x < gridSize; x++) {
            grid[x, mainStreetZ] = StreetType.Straight;
        }

        // main street vertical
        int mainStreetX = Random.Range(1, gridSize - 1);
        for (int z = 0; z < gridSize; z++) {
            if (grid[mainStreetX, z] == StreetType.Straight) {
                grid[mainStreetX, z] = StreetType.Intersection;
            }
            else {
                grid[mainStreetX, z] = StreetType.Straight;
            }
        }
    }

    private void GenerateSecondaryStreets() {
        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                if (grid[x, z] == StreetType.Empty && Random.value < 0.3f) {
                    grid[x, z] = StreetType.Straight;
                    ExtendStreet(x, z);
                }
            }
        }
    }

    private void ExtendStreet(int startX, int startZ) {
        int direction = Random.Range(0, 4);
        int x = startX;
        int z = startZ;

        while (true) {
            switch (direction) {
                case 0: z++; break;
                case 1: x++; break;
                case 2: z--; break;
                case 3: x--; break;
            }

            if (x < 0 || x >= gridSize || z < 0 || z >= gridSize)
                break;
            if (grid[x, z] != StreetType.Empty)
                break;
            grid[x, z] = StreetType.Straight;
            if (Random.value < 0.2f)
                break;
        }
    }

    private void EnsureProperConnections() {
        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                if (grid[x, z] != StreetType.Empty) {
                    UpdateStreetType(x, z);
                }
            }
        }
    }

    private void UpdateStreetType(int x, int z) {
        bool hasNorth = z < gridSize - 1 && grid[x, z + 1] != StreetType.Empty;
        bool hasSouth = z > 0 && grid[x, z - 1] != StreetType.Empty;
        bool hasEast = x < gridSize - 1 && grid[x + 1, z] != StreetType.Empty;
        bool hasWest = x > 0 && grid[x - 1, z] != StreetType.Empty;

        int connectionCount = (hasNorth ? 1 : 0) + (hasSouth ? 1 : 0) + (hasEast ? 1 : 0) + (hasWest ? 1 : 0);

        switch (connectionCount) {
            case 1:
                grid[x, z] = StreetType.DeadEnd;
                break;
            case 2:
                grid[x, z] = (hasNorth && hasSouth) || (hasEast && hasWest) ? StreetType.Straight : StreetType.Turn;
                break;
            case 3:
                grid[x, z] = StreetType.TJunction;
                break;
            case 4:
                grid[x, z] = StreetType.Intersection;
                break;
        }
    }

    private void CreateStreetMeshes() {
        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                if (grid[x, z] != StreetType.Empty) {
                    Vector3 position = new Vector3(x * CELL_SIZE, 0f, z * CELL_SIZE);
                    CreateStreetComponent(grid[x, z], position, GetRotation(x, z));
                }
            }
        }
    }

    private Quaternion GetRotation(int x, int z) {
        switch (grid[x, z]) {
            case StreetType.Straight:
                return Quaternion.Euler(0f, IsVertical(x, z) ? 90f : 0f, 0f);
            case StreetType.Turn:
                return GetTurnRotation(x, z);
            case StreetType.TJunction:
                return GetTJunctionRotation(x, z);
            case StreetType.DeadEnd:
                return GetDeadEndRotation(x, z);
            default:
                return Quaternion.identity;
        }
    }

    private bool IsVertical(int x, int z) {
        return (z > 0 && grid[x, z - 1] != StreetType.Empty) || (z < gridSize - 1 && grid[x, z + 1] != StreetType.Empty);
    }

    private Quaternion GetTurnRotation(int x, int z) {
        bool hasNorth = z < gridSize - 1 && grid[x, z + 1] != StreetType.Empty;
        bool hasEast = x < gridSize - 1 && grid[x + 1, z] != StreetType.Empty;
        bool hasSouth = z > 0 && grid[x, z - 1] != StreetType.Empty;
        bool hasWest = x > 0 && grid[x - 1, z] != StreetType.Empty;

        if (hasNorth && hasEast) return Quaternion.Euler(0f, 0f, 0f);
        if (hasEast && hasSouth) return Quaternion.Euler(0f, 90f, 0f);
        if (hasSouth && hasWest) return Quaternion.Euler(0f, 180f, 0f);
        if (hasWest && hasNorth) return Quaternion.Euler(0f, 270f, 0f);

        return Quaternion.identity;
    }

    private Quaternion GetTJunctionRotation(int x, int z) {
        bool hasNorth = z < gridSize - 1 && grid[x, z + 1] != StreetType.Empty;
        bool hasEast = x < gridSize - 1 && grid[x + 1, z] != StreetType.Empty;
        bool hasSouth = z > 0 && grid[x, z - 1] != StreetType.Empty;
        bool hasWest = x > 0 && grid[x - 1, z] != StreetType.Empty;

        if (!hasNorth) return Quaternion.Euler(0f, 0f, 0f);
        if (!hasEast) return Quaternion.Euler(0f, 90f, 0f);
        if (!hasSouth) return Quaternion.Euler(0f, 180f, 0f);
        if (!hasWest) return Quaternion.Euler(0f, 270f, 0f);

        return Quaternion.identity;
    }

    private Quaternion GetDeadEndRotation(int x, int z) {
        bool hasNorth = z < gridSize - 1 && grid[x, z + 1] != StreetType.Empty;
        bool hasEast = x < gridSize - 1 && grid[x + 1, z] != StreetType.Empty;
        bool hasSouth = z > 0 && grid[x, z - 1] != StreetType.Empty;
        bool hasWest = x > 0 && grid[x - 1, z] != StreetType.Empty;

        if (hasNorth) return Quaternion.Euler(0f, 0f, 0f);
        if (hasEast) return Quaternion.Euler(0f, 90f, 0f);
        if (hasSouth) return Quaternion.Euler(0f, 180f, 0f);
        if (hasWest) return Quaternion.Euler(0f, 270f, 0f);

        return Quaternion.identity;
    }

    private void CreateStreetComponent(StreetType streetType, Vector3 position, Quaternion rotation) {
        Mesh mesh = null;

        switch (streetType) {
            case StreetType.Straight:
                mesh = CreateStraightStreetMesh();
                break;
            case StreetType.Turn:
                mesh = CreateTurnStreetMesh();
                break;
            case StreetType.Intersection:
                mesh = CreateIntersectionStreetMesh();
                break;
            case StreetType.TJunction:
                mesh = CreateTJunctionStreetMesh();
                break;
            case StreetType.DeadEnd:
                mesh = CreateDeadEndStreetMesh();
                break;
        }

        if (mesh != null) {
            GameObject street = new GameObject("Street");
            street.transform.position = position;
            street.transform.rotation = rotation;

            MeshFilter meshFilter = street.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshRenderer meshRenderer = street.AddComponent<MeshRenderer>();
            if (streetMaterials != null && streetMaterials.Length > 0) {
                meshRenderer.material = streetMaterials[Random.Range(0, streetMaterials.Length)];
            }
            else {
                Debug.LogError("No street materials assigned in the Inspector!");
            }
        }
    }

    private Mesh CreateStraightStreetMesh() {
        Vector3[] vertices = new Vector3[] {
            new Vector3(-CELL_SIZE/2, 0, -ROAD_WIDTH/2),
            new Vector3(CELL_SIZE/2, 0, -ROAD_WIDTH/2),
            new Vector3(CELL_SIZE/2, 0, ROAD_WIDTH/2),
            new Vector3(-CELL_SIZE/2, 0, ROAD_WIDTH/2)
        };

        int[] triangles = new int[] {
            0, 2, 1,
            0, 3, 2
        };
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh CreateTurnStreetMesh() {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int segments = 16;
        float innerRadius = CELL_SIZE/2 - ROAD_WIDTH/2;
        float outerRadius = CELL_SIZE/2 + ROAD_WIDTH/2;

        for (int i = 0; i <= segments; i++) {
            float angle = i * 90f / segments * Mathf.Deg2Rad;
            float cosA = Mathf.Cos(angle);
            float sinA = Mathf.Sin(angle);

            vertices.Add(new Vector3(innerRadius * cosA, 0, innerRadius * sinA));
            vertices.Add(new Vector3(outerRadius * cosA, 0, outerRadius * sinA));

            if (i > 0) {
                int baseIndex = (i - 1) * 2;
                triangles.AddRange(new int[] {
                    baseIndex, baseIndex + 2, baseIndex + 1,
                    baseIndex + 1, baseIndex + 2, baseIndex + 3
                });
            }
        }
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh CreateIntersectionStreetMesh() {
        Vector3[] vertices = new Vector3[] {
            new Vector3(-CELL_SIZE/2, 0, -ROAD_WIDTH/2),
            new Vector3(-ROAD_WIDTH/2, 0, -ROAD_WIDTH/2),
            new Vector3(ROAD_WIDTH/2, 0, -ROAD_WIDTH/2),
            new Vector3(CELL_SIZE/2, 0, -ROAD_WIDTH/2),
            new Vector3(-CELL_SIZE/2, 0, ROAD_WIDTH/2),
            new Vector3(-ROAD_WIDTH/2, 0, ROAD_WIDTH/2),
            new Vector3(ROAD_WIDTH/2, 0, ROAD_WIDTH/2),
            new Vector3(CELL_SIZE/2, 0, ROAD_WIDTH/2),
            new Vector3(-ROAD_WIDTH/2, 0, -CELL_SIZE/2),
            new Vector3(ROAD_WIDTH/2, 0, -CELL_SIZE/2),
            new Vector3(-ROAD_WIDTH/2, 0, CELL_SIZE/2),
            new Vector3(ROAD_WIDTH/2, 0, CELL_SIZE/2)
        };

        int[] triangles = new int[] {
            0, 5, 1, 0, 4, 5,
            1, 6, 2, 1, 5, 6,
            2, 7, 3, 2, 6, 7,
            8, 1, 9, 8, 0, 1,
            9, 2, 3, 9, 3, 10,
            4, 10, 5, 4, 11, 10,
            5, 11, 6, 6, 11, 7
        };
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
    private Mesh CreateTJunctionStreetMesh() {
        Vector3[] vertices = new Vector3[] {
            new Vector3(-CELL_SIZE/2, 0, -ROAD_WIDTH/2),
            new Vector3(-ROAD_WIDTH/2, 0, -ROAD_WIDTH/2),
            new Vector3(ROAD_WIDTH/2, 0, -ROAD_WIDTH/2),
            new Vector3(CELL_SIZE/2, 0, -ROAD_WIDTH/2),
            new Vector3(-CELL_SIZE/2, 0, ROAD_WIDTH/2),
            new Vector3(-ROAD_WIDTH/2, 0, ROAD_WIDTH/2),
            new Vector3(ROAD_WIDTH/2, 0, ROAD_WIDTH/2),
            new Vector3(CELL_SIZE/2, 0, ROAD_WIDTH/2),
            new Vector3(-ROAD_WIDTH/2, 0, CELL_SIZE/2),
            new Vector3(ROAD_WIDTH/2, 0, CELL_SIZE/2)
        };

        int[] triangles = new int[] {
            0, 5, 1, 0, 4, 5,
            1, 6, 2, 1, 5, 6,
            2, 7, 3, 2, 6, 7,
            5, 8, 6, 5, 9, 8,
            6, 9, 7
        };
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh CreateDeadEndStreetMesh() {
        Vector3[] vertices = new Vector3[] {
            new Vector3(-CELL_SIZE/2, 0, -ROAD_WIDTH/2),
            new Vector3(0, 0, -ROAD_WIDTH/2),
            new Vector3(0, 0, ROAD_WIDTH/2),
            new Vector3(-CELL_SIZE/2, 0, ROAD_WIDTH/2),
            new Vector3(ROAD_WIDTH/2, 0, -ROAD_WIDTH/2),
            new Vector3(ROAD_WIDTH/2, 0, ROAD_WIDTH/2)
        };

        int[] triangles = new int[] {
            0, 2, 1, 0, 3, 2,
            1, 5, 4, 1, 2, 5
        };
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private void PlaceBuildings() {
        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                if (grid[x, z] == StreetType.Empty && IsAdjacentToRoad(x, z)) {
                    Vector3 position = new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE);
                    position += new Vector3(Random.Range(-CELL_SIZE/4, CELL_SIZE/4), 0, Random.Range(-CELL_SIZE/4, CELL_SIZE/4));
                    
                    if (buildingPrefabs != null && buildingPrefabs.Length > 0) {
                        GameObject buildingPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
                        GameObject building = Instantiate(buildingPrefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0));
                        
                        float scale = Random.Range(0.8f, 1.2f);
                        building.transform.localScale = new Vector3(scale, scale * Random.Range(1f, 3f), scale);
                    }
                    else {
                        Debug.LogError("No building prefabs assigned in the Inspector!");
                    }
                }
            }
        }
    }

    private bool IsAdjacentToRoad(int x, int z) {
        return (x > 0 && grid[x - 1, z] != StreetType.Empty) ||
               (x < gridSize - 1 && grid[x + 1, z] != StreetType.Empty) ||
               (z > 0 && grid[x, z - 1] != StreetType.Empty) ||
               (z < gridSize - 1 && grid[x, z + 1] != StreetType.Empty);
    }

    private void CreateTexturedGround() {
        // ground mesh
        Mesh groundMesh = CreateGroundMesh();

        // gameObject for the ground
        if (groundObject == null) {
            groundObject = new GameObject("Textured Ground");
            groundObject.transform.SetParent(transform);
            groundObject.AddComponent<MeshFilter>();
            groundObject.AddComponent<MeshRenderer>();
        }

        groundObject.GetComponent<MeshFilter>().sharedMesh = groundMesh;

        // apply material
        Texture2D groundTexture = CreateGroundTexture();
        Material groundMaterial = new Material(Shader.Find("Standard"));
        groundMaterial.mainTexture = groundTexture;
        groundObject.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        // Make the ground in right place
        UpdateGroundPosition();
    }

    private void UpdateGroundPosition() {
        if (groundObject != null) {
            // Center ground 
            groundObject.transform.localPosition = groundOffset;
        }
    }

    private Mesh CreateGroundMesh() {
        Mesh mesh = new Mesh();

        float size = gridSize * CELL_SIZE;
        float halfSize = size / 2f;
        Vector3[] vertices = new Vector3[4] {
            new Vector3(-halfSize, 0, -halfSize),
            new Vector3(halfSize, 0, -halfSize),
            new Vector3(-halfSize, 0, halfSize),
            new Vector3(halfSize, 0, halfSize)
        };

        int[] triangles = new int[6] {
            0, 2, 1,
            2, 3, 1
        };
        Vector2[] uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Texture2D CreateGroundTexture() {
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] colors = new Color[textureWidth * textureHeight];

        for (int y = 0; y < textureHeight; y++) {
            for (int x = 0; x < textureWidth; x++) {
                float xCoord = (float)x / textureWidth * noiseScale;
                float yCoord = (float)y / textureHeight * noiseScale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                colors[y * textureWidth + x] = new Color(sample, 0f, 0f, 1f);  // Only red channel varies, green and blue are 0
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(StreetPatternGenerator))]
    public class StreetPatternGeneratorEditor : Editor {
        public override void OnInspectorGUI() {
            StreetPatternGenerator generator = (StreetPatternGenerator)target;

            // Inspector
            DrawDefaultInspector();

            // button to allow ground to show up - use to edit the look in the edit mode
            if (GUILayout.Button("Generate/Update Ground")) {
                generator.CreateTexturedGround();
            }
            // update ground position
            if (GUI.changed) {
                generator.UpdateGroundPosition();
            }
        }
    }
    #endif
}