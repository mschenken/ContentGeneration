using UnityEngine;
using System.Collections.Generic;

public class FlockingBehavior : MonoBehaviour {
    public int initialNumberOfCreatures = 20;
    public float worldBoxSize = 10f;
    public float maxVelocity = 5f;
    public float minVelocity = 1f;
    public float flockCenteringStrength = 1f;
    public float velocityMatchingStrength = 1f;
    public float collisionAvoidanceStrength = 1f;
    public float wanderingStrength = 1f;
    public bool showTrails = false;
    public int maxTrailLength = 10;

    public bool flockCenteringEnabled = true;
    public bool velocityMatchingEnabled = true;
    public bool collisionAvoidanceEnabled = true;
    public bool wanderingEnabled = true;

    private List<GameObject> creatures = new List<GameObject>();
    private List<Vector3> velocities = new List<Vector3>();
    private List<List<GameObject>> trails = new List<List<GameObject>>();

    private void Start() {
        CreateCreatures(initialNumberOfCreatures);
    }

    private void Update() {
        for (int i = 0; i < creatures.Count; i++)
        {
            GameObject creature = creatures[i];
            Vector3 velocity = velocities[i];

            List<GameObject> nearbyCreatures = FindNearbyCreatures(creature);

            if (flockCenteringEnabled)
                velocity += FlockCentering(creature, nearbyCreatures) * flockCenteringStrength;

            if (velocityMatchingEnabled)
                velocity += VelocityMatching(nearbyCreatures) * velocityMatchingStrength;

            if (collisionAvoidanceEnabled)
                velocity += CollisionAvoidance(creature, nearbyCreatures) * collisionAvoidanceStrength;

            if (wanderingEnabled)
                velocity += Wander() * wanderingStrength;

            velocity = Vector3.ClampMagnitude(velocity, maxVelocity);
            velocity = velocity.normalized * Mathf.Max(minVelocity, velocity.magnitude);

            creature.transform.position += velocity * Time.deltaTime;
            // creature.transform.rotation = Quaternion.LookRotation(velocity);
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            creature.transform.rotation = Quaternion.Slerp(creature.transform.rotation, targetRotation, Time.deltaTime * 5f);

            velocities[i] = velocity;

            HandleWorldBoxBoundaries(creature);

            if (showTrails)
                UpdateTrail(i, creature.transform.position);
        }

        if (Input.GetKeyDown(KeyCode.Space))
            ScatterCreatures();
    }

    private void CreateCreatures(int count) {
        for (int i = 0; i < count; i++) {
            GameObject creature = CreateCreature();
            creatures.Add(creature);
            velocities.Add(Random.insideUnitSphere * maxVelocity);
            trails.Add(new List<GameObject>());
        }
    }

    private GameObject CreateCreature() {
        GameObject creature = new GameObject("Creature");
        MeshFilter meshFilter = creature.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = creature.AddComponent<MeshRenderer>();

        Vector3[] vertices = new Vector3[18];
        vertices[0] = new Vector3(0f, 0f, 1f);
        vertices[1] = new Vector3(-0.5f, 0f, -1f);
        vertices[2] = new Vector3(0.5f, 0f, -1f);
        vertices[3] = new Vector3(0f, 0f, 1f);
        vertices[4] = new Vector3(0.5f, 0f, -1f);
        vertices[5] = new Vector3(0.5f, 0.2f, -0.5f);
        vertices[6] = new Vector3(0f, 0f, 1f);
        vertices[7] = new Vector3(0.5f, 0.2f, -0.5f);
        vertices[8] = new Vector3(0f, 0.2f, 0f);
        vertices[9] = new Vector3(0f, 0f, 1f);
        vertices[10] = new Vector3(0f, 0.2f, 0f);
        vertices[11] = new Vector3(-0.5f, 0.2f, -0.5f);
        vertices[12] = new Vector3(0f, 0f, 1f);
        vertices[13] = new Vector3(-0.5f, 0.2f, -0.5f);
        vertices[14] = new Vector3(-0.5f, 0f, -1f);
        vertices[15] = new Vector3(0.5f, 0.2f, -0.5f);
        vertices[16] = new Vector3(-0.5f, 0.2f, -0.5f);
        vertices[17] = new Vector3(0f, 0.2f, 0f);

        int[] triangles = new int[24];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 3;
        triangles[4] = 4;
        triangles[5] = 5;
        triangles[6] = 6;
        triangles[7] = 7;
        triangles[8] = 8;
        triangles[9] = 9;
        triangles[10] = 10;
        triangles[11] = 11;
        triangles[12] = 12;
        triangles[13] = 13;
        triangles[14] = 14;
        triangles[15] = 13;
        triangles[16] = 12;
        triangles[17] = 14;
        triangles[18] = 15;
        triangles[19] = 16;
        triangles[20] = 17;
        triangles[21] = 16;
        triangles[22] = 15;
        triangles[23] = 17;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        meshRenderer.material.color = new Color(0.6f, 0.4f, 0.2f);

        creature.transform.position = Random.insideUnitSphere * worldBoxSize;

        creature.AddComponent<WingFlap>();

        return creature;
    }

