using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// tela de abertura do jogo (Tools/Build/Logo FANS na Abertura): primeiro a logo do Unity, depois
// a da FANS, as duas sobre o fundo escuro padrão do Unity. A logo da FANS usada aqui é a versão
// clara (letras brancas e subtítulo em vermelho claro), feita da original para aparecer no escuro.
// O mesmo fica em Edit > Project Settings > Player > Splash Image
public static class SplashLogo
{
    const string LogoPath = "Assets/Objects/Logo FANS Abertura.png";

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

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);

        // só a FANS na lista (a logo do Unity entra sozinha, antes dela, no modo sequencial)
        var logos = new List<PlayerSettings.SplashScreenLogo>
        {
            PlayerSettings.SplashScreenLogo.Create(2.5f, sprite),
        };

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = true;
        PlayerSettings.SplashScreen.logos = logos.ToArray();
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
        PlayerSettings.SplashScreen.backgroundColor = Background;
        PlayerSettings.SplashScreen.overlayOpacity = 1f;

        AssetDatabase.SaveAssets();
        Debug.Log("SplashLogo: " + PlayerSettings.SplashScreen.logos.Length + " logos, Unity " + PlayerSettings.SplashScreen.showUnityLogo);
    }
}
