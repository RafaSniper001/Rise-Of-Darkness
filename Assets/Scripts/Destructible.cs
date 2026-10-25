using UnityEngine;
using UnityEngine.AI;

// objeto de cenário que quebra: sabre, poder, tiros dos inimigos e explosões tiram vida.
// Treme a cada golpe; ao quebrar solta pedaços, pode soltar uma cápsula de cura
// e, se for explosivo (barris), causa dano e empurrão em volta (inimigos, player e outros objetos)
public class Destructible : MonoBehaviour
{
    public float health = 50f;

    [Header("Efeitos")]
    public AudioClip breakSound;
    public int debrisCount = 8;
    public float debrisLifetime = 2.5f;
    public float shakeTime = 0.15f;
    public float shakeAmount = 0.08f;

    [Header("Explosão")]
    public bool explosive = false;
    public float explosionRadius = 7f;
    public float explosionDamage = 40f;
    public float explosionPlayerDamage = 15f;
    public float explosionPush = 8f;
    public Material explosionMaterial;

    [Header("Recompensa")]
    [Range(0f, 1f)]
    public float healthDropChance = 0f;
    public float healthDropAmount = 20f;
    public Material healthDropMaterial;

    bool broken = false;
    float shake = 0f;
    Transform visual;
    Vector3 visualPosition;

    void Start()
    {
        // treme só o modelo (o colisor fica parado)
        if (transform.childCount > 0)
        {
            visual = transform.GetChild(0);
            visualPosition = visual.localPosition;
        }
    }

    void Update()
    {
        if (visual == null || shake <= 0f)
            return;

        shake -= Time.deltaTime;
        visual.localPosition = shake > 0f
            ? visualPosition + Random.insideUnitSphere * shakeAmount / transform.lossyScale.x
            : visualPosition;
    }

    public void TakeHit(float damage)
    {
        if (broken)
            return;

        health -= damage;
        shake = shakeTime;

        if (health <= 0f)
            Break();
    }

    void Break()
    {
        broken = true;

        if (breakSound != null)
            AudioSettings.PlayEffectAt(breakSound, transform.position);

        Bounds bounds = WorldBounds();
        SpawnDebris(bounds);

        if (healthDropChance > 0f && Random.value < healthDropChance)
            HealthPickup.Spawn(bounds.center, healthDropAmount, healthDropMaterial);

        // some antes da explosão para não se atingir de novo
        gameObject.SetActive(false);

        if (explosive)
            Explode(bounds.center);

        Destroy(gameObject);
    }

    void Explode(Vector3 center)
    {
        SpawnExplosionEffect(center);

        foreach (Collider col in Physics.OverlapSphere(center, explosionRadius))
        {
            float falloff = 1f - Mathf.Clamp01(Vector3.Distance(center, col.transform.position) / explosionRadius) * 0.5f;

            Destructible other = col.GetComponent<Destructible>();
            if (other != null)
            {
                other.TakeHit(explosionDamage * falloff);
                continue;
            }

            PlayerHealth player = col.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(explosionPlayerDamage * falloff);
                continue;
            }

            if (!col.CompareTag("Enemy"))
                continue;

            EnemtStatus status = col.GetComponent<EnemtStatus>();
            if (status != null)
                status.GetHit(explosionDamage * falloff);

            EnemyHealth enemyHealth = col.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
                enemyHealth.TakeDamage(explosionDamage * falloff);

            Chase chase = col.GetComponent<Chase>();
            if (chase != null)
                chase.PushAwayFrom(center, explosionPush, 0.25f);
        }
    }

    // pedaços do próprio objeto (cubos com o mesmo material) voando para fora
    void SpawnDebris(Bounds bounds)
    {
        Renderer source = GetComponentInChildren<Renderer>();
        Material material = source != null ? source.sharedMaterial : null;
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.22f;

        for (int i = 0; i < debrisCount; i++)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = "Pedaço";
            piece.transform.position = bounds.center + Vector3.Scale(Random.insideUnitSphere * 0.4f, bounds.size);
            piece.transform.rotation = Random.rotation;
            piece.transform.localScale = Vector3.one * size * Random.Range(0.5f, 1f);

            if (material != null)
                piece.GetComponent<Renderer>().sharedMaterial = material;

            var body = piece.AddComponent<Rigidbody>();
            body.mass = 0.2f;
            Vector3 push = (piece.transform.position - bounds.center).normalized + Vector3.up;
            body.AddForce(push * Random.Range(3f, 6f), ForceMode.VelocityChange);
            body.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);

            // não empurra o player nem os inimigos
            piece.layer = 2; // Ignore Raycast
            piece.AddComponent<Debris>().lifetime = debrisLifetime;
        }
    }

    void SpawnExplosionEffect(Vector3 center)
    {
        var go = new GameObject("Explosão");
        go.transform.position = center;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.35f, 0.05f));
        main.gravityModifier = 0.5f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        if (explosionMaterial != null)
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = explosionMaterial;
        ps.Play();

        // clarão
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.25f);
        light.range = explosionRadius * 2f;
        light.intensity = 30f;
        go.AddComponent<FadeLight>();
    }

    Bounds WorldBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }
}

// pedaço solto: encolhe e some
public class Debris : MonoBehaviour
{
    public float lifetime = 2.5f;
    float age = 0f;
    Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;
        float shrink = Mathf.InverseLerp(lifetime * 0.6f, lifetime, age);
        transform.localScale = startScale * (1f - shrink);

        if (age >= lifetime)
            Destroy(gameObject);
    }
}

// clarão da explosão: apaga rápido
public class FadeLight : MonoBehaviour
{
    public float duration = 0.35f;
    Light lightSource;
    float startIntensity;
    float age = 0f;

    void Start()
    {
        lightSource = GetComponent<Light>();
        startIntensity = lightSource.intensity;
    }

    void Update()
    {
        age += Time.deltaTime;
        lightSource.intensity = startIntensity * Mathf.Clamp01(1f - age / duration);
    }
}
