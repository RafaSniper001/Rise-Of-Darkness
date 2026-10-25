using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// texturas da nave (Tools/Scenes/Texturizar Nave):
// - chão do interior (Chão.mat): chapas de aço claras (MetalPlates004 do ambientCG, CC0, clareada)
// - paredes do interior: dois painéis sci-fi gerados para o jogo (PainelA nas barreiras,
//   PainelB nas paredes curvas), com relevo, brilho e luzes acesas (emissão)
// - casco dos modelos da Millennium Falcon: chapas de metal (MetalPlates001 do ambientCG, CC0)
//   com projeção triplanar, no lugar das cores lisas
// - luz ambiente azulada (as sombras não ficam pretas) e bloom um pouco mais forte
public static class ShipTextures
{
    const string Folder = "Assets/Materials/Nave/";
    const string Shader = "Rise of Darkness/Triplanar Lit";

    // só os modelos grandes da nave (a miniatura da Falcon na cena 1 fica como está)
    const float MinScale = 4f;

    [MenuItem("Tools/Scenes/Texturizar Nave")]
    public static void Run()
    {
        ImportTextures();

        Material hull = Hull("Casco", new Color(0.85f, 0.86f, 0.88f));
        Material hullGray = Hull("Casco Cinza", new Color(0.48f, 0.5f, 0.54f));
        Material hullBeige = Hull("Casco Bege", new Color(0.86f, 0.78f, 0.62f));
        Material hullRed = Hull("Casco Vermelho", new Color(0.6f, 0.12f, 0.09f));
        Material innerWall = InnerWall();
        Material floor = SetupFloor();
        Material wall = SetupWall();

        foreach (string name in new[] { "Scene 1", "Scene 2" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");

            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Transform root = r.transform.root;
                Vector3 s = r.transform.lossyScale;
                float scale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

                Material[] mats = r.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null)
                        continue;

                    Material replacement = null;

                    if (root.name == "Millennium Falcon" && scale >= MinScale)
                    {
                        switch (mats[i].name)
                        {
                            case "Metal": replacement = hull; break;
                            case "Cinza": replacement = hullGray; break;
                            case "Lit": replacement = hullGray; break;
                            case "Bege": replacement = hullBeige; break;
                            case "Vermelho": replacement = hullRed; break;
                            case "Paredes 2": replacement = innerWall; break;
                            case "Nave": replacement = TintedHull(mats[i]); break;
                        }
                    }
                    else if (root.name == "Barreira" && mats[i].name == "Lit")
                    {
                        // barreira da cena 2 com a mesma parede da cena 1
                        replacement = wall;
                    }

                    if (replacement != null && mats[i] != replacement)
                    {
                        mats[i] = replacement;
                        changed = true;
                    }
                }

                if (changed)
                {
                    r.sharedMaterials = mats;
                    Debug.Log("ShipTextures: " + name + " " + r.name + " (" + root.name + ")");
                }
            }

            // sombras azuladas em vez de pretas (o céu de estrelas não ilumina nada)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.26f, 0.29f, 0.38f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.19f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.09f, 0.11f);
            DynamicGI.UpdateEnvironment();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        if (profile != null && profile.TryGet(out Bloom bloom))
        {
            bloom.intensity.Override(0.5f);
            EditorUtility.SetDirty(profile);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("ShipTextures: pronto");
    }

    static void ImportTextures()
    {
        // chão: metálico (R) e brilho (A) num mapa só, como o Lit do URP lê.
        // O metálico vai a 1/4: com o céu escuro, metal puro fica preto e o chão claro escureceria
        string metalGlossPath = Folder + "Chao_MetalGloss.png";
        if (!File.Exists(metalGlossPath))
        {
            var metal = new Texture2D(2, 2);
            metal.LoadImage(File.ReadAllBytes(Folder + "Chao_Metalness.jpg"));
            var rough = new Texture2D(2, 2);
            rough.LoadImage(File.ReadAllBytes(Folder + "Chao_Roughness.jpg"));
            Color[] m = metal.GetPixels(), r = rough.GetPixels();

            for (int i = 0; i < m.Length; i++)
                m[i] = new Color(m[i].r * 0.25f, 0f, 0f, 1f - r[i].r);

            var packed = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false);
            packed.SetPixels(m);
            File.WriteAllBytes(metalGlossPath, packed.EncodeToPNG());
        }

        AssetDatabase.Refresh();

