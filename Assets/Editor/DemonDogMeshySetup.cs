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
    private const string DemonDogPrefabPath = "Assets/Prefabs/DemonDog.prefab";

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

        GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        if (characterPrefab == null)
        {
            Debug.LogError($"[DemonDogMeshySetup] Meshy character prefab could not be loaded from {CharacterPath}.");
            return;
        }

        SetupPrefabDog(characterPrefab, characterAvatar, controller);
        SetupSceneDogs(characterPrefab, characterAvatar, controller);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DemonDogMeshySetup] Assigned Meshy walking clip '{walkClip.name}' to DemonDog Run state.");
    }

    private static void SetupPrefabDog(GameObject characterPrefab, Avatar characterAvatar, AnimatorController controller)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(DemonDogPrefabPath);
        try
        {
            DemonDogController dog = prefabRoot.GetComponent<DemonDogController>();
            if (dog == null)
            {
                Debug.LogError($"[DemonDogMeshySetup] DemonDogController was not found on {DemonDogPrefabPath}.");
                return;
            }

            ConfigureDog(dog, characterPrefab, characterAvatar, controller, true);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, DemonDogPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void SetupSceneDogs(GameObject characterPrefab, Avatar characterAvatar, AnimatorController controller)
    {
        DemonDogController[] dogs = Object.FindObjectsByType<DemonDogController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        if (dogs.Length == 0)
        {
            return;
        }

        foreach (DemonDogController dog in dogs)
        {
            ConfigureDog(dog, characterPrefab, characterAvatar, controller, false);
            EditorSceneManager.MarkSceneDirty(dog.gameObject.scene);
        }

        EditorSceneManager.SaveOpenScenes();
    }

    private static void ConfigureDog(
        DemonDogController dog,
        GameObject characterPrefab,
        Avatar characterAvatar,
        AnimatorController controller,
        bool editingPrefabAsset)
    {
        Transform oldModel = dog.transform.Find("DogModel");
        if (oldModel != null)
        {
            oldModel.gameObject.SetActive(false);

            Animator oldAnimator = oldModel.GetComponent<Animator>();
            if (oldAnimator != null)
            {
                Object.DestroyImmediate(oldAnimator, true);
            }

            EditorUtility.SetDirty(oldModel.gameObject);
        }

        Transform meshyModel = dog.transform.Find("MeshyDogModel");
        if (meshyModel == null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(characterPrefab, dog.transform) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(characterPrefab, dog.transform);
            }

            if (instance != null)
            {
                instance.name = "MeshyDogModel";
                meshyModel = instance.transform;
            }
            else
            {
                Debug.LogError("[DemonDogMeshySetup] Failed to instantiate MeshyDogModel.");
            }
        }

        if (meshyModel != null)
        {
            meshyModel.localPosition = Vector3.zero;
            meshyModel.localRotation = Quaternion.identity;
            meshyModel.localScale = Vector3.one * 120f;

            dog.modelRoot = meshyModel;
            dog.useProceduralRunAnimation = false;
            EditorUtility.SetDirty(meshyModel.gameObject);
            EditorUtility.SetDirty(dog);
        }

        Animator parentAnimator = dog.GetComponent<Animator>();
        if (parentAnimator != null)
        {
            parentAnimator.applyRootMotion = false;
            parentAnimator.enabled = false;
            EditorUtility.SetDirty(parentAnimator);
        }

        if (meshyModel != null)
        {
            Animator modelAnimator = meshyModel.GetComponent<Animator>();
            if (modelAnimator == null)
            {
                modelAnimator = meshyModel.gameObject.AddComponent<Animator>();
            }

            modelAnimator.runtimeAnimatorController = controller;
            modelAnimator.avatar = characterAvatar;
            modelAnimator.applyRootMotion = false;
            modelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            modelAnimator.updateMode = AnimatorUpdateMode.Normal;
            modelAnimator.enabled = true;

            dog.animator = modelAnimator;
            EditorUtility.SetDirty(modelAnimator);
            EditorUtility.SetDirty(dog);
        }

        if (editingPrefabAsset)
        {
            EditorUtility.SetDirty(dog.gameObject);
        }
    }
}
