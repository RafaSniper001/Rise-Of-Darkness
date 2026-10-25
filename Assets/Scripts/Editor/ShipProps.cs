using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// decoração da cena 1 (Tools/Scenes/Decorar Nave), com modelos CC0 da Kenney
// (Space Station Kit, Space Kit, Graveyard Kit e Furniture Kit):
// - objetos junto às paredes das fases 1 e 2, quase sem repetir modelo. Cada um tem colisor,
//   NavMeshObstacle que recorta o NavMesh (os inimigos desviam) e Destructible: quebra com o
//   sabre, o poder e os tiros dos inimigos. Barris explodem; containers podem soltar cura
// - enfeites pequenos nas prateleiras (sem colisão)
// - telas, interruptores e conduítes presos nas faces das barreiras (fases 1 e 2)
// - fase 3 (sala do caixão): castiçais com luz de vela em volta do caixão, telas e painéis na
//   parede norte, caixas de som, mesa holográfica e console nos cantos
// - LED branco embaixo de cada tábua da prateleira (fase 1), para iluminar os enfeites
// Rodar de novo apaga e recria os grupos "Decoração (Kenney)", "Enfeites da prateleira",
// "Enfeites de parede" e "Sala do caixão".
public static class ShipProps
{
    const string Station = "Assets/Objects/Kenney Space Station/";
    const string Kit = "Assets/Objects/Kenney Space Kit/";
    const string Graveyard = "Assets/Objects/Kenney Graveyard/";
    const string Furniture = "Assets/Objects/Kenney Furniture/";
    const string CeremonyGroupName = "Sala do caixão";

    // paleta do Graveyard Kit (textura própria, como o Space Station Kit)
    static Material graveyardMat;
    const string GroupName = "Decoração (Kenney)";
    const string ShelfGroupName = "Enfeites da prateleira";
    const string WallGroupName = "Enfeites de parede";

    // peça presa na face de uma barreira: x ao longo da parede, z da face, lado para onde a face
    // olha (+1 = +z, -1 = -z), altura da peça e altura da base acima do piso
    struct WallProp
    {
        public string model;
        public float x, faceZ, side, height, baseY;
        public WallProp(string model, float x, float faceZ, float side, float height, float baseY)
        {
            this.model = model; this.x = x; this.faceZ = faceZ; this.side = side; this.height = height; this.baseY = baseY;
        }
    }

    // faces das barreiras: Barreira z 17.5 (corredor 1) e 15.74 (corredor 2);
    // Barreira (1) z -2.38 (corredor 2) e -4.14 (corredor 3).
    // Longe dos postes do laser (x 44, corredor 1), das torretas (x 28.6, 46.7 e 65.2 na
    // Barreira (1)) e, no corredor 2, só telas altas acima da cabeça do R2D2
    static readonly WallProp[] WallProps =
    {
        new WallProp("S/display-wall-wide", 37f, 17.5f, 1f, 1.3f, 2.5f),
        new WallProp("S/wall-switch", 40.5f, 17.5f, 1f, 1f, 1.4f),
        new WallProp("S/wall-detail", 48f, 17.5f, 1f, 3.2f, 0f),
        new WallProp("S/display-wall", 51.5f, 17.5f, 1f, 1.2f, 2.6f),
        new WallProp("S/wall-detail", 61.5f, 17.5f, 1f, 3.2f, 0f),
        new WallProp("S/display-wall-wide", 66f, 17.5f, 1f, 1.3f, 2.5f),

        new WallProp("S/display-wall", 39f, 15.74f, -1f, 1.2f, 2.6f),
        new WallProp("S/wall-detail", 42f, 15.74f, -1f, 3.2f, 0f),
        new WallProp("S/display-wall-wide", 52f, 15.74f, -1f, 1.3f, 2.5f),
        new WallProp("S/wall-switch", 55f, 15.74f, -1f, 1f, 1.4f),
        new WallProp("S/wall-detail", 64f, 15.74f, -1f, 3.2f, 0f),

        new WallProp("S/display-wall-wide", 37f, -2.38f, 1f, 1.3f, 2.9f),
        new WallProp("S/display-wall", 56f, -2.38f, 1f, 1.2f, 2.9f),

        // fase 3 (sala do caixão), parede norte: telas de homenagem e painéis, sem conduítes
        new WallProp("S/display-wall-wide", 33f, -4.14f, -1f, 1.4f, 2.6f),
        new WallProp("S/wall-switch", 39f, -4.14f, -1f, 1f, 1.4f),
        new WallProp("S/display-wall-wide", 45f, -4.14f, -1f, 1.4f, 2.6f),
        new WallProp("S/wall-switch", 51f, -4.14f, -1f, 1f, 1.4f),
        new WallProp("S/display-wall-wide", 57f, -4.14f, -1f, 1.4f, 2.6f),
    };

