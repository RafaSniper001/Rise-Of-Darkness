using UnityEngine;

// cápsula de cura solta por objetos quebrados: gira, flutua e cura o player ao encostar
public class HealthPickup : MonoBehaviour
{
    public float amount = 20f;
    public float lifetime = 20f;

    Vector3 basePosition;
    float age = 0f;

    public static void Spawn(Vector3 position, float amount, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Cura";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.45f, 0.35f, 0.45f);

        if (material != null)
            go.GetComponent<Renderer>().sharedMaterial = material;

        var trigger = go.GetComponent<Collider>();
        trigger.isTrigger = true;

        var light = new GameObject("Luz").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.type = LightType.Point;
        light.color = new Color(0.3f, 1f, 0.45f);
        light.range = 3f;
        light.intensity = 3f;

        go.AddComponent<HealthPickup>().amount = amount;
    }

    void Start()
    {
        // desce até perto do chão
        basePosition = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            basePosition = hit.point + Vector3.up * 0.8f;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position = basePosition + Vector3.up * Mathf.Sin(age * 3f) * 0.15f;
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);

        if (age >= lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.currentHealth >= player.maxHealth)
            return;

        player.Heal(amount);
        Destroy(gameObject);
    }
}