    private List<GameObject> FindNearbyCreatures(GameObject creature) {
        List<GameObject> nearbyCreatures = new List<GameObject>();
        foreach (GameObject otherCreature in creatures) {
            if (otherCreature != creature) {
                float distance = Vector3.Distance(creature.transform.position, otherCreature.transform.position);
                if (distance <= worldBoxSize * 0.2f)
                    nearbyCreatures.Add(otherCreature);
            }
        }
        return nearbyCreatures;
    }

    private Vector3 FlockCentering(GameObject creature, List<GameObject> nearbyCreatures) {
        if (nearbyCreatures.Count == 0)
            return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        foreach (GameObject otherCreature in nearbyCreatures)
            centerOfMass += otherCreature.transform.position;
        centerOfMass /= nearbyCreatures.Count;

        return (centerOfMass - creature.transform.position).normalized;
    }

    private Vector3 VelocityMatching(List<GameObject> nearbyCreatures) {
        if (nearbyCreatures.Count == 0)
            return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;
        foreach (GameObject otherCreature in nearbyCreatures)
            averageVelocity += velocities[creatures.IndexOf(otherCreature)];
        averageVelocity /= nearbyCreatures.Count;

        return averageVelocity.normalized;
    }

    private Vector3 CollisionAvoidance(GameObject creature, List<GameObject> nearbyCreatures) {
        if (nearbyCreatures.Count == 0)
            return Vector3.zero;

        Vector3 avoidanceVector = Vector3.zero;
        foreach (GameObject otherCreature in nearbyCreatures) {
            Vector3 offset = creature.transform.position - otherCreature.transform.position;
            float distance = offset.magnitude;
            avoidanceVector += offset / (distance * distance);
        }

        return avoidanceVector.normalized;
    }

    private Vector3 Wander() {
        return Random.insideUnitSphere;
    }

    private void HandleWorldBoxBoundaries(GameObject creature) {
        Vector3 position = creature.transform.position;
        Vector3 velocity = velocities[creatures.IndexOf(creature)];

        if (position.x < -worldBoxSize || position.x > worldBoxSize) {
            position.x = Mathf.Clamp(position.x, -worldBoxSize, worldBoxSize);
            velocity.x = -velocity.x;
        }

        if (position.y < -worldBoxSize || position.y > worldBoxSize) {
            position.y = Mathf.Clamp(position.y, -worldBoxSize, worldBoxSize);
            velocity.y = -velocity.y;
        }

        if (position.z < -worldBoxSize || position.z > worldBoxSize) {
            position.z = Mathf.Clamp(position.z, -worldBoxSize, worldBoxSize);
            velocity.z = -velocity.z;
        }

        creature.transform.position = position;
        velocities[creatures.IndexOf(creature)] = velocity;
    }

    private void UpdateTrail(int index, Vector3 position) {
        List<GameObject> trail = trails[index];

        // Create a particle system for the trail
        GameObject trailObject = new GameObject("Trail");
        trailObject.transform.position = position;
        trailObject.transform.rotation = Quaternion.LookRotation(-velocities[index]);

        ParticleSystem particleSystem = trailObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = 0.1f;

        // Create a new material for the trail particles
        Material trailMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        trailMaterial.color = Color.gray;

        // Set the trail material on the particle system renderer
        ParticleSystemRenderer renderer = trailObject.GetComponent<ParticleSystemRenderer>();
        renderer.material = trailMaterial;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 50f;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.1f;

        Destroy(trailObject, 1f);
        trail.Add(trailObject);
    }

    private void ScatterCreatures() {
        for (int i = 0; i < creatures.Count; i++) {
            GameObject creature = creatures[i];
            creature.transform.position = Random.insideUnitSphere * worldBoxSize;
            velocities[i] = Random.insideUnitSphere * maxVelocity;
        }
    }

    public void UpdateNumberOfCreatures(int count) {
        int currentCount = creatures.Count;
        if (count < currentCount) {
            for (int i = currentCount - 1; i >= count; i--) {
                Destroy(creatures[i]);
                creatures.RemoveAt(i);
                velocities.RemoveAt(i);
                ClearTrail(i);
                trails.RemoveAt(i);
            }
        }
        else if (count > currentCount) {
            CreateCreatures(count - currentCount);
        }
    }

    private void ClearTrail(int index) {
        List<GameObject> trail = trails[index];
        foreach (GameObject trailObject in trail)
            Destroy(trailObject);
        trail.Clear();
    }
}