    enum Kind { Container, Computer, Machine, Barrel, Rocks }

    struct Prop
    {
        public string model;
        public float x, z, yaw, height;
        public Kind kind;
        public Prop(string model, float x, float z, float yaw, float height, Kind kind)
        {
            this.model = model; this.x = x; this.z = z; this.yaw = yaw; this.height = height; this.kind = kind;
        }
    }

    // model: "S/" = Space Station Kit, "K/" = Space Kit. yaw: para onde a frente olha (0 = +z).
    // height: altura final em metros (o player tem ~4 m).
    // Livres de propósito: a faixa do laser da fase 1 (x 39 a 49 no corredor 1) e a ronda do
    // R2D2 no corredor 2 (x 37 a 65, z -1.5 a 4.5)
    static readonly Prop[] Props =
    {
        // corredor 1 (fase 1): barreira de baixo (z 17.5) e casco de cima (z ~32)
        new Prop("S/container-tall", 28f, 19.4f, 0f, 2.6f, Kind.Container),
        new Prop("S/container", 30.6f, 19.4f, 10f, 2f, Kind.Container),
        new Prop("S/computer-system", 34.5f, 19.6f, 0f, 2.2f, Kind.Computer),
        new Prop("K/machine_generator", 55f, 19.8f, 0f, 1.8f, Kind.Machine),
        new Prop("S/container-wide", 57.8f, 19.6f, 0f, 2.2f, Kind.Container),
        new Prop("S/skip-rocks", 31f, 30.2f, 90f, 2f, Kind.Rocks),
        new Prop("K/machine_barrelLarge", 35f, 30.2f, 180f, 2.8f, Kind.Machine),
        new Prop("S/computer-screen", 55f, 30.4f, 180f, 2.4f, Kind.Computer),
        new Prop("K/satelliteDish", 57.8f, 30.2f, 180f, 2.4f, Kind.Machine),

        // corredor 2 (fase 2): entre as duas barreiras (z 15.7 e -2.4)
        new Prop("S/computer-wide", 32f, 13.8f, 180f, 1.8f, Kind.Computer),
        new Prop("K/desk_computer", 35f, 13.8f, 180f, 1.6f, Kind.Computer),
        new Prop("S/container-tall", 45f, 14f, 180f, 2.6f, Kind.Container),
        new Prop("K/barrel", 47.6f, 14.2f, 0f, 1.6f, Kind.Barrel),
        new Prop("S/table-display", 58f, 13.6f, 180f, 2f, Kind.Computer),
        new Prop("K/barrels_rail", 30.5f, -0.8f, 0f, 1.8f, Kind.Barrel),

        // corredor 3 (fase 3): sala especial, decorada à parte (CeremonyProps)
    };

    // fase 3: sala do caixão (tapete vermelho, caixão no estrado ao sul, miniatura da nave a oeste).
    // Velas do Graveyard Kit ("G/") em volta do caixão e peças tecnológicas (Furniture Kit "F/",
    // Space Station Kit "S/") nos cantos, deixando livres o centro (luta), a rampa do caixão
    // (x 36 a 51 no sul) e o laser da entrada (x 64 a 74, z -4 a -9). light: luz de vela (0 = sem)
    struct CeremonyProp
    {
        public string model;
        public float x, z, yaw, height, light;
        public CeremonyProp(string model, float x, float z, float yaw, float height, float light = 0f)
        {
            this.model = model; this.x = x; this.z = z; this.yaw = yaw; this.height = height; this.light = light;
        }
    }

