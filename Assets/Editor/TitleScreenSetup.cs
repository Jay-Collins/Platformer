using System.Linq;
using System.IO;
using Player;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleScreenSetup
{
    private const string TitlePath = "Assets/Scenes/TitleScreen.unity";
    private static readonly Color Gold = new Color(1f, 0.77f, 0.29f);
    private static readonly Color Ink = new Color(0.055f, 0.085f, 0.14f);
    private static readonly Color Muted = new Color(0.61f, 0.7f, 0.78f);

    public static void BuildAndRender()
    {
        Build();
        RenderPreviews();
    }

    public static void RenderPreviews()
    {
        EditorSceneManager.OpenScene(TitlePath);
        var camera = Camera.main;
        var canvas = Object.FindFirstObjectByType<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        Directory.CreateDirectory("Logs");
        foreach (var size in new[] { new Vector2Int(1600, 900), new Vector2Int(1024, 768) })
        {
            var target = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = target;
            camera.aspect = (float)size.x / size.y;
            Canvas.ForceUpdateCanvases();
            // The first SRP render initializes the pipeline and canvas resources.
            camera.Render();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            image.Apply();
            File.WriteAllBytes($"Logs/Title-{size.x}x{size.y}.png", image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }
        Debug.Log("TITLE_PREVIEWS_OK");
    }

    [MenuItem("Tools/Player/Create Title Screen")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var source = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var preview = Object.Instantiate(Object.FindFirstObjectByType<PlayerMovement>().GetComponentInChildren<Animator>().gameObject);
        Object.DestroyImmediate(preview.GetComponent<PlayerAnimation>());
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(preview, scene);
        SceneManager.SetActiveScene(scene);
        EditorSceneManager.CloseScene(source, true);
        preview.name = "Rogue Preview";
        preview.transform.position = new Vector3(2.4f, 0.3f, 0);
        preview.transform.rotation = Quaternion.Euler(0, 205, 0);
        preview.transform.localScale = Vector3.one * 1.65f;

        if (!AssetDatabase.IsValidFolder("Assets/Animations/Menu")) AssetDatabase.CreateFolder("Assets/Animations", "Menu");
        const string controllerPath = "Assets/Animations/Menu/TitleCharacter.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.layers[0].stateMachine.AddState("Idle").motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Idle.anim");
        }
        preview.GetComponent<Animator>().runtimeAnimatorController = controller;
        preview.GetComponent<Animator>().applyRootMotion = false;
        AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Idle.anim").SampleAnimation(preview, 0);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.66f, 0.8f);
        var camera = new GameObject("Title Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 3.3f, -10);
        camera.transform.rotation = Quaternion.Euler(8, 0, 0);
        camera.orthographic = true;
        camera.orthographicSize = 3.9f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Ink;
        var key = new GameObject("Warm Key", typeof(Light)).GetComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 2.2f;
        key.color = new Color(1, 0.89f, 0.72f);
        key.transform.rotation = Quaternion.Euler(40, -35, 0);
        var rim = new GameObject("Cool Fill", typeof(Light)).GetComponent<Light>();
        rim.type = LightType.Directional;
        rim.color = new Color(0.4f, 0.7f, 1);
        rim.intensity = 1.1f;
        rim.transform.rotation = Quaternion.Euler(20, 150, 0);
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Display Platform";
        platform.transform.position = new Vector3(2.4f, 0, 0);
        platform.transform.localScale = new Vector3(4, 0.3f, 3.1f);
        platform.GetComponent<Renderer>().sharedMaterial = Material("TitlePlatform", new Color(0.14f, 0.25f, 0.32f));
        var trim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trim.name = "Gold Platform Trim";
        trim.transform.position = new Vector3(2.4f, -0.2f, 0);
        trim.transform.localScale = new Vector3(4.06f, 0.045f, 3.16f);
        trim.GetComponent<Renderer>().sharedMaterial = Material("TitleTrim", Gold);
        var stars = new Transform[3];
        for (int i = 0; i < stars.Length; i++)
        {
            var star = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/star_yellow.prefab"));
            star.name = "Floating Star " + (i + 1);
            foreach (var script in star.GetComponentsInChildren<MonoBehaviour>()) Object.DestroyImmediate(script);
            foreach (var collider in star.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            star.GetComponent<Renderer>().sharedMaterial = Material("TitleStars", Gold);
            star.transform.position = new Vector3(1.1f + i * 1.25f, 4.1f + (i == 1 ? 0.35f : 0), 0.6f);
            star.transform.localScale = Vector3.one * 0.65f;
            stars[i] = star.transform;
        }

        var canvas = new GameObject("Title Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var content = Rect("Content", canvas.transform, new Vector2(800, 450), new Vector2(1600, 900));
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        Text("Eyebrow", content, "A LITTLE ADVENTURE", 18, Gold, new Vector2(120, 700), new Vector2(650, 35));
        var title = Text("Title", content, "PLATFORMER", 78, Color.white, new Vector2(114, 593), new Vector2(785, 100));
        title.fontStyle = FontStyles.Bold;
        Text("Tagline", content, "Every jump counts.", 30, Color.white, new Vector2(120, 534), new Vector2(650, 50));
        Text("Description", content, "Chase the stars. Dodge the hammers.\nFind your way to the next checkpoint.", 23, Muted, new Vector2(120, 440), new Vector2(650, 80));
        var play = Button("Play", content, "PLAY", new Vector2(120, 318), new Vector2(300, 76), Gold, Ink);
        var quit = Button("Quit", content, "QUIT", new Vector2(440, 318), new Vector2(155, 76), new Color(0.13f, 0.19f, 0.26f), Color.white);
        var playNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = quit, selectOnDown = quit };
        var quitNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = play, selectOnUp = play };
        play.navigation = playNav;
        quit.navigation = quitNav;
        Text("Keyboard Hint", content, "ENTER TO PLAY", 14, Muted, new Vector2(120, 277), new Vector2(450, 25));
        var rule = Rect("Rule", content, new Vector2(120, 206), new Vector2(500, 2));
        rule.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.33f, 0.4f);
        Text("Move Keys", content, "W A S D", 23, Color.white, new Vector2(120, 150), new Vector2(170, 35));
        Text("Move Label", content, "MOVE", 13, Muted, new Vector2(120, 124), new Vector2(170, 25));
        Text("Jump Key", content, "SPACE", 23, Color.white, new Vector2(340, 150), new Vector2(200, 35));
        Text("Jump Label", content, "HOLD TO JUMP HIGHER", 13, Muted, new Vector2(340, 124), new Vector2(270, 25));
        Text("Footer", content, "COLLECT.  LEAP.  REPEAT.", 13, Muted, new Vector2(120, 42), new Vector2(650, 30));
        var fadeRect = Rect("Transition", canvas.transform, Vector2.zero, Vector2.zero);
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
        fadeRect.gameObject.AddComponent<Image>().color = Ink;
        var fade = fadeRect.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0;
        fade.blocksRaycasts = false;
        var screen = canvas.AddComponent<TitleScreen>();
        var serialized = new SerializedObject(screen);
        serialized.FindProperty("_playButton").objectReferenceValue = play;
        serialized.FindProperty("_quitButton").objectReferenceValue = quit;
        serialized.FindProperty("_playLabel").objectReferenceValue = play.GetComponentInChildren<TMP_Text>();
        serialized.FindProperty("_fade").objectReferenceValue = fade;
        var array = serialized.FindProperty("_stars");
        array.arraySize = stars.Length;
        for (int i = 0; i < stars.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        events.GetComponent<EventSystem>().firstSelectedGameObject = play.gameObject;
        EditorSceneManager.SaveScene(scene, TitlePath);
        var otherScenes = EditorBuildSettings.scenes.Where(s => s.path != TitlePath).ToList();
        otherScenes.Insert(0, new EditorBuildSettingsScene(TitlePath, true));
        EditorBuildSettings.scenes = otherScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("TITLE_SCREEN_BUILD_OK");
    }

    private static Material Material(string name, Color color)
    {
        var path = "Assets/Animations/Menu/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        material.SetTexture("_MainTex", Texture2D.whiteTexture);
        material.SetFloat("_Smoothness", 0.25f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, Vector2 position, Vector2 dimensions)
    {
        var label = Rect(name, parent, position, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = value;
        label.fontSize = size;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.verticalAlignment = VerticalAlignmentOptions.Middle;
        return label;
    }

    private static Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, Color background, Color foreground)
    {
        var rect = Rect(name, parent, position, size);
        rect.gameObject.AddComponent<Image>().color = background;
        var button = rect.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.selectedColor = new Color(1.12f, 1.12f, 1.12f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        button.colors = colors;
        var text = Text("Label", rect, label, 23, foreground, Vector2.zero, size);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        return button;
    }
}
