using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Monta o player animado (máquina de estados + Animation Events) e o sabre na mão.
// Menus: Tools > Characters > Setup Player Rig  |  Tools > Fonts > Apply Fonts
public static class PlayerRigSetup
{
    const string MageDir = "Assets/Characters/Mage";
    const string ModelPath = MageDir + "/Mage.fbx";
    const string ControllerPath = MageDir + "/Player.controller";
    const string UpperMaskPath = MageDir + "/UpperBody.mask";
    const string PlayerPrefab = "Assets/Objects/Player.prefab";
    const string MageEnemyPrefab = "Assets/Objects/Enemy 4.prefab";
    const string CutSoundsDir = "Assets/Sounds/Effects/Cortados";

    const string IdleClip = "Sword And Shield Idle";
    const string WalkClip = "Sword And Shield Walk";
    const string RunClip = "Run With Sword";
    const string AttackClip = "Standing Melee Combo Attack Ver. 3";
    const string PowerClip = "Standing 1H Magic Attack 03";
    const string DeathClip = "Two Handed Sword Death";
    const string FallClip = "Falling Idle";
    const string LandClip = "Falling To Landing";

    // tamanho do sabre em relação à altura do personagem
    const float SabreToHeight = 0.6f;

    // os sons do sabre (cortados) têm o ápice ~0,12 s após o início
    const float SwingSoundLead = 0.12f;

    // passos: ápice do som cortado (~10 ms) + buffer de áudio padrão (1024)
    // + o atraso que ainda se ouvia no jogo
    const float FootstepLead = 0.09f;

