using System.IO;
using UnityEditor;
using UnityEngine;

// Gera materiais URP/Lit e prefabs de variação de cor para o mago e o robô.
// Menu: Tools > Characters > Build Color Variants
public static class CharacterVariantBuilder
{
    const string Root = "Assets/Characters";

    struct Variant
    {
        public string character, name, texture, modelPath;
        public Color tint;
        public bool emissionFromTexture;
    }

    [MenuItem("Tools/Characters/Build Color Variants")]
    public static void Build()
    {
        var variants = new[]
        {
            new Variant { character = "Mage",  name = "Mage_Purple", texture = "mageTexture_Original.png",  modelPath = Root + "/Mage/Mage.fbx",   tint = Color.white },
            new Variant { character = "Mage",  name = "Mage_Beige",  texture = "mageTexture_Beige.png",     modelPath = Root + "/Mage/Mage.fbx",   tint = Color.white },
            new Variant { character = "Robot", name = "Robot_White", texture = "robotTexture_Original.jpg", modelPath = Root + "/Robot/Robot.fbx", tint = Color.white },
            new Variant { character = "Robot", name = "Robot_Red",   texture = "robotTexture_Red.png",      modelPath = Root + "/Robot/Robot.fbx", tint = Color.white },
            new Variant { character = "Robot", name = "Robot_Black", texture = "robotTexture_Black.png",    modelPath = Root + "/Robot/Robot.fbx", tint = Color.white },
        };

        AssetDatabase.Refresh();
        foreach (var v in variants) BuildVariant(v);
        AssetDatabase.SaveAssets();
        Debug.Log("[CharacterVariantBuilder] Variantes geradas.");
    }

    static void BuildVariant(Variant v)
    {
        string dir = $"{Root}/{v.character}";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Textures/{v.texture}");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(v.modelPath);
        if (tex == null || model == null)
        {
            Debug.LogError($"Faltando textura ou modelo para {v.name}");
            return;
        }

        string matPath = $"{dir}/Materials/{v.name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", v.tint);
        mat.SetFloat("_Smoothness", 0.25f);
        mat.SetFloat("_Metallic", v.character == "Robot" ? 0.3f : 0f);
        EditorUtility.SetDirty(mat);

        string prefabPath = $"{dir}/{v.name}.prefab";
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
        instance.name = v.name;
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }
}
