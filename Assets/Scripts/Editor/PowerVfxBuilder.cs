using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

// Restaura as partículas originais (versão do último commit, copiada em Assets/_Originais)
// e, nas do poder, muda só a estética: cor em dois tons, sumir aos poucos, encolher no fim
// e faísca esticada na explosão. Quantidade, direção, velocidade, tempo de vida e forma
// continuam os originais.
// Menu: Tools > VFX > Restore Particles
public static class PowerVfxBuilder
{
    const string OrigDir = "Assets/_Originais";

    [MenuItem("Tools/VFX/Restore Particles")]
    public static void Run()
    {
        RestorePrefab("Assets/Objects/Player.prefab", $"{OrigDir}/Player_orig.prefab");
        RestorePrefab("Assets/Objects/Enemy 4.prefab", $"{OrigDir}/Enemy4_orig.prefab");
        RestoreButtons("Assets/Scenes/Scene 1.unity", $"{OrigDir}/Scene1_orig.unity");
        RestoreBoss("Assets/Scenes/Scene 2.unity", $"{OrigDir}/Scene2_orig.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[PowerVfxBuilder] Concluído.");
    }

    static void Copy(ParticleSystem from, ParticleSystem to)
    {
        ComponentUtility.CopyComponent(from);
        ComponentUtility.PasteComponentValues(to);
        ComponentUtility.CopyComponent(from.GetComponent<ParticleSystemRenderer>());
        ComponentUtility.PasteComponentValues(to.GetComponent<ParticleSystemRenderer>());

        for (int i = to.transform.childCount - 1; i >= 0; i--)
            if (to.transform.GetChild(i).name.StartsWith("VFX_"))
                Object.DestroyImmediate(to.transform.GetChild(i).gameObject);

        EditorUtility.SetDirty(to);
    }

    static void RestorePrefab(string path, string origPath)
    {
        var orig = AssetDatabase.LoadAssetAtPath<GameObject>(origPath);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var name in new[] { "PowerAura", "PowerExplosion" })
            {
                var to = root.transform.Find(name).GetComponent<ParticleSystem>();
                Copy(orig.transform.Find(name).GetComponent<ParticleSystem>(), to);
                Beautify(to, name == "PowerExplosion");
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static string PathOf(Transform t)
    {
        return t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }

    static ParticleSystem FindByPath(Scene scene, string path)
    {
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ps.gameObject.scene == scene && PathOf(ps.transform) == path)
                return ps;
        return null;
    }

    // faíscas do botão: exatamente como eram
    static void RestoreButtons(string scenePath, string origScenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var origScene = EditorSceneManager.OpenScene(origScenePath, OpenSceneMode.Additive);
        int count = 0;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ps.gameObject.scene != scene || ps.name != "BotonExplosion") continue;
            var orig = FindByPath(origScene, PathOf(ps.transform));
            if (orig == null) { Debug.LogWarning("Original não encontrado: " + PathOf(ps.transform)); continue; }
            Copy(orig, ps);
            count++;
        }
        EditorSceneManager.CloseScene(origScene, true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PowerVfxBuilder] botões restaurados: {count}");
    }

    // efeitos do boss na cena 2 (que têm cor própria na cena) + estética
    static void RestoreBoss(string scenePath, string origScenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var origScene = EditorSceneManager.OpenScene(origScenePath, OpenSceneMode.Additive);

        BossAura current = null, orig = null;
        foreach (var b in Object.FindObjectsByType<BossAura>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.gameObject.scene == scene) current = b;
            else if (b.gameObject.scene == origScene) orig = b;
        }

        if (current != null && orig != null)
        {
            Copy(orig.auraChargedEffect, current.auraChargedEffect);
            Beautify(current.auraChargedEffect, false);
            Copy(orig.auraExplosionEffect, current.auraExplosionEffect);
            Beautify(current.auraExplosionEffect, true);
        }

        EditorSceneManager.CloseScene(origScene, true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ---------- só estética ----------

    static void Beautify(ParticleSystem ps, bool explosion)
    {
        var main = ps.main;
        Color baseColor = main.startColor.mode == ParticleSystemGradientMode.TwoColors
            ? main.startColor.colorMax
            : main.startColor.color;
        baseColor.a = 1f;
        Color light = Color.Lerp(baseColor, Color.white, 0.55f);

        // cada partícula sai num tom entre a cor original e uma versão mais clara
        main.startColor = new ParticleSystem.MinMaxGradient(baseColor, light);

        // nasce clara, puxa para a cor original e some aos poucos
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(baseColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.45f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        // encolhe no fim da vida
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.6f, 0.8f), new Keyframe(1f, 0.15f)));

        // explosão: rastro curto na direção do movimento (parece energia)
        if (explosion)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.02f;
            r.lengthScale = 1.2f;
        }

        EditorUtility.SetDirty(ps);
    }
}
