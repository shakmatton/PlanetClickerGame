// ==================== PlanetRotation.cs ====================
// Arquivo: PlanetRotation.cs
// Anexe este script ao GameObject "Planet"

using UnityEngine;

public class PlanetRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Vector3 rotationSpeed = new Vector3(0, 15, 0);
    public bool randomizeRotation = true;

    private bool isRotating = false;

    void Start()
    {
        if (randomizeRotation)
        {
            rotationSpeed += new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-10f, 10f),
                Random.Range(-5f, 5f)
            );
        }
    }

    public void StartRotation()
    {
        isRotating = true;
    }

    public void StopRotation()
    {
        isRotating = false;
    }

    void Update()
    {
        if (isRotating)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}
