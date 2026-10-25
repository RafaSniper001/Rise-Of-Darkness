using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Monta os robôs animados da cena 1 (branco, vermelho e preto): máquina de estados,
// arma segura com as duas mãos e tiro sincronizado. Velocidade, vida, tiro etc. de cada um
// ficam como estão; o tamanho fica igual ao do player.
// Menu: Tools > Characters > Setup Robots
public static class RobotRigSetup
{
    const string RobotDir = "Assets/Characters/Robot";
    const string ModelPath = RobotDir + "/Robot.fbx";
    const string ControllerPath = RobotDir + "/Robot.controller";
    const string UpperMaskPath = "Assets/Characters/Mage/UpperBody.mask";
    const string PlayerScene = "Assets/Scenes/Scene 1.unity";

    const string IdleClip = "Rifle Idle";
    const string WalkClip = "Rifle Walk";
    const string RunClip = "Rifle Run";
    const string FireClip = "Firing Rifle";
    // sub-clipes do Firing Rifle: pose de mira parada (antes do recuo) e o disparo (do recuo em diante)
    const string AimName = "Aim";
    const string ShootName = "Shoot";

    // posição "pronta" (sem mirar): a arma gira em volta da coronha, cano para baixo e para dentro
    const float ReadyPitch = 25f;
    const float ReadyYaw = 35f;

    static readonly string[] RobotPrefabs =
    {
        "Assets/Objects/Enemy.prefab",   // branco (fase 1)
        "Assets/Objects/Enemy 1.prefab", // vermelho (fase 2)
        "Assets/Objects/Enemy 2.prefab", // preto (fase 3)
    };

    [MenuItem("Tools/Characters/Setup Robots")]
    public static void Run()
    {
        float playerHeight = PlayerHeight();
        ImportClips();
        var (walkSpeed, runSpeed) = MeasureGait(playerHeight);
        BuildController(walkSpeed, runSpeed);
        foreach (var path in RobotPrefabs)
            SetupPrefab(path, runSpeed, playerHeight);
        AssetDatabase.SaveAssets();
        Debug.Log("[RobotRigSetup] Concluído.");
    }

    static string ClipPath(string file) => $"{RobotDir}/Animations/{file}.fbx";

