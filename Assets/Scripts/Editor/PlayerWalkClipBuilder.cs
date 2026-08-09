#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// P_MoveForth / P_MoveBack 스프라이트로 플레이어 walk·idle 클립을 다시 심는다.
public static class PlayerWalkClipBuilder
{
    private const string PlayerArtFolder = "Assets/Art/Player";
    private const string WalkBackClipPath = PlayerArtFolder + "/Player_walk_back.anim";
    private const string WalkFrontClipPath = PlayerArtFolder + "/Player_walk_front.anim";
    private const string WalkLeftClipPath = PlayerArtFolder + "/Player_walk_left.anim";
    private const string WalkRightClipPath = PlayerArtFolder + "/Player_walk_right.anim";
    private const string IdleClipPath = PlayerArtFolder + "/Player_Idle.anim";
    private const string IdleBackClipPath = PlayerArtFolder + "/Player_Idle_back.anim";
    private const float FrameDuration = 0.1f;

    [MenuItem("DungeonFront/Rebuild Player Walk Clips")]
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

            AnimationClip leftClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkLeftClipPath);
            if (leftClip == null)
            {
                return;
            }

            // 좌우가 이미 MoveForth를 쓰면 스킵한다.
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(leftClip);
            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.propertyName != "m_Sprite")
                {
                    continue;
                }

                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(leftClip, binding);
                if (keys != null && keys.Length >= 6 && keys[0].value != null
                    && keys[0].value.name.StartsWith("P_MoveForth"))
                {
                    // idle도 MoveForth인지 확인
                    AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
                    if (idleClip != null)
                    {
                        EditorCurveBinding[] idleBindings =
                            AnimationUtility.GetObjectReferenceCurveBindings(idleClip);
                        foreach (EditorCurveBinding idleBinding in idleBindings)
                        {
                            if (idleBinding.propertyName != "m_Sprite")
                            {
                                continue;
                            }

                            ObjectReferenceKeyframe[] idleKeys =
                                AnimationUtility.GetObjectReferenceCurve(idleClip, idleBinding);
                            if (idleKeys != null && idleKeys.Length > 0 && idleKeys[0].value != null
                                && idleKeys[0].value.name.StartsWith("P_MoveForth"))
                            {
                                return;
                            }
                        }
                    }

                    break;
                }
            }

            Rebuild();
        };
    }

    private static bool HasAllSprites()
    {
        for (int i = 1; i <= 6; i++)
        {
            if (LoadSprite($"{PlayerArtFolder}/P_MoveForth_{i}.png") == null)
            {
                return false;
            }

            if (LoadSprite($"{PlayerArtFolder}/P_MoveBack_{i}.png") == null)
            {
                return false;
            }
        }

        return true;
    }

    public static void Rebuild()
    {
        Sprite[] forth = LoadSpriteSet("P_MoveForth");
        Sprite[] back = LoadSpriteSet("P_MoveBack");
        if (forth == null || back == null)
        {
            Debug.LogError("[PlayerWalkClipBuilder] P_MoveForth/P_MoveBack 스프라이트를 찾지 못했습니다.");
            return;
        }

        // MoveY=-1(아래)·좌·우 → MoveForth, MoveY=+1(위) → MoveBack
        WriteSpriteClip(WalkBackClipPath, forth);
        WriteSpriteClip(WalkLeftClipPath, forth);
        WriteSpriteClip(WalkRightClipPath, forth);
        WriteSpriteClip(WalkFrontClipPath, back);
        WriteIdleClip(IdleClipPath, forth[0]);
        WriteIdleClip(IdleBackClipPath, back[0]);
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerWalkClipBuilder] walk(상하좌우)·idle(앞/뒤) 클립을 새 스프라이트로 재구성했습니다.");
    }

    private static Sprite[] LoadSpriteSet(string prefix)
    {
        var sprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            sprites[i] = LoadSprite($"{PlayerArtFolder}/{prefix}_{i + 1}.png");
            if (sprites[i] == null)
            {
                return null;
            }
        }

        return sprites;
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
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void WriteSpriteClip(string clipPath, Sprite[] frames)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogError($"[PlayerWalkClipBuilder] 클립 없음: {clipPath}");
            return;
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
        settings.loopTime = true;
        settings.stopTime = frames.Length * FrameDuration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
    }

    private static void WriteIdleClip(string clipPath, Sprite idleSprite)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogError($"[PlayerWalkClipBuilder] 클립 없음: {clipPath}");
            return;
        }

        var keys = new[]
        {
            new ObjectReferenceKeyframe
            {
                time = 0f,
                value = idleSprite
            }
        };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.stopTime = 1f / 60f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
    }
}
#endif
