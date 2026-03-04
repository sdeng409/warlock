#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Warlock.Playable
{
    public static class WarlockPlayableSceneMenu
    {
        [MenuItem("Warlock/Playable/Create Empty Playable Scene")]
        private static void CreatePlayableScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("WarlockPlayableBootstrap");
            bootstrap.AddComponent<WarlockPlayableBootstrap>();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = bootstrap;
            Debug.Log("Warlock playable scene scaffold created. Press Play, then Space to start.");
        }
    }
}
#endif
