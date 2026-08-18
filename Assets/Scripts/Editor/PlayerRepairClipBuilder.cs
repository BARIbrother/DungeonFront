#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// P_Repair_1..12 스프라이트로 수리 클립을 심고 PlayerMovement.controller에 연결한다.
public static class PlayerRepairClipBuilder
{
    private const string PlayerArtFolder = "Assets/Art/Player";
    private const string ClipPath = PlayerArtFolder + "/Player_Repair.anim";
    private const string ControllerPath = PlayerArtFolder + "/PlayerMovement.controller";
    private const float FrameDuration = 0.1f;
    private const int FrameCount = 12;

    [MenuItem("DungeonFront/Rebuild Player Repair Clip")]
    public static void RebuildFromMenu()
    {
        Rebuild();
    }

    [InitializeOnLoadMethod]
    private static void RebuildOnceWhenSpritesReady()
    {
        EditorApplication.delayCall += () =>
        {
            if (!HasAllSprites())
            {
                return;
            }

            if (!IsAlreadyWired())
            {
                Rebuild();
            }
        };
    }

    public static void Rebuild()
    {
        Sprite[] frames = LoadRepairFrames();
        if (frames == null)
        {
            Debug.LogError("[PlayerRepairClipBuilder] P_Repair_1..12 스프라이트를 찾지 못했습니다.");
            return;
        }

        AnimationClip clip = WriteRepairClip(frames);
        if (clip == null)
        {
            return;
        }

        WireAnimatorController(clip);
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerRepairClipBuilder] 수리 클립을 연결했습니다.");
    }

    private static bool HasAllSprites()
    {
        for (int i = 1; i <= FrameCount; i++)
        {
            if (LoadSprite(PlayerArtFolder + "/P_Repair_" + i + ".png") == null)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAlreadyWired()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (clip == null || controller == null)
        {
            return false;
        }

        if (!HasRepairParameter(controller))
        {
            return false;
        }

        AnimatorState repairState = FindState(controller.layers[0].stateMachine, "Repair");
        if (repairState == null || repairState.motion != clip)
        {
            return false;
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.propertyName != "m_Sprite")
            {
                continue;
            }

            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys == null || keys.Length < FrameCount)
            {
                return false;
            }

            Sprite first = keys[0].value as Sprite;
            if (first == null)
            {
                return false;
            }

            return first.name.StartsWith("P_Repair_");
        }

        return false;
    }

    private static Sprite[] LoadRepairFrames()
    {
        var frames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            frames[i] = LoadSprite(PlayerArtFolder + "/P_Repair_" + (i + 1) + ".png");
            if (frames[i] == null)
            {
                return null;
            }
        }

        return frames;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null)
        {
            return null;
        }

        foreach (Object asset in assets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static AnimationClip WriteRepairClip(Sprite[] frames)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Player_Repair" };
            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        var keys = new List<ObjectReferenceKeyframe>(frames.Length);
        for (int i = 0; i < frames.Length; i++)
        {
            keys.Add(new ObjectReferenceKeyframe
            {
                time = i * FrameDuration,
                value = frames[i]
            });
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.stopTime = frames.Length * FrameDuration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void WireAnimatorController(AnimationClip clip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[PlayerRepairClipBuilder] PlayerMovement.controller 없음.");
            return;
        }

        if (!HasRepairParameter(controller))
        {
            controller.AddParameter("Repair", AnimatorControllerParameterType.Trigger);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState repairState = FindState(stateMachine, "Repair");
        if (repairState == null)
        {
            repairState = stateMachine.AddState("Repair", new Vector3(320f, 240f, 0f));
        }

        repairState.motion = clip;

        if (!HasAnyStateTransitionTo(stateMachine, repairState))
        {
            AnimatorStateTransition any = stateMachine.AddAnyStateTransition(repairState);
            any.hasExitTime = false;
            any.hasFixedDuration = true;
            any.duration = 0f;
            any.canTransitionToSelf = false;
            any.AddCondition(AnimatorConditionMode.If, 0f, "Repair");
        }

        AnimatorState idleState = FindState(stateMachine, "Idle");
        if (idleState != null && !HasTransitionTo(repairState, idleState))
        {
            AnimatorStateTransition exit = repairState.AddTransition(idleState);
            exit.hasExitTime = true;
            exit.hasFixedDuration = true;
            exit.exitTime = 1f;
            exit.duration = 0f;
        }

        EditorUtility.SetDirty(controller);
    }

    private static bool HasRepairParameter(AnimatorController controller)
    {
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == "Repair"
                && parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }

        return false;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state != null && states[i].state.name == name)
            {
                return states[i].state;
            }
        }

        return null;
    }

    private static bool HasAnyStateTransitionTo(AnimatorStateMachine stateMachine, AnimatorState dst)
    {
        AnimatorStateTransition[] transitions = stateMachine.anyStateTransitions;
        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i] != null && transitions[i].destinationState == dst)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTransitionTo(AnimatorState from, AnimatorState dst)
    {
        AnimatorStateTransition[] transitions = from.transitions;
        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i] != null && transitions[i].destinationState == dst)
            {
                return true;
            }
        }

        return false;
    }
}
#endif
