using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// tela de abertura do jogo (Tools/Build/Logo FANS na Abertura): a logo da FANS entra depois
// da logo do Unity, sobre fundo branco (a logo tem fundo transparente e letras azul-marinho).
// O mesmo fica em Edit > Project Settings > Player > Splash Image
public static class SplashLogo
{
    const string LogoPath = "Assets/Objects/Logo FANS.png";

    [MenuItem("Tools/Build/Logo FANS na Abertura")]
    public static void Run()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(LogoPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);

        var logos = new List<PlayerSettings.SplashScreenLogo>();
        foreach (var logo in PlayerSettings.SplashScreen.logos)
            if (logo.logo != sprite)
                logos.Add(logo);
        logos.Add(PlayerSettings.SplashScreenLogo.Create(2.5f, sprite));

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.logos = logos.ToArray();
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
        PlayerSettings.SplashScreen.backgroundColor = Color.white;
        PlayerSettings.SplashScreen.overlayOpacity = 0f;

        AssetDatabase.SaveAssets();
        Debug.Log("SplashLogo: " + PlayerSettings.SplashScreen.logos.Length + " logos, Unity " + PlayerSettings.SplashScreen.showUnityLogo);
    }
}
