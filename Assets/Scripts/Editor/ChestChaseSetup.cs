using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public static class ChestChaseSetup
{
    [MenuItem("Arena/Setup Selected Chest")]
    private static void Setup()
    {
        GameObject chest = Selection.activeGameObject;
        if (EditorApplication.isPlaying || chest == null || EditorUtility.IsPersistent(chest))
        {
            Debug.LogWarning("Stop Play Mode and select the root chest object in Hierarchy.");
            return;
        }

        const string sourcePath = "Assets/RPGMonsterPartnersPBRPolyart/Animators/ChestMonster.controller";
        AnimatorController source = AssetDatabase.LoadAssetAtPath<AnimatorController>(sourcePath);
        Animator animator = chest.GetComponentInChildren<Animator>();
        if (source == null || animator == null)
        {
            Debug.LogError("Select the chest root with an Animator. Source ChestMonster controller is required.");
            return;
        }

        Motion idle = null;
        Motion walk = null;
        foreach (ChildAnimatorState child in source.layers[0].stateMachine.states)
        {
            if (child.state.name == "IdleNormal") idle = child.state.motion;
            if (child.state.name == "WalkFWD") walk = child.state.motion;
        }
        if (idle == null || walk == null)
        {
            Debug.LogError("IdleNormal or WalkFWD clip was not found. No changes made.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Game"))
            AssetDatabase.CreateFolder("Assets", "Game");
        if (!AssetDatabase.IsValidFolder("Assets/Game/Animations"))
            AssetDatabase.CreateFolder("Assets/Game", "Animations");

        const string controllerPath = "Assets/Game/Animations/ChestChase.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle", new Vector3(250f, 100f, 0f));
            idleState.motion = idle;
            AnimatorState walkState = machine.AddState("Walk", new Vector3(250f, 250f, 0f));
            walkState.motion = walk;
            machine.defaultState = idleState;

            AnimatorStateTransition startWalk = idleState.AddTransition(walkState);
            startWalk.hasExitTime = false;
            startWalk.hasFixedDuration = true;
            startWalk.duration = 0.15f;
            startWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            AnimatorStateTransition stopWalk = walkState.AddTransition(idleState);
            stopWalk.hasExitTime = false;
            stopWalk.hasFixedDuration = true;
            stopWalk.duration = 0.15f;
            stopWalk.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }

        Undo.RecordObject(animator, "Configure chest animation");
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

        NavMeshAgent agent = chest.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = Undo.AddComponent<NavMeshAgent>(chest);
            agent.radius = 0.5f;
            agent.height = 1.5f;
            agent.speed = 2.5f;
            agent.stoppingDistance = 1.8f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
        }
        if (chest.GetComponent<EnemyChase>() == null)
            Undo.AddComponent<EnemyChase>(chest);

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = chest;
        Debug.Log("Chest chase ready. Assign EnemyChase Target, bake a NavMeshSurface, and place the chest on the baked surface.", chest);
    }
}