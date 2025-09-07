using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EvacShip : MonoBehaviour
{
    public Vector3 velocity = Vector3.up * 3f + Vector3.forward * 2f;
    public float lifeTime = 6f;
    public float speedVariance = 1.2f;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 rnd = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1f), Random.Range(-1f, 1f));
        Vector3 applied = (velocity + rnd) * Random.Range(0.6f, speedVariance);

        if (rb != null)
            rb.AddForce(applied, ForceMode.Impulse);
        else
            transform.Translate(applied * Time.deltaTime, Space.World);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (GetComponent<Rigidbody>() == null)
            transform.Translate((velocity + Vector3.up * 0.5f) * Time.deltaTime, Space.World);
    }
}
