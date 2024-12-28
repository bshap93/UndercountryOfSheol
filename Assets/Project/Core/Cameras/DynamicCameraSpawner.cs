using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Project.Core.Cameras
{
    public class DynamicCameraSpawner : MonoBehaviour, MMEventListener<TopDownEngineEvent>, MMEventListener<MMCameraEvent>
    {
        [SerializeField] private GameObject cameraPrefab;

        private void Start()
        {
            // Start listening for TopDownEngine events
            this.MMEventStartListening<TopDownEngineEvent>();
            this.MMEventStartListening<MMCameraEvent>();
        }
        

        public void OnMMEvent(TopDownEngineEvent engineEvent)
        {
            // Check if the event is SpawnComplete
            if (engineEvent.EventType == TopDownEngineEventTypes.SpawnComplete)
            {
                Debug.LogError("SpawnComplete event received.");
                // Get the spawned character
                MoreMountains.TopDownEngine.Character spawnedCharacter = engineEvent.OriginCharacter;

                // Spawn and link the camera
                SpawnAndLinkCamera(spawnedCharacter);
            }
        }
        
        public void OnMMEvent(MMCameraEvent cameraEvent)
        {
            Debug.Log("Camera event received.");
            // Check if the event is SetTargetCharacter
            if (cameraEvent.EventType == MMCameraEventTypes.SetTargetCharacter)
            {
                Debug.LogError("SetTargetCharacter event received.");
                // Get the target character
                MoreMountains.TopDownEngine.Character targetCharacter = cameraEvent.TargetCharacter;

                // Spawn and link the camera
                SpawnAndLinkCamera(targetCharacter);
            }
        }

        private void SpawnAndLinkCamera(MoreMountains.TopDownEngine.Character character)
        {
            // Instantiate the camera prefab
            GameObject cameraInstance = Instantiate(cameraPrefab);

            // Link the camera to the character
            var cameraController = cameraInstance.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (cameraController != null)
            {
                cameraController.Character = character.gameObject;
                Debug.Log("Camera linked to the spawned character.");
            }
            else
            {
                Debug.LogError("CameraController not found on the camera prefab.");
            }
        }

        private void OnDisable()
        {
            // Stop listening for events
            this.MMEventStopListening<TopDownEngineEvent>();
        }
    }
}
