using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// abertura do jogo (Tools/Build/Logo FANS na Abertura):
// 1) a tela de abertura do Unity fica como é originalmente: só a logo do Unity, em branco,
//    no fundo escuro padrão;
// 2) quando ela termina, a cena Intro (a primeira da build) mostra só a logo da FANS, no mesmo
//    fundo, com fade, e abre o menu principal (IntroLogo).
// A logo da FANS usada é a original (azul); há também uma versão clara para fundo escuro
// (letras brancas e subtítulo em vermelho claro) em Logo FANS Abertura.png
public static class SplashLogo
{
    // logo da Intro: a original (azul). A versão clara para fundo escuro é "Logo FANS Abertura.png"
    const string LogoPath = "Assets/Objects/Logo FANS.png";
    const string IntroPath = "Assets/Scenes/Intro.unity";

    // fundo escuro padrão do Unity (o que o projeto usava antes)
    static readonly Color Background = new Color(0.13725491f, 0.12156863f, 0.1254902f);

    [MenuItem("Tools/Build/Logo FANS na Abertura")]
    public static void Run()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(LogoPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        // tela do Unity como originalmente: nenhuma logo extra
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = true;
        PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[0];
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;
        PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
        PlayerSettings.SplashScreen.backgroundColor = Background;
        PlayerSettings.SplashScreen.overlayOpacity = 1f;

        BuildIntro(AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath));

        // Intro primeiro na build
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(IntroPath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != IntroPath)
                scenes.Add(s);
        EditorBuildSettings.scenes = scenes.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("SplashLogo: build = " + string.Join(", ", System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path)));
    }

    // cena com câmera no fundo escuro e a logo centralizada (60% da largura da tela)
    static void BuildIntro(Sprite sprite)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        cam.cullingMask = 0;
        camGo.AddComponent<AudioListener>();

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var logoGo = new GameObject("Logo FANS", typeof(RectTransform));
        logoGo.transform.SetParent(canvasGo.transform, false);
        var image = logoGo.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        var rect = (RectTransform)logoGo.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1150f, 1150f * sprite.rect.height / sprite.rect.width);
        var group = logoGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var intro = new GameObject("Intro").AddComponent<IntroLogo>();
        intro.logo = group;

        EditorSceneManager.SaveScene(scene, IntroPath);
    }
}
