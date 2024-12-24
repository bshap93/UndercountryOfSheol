using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Player.Scripts
{
    public class FirstPersonCharacter : MMMonoBehaviour 
    {
        /// the possible initial facing direction for your character
        public enum FacingDirections { West, East, North, South } 
         
        
        public enum CharacterDimensions { Type3D }
        
        [MMReadOnly]
        public CharacterDimensions CharacterDimension;


        /// the possible character types : player controller or AI (controlled by the computer)
        public enum CharacterTypes { Player, AI }
        
        [MMInformation("The Character script is the mandatory basis for all Character abilities. Your character can either be a Non Player Character, controlled by an AI, or a Player character, controlled by the player. In this case, you'll need to specify a PlayerID, which must match the one specified in your InputManager. Usually 'Player1', 'Player2', etc.",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
        /// Is the character player-controlled or controlled by an AI ?
        [Tooltip("Is the character player-controlled or controlled by an AI ?")]
        public CharacterTypes CharacterType = CharacterTypes.AI;
        /// Only used if the character is player-controlled. The PlayerID must match an input manager's PlayerID. It's also used to match Unity's input settings. So you'll be safe if you keep to Player1, Player2, Player3 or Player4
        [Tooltip("Only used if the character is player-controlled. The PlayerID must match an input manager's PlayerID. It's also used to match Unity's input settings. So you'll be safe if you keep to Player1, Player2, Player3 or Player4")]
        public string PlayerID = "";
        
        /// the various states of the character
        public virtual FPCharacterStates CharacterState { get; protected set; } 
        
        [Header("Animator")]
        [MMInformation("The engine will try and find an animator for this character. If it's on the same gameobject it should have found it. If it's nested somewhere, you'll need to bind it below. You can also decide to get rid of it altogether, in that case, just uncheck 'use mecanim'.",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
        /// the character animator
        [Tooltip("the character animator, that this class and all abilities should update parameters on")]
        public Animator CharacterAnimator;
        /// Set this to false if you want to implement your own animation system
        [Tooltip("Set this to false if you want to implement your own animation system")]
        public bool UseDefaultMecanim = true;
        /// If this is true, sanity checks will be performed to make sure animator parameters exist before updating them. Turning this to false will increase performance but will throw errors if you're trying to update non existing parameters. Make sure your animator has the required parameters.
        [Tooltip("If this is true, sanity checks will be performed to make sure animator parameters exist before updating them. Turning this to false will increase performance but will throw errors if you're trying to update non existing parameters. Make sure your animator has the required parameters.")]
        public bool RunAnimatorSanityChecks = false;
        /// if this is true, animator logs for the associated animator will be turned off to avoid potential spam
        [Tooltip("if this is true, animator logs for the associated animator will be turned off to avoid potential spam")]
        public bool DisableAnimatorLogs = true;
        
        [Header("Bindings")]
        [MMInformation("Leave this unbound if this is a regular, sprite-based character, and if the SpriteRenderer and the Character are on the same GameObject. If not, you'll want to parent the actual model to the Character object, and bind it below. See the 3D demo characters for an example of that. The idea behind that is that the model may move, flip, but the collider will remain unchanged.",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
        /// the 'model' (can be any gameobject) used to manipulate the character. Ideally it's separated (and nested) from the collider/TopDown controller/abilities, to avoid messing with collisions.
        [Tooltip("the 'model' (can be any gameobject) used to manipulate the character. Ideally it's separated (and nested) from the collider/TopDown controller/abilities, to avoid messing with collisions.")]
        public GameObject CharacterModel;
        /// the Health script associated to this Character, will be grabbed automatically if left empty
        [Tooltip("the Health script associated to this Character, will be grabbed automatically if left empty")]
        public Health CharacterHealth;
    }
}
