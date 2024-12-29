using UnityEngine;
using System.Collections.Generic;
using MoreMountains.InventoryEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Project.Gameplay.Events;
using Project.Gameplay.ItemManagement.InventoryItemTypes;
using Project.Gameplay.Player;
using Project.UI.HUD;

namespace Items.Scripts
{
    [DefaultExecutionOrder(100)]
    public class PlayerItemPreviewManager : MonoBehaviour, MMEventListener<ItemEvent>
    {
        [Header("UI")]
        public GameObject PreviewPanelUI;
        public MMFeedbacks SelectionFeedbacks;
        public MMFeedbacks DeselectionFeedbacks;
        
        public Inventory PrimaryInventory;
        public Inventory SecondaryInventory;

        [Header("Raycast Settings")]
        public LayerMask layerMask = -1;
        public Camera raycastCamera;
        [Tooltip("Minimum distance for target.")]
        public float minDistance;
        [Tooltip("Maximum distance for target. 0 = infinity")]
        public float maxDistance;
        [Tooltip("Blocks interaction if pointer is over an UI element")]
        public bool respectUI = true;

        private PreviewManager _previewManager;
        private Transform _currentTarget;
        private InventoryItem _currentPreviewedItem;
        private InventoryItem _selectedItem;
        private readonly HashSet<InventoryItem> _registeredItems = new HashSet<InventoryItem>();
        private PromptManager _promptManager; 
        

        public InventoryItem CurrentPreviewedItem => _currentPreviewedItem;

