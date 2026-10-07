using System;
using System.Linq;
using Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayerAnimationSetup
{
    private const string SourceFolder = "Assets/Assets/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";
    private const string OutputFolder = "Assets/Animations/Player";
    private const string ControllerPath = "Assets/PlayerCharacterAnimatorController.controller";

    [MenuItem("Tools/Player/Rebuild Animations")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations")) AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets/Animations", "Player");

        var idle = CopyClip("General", "Idle_B", "Idle", true);
        var run = CopyClip("MovementBasic", "Running_A", "Run", true);
        var jump = CopyClip("MovementBasic", "Jump_Start", "Jump", false);
        var fall = CopyClip("MovementBasic", "Jump_Idle", "Fall", true);
        var land = CopyClip("MovementBasic", "Jump_Land", "Land", false);
        var death = CopyClip("General", "Death_A", "Death", false);
        AnimationUtility.SetAnimationEvents(run, new[]
        {
            new AnimationEvent { functionName = "PlayFootstep", time = run.length * 0.25f },
            new AnimationEvent { functionName = "PlayFootstep", time = run.length * 0.75f }
        });

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        // Retain the controller asset's GUID so existing references remain valid.
        controller.layers = Array.Empty<AnimatorControllerLayer>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            if (asset != controller) UnityEngine.Object.DestroyImmediate(asset, true);
        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = "MovementEnabled", type = AnimatorControllerParameterType.Bool, defaultBool = true
        });
        controller.AddLayer("Base Layer");
        var machine = controller.layers[0].stateMachine;
        var locomotion = machine.AddState("Locomotion", new Vector3(250, 100));
        var blend = new BlendTree { name = "Idle Run", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(blend, controller);
        blend.AddChild(idle, 0f);
        blend.AddChild(run, 1f);
        locomotion.motion = blend;
        var jumping = AddState(machine, "Jump", jump, new Vector3(500, 0), 2.4f);
        var falling = AddState(machine, "Fall", fall, new Vector3(750, 100));
        var landing = AddState(machine, "Land", land, new Vector3(500, 250), 2.5f);
        var disabled = AddState(machine, "Death", death, new Vector3(250, 400));
        machine.defaultState = locomotion;

        foreach (var state in new[] { locomotion, landing })
        {
            var up = Transition(state, jumping, 0.06f);
            up.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            up.AddCondition(AnimatorConditionMode.Greater, 0, "VerticalSpeed");
            var down = Transition(state, falling, 0.08f);
            down.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            down.AddCondition(AnimatorConditionMode.Less, 0.01f, "VerticalSpeed");
        }
        Transition(jumping, falling, 0.1f).AddCondition(AnimatorConditionMode.Less, 0.01f, "VerticalSpeed");
        foreach (var state in new[] { jumping, falling })
            Transition(state, landing, 0.06f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        Transition(landing, locomotion, 0.08f).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var finishLanding = Transition(landing, locomotion, 0.08f);
        finishLanding.hasExitTime = true;
        finishLanding.exitTime = 0.85f;
        var stop = machine.AddAnyStateTransition(disabled);
        stop.hasExitTime = false;
        stop.duration = 0.1f;
        stop.hasFixedDuration = true;
        stop.canTransitionToSelf = false;
        stop.AddCondition(AnimatorConditionMode.IfNot, 0, "MovementEnabled");
        Transition(disabled, locomotion, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "MovementEnabled");
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("PLAYER_ANIMATION_BUILD_OK");
    }

    public static void BuildAndWireScene()
    {
        Build();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var movement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        var animator = movement.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        animator.applyRootMotion = false;
        var driver = animator.GetComponent<PlayerAnimation>();
        if (driver == null) driver = animator.gameObject.AddComponent<PlayerAnimation>();
        var serialized = new SerializedObject(driver);
        serialized.FindProperty("_animator").objectReferenceValue = animator;
        serialized.FindProperty("_movement").objectReferenceValue = movement;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
    }

    private static AnimationClip CopyClip(string category, string sourceName, string name, bool loop)
    {
        var source = AssetDatabase.LoadAllAssetsAtPath(SourceFolder + "Rig_Medium_" + category + ".fbx")
            .OfType<AnimationClip>().Single(clip => clip.name == sourceName);
        var clip = UnityEngine.Object.Instantiate(source);
        clip.name = name;
        // KayKit's animation pack and Rogue use different names for the same generic rig.
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var remapped = binding;
            if (binding.path == "Rig_Medium" || binding.path.StartsWith("Rig_Medium/"))
                remapped.path = "Rig" + binding.path.Substring("Rig_Medium".Length);
            if (remapped.path == binding.path) continue;
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            AnimationUtility.SetEditorCurve(clip, binding, null);
            AnimationUtility.SetEditorCurve(clip, remapped, curve);
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.loopBlend = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        var path = OutputFolder + "/" + name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) AssetDatabase.CreateAsset(clip, path);
        else
        {
            EditorUtility.CopySerialized(clip, existing);
            UnityEngine.Object.DestroyImmediate(clip);
            clip = existing;
        }
        return clip;
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position, float speed = 1f)
    {
        var state = machine.AddState(name, position);
        state.motion = motion;
        state.speed = speed;
        return state;
    }

    private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, float duration)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.interruptionSource = TransitionInterruptionSource.Destination;
        return transition;
    }

    [MenuItem("Tools/Player/Validate Animation Bindings")]
    public static void Validate()
    {
        var movement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        var original = movement.GetComponentInChildren<Animator>();
        var preview = UnityEngine.Object.Instantiate(original.gameObject);
        try
        {
            var animator = preview.GetComponent<Animator>();
            if (animator.GetComponent<PlayerAnimation>() == null) throw new Exception("Missing animation driver.");
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                foreach (var binding in bindings)
                    if (!string.IsNullOrEmpty(binding.path) && animator.transform.Find(binding.path) == null)
                        throw new Exception($"Unbound animation: {clip.name}: {binding.path}");
                var bone = animator.transform.Find("Rig/root/hips/spine/chest/upperarm.l");
                clip.SampleAnimation(animator.gameObject, 0f);
                var start = bone.localRotation;
                clip.SampleAnimation(animator.gameObject, clip.length * 0.4f);
                Debug.Log($"PLAYER_VALIDATED {clip.name}: {bindings.Length} bindings, loop={clip.isLooping}, armDelta={Quaternion.Angle(start, bone.localRotation)}");
            }
            Debug.Log("PLAYER_ANIMATION_BINDINGS_OK");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(preview);
        }
    }
}
