using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// efeitos de energia (Tools/VFX/Melhorar Auras):
// - aura carregada do player (azul) e do boss (verde): chamas subindo pelo corpo, brilho com luz,
//   faíscas e um anel no chão
// - explosão da aura, no mesmo estilo: clarão com luz, onda de choque no chão, anel de frente,
//   chamas e faíscas para fora
// - faíscas do botão que abre a porta (cena 1): clarão, faíscas que quicam no chão com novos
//   estalos, brasas e fumaça
// As camadas são filhas do sistema original e tocam, param e limpam junto com ele (Play/Stop/Clear
// do PlayerPower, do BossAura e dos botões), então a lógica não muda. Os nomes começam com VFX_
// (o Restore Particles do PowerVfxBuilder apaga esses filhos).
// Tamanhos em metros (escala Local): o player tem escala 1,8 na cena 1, então nas cenas os valores
// são refeitos com a altura de cada personagem
public static class AuraVfx
{
    const string Folder = "Assets/Materials/Aura/";
    const float PlayerHeight = 2.2f;
    const float BossHeight = 4f;

    static Material glow, flame, ring, smoke;

    [MenuItem("Tools/VFX/Melhorar Auras")]
    public static void Run()
    {
        glow = Additive("Aura Brilho", "Aura_Brilho.png");
        flame = Additive("Aura Chama", "Aura_Chama.png");
        ring = Additive("Aura Anel", "Aura_Anel.png");
        smoke = AlphaBlend("FX Fumaca", "FX_Fumaca.png");

        float playerScale = EditPrefab("Assets/Objects/Player.prefab", "PowerAura", "PowerExplosion", PlayerHeight);
        float bossScale = EditPrefab("Assets/Objects/Enemy 4.prefab", "EnemyAura", "EnemyExplosion", BossHeight);

        // as cenas têm valores próprios no sistema original (cores, escala): ajusta lá também
        foreach (string s in new[] { "Scene 1", "Scene 2" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + s + ".unity");

            foreach (var p in Object.FindObjectsByType<PlayerPower>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Instance(p.powerAura, p.powerExplosion, PlayerHeight, playerScale);

            foreach (var b in Object.FindObjectsByType<BossAura>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Instance(b.auraChargedEffect, b.auraExplosionEffect, BossHeight, bossScale);

            // a lista inclui os filhos VFX_ de antes, que o ButtonSparks apaga e refaz
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (ps != null && ps.name == "BotonExplosion")
                    ButtonSparks(ps);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("AuraVfx: pronto");
    }

    // devolve a escala da aura no prefab, para as cenas compararem
    static float EditPrefab(string path, string auraName, string explosionName, float height)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var aura = root.transform.Find(auraName).GetComponent<ParticleSystem>();
            BuildAura(aura, height);
            var explosion = root.transform.Find(explosionName);
            if (explosion != null)
                BuildExplosion(explosion.GetComponent<ParticleSystem>(), height);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return aura.transform.lossyScale.x;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void Instance(ParticleSystem aura, ParticleSystem explosion, float height, float prefabScale)
    {
        if (aura == null)
            return;

        float h = height * aura.transform.lossyScale.x / prefabScale;
        ConfigureAura(aura, h);
        Colorize(aura);
        if (explosion != null)
        {
            ConfigureExplosion(explosion, h);
            Colorize(explosion);
        }
    }

    // ---------- aura carregada ----------

    static void BuildAura(ParticleSystem aura, float h)
    {
        Clear(aura);

        // o sistema original fica na cintura (no pivô do personagem): pés meia altura abaixo
        Vector3 feet = Vector3.down * 0.48f * h;

        var flames = Child(aura, "VFX_Chamas", flame, feet);
        flames.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.VerticalBillboard;

        var aureole = Child(aura, "VFX_Brilho", glow, Vector3.up * 0.05f * h);
        var aureoleMain = aureole.main;
        aureoleMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        Lamp(aureole);

        var sparks = Child(aura, "VFX_Faiscas", glow, feet);
        sparks.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Stretch;

        var halo = Child(aura, "VFX_Anel", ring, feet + Vector3.up * 0.05f);
        var haloMain = halo.main;
        haloMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        halo.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;

        ConfigureAura(aura, h);
        Colorize(aura);
    }

    static void ConfigureAura(ParticleSystem aura, float h)
    {
        Base(aura, h);

        foreach (Transform child in aura.transform)
        {
            var ps = child.GetComponent<ParticleSystem>();
            if (ps == null)
                continue;
            var m = ps.main;
            var e = ps.emission;

            switch (child.name)
            {
                // chamas: línguas em pé subindo em volta da silhueta, tremulando
                case "VFX_Chamas":
                    m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.25f * h, 0.38f * h);
                    m.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
                    e.rateOverTime = 90f;
                    Circle(ps, 0.24f * h, 0.35f);
                    Rise(ps, 1.0f * h, 1.5f * h);
                    Grow(ps, 0.6f, 1f, 0.15f);
                    Noise(ps, 0.25f * h, 1.2f);
                    break;

                // brilho em volta do corpo, pulsando, com a luz da aura
                case "VFX_Brilho":
                    m.startLifetime = 0.6f;
                    m.startSize = 0.95f * h;
                    e.rateOverTime = 8f;
                    Grow(ps, 0.85f, 1.05f, 0.9f);
                    var lights = ps.lights;
                    lights.rangeMultiplier = 2.2f * h;
                    lights.intensityMultiplier = 8f;
                    break;

                // faíscas rápidas, esticadas no movimento
                case "VFX_Faiscas":
                    m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.025f * h, 0.05f * h);
                    e.rateOverTime = 30f;
                    Circle(ps, 0.22f * h, 0.6f);
                    Rise(ps, 1.5f * h, 3f * h);
                    Noise(ps, 0.4f * h, 2f);
                    Stretch(ps, 0.06f);
                    break;

                // anel no chão abrindo
                case "VFX_Anel":
                    m.startLifetime = 0.9f;
                    m.startSize = 0.9f * h;
                    e.rateOverTime = 2.5f;
                    Grow(ps, 0.5f, 0.9f, 1.25f);
                    break;
            }
            EditorUtility.SetDirty(ps);
        }
    }

    // sistema original: as bolas grandes viram pontos de energia menores, com textura suave
    static void Base(ParticleSystem aura, float h)
    {
        var m = aura.main;
        m.startSize = new ParticleSystem.MinMaxCurve(0.03f * h, 0.12f * h);
        aura.GetComponent<ParticleSystemRenderer>().sharedMaterial = glow;
        EditorUtility.SetDirty(aura);
    }

    // ---------- explosão da aura ----------

    static void BuildExplosion(ParticleSystem explosion, float h)
    {
        Clear(explosion);
        Vector3 feet = Vector3.down * 0.48f * h;

        var flash = Child(explosion, "VFX_Clarao", glow, Vector3.zero);
        var flashMain = flash.main;
        flashMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        Lamp(flash);

        var wave = Child(explosion, "VFX_Onda", ring, feet + Vector3.up * 0.08f);
        var waveMain = wave.main;
        waveMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        wave.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;

        var front = Child(explosion, "VFX_OndaFrente", ring, Vector3.zero);
        var frontMain = front.main;
        frontMain.simulationSpace = ParticleSystemSimulationSpace.Local;

        Child(explosion, "VFX_Chamas", flame, feet + Vector3.up * 0.2f * h);

        var sparks = Child(explosion, "VFX_Faiscas", glow, Vector3.zero);
        sparks.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Stretch;

        ConfigureExplosion(explosion, h);
        Colorize(explosion);
    }

    static void ConfigureExplosion(ParticleSystem explosion, float h)
    {
        // rastros originais: mais finos, com textura suave
        var bm = explosion.main;
        bm.startSize = new ParticleSystem.MinMaxCurve(0.04f * h, 0.1f * h);
        explosion.GetComponent<ParticleSystemRenderer>().sharedMaterial = glow;
        EditorUtility.SetDirty(explosion);

        foreach (Transform child in explosion.transform)
        {
            var ps = child.GetComponent<ParticleSystem>();
            if (ps == null)
                continue;
            var m = ps.main;
            m.duration = 1f;
            m.loop = false;

            switch (child.name)
            {
                // clarão branco-azulado que acende o ambiente
                case "VFX_Clarao":
                    m.startLifetime = 0.3f;
                    m.startSize = 2.2f * h;
                    Burst(ps, 0f, 2);
                    Grow(ps, 0.5f, 1f, 1.1f);
                    var lights = ps.lights;
                    lights.rangeMultiplier = 4f * h;
                    lights.intensityMultiplier = 25f;
                    break;

                // onda de choque rasante no chão
                case "VFX_Onda":
                    m.startLifetime = 0.55f;
                    m.startSize = 4.5f * h;
                    Burst(ps, 0f, 1);
                    Expand(ps);
                    break;

                // anel de frente para a câmera, mais rápido
                case "VFX_OndaFrente":
                    m.startLifetime = 0.35f;
                    m.startSize = 2.6f * h;
                    Burst(ps, 0f, 1);
                    Expand(ps);
                    break;

                // chamas jogadas para fora em roda
                case "VFX_Chamas":
                    m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.3f * h, 0.45f * h);
                    m.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * h, 4.5f * h);
                    m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    Burst(ps, 0f, 45);
                    Circle(ps, 0.2f * h, 0.2f);
                    Rise(ps, 0.3f * h, 0.8f * h);
                    Grow(ps, 0.7f, 1f, 0.2f);
                    Drag(ps, 3f);
                    break;

                // faíscas em todas as direções
                case "VFX_Faiscas":
                    m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.03f * h, 0.06f * h);
                    m.startSpeed = new ParticleSystem.MinMaxCurve(4f * h, 8f * h);
                    m.gravityModifier = 0.3f;
                    Burst(ps, 0f, 70);
                    var shape = ps.shape;
                    shape.enabled = true;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.15f * h;
                    Drag(ps, 2.5f);
                    Stretch(ps, 0.05f);
                    break;
            }
            EditorUtility.SetDirty(ps);
        }
    }

    // ---------- faíscas do botão ----------

    static void ButtonSparks(ParticleSystem button)
    {
        Clear(button);

        // estouro original: faíscas finas e esticadas (antes eram bolas amarelas)
        var bm = button.main;
        bm.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        bm.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        bm.startColor = new Color(1f, 0.85f, 0.45f);
        button.GetComponent<ParticleSystemRenderer>().sharedMaterial = glow;
        Stretch(button, 0.04f);
        EditorUtility.SetDirty(button);

        Color white = new Color(1f, 0.97f, 0.85f);
        Color yellow = new Color(1f, 0.82f, 0.35f);
        Color orange = new Color(1f, 0.45f, 0.1f);
        Color red = new Color(0.8f, 0.12f, 0.05f);

        // clarão com luz laranja
        var flash = Child(button, "VFX_Clarao", glow, Vector3.zero);
        {
            var m = flash.main;
            m.duration = 1f;
            m.loop = false;
            m.simulationSpace = ParticleSystemSimulationSpace.Local;
            m.startLifetime = 0.2f;
            m.startSize = 1.4f;
            m.startColor = yellow;
            Burst(flash, 0f, 1);
            Grow(flash, 0.6f, 1f, 1.1f);
            Gradient(flash, white, orange, 0.9f, 0.05f);
            Lamp(flash);
            var lights = flash.lights;
            lights.rangeMultiplier = 6f;
            lights.intensityMultiplier = 12f;
        }

        // faíscas: estouro e mais dois estalos, caindo e quicando no chão
        var sparks = Child(button, "VFX_Faiscas", glow, Vector3.zero);
        {
            var m = sparks.main;
            m.duration = 1f;
            m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 1f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            m.startColor = new ParticleSystem.MinMaxGradient(white, yellow);
            m.gravityModifier = 1.3f;
            var e = sparks.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 45),
                new ParticleSystem.Burst(0.18f, 14),
                new ParticleSystem.Burst(0.42f, 9),
            });
            var shape = sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            Gradient(sparks, yellow, orange, 1f, 0.6f);
            Stretch(sparks, 0.035f);
            var collision = sparks.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.bounce = 0.35f;
            collision.dampen = 0.3f;
            collision.lifetimeLoss = 0.15f;
            collision.radiusScale = 0.5f;
        }

        // brasas mais lentas, flutuando
        var embers = Child(button, "VFX_Brasas", glow, Vector3.zero);
        {
            var m = embers.main;
            m.duration = 1f;
            m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            m.startColor = new ParticleSystem.MinMaxGradient(yellow, orange);
            m.gravityModifier = 0.25f;
            Burst(embers, 0f, 16);
            var shape = embers.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            Noise(embers, 0.6f, 1.5f);
            Gradient(embers, orange, red, 1f, 0.7f);
        }

        // fumaça escura subindo
        var puff = Child(button, "VFX_Fumaca", smoke, Vector3.zero);
        {
            var m = puff.main;
            m.duration = 1f;
            m.loop = false;
            m.startDelay = 0.05f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startColor = new Color(0.16f, 0.16f, 0.18f);
            Burst(puff, 0f, 7);
            var shape = puff.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            Rise(puff, 0.3f, 0.6f);
            Grow(puff, 0.6f, 1.2f, 1.8f);
            var col = puff.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.5f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var rot = puff.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        }
    }

    // ---------- cores ----------

    // cores das camadas a partir das duas cores do sistema original (azul do player, verde do boss)
    static void Colorize(ParticleSystem system)
    {
        Color color = system.main.startColor.colorMin;
        Color light = system.main.startColor.colorMax;
        color.a = light.a = 1f;

        foreach (Transform child in system.transform)
        {
            var ps = child.GetComponent<ParticleSystem>();
            if (ps == null)
                continue;

            var m = ps.main;
            switch (child.name)
            {
                case "VFX_Chamas":
                    m.startColor = new ParticleSystem.MinMaxGradient(color, light);
                    Fade(ps, color, light, 0.9f);
                    break;
                case "VFX_Brilho":
                    m.startColor = light;
                    Fade(ps, light, light, 0.16f);
                    break;
                case "VFX_Clarao":
                    m.startColor = light;
                    Gradient(ps, Color.white, light, 0.55f, 0f);
                    break;
                case "VFX_Faiscas":
                    m.startColor = new ParticleSystem.MinMaxGradient(light, Color.white);
                    Fade(ps, light, color, 1f);
                    break;
                case "VFX_Anel":
                    m.startColor = light;
                    Fade(ps, light, color, 0.7f);
                    break;
                case "VFX_Onda":
                case "VFX_OndaFrente":
                    m.startColor = light;
                    Gradient(ps, Color.white, color, 1f, 0f);
                    break;
                default:
                    continue;
            }
            EditorUtility.SetDirty(ps);
        }
    }

    // nasce clara, vai para a cor e some (com entrada suave)
    static void Fade(ParticleSystem ps, Color start, Color end, float alpha)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.Lerp(start, Color.white, 0.5f), 0f), new GradientColorKey(start, 0.3f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, 0.15f), new GradientAlphaKey(alpha * 0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    // começa forte já no primeiro quadro (estouros) e some
    static void Gradient(ParticleSystem ps, Color start, Color end, float alpha, float hold)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha, Mathf.Max(hold, 0.01f)), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    // ---------- módulos ----------

    static void Clear(ParticleSystem parent)
    {
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
            if (parent.transform.GetChild(i).name.StartsWith("VFX_"))
                Object.DestroyImmediate(parent.transform.GetChild(i).gameObject);
    }

    static ParticleSystem Child(ParticleSystem parent, string name, Material mat, Vector3 offset)
    {
        var go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = parent.transform.position + offset;
        go.transform.rotation = Quaternion.identity;

        var ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.duration = 5f;
        m.loop = true;
        m.playOnAwake = false;
        m.startSpeed = 0f;
        m.maxParticles = 300;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        // tamanho em metros, sem a escala do personagem
        m.scalingMode = ParticleSystemScalingMode.Local;

        var shape = ps.shape;
        shape.enabled = false;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    // luz pontual presa às partículas (acende e apaga com elas)
    static void Lamp(ParticleSystem ps)
    {
        var lamp = new GameObject("VFX_Luz");
        lamp.transform.SetParent(ps.transform, false);
        var l = lamp.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 1f;
        l.intensity = 1f;
        l.shadows = LightShadows.None;
        lamp.SetActive(false);

        var lights = ps.lights;
        lights.enabled = true;
        lights.light = l;
        lights.ratio = 1f;
        lights.maxLights = 1;
        lights.useParticleColor = true;
        lights.alphaAffectsIntensity = true;
        lights.sizeAffectsRange = false;
    }

    static void Burst(ParticleSystem ps, float time, int count)
    {
        var e = ps.emission;
        e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(time, count) });
    }

    static void Circle(ParticleSystem ps, float radius, float thickness)
    {
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = thickness;
        shape.rotation = new Vector3(90f, 0f, 0f);
    }

    static void Rise(ParticleSystem ps, float min, float max)
    {
        var v = ps.velocityOverLifetime;
        v.enabled = true;
        v.space = ParticleSystemSimulationSpace.World;
        v.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        v.y = new ParticleSystem.MinMaxCurve(min, max);
        v.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    static void Noise(ParticleSystem ps, float strength, float frequency)
    {
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = strength;
        noise.frequency = frequency;
        noise.scrollSpeed = 1.5f;
    }

    static void Drag(ParticleSystem ps, float drag)
    {
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 1000f;
        limit.drag = drag;
    }

    static void Stretch(ParticleSystem ps, float velocityScale)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = velocityScale;
        r.lengthScale = 1f;
    }

    static void Grow(ParticleSystem ps, float start, float middle, float end)
    {
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, start), new Keyframe(0.3f, middle), new Keyframe(1f, end)));
    }

    // onda: abre rápido e desacelera
    static void Expand(ParticleSystem ps)
    {
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.08f, 0f, 4f), new Keyframe(0.4f, 0.75f), new Keyframe(1f, 1f)));
    }

    // ---------- materiais ----------

    // partícula aditiva (brilha com o bloom)
    static Material Additive(string name, string texture)
    {
        Material mat = Particle(name, texture);
        mat.SetColor("_BaseColor", new Color(1.6f, 1.6f, 1.6f, 1f));
        mat.SetFloat("_Blend", 2f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.One);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // fumaça: transparência comum (escurece em vez de brilhar)
    static Material AlphaBlend(string name, string texture)
    {
        Material mat = Particle(name, texture);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material Particle(string name, string texture)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + texture);
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        string path = Folder + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + texture));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        return mat;
    }
}