    // cantos da sala: peças tecnológicas (as telas e painéis da parede norte estão em WallProps)
    static readonly CeremonyProp[] CeremonyProps =
    {
        new CeremonyProp("F/speaker", 18.8f, -6f, 135f, 3f),
        new CeremonyProp("F/speaker", 18.8f, -17.8f, 45f, 3f),
        new CeremonyProp("S/table-display-planet", 73.4f, -17.6f, -45f, 2f),
        new CeremonyProp("S/computer-screen", 68.5f, -17.8f, 0f, 2.4f),
    };

    // em volta do caixão (no estrado): castiçais nos quatro cantos
    const string CoffinCandle = "G/candle-multiple";

    // tábua da prateleira no mundo: centro, altura da face de cima e eixos no plano do chão
    struct Plank
    {
        public Vector3 center, along, across;
        public float top, length, depth;
    }

    // enfeites das prateleiras, em ordem (repete a lista se faltar)
    static readonly string[] ShelfItems =
    {
        "K/weapon_rifle", "K/rock_crystals", "K/astronautA", "S/table-display-planet",
        "K/meteor_detailed", "K/weapon_gun", "S/computer-screen", "K/barrel",
        "K/satelliteDish", "S/container", "K/machine_wireless", "K/rock_crystalsLargeA",
    };

