// Assets/Editor/CatAnimatorSetup.cs

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class CatAnimatorSetup : Editor
{
    private const string SPRITE_FOLDER = "Assets/PNG/Character/Cat/Sprites/Cat-1";
    private const string OUTPUT_FOLDER = "Assets/PNG/Character/Cat/Animation";
    private const string OUTPUT_PATH   = "Assets/PNG/Character/Cat/Animation/Cat-1-Auto.controller";

    // ── Tốc độ animation (frame/giây) — chỉnh tuỳ ý ────────────────────
    private const float FPS_RUN  = 12f;
    private const float FPS_IDLE = 8f;
    private const float FPS_MISC = 8f;
    // ─────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Setup Cat Animator")]
    public static void CreateCatAnimator()
    {
        // ── BƯỚC 1: Tạo AnimationClip từ Sprite frames ───────────────────
        AnimationClip clipIdle       = CreateClipFromSprites("Cat-1-Idle",      FPS_IDLE, loop: true);
        AnimationClip clipRun        = CreateClipFromSprites("Cat-1-Run",       FPS_RUN,  loop: true);
        AnimationClip clipMeow       = CreateClipFromSprites("Cat-1-Meow",      FPS_MISC, loop: false);
        AnimationClip clipItch       = CreateClipFromSprites("Cat-1-Itch",      FPS_MISC, loop: false);
        AnimationClip clipLicking    = CreateClipFromSprites("Cat-1-Licking 1", FPS_MISC, loop: false);
        AnimationClip clipSitting    = CreateClipFromSprites("Cat-1-Sitting",   FPS_MISC, loop: true);
        AnimationClip clipStretching = CreateClipFromSprites("Cat-1-Stretching",FPS_MISC, loop: false);
        AnimationClip clipWalk       = CreateClipFromSprites("Cat-1-Walk",      FPS_MISC, loop: true);
        AnimationClip clipLaying     = CreateClipFromSprites("Cat-1-Laying",    FPS_MISC, loop: true);

        if (clipIdle == null || clipRun == null || clipMeow == null)
        {
            Debug.LogError("[CatAnimatorSetup] Thiếu clip bắt buộc! Dừng lại.");
            return;
        }

        // ── BƯỚC 2: Xóa controller cũ ────────────────────────────────────
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(OUTPUT_PATH) != null)
        {
            AssetDatabase.DeleteAsset(OUTPUT_PATH);
            AssetDatabase.Refresh();
        }

        // ── BƯỚC 3: Tạo controller ────────────────────────────────────────
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(OUTPUT_PATH);

        // ── BƯỚC 4: Parameters ───────────────────────────────────────────
        ctrl.AddParameter("isRunning",  AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("isCatching", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("idleAction", AnimatorControllerParameterType.Int);
        // idleAction: 0=Idle, 1=Itch, 2=Licking, 3=Sitting, 4=Stretching, 5=Walk, 6=Laying

        // ── BƯỚC 5: States ───────────────────────────────────────────────
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        AnimatorState stateIdle       = AddState(sm, "Idle",       clipIdle,       new Vector3(250,    0));
        AnimatorState stateRun        = AddState(sm, "Run",        clipRun,        new Vector3(550,    0));
        AnimatorState stateMeow       = AddState(sm, "Meow",       clipMeow,       new Vector3(550, -150));
        AnimatorState stateItch       = AddState(sm, "Itch",       clipItch,       new Vector3(250,  150));
        AnimatorState stateLicking    = AddState(sm, "Licking",    clipLicking,    new Vector3(250,  270));
        AnimatorState stateSitting    = AddState(sm, "Sitting",    clipSitting,    new Vector3(250,  390));
        AnimatorState stateStretching = AddState(sm, "Stretching", clipStretching, new Vector3(250,  510));
        AnimatorState stateWalk       = AddState(sm, "Walk",       clipWalk,       new Vector3(250,  630));
        AnimatorState stateLaying     = AddState(sm, "Laying",     clipLaying,     new Vector3(250,  750));

        sm.defaultState = stateIdle;

        // ── BƯỚC 6: Transitions ──────────────────────────────────────────

        // Any State → Run (ngắt tất cả khi có cá)
        var anyToRun = sm.AddAnyStateTransition(stateRun);
        anyToRun.hasExitTime = false; anyToRun.duration = 0.05f;
        anyToRun.canTransitionToSelf = false;
        anyToRun.AddCondition(AnimatorConditionMode.If, 0, "isRunning");

        // Any State → Meow (khi bắt được cá)
        var anyToMeow = sm.AddAnyStateTransition(stateMeow);
        anyToMeow.hasExitTime = false; anyToMeow.duration = 0.05f;
        anyToMeow.canTransitionToSelf = false;
        anyToMeow.AddCondition(AnimatorConditionMode.If, 0, "isCatching");

        // Run → Idle
        AddBoolTransition(stateRun, stateIdle, "isRunning", false, hasExitTime: false);

        // Meow → Idle (chờ hết clip)
        var meowToIdle = stateMeow.AddTransition(stateIdle);
        meowToIdle.hasExitTime = true; meowToIdle.exitTime = 1f; meowToIdle.duration = 0.05f;
        meowToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isCatching");

        // Idle → idle actions
        AddIntTransition(stateIdle, stateItch,       "idleAction", 1);
        AddIntTransition(stateIdle, stateLicking,    "idleAction", 2);
        AddIntTransition(stateIdle, stateSitting,    "idleAction", 3);
        AddIntTransition(stateIdle, stateStretching, "idleAction", 4);
        AddIntTransition(stateIdle, stateWalk,       "idleAction", 5);
        AddIntTransition(stateIdle, stateLaying,     "idleAction", 6);

        // idle actions → Idle sau khi xong
        AddExitToIdle(stateItch,       stateIdle);
        AddExitToIdle(stateLicking,    stateIdle);
        AddExitToIdle(stateSitting,    stateIdle);
        AddExitToIdle(stateStretching, stateIdle);
        AddExitToIdle(stateWalk,       stateIdle);
        AddExitToIdle(stateLaying,     stateIdle);

        // ── BƯỚC 7: Lưu ─────────────────────────────────────────────────
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── BƯỚC 8: Tự động gán vào object Cat-1 ────────────────────────
        GameObject catObj = GameObject.Find("Cat-1");
        if (catObj != null)
        {
            Animator anim = catObj.GetComponent<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = ctrl;
                EditorUtility.SetDirty(catObj);
                Debug.Log("[CatAnimatorSetup] ✅ Đã gán controller vào Cat-1!");
            }
            else
                Debug.LogWarning("[CatAnimatorSetup] Cat-1 không có Animator component!");
        }
        else
            Debug.LogWarning("[CatAnimatorSetup] Không tìm thấy object 'Cat-1' trong scene!");

        Debug.Log($"[CatAnimatorSetup] ✅ Hoàn tất! Controller: {OUTPUT_PATH}");
    }

    // ── TẠO AnimationClip TỪ SPRITE FRAMES trong PNG ────────────────────
    // Đây là phần quan trọng nhất: load tất cả Sprite từ PNG rồi key từng frame
    private static AnimationClip CreateClipFromSprites(string pngName, float fps, bool loop)
    {
        string pngPath = $"{SPRITE_FOLDER}/{pngName}.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(pngPath);

        // Lọc ra các Sprite, sắp xếp theo tên (_0, _1, _2...)
        List<Sprite> sprites = new List<Sprite>();
        foreach (var a in assets)
            if (a is Sprite s) sprites.Add(s);

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[CatAnimatorSetup] Không tìm thấy sprite nào trong: {pngPath}");
            return null;
        }

        // Sắp xếp theo số thứ tự cuối tên (Cat-1-Idle_0, _1, _2...)
        sprites.Sort((a, b) =>
        {
            int numA = ExtractIndex(a.name);
            int numB = ExtractIndex(b.name);
            return numA.CompareTo(numB);
        });

        // Tạo AnimationClip
        AnimationClip clip = new AnimationClip();
        clip.frameRate = fps;

        // Tạo keyframes cho property "m_Sprite" của SpriteRenderer
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count];
        float frameDuration = 1f / fps;
        for (int i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time  = i * frameDuration,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        // Cài loop setting
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Lưu clip vào folder Animation
        string savePath = $"{OUTPUT_FOLDER}/{pngName}.anim";

        // Xóa file cũ nếu có
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath) != null)
            AssetDatabase.DeleteAsset(savePath);

        AssetDatabase.CreateAsset(clip, savePath);
        Debug.Log($"[CatAnimatorSetup]  Tạo clip: {savePath} ({sprites.Count} frames, loop={loop})");

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);
    }

    // Lấy số index từ tên sprite (Cat-1-Idle_7 → 7)
    private static int ExtractIndex(string name)
    {
        int underscore = name.LastIndexOf('_');
        if (underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int idx))
            return idx;
        return 0;
    }
    // ─────────────────────────────────────────────────────────────────────

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 pos)
    {
        AnimatorState state = sm.AddState(name, pos);
        if (clip != null) state.motion = clip;
        return state;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value, bool hasExitTime)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = hasExitTime; t.duration = 0.05f;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    private static void AddIntTransition(AnimatorState from, AnimatorState to, string param, int value)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.Equals, value, param);
    }

    private static void AddExitToIdle(AnimatorState from, AnimatorState idle)
    {
        var t = from.AddTransition(idle);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.Equals, 0, "idleAction");
    }

    // ── DEBUG helper giữ lại để dùng khi cần ────────────────────────────
    [MenuItem("Tools/Debug - Scan Cat Folders")]
    public static void ScanFolders()
    {
        foreach (string guid in AssetDatabase.FindAssets("", new[] { SPRITE_FOLDER }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"[Sprites] {path}");
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                Debug.Log($"    {a.name}  ({a.GetType().Name})");
        }
    }
}