    [MenuItem("Tools/Characters/Setup Player Rig")]
    public static void Run()
    {
        FixFeetReference();
        ImportClips();
        BuildController();
        SetupPrefab();
        SetupMageEnemy();
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerRigSetup] Concluído.");
    }

    // ---------- mão do sabre na queda e no pouso ----------

    // grava no player e no boss a pegada da animação parada (mão direita: pulso e dedos),
    // que a queda e o pouso usam no lugar da mão aberta dessas animações
    [MenuItem("Tools/Characters/Gravar Pegada do Sabre")]
    public static void BakeSwordGrip()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        foreach (string path in new[] { PlayerPrefab, MageEnemyPrefab })
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var animator = instance.GetComponentInChildren<Animator>();
            animator.Rebind();
            animator.SetBool("Grounded", true);
            animator.SetFloat("Speed", 0f);
            animator.Play("Locomotion", 0, 0.3f);
            animator.Update(0f);
            animator.Update(0.2f);

            var pose = new Quaternion[PlayerAnimator.SwordGripBones.Length];
            for (int i = 0; i < pose.Length; i++)
                pose[i] = animator.GetBoneTransform(PlayerAnimator.SwordGripBones[i]).localRotation;
            Object.DestroyImmediate(instance);

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var player = root.GetComponentInChildren<PlayerAnimator>(true);
                if (player != null)
                    player.swordGripPose = pose;
                var boss = root.GetComponentInChildren<BossAnimator>(true);
                if (boss != null)
                    boss.swordGripPose = pose;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[PlayerRigSetup] pegada do sabre gravada em " + path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    // ---------- pés: corrige o pé virado para fora ----------

    // ângulo (em torno do eixo vertical) entre a direção do pé (pé -> dedos) e a frente do modelo
    static float FootYaw(Animator animator, HumanBodyBones foot, HumanBodyBones toes)
    {
        Vector3 dir = animator.GetBoneTransform(toes).position - animator.GetBoneTransform(foot).position;
        dir.y = 0f;
        return Vector3.SignedAngle(animator.transform.forward, dir, Vector3.up);
    }

    // O modelo do mago tem os pés ~23° virados para fora na pose base; o avatar humanoid usa
    // essa pose como "pé reto", então toda animação sai com o pé virado a mais.
    // Com StraightenFeet, a pose de referência do pé é girada para frente (só no eixo vertical).
    // (O tranco lateral na passada que aparecia junto vinha do retarget e é resolvido pelo Foot IK.)
    const bool StraightenFeet = true;

    static void FixFeetReference()
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        var model = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        try
        {
            var animator = model.GetComponent<Animator>();
            var hd = imp.humanDescription;
            var skeleton = hd.skeleton;

            foreach (var (foot, toes) in new[] { (HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes), (HumanBodyBones.RightFoot, HumanBodyBones.RightToes) })
            {
                var footT = animator.GetBoneTransform(foot);
                float yaw = FootYaw(animator, foot, toes);
                Quaternion local = footT.localRotation;
                if (StraightenFeet)
                    local = Quaternion.Inverse(footT.parent.rotation) * (Quaternion.AngleAxis(-yaw, Vector3.up) * footT.rotation);

                for (int i = 0; i < skeleton.Length; i++)
                    if (skeleton[i].name == footT.name)
                        skeleton[i].rotation = local;

                Debug.Log($"[PlayerRigSetup] {foot}: virado {yaw:F1}° na pose base {(StraightenFeet ? "(referência endireitada)" : "(referência original)")}");
            }

            hd.skeleton = skeleton;
            imp.humanDescription = hd;
            imp.SaveAndReimport();
        }
        finally
        {
            Object.DestroyImmediate(model);
        }
    }

    // ---------- inimigo mago (cena 2): mesmo modelo e mesmas animações do player ----------

    const string MageEnemyScene = "Assets/Scenes/Scene 2.unity";

    // altura do modelo na pose base (mesma pose para player e boss: mesmo modelo)
    static float ModelHeight(Transform visual)
    {
        float height = 0f;
        foreach (var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            height = Mathf.Max(height, r.bounds.size.y);
        return height;
    }

    static void SetupMageEnemy()
    {
        // os scripts do boss e o sabre azul foram adicionados na cena: passam para o prefab
        var scene = EditorSceneManager.OpenScene(MageEnemyScene, OpenSceneMode.Single);
        var boss = Object.FindFirstObjectByType<BossChase>();
        var instance = PrefabUtility.GetOutermostPrefabInstanceRoot(boss.gameObject);
        if (instance != null && PrefabUtility.HasPrefabInstanceAnyOverrides(instance, false))
        {
            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene);
        }

        // o player da cena 2 tem escala própria (1,8): o boss fica da mesma altura dele
        float playerHeight = ModelHeight(Object.FindFirstObjectByType<PlayerMovement>().transform.Find("Visual"));
        float bossHeight = ModelHeight(boss.transform.Find("Visual"));
        float heightScale = playerHeight / bossHeight;
        Debug.Log($"[PlayerRigSetup] cena 2: altura do player {playerHeight:F2}, do boss {bossHeight:F2}");

        var root = PrefabUtility.LoadPrefabContents(MageEnemyPrefab);
        try
        {
            if (Mathf.Abs(heightScale - 1f) > 0.005f)
            {
                root.transform.localScale *= heightScale;
                Debug.Log($"[PlayerRigSetup] Enemy 4: escala x{heightScale:F3} para ficar da altura do player da cena 2 (escala {root.transform.localScale})");
            }

            var visual = root.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(ModelPath);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var driver = visual.GetComponent<BossAnimator>();
            if (driver == null) driver = visual.gameObject.AddComponent<BossAnimator>();
            driver.animator = animator;
            driver.deathDuration = LoadClip(DeathClip).length;
            // mesma correção de pés do player (mesmo modelo e mesmo retarget)
            var playerDriver = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab).GetComponentInChildren<PlayerAnimator>();
            driver.leftFootYaw = playerDriver.leftFootYaw;
            driver.rightFootYaw = playerDriver.rightFootYaw;
            driver.leftFootYawRun = playerDriver.leftFootYawRun;
            driver.rightFootYawRun = playerDriver.rightFootYawRun;

            var chase = root.GetComponent<BossChase>();
            chase.leftFootSounds = Sounds("step 1", "step 3");
            chase.rightFootSounds = Sounds("step 2", "step 4");

            var sword = root.GetComponentInChildren<BossSword>(true);
            sword.attackSounds = Sounds("saber 1", "saber 2", "saber 3", "saber 4");

            AlignVisualToCapsule(root.transform, visual, false);
            AttachSabre(sword.transform.parent, visual, animator);

            driver.runAnimationSpeed = GaitMeasure.GroundSpeed(visual.gameObject, animator, LoadClip(RunClip));
            Debug.Log($"[PlayerRigSetup] Enemy 4: corrida acompanha os pés a {driver.runAnimationSpeed:F2} m/s (anda a {chase.moveSpeed})");

            PrefabUtility.SaveAsPrefabAsset(root, MageEnemyPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        // a cena herda o Visual do prefab e o boss começa apoiado no chão
        scene = EditorSceneManager.OpenScene(MageEnemyScene, OpenSceneMode.Single);
        boss = Object.FindFirstObjectByType<BossChase>();
        var bossVisual = boss.transform.Find("Visual");
        if (bossVisual != null && PrefabUtility.IsPartOfPrefabInstance(bossVisual))
            PrefabUtility.RevertObjectOverride(bossVisual, InteractionMode.AutomatedAction);
        PlaceOnGround(boss.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ---------- clipes e Animation Events ----------

    static string ClipPath(string file) => $"{MageDir}/Animations/{file}.fbx";

    static void ImportClips()
    {
        // 1ª passada: configurações de import (loop/root) com um clipe só e sem eventos
        foreach (var file in new[] { IdleClip, WalkClip, RunClip, AttackClip, PowerClip, DeathClip, FallClip, LandClip })
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(ClipPath(file));
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.importAnimation = true;

            bool loop = file == IdleClip || file == WalkClip || file == RunClip || file == FallClip;
            var clips = imp.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
                ConfigureClip(clips[i], loop);
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        // 2ª passada: amostra os clipes no modelo e posiciona os eventos
        var model = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        try
        {
            var animator = model.GetComponent<Animator>();
            SetEvents(WalkClip, FootstepEvents(model, animator, LoadClip(WalkClip)));
            SetEvents(RunClip, FootstepEvents(model, animator, LoadClip(RunClip)));
            CropPower(model, animator);
            SplitAttack(model, animator);
            CropLanding(model, animator);
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(model);
        }
    }

    static void ConfigureClip(ModelImporterClipAnimation clip, bool loop)
    {
        clip.loopTime = loop;
        clip.lockRootRotation = true;      // gira só pelo script
        clip.lockRootHeightY = true;       // altura fica na pose (morte cai no chão)
        clip.keepOriginalPositionY = true;
        clip.lockRootPositionXZ = false;   // deslocamento é descartado: anima no lugar
        clip.events = new AnimationEvent[0];
    }

    static AnimationClip LoadClip(string file, string clipName = null)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ClipPath(file)))
            if (o is AnimationClip c && !c.name.StartsWith("__preview__") && (clipName == null || c.name == clipName))
                return c;
        throw new FileNotFoundException($"Clip não encontrado: {file} {clipName}");
    }

    static AnimationEvent Event(string function, float time, int intParameter = 0)
    {
        return new AnimationEvent { functionName = function, time = time, intParameter = intParameter };
    }

    // ModelImporter usa tempo normalizado (0..1) dentro de cada clipe
    static AnimationEvent[] Normalized(List<AnimationEvent> events, float length)
    {
        var list = new List<AnimationEvent>();
        foreach (var e in events)
            list.Add(Event(e.functionName, Mathf.Clamp01(e.time / length), e.intParameter));
        return list.ToArray();
    }

    static void LogEvents(string name, float length, List<AnimationEvent> events)
    {
        var log = new System.Text.StringBuilder();
        foreach (var e in events) log.Append($"{e.functionName}{(e.functionName == "Footstep" ? "(" + e.intParameter + ")" : "")}@{e.time:F2}s ");
        Debug.Log($"[PlayerRigSetup] {name} ({length:F2}s): {log}");
    }

    static void SetEvents(string file, List<AnimationEvent> events)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(ClipPath(file));
        var clips = imp.clipAnimations;
        var clip = LoadClip(file);
        for (int i = 0; i < clips.Length; i++)
            clips[i].events = Normalized(events, clip.length);
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
        LogEvents(file, clip.length, events);
    }

    delegate void Probe(float t);

    static void Sample(GameObject model, AnimationClip clip, float fps, Probe probe)
    {
        if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        int frames = Mathf.Max(2, Mathf.RoundToInt(clip.length * fps));
        for (int f = 0; f <= frames; f++)
        {
            float t = clip.length * f / frames;
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(model, clip, t);
            AnimationMode.EndSampling();
            probe(t);
        }
    }

    // um evento Footstep (0 = esquerdo, 1 = direito) no instante em que cada pé toca o chão
    static List<AnimationEvent> FootstepEvents(GameObject model, Animator animator, AnimationClip clip)
    {
        var events = new List<AnimationEvent>();
        var feet = new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot };
        for (int side = 0; side < 2; side++)
        {
            var foot = animator.GetBoneTransform(feet[side]);
            var times = new List<float>();
            var heights = new List<float>();
            Sample(model, clip, 60f, t => { times.Add(t); heights.Add(foot.position.y); });

            // ciclo fechado com média móvel (tira tremidas)
            int n = heights.Count - 1;
            var h = new float[n];
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                float sum = 0f;
                for (int k = -2; k <= 2; k++) sum += heights[(i + k + n) % n];
                h[i] = sum / 5f;
                min = Mathf.Min(min, h[i]); max = Mathf.Max(max, h[i]);
            }

            // a partir do ponto mais alto da passada, o pé desce; o contato com o chão é quando
            // a descida praticamente para (velocidade < 30% da maior velocidade de descida).
            // Medido nos quadros: no correr o pé ainda desce rápido a 20% da altura, então
            // "altura abaixo de um limiar" disparava cedo; "pé mais à frente" acontece no ar.
            for (int apex = 0; apex < n; apex++)
            {
                float prev = h[(apex - 1 + n) % n], next = h[(apex + 1) % n];
                if (!(h[apex] > prev && h[apex] >= next && h[apex] > (min + max) * 0.5f)) continue;

                // o contato precisa estar perto do chão (25% mais baixo da passada) e com a descida
                // já freando; no correr o pé passa à frente ainda no ar (pequeno "degrau" na curva)
                float threshold = min + (max - min) * 0.25f;
                float maxDrop = 0f;
                int contact = apex;
                for (int s = 1; s < n; s++)
                {
                    int i = (apex + s) % n;
                    int prevI = (i - 1 + n) % n;
                    float drop = h[prevI] - h[i];
                    maxDrop = Mathf.Max(maxDrop, drop);
                    if (h[i] < threshold && drop < maxDrop * 0.3f)
                    {
                        contact = h[prevI] < threshold ? prevI : i;
                        break;
                    }
                }

                events.Add(Event("Footstep", Mathf.Repeat(times[contact] - FootstepLead, clip.length), side));
            }
        }
        events.Sort((a, b) => a.time.CompareTo(b.time));
        return events;
    }

    // velocidade (suavizada) de uma mão ao longo do clipe
    static List<float> HandSpeeds(GameObject model, Animator animator, AnimationClip clip, HumanBodyBones bone, List<float> times)
    {
        var hand = animator.GetBoneTransform(bone);
        var raw = new List<float>();
        Vector3 last = Vector3.zero;
        bool first = true;
        times.Clear();
        Sample(model, clip, 60f, t =>
        {
            raw.Add(first ? 0f : (hand.position - last).magnitude);
            times.Add(t);
            last = hand.position;
            first = false;
        });

        var smooth = new List<float>();
        for (int i = 0; i < raw.Count; i++)
        {
            float sum = 0f; int count = 0;
            for (int k = -3; k <= 3; k++)
                if (i + k >= 0 && i + k < raw.Count) { sum += raw[i + k]; count++; }
            smooth.Add(sum / count);
        }
        return smooth;
    }

    struct Swing { public int start, end, peak; }

    // golpes = trechos em que a mão do sabre está rápida
    static List<Swing> FindSwings(List<float> speeds, List<float> times)
    {
        float max = 0f;
        foreach (var s in speeds) max = Mathf.Max(max, s);

        var swings = new List<Swing>();
        int i = 0;
        while (i < speeds.Count)
        {
            if (speeds[i] < max * 0.35f) { i++; continue; }
            var sw = new Swing { start = i, peak = i };
            while (i < speeds.Count && speeds[i] >= max * 0.35f)
            {
                if (speeds[i] > speeds[sw.peak]) sw.peak = i;
                i++;
            }
            sw.end = i - 1;

            // junta golpes separados por menos de 0,12 s
            if (swings.Count > 0 && times[sw.start] - times[swings[swings.Count - 1].end] < 0.12f)
            {
                var prev = swings[swings.Count - 1];
                prev.end = sw.end;
                if (speeds[sw.peak] > speeds[prev.peak]) prev.peak = sw.peak;
                swings[swings.Count - 1] = prev;
            }
            else
            {
                swings.Add(sw);
            }
        }
        return swings;
    }

    // divide o combo: Attack1 = 1º golpe (sem a preparação lenta), Attack2 = o resto
    static void SplitAttack(GameObject model, Animator animator)
    {
        var full = LoadClip(AttackClip);
        var times = new List<float>();
        var speeds = HandSpeeds(model, animator, full, HumanBodyBones.RightHand, times);
        var swings = FindSwings(speeds, times);

        var desc = new System.Text.StringBuilder();
        foreach (var s in swings) desc.Append($"[{times[s.start]:F2}-{times[s.end]:F2} pico {times[s.peak]:F2}] ");
        Debug.Log($"[PlayerRigSetup] combo {full.length:F2}s golpes: {desc}");

        // corte entre o 1º e o 2º golpe, no ponto mais lento
        int split = swings.Count > 1 ? swings[0].end : speeds.Count / 2;
        if (swings.Count > 1)
            for (int i = swings[0].end; i <= swings[1].start; i++)
                if (speeds[i] < speeds[split]) split = i;

        float start1 = Mathf.Max(0f, times[swings[0].start] - 0.15f);
        float splitTime = times[split];

        var imp = (ModelImporter)AssetImporter.GetAtPath(ClipPath(AttackClip));
        var baseClip = imp.defaultClipAnimations[0];
        float fps = full.frameRate;

        var part1 = new ModelImporterClipAnimation();
        var part2 = new ModelImporterClipAnimation();
        foreach (var (part, name, from, to) in new[] { (part1, "Attack1", start1, splitTime), (part2, "Attack2", splitTime, full.length) })
        {
            part.name = name;
            part.takeName = baseClip.takeName;
            part.firstFrame = baseClip.firstFrame + from * fps;
            part.lastFrame = baseClip.firstFrame + to * fps;
            ConfigureClip(part, false);
        }

        // eventos de cada parte, em tempo relativo ao início dela;
        // o sabre causa dano do início ao fim de cada parte
        float len1 = splitTime - start1, len2 = full.length - splitTime;
        var ev1 = new List<AnimationEvent> { Event("AttackStart", 0f), Event("AttackEnd", len1 * 0.99f) };
        var ev2 = new List<AnimationEvent> { Event("AttackStart", 0f), Event("AttackEnd", len2 * 0.99f) };
        foreach (var s in swings)
        {
            bool first = s.start <= split;
            float offset = first ? start1 : splitTime;
            var list = first ? ev1 : ev2;
            list.Add(Event("SwingSound", Mathf.Max(0f, times[s.peak] - SwingSoundLead - offset)));
        }
        ev1.Add(Event("ComboCheck", len1 * 0.8f));
        ev2.Add(Event("ActionEnd", len2 * 0.85f));
        ev1.Sort((a, b) => a.time.CompareTo(b.time));
        ev2.Sort((a, b) => a.time.CompareTo(b.time));

        part1.events = Normalized(ev1, len1);
        part2.events = Normalized(ev2, len2);
        imp.clipAnimations = new[] { part1, part2 };
        imp.SaveAndReimport();

        LogEvents("Attack1", len1, ev1);
        LogEvents("Attack2", len2, ev2);
    }

    // poder: corta a preparação parada do começo (começa pouco antes das mãos acelerarem)
    // e libera o poder no pico de velocidade das mãos
    static void CropPower(GameObject model, Animator animator)
    {
        var clip = LoadClip(PowerClip);
        var times = new List<float>();
        var right = HandSpeeds(model, animator, clip, HumanBodyBones.RightHand, times);
        var left = HandSpeeds(model, animator, clip, HumanBodyBones.LeftHand, times);
        int peak = 0;
        for (int i = 0; i < right.Count; i++)
            if (right[i] + left[i] > right[peak] + left[peak]) peak = i;

        float peakSpeed = right[peak] + left[peak];
        int rise = 0;
        while (rise < peak && right[rise] + left[rise] < peakSpeed * 0.3f) rise++;
        float start = Mathf.Max(0f, times[rise] - 0.05f);
        float length = clip.length - start;

        var events = new List<AnimationEvent>
        {
            Event("PowerRelease", times[peak] - start),
            Event("ActionEnd", length * 0.85f),
        };

        var imp = (ModelImporter)AssetImporter.GetAtPath(ClipPath(PowerClip));
        var baseClip = imp.defaultClipAnimations[0];
        var power = imp.clipAnimations[0];
        power.firstFrame = baseClip.firstFrame + start * clip.frameRate;
        power.lastFrame = baseClip.lastFrame;
        power.events = Normalized(events, length);
        imp.clipAnimations = new[] { power };
        imp.SaveAndReimport();
        Debug.Log($"[PlayerRigSetup] {PowerClip}: corta {start:F2}s do começo");
        LogEvents(PowerClip, length, events);
    }

    // aterrissagem: começa pouco antes do pé tocar o chão (a queda já é o Falling Idle)
    static void CropLanding(GameObject model, Animator animator)
    {
        var clip = LoadClip(LandClip);
        var l = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var r = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        var times = new List<float>();
        var heights = new List<float>();
        Sample(model, clip, 60f, t => { times.Add(t); heights.Add(Mathf.Min(l.position.y, r.position.y)); });

        float min = float.MaxValue, max = float.MinValue;
        foreach (var h in heights) { min = Mathf.Min(min, h); max = Mathf.Max(max, h); }
        int impact = heights.FindIndex(h => h <= min + (max - min) * 0.1f);
        float start = Mathf.Max(0f, times[impact] - 0.1f);

        var imp = (ModelImporter)AssetImporter.GetAtPath(ClipPath(LandClip));
        var baseClip = imp.defaultClipAnimations[0];
        var land = imp.clipAnimations[0];
        land.firstFrame = baseClip.firstFrame + start * clip.frameRate;
        land.lastFrame = baseClip.lastFrame;
        imp.clipAnimations = new[] { land };
        imp.SaveAndReimport();
        Debug.Log($"[PlayerRigSetup] {LandClip}: contato em {times[impact]:F2}s, recortado a partir de {start:F2}s de {clip.length:F2}s");
    }

    // ---------- máquina de estados ----------

    static AvatarMask BuildUpperBodyMask()
    {
        if (File.Exists(UpperMaskPath)) AssetDatabase.DeleteAsset(UpperMaskPath);
        var mask = new AvatarMask();
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            var part = (AvatarMaskBodyPart)i;
            bool upper = part == AvatarMaskBodyPart.Body || part == AvatarMaskBodyPart.Head ||
                         part == AvatarMaskBodyPart.LeftArm || part == AvatarMaskBodyPart.RightArm ||
                         part == AvatarMaskBodyPart.LeftFingers || part == AvatarMaskBodyPart.RightFingers ||
                         part == AvatarMaskBodyPart.LeftHandIK || part == AvatarMaskBodyPart.RightHandIK;
            mask.SetHumanoidBodyPartActive(part, upper);
        }
        AssetDatabase.CreateAsset(mask, UpperMaskPath);
        return mask;
    }

    static void BuildController()
    {
        var mask = BuildUpperBodyMask();

        if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
        var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter(new AnimatorControllerParameter { name = "Grounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
        c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Combo", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Power", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        c.AddParameter(new AnimatorControllerParameter { name = "PowerSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1.5f });
        // multiplicador da corrida: o player fica em 1; o boss acelera para os pés acompanharem o chão
        c.AddParameter(new AnimatorControllerParameter { name = "LocomotionSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });

        // camada 0: corpo inteiro
        var sm = c.layers[0].stateMachine;

        var tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
            hideFlags = HideFlags.HideInHierarchy,
        };
        tree.AddChild(LoadClip(IdleClip), 0f);
        tree.AddChild(LoadClip(WalkClip), 1f);
        tree.AddChild(LoadClip(RunClip), 2f);
        AssetDatabase.AddObjectToAsset(tree, c);

        // Foot IK: os pés seguem a posição/direção da animação original do Mixamo.
        // Sem ele, o retarget para este modelo virava o pé esquerdo ~30° na corrida
        // e dava um tranco lateral no pé direito no fim da passada (medido quadro a quadro)
        var locomotion = sm.AddState("Locomotion", new Vector3(300, 0));
        locomotion.motion = tree;
        locomotion.iKOnFeet = true;
        locomotion.speedParameter = "LocomotionSpeed";
        locomotion.speedParameterActive = true;
        sm.defaultState = locomotion;

        var falling = sm.AddState("Falling", new Vector3(300, 140));
        falling.motion = LoadClip(FallClip);
        // sem Foot IK o retarget deixava o pé direito esticado para baixo no ar
        // (~17° entre pé e canela, contra ~38° na animação original do Mixamo)
        falling.iKOnFeet = true;
        var landing = sm.AddState("Landing", new Vector3(560, 140));
        landing.motion = LoadClip(LandClip);
        landing.iKOnFeet = true;

        var toFall = locomotion.AddTransition(falling);
        toFall.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        toFall.hasExitTime = false;
        toFall.duration = 0.2f;

        var toLand = falling.AddTransition(landing);
        toLand.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        toLand.hasExitTime = false;
        toLand.duration = 0.05f;

        // se já está andando ao tocar o chão, volta logo a andar
        var landMoving = landing.AddTransition(locomotion);
        landMoving.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
        landMoving.hasExitTime = false;
        landMoving.duration = 0.2f;

        var landDone = landing.AddTransition(locomotion);
        landDone.hasExitTime = true;
        landDone.exitTime = 0.6f;
        landDone.duration = 0.25f;

        var death = sm.AddState("Death", new Vector3(560, -80));
        death.motion = LoadClip(DeathClip);
        var toDeath = sm.AddAnyStateTransition(death);
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        toDeath.canTransitionToSelf = false;
        toDeath.hasExitTime = false;
        toDeath.duration = 0.1f;

        // camada 1: tronco e braços (ataca andando sem travar as pernas)
        c.AddLayer("UpperBody");
        var layers = c.layers;
        layers[1].avatarMask = mask;
        layers[1].defaultWeight = 1f;
        layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
        c.layers = layers;

        var upper = c.layers[1].stateMachine;
        var empty = upper.AddState("Empty", new Vector3(300, 0));
        upper.defaultState = empty;

        var attack1 = upper.AddState("Attack1", new Vector3(300, 120));
        attack1.motion = LoadClip(AttackClip, "Attack1");
        attack1.speed = 1.15f;
        var attack2 = upper.AddState("Attack2", new Vector3(560, 120));
        attack2.motion = LoadClip(AttackClip, "Attack2");
        var power = upper.AddState("Power", new Vector3(560, 0));
        power.motion = LoadClip(PowerClip);
        // PowerSpeed: rápido até a mão chegar no alto, normal depois (PlayerAnimator)
        power.speed = 1f;
        power.speedParameter = "PowerSpeed";
        power.speedParameterActive = true;

        Trigger(empty, attack1, "Attack");
        Trigger(attack1, attack2, "Combo");
        Back(attack1, empty, 0.95f);
        Back(attack2, empty, 0.9f);
        Trigger(empty, power, "Power");
        Back(power, empty, 0.9f);
    }

    static void Trigger(AnimatorState from, AnimatorState to, string trigger)
    {
        var t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.hasExitTime = false;
        t.duration = 0.1f;
    }

    static void Back(AnimatorState from, AnimatorState to, float exitTime)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = exitTime;
        t.duration = 0.15f;
    }

    // ---------- prefab: animator, componentes, sons e sabre na mão ----------

    // sons curtos: PCM pré-carregado, sem atraso de decodificação no primeiro toque
    static AudioClip[] Sounds(params string[] names)
    {
        var list = new List<AudioClip>();
        foreach (var n in names)
        {
            string path = $"{CutSoundsDir}/{n}.wav";
            var imp = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = imp.defaultSampleSettings;
            if (settings.compressionFormat != AudioCompressionFormat.PCM ||
                settings.loadType != AudioClipLoadType.DecompressOnLoad ||
                !settings.preloadAudioData)
            {
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
                imp.defaultSampleSettings = settings;
                imp.SaveAndReimport();
            }
            list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
        }
        return list.ToArray();
    }

    // modelo: o pé (sola, na pose parada) fica exatamente na base da cápsula
    // (o inimigo mantém a escala própria do Visual, que compensa a escala do root)
    static void AlignVisualToCapsule(Transform root, Transform visual, bool playerScale)
    {
        if (playerScale) visual.localScale = Vector3.one * 2f;
        visual.localPosition = new Vector3(0f, visual.localPosition.y, 0f);

        AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(visual.gameObject, LoadClip(IdleClip), 0.3f);
        AnimationMode.EndSampling();

        float min = float.MaxValue;
        var baked = new Mesh();
        foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.BakeMesh(baked, true);
            var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
            foreach (var v in baked.vertices)
                min = Mathf.Min(min, root.InverseTransformPoint(m.MultiplyPoint3x4(v)).y);
        }
        Object.DestroyImmediate(baked);
        AnimationMode.StopAnimationMode();

        var capsule = root.GetComponent<CapsuleCollider>();
        float bottom = capsule.center.y - capsule.height * 0.5f;
        visual.localPosition += Vector3.up * (bottom - min);
        Debug.Log($"[PlayerRigSetup] {root.name}: Visual alinhado, y local = {visual.localPosition.y:F3} (sola estava {min - bottom:F3} da base da cápsula)");
    }

    // as cenas tinham posição/escala próprias para o Visual: passam a herdar do prefab;
    // e o player começa apoiado no chão (base da cápsula = chão), sem cair no início da fase
    static void RevertVisualOverrides()
    {
        foreach (var scenePath in new[] { "Assets/Scenes/Scene 1.unity", "Assets/Scenes/Scene 2.unity" })
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var player = Object.FindFirstObjectByType<PlayerMovement>();
            if (player == null) continue;

            var visual = player.transform.Find("Visual");
            if (visual != null && PrefabUtility.IsPartOfPrefabInstance(visual))
                PrefabUtility.RevertObjectOverride(visual, InteractionMode.AutomatedAction);

            PlaceOnGround(player.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    static void PlaceOnGround(Transform root)
    {
        Physics.SyncTransforms();
        var capsule = root.GetComponent<CapsuleCollider>();
        Vector3 center = root.TransformPoint(capsule.center);
        float bottom = root.TransformPoint(capsule.center - Vector3.up * capsule.height * 0.5f).y;

        float ground = float.NaN, best = float.MaxValue;
        foreach (var hit in Physics.RaycastAll(center, Vector3.down, 50f))
        {
            if (hit.collider.transform.IsChildOf(root) || hit.collider.isTrigger) continue;
            if (hit.distance < best) { best = hit.distance; ground = hit.point.y; }
        }
        if (float.IsNaN(ground)) return;

        float delta = ground + 0.01f - bottom;
        root.position += Vector3.up * delta;
        Debug.Log($"[PlayerRigSetup] {root.gameObject.scene.name}: player movido {delta:F3} na vertical (y = {root.position.y:F3})");
    }

    static void SetupPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        try
        {
            var visual = root.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(ModelPath);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var driver = visual.GetComponent<PlayerAnimator>();
            if (driver == null) driver = visual.gameObject.AddComponent<PlayerAnimator>();
            driver.animator = animator;
            driver.deathDuration = LoadClip(DeathClip).length;
            // pés agora vêm do Foot IK do Animator (direção original da animação);
            // a correção manual fica zerada, disponível no Inspector se precisar
            driver.leftFootYaw = 0f;
            driver.rightFootYaw = 0f;
            // exceção medida: na corrida o retarget deixa o pé esquerdo ~31° mais virado para fora
            // (em torno da canela) que na animação original do Mixamo, apoiado e no ar;
            // +12° para ficar mais reto que o original, que já vira o pé ~13° para fora
            driver.leftFootYawRun = 43f;
            driver.rightFootYawRun = 0f;

            // passos cortados no ápice, um conjunto por pé
            var movement = root.GetComponent<PlayerMovement>();
            movement.leftFootSounds = Sounds("step 1", "step 3");
            movement.rightFootSounds = Sounds("step 2", "step 4");

            // sons do sabre cortados para o ápice cair no pico do golpe
            var sword = root.GetComponentInChildren<Sword>(true);
            sword.attackSounds = Sounds("saber 1", "saber 2", "saber 3", "saber 4");

            // alvo da câmera (cena 2): estava em 1,7 (acima da cabeça com a escala 1,8); desce só no Y
            var cameraTarget = root.transform.Find("CameraTarget");
            cameraTarget.localPosition = new Vector3(cameraTarget.localPosition.x, 0.5f, cameraTarget.localPosition.z);

            AlignVisualToCapsule(root.transform, visual, true);
            AttachSabre(FindDeep(root.transform, "Sabre de Luz"), visual, animator);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        RevertVisualOverrides();
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static void AttachSabre(Transform sabre, Transform visual, Animator animator)
    {
        var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        // o sabre não tem mais animação própria: quem move é a mão
        foreach (var sabreAnimator in sabre.GetComponentsInChildren<Animator>(true))
            Object.DestroyImmediate(sabreAnimator);

        // suporte na mão; o sabre mantém seus valores locais (escala uniforme)
        var grip = hand.Find("SabreGrip");
        if (grip == null)
        {
            grip = new GameObject("SabreGrip").transform;
            grip.SetParent(hand, false);
        }
        grip.localPosition = Vector3.zero;
        grip.localRotation = Quaternion.identity;
        grip.localScale = Vector3.one;

        sabre.SetParent(grip, false);
        sabre.localPosition = Vector3.zero;
        sabre.localRotation = Quaternion.identity;
        sabre.localScale = Vector3.one;

        // geometria do sabre: eixo mais longo da malha; o cabo é a ponta mais próxima do pivô da malha
        // (o pivô fica no cabo nos dois sabres; o BoxCollider do sabre azul é deslocado para a lâmina)
        var mesh = sabre.GetComponentInChildren<MeshFilter>(true);
        var meshT = mesh.transform;
        Bounds mb = mesh.sharedMesh.bounds;
        int axis = mb.size.x >= mb.size.y && mb.size.x >= mb.size.z ? 0 : (mb.size.y >= mb.size.z ? 1 : 2);
        Vector3 axisLocal = Vector3.zero; axisLocal[axis] = 1f;
        Vector3 endA = mb.center - axisLocal * mb.extents[axis];
        Vector3 endB = mb.center + axisLocal * mb.extents[axis];

        bool aIsHilt = endA.sqrMagnitude < endB.sqrMagnitude;
        Vector3 a = meshT.TransformPoint(endA), b = meshT.TransformPoint(endB);
        Vector3 hiltEnd = aIsHilt ? a : b;
        Vector3 tipEnd = aIsHilt ? b : a;

        // mão: lâmina sai do lado do indicador; o cabo passa por dentro dos dedos fechados
        // (mais para os dedos e para a palma, longe da região tenar / entre polegar e indicador)
        Vector3 index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal).position;
        Vector3 little = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal).position;
        Vector3 middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position;
        Vector3 thumb = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal).position;
        Vector3 bladeDir = (index - little).normalized;
        Vector3 fingerDir = middle - hand.position;
        Vector3 palmDir = Vector3.Cross(bladeDir, fingerDir).normalized;
        if (Vector3.Dot(palmDir, thumb - hand.position) < 0f) palmDir = -palmDir;
        Vector3 gripPoint = hand.position + fingerDir * 0.9f + palmDir * fingerDir.magnitude * 0.2f;

        // gira, escala e posiciona o suporte
        grip.rotation = Quaternion.FromToRotation((tipEnd - hiltEnd).normalized, bladeDir) * grip.rotation;

        float characterHeight = 0f;
        foreach (var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            characterHeight = Mathf.Max(characterHeight, r.bounds.size.y);
        float currentLength = (meshT.TransformPoint(endB) - meshT.TransformPoint(endA)).magnitude;
        grip.localScale *= characterHeight * SabreToHeight / currentLength;

        hiltEnd = meshT.TransformPoint(aIsHilt ? endA : endB);
        tipEnd = meshT.TransformPoint(aIsHilt ? endB : endA);
        Vector3 sabreGrip = hiltEnd + (tipEnd - hiltEnd) * 0.11f;
        grip.position += gripPoint - sabreGrip;

        Debug.Log($"[PlayerRigSetup] {sabre.name}: altura personagem {characterHeight:F2}, comprimento {(tipEnd - hiltEnd).magnitude:F2}");
    }

    // ---------- fontes ----------

    const string TechnoTtf = "Assets/Fontes/Techno.ttf";
    const string TechnoAsset = "Assets/Fontes/Techno SDF.asset";
    const string Star7Asset = "Assets/Fontes/STAR7 SDF.asset";

    // botões e títulos: STAR7 | textos longos e rótulos discretos: Techno
    // (Techno só tem letras; números, pontuação e acentos vêm da STAR7 como fallback)
    [MenuItem("Tools/Fonts/Apply Fonts")]
    public static void ApplyFonts()
    {
        foreach (var path in new[] { "Assets/Fontes/Orbitron SDF.asset", "Assets/Fontes/Orbitron.ttf" })
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        var star7 = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Star7Asset);
        var techno = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TechnoAsset);
        if (techno == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(TechnoTtf);
            techno = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            AssetDatabase.CreateAsset(techno, TechnoAsset);
            techno.atlasTextures[0].name = "Techno Atlas";
            AssetDatabase.AddObjectToAsset(techno.atlasTextures[0], techno);
            techno.material.name = "Techno Material";
            AssetDatabase.AddObjectToAsset(techno.material, techno);
        }
        techno.fallbackFontAssetTable = new List<TMP_FontAsset> { star7 };
        EditorUtility.SetDirty(techno);
        AssetDatabase.SaveAssets();

        foreach (var scenePath in new[]
        {
            "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Scene 1.unity", "Assets/Scenes/Scene 2.unity",
            "Assets/Scenes/DefeatScene.unity", "Assets/Scenes/VictoryScene.unity",
        })
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int toStar7 = 0, toTechno = 0;
            foreach (var go in scene.GetRootGameObjects())
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            {
                bool isButton = t.GetComponentInParent<Button>(true) != null;
                var target = isButton ? star7 : techno;
                if (t.font != target)
                {
                    t.font = target;
                    EditorUtility.SetDirty(t);
                }
                if (isButton) toStar7++; else toTechno++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ApplyFonts] {scenePath}: STAR7={toStar7} Techno={toTechno}");
        }
    }
}
