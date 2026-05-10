using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DemonDogMeshySetup
{
    private const string CharacterPath = "Assets/Models/Characters/MeshyQuadruped/Meshy_AI_quadruped_Character_output.fbx";
    private const string WalkPath = "Assets/Models/Characters/MeshyQuadruped/Meshy_AI_quadruped_Animation_Walking_frame_rate_60.fbx";
    private const string ControllerPath = "Assets/Animations/DungeonDogAnimator.controller";

    [MenuItem("Labyrinth/Demon Dog/Setup Meshy Walking Animation")]
    public static void Setup()
    {
        ModelImporter characterImporter = AssetImporter.GetAtPath(CharacterPath) as ModelImporter;
        ModelImporter walkImporter = AssetImporter.GetAtPath(WalkPath) as ModelImporter;
        if (characterImporter == null || walkImporter == null)
        {
            Debug.LogError("[DemonDogMeshySetup] Meshy FBX assets were not found in Assets/Models/Characters/MeshyQuadruped.");
            return;
        }

        characterImporter.animationType = ModelImporterAnimationType.Generic;
        characterImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        characterImporter.importAnimation = true;
        characterImporter.SaveAndReimport();

        Avatar characterAvatar = AssetDatabase.LoadAllAssetsAtPath(CharacterPath).OfType<Avatar>().FirstOrDefault();
        walkImporter.animationType = ModelImporterAnimationType.Generic;
        walkImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        walkImporter.sourceAvatar = characterAvatar;
        walkImporter.importAnimation = true;
        walkImporter.SaveAndReimport();

        AnimationClip walkClip = AssetDatabase.LoadAllAssetsAtPath(WalkPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal));

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || walkClip == null)
        {
            Debug.LogError("[DemonDogMeshySetup] Animator controller or walking clip could not be loaded.");
            return;
        }

        AnimatorState runState = controller.layers[0].stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state.name == "Run" || state.name == "WalkRun");

        if (runState == null)
        {
            Debug.LogError("[DemonDogMeshySetup] Could not find Run state in DungeonDogAnimator.controller.");
            return;
        }

        runState.name = "Run";
        runState.motion = walkClip;
        runState.speed = 1.2f;

        SetupSceneDog(characterAvatar, controller);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DemonDogMeshySetup] Assigned Meshy walking clip '{walkClip.name}' to DemonDog Run state.");
    }

    private static void SetupSceneDog(Avatar characterAvatar, AnimatorController controller)
    {
        DemonDogController dog = Object.FindAnyObjectByType<DemonDogController>();
        if (dog == null)
        {
            return;
        }

        Transform oldModel = dog.transform.Find("DogModel");
        if (oldModel != null)
        {
            oldModel.gameObject.SetActive(false);
        }

        Transform meshyModel = dog.transform.Find("MeshyDogModel");
        if (meshyModel == null)
        {
            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(characterPrefab, dog.transform) as GameObject;
            if (instance != null)
            {
                instance.name = "MeshyDogModel";
                meshyModel = instance.transform;
            }
        }

        if (meshyModel != null)
        {
            meshyModel.localPosition = Vector3.zero;
            meshyModel.localRotation = Quaternion.identity;
            meshyModel.localScale = Vector3.one * 120f;

            Animator nestedAnimator = meshyModel.GetComponent<Animator>();
            if (nestedAnimator != null)
            {
                Object.DestroyImmediate(nestedAnimator);
            }

            dog.modelRoot = meshyModel;
            dog.useProceduralRunAnimation = false;
            EditorUtility.SetDirty(dog);
        }

        Animator animator = dog.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = controller;
            animator.avatar = characterAvatar;
            animator.applyRootMotion = false;
            EditorUtility.SetDirty(animator);
        }

        EditorSceneManager.MarkSceneDirty(dog.gameObject.scene);
        EditorSceneManager.SaveScene(dog.gameObject.scene);
    }
}
