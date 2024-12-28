using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core.GameInitialization
{
    public class SimpleLevelManager : MonoBehaviour
    {
        [Header("Player Settings")]
        [Tooltip("The UFPS Player Prefab to spawn")] 
        public GameObject UFPSPlayerPrefab;

        [Tooltip("The spawn point for the player")] 
        public Transform InitialSpawnPoint;

        private GameObject _spawnedPlayer;

        void Start()
        {
            if (UFPSPlayerPrefab == null)
            {
                Debug.LogError("UFPS Player Prefab is not assigned!");
                return;
            }

            SpawnPlayer();
        }

        private void SpawnPlayer()
        {
            // Check if a spawn point is set
            Vector3 spawnPosition = InitialSpawnPoint != null ? InitialSpawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = InitialSpawnPoint != null ? InitialSpawnPoint.rotation : Quaternion.identity;

            // Instantiate the player prefab
            _spawnedPlayer = Instantiate(UFPSPlayerPrefab, spawnPosition, spawnRotation);

            // Optional: Validate if the MoreMountains Character component is present
            var characterComponent = _spawnedPlayer.GetComponent<MoreMountains.TopDownEngine.Character>();
            if (characterComponent == null)
            {
                Debug.LogWarning("Spawned player does not have a MoreMountains Character component.");
            }

            Debug.Log("Player spawned successfully at " + spawnPosition);
        }

        public void RespawnPlayer()
        {
            if (_spawnedPlayer != null)
            {
                Destroy(_spawnedPlayer);
            }

            SpawnPlayer();
        }

        public void LoadLevel(string levelName)
        {
            if (string.IsNullOrEmpty(levelName))
            {
                Debug.LogError("Level name is null or empty!");
                return;
            }

            SceneManager.LoadScene(levelName);
        }
    }
}
