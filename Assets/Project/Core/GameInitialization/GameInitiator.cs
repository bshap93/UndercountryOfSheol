// GameInitiator.cs

using System.Threading.Tasks;
using DunGen;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using Project.Core.SaveSystem;
using Project.Gameplay.DungeonGeneration;
using Project.Gameplay.Enemy;
using Project.Gameplay.Player;
using UnityEngine;

namespace Project.Core.GameInitialization
{
    public class GameInitiator : MonoBehaviour, MMEventListener<MMCameraEvent>
    {
        static GameInitiator _instance;

        public float enemySpawnRate;
        RuntimeDungeon _runtimeDungeon;
        NewSaveManager _saveManager;
        NewDungeonManager dungeonManager;

        void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            

            // Find references in our prefab structure
            dungeonManager = GetComponentInChildren<NewDungeonManager>();
            _runtimeDungeon = GetComponentInChildren<RuntimeDungeon>();

            if (dungeonManager == null || _runtimeDungeon == null)
                Debug.Log("DungeonManager or RuntimeDungeon not found in prefab structure.");

            // Check if NewSaveManager is already in the scene
            _saveManager = NewSaveManager.Instance;
            if (_saveManager == null) _saveManager = gameObject.AddComponent<NewSaveManager>();
        }


        async void Start()
        {
            await InitializeCore();
        }

        void OnEnable()
        {
            // Listen for CharacterSwitch events
            this.MMEventStartListening();
        }

        void OnDisable()
        {
            this.MMEventStopListening();
        }

        public void OnMMEvent(MMCameraEvent eventType)
        {
            if (eventType.EventType == MMCameraEventTypes.SetTargetCharacter)
            {
                MMGameEvent.Trigger("SaveInventory");
                ApplyCharacterCreationDataToPlayer(eventType.TargetCharacter.gameObject);

                SpawnEnemiesIfPossible(eventType.TargetCharacter.gameObject);
            }
        }
        public void OnMMEvent(TopDownEngineEvent engineEvent)
        {
            if (engineEvent.EventType == TopDownEngineEventTypes.CharacterSwitch)
            {
                // Apply character creation data to the player
                NewSaveManager.Instance.ApplyCharacterCreationDataToPlayer();
                Debug.Log("CharacterSwitch event received. Applied CharacterCreationData to PlayerStats.");
            }
        }

        async Task InitializeCore()
        {
            var hasSave = await LoadLastGame(); // Attempt to load the last save
            if (SaveStateManager.Instance == null)
                Debug.LogWarning("SaveStateManager not found in the scene.");
            else
                SaveStateManager.Instance.IsSaveLoaded = hasSave;

            if (!hasSave)
            {
                await StartNewGame();
            }
            else
            {
                Debug.Log("Valid save loaded.");
            }
        }

        async Task<bool> LoadLastGame()
        {
            var saveLoaded = _saveManager.LoadGame();
            return await Task.FromResult(saveLoaded);
        }


        async Task StartNewGame()
        {
            if (dungeonManager != null)
            {
            var seed = Random.Range(0, int.MaxValue);
            await dungeonManager.GenerateNewDungeon(seed);
            }

            // Spawn the player
            // var initialSpawnPoint = FindObjectOfType<CheckPoint>();
            // if (initialSpawnPoint == null) Debug.LogError("No CheckPoint found for initial spawn!");
        }

        void ApplyCharacterCreationDataToPlayer(GameObject playerGameObject)
        {
            if (playerGameObject != null)
            {
                var playerStats = playerGameObject.GetComponent<PlayerStats>();
                if (playerStats != null)
                    playerStats.Initialize(NewSaveManager.Instance.CurrentSave.characterCreationData);
                else
                    Debug.LogError("PlayerStats component not found on Player GameObject.");
            }
            else
            {
                Debug.LogError("Player GameObject not found.");
            }
        }

        void SpawnEnemiesIfPossible(GameObject playerGameObject)
        {
            if (playerGameObject != null)
            {
                var enemySpawners = FindObjectsOfType<EnemySpawnPoint>();
                var randomPathGenerator = gameObject.AddComponent<RandomPathGenerator>();

                Debug.Log("Spawning enemies...");

                foreach (var spawner in enemySpawners)
                {
                    // Return early at the rate of the  EnemySpawnRate randomly
                    if (Random.Range(0f, 1f) > enemySpawnRate) continue;


                    var enemyClass = spawner.GetComponent<EnemySpawnPoint>().EnemyClass;
                    var enemyPrefab = enemyClass.GetRandomEnemyPrefab();

                    // Spawn the enemy
                    Instantiate(enemyPrefab, spawner.transform.position, Quaternion.identity);

                    Debug.Log("Enemy spawned.");
                }
            }
        }
    }
}
