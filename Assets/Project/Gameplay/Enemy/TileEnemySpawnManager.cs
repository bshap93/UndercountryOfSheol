﻿using DunGen;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

// Namespace from DungenCharacter

namespace Project.Gameplay.Enemy
{
    public class TileEnemySpawnerManager : MonoBehaviour, MMEventListener<MMCameraEvent>
    {
        void OnEnable()
        {
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
                var character = FindObjectOfType<DungenCharacter>();
                character.OnTileChanged += OnPlayerTileChanged;
            }
        }    /// <summary>
        /// Called whenever the player enters a new tile
        /// </summary>
        private void OnPlayerTileChanged(DungenCharacter character, Tile previousTile, Tile newTile)
        {
            if (newTile == null) return;

            // Check if the new tile has a SpawnEnemiesInTile component
            var enemySpawner = newTile.GetComponent<SpawnEnemiesInTile>();
        
            if (enemySpawner != null && !enemySpawner.HasSpawnedEnemies)
            {
                Debug.Log($"Player entered tile {newTile.gameObject.name}. Spawning enemies...");
                enemySpawner.SpawnEnemies();
            }
        }
    }
}