    static AnimationClip LoadClip(string file, string clipName = null)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ClipPath(file)))
            if (o is AnimationClip c && !c.name.StartsWith("__preview__") && (clipName == null || c.name == clipName))
                return c;
        throw new FileNotFoundException($"Clip não encontrado: {file} {clipName}");
    }

    // ---------- tamanho ----------

    static float ModelHeight(Transform visual)
    {
        float height = 0f;
        foreach (var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            height = Mathf.Max(height, r.bounds.size.y);
        return height;
    }

    // altura do player na cena 1 (os robôs ficam do mesmo tamanho)
    static float PlayerHeight()
    {
        EditorSceneManager.OpenScene(PlayerScene, OpenSceneMode.Single);
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        float height = ModelHeight(player.transform.Find("Visual"));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log($"[RobotRigSetup] altura do player na cena 1: {height:F2}");
        return height;
    }

    // ---------- clipes ----------

    static void ImportClips()
    {
        foreach (var file in new[] { IdleClip, WalkClip, RunClip, FireClip })
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(ClipPath(file));
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.importAnimation = true;

            var clips = imp.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = file;
                clip.loopTime = file != FireClip;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = true;
                clip.lockRootPositionXZ = false;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        CropFire();
    }

    // tiro: divide o Firing Rifle em "Aim" (pose de mira um pouco antes do recuo, parada)
    // e "Shoot" (começa no recuo da arma, no mesmo instante em que o Chase dispara)
    static void CropFire()
    {
        var model = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        try
        {
            var animator = model.GetComponent<Animator>();
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var clip = LoadClip(FireClip, FireClip);

            const float fps = 60f;
            int frames = Mathf.RoundToInt(clip.length * fps);
            var positions = new List<Vector3>();
            AnimationMode.StartAnimationMode();
            for (int f = 0; f <= frames; f++)
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(model, clip, clip.length * f / frames);
                AnimationMode.EndSampling();
                positions.Add(hand.position - model.transform.position);
            }
            AnimationMode.StopAnimationMode();

            // recuo = maior velocidade da mão (o tranco do disparo)
            int peak = 1;
            for (int f = 1; f < frames; f++)
                if ((positions[f] - positions[f - 1]).sqrMagnitude > (positions[peak] - positions[peak - 1]).sqrMagnitude)
                    peak = f;

            float recoil = (peak - 1) / fps;
            float start = Mathf.Max(0f, recoil - 0.03f);
            float aimStart = Mathf.Max(0f, start - 0.1f);
            var imp = (ModelImporter)AssetImporter.GetAtPath(ClipPath(FireClip));
            var baseClip = imp.defaultClipAnimations[0];
            float fullLength = clip.length;
            float frameRate = clip.frameRate;
            ModelImporterClipAnimation Part(string name, float from, float to, bool loop) => new ModelImporterClipAnimation
            {
                name = name,
                takeName = baseClip.takeName,
                firstFrame = baseClip.firstFrame + from * frameRate,
                lastFrame = baseClip.firstFrame + to * frameRate,
                loopTime = loop,
                lockRootRotation = true,
                lockRootHeightY = true,
                keepOriginalPositionY = true,
                lockRootPositionXZ = false,
            };

            imp.clipAnimations = new[]
            {
                Part(AimName, aimStart, aimStart + 0.08f, true),
                Part(ShootName, start, fullLength, false),
            };
            imp.SaveAndReimport();
            Debug.Log($"[RobotRigSetup] {FireClip}: recuo em {recoil:F2}s; mira de {aimStart:F2}s, disparo de {start:F2}s a {fullLength:F2}s");
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(model);
        }
    }

    // velocidade em que andar e correr acompanham os pés no robô, já no tamanho do player
    // (todos os robôs têm a mesma escala)
    static (float walk, float run) MeasureGait(float playerHeight)
    {
        var root = PrefabUtility.LoadPrefabContents(RobotPrefabs[0]);
        try
        {
            var visual = root.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();
            float scale = playerHeight / ModelHeight(visual);
            float walk = GaitMeasure.GroundSpeed(visual.gameObject, animator, LoadClip(WalkClip)) * scale;
            float run = GaitMeasure.GroundSpeed(visual.gameObject, animator, LoadClip(RunClip)) * scale;
            Debug.Log($"[RobotRigSetup] pés acompanham o chão: andar {walk:F2} m/s, correr {run:F2} m/s");
            return (walk, run);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------- máquina de estados ----------

    static void BuildController(float walkSpeed, float runSpeed)
    {
        if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
        var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        // Speed: velocidade do NavMeshAgent (negativa recuando de costas)
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter(new AnimatorControllerParameter { name = "MoveSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        c.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        // player ao alcance de tiro: mantém a arma apontada
        c.AddParameter("Aiming", AnimatorControllerParameterType.Bool);

        var layers = c.layers;
        layers[0].iKPass = true; // tronco mira o player e mãos na arma (EnemyAnimator.OnAnimatorIK)
        c.layers = layers;

        var sm = c.layers[0].stateMachine;
        var tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
            hideFlags = HideFlags.HideInHierarchy,
        };
        var walk = LoadClip(WalkClip);
        var run = LoadClip(RunClip);
        tree.AddChild(run, -runSpeed);
        tree.AddChild(walk, -walkSpeed);
        tree.AddChild(LoadClip(IdleClip), 0f);
        tree.AddChild(walk, walkSpeed);
        tree.AddChild(run, runSpeed);
        // de costas: as mesmas animações tocadas ao contrário
        var children = tree.children;
        children[0].timeScale = -1f;
        children[1].timeScale = -1f;
        tree.children = children;
        AssetDatabase.AddObjectToAsset(tree, c);

        var locomotion = sm.AddState("Locomotion", new Vector3(300, 0));
        locomotion.motion = tree;
        locomotion.speedParameter = "MoveSpeed";
        locomotion.speedParameterActive = true;
        sm.defaultState = locomotion;

        // camada 1: tronco e braços atiram sem travar as pernas
        c.AddLayer("UpperBody");
        layers = c.layers;
        layers[1].avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperMaskPath);
        layers[1].defaultWeight = 1f;
        layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
        layers[1].iKPass = true;
        c.layers = layers;

        // Empty -> Aim: com o player ao alcance de tiro, levanta a arma devagar e mantém a mira;
        // cada disparo toca só o recuo (Shoot) e volta para a mira
        var upper = c.layers[1].stateMachine;
        var empty = upper.AddState("Empty", new Vector3(300, 0));
        upper.defaultState = empty;
        var aim = upper.AddState("Aim", new Vector3(560, 0));
        aim.motion = LoadClip(FireClip, AimName);
        var shoot = upper.AddState("Shoot", new Vector3(560, 140));
        shoot.motion = LoadClip(FireClip, ShootName);

        var raise = empty.AddTransition(aim);
        raise.AddCondition(AnimatorConditionMode.If, 0, "Aiming");
        raise.hasExitTime = false;
        raise.duration = 0.3f;

        var lower = aim.AddTransition(empty);
        lower.AddCondition(AnimatorConditionMode.IfNot, 0, "Aiming");
        lower.hasExitTime = false;
        lower.duration = 0.4f;

        // cada disparo recomeça o recuo (o preto atira a cada 0,5 s)
        var toShoot = upper.AddAnyStateTransition(shoot);
        toShoot.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
        toShoot.canTransitionToSelf = true;
        toShoot.hasExitTime = false;
        toShoot.duration = 0.1f;

        var backToAim = shoot.AddTransition(aim);
        backToAim.AddCondition(AnimatorConditionMode.If, 0, "Aiming");
        backToAim.hasExitTime = true;
        backToAim.exitTime = 0.6f;
        backToAim.duration = 0.3f;

        var backToEmpty = shoot.AddTransition(empty);
        backToEmpty.AddCondition(AnimatorConditionMode.IfNot, 0, "Aiming");
        backToEmpty.hasExitTime = true;
        backToEmpty.exitTime = 0.85f;
        backToEmpty.duration = 0.3f;
    }

    // ---------- prefabs ----------

    static void SetupPrefab(string path, float runSpeed, float playerHeight)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual = root.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(ModelPath);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // mesmo tamanho do player (corpo e cápsula juntos)
            float scale = playerHeight / ModelHeight(visual);
            if (Mathf.Abs(scale - 1f) > 0.005f)
            {
                root.transform.localScale *= scale;
                Debug.Log($"[RobotRigSetup] {root.name}: escala x{scale:F3} para ficar da altura do player (escala {root.transform.localScale})");
            }

            var driver = visual.GetComponent<EnemyAnimator>();
            if (driver == null) driver = visual.gameObject.AddComponent<EnemyAnimator>();
            driver.animator = animator;
            driver.runAnimationSpeed = runSpeed;

            // a altura do Visual muda fora do modo de amostragem (senão ela é desfeita ao sair dele)
            AnimationMode.StartAnimationMode();
            float lift;
            try
            {
                Pose(visual.gameObject, LoadClip(IdleClip));
                lift = SoleToCapsule(root.transform, visual);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
            visual.localPosition = new Vector3(0f, visual.localPosition.y + lift, 0f);
            Debug.Log($"[RobotRigSetup] {root.name}: Visual alinhado, y local = {visual.localPosition.y:F3} (sola estava {-lift:F3} da base da cápsula)");

            AttachGun(root.transform, animator, driver);

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void Pose(GameObject visual, AnimationClip clip)
    {
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(visual, clip, 0f);
        AnimationMode.EndSampling();
    }

    // quanto subir o Visual para a sola do pé (parado) ficar na base da cápsula,
    // que o NavMeshAgent apoia no chão
    static float SoleToCapsule(Transform root, Transform visual)
    {
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

        var capsule = root.GetComponent<CapsuleCollider>();
        float bottom = capsule.center.y - capsule.height * 0.5f;
        return bottom - min;
    }

    // pose de mira da animação, no espaço do Visual (o corpo do robô): frente/alto, ombros
    // e cotovelos. Roda numa cópia temporária do robô, numa cena
    // (o Animator só anima numa cena; mesmo esqueleto, os valores locais servem para o original)
    struct AimPose
    {
        public Vector3 forward, up, leftShoulder, rightShoulder, leftElbow, rightElbow;
    }

    static AimPose MeasureAimPose(Transform root)
    {
        var copy = Object.Instantiate(root.gameObject);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, EditorSceneManager.GetActiveScene());
        try
        {
            var visual = copy.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();
            // sem câmera na medição: o "cull" fora da tela pararia a animação
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.SetFloat("Speed", 0f);
            animator.SetBool("Aiming", true);
            animator.Update(0f);
            animator.Update(1f);

            Vector3 Local(HumanBodyBones bone) => visual.InverseTransformPoint(animator.GetBoneTransform(bone).position);
            return new AimPose
            {
                forward = Vector3.forward,
                up = Vector3.up,
                leftShoulder = Local(HumanBodyBones.LeftUpperArm),
                rightShoulder = Local(HumanBodyBones.RightUpperArm),
                leftElbow = Local(HumanBodyBones.LeftLowerArm),
                rightElbow = Local(HumanBodyBones.RightLowerArm),
            };
        }
        finally
        {
            Object.DestroyImmediate(copy);
        }
    }

    // a arma fica presa ao corpo, com a coronha no ombro esquerdo: mirando aponta para a frente,
    // pronta aponta para baixo e para dentro. As mãos vão até ela por IK (esquerda no gatilho,
    // direita no cano). O tiro sai da ponta do cano (Muzzle) mirando no mesmo ponto de antes
    // (FirePoint no corpo, onde ficava a arma)
    static void AttachGun(Transform root, Animator animator, EnemyAnimator driver)
    {
        var chase = root.GetComponent<Chase>();
        var gun = FindDeep(root, "BLASTER");

        // parte sempre da posição original da arma no corpo (como estava no prefab),
        // para o setup poder ser rodado de novo sem acumular giros
        gun.SetParent(root, false);
        gun.localPosition = new Vector3(-0.7f, 0.17666659f, 0.81f);
        gun.localRotation = new Quaternion(0.4867402f, -0.4867402f, -0.5129171f, 0.5129171f);
        gun.localScale = new Vector3(0.16666673f, 0.3f, 0.30000004f);

        var oldFirePoint = FindDeep(gun, "FirePoint");
        if (chase.firePoint == null || chase.firePoint.IsChildOf(gun))
        {
            var firePoint = root.Find("FirePoint");
            if (firePoint == null)
            {
                firePoint = new GameObject("FirePoint").transform;
                firePoint.SetParent(root, false);
            }
            firePoint.SetPositionAndRotation(chase.firePoint.position, chase.firePoint.rotation);
            chase.firePoint = firePoint;
        }

        // restos das versões anteriores (pontos de apoio presos às mãos)
        foreach (var old in new[] { "LeftHandGrip", "SupportHandGrip" })
        {
            var t = FindDeep(root, old);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // tamanho real da arma em cada eixo dela, como aparecia no corpo do robô.
        // O corpo tem escala (1; 1,8; 1): esse esticão vertical faz parte do visual da arma
        // e precisa ser levado para o eixo certo da arma ao mudar de pai
        Matrix4x4 original = gun.localToWorldMatrix;
        Vector3 worldSize = new Vector3(
            ((Vector3)original.GetColumn(0)).magnitude,
            ((Vector3)original.GetColumn(1)).magnitude,
            ((Vector3)original.GetColumn(2)).magnitude);
        Vector3 rootUpLocal = gun.InverseTransformDirection(root.up);

        // a arma fica presa ao corpo do robô (Visual), que não gira com as animações:
        // as posições de mira e pronta ficam sempre iguais; as mãos vão até ela por IK
        var body = animator.transform;
        gun.SetParent(body, false);
        gun.localScale = worldSize / body.lossyScale.x;

        // cantos de cada peça da arma, no espaço da arma
        var parts = new List<Vector3[]>();
        var corners = new List<Vector3>();
        foreach (var r in gun.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var b = mf.sharedMesh.bounds;
            var part = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var p = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                part[i] = gun.InverseTransformPoint(r.transform.TransformPoint(p));
            }
            parts.Add(part);
            corners.AddRange(part);
        }

        // cano = eixo mais comprido da arma; "para cima" = o outro eixo que estava mais perto do alto do robô
        Vector3 cMin = corners[0], cMax = corners[0];
        foreach (var p in corners) { cMin = Vector3.Min(cMin, p); cMax = Vector3.Max(cMax, p); }
        Vector3 extent = Vector3.Scale(cMax - cMin, worldSize);
        int barrelAxis = extent.x >= extent.y && extent.x >= extent.z ? 0 : (extent.y >= extent.z ? 1 : 2);
        int upAxis = -1;
        for (int i = 0; i < 3; i++)
            if (i != barrelAxis && (upAxis < 0 || Mathf.Abs(rootUpLocal[i]) > Mathf.Abs(rootUpLocal[upAxis])))
                upAxis = i;
        Vector3 upLocal = Vector3.zero;
        upLocal[upAxis] = Mathf.Sign(rootUpLocal[upAxis]);

        // a ponta do cano é o lado do FirePoint original
        float gunMidB = (cMin[barrelAxis] + cMax[barrelAxis]) * 0.5f;
        Vector3 barrelLocal = Vector3.zero;
        barrelLocal[barrelAxis] = Mathf.Sign(gun.InverseTransformPoint(oldFirePoint.position)[barrelAxis] - gunMidB);

        Vector3 sideLocal = Vector3.Cross(upLocal, barrelLocal);
        float bMin = float.MaxValue, bMax = float.MinValue, uMin = float.MaxValue, uMax = float.MinValue, sMin = float.MaxValue, sMax = float.MinValue;
        foreach (var p in corners)
        {
            float b = Vector3.Dot(p, barrelLocal), u = Vector3.Dot(p, upLocal), s = Vector3.Dot(p, sideLocal);
            bMin = Mathf.Min(bMin, b); bMax = Mathf.Max(bMax, b);
            uMin = Mathf.Min(uMin, u); uMax = Mathf.Max(uMax, u);
            sMin = Mathf.Min(sMin, s); sMax = Mathf.Max(sMax, s);
        }
        Vector3 GunPoint(float along, float height) =>
            barrelLocal * (bMin + (bMax - bMin) * along) +
            upLocal * (uMin + (uMax - uMin) * height) +
            sideLocal * ((sMin + sMax) * 0.5f);

        // ponta do cano: centro das peças que chegam na frente da arma
        Vector3 tipSum = Vector3.zero;
        int tipCount = 0;
        foreach (var p in corners)
            if (Vector3.Dot(p, barrelLocal) > bMax - (bMax - bMin) * 0.03f) { tipSum += p; tipCount++; }
        Vector3 tipLocal = tipSum / tipCount;
        tipLocal += barrelLocal * (bMax - Vector3.Dot(tipLocal, barrelLocal));

        // pose de mira da animação, levada para o peito deste prefab
        var pose = MeasureAimPose(root);
        Vector3 forward = body.TransformDirection(pose.forward).normalized;
        Vector3 up = Vector3.ProjectOnPlane(body.TransformDirection(pose.up), forward).normalized;
        Vector3 right = Vector3.Cross(up, forward);
        Vector3 leftShoulder = body.TransformPoint(pose.leftShoulder);
        Vector3 rightShoulder = body.TransformPoint(pose.rightShoulder);
        float shoulders = (rightShoulder - leftShoulder).magnitude;

        // mirando: coronha encostada na frente do ombro esquerdo, cano para a frente
        Vector3 stock = leftShoulder + forward * shoulders * 0.25f + (rightShoulder - leftShoulder) * 0.15f - up * shoulders * 0.05f;
        gun.rotation = Quaternion.LookRotation(forward, up) * Quaternion.Inverse(Quaternion.LookRotation(barrelLocal, upLocal));
        gun.position += stock - gun.TransformPoint(GunPoint(0f, 0.6f));
        Vector3 aimPosition = gun.localPosition;
        Quaternion aimRotation = gun.localRotation;

        // mãos (na pose de mira). O IK leva o PULSO até o ponto: o ponto fica recuado
        // para a PALMA encostar na arma, com a mão girada para segurar:
        //  - esquerda no gatilho: do lado de fora da empunhadura, palma virada para a arma,
        //    mão apontando para a frente (dedos fecham em volta da empunhadura)
        //  - direita no cano: por baixo, palma para cima, dedos envolvendo o cano
        Vector3 gunForward = gun.TransformDirection(barrelLocal).normalized;
        Vector3 gunUp = gun.TransformDirection(upLocal).normalized;
        Vector3 gunRight = Vector3.Cross(gunUp, gunForward);

        // perfil da arma num ponto do comprimento: só as peças que passam por ali
        (float uLo, float uHi, float sLo, float sHi) Section(float b0)
        {
            float lo = float.MaxValue, hi = float.MinValue, sl = float.MaxValue, sh = float.MinValue;
            foreach (var part in parts)
            {
                float pbMin = float.MaxValue, pbMax = float.MinValue;
                foreach (var p in part) { float b = Vector3.Dot(p, barrelLocal); pbMin = Mathf.Min(pbMin, b); pbMax = Mathf.Max(pbMax, b); }
                if (b0 < pbMin || b0 > pbMax) continue;
                foreach (var p in part)
                {
                    float u = Vector3.Dot(p, upLocal), s = Vector3.Dot(p, sideLocal);
                    lo = Mathf.Min(lo, u); hi = Mathf.Max(hi, u); sl = Mathf.Min(sl, s); sh = Mathf.Max(sh, s);
                }
            }
            return lo <= hi ? (lo, hi, sl, sh) : (uMin, uMax, sMin, sMax);
        }
        Vector3 Local(float b, float u, float s) => barrelLocal * b + upLocal * u + sideLocal * s;

        // punho: os vértices que descem abaixo da metade da altura da arma, na peça que vai
        // mais para baixo (o corpo da arma com o punho). O guarda-mato e o gatilho são outras peças
        float gunLen = bMax - bMin, gunHeight = uMax - uMin;
        MeshFilter lowestPart = null;
        float lowest = float.MaxValue;
        foreach (var mf in gun.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            foreach (var v in mf.sharedMesh.vertices)
            {
                float u = Vector3.Dot(gun.InverseTransformPoint(mf.transform.TransformPoint(v)), upLocal);
                if (u < lowest) { lowest = u; lowestPart = mf; }
            }
        }
        var gripVerts = new List<Vector3>();
        foreach (var v in lowestPart.sharedMesh.vertices)
        {
            var p = gun.InverseTransformPoint(lowestPart.transform.TransformPoint(v));
            if (Vector3.Dot(p, upLocal) < uMin + gunHeight * 0.5f && Vector3.Dot(p, barrelLocal) < bMin + gunLen * 0.5f)
                gripVerts.Add(p);
        }
        float gripBottom = float.MaxValue, gripTop = float.MinValue;
        foreach (var p in gripVerts) { float u = Vector3.Dot(p, upLocal); gripBottom = Mathf.Min(gripBottom, u); gripTop = Mathf.Max(gripTop, u); }

        // mão esquerda na parte de cima do punho (o indicador alcança o gatilho): centro do punho
        // naquela altura e a lateral de fora (esquerda) dele
        float palmU = gripTop - (gripTop - gripBottom) * 0.35f;
        float bandB = 0f, bandSide = float.MaxValue, bandBack = float.MaxValue, bandFar = float.MinValue;
        int bandCount = 0;
        foreach (var p in gripVerts)
        {
            if (Mathf.Abs(Vector3.Dot(p, upLocal) - palmU) > (gripTop - gripBottom) * 0.15f) continue;
            bandB += Vector3.Dot(p, barrelLocal);
            bandSide = Mathf.Min(bandSide, Vector3.Dot(p, sideLocal));
            bandBack = Mathf.Min(bandBack, Vector3.Dot(p, barrelLocal));
            bandFar = Mathf.Max(bandFar, Vector3.Dot(p, sideLocal));
            bandCount++;
        }
        bandB /= Mathf.Max(1, bandCount);
        Vector3 triggerPoint = Local(bandB, palmU, bandSide);
        // polegar: contorna a parte de trás do punho (lado da coronha) até o outro lado
        Vector3 thumbPoint = Local(bandBack, palmU + (gripTop - gripBottom) * 0.15f, bandFar);
        Debug.Log($"[RobotRigSetup] {root.name}: punho em {lowestPart.name}, a {(bandB - bMin) / gunLen * 100f:F0}% do comprimento a partir da coronha, de {(gripBottom - uMin) / gunHeight * 100f:F0}% a {(gripTop - uMin) / gunHeight * 100f:F0}% da altura");

        // mão direita embaixo do trilho/cano, um pouco à frente do meio
        float supportB = bMin + gunLen * 0.5f;
        var supportSection = Section(supportB);
        Vector3 supportPoint = Local(supportB, supportSection.uLo, (supportSection.sLo + supportSection.sHi) * 0.5f);

        // Giro de cada mão pela anatomia dela (vale em qualquer pose):
        //   eixo dos dedos = pulso -> nó do dedo médio; eixo de través = mindinho -> indicador.
        //   A palma fica de um lado fixo desses dois eixos (diferente para mão esquerda e direita)
        // fingers/across: para onde esses eixos devem apontar segurando a arma
        Transform Grip(string name, bool left, Vector3 surfaceLocal, Vector3 fingers, Vector3 across)
        {
            var grip = FindDeep(gun, name);
            if (grip == null)
            {
                grip = new GameObject(name).transform;
                grip.SetParent(gun, false);
            }
            grip.localScale = Vector3.one;

            var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            Vector3 middle = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal).position;
            Vector3 index = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal).position;
            Vector3 little = animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal).position;
            Vector3 fingersLocal = hand.InverseTransformDirection(middle - hand.position).normalized;
            Vector3 acrossLocal = Vector3.ProjectOnPlane(hand.InverseTransformDirection(index - little), fingersLocal).normalized;
            float handLength = (middle - hand.position).magnitude;

            fingers = fingers.normalized;
            Quaternion rotation =
                Quaternion.LookRotation(fingers, Vector3.ProjectOnPlane(across, fingers)) *
                Quaternion.Inverse(Quaternion.LookRotation(fingersLocal, acrossLocal));

            // lado da palma: mão direita = dedos x través, mão esquerda = través x dedos
            Vector3 palmLocal = left ? Vector3.Cross(acrossLocal, fingersLocal) : Vector3.Cross(fingersLocal, acrossLocal);
            Vector3 palm = (rotation * palmLocal).normalized;

            // a palma encosta na superfície: o osso da mão fica um pouco para fora dela,
            // e o pulso meio palmo atrás do centro da palma
            Vector3 palmCenter = gun.TransformPoint(surfaceLocal) - palm * handLength * 0.12f;
            grip.rotation = rotation;
            grip.position = palmCenter - fingers * handLength * 0.5f;
            return grip;
        }

        // pulso baixo e mão inclinada para cima: os nós dos dedos ficam logo abaixo do guarda-mato,
        // o indicador sobe separado até o gatilho e os outros dedos fecham no punho
        driver.leftHandGrip = Grip("LeftHandGrip", true, triggerPoint, gunForward + gunUp * 0.4f, gunUp - gunForward * 0.4f);
        // direita embaixo do trilho: dedos atravessando por baixo, indicador para a frente, palma para cima
        driver.rightHandGrip = Grip("RightHandGrip", false, supportPoint, -gunRight + gunForward * 0.3f, gunForward);

        // dedos com pose própria: cada falange dobra na direção da palma (anatomia, na pose base)
        //  - mão direita inteira fechada em volta do cano (nas animações deste robô os dedos
        //    dobram para as costas da mão)
        //  - indicador esquerdo quase esticado, para a frente, no gatilho
        {
            var fingers = new List<Transform>();
            var rotations = new List<Quaternion>();
            void Curl(bool left, HumanBodyBones first, HumanBodyBones last, System.Func<string, float> curlOf)
            {
                var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                Vector3 f = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal).position - hand.position;
                Vector3 a = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal).position -
                            animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal).position;
                Vector3 palmBind = (left ? Vector3.Cross(a, f) : Vector3.Cross(f, a)).normalized;
                for (var bone = first; bone <= last; bone++)
                {
                    var t = animator.GetBoneTransform(bone);
                    if (t == null) continue;
                    Vector3 along = t.childCount > 0 ? t.GetChild(0).position - t.position : t.position - t.parent.position;
                    Vector3 axis = Vector3.Cross(along.normalized, palmBind);
                    if (axis.sqrMagnitude < 1e-4f) continue;
                    fingers.Add(t);
                    rotations.Add(t.localRotation * Quaternion.AngleAxis(curlOf(bone.ToString()), Quaternion.Inverse(t.rotation) * axis.normalized));
                }
            }
            Curl(false, HumanBodyBones.RightThumbProximal, HumanBodyBones.RightLittleDistal, n => n.Contains("Thumb")
                ? (n.EndsWith("Proximal") ? 25f : 20f)
                : (n.EndsWith("Proximal") ? 50f : n.EndsWith("Intermediate") ? 65f : 45f));
            // médio, anelar e mindinho esquerdos bem fechados, abaixo do guarda-mato
            Curl(true, HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftLittleDistal, n =>
                n.EndsWith("Proximal") ? 85f : n.EndsWith("Intermediate") ? 85f : 55f);
            // base de um dedo esquerdo apontada para um ponto da arma (filha direta da mão),
            // com a mão já na pegada: o osso da mão vai para leftHandGrip
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            bool Aim(HumanBodyBones bone, Vector3 targetWorld)
            {
                var t = animator.GetBoneTransform(bone);
                if (t == null || t.childCount == 0) return false;
                Quaternion handToGrip = driver.leftHandGrip.rotation * Quaternion.Inverse(leftHand.rotation);
                Vector3 pos = driver.leftHandGrip.position + handToGrip * (t.position - leftHand.position);
                Vector3 current = handToGrip * (t.GetChild(0).position - t.position);
                Quaternion aimed = Quaternion.FromToRotation(current, targetWorld - pos) * (handToGrip * t.rotation);
                fingers.Add(t);
                rotations.Add(Quaternion.Inverse(driver.leftHandGrip.rotation) * aimed);
                return true;
            }

            // polegar esquerdo aberto, contornando a parte de trás do punho até o outro lado
            if (Aim(HumanBodyBones.LeftThumbProximal, gun.TransformPoint(thumbPoint)))
                Curl(true, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal, n =>
                    n.EndsWith("Intermediate") ? 25f : 30f);

            // indicador esquerdo: a base aponta para o gatilho (a peça pequena "Cubo.001" logo atrás
            // do guarda-mato), as outras falanges dobram em volta dele
            var trigger = FindDeep(gun, "Cubo.001");
            if (trigger != null && Aim(HumanBodyBones.LeftIndexProximal, trigger.GetComponent<Renderer>().bounds.center))
                Curl(true, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal, n =>
                    n.EndsWith("Intermediate") ? 25f : 20f);
            driver.fingers = fingers.ToArray();
            driver.fingerRotations = rotations.ToArray();
        }

        // confere o lado da palma pela anatomia: a base do polegar fica do lado da palma
        float ThumbOnPalmSide(bool left)
        {
            var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            Vector3 f = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal).position - hand.position;
            Vector3 a = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal).position -
                        animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal).position;
            Vector3 palm = (left ? Vector3.Cross(a, f) : Vector3.Cross(f, a)).normalized;
            Vector3 thumbTip = animator.GetBoneTransform(left ? HumanBodyBones.LeftThumbDistal : HumanBodyBones.RightThumbDistal).position;
            return Vector3.Dot((thumbTip - hand.position).normalized, palm);
        }
        Debug.Log($"[RobotRigSetup] {root.name}: ponta do polegar para o lado da palma: esquerda {ThumbOnPalmSide(true):F2}, direita {ThumbOnPalmSide(false):F2} (positivo = lado da palma certo)");

        // pronta: gira em volta da coronha, cano para baixo e para dentro (para a direita do robô)
        Quaternion ready = Quaternion.AngleAxis(ReadyYaw, up) * Quaternion.AngleAxis(ReadyPitch, right);
        gun.rotation = ready * gun.rotation;
        gun.position = stock + ready * (gun.position - stock);

        driver.weapon = gun;
        driver.weaponAimPosition = aimPosition;
        driver.weaponAimRotation = aimRotation;
        driver.weaponReadyPosition = gun.localPosition;
        driver.weaponReadyRotation = gun.localRotation;
        driver.leftElbowHint = pose.leftElbow;
        driver.rightElbowHint = pose.rightElbow;
        driver.recoilKick = body.InverseTransformVector(-forward * shoulders * 0.12f);
        // com a arma presa ao corpo, o tronco gira pouco para o player (senão os ombros se afastam da arma)
        driver.aimBodyWeight = 0.3f;

        // o tiro sai da ponta do cano
        var muzzle = FindDeep(gun, "Muzzle");
        if (muzzle == null)
        {
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gun, false);
        }
        muzzle.localPosition = tipLocal;
        muzzle.localRotation = Quaternion.LookRotation(barrelLocal, upLocal);
        muzzle.localScale = Vector3.one;
        chase.muzzle = muzzle;

        float gunLength = (gun.TransformPoint(barrelLocal * bMax) - gun.TransformPoint(barrelLocal * bMin)).magnitude;
        Debug.Log($"[RobotRigSetup] {root.name}: arma presa ao corpo, coronha no ombro esquerdo; comprimento {gunLength:F2}, ombros {shoulders:F2}");
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
}
