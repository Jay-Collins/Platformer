using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UI;
using CameraSystem;

// Run with -batchmode -nographics -executeMethod PlayerAnimationValidation.Run (without -quit).
// Exercises the saved player and its real physics/animation update in a temporary test area.
[InitializeOnLoad]
public static class PlayerAnimationValidation
{
    private const string PendingKey = "PlayerAnimationValidation.Pending";
    private static readonly Stack<IEnumerator> Steps = new Stack<IEnumerator>();
    private static double _deadline;

    static PlayerAnimationValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            _deadline = EditorApplication.timeSinceStartup + 60;
            Steps.Push(SessionState.GetBool("PlayerValidation.Title", false) ? CheckTitle() : CheckPlayer());
            EditorApplication.update += Tick;
        };
    }

    [MenuItem("Tools/Player/Validate Gameplay in Play Mode")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetBool("PlayerValidation.Title", false);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Tools/Player/Validate Title Screen in Play Mode")]
    public static void RunTitle()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/TitleScreen.unity");
        SessionState.SetBool("PlayerValidation.Title", true);
        SessionState.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static IEnumerator CheckTitle()
    {
        yield return Wait(0.2f);
        var title = UnityEngine.Object.FindFirstObjectByType<TitleScreen>();
        Require(title != null, "Title screen must load.");
        Require(UnityEngine.Object.FindFirstObjectByType<PlayerMovement>() == null, "Gameplay must not run behind the menu.");
        Require(EventSystem.current.currentSelectedGameObject.name == "Play", "Play must be selected for keyboard input.");
        Require(SceneUtility.GetScenePathByBuildIndex(0).EndsWith("TitleScreen.unity"), "Build must start on the title screen.");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var backgroundBehavior = InputSystem.settings.backgroundBehavior;
        var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        try
        {
            yield return Wait(0.1f);
            var module = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log($"TITLE_INPUT submitEnabled={module.submit.action.enabled} controls={module.submit.action.controls.Count} keyboardEnabled={keyboard.enabled}");
            module.submit.action.performed += _ => Debug.Log("TITLE_SUBMIT_RECEIVED");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            yield return Until(() => title == null || title.IsLoading, "Enter activates Play");
            if (title != null) title.Play(); // Repeated activation must not start a second load.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Until(() => SceneManager.GetActiveScene().name == "SampleScene", "gameplay scene load");
            yield return Wait(0.4f);
            Require(UnityEngine.Object.FindFirstObjectByType<PlayerMovement>() != null, "Play must load the player.");
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Only one EventSystem should remain.");
            Debug.Log("TITLE_FLOW_OK: default selection, keyboard activation, duplicate guard, gameplay load");
        }
        finally
        {
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            InputSystem.RemoveDevice(keyboard);
        }
        yield return CheckPlayer();
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > _deadline) throw new Exception("Player animation test timed out.");
            if (Steps.Count > 0)
            {
                var step = Steps.Peek();
                if (!step.MoveNext()) Steps.Pop();
                else if (step.Current is IEnumerator nested) Steps.Push(nested);
                return;
            }
            Debug.Log("PLAYER_ANIMATION_PLAYMODE_OK");
            Finish(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(1);
        }
    }

    private static void Finish(int exitCode)
    {
        EditorApplication.update -= Tick;
        Steps.Clear();
        if (Application.isBatchMode) EditorApplication.Exit(exitCode);
        else EditorApplication.ExitPlaymode();
    }

    private static IEnumerator CheckPlayer()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        var animator = player.GetComponentInChildren<Animator>();
        var body = player.GetComponent<Rigidbody>();
        UnityEngine.Object.FindFirstObjectByType<InputManager>().enabled = false;
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Animation Validation Ground";
        platform.layer = 6;
        platform.transform.position = new Vector3(1000, 0, 1000);
        platform.transform.localScale = new Vector3(100, 1, 100);
        player.ResetMotion();
        body.position = new Vector3(1000, 5, 1000);
        Physics.SyncTransforms();
        yield return Until(() => player.IsGrounded, "initial grounding");
        yield return Wait(0.4f);
        RequireState(animator, "Locomotion");
        Require(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "Idle", "Idle clip must play at rest.");
        Debug.Log("PLAYER_TEST idle passed");

        InputManager.movement?.Invoke(Vector2.up);
        yield return Wait(0.5f);
        RequireState(animator, "Locomotion");
        Require(player.NormalizedSpeed > 0.9f && animator.GetFloat("Speed") > 0.9f, "Running must follow movement speed.");
        var arm = animator.transform.Find("Rig/root/hips/spine/chest/upperarm.l");
        var startRotation = arm.localRotation;
        yield return Wait(0.2f);
        Require(Quaternion.Angle(startRotation, arm.localRotation) > 1f, "Running must animate the actual Rogue skeleton.");
        yield return Wait(1f);
        RequireState(animator, "Locomotion");
        Require(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f, "Locomotion must keep looping.");
        Require(animator.transform.localPosition == new Vector3(0, -0.998f, 0), "Animation must preserve the visual offset.");
        Require(!animator.applyRootMotion, "Animation must not move the physics root.");
        Debug.Log("PLAYER_TEST running, bone motion, looping, root ownership passed");

        InputManager.movement?.Invoke(Vector2.zero);
        yield return Wait(0.8f);
        Require(animator.GetFloat("Speed") < 0.05f, "Stopping must blend back to idle.");
        InputManager.jumpStarted?.Invoke();
        yield return Until(() => !player.IsGrounded, "fixed timestep takeoff");
        yield return Until(() => IsState(animator, "Jump"), "jump animation");
        Require(player.VerticalVelocity > 0f, "Jump must play on ascent.");
        yield return Until(() => IsState(animator, "Fall"), "fall animation");
        yield return Until(() => IsState(animator, "Land"), "landing animation");
        yield return Wait(0.5f);
        RequireState(animator, "Locomotion");
        Debug.Log("PLAYER_TEST stop, jump, fall, land, idle passed");

        InputManager.jumpStarted?.Invoke();
        yield return Wait(0.04f);
        InputManager.jumpCanceled?.Invoke();
        yield return Until(() => IsState(animator, "Land"), "short jump landing");
        yield return Wait(0.4f);
        body.position += Vector3.up * 5;
        player.ResetMotion();
        Physics.SyncTransforms();
        yield return Until(() => IsState(animator, "Fall"), "fall without jumping");
        yield return Until(() => player.IsGrounded, "ledge fall landing");
        yield return Wait(0.5f);
        Debug.Log("PLAYER_TEST short jump and falling without jump passed");

        // Both input edges may arrive between physics ticks at high refresh rates.
        InputManager.jumpStarted?.Invoke();
        InputManager.jumpCanceled?.Invoke();
        yield return Until(() => !player.IsGrounded, "tap between physics ticks");
        Require(player.VerticalVelocity < 6f, "Quick taps must apply the release in the same tick.");
        yield return Until(() => player.IsGrounded, "quick tap landing");
        yield return Wait(0.4f);

        player.ApplyKnockback(new Vector3(2, 15, 0));
        yield return Until(() => IsState(animator, "Jump"), "knockback ascent");
        yield return Until(() => IsState(animator, "Fall"), "knockback descent");
        yield return Until(() => player.IsGrounded, "knockback landing");
        yield return Wait(0.5f);
        player.SetMovementEnabled(false);
        yield return Wait(1.2f);
        RequireState(animator, "Death");
        player.SetMovementEnabled(true);
        player.ResetMotion();
        body.position = new Vector3(1000, 5, 1000);
        Physics.SyncTransforms();
        yield return Until(() => player.IsGrounded, "respawn landing");
        yield return Wait(0.5f);
        RequireState(animator, "Locomotion");
        Require(animator.GetFloat("Speed") < 0.05f, "Respawn must not retain running momentum.");
        Debug.Log("PLAYER_TEST knockback, game over, respawn passed");

        var rates = new[] { 30, 144 };
        var speeds = new float[2];
        int previousRate = Application.targetFrameRate;
        int previousVsync = QualitySettings.vSyncCount;
        QualitySettings.vSyncCount = 0;
        for (int i = 0; i < rates.Length; i++)
        {
            Application.targetFrameRate = rates[i];
            player.Teleport(new Vector3(1000, 2, 1000));
            var camera = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            camera.SnapToTarget();
            var snapped = camera.transform.position - player.transform.position;
            Require(snapped.magnitude > 1f && snapped.magnitude < 30f, "Camera must snap to its normal follow offset.");
            yield return Until(() => player.IsGrounded, "framerate test grounding");
            InputManager.movement?.Invoke(Vector2.up);
            yield return Wait(0.6f);
            var position = body.position;
            float fixedStart = Time.fixedTime;
            yield return Wait(1f);
            var displacement = body.position - position;
            displacement.y = 0;
            speeds[i] = displacement.magnitude / (Time.fixedTime - fixedStart);
            Require(Mathf.Abs(speeds[i] - 12f) < 0.3f, "Movement speed must remain 12 at " + rates[i] + " FPS.");
            Debug.Log($"PHYSICS_RATE_OK {rates[i]} FPS: {speeds[i]:F3} units/second");
        }
        Application.targetFrameRate = previousRate;
        QualitySettings.vSyncCount = previousVsync;
        Require(Mathf.Abs(speeds[0] - speeds[1]) < 0.15f, "Movement must agree across render rates.");
    }

    private static bool IsState(Animator animator, string name) => animator.GetCurrentAnimatorStateInfo(0).IsName(name);
    private static void RequireState(Animator animator, string name) => Require(IsState(animator, name), "Expected state " + name);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static IEnumerator Wait(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until) yield return null;
    }

    private static IEnumerator Until(Func<bool> condition, string description)
    {
        float until = Time.time + 4f;
        while (!condition())
        {
            Require(Time.time < until, "Timed out waiting for " + description);
            yield return null;
        }
    }
}
