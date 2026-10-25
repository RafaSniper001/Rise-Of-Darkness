using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// estilo da interface (Tools/UI/Aplicar Estilo): botões de vidro escuro com cantos cortados e
// borda azul (sprites em Assets/UI, troca de sprite no realce + UIButtonFX), títulos e botões
// em Orbitron (a letra do logo), textos e HUD em Rajdhani (fontes OFL em Assets/Fontes).
// A STAR7 tinha o espaçamento quebrado ("CONT INUAR") e a Techno não tem acentos nem números.
// Só mexe na aparência e na posição: os botões continuam os mesmos objetos, com os mesmos eventos
public static class UIStyle
{
    const string UI = "Assets/UI/";
    const string Fonts = "Assets/Fontes/";

    static readonly Color TextColor = new Color(0.82f, 0.88f, 1f);
    static readonly Color LabelColor = new Color(0.62f, 0.78f, 1f);
    static readonly Color HealthColor = new Color(0.25f, 0.88f, 0.45f);
    static readonly Color StaminaColor = new Color(0.25f, 0.65f, 1f);
    static readonly Color BossColor = new Color(1f, 0.22f, 0.3f);
    static readonly Color AccentColor = new Color(1f, 0.6f, 0.2f);

    static TMP_FontAsset title;
    static TMP_FontAsset body;
    static Sprite button, buttonHover, buttonPressed, panel, barBack, barFill, handle;

    [MenuItem("Tools/UI/Aplicar Estilo")]
    public static void Run()
    {
        ImportSprites();
        title = FontAsset("Orbitron.ttf", "Orbitron SDF");
        body = FontAsset("Rajdhani-SemiBold.ttf", "Rajdhani SDF");

        MainMenu();
        foreach (string s in new[] { "Scene 1", "Scene 2" })
            GameScene(s);
        foreach (string s in new[] { "DefeatScene", "VictoryScene" })
            EndScene(s);

        AssetDatabase.SaveAssets();
    }

    // ---------- cenas ----------

    static void MainMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var canvas = GameObject.Find("Canvas").transform;

        float y = 150f;
        foreach (string name in new[] { "PlayButton", "ContinueButton", "ExitButton" })
        {
            StyleButton(canvas.Find(name), new Vector2(0f, y), new Vector2(460f, 76f), 32f);
            y -= 100f;
        }