        static PlayerItemPreviewManager _instance;
        public static PlayerItemPreviewManager instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<PlayerItemPreviewManager>();
                }
                return _instance;
            }
        }

        void Start()
        {
            _previewManager = FindFirstObjectByType<PreviewManager>();
            if (_previewManager == null)
            {
                Debug.LogWarning("PreviewManager not found in the scene.");
            }
            
            _promptManager = FindFirstObjectByType<PromptManager>();
            if (_promptManager == null) Debug.LogWarning("PickupPromptManager not found in the scene.");



            if (raycastCamera == null)
            {
                raycastCamera = GetCamera();
                if (raycastCamera == null)
                {
                    Debug.LogError("PlayerItemPreviewManager: no camera found!");
                }
            }
        }

        void OnEnable()
        {
            this.MMEventStartListening();
        }

        void OnDisable()
        {
            this.MMEventStopListening();

            if (this != null) // Check if the script itself is being destroyed
            {
                ClearPreview();
            }
        }


        void Update()
        {
            if (raycastCamera == null)
                return;

            // Check UI blocking
            if (respectUI && IsBlockedByUI())
            {
                HandleNoValidTarget();
                return;
            }

            // Create ray from mouse position or camera center
            Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            // Check for hits
            if (Physics.Raycast(ray, out hitInfo, maxDistance > 0 ? maxDistance : raycastCamera.farClipPlane, layerMask) && 
                Vector3.Distance(hitInfo.point, ray.origin) >= minDistance)
            {
                Transform target = hitInfo.collider.transform;
                ItemPreviewTrigger previewTrigger = target.GetComponentInParent<ItemPreviewTrigger>();
                
                if (previewTrigger != null && previewTrigger.Item != null)
                {
                    // Handle hover preview
                    HandleItemPreview(target, previewTrigger.Item);
                    _promptManager?.ShowPickupPrompt();
                    
                    if (Input.GetKey("f"))
                    {
                        TryPickupItem(previewTrigger);
                    }

                    // Handle click selection
                    if (Input.GetMouseButtonDown(0))
                    {
                        HandleItemSelection(target, previewTrigger);
                    }
                    return;
                }

                Debug.LogError("ItemPreviewTrigger not found in the target object.");
            }

            HandleNoValidTarget();
        }
        
        private void TryPickupItem(ItemPreviewTrigger trigger)
        {
            if (trigger.Item == null) return;

            if (trigger.Item is InventoryCoinPickup coinPickup)
            {
                HandleCoinPickup(coinPickup, trigger);
            }
            else
            {
                HandleInventoryItemPickup(trigger);
            }
        }
        
        private void HandleInventoryItemPickup(ItemPreviewTrigger trigger)
        {
            var Item = trigger.Item;
            var Quantity = trigger.Quantity;
            var inventoryTagName = trigger.TargetInventoryTagName;
            
            if (PrimaryInventory.gameObject.CompareTag(inventoryTagName))
            {
                // Doesn't change
                
            }
            else if (SecondaryInventory.gameObject.CompareTag(inventoryTagName))
            {
                PrimaryInventory = SecondaryInventory;
            }
            else
            
            if (PrimaryInventory == null) Debug.LogWarning($"Target inventory '{inventoryTagName}' not found.");
            
            if (PrimaryInventory == null) return;

            if (PrimaryInventory.AddItem(Item, Quantity))
            {
                CompletePickup(trigger);
            }
            else
            {
                ShowInventoryFullMessage();
            }
        }
        
        private void CompletePickup(ItemPreviewTrigger trigger)
        {
            if (_promptManager != null)
            {
                _promptManager.HidePickupPrompt();
            }
    
            if (trigger.PickedMMFeedbacks != null)
            {
                trigger.PickedMMFeedbacks.PlayFeedbacks();
            }

            // Trigger item picked up event
            ItemEvent.Trigger("ItemPickedUp", trigger.Item, transform);

            // Destroy the item at the end
            Destroy(trigger.gameObject);
        }

        
        private void HandleCoinPickup(InventoryCoinPickup coinPickup, ItemPreviewTrigger trigger)
        {

            var playerStats = GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                int coinsToAdd = Random.Range(coinPickup.MinimumCoins, coinPickup.MaximumCoins + 1);
                playerStats.AddCoins(coinsToAdd);
                
                CompletePickup(trigger);
            }
        }
        
        

        private void HandleNoValidTarget()
        {
            // Only clear hover preview if we don't have a selected item
            if (_selectedItem == null && _currentTarget != null)
            {
                ClearPreview();
                _promptManager?.HidePickupPrompt();
            }
        }

        private void HandleItemPreview(Transform target, InventoryItem item)
        {
            // Don't change preview if we have a selected item
            if (_selectedItem != null)
                return;

            if (target != _currentTarget)
            {
                ClearPreview();
                _currentTarget = target;
                ShowPreview(item);
            }
        }

        private void HandleItemSelection(Transform target, ItemPreviewTrigger trigger)
        {
            if (_selectedItem == trigger.Item)
            {
                // Deselect if clicking the same item
                _selectedItem = null;
                trigger.OnUnSelectedItem();
            }
            else
            {
                // Clear previous selection if exists
                if (_selectedItem != null)
                {
                    var previousTrigger = _currentTarget.GetComponentInParent<ItemPreviewTrigger>();
                    previousTrigger?.OnUnSelectedItem();
                }

                // Select new item
                _selectedItem = trigger.Item;
                trigger.OnSelectedItem();
            }
        }

        private void ShowPreview(InventoryItem item)
        {
            if (item == null) return;

            _currentPreviewedItem = item;
            PreviewPanelUI?.SetActive(true);
            _previewManager?.ShowPreview(item);
        }

        private void ClearPreview()
        {
            _currentTarget = null;
            _currentPreviewedItem = null;

            if (PreviewPanelUI != null)
            {
                if (PreviewPanelUI.activeInHierarchy) // Ensure the GameObject is not destroyed
                {
                    PreviewPanelUI.SetActive(false);
                }
            }

            if (_previewManager != null)
            {
                _previewManager.HidePreview();
            }
        }


        public void OnMMEvent(ItemEvent eventType)
        {
            if (eventType.EventName == "ItemPickedUp")
            {
                if (_registeredItems.Contains(eventType.Item))
                {
                    UnregisterItem(eventType.Item);
                }
                
                if (_selectedItem == eventType.Item)
                {
                    _selectedItem = null;
                }
            }
        }

        public void RegisterItem(InventoryItem item)
        {
            if (item != null)
            {
                _registeredItems.Add(item);
            }
        }

        public void UnregisterItem(InventoryItem item)
        {
            if (item != null)
            {
                _registeredItems.Remove(item);
                if (_currentPreviewedItem == item)
                {
                    ClearPreview();
                }
            }
        }

        private bool IsBlockedByUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
                return false;

            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        private static Camera GetCamera()
        {
            Camera raycastCamera = Camera.main;
            if (raycastCamera == null)
            {
                raycastCamera = FindFirstObjectByType<Camera>();
            }
            return raycastCamera;
        }

        // Public methods for manual control
        public void ShowSelectedItemPreviewPanel(InventoryItem item)
        {
            if (item == null) return;
            
            _selectedItem = item;
            PreviewPanelUI?.SetActive(true);
            _previewManager?.ShowPreview(item);
            SelectionFeedbacks?.PlayFeedbacks();
        }

        public void HideSelectedItemPreviewPanel()
        {
            _selectedItem = null;
            ClearPreview();
            DeselectionFeedbacks?.PlayFeedbacks();
        }
        
        private void ShowInventoryFullMessage()
        {
            Debug.Log("Inventory is full or item cannot be picked up.");
            // Additional UI feedback for full inventory could be added here
        }
    }
}