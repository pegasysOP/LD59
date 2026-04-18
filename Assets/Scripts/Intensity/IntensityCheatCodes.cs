using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Developer-only cheat for driving <see cref="IntensityManager"/> from the keyboard.
/// Ctrl + '+' (plus / equals / numpad plus) jumps UP one <see cref="IntensityLevel"/>.
/// Ctrl + '-' (minus / numpad minus) jumps DOWN one level.
/// Ctrl + Shift + I toggles a minimal IMGUI debug overlay showing the current
/// intensity level and live heartbeat volume multiplier.
/// On each change the new intensity is snapped to the target zone's lower bound plus
/// <see cref="ExtraAboveZoneBase"/> so ambient decay can't immediately drop you back out.
/// Compiled to a no-op outside the Unity Editor so it cannot leak into shipped builds.
/// </summary>
public class IntensityCheatCodes : MonoBehaviour
{
#if UNITY_EDITOR
    private const bool Enabled = true;
    private const float ExtraAboveZoneBase = 0.10f;
    private const bool RequireCtrlModifier = true;

    private bool showOverlay;
    private GUIStyle overlayHeaderStyle;
    private GUIStyle overlayLineStyle;
    private Texture2D overlayBgTex;

    private void Update()
    {
        if (!Enabled) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
        bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

        if (ctrl && shift && kb.iKey.wasPressedThisFrame)
        {
            showOverlay = !showOverlay;
            Debug.Log($"[IntensityCheat] overlay {(showOverlay ? "ON" : "OFF")}");
            return;
        }

        IntensityManager manager = IntensityManager.Instance;
        if (manager == null) return;

        if (RequireCtrlModifier && !ctrl)
            return;

        if (IsPlusPressedThisFrame(kb))
            StepLevel(manager, +1);
        else if (IsMinusPressedThisFrame(kb))
            StepLevel(manager, -1);
    }

    private void OnGUI()
    {
        if (!Enabled || !showOverlay) return;

        IntensityManager manager = IntensityManager.Instance;
        if (manager == null) return;

        EnsureOverlayStyles();

        const float pad = 10f;
        const float width = 260f;
        const float height = 86f;
        Rect rect = new Rect(pad, pad, width, height);

        GUI.color = Color.white;
        GUI.DrawTexture(rect, overlayBgTex, ScaleMode.StretchToFill);

        GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
        GUILayout.Label("Intensity Debug", overlayHeaderStyle);

        float intensity = manager.CurrentIntensity;
        GUILayout.Label($"Level:  {manager.CurrentLevel}  ({intensity:0.00})", overlayLineStyle);

        HeartbeatSoundPlayer hb = HeartbeatSoundPlayer.Instance;
        string volText = hb != null
            ? hb.CurrentVolumeMultiplier.ToString("0.00")
            : "-- (no player)";
        GUILayout.Label($"Heartbeat Vol:  {volText}", overlayLineStyle);
        GUILayout.EndArea();
    }

    private void EnsureOverlayStyles()
    {
        if (overlayBgTex == null)
        {
            overlayBgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            overlayBgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            overlayBgTex.Apply();
            overlayBgTex.hideFlags = HideFlags.HideAndDontSave;
        }
        if (overlayHeaderStyle == null)
        {
            overlayHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) },
            };
        }
        if (overlayLineStyle == null)
        {
            overlayLineStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
            };
        }
    }

    private void OnDestroy()
    {
        if (overlayBgTex != null)
        {
            DestroyImmediate(overlayBgTex);
            overlayBgTex = null;
        }
    }

    private static bool IsPlusPressedThisFrame(Keyboard kb)
    {
        // '+' on the main row is Shift+'='. We accept '=' as the unshifted plus
        // since that's what users typically hit for "Ctrl plus" in editors.
        return kb.equalsKey.wasPressedThisFrame
            || kb.numpadPlusKey.wasPressedThisFrame;
    }

    private static bool IsMinusPressedThisFrame(Keyboard kb)
    {
        return kb.minusKey.wasPressedThisFrame
            || kb.numpadMinusKey.wasPressedThisFrame;
    }

    private void StepLevel(IntensityManager manager, int direction)
    {
        int current = (int)manager.CurrentLevel;
        int target = Mathf.Clamp(current + direction, (int)IntensityLevel.Calm, (int)IntensityLevel.Overload);

        IntensityLevel targetLevel = (IntensityLevel)target;
        float zoneBase = ZoneBase(targetLevel);
        float desired = Mathf.Clamp01(zoneBase + ExtraAboveZoneBase);
        manager.SetIntensity(desired);

        Debug.Log($"[IntensityCheat] {(direction > 0 ? "UP" : "DOWN")} -> {targetLevel} ({desired:0.00})");
    }

    private static float ZoneBase(IntensityLevel level)
    {
        switch (level)
        {
            case IntensityLevel.Overload: return 0.75f;
            case IntensityLevel.Intense: return 0.50f;
            case IntensityLevel.Elevated: return 0.25f;
            case IntensityLevel.Calm:
            default: return 0.00f;
        }
    }
#endif
}