        // instruções: o texto corrido de antes vira um painel com três colunas
        var old = canvas.Find("Text (TMP)");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);
        var existing = canvas.Find("Instrucoes");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var box = NewImage("Instrucoes", canvas, panel);
        Place(box, new Vector2(0.5f, 0.5f), new Vector2(0f, -360f), new Vector2(1560f, 300f));

        string key = "<color=#9FD8FF>";
        Column(box, 0, "Controles",
            key + "W A S D</color>   Movimentação\n" +
            key + "Mouse</color>   Câmera\n" +
            key + "Clique esquerdo</color>   Ataque\n" +
            key + "Shift</color>   Correr / usar stamina\n" +
            key + "Espaço</color>   Explosão de aura");
        Column(box, 1, "Aura",
            "Ataque inimigos com o sabre para carregar sua aura. " +
            "Com a aura carregada, você ganha mais velocidade, vida e dano.\n" +
            "Pressione " + key + "Espaço</color> para liberar uma explosão de aura que causa dano em inimigos comuns " +
            "e empurra inimigos próximos, consumindo todo o acumulado.");
        Column(box, 2, "Objetivo",
            "Elimine todos os inimigos e destrua os scanners para avançar pela nave.\n" +
            "Após sair da nave inimiga, derrote o guerreiro fantasma e alcance sua própria nave para vencer.");

        Save(scene);
    }

    static void Column(Transform box, int index, string header, string text)
    {
        const float width = 1560f / 3f;
        float x = -1560f / 2f + width * index + 36f;

        var head = NewText("Titulo " + header, box, title, 22f, AccentColor);
        head.text = header;
        head.characterSpacing = 10f;
        head.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        head.alignment = TextAlignmentOptions.TopLeft;
        Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(x + (width - 72f) / 2f, -28f), new Vector2(width - 72f, 32f), new Vector2(0.5f, 1f));

        var line = NewImage("Linha " + header, box, null);
        line.GetComponent<Image>().color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.5f);
        Place(line, new Vector2(0.5f, 1f), new Vector2(x + 30f, -64f), new Vector2(60f, 2f), new Vector2(0.5f, 1f));

        var t = NewText("Texto " + header, box, body, 22f, TextColor);
        t.text = text;
        t.lineSpacing = 4f;
        t.paragraphSpacing = 8f;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.richText = true;
        Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(x + (width - 72f) / 2f, -78f), new Vector2(width - 72f, 210f), new Vector2(0.5f, 1f));
    }

    static void GameScene(string sceneName)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");

        // pausa: fundo escurecido atrás do painel (dentro do PauseMenu, que é o objeto ligado e
        // desligado pelo InGameController): a arte do painel passa para um filho depois do escuro
        var pause = GameObject.Find("PauseCanvas").transform.Find("PauseMenu");
        var pauseImage = pause.GetComponent<Image>();
        if (pause.Find("Arte") == null)
        {
            var dim = NewImage("Escurecer", pause, null);
            dim.GetComponent<Image>().color = new Color(0f, 0.01f, 0.03f, 0.6f);
            Place(dim, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 3000f));
            dim.SetSiblingIndex(0);

            var art = NewImage("Arte", pause, pauseImage.sprite);
            art.GetComponent<Image>().raycastTarget = false;
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = art.offsetMax = Vector2.zero;
            art.SetSiblingIndex(1);

            var frame = NewImage("Moldura", pause, panel);
            frame.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
            frame.GetComponent<Image>().raycastTarget = false;
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.offsetMin = frame.offsetMax = Vector2.zero;
            frame.SetSiblingIndex(2);

            pauseImage.enabled = false;
        }

        // o painel tem 500 de largura: botões menores, com a letra menor ("VOLTAR AO MENU" cabe)
        StyleButton(pause.Find("ResumeButton"), null, new Vector2(380f, 66f), 22f);
        StyleButton(pause.Find("MainMenuButton"), null, new Vector2(380f, 66f), 22f);
        QuitButton(pause);

        foreach (Transform group in pause)
        {
            var slider = group.GetComponentInChildren<Slider>(true);
            if (slider == null)
                continue;

            StyleSlider(slider, StaminaColor, true, new Vector2(300f, 14f));
            foreach (var label in group.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleLabel(label, body, 26f, LabelColor);
                label.characterSpacing = 8f;
            }
        }

        // HUD: vidas, vida e stamina num painel no canto de cima à direita; vida do boss no alto, ao centro
        var hud = Object.FindAnyObjectByType<InGameHUD>(FindObjectsInactive.Include);
        var canvas = hud.transform;

        var box = canvas.Find("PainelStatus");
        if (box == null)
        {
            box = NewImage("PainelStatus", canvas, panel);
            box.SetSiblingIndex(0);
        }
        box.GetComponent<Image>().raycastTarget = false;
        Place((RectTransform)box, Vector2.one, new Vector2(-24f, -24f), new Vector2(370f, 156f), Vector2.one);

        if (hud.livesText != null)
        {
            StyleLabel(hud.livesText, title, 26f, new Color(1f, 0.42f, 0.42f));
            hud.livesText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            hud.livesText.characterSpacing = 6f;
            hud.livesText.alignment = TextAlignmentOptions.TopLeft;
            Place(hud.livesText.rectTransform, Vector2.one, new Vector2(-48f, -38f), new Vector2(322f, 40f), Vector2.one);
        }

        HudBar(hud.healthSlider, HealthColor, -102f, "Vida");
        HudBar(hud.staminaSlider, StaminaColor, -146f, "Stamina");

        if (hud.enemyHealthSlider != null)
        {
            StyleSlider(hud.enemyHealthSlider, BossColor, false, new Vector2(720f, 16f));
            Place((RectTransform)hud.enemyHealthSlider.transform, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(720f, 16f), new Vector2(0.5f, 1f));
            var label = hud.enemyHealthSlider.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                StyleLabel(label, title, 22f, new Color(1f, 0.75f, 0.78f));
                label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                label.characterSpacing = 14f;
                label.alignment = TextAlignmentOptions.Bottom;
                Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(720f, 34f), new Vector2(0.5f, 0f));
            }
        }

        Save(scene);
    }

    // botão Sair do jogo na pausa, logo abaixo do Voltar ao Menu (encerra o jogo pela pausa)
    static void QuitButton(Transform pause)
    {
        var menu = pause.Find("MainMenuButton");
        var controller = Object.FindAnyObjectByType<InGameController>(FindObjectsInactive.Include);
        if (menu == null || controller == null)
            return;

        var quit = pause.Find("QuitButton");
        if (quit == null)
        {
            quit = Object.Instantiate(menu.gameObject, pause).transform;
            quit.name = "QuitButton";
        }

        var rect = (RectTransform)quit;
        var menuRect = (RectTransform)menu;
        rect.anchoredPosition = menuRect.anchoredPosition + new Vector2(0f, -82f);

        var b = quit.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, controller.QuitGame);

        // cópia do Voltar ao Menu: já vem com o mesmo estilo
        var label = quit.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "Sair do jogo";
    }

    // só o botão Sair do jogo nas pausas (sem refazer o resto do estilo das cenas)
    [MenuItem("Tools/UI/Botão Sair na Pausa")]
    public static void AddQuitButtons()
    {
        foreach (string s in new[] { "Scene 1", "Scene 2" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + s + ".unity");
            QuitButton(GameObject.Find("PauseCanvas").transform.Find("PauseMenu"));
            Save(scene);
        }
    }

    // barra do HUD com o nome em cima, à esquerda
    static void HudBar(Slider slider, Color color, float y, string name)
    {
        if (slider == null)
            return;

        StyleSlider(slider, color, false, new Vector2(322f, 14f));
        Place((RectTransform)slider.transform, Vector2.one, new Vector2(-48f, y), new Vector2(322f, 14f), Vector2.one);

        var label = slider.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            StyleLabel(label, body, 20f, LabelColor);
            label.text = name;
            label.characterSpacing = 8f;
            label.alignment = TextAlignmentOptions.BottomLeft;
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 2f), new Vector2(322f, 26f), new Vector2(0f, 0f));
        }
    }

    static void EndScene(string sceneName)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
        var canvas = GameObject.Find("Canvas").transform;

        // os dois botões lado a lado, embaixo, sem cobrir a arte
        StyleButton(canvas.Find("PlayButton"), new Vector2(-250f, -430f), new Vector2(470f, 80f), 28f);
        StyleButton(canvas.Find("MainMenuButton"), new Vector2(250f, -430f), new Vector2(470f, 80f), 28f);

        Save(scene);
    }

    // ---------- componentes ----------

    static void StyleButton(Transform t, Vector2? position, Vector2 size, float fontSize)
    {
        if (t == null)
            return;

        var rect = (RectTransform)t;
        rect.sizeDelta = size;
        if (position.HasValue)
            rect.anchoredPosition = position.Value;

        var outline = t.GetComponent<Outline>();
        if (outline != null)
            Object.DestroyImmediate(outline);

        var image = t.GetComponent<Image>();
        image.sprite = button;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        var b = t.GetComponent<Button>();
        b.transition = Selectable.Transition.SpriteSwap;
        b.spriteState = new SpriteState
        {
            highlightedSprite = buttonHover,
            pressedSprite = buttonPressed,
            selectedSprite = buttonHover,
            disabledSprite = button,
        };

        var label = t.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            StyleLabel(label, title, fontSize, TextColor);
            label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            label.characterSpacing = 8f;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = new Vector4(16f, 0f, 16f, 0f);
        }

        var fx = t.GetComponent<UIButtonFX>();
        if (fx == null)
            fx = t.gameObject.AddComponent<UIButtonFX>();
        fx.normalTextColor = TextColor;
        EditorUtility.SetDirty(fx);
    }

    static void StyleSlider(Slider slider, Color color, bool interactive, Vector2 size)
    {
        ((RectTransform)slider.transform).sizeDelta = size;

        var back = slider.transform.Find("Background") as RectTransform;
        if (back != null)
        {
            back.anchorMin = Vector2.zero;
            back.anchorMax = Vector2.one;
            back.offsetMin = back.offsetMax = Vector2.zero;
            var img = back.GetComponent<Image>();
            img.sprite = barBack;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        var area = slider.fillRect != null ? slider.fillRect.parent as RectTransform : null;
        if (area != null)
        {
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(3f, 3f);
            area.offsetMax = new Vector2(-3f, -3f);
            slider.fillRect.sizeDelta = Vector2.zero;
            var img = slider.fillRect.GetComponent<Image>();
            img.sprite = barFill;
            img.type = Image.Type.Sliced;
            img.color = color;
        }

        if (slider.handleRect != null)
        {
            var handleArea = slider.handleRect.parent as RectTransform;
            if (interactive)
            {
                handleArea.offsetMin = new Vector2(3f, 0f);
                handleArea.offsetMax = new Vector2(-3f, 0f);
                slider.handleRect.sizeDelta = new Vector2(26f, 12f);
                var img = slider.handleRect.GetComponent<Image>();
                img.sprite = handle;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;
                slider.transition = Selectable.Transition.ColorTint;
                var colors = slider.colors;
                colors.normalColor = new Color(0.85f, 0.92f, 1f);
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.6f, 0.85f, 1f);
                colors.selectedColor = Color.white;
                slider.colors = colors;
            }
            else
            {
                // barras do HUD só mostram o valor
                handleArea.gameObject.SetActive(false);
                slider.handleRect = null;
                slider.transition = Selectable.Transition.None;
                slider.interactable = false;
            }
        }

        EditorUtility.SetDirty(slider);
    }

    static void StyleLabel(TMP_Text label, TMP_FontAsset font, float size, Color color)
    {
        label.font = font;
        label.fontSharedMaterial = font.material;
        label.fontSize = size;
        label.enableAutoSizing = false;
        label.color = color;
        label.fontStyle = FontStyles.UpperCase;
        label.text = label.text.Trim();
        label.textWrappingMode = TextWrappingModes.NoWrap;
        EditorUtility.SetDirty(label);
    }

    static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.fontSharedMaterial = font.material;
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static RectTransform NewImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Save(UnityEngine.SceneManagement.Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("UIStyle: " + scene.name + " pronta");
    }

    // ---------- recursos ----------

    static void ImportSprites()
    {
        var borders = new Dictionary<string, Vector4>
        {
            { "UI_Botao", new Vector4(24, 24, 24, 24) },
            { "UI_Botao_Realce", new Vector4(24, 24, 24, 24) },
            { "UI_Botao_Pressionado", new Vector4(24, 24, 24, 24) },
            { "UI_Painel", new Vector4(22, 22, 22, 22) },
            { "UI_Barra_Fundo", new Vector4(6, 6, 6, 6) },
            { "UI_Barra", new Vector4(5, 5, 5, 5) },
            { "UI_Alca", Vector4.zero },
        };

        foreach (var pair in borders)
        {
            string path = UI + pair.Key + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = pair.Value;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        button = Sprite("UI_Botao");
        buttonHover = Sprite("UI_Botao_Realce");
        buttonPressed = Sprite("UI_Botao_Pressionado");
        panel = Sprite("UI_Painel");
        barBack = Sprite("UI_Barra_Fundo");
        barFill = Sprite("UI_Barra");
        handle = Sprite("UI_Alca");
    }

    static Sprite Sprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(UI + name + ".png");
    }

    // fonte TMP dinâmica (o atlas cresce com as letras usadas), já com o alfabeto em português
    static TMP_FontAsset FontAsset(string ttf, string name)
    {
        string path = Fonts + name + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset != null)
            return asset;

        var font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + ttf);
        asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        asset.name = name;
        AssetDatabase.CreateAsset(asset, path);

        asset.atlasTextures[0].name = name + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        asset.material.name = name + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        asset.TryAddCharacters(
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÀÁÂÃÇÉÊÍÓÔÕÚÜàáâãçéêíóôõúü—–·•");

        if (TMP_Settings.defaultFontAsset != null)
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }
}
