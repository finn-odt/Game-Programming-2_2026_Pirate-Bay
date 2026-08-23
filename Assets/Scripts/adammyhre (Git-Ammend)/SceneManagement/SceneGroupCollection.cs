using UnityEngine;

namespace Systems.SceneManagement
{
    [CreateAssetMenu(
        fileName = "SceneGroupCollection",
        menuName = "Scene Management/Scene Group Collection")]
    public class SceneGroupCollection : ScriptableObject
    {
        [SerializeField] private SceneGroup[] sceneGroups;

        public SceneGroup[] SceneGroups => sceneGroups;
        public int Count => sceneGroups?.Length ?? 0;

        public SceneGroup this[int index] => sceneGroups[index];
    }
}