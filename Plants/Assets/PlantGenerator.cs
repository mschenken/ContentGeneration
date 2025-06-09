using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantGeneratorScript : MonoBehaviour {
    public int randomSeed = 12345;
    public int numPlantsToGenerate = 3;
    public float spacing = 5f;
    public int numSegments = 16;

    private void Start() {
        Debug.Log("start called");
        GeneratePlants();
    }

    [ContextMenu("Generate Plants")]
    public void GeneratePlants() {
        Debug.Log("GeneratePlants call");

        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        Random.InitState(randomSeed);
        for (int i = 0; i < numPlantsToGenerate; i++) {
            float angle = i * (360f / numPlantsToGenerate);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 position = direction * spacing;
            GeneratePlant(position);
        }
        Debug.Log($"created {numPlantsToGenerate} plants");
    }

    private void GeneratePlant(Vector3 position) {
        bool isTree = Random.value > 0.5f;

        if (isTree) {
            GenerateTree(position);
        }
        else {
            GenerateBush(position);
        }
        Debug.Log($"Created {(isTree ? "tree" : "bush")} at {position}");
    }

    private void GenerateTree(Vector3 position) {
        GameObject tree = new GameObject("tree");
        tree.transform.SetParent(transform);
        tree.transform.position = position;

        // trunk
        GameObject trunk = GenerateBranch(tree.transform, 0, true);
        float trunkHeight = trunk.GetComponent<MeshFilter>().mesh.bounds.size.y;
        float trunkRadius = trunk.GetComponent<MeshFilter>().mesh.bounds.extents.x;

        // main branches
        int numMainBranches = Random.Range(4, 8);
        float branchStartHeight = trunkHeight * 0.6f;
        float branchEndHeight = trunkHeight * 0.9f;

        for (int i = 0; i < numMainBranches; i++) {
            float t = (float)i / (numMainBranches - 1);
            float height = Mathf.Lerp(branchStartHeight, branchEndHeight, t);
            float angle = i * (360f / numMainBranches);
            GenerateTreeBranch(trunk.transform, 1, height, trunkRadius, angle);
        }
        Debug.Log($"Tree with {numMainBranches} main branches");
    }

    private GameObject GenerateTreeBranch(Transform parent, int order, float heightOffset, float parentRadius, float angle) {
        GameObject branch = new GameObject($"Branch_Order{order}");
        branch.transform.SetParent(parent);
        branch.layer = LayerMask.NameToLayer("branch");

        // branch mesh
        MeshFilter meshFilter = branch.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = branch.AddComponent<MeshRenderer>();

        // curved branch mesh
        Mesh branchMesh = GenerateCurvedBranchMesh(order, parentRadius);
        meshFilter.mesh = branchMesh;

        // material
        meshRenderer.material = CreateMaterial(order);

        // position and scale
        float scale = Mathf.Pow(0.7f, order);
        branch.transform.localScale = new Vector3(scale, scale, scale);

        // smooth connection
        float adjustedRadius = parentRadius * scale;
        Vector3 branchPosition = Quaternion.Euler(0, angle, 0) * Vector3.forward * adjustedRadius;
        branchPosition.y = heightOffset;

        // collision chec
        int layerMask = LayerMask.GetMask("branch");
        if (CheckBranchCollision(parent.TransformPoint(branchPosition), adjustedRadius, layerMask)) {
            Debug.Log($"Collision found. skip branch generation at this position: {branchPosition}");
            return null;
        }

        branch.transform.localPosition = branchPosition;

        // rotate bc of order
        float branchAngle = order == 1 ? Random.Range(30f, 60f) : Random.Range(15f, 45f);
        Quaternion targetRotation = Quaternion.LookRotation(parent.up, branch.transform.localPosition) * Quaternion.Euler(branchAngle, 0, 0);
        branch.transform.localRotation = targetRotation;

        if (order < 4) {
            int numChildBranches = Random.Range(2, 4);
            float branchLength = branchMesh.bounds.size.y;
            float childParentRadius = branchMesh.bounds.extents.x;

            for (int i = 0; i < numChildBranches; i++) {
                float t = (float)(i + 1) / (numChildBranches + 1);
                float childHeightOffset = branchLength * t;
                float childAngle = i * (360f / numChildBranches);
                GenerateTreeBranch(branch.transform, order + 1, childHeightOffset, childParentRadius, childAngle);
            }
        }
        Debug.Log($"tree branch generated; Order {order}, Height Offset: {heightOffset}, Angle: {angle}");
        return branch;
    }
    private void GenerateBush(Vector3 position) {
        GameObject bush = new GameObject("bush");
        bush.transform.SetParent(transform);
        bush.transform.position = position;

        // create main branch
        int numMainBranches = Random.Range(5, 10);
        for (int i = 0; i < numMainBranches; i++) {
            float angle = i * (360f / numMainBranches);
            GameObject branch = GenerateSmoothBranch(bush.transform, 0, 0f, 0.5f, angle);
            AddBushTexture(branch);
        }
        Debug.Log($"Generated with {numMainBranches} main branches");
    }
    private void AddBushTexture(GameObject branch) {
        MeshFilter meshFilter = branch.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;

        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++) {
            Vector3 vertex = vertices[i];
            float u = Mathf.Atan2(vertex.x, vertex.z) / (2f * Mathf.PI);
            float v = vertex.y / mesh.bounds.size.y;
            uvs[i] = new Vector2(u, v);
        }

        mesh.uv = uvs;

        Material material = branch.GetComponent<MeshRenderer>().material;
        material.mainTexture = GenerateBushTexture();
    }
    private Texture2D GenerateBushTexture() {
        int width = 256;
        int height = 256;
        Texture2D texture = new Texture2D(width, height);

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float xNorm = (float)x / width;
                float yNorm = (float)y / height;
                float noise = Mathf.PerlinNoise(xNorm * 10f, yNorm * 10f);
                Color color = new Color(noise, noise, noise);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private GameObject GenerateSmoothBranch(Transform parent, int order, float heightOffset, float parentRadius, float angle) {
        GameObject branch = new GameObject($"Branch_Order{order}");
        branch.transform.SetParent(parent);
        branch.layer = LayerMask.NameToLayer("branch");

        // branch mesh
        MeshFilter meshFilter = branch.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = branch.AddComponent<MeshRenderer>();

        // curved branch mesh
        Mesh branchMesh = GenerateCurvedBranchMesh(order, parentRadius);
        meshFilter.mesh = branchMesh;

        // material
        meshRenderer.material = CreateMaterial(order);

        // position
        float scale = Mathf.Pow(0.7f, order);
        branch.transform.localScale = new Vector3(scale, scale, scale);

        // smooth connection
        float adjustedRadius = parentRadius * scale;
        Vector3 branchPosition = Quaternion.Euler(0, angle, 0) * Vector3.forward * adjustedRadius;
        branchPosition.y = heightOffset;

        // collision check 
        int layerMask = LayerMask.GetMask("Branch");
        if (CheckBranchCollision(parent.TransformPoint(branchPosition), adjustedRadius, layerMask)) {
            Debug.Log($"Collision. spki branch generation at: {branchPosition}");
            return null;
        }
        branch.transform.localPosition = branchPosition;

        // orthotropic or plagiotropic growth
        bool isOrthotropic = Random.value < 0.5f;
        Vector3 growthDirection = isOrthotropic ? parent.up : (branch.transform.localPosition - parent.localPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(growthDirection, branch.transform.localPosition) * Quaternion.Euler(Random.Range(30f, 60f), 0, 0);
        branch.transform.localRotation = Quaternion.Slerp(parent.localRotation, targetRotation, 0.5f);

        if (order < 3) {
            int numChildBranches = Random.Range(2, 5);
            float branchLength = branchMesh.bounds.size.y;
            float childParentRadius = branchMesh.bounds.extents.x;

            for (int i = 0; i < numChildBranches; i++) {
                float t = (float)(i + 1) / (numChildBranches + 1);
                float childHeightOffset = branchLength * t;
                float childAngle = i * (360f / numChildBranches);
                GenerateSmoothBranch(branch.transform, order + 1, childHeightOffset, childParentRadius, childAngle);
            }
        }
        Debug.Log($"branch generated; Order {order}, Height Offset: {heightOffset}, Angle: {angle}");
        return branch;
    }

    private Mesh GenerateCurvedBranchMesh(int order, float parentRadius) {
        float baseRadius = parentRadius * 0.6f;
        float topRadius = baseRadius * 0.4f;
        float height = 3f * Mathf.Pow(0.7f, order);
        int segments = 16;
        int curve = 3;

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[(segments + 1) * (curve + 1) + 2];
        int[] triangles = new int[segments * curve * 6 + segments * 6];

        for (int i = 0; i <= segments; i++) {
            float angle = 2 * Mathf.PI * i / segments;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            for (int j = 0; j <= curve; j++) {
                float t = (float)j / curve;
                float radius = Mathf.Lerp(baseRadius, topRadius, t);
                float curveAngle = Mathf.PI * t;
                float xCurve = x * Mathf.Cos(curveAngle);
                float yCurve = y;
                float zCurve = x * Mathf.Sin(curveAngle);

                vertices[i + j * (segments + 1)] = new Vector3(xCurve * radius, t * height, zCurve * radius);

                if (i < segments && j < curve) {
                    int baseIndex = (i + j * segments) * 6;
                    int i0 = i + j * (segments + 1);
                    int i1 = (i + 1) % (segments + 1) + j * (segments + 1);
                    int i2 = i + (j + 1) * (segments + 1);
                    int i3 = (i + 1) % (segments + 1) + (j + 1) * (segments + 1);

                    triangles[baseIndex] = i0;
                    triangles[baseIndex + 1] = i1;
                    triangles[baseIndex + 2] = i2;
                    triangles[baseIndex + 3] = i1;
                    triangles[baseIndex + 4] = i3;
                    triangles[baseIndex + 5] = i2;
                }
            }
        }

        // top/bottom vertices
        vertices[vertices.Length - 2] = Vector3.zero;
        vertices[vertices.Length - 1] = new Vector3(0, height, 0);

        // top/bottom triangles
        for (int i = 0; i < segments; i++) {
            int baseIndex = segments * curve * 6 + i * 3;
            int i0 = i;
            int i1 = (i + 1) % segments;
            int i2 = vertices.Length - 2;
            int i3 = i + curve * (segments + 1);
            int i4 = (i + 1) % segments + curve * (segments + 1);
            int i5 = vertices.Length - 1;

            triangles[baseIndex] = i0;
            triangles[baseIndex + 1] = i2;
            triangles[baseIndex + 2] = i1;
            triangles[baseIndex + segments * 3] = i3;
            triangles[baseIndex + segments * 3 + 1] = i4;
            triangles[baseIndex + segments * 3 + 2] = i5;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private GameObject GenerateBranch(Transform parent, int order, bool isTrunk = false) {
        GameObject branch = new GameObject($"Branch_Order{order}");
        branch.transform.SetParent(parent);  
        branch.layer = LayerMask.NameToLayer("Branch");

        // Create beanch mesh
        MeshFilter meshFilter = branch.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = branch.AddComponent<MeshRenderer>(); 

        // Generate branch mesh
        Mesh branchMesh = GenerateBranchMesh(order, isTrunk);  
        meshFilter.mesh = branchMesh;

        // material
        meshRenderer.material = CreateMaterial(order);

        // position
        float scale = isTrunk ? 1f : Mathf.Pow(0.7f, order);  
        branch.transform.localScale = new Vector3(scale, scale, scale);
        branch.transform.localPosition = isTrunk ? Vector3.zero : Vector3.up * branchMesh.bounds.size.y / 2f;

        if (!isTrunk) {
            branch.transform.localRotation = Quaternion.Euler(
                Random.Range(-30f, 30f),
                Random.Range(0f, 360f),
                Random.Range(-30f, 30f)
            );
        }

        // collision check 
        int layerMask = LayerMask.GetMask("Branch");
        if (!isTrunk && CheckBranchCollision(branch.transform.position, branchMesh.bounds.extents.x, layerMask)) {
            Debug.Log($"Collision. Skip branch generation at: {branch.transform.position}");
            Destroy(branch);
            return null;
        }

        if (order < 2) {
            int numChildBranches = Random.Range(2, 5);
            for (int i = 0; i < numChildBranches; i++) {
                GenerateBranch(branch.transform, order + 1);
            }
        }
        Debug.Log($"branch generated; Order {order}, Is Trunk: {isTrunk}");
        return branch; 
    }
    private Mesh CreateTrunkMeshWithBark(float radius, float height, int segments) {
        Mesh mesh = new Mesh();

        int barkSegments = segments * 2;
        float barkDepth = 0.1f;

        Vector3[] vertices = new Vector3[(barkSegments + 1) * 2 + 2];
        int[] triangles = new int[barkSegments * 6 + barkSegments * 6];

        for (int i = 0; i <= barkSegments; i++) {
            float angle = 2 * Mathf.PI * i / barkSegments;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            float barkOffset = (i % 2 == 0) ? barkDepth : 0f;
            float barkX = (radius + barkOffset) * Mathf.Cos(angle);
            float barkZ = (radius + barkOffset) * Mathf.Sin(angle);

            vertices[i] = new Vector3(barkX, 0, barkZ);
            vertices[i + barkSegments + 1] = new Vector3(barkX, height, barkZ);

            if (i < barkSegments) {
                int baseIndex = i * 6;
                triangles[baseIndex] = i;
                triangles[baseIndex + 1] = (i + 1) % barkSegments;
                triangles[baseIndex + 2] = i + barkSegments + 1;

                triangles[baseIndex + 3] = (i + 1) % barkSegments;
                triangles[baseIndex + 4] = ((i + 1) % barkSegments) + barkSegments + 1;
                triangles[baseIndex + 5] = i + barkSegments + 1;
            }
        }

        // top/bottom vertices
        vertices[vertices.Length - 2] = Vector3.zero;
        vertices[vertices.Length - 1] = new Vector3(0, height, 0);

        // top/bottom trinagles
        for (int i = 0; i < barkSegments; i++) {
            int baseIndex = barkSegments * 6 + i * 3;
            triangles[baseIndex] = i;
            triangles[baseIndex + 1] = vertices.Length - 2;
            triangles[baseIndex + 2] = (i + 1) % barkSegments;

            triangles[baseIndex + barkSegments * 3] = i + barkSegments + 1;
            triangles[baseIndex + barkSegments * 3 + 1] = (i + 1) % barkSegments + barkSegments + 1;
            triangles[baseIndex + barkSegments * 3 + 2] = vertices.Length - 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
    private Material CreateMaterial(int order) {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = GetRandomBranchColor(order);
        return mat;
    }

    private Color GetRandomBranchColor(int order) {
        float hue = Random.Range(0f, 0.3f);
        float saturation = 0.5f + (order * 0.1f);
        float value = 0.5f + (order * 0.1f);
        return Color.HSVToRGB(hue, saturation, value);
    }

    private Mesh GenerateBranchMesh(int order, bool isTrunk) {
        float radius = isTrunk ? 0.5f : 0.2f * Mathf.Pow(0.6f, order);
        float height = isTrunk ? 6f : 3f * Mathf.Pow(0.7f, order);

        if (isTrunk) {
            return CreateTrunkMeshWithBark(radius, height, numSegments);
        }
        else {
            return CreateCylinderMesh(radius, height, numSegments);
        }
    }

    private Mesh GenerateSmoothBranchMesh(int order, float parentRadius) {
        float baseRadius = parentRadius * 0.6f;
        float topRadius = baseRadius * 0.4f;
        float height = 3f * Mathf.Pow(0.7f, order);
        return CreateSmoothBranchMesh(baseRadius, topRadius, height, numSegments);
    }

    private Mesh CreateCylinderMesh(float radius, float height, int segments) {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[(segments + 1) * 2 + 2];
        int[] triangles = new int[segments * 6 + segments * 6];

        for (int i = 0; i <= segments; i++) {
            float angle = 2 * Mathf.PI * i / segments;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            vertices[i] = new Vector3(x, 0, z);
            vertices[i + segments + 1] = new Vector3(x, height, z);

            if (i < segments) {
                int baseIndex = i * 6;
                triangles[baseIndex] = i;
                triangles[baseIndex + 1] = (i + 1) % segments;
                triangles[baseIndex + 2] = i + segments + 1;

                triangles[baseIndex + 3] = (i + 1) % segments;
                triangles[baseIndex + 4] = ((i + 1) % segments) + segments + 1;
                triangles[baseIndex + 5] = i + segments + 1;
            }
        }

        // top/bottom vertices
        vertices[vertices.Length - 2] = Vector3.zero;
        vertices[vertices.Length - 1] = new Vector3(0, height, 0);

        // top/bottom triangles
        for (int i = 0; i < segments; i++) {
            int baseIndex = segments * 6 + i * 3;
            triangles[baseIndex] = i;
            triangles[baseIndex + 1] = vertices.Length - 2;
            triangles[baseIndex + 2] = (i + 1) % segments;

            triangles[baseIndex + segments * 3] = i + segments + 1;
            triangles[baseIndex + segments * 3 + 1] = (i + 1) % segments + segments + 1;
            triangles[baseIndex + segments * 3 + 2] = vertices.Length - 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh CreateSmoothBranchMesh(float baseRadius, float topRadius, float height, int segments) {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[(segments + 1) * 2 + 2];
        int[] triangles = new int[segments * 6 + segments * 6];

        for (int i = 0; i <= segments; i++) {
            float angle = 2 * Mathf.PI * i / segments;
            float xBase = baseRadius * Mathf.Cos(angle);
            float zBase = baseRadius * Mathf.Sin(angle);
            float xTop = topRadius * Mathf.Cos(angle);
            float zTop = topRadius * Mathf.Sin(angle);

            vertices[i] = new Vector3(xBase, 0, zBase);
            vertices[i + segments + 1] = new Vector3(xTop, height, zTop);

            if (i < segments) {
                int baseIndex = i * 6;
                triangles[baseIndex] = i;
                triangles[baseIndex + 1] = (i + 1) % segments;
                triangles[baseIndex + 2] = i + segments + 1;

                triangles[baseIndex + 3] = (i + 1) % segments;
                triangles[baseIndex + 4] = ((i + 1) % segments) + segments + 1;
                triangles[baseIndex + 5] = i + segments + 1;
            }
        }

        // top/bottom vertices
        vertices[vertices.Length - 2] = Vector3.zero;
        vertices[vertices.Length - 1] = new Vector3(0, height, 0);

        // top/bottom triangles
        for (int i = 0; i < segments; i++) {
            int baseIndex = segments * 6 + i * 3;
            triangles[baseIndex] = i;
            triangles[baseIndex + 1] = vertices.Length - 2;
            triangles[baseIndex + 2] = (i + 1) % segments;

            triangles[baseIndex + segments * 3] = i + segments + 1;
            triangles[baseIndex + segments * 3 + 1] = (i + 1) % segments + segments + 1;
            triangles[baseIndex + segments * 3 + 2] = vertices.Length - 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
    private bool CheckBranchCollision(Vector3 position, float radius, int layerMask) {
        Collider[] colliders = Physics.OverlapSphere(position, radius, layerMask);
        return colliders.Length > 0;
    }
}