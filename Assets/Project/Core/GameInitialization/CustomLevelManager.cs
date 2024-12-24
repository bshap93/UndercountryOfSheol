﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DunGen;
using DunGen;
using MoreMountains.TopDownEngine;
using Project.Core.SaveSystem;
using UnityEngine;

namespace Project.Core.GameInitialization
{
    public class CustomLevelManager : LevelManager
    {
        [Header("Custom Level Manager")] [Tooltip("The ID of the spawn point to use")]
        public string SpawnPointID;
        // Readonly 
        [ReadOnly(true)] public Dungeon generatedDungeon;
        [ReadOnly(true)] public RuntimeDungeon runtimeDungeon;

        protected override void Awake()
        {
            if (InitialSpawnPoint != null)
            {
                base.Awake();
                return;
            }

            runtimeDungeon = FindObjectOfType<RuntimeDungeon>();
            if (runtimeDungeon == null)
            {
                Debug.LogWarning("No RuntimeDungeon found in the scene");
                return;
            }

            runtimeDungeon.Generator.OnGenerationStatusChanged += OnDungeonGenerationStatusChanged;

            Debug.Log("Custom Level Manager Awake");
        }

        protected override void Start()
        {
            // if (InitialSpawnPoint != null)
            // {
            //     base.Start();
            //     return;
            // }

            Debug.Log("Custom Level Manager Start");
        }

        void OnDestroy()
        {
            runtimeDungeon.Generator.OnGenerationStatusChanged -= OnDungeonGenerationStatusChanged;
        }

        void OnDungeonGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
        {
            Debug.Log("Dungeon Generation Status Changed");
            if (status == GenerationStatus.Complete)
            {
                Debug.Log("Dungeon Generation Complete");
                generatedDungeon = FindObjectOfType<Dungeon>();

                if (generatedDungeon == null)
                {
                    Debug.LogWarning("No Dungeon found in the scene");
                    return;
                }

                var initialSpawnPoints = generatedDungeon.GetComponentsInChildren<InitialSpawnPoint>();

                PointsOfEntry = new Transform[initialSpawnPoints.Length];

                for (var i = 0; i < initialSpawnPoints.Length; i++)
                    PointsOfEntry[i] = initialSpawnPoints[i].gameObject.transform;


                if (initialSpawnPoints.Length == 0)
                {
                    Debug.LogWarning("No Checkpoints found in the dungeon");
                    return;
                }

                if (SpawnPointID != null)
                {
                    var spawnPoint = initialSpawnPoints.FirstOrDefault(x => x.SpawnPointID == SpawnPointID);

                    if (spawnPoint != null)
                    {
                        InitialSpawnPoint = spawnPoint.gameObject.GetComponent<CheckPoint>();

                        if (InitialSpawnPoint == null)
                            InitialSpawnPoint = initialSpawnPoints[0].gameObject.AddComponent<CheckPoint>();
                    }
                    else
                    {
                        InitialSpawnPoint = initialSpawnPoints[0].gameObject.AddComponent<CheckPoint>();
                    }
                }
                else
                {
                    InitialSpawnPoint = initialSpawnPoints[0].gameObject.AddComponent<CheckPoint>();
                }


                StartCoroutine(InitializationCoroutine());
            }
        }


        /// <summary>
        ///     Instantiate playable characters based on the ones specified in the PlayerPrefabs list in the LevelManager's
        ///     inspector.
        /// </summary>
        protected override void InstantiatePlayableCharacters()
        {
            _initialSpawnPointPosition =
                InitialSpawnPoint == null ? Vector3.zero : InitialSpawnPoint.transform.position;

            Players = new List<MoreMountains.TopDownEngine.Character>();

            if (GameManager.Instance.PersistentCharacter != null)
            {
                Players.Add(GameManager.Instance.PersistentCharacter);
                return;
            }

            // we check if there's a stored character in the game manager we should instantiate
            if (GameManager.Instance.StoredCharacter != null)
            {
                var newPlayer = Instantiate(
                    GameManager.Instance.StoredCharacter, _initialSpawnPointPosition, Quaternion.identity);

                newPlayer.name = GameManager.Instance.StoredCharacter.name;
                Players.Add(newPlayer);
                return;
            }

            if (SceneCharacters != null && SceneCharacters.Count > 0)
            {
                foreach (var character in SceneCharacters) Players.Add(character);
                return;
            }

            if (PlayerPrefabs == null) return;

            // player instantiation
            if (PlayerPrefabs.Length != 0)
                foreach (var playerPrefab in PlayerPrefabs)
                {
                    var newPlayer = Instantiate(playerPrefab, _initialSpawnPointPosition, Quaternion.identity);
                    newPlayer.name = playerPrefab.name;
                    Players.Add(newPlayer);

                    if (playerPrefab.CharacterType != MoreMountains.TopDownEngine.Character.CharacterTypes.Player)
                        Debug.LogWarning(
                            "LevelManager : The Character you've set in the LevelManager isn't a Player, which means it's probably not going to move. You can change that in the Character component of your prefab.");
                }
        }
    }
}