        SetNormal(Folder + "Casco_Normal.jpg", false);
        SetNormal(Folder + "Chao_NormalGL.jpg", false);
        SetLinear(Folder + "Chao_MetalGloss.png");
        SetLinear(Folder + "Casco_Roughness.jpg");

        // painéis das paredes internas (gerados para o jogo: placas, grelhas, dutos e luzes)
        foreach (string panel in new[] { "PainelA", "PainelB" })
        {
            SetNormal(Folder + panel + "_Normal.png", false);
            SetLinear(Folder + panel + "_MetalGloss.png");
            SetLinear(Folder + panel + "_Roughness.png");
        }
    }

    static void SetNormal(string path, bool fromGrayscale)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.NormalMap;
        importer.convertToNormalmap = fromGrayscale;
        importer.heightmapScale = 0.06f;
        importer.normalmapFilter = TextureImporterNormalFilter.Sobel;
        importer.SaveAndReimport();
    }

    static void SetLinear(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture = false;
        importer.SaveAndReimport();
    }

    static Texture2D Tex(string file)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + file);
    }

    static Material Triplanar(string name)
    {
        string path = Folder + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(UnityEngine.Shader.Find(Shader));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.shader = UnityEngine.Shader.Find(Shader);
        return mat;
    }

    // chapas de metal do casco, num tom por material original
    static Material Hull(string name, Color tint)
    {
        Material mat = Triplanar(name);
        mat.SetTexture("_BaseMap", Tex("Casco_Color.jpg"));
        mat.SetTextureScale("_BaseMap", Vector2.one);
        mat.SetTexture("_BumpMap", Tex("Casco_Normal.jpg"));
        mat.SetTexture("_RoughnessMap", Tex("Casco_Roughness.jpg"));
        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_BumpScale", 1f);
        mat.SetFloat("_Smoothness", 0.9f);
        mat.SetFloat("_Metallic", 0.35f);
        mat.SetFloat("_Tiling", 7f);
        mat.SetColor("_EmissionColor", Color.black);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // nave própria da cena 2: o mesmo metal no tom original
    static Material TintedHull(Material original)
    {
        string n = original.name;
        Color tint = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.color;
        tint.a = 1f;
        return Hull("Casco " + n, tint);
    }

    // paredes curvas do interior: painel B (nervuras, duto e painéis de status acesos)
    static Material InnerWall()
    {
        Material mat = Triplanar("Parede Nave");
        mat.SetTexture("_BaseMap", Tex("PainelB_Color.png"));
        mat.SetTextureScale("_BaseMap", Vector2.one);
        mat.SetTexture("_BumpMap", Tex("PainelB_Normal.png"));
        mat.SetTexture("_RoughnessMap", Tex("PainelB_Roughness.png"));
        mat.SetTexture("_EmissionMap", Tex("PainelB_Emission.png"));
        mat.SetColor("_EmissionColor", new Color(2.5f, 2.5f, 2.5f));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_BumpScale", 1f);
        mat.SetFloat("_Smoothness", 1f);
        mat.SetFloat("_Metallic", 0.45f);
        mat.SetFloat("_Tiling", 5f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material SetupFloor()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Chão.mat");
        // chapas de aço com rebites (MetalPlates004 do ambientCG, CC0), clareadas para contrastar
        // com as paredes escuras
        mat.SetTexture("_BaseMap", Tex("Chao_Color_Claro.jpg"));
        mat.SetTextureScale("_BaseMap", new Vector2(18f, 18f));
        mat.SetColor("_BaseColor", new Color(1f, 1f, 1f));
        mat.SetTexture("_BumpMap", Tex("Chao_NormalGL.jpg"));
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_MetallicGlossMap", Tex("Chao_MetalGloss.png"));
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Smoothness", 1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material SetupWall()
    {
        // barreiras: painel A (placas com grelhas e faixas de luz). O cubo da barreira tem
        // ~49 x 8,6 m e o UV vai de 0 a 1 em cada face: 11 x 2 repetições = placas de ~4,4 m
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Parede.mat");
        mat.SetTexture("_BaseMap", Tex("PainelA_Color.png"));
        mat.SetTextureScale("_BaseMap", new Vector2(11f, 2f));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTexture("_BumpMap", Tex("PainelA_Normal.png"));
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_MetallicGlossMap", Tex("PainelA_MetalGloss.png"));
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Smoothness", 1f);
        mat.SetTexture("_EmissionMap", Tex("PainelA_Emission.png"));
        mat.SetColor("_EmissionColor", new Color(2.5f, 2.5f, 2.5f));
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
