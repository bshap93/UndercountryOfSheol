using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.InventoryEngine;
using Project.Gameplay.Events;
using Project.Gameplay.ItemManagement;
using Project.Gameplay.ItemManagement.InventoryItemTypes;
using Project.Gameplay.Player;
using Project.UI.HUD;

namespace Items.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class ManualItemPicker : MonoBehaviour
    {
        public InventoryItem Item;
        public int Quantity = 1;

        [Header("Pickup Settings")]
        [Tooltip("Maximum distance at which the player can pick up this item")]
        public float maxPickupDistance = 3f;
        [Tooltip("Layer mask for pickup raycast")]
        public LayerMask pickupLayerMask = -1;

        [Header("Feedbacks")]
        [Tooltip("Feedbacks to play when the item is picked up")]
        public MMFeedbacks pickedMmFeedbacks;

        private PromptManager _promptManager;
        private MoreMountains.InventoryEngine.Inventory _targetInventory;
        private Collider _itemCollider;
        private bool _isInRange;
        private Transform _playerTransform;
        private PlayerItemPreviewManager _previewManager;

        private void Start()
        {
            InitializeComponents();
            InitializeInventory();
            InitializeFeedbacks();
        }

        private void InitializeComponents()
        {
            _promptManager = FindFirstObjectByType<PromptManager>();
            if (_promptManager == null) Debug.LogWarning("PickupPromptManager not found in the scene.");

        }

        private void InitializeInventory()
        {
            string inventoryTag = Item.TargetInventoryName switch
            {
                "MainPlayerInventory" => "MainPlayerInventory",
                "HotbarInventory" => "HotbarInventory",
                _ => "MainPlayerInventory"
            };

            _targetInventory = GameObject.FindWithTag(inventoryTag)?.GetComponent<MoreMountains.InventoryEngine.Inventory>();
            if (_targetInventory == null) Debug.LogWarning($"Target inventory '{inventoryTag}' not found.");
        }

        private void InitializeFeedbacks()
        {
            pickedMmFeedbacks?.Initialization(gameObject);
        }

        private void Update()
        {

            // Check if we're looking at the item
            bool isLookingAtItem = CheckIfLookingAtItem();
            
            // Check if we're within pickup distance
            bool isWithinDistance = CheckDistanceToPlayer();
            
            if (isLookingAtItem && isWithinDistance)
            {
                _promptManager?.ShowPickupPrompt();
                Debug.Log("Press F to pick up item.");
            }
            else
            {
                _promptManager?.HidePickupPrompt();
            }

            // Only allow pickup if both conditions are met
            if (isLookingAtItem && isWithinDistance && Input.GetKeyDown(KeyCode.F))
            {
                TryPickupItem();
            }
        }

        private bool CheckIfLookingAtItem()
        {
            if (_previewManager == null || _previewManager.CurrentPreviewedItem != Item)
                return false;

            return true;
        }

        private bool CheckDistanceToPlayer()
        {
            if (_playerTransform == null) return false;

            // Get the closest point on the item's collider to the player
            Vector3 closestPoint = _itemCollider.ClosestPoint(_playerTransform.position);
            float distance = Vector3.Distance(_playerTransform.position, closestPoint);

            return distance <= maxPickupDistance;
        }



        private void TryPickupItem()
        {
            if (Item == null) return;

            if (Item is InventoryCoinPickup coinPickup)
            {
                HandleCoinPickup(coinPickup);
            }
            else
            {
                HandleInventoryItemPickup();
            }
        }

        private void HandleCoinPickup(InventoryCoinPickup coinPickup)
        {
            if (_playerTransform == null) return;

            var playerStats = _playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                int coinsToAdd = Random.Range(coinPickup.MinimumCoins, coinPickup.MaximumCoins + 1);
                playerStats.AddCoins(coinsToAdd);
                
                CompletePickup();
            }
        }

        private void HandleInventoryItemPickup()
        {
            if (_targetInventory == null) return;

            if (_targetInventory.AddItem(Item, Quantity))
            {
                CompletePickup();
            }
            else
            {
                ShowInventoryFullMessage();
            }
        }

        private void CompletePickup()
        {
            _promptManager?.HidePickupPrompt();
            ItemEvent.Trigger("ItemPickedUp", Item, transform);
            pickedMmFeedbacks?.PlayFeedbacks();
            Destroy(gameObject);
        }

        private void ShowInventoryFullMessage()
        {
            Debug.Log("Inventory is full or item cannot be picked up.");
            // Additional UI feedback for full inventory could be added here
        }
    }
}