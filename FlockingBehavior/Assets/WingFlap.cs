using UnityEngine;

public class WingFlap : MonoBehaviour {
    public float flapFrequency = 2f;
    public float flapAmplitude = 0.2f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] vertices;

    private void Start() {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        vertices = new Vector3[originalVertices.Length];
    }

    private void Update() {
        float flapAngle = Mathf.Sin(Time.time * flapFrequency) * flapAmplitude;

        for (int i = 0; i < originalVertices.Length; i++) {
            vertices[i] = originalVertices[i];

            if (i == 4 || i == 5 || i == 9 || i == 11 || i == 14 || i == 17 || i == 22) {
                vertices[i].y -= flapAngle;
            }
            else if (i == 6 || i == 7 || i == 15 || i == 19 || i == 23) {
                vertices[i].y += flapAngle;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}