    [MenuItem("Tools/Scenes/Decorar Nave")]
    public static void Run()
    {
        Material stationMat = SetupMaterial();
        Material sparks = ParticleMaterial();
        Material heal = HealMaterial();
        graveyardMat = PaletteMaterial(Graveyard + "Kenney Graveyard Colormap.mat", Graveyard + "Textures/colormap.png");
        AudioClip breakSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Effects/kill.wav");

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Scene 1.unity");

        foreach (string old in new[] { GroupName, ShelfGroupName, WallGroupName, CeremonyGroupName })
        {
            var go = GameObject.Find(old);
            if (go != null)
                Object.DestroyImmediate(go);
        }

        var group = new GameObject(GroupName).transform;

        foreach (var p in Props)
        {
            var go = Place(p.model, group, stationMat, p.height, p.yaw);
            if (go == null)
                continue;

            // chão embaixo do ponto (ignora gatilhos)
            float floor = 5.25f;
            if (Physics.Raycast(new Vector3(p.x, 15f, p.z), Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                floor = hit.point.y;

            // centro da peça no ponto e base no chão
            Bounds b = WorldBounds(go);
            go.transform.position += new Vector3(p.x - b.center.x, floor - b.min.y, p.z - b.center.z);

            // colisor e obstáculo do tamanho da peça (no espaço local)
            LocalBox(go, out Vector3 center, out Vector3 size);

            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;

            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = center;
            obstacle.size = size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            // recorta já no primeiro frame (os inimigos nascem no Start)
            obstacle.carvingTimeToStationary = 0f;

            var d = go.AddComponent<Destructible>();
            d.breakSound = breakSound;
            d.explosionMaterial = sparks;
            d.healthDropMaterial = heal;
            switch (p.kind)
            {
                case Kind.Container: d.health = 50f; d.healthDropChance = 0.4f; break;
                case Kind.Computer: d.health = 30f; break;
                case Kind.Machine: d.health = 75f; d.healthDropChance = 0.25f; break;
                case Kind.Rocks: d.health = 40f; break;
                case Kind.Barrel: d.health = 20f; d.explosive = true; break;
            }
        }

        DecorateShelf(stationMat);
        DecorateWalls(stationMat);
        DecorateCeremony(stationMat);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ShipProps: " + group.childCount + " peças");
    }

    // instancia o modelo do kit com a altura pedida, girado para 'yaw'
    static GameObject Place(string model, Transform parent, Material stationMat, float height, float yaw)
    {
        string prefix = model.Substring(0, 2);
        string folder = prefix == "S/" ? Station : prefix == "G/" ? Graveyard : prefix == "F/" ? Furniture : Kit;
        string path = folder + model.Substring(2) + ".fbx";
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogWarning("ShipProps: modelo não encontrado " + path);
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.rotation;

        // Space Station Kit e Graveyard Kit usam uma textura de paleta;
        // Space Kit e Furniture Kit vêm com materiais de cor próprios
        Material palette = prefix == "S/" ? stationMat : prefix == "G/" ? graveyardMat : null;
        if (palette != null)
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = palette;
                r.sharedMaterials = mats;
            }

        float current = WorldBounds(go).size.y;
        if (current > 0.001f)
            go.transform.localScale *= height / current;

        return go;
    }

    // prateleiras: as tábuas são as peças finas e largas do modelo; os enfeites ficam
    // na frente de cada tábua (lado do corredor), sem encostar no que já está lá
    static void DecorateShelf(Material stationMat)
    {
        var shelf = GameObject.Find("Millennium Falcon/Prateleira");
        if (shelf == null)
        {
            Debug.LogWarning("ShipProps: prateleira não encontrada");
            return;
        }

        var planks = new List<Plank>();
        var occupied = new List<Bounds>();

        // a prateleira é girada (diagonal): cada tábua é medida nos eixos da própria malha
        foreach (var mf in shelf.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null)
                continue;

            Bounds local = mf.sharedMesh.bounds;
            Vector3[] edges =
            {
                mf.transform.TransformVector(Vector3.right * local.size.x),
                mf.transform.TransformVector(Vector3.up * local.size.y),
                mf.transform.TransformVector(Vector3.forward * local.size.z),
            };
            System.Array.Sort(edges, (a, b) => a.magnitude.CompareTo(b.magnitude));

            // tábua: fina na vertical e comprida
            if (edges[0].magnitude < 0.15f && Mathf.Abs(edges[0].normalized.y) > 0.9f && edges[2].magnitude > 2f)
            {
                Vector3 center = mf.transform.TransformPoint(local.center);
                planks.Add(new Plank
                {
                    center = center,
                    top = center.y + edges[0].magnitude * 0.5f,
                    along = Vector3.ProjectOnPlane(edges[2], Vector3.up).normalized,
                    across = Vector3.ProjectOnPlane(edges[1], Vector3.up).normalized,
                    length = edges[2].magnitude,
                    depth = edges[1].magnitude,
                });
            }
            else if (edges[2].magnitude < 2f)
            {
                // divisórias entre as tábuas (um pouco menores, para os enfeites caberem perto)
                Bounds b = mf.GetComponent<Renderer>().bounds;
                b.Expand(-0.1f);
                occupied.Add(b);
            }
        }

        planks.Sort((a, b) => a.top.CompareTo(b.top));
        Debug.Log("ShipProps: prateleira com " + planks.Count + " tábuas, " + occupied.Count + " divisórias");

        var group = new GameObject(ShelfGroupName).transform;
        int next = 0;
        Random.InitState(5);
        Vector3 corridor = new Vector3(45f, 0f, 24f);

        for (int level = 0; level < planks.Count; level++)
        {
            Plank plank = planks[level];
            float gap = level + 1 < planks.Count ? planks[level + 1].top - plank.top - 0.05f : 0.9f;
            float itemHeight = Mathf.Clamp(gap * 0.7f, 0.25f, 0.6f);

            // lado da tábua virado para o corredor
            Vector3 front = Vector3.Dot(corridor - plank.center, plank.across) > 0f ? plank.across : -plank.across;
            float frontYaw = Quaternion.LookRotation(front).eulerAngles.y;

            int slots = Mathf.Max(1, Mathf.FloorToInt(plank.length / 1.2f));
            for (int s = 0; s < slots; s++)
            {
                float t = ((s + 0.5f) / slots - 0.5f) * plank.length * 0.88f;
                Vector3 spot = plank.center + plank.along * t + front * plank.depth * 0.1f;

                string model = ShelfItems[next % ShelfItems.Length];
                var go = Place(model, group, stationMat, itemHeight, frontYaw + Random.Range(-25f, 25f));
                if (go == null)
                    continue;

                // cabe na profundidade da tábua
                Bounds b = WorldBounds(go);
                float foot = Mathf.Max(b.size.x, b.size.z);
                float maxFoot = plank.depth * 0.75f;
                if (foot > maxFoot)
                    go.transform.localScale *= maxFoot / foot;

                b = WorldBounds(go);
                go.transform.position += new Vector3(spot.x - b.center.x, plank.top - b.min.y + 0.005f, spot.z - b.center.z);
                b = WorldBounds(go);

                bool blocked = false;
                foreach (var o in occupied)
                    if (o.Intersects(b)) { blocked = true; break; }

                if (blocked)
                {
                    Object.DestroyImmediate(go);
                    continue;
                }

                occupied.Add(b);
                next++;
            }

            // LED branco: fita contínua colada embaixo da tábua de cima, na borda da frente.
            // A luz é uma fileira densa de luzes fracas ao longo da fita, com alcance que cobre o
            // vão: os círculos se sobrepõem e a prateleira fica clara por igual, como uma linha
            // de luz (o URP não tem luz de área em tempo real; o projeto usa Forward+).
            // A fita tem o comprimento da tábua de cima, que na moldura oval é mais curta: assim
            // não atravessa a moldura. A tábua mais alta não tem tábua em cima (fica rente à
            // curva da moldura): só leva as luzes, mais curtas e no miolo
            bool top = level + 1 >= planks.Count;
            Plank upper = top ? plank : planks[level + 1];
            float ledY = top ? plank.top + 0.55f : upper.top - 0.08f;
            float height = ledY - plank.top;
            float span = top ? plank.length * 0.55f : Mathf.Min(plank.length, upper.length) * 0.88f;
            Vector3 ledCenter = new Vector3(plank.center.x, ledY, plank.center.z) + front * plank.depth * 0.35f;
            var holder = new GameObject("LED").transform;
            holder.SetParent(group, false);
            holder.position = ledCenter;

            if (!top)
            {
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = "Fita";
                Object.DestroyImmediate(strip.GetComponent<Collider>());
                strip.transform.SetParent(holder, false);
                // eixo x da fita ao longo da tábua
                strip.transform.rotation = Quaternion.LookRotation(front, Vector3.up);
                strip.transform.localScale = new Vector3(span, 0.02f, 0.035f);
                var stripRenderer = strip.GetComponent<Renderer>();
                stripRenderer.sharedMaterial = LedMaterial();
                stripRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            int lights = Mathf.CeilToInt(span / 0.6f);
            for (int l = 0; l < lights; l++)
            {
                float t = ((l + 0.5f) / lights - 0.5f) * span;
                var led = new GameObject("Luz LED").AddComponent<Light>();
                led.transform.SetParent(holder, true);
                led.transform.position = ledCenter + plank.along * t + Vector3.down * 0.05f - front * plank.depth * 0.15f;
                led.type = LightType.Point;
                led.color = new Color(0.94f, 0.97f, 1f);
                led.range = Mathf.Max(1f, height * 2f);
                led.intensity = 0.3f;
                led.shadows = LightShadows.None;
            }
        }

        Debug.Log("ShipProps: " + group.childCount + " enfeites e LEDs na prateleira");
    }

    // fita de LED: branca e brilhante (o bloom faz o halo)
    static Material LedMaterial()
    {
        string path = Kit + "LED.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", Color.white);
        mat.SetColor("_EmissionColor", new Color(1.3f, 1.35f, 1.45f));
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // sala do caixão (fase 3): castiçais em volta do caixão e peças tecnológicas nos cantos.
    // Têm colisor e recortam o NavMesh, mas não quebram (é uma sala especial)
    static void DecorateCeremony(Material stationMat)
    {
        var group = new GameObject(CeremonyGroupName).transform;

        foreach (var p in CeremonyProps)
        {
            var go = Place(p.model, group, stationMat, p.height, p.yaw);
            if (go != null)
                Settle(go, p.x, p.z, p.light, true);
        }

        // caixão: castiçais nos quatro cantos, a 1,2 m dele, no estrado
        var coffin = GameObject.Find("Millennium Falcon/CAIXÃO PADMÉ_COLORIDO");
        if (coffin != null)
        {
            Bounds c = WorldBounds(coffin);
            float[] xs = { c.min.x - 1.2f, c.max.x + 1.2f };
            float[] zs = { c.min.z - 1.2f, c.max.z + 1.2f };
            foreach (float x in xs)
                foreach (float z in zs)
                {
                    var go = Place(CoffinCandle, group, stationMat, 0.9f, Random.Range(0f, 360f));
                    if (go != null)
                        Settle(go, x, z, 1f, false);
                }
            Debug.Log("ShipProps: caixão em " + c.center.ToString("F1") + " tamanho " + c.size.ToString("F1"));
        }

        Debug.Log("ShipProps: " + group.childCount + " peças na sala do caixão");
    }

    // apoia a peça no chão do ponto (x, z), com colisor; obstáculo do NavMesh e luz de vela opcionais
    static void Settle(GameObject go, float x, float z, float light, bool obstacle)
    {
        float floor = 5.25f;
        if (Physics.Raycast(new Vector3(x, 15f, z), Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            floor = hit.point.y;

        Bounds b = WorldBounds(go);
        go.transform.position += new Vector3(x - b.center.x, floor - b.min.y, z - b.center.z);

        LocalBox(go, out Vector3 center, out Vector3 size);
        var box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;

        if (obstacle)
        {
            var o = go.AddComponent<NavMeshObstacle>();
            o.shape = NavMeshObstacleShape.Box;
            o.center = center;
            o.size = size;
            o.carving = true;
            o.carveOnlyStationary = true;
            o.carvingTimeToStationary = 0f;
        }

        if (light > 0f)
        {
            b = WorldBounds(go);
            var lamp = new GameObject("Luz").AddComponent<Light>();
            lamp.transform.SetParent(go.transform, true);
            lamp.transform.position = new Vector3(b.center.x, b.max.y - b.size.y * 0.2f, b.center.z);
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.72f, 0.4f);
            lamp.range = 5f * light;
            lamp.intensity = 4f * light;
            lamp.shadows = LightShadows.None;
        }
    }

    // telas, interruptores e conduítes presos nas barreiras (com colisor, sem quebrar)
    static void DecorateWalls(Material stationMat)
    {
        const float floor = 5.25f;
        var group = new GameObject(WallGroupName).transform;

        foreach (var w in WallProps)
        {
            var go = Place(w.model, group, stationMat, w.height, w.side > 0f ? 0f : 180f);
            if (go == null)
                continue;

            // costas encostadas na face da barreira e base na altura pedida
            Bounds b = WorldBounds(go);
            float backZ = w.side > 0f ? b.min.z : b.max.z;
            go.transform.position += new Vector3(w.x - b.center.x, floor + w.baseY - b.min.y, w.faceZ - backZ);

            LocalBox(go, out Vector3 center, out Vector3 size);
            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;

            // os conduítes vão até o chão: os inimigos desviam
            if (w.baseY < 0.1f)
            {
                var obstacle = go.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = center;
                obstacle.size = size;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.carvingTimeToStationary = 0f;
            }
        }

        Debug.Log("ShipProps: " + group.childCount + " enfeites de parede");
    }

    static Material PaletteMaterial(string path, string texture)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.4f);
        mat.SetFloat("_Metallic", 0.1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material SetupMaterial()
    {
        string path = Station + "Kenney Colormap.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Station + "Textures/colormap.png"));
        // cores do kit um pouco mais escuras, para casar com o metal da nave
        mat.SetColor("_BaseColor", new Color(0.78f, 0.78f, 0.82f));
        mat.SetFloat("_Smoothness", 0.45f);
        mat.SetFloat("_Metallic", 0.2f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // faíscas da explosão dos barris
    static Material ParticleMaterial()
    {
        string path = Kit + "Faisca.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        // transparente aditivo
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 2f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // cápsula de cura: verde brilhante
    static Material HealMaterial()
    {
        string path = Kit + "Cura.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", new Color(0.3f, 1f, 0.45f));
        mat.SetColor("_EmissionColor", new Color(0.4f, 2.5f, 0.8f));
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Bounds WorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    // caixa da peça nos eixos do próprio objeto (sem a escala)
    static void LocalBox(GameObject go, out Vector3 center, out Vector3 size)
    {
        var min = Vector3.one * float.MaxValue;
        var max = Vector3.one * float.MinValue;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            foreach (var v in mf.sharedMesh.vertices)
            {
                var p = go.transform.InverseTransformPoint(mf.transform.TransformPoint(v));
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }
        center = (min + max) * 0.5f;
        size = max - min;
    }
}
