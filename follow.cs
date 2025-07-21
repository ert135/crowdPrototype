using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class follow : MonoBehaviour
{
    public LineRenderer path;
    private GameObject[] boids;

    [Header("Debug")]
    public bool drawDebugLines = true;

    [Header("Movement Settings")]
    public float maxVelocity = 2f;
    public float maxDistanceFromPath = 3f;
    public float desireMag = 1.2f;
    public float seperationDesire = 1.5f;
    public float steerLimit = 0.5f;
    public float steerTargetMag = 4f;
    public float dirTargetMag = 2f;
    public float maxFinalPosition = 150f;

    // Movement vectors
    private Vector3 Vvelocity = Vector3.zero;
    private Vector3 Vacceleration = Vector3.zero;

    // Path following
    private Vector3 steerTarget = Vector3.zero;
    private Vector3 recordNormal = Vector3.zero;
    private float recordDistance = float.MaxValue;

    void Start()
    {
        boids = GameObject.FindGameObjectsWithTag("Boid");
    }

    void Update()
    {
        float distanceFromPath = (transform.position - recordNormal).magnitude;

        CalculatePathFollowVectors();

        if (drawDebugLines)
        {
            Debug.DrawLine(transform.position, recordNormal, Color.green);
        }

        if (distanceFromPath > maxDistanceFromPath)
        {
            Seek(steerTarget);
        }

        // Optional: apply separation from nearby boids
        Seperate(boids);

        Move();

        // Reset position if too far along X
        if (transform.position.x > maxFinalPosition)
        {
            transform.position = new Vector3(-20f, 0f, 0f);
            Vvelocity = Vector3.zero;
        }
    }

    private void Move()
    {
        Vector3 targetVelocity = Vvelocity + Vacceleration;
        targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxVelocity);

        Vvelocity = Vector3.Lerp(Vvelocity, targetVelocity, Time.deltaTime * 5f);

        transform.position += new Vector3(Vvelocity.x, 0f, Vvelocity.z);

        Vacceleration = Vector3.zero;
    }

    private void ApplyForce(Vector3 force)
    {
        Vacceleration += force;
    }

    private void Seek(Vector3 target)
    {
        Vector3 desired = (target - transform.position).normalized * desireMag;
        Vector3 steer = Vector3.ClampMagnitude(desired - Vvelocity, steerLimit);
        ApplyForce(steer);
    }

    private void CalculatePathFollowVectors()
    {
        Vector3 predictedLocation = transform.position + Vvelocity;
        recordDistance = float.MaxValue;

        if (drawDebugLines)
        {
            Debug.DrawLine(transform.position, predictedLocation, Color.red);
        }

        for (int i = 0; i < path.positionCount - 1; i++)
        {
            Vector3 start = path.GetPosition(i);
            Vector3 end = path.GetPosition(i + 1);
            Vector3 segment = end - start;
            Vector3 normal = GetNormalPoint(predictedLocation, start, end);

            // Clamp normal to segment
            float dot = Vector3.Dot(predictedLocation - start, segment.normalized);
            dot = Mathf.Clamp(dot, 0f, segment.magnitude);
            normal = start + segment.normalized * dot;

            float distance = Vector3.Distance(predictedLocation, normal);

            if (drawDebugLines)
            {
                Debug.DrawLine(transform.position, normal, Color.black);
            }

            if (distance < recordDistance)
            {
                recordDistance = distance;
                steerTarget = normal + (segment.normalized * steerTargetMag);
                recordNormal = normal;
            }
        }

        steerTarget += (steerTarget - transform.position).normalized * dirTargetMag;

        if (drawDebugLines)
        {
            Debug.DrawLine(transform.position, steerTarget, Color.blue);
        }
    }

    private Vector3 GetNormalPoint(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;
        float t = Vector3.Dot(ap, ab.normalized);
        return a + ab.normalized * t;
    }

    private void Seperate(GameObject[] others)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (GameObject other in others)
        {
            float dist = Vector3.Distance(other.transform.position, transform.position);
            if (dist > 0 && (dist < seperationDesire))
            {
                Vector3 diff = (transform.position - other.transform.position).normalized / dist;
                sum += diff;
                count++;
            }
        }

        if (count > 0)
        {
            Vector3 average = (sum / count).normalized;
            Vector3 steer = Vector3.ClampMagnitude(average - Vvelocity, steerLimit);
            ApplyForce(steer);
        }
    }
}
