using UnityEngine;

public class UFOGenerator : MonoBehaviour
{
    public int numUFOs = 5;
    public float spacing = 5f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public Material bodyMaterial;
    public Material cockpitMaterial;
    public Material lightsMaterial;
    public Material antennaMaterial;

    private Material CreateOpaqueMaterial(Color color) {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Mode", 0);
        material.SetOverrideTag("RenderType", "");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
        return material;
    }

    private void SetupOpaqueMaterials() {
        bodyMaterial = CreateOpaqueMaterial(bodyMaterial.color);
        cockpitMaterial = CreateOpaqueMaterial(cockpitMaterial.color);
        lightsMaterial = CreateOpaqueMaterial(lightsMaterial.color);
        antennaMaterial = CreateOpaqueMaterial(antennaMaterial.color);
    }

    private void Start() {
        SetupOpaqueMaterials();

        for (int i = 0; i < numUFOs; i++) {
            int ufoSeed = Random.Range(0, 1000);
            GenerateUFO(ufoSeed, i);
        }
    }

    private void GenerateUFO(int seed, int index) {
        Random.InitState(seed);

        GameObject ufo = new GameObject("UFO_" + index);

        // Generate main body using a curved surface
        GameObject body = CreateBody();
        body.transform.parent = ufo.transform;

        // Add swappable parts with variations
        GameObject cockpit = AddCockpit();
        cockpit.transform.parent = ufo.transform;

        GameObject lights = AddLights();
        lights.transform.parent = ufo.transform;

        GameObject antenna = AddAntenna();
        antenna.transform.parent = ufo.transform;

        // Add rivets along the top of the UFO
        AddRivets(ufo);

        // Randomize sizes of parts
        float bodyScale = Random.Range(minScale, maxScale);
        body.transform.localScale = Vector3.one * bodyScale;

        float cockpitScale = Random.Range(0.5f, 1f);
        cockpit.transform.localScale = Vector3.one * cockpitScale;

        float lightsScale = Random.Range(0.5f, 1f);
        lights.transform.localScale = Vector3.one * lightsScale;

        float antennaScale = Random.Range(0.5f, 1f);
        antenna.transform.localScale = Vector3.one * antennaScale;

        // Randomize colors with more realistic hues
        Color bodyColor = new Color(Random.Range(0.2f, 0.4f), Random.Range(0.2f, 0.4f), Random.Range(0.2f, 0.4f));
        body.GetComponent<Renderer>().material.color = bodyColor;

        Color cockpitColor = new Color(Random.Range(0.4f, 0.6f), Random.Range(0.4f, 0.6f), Random.Range(0.4f, 0.6f));
        cockpit.GetComponent<Renderer>().material.color = cockpitColor;

        Color lightsColor = new Color(Random.Range(0.8f, 1f), Random.Range(0.8f, 1f), Random.Range(0.8f, 1f));
        lights.GetComponent<Renderer>().material.color = lightsColor;

        Color antennaColor = new Color(Random.Range(0.2f, 0.4f), Random.Range(0.2f, 0.4f), Random.Range(0.2f, 0.4f));
        antenna.GetComponent<Renderer>().material.color = antennaColor;


        // Add variations based on UFO index
        switch (index) {
            case 0:
                AddLandingGear(ufo);
                break;
            case 1:
                AddSpikes(ufo);
                AddExhaustTrails(ufo);
                break;
            case 2:
                AddMultipleLayers(ufo);
                AddRotatingRing(ufo);
                break;
            case 3:
                AddLandingGear(ufo);
                AddSpikes(ufo);
                AddPulsingLights(ufo);
                break;
            case 4:
                AddMultipleLayers(ufo);
                AddSpikes(ufo);
                AddHolographicDisplay(ufo);
                break;
        }

        // Position UFO above the ground
        float yPos = ufo.transform.localScale.y / 2f + 1f;
        float xPos = (index * spacing) - 15;
        ufo.transform.position = new Vector3(xPos, yPos, 0f);


    }

    private GameObject CreateBody() {
        GameObject body = new GameObject("Body");
        MeshFilter meshFilter = body.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = body.AddComponent<MeshRenderer>();

        // Create a curved surface for the UFO body
        Vector3[] controlPoints = new Vector3[4];
        controlPoints[0] = new Vector3(0f, 0f, 0f);
        controlPoints[1] = new Vector3(1f, 0.5f, 0f);
        controlPoints[2] = new Vector3(2f, 0.5f, 0f);
        controlPoints[3] = new Vector3(3f, 0f, 0f);

        int segments = 32;
        Mesh mesh = GenerateBezierSurfaceOfRevolution(controlPoints, segments);

        // Add a bottom to the UFO body
        AddBottomToMesh(mesh, segments);

        meshFilter.mesh = mesh;

        // Assign the body material
        meshRenderer.material = bodyMaterial;

        return body;
    }

    private Mesh GenerateBezierSurfaceOfRevolution(Vector3[] controlPoints, int segments) {
        Vector3[] vertices = new Vector3[(segments + 1) * (segments + 1)];
        int[] triangles = new int[segments * segments * 6];

        for (int i = 0; i <= segments; i++) {
            float t = (float)i / segments;
            Vector3 point = CalculateBezierPoint(t, controlPoints);

            for (int j = 0; j <= segments; j++) {
                float angle = (float)j / segments * Mathf.PI * 2f;
                Vector3 rotatedPoint = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f) * point;
                vertices[i * (segments + 1) + j] = rotatedPoint;
            }
        }

        for (int i = 0; i < segments; i++) {
            for (int j = 0; j < segments; j++) {
                int index = (i * segments + j) * 6;
                triangles[index] = i * (segments + 1) + j;
                triangles[index + 1] = (i + 1) * (segments + 1) + j;
                triangles[index + 2] = i * (segments + 1) + j + 1;
                triangles[index + 3] = (i + 1) * (segments + 1) + j;
                triangles[index + 4] = (i + 1) * (segments + 1) + j + 1;
                triangles[index + 5] = i * (segments + 1) + j + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Vector3 CalculateBezierPoint(float t, Vector3[] controlPoints) {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 point = uuu * controlPoints[0] +
                        3f * uu * t * controlPoints[1] +
                        3f * u * tt * controlPoints[2] +
                        ttt * controlPoints[3];

        return point;
    }

    private void AddBottomToMesh(Mesh mesh, int segments) {
        int vertexCount = mesh.vertices.Length;
        int triangleCount = mesh.triangles.Length;

        Vector3[] vertices = new Vector3[vertexCount + mesh.vertices.Length / (vertexCount / (segments + 1))];
        int[] triangles = new int[triangleCount + (mesh.vertices.Length / (vertexCount / (segments + 1))) * 3];

        mesh.vertices.CopyTo(vertices, 0);
        mesh.triangles.CopyTo(triangles, 0);

        int bottomCenterIndex = vertexCount;
        int triangleIndex = triangleCount;

        for (int i = 0; i < mesh.vertices.Length / (vertexCount / (segments + 1)); i++) {
            vertices[bottomCenterIndex + i] = vertices[i * (vertexCount / (segments + 1))];
        }

        for (int i = 0; i < mesh.vertices.Length / (vertexCount / (segments + 1)) - 1; i++) {
            triangles[triangleIndex++] = bottomCenterIndex + i;
            triangles[triangleIndex++] = i * (vertexCount / (segments + 1));
            triangles[triangleIndex++] = (i + 1) * (vertexCount / (segments + 1));
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void AddRivets(GameObject ufo) {
        int numRivets = 16;
        float rivetSize = 0.05f;
        float rivetSpacing = 360f / numRivets;
        float rivetHeight = ufo.transform.localScale.y / 2f * .4f;

        for (int i = 0; i < numRivets; i++) {
            GameObject rivet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rivet.transform.parent = ufo.transform;
            rivet.transform.localScale = Vector3.one * rivetSize;

            float angle = i * rivetSpacing;
            Vector3 position = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (ufo.transform.localScale.x / 2f);
            position.y = rivetHeight;
            rivet.transform.localPosition = position;

            rivet.GetComponent<Renderer>().material.color = Color.gray;
        }
    }   

    private GameObject AddCockpit() {
        GameObject cockpit = new GameObject("Cockpit");
        MeshFilter meshFilter = cockpit.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = cockpit.AddComponent<MeshRenderer>();

        // Generate a procedural mesh for the cockpit
        Mesh mesh = GenerateCockpitMesh();
        meshFilter.mesh = mesh;

        // Assign the cockpit material
        meshRenderer.material = cockpitMaterial;

        return cockpit;
    }

    private Mesh GenerateCockpitMesh() {
        // Generate a procedural mesh for the cockpit
        // Example: Create a half-sphere mesh
        float radius = 0.8f;
        int segments = 16;
        int rings = 8;

        Vector3[] vertices = new Vector3[(segments + 1) * (rings + 1)];
        int[] triangles = new int[segments * rings * 6];

        int index = 0;

        for (int i = 0; i <= rings; i++) {
            float v = (float)i / rings;
            float phi = v * Mathf.PI * 0.5f; // Only generate the top half of the sphere

            for (int j = 0; j <= segments; j++) {
                float u = (float)j / segments;
                float theta = u * 2f * Mathf.PI;

                float x = radius * Mathf.Cos(theta) * Mathf.Sin(phi);
                float y = radius * Mathf.Cos(phi);
                float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);

                vertices[index++] = new Vector3(x, y, z);
            }
        }

        index = 0;
        for (int i = 0; i < rings; i++) {
            for (int j = 0; j < segments; j++) {
                int a = i * (segments + 1) + j;
                int b = (i + 1) * (segments + 1) + j;
                int c = (i + 1) * (segments + 1) + j + 1;
                int d = i * (segments + 1) + j + 1;

                triangles[index++] = a;
                triangles[index++] = b;
                triangles[index++] = d;
                triangles[index++] = b;
                triangles[index++] = c;
                triangles[index++] = d;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private GameObject AddLights() {
        GameObject lights = new GameObject("Lights");
        MeshFilter meshFilter = lights.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = lights.AddComponent<MeshRenderer>();

        // Generate a procedural mesh for the lights
        Mesh mesh = GenerateLightsMesh();
        meshFilter.mesh = mesh;

        // Assign the lights material
        meshRenderer.material = lightsMaterial;

        return lights;
    }

    private Mesh GenerateLightsMesh() {
        // Generate a procedural mesh for the lights
        // Example: Create a ring of spheres
        float radius = 1.5f;
        int numLights = 8;
        float lightSize = 0.1f;

        Vector3[] vertices = new Vector3[numLights * 24];
        int[] triangles = new int[numLights * 36];

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < numLights; i++) {
            float angle = (float)i / numLights * 2f * Mathf.PI;
            Vector3 position = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            // Generate a small sphere for each light
            for (int j = 0; j < 24; j++) {
                vertices[vertexIndex++] = position + Random.insideUnitSphere * lightSize;
            }

            for (int j = 0; j < 36; j++) {
                triangles[triangleIndex++] = vertexIndex - 24 + MeshData.IcosphereTriangles[j];
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private GameObject AddAntenna() {
        GameObject antenna = new GameObject("Antenna");
        MeshFilter meshFilter = antenna.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = antenna.AddComponent<MeshRenderer>();

        // Generate a procedural mesh for the antenna
        Mesh mesh = GenerateAntennaMesh();
        meshFilter.mesh = mesh;

        // Assign the antenna material
        meshRenderer.material = antennaMaterial;

        return antenna;
    }

    private Mesh GenerateAntennaMesh() {
        // Generate a procedural mesh for the antenna
        // Example: Create a cone mesh
        float height = 1f;
        float radius = 0.1f;
        int segments = 8;

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero; // Cone tip
        vertices[segments + 1] = new Vector3(0f, height, 0f); // Cone base center

        for (int i = 0; i < segments; i++) {
            float angle = (float)i / segments * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[i + 1] = new Vector3(x, height, z);

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
    private void AddLandingGear(GameObject ufo) {
        int numLegs = 3;
        float legLength = 1f;
        float legWidth = 0.1f;
        float legSpacing = 120f;

        for (int i = 0; i < numLegs; i++) {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.transform.parent = ufo.transform;
            leg.transform.localScale = new Vector3(legWidth, legLength, legWidth);

            float angle = i * legSpacing;
            Vector3 position = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (ufo.transform.localScale.x / 2f);
            position.y = -legLength / 2f;
            leg.transform.localPosition = position;

            leg.GetComponent<Renderer>().material.color = Color.gray;
        }
    }

    private void AddSpikes(GameObject ufo) {
        int numSpikes = 8;
        float spikeLength = 0.5f;
        float spikeWidth = 0.1f;

        for (int i = 0; i < numSpikes; i++) {
            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spike.transform.parent = ufo.transform;
            spike.transform.localScale = new Vector3(spikeWidth, spikeLength, spikeWidth);

            float angle = i * (360f / numSpikes);
            Vector3 position = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (ufo.transform.localScale.x / 2f);
            position.y = ufo.transform.localScale.y / 2f;
            spike.transform.localPosition = position;
            spike.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            spike.GetComponent<Renderer>().material.color = bodyMaterial.color * 0.8f;
        }
    }

    private void AddMultipleLayers(GameObject ufo) {
        int numLayers = 3;
        float layerSpacing = 0.2f;

        for (int i = 1; i < numLayers; i++) {
            GameObject layer = CreateBody();
            layer.transform.parent = ufo.transform;
            layer.transform.localPosition = Vector3.up * (layerSpacing * i);
            layer.transform.localScale = ufo.transform.localScale * (1f - (i * 0.1f));

            Color layerColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
            layer.GetComponent<Renderer>().material.color = layerColor;
        }
    }

    private void AddExhaustTrails(GameObject ufo) {
        int numTrails = 2;
        float trailLength = 1f;
        float trailWidth = 0.1f;

        for (int i = 0; i < numTrails; i++) {
            GameObject trail = GameObject.CreatePrimitive(PrimitiveType.Quad);
            trail.transform.parent = ufo.transform;
            trail.transform.localScale = new Vector3(trailWidth, trailLength, 1f);

            float angle = i * 180f;
            Vector3 position = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (ufo.transform.localScale.x / 2f);
            position.y = -ufo.transform.localScale.y / 2f;
            trail.transform.localPosition = position;

            Material trailMaterial = new Material(Shader.Find("Unlit/Color"));
            trailMaterial.color = new Color(0f, 0.8f, 1f, 0.5f);
            trailMaterial.SetFloat("_Mode", 2);
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            trailMaterial.EnableKeyword("_ALPHABLEND_ON");
            trailMaterial.renderQueue = 3000;
            trail.GetComponent<Renderer>().material = trailMaterial;
        }
    }

    private void AddRotatingRing(GameObject ufo) {
        GameObject ring = new GameObject("Ring");
        ring.transform.parent = ufo.transform;
        ring.transform.localPosition = Vector3.up * (ufo.transform.localScale.y / 4f);

        float ringRadius = ufo.transform.localScale.x / 1.5f;
        float ringThickness = 0.1f;
        int ringSegments = 32;

        Vector3[] vertices = new Vector3[ringSegments * 2];
        int[] triangles = new int[ringSegments * 6];
        Vector2[] uv = new Vector2[ringSegments * 2];

        for (int i = 0; i < ringSegments; i++) {
            float angle = i * Mathf.PI * 2f / ringSegments;
            Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));

            vertices[i * 2] = dir * ringRadius;
            vertices[i * 2 + 1] = dir * (ringRadius + ringThickness);

            uv[i * 2] = new Vector2((float)i / ringSegments, 0f);
            uv[i * 2 + 1] = new Vector2((float)i / ringSegments, 1f);

            int nextIndex = (i + 1) % ringSegments;
            triangles[i * 6] = i * 2;
            triangles[i * 6 + 1] = i * 2 + 1;
            triangles[i * 6 + 2] = nextIndex * 2;
            triangles[i * 6 + 3] = i * 2 + 1;
            triangles[i * 6 + 4] = nextIndex * 2 + 1;
            triangles[i * 6 + 5] = nextIndex * 2;
        }

        Mesh ringMesh = new Mesh();
        ringMesh.vertices = vertices;
        ringMesh.triangles = triangles;
        ringMesh.uv = uv;
        ringMesh.RecalculateNormals();

        MeshFilter meshFilter = ring.AddComponent<MeshFilter>();
        meshFilter.mesh = ringMesh;

        MeshRenderer meshRenderer = ring.AddComponent<MeshRenderer>();
        Material ringMaterial = new Material(Shader.Find("Unlit/Color"));
        ringMaterial.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
        meshRenderer.material = ringMaterial;

        ring.AddComponent<RotateObject>();
    }

    private void AddPulsingLights(GameObject ufo) {
        int numLights = 4;
        float lightRadius = 0.2f;

        for (int i = 0; i < numLights; i++) {
            GameObject light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            light.transform.parent = ufo.transform;
            light.transform.localScale = Vector3.one * lightRadius;

            float angle = i * (360f / numLights);
            Vector3 position = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * ufo.transform.localScale.x / 2f + Vector3.up * ufo.transform.localScale.y / 4f);
            light.transform.localPosition = position;

            Material lightMaterial = new Material(Shader.Find("Unlit/Color"));
            lightMaterial.EnableKeyword("_EMISSION");
            lightMaterial.SetColor("_EmissionColor", new Color(Random.Range(0.8f, 1f), Random.Range(0.8f, 1f), Random.Range(0.8f, 1f)));
            light.GetComponent<Renderer>().material = lightMaterial;

            light.AddComponent<PulseLight>();
        }
    }

    private void AddHolographicDisplay(GameObject ufo) {
        GameObject display = GameObject.CreatePrimitive(PrimitiveType.Quad);
        display.transform.parent = ufo.transform;
        display.transform.localScale = new Vector3(2f, 1f, 1f);
        display.transform.localPosition = Vector3.up * ufo.transform.localScale.y / 2f;
        display.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Material displayMaterial = new Material(Shader.Find("Unlit/Transparent"));
        displayMaterial.mainTexture = GenerateHolographicTexture();
        displayMaterial.SetFloat("_Mode", 2);
        displayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        displayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        displayMaterial.EnableKeyword("_ALPHABLEND_ON");
        displayMaterial.renderQueue = 3000;
        display.GetComponent<Renderer>().material = displayMaterial;
    }

    private Texture2D GenerateHolographicTexture() {
        int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize);

        for (int y = 0; y < textureSize; y++) {
            for (int x = 0; x < textureSize; x++) {
                float value = Mathf.PerlinNoise((float)x / textureSize * 5f, (float)y / textureSize * 5f);
                Color color = new Color(0f, 1f, 0f, value);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}

public class RotateObject : MonoBehaviour {
    public float rotationSpeed = 30f;

    private void Update() {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}

public class PulseLight : MonoBehaviour {
    public float minIntensity = 0.5f;
    public float maxIntensity = 1f;
    public float pulseSpeed = 1f;

    private Material lightMaterial;
    private float currentIntensity;

    private void Start() {
        lightMaterial = GetComponent<Renderer>().material;
        currentIntensity = minIntensity;
    }

    private void Update() {
        currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.PingPong(Time.time * pulseSpeed, 1f));
        lightMaterial.SetColor("_EmissionColor", lightMaterial.GetColor("_EmissionColor") * currentIntensity);
    }
}




public static class MeshData {
    public static int[] IcosphereTriangles = {
        0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 1,
        1, 6, 2, 2, 7, 3, 3, 8, 4, 4, 9, 5, 5, 10, 1,
        1, 10, 6, 2, 6, 7, 3, 7, 8, 4, 8, 9, 5, 9, 10,
        6, 10, 11, 7, 6, 11, 8, 7, 11, 9, 8, 11, 10, 9, 11
    };
}