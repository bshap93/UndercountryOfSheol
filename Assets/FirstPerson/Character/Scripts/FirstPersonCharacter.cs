using System;
using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using Character.Abilities.Scripts;
using Character.Movement.Scripts;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using Player.Scripts;
using Player.Scripts.Project.Gameplay.Player.Health;
using UnityEngine;

namespace Character.Scripts
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
        public virtual FPCharacterStates FPCharacterState { get; protected set; } 
        
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
        public GameObject FPCharacterModel;
        /// the Health script associated to this Character, will be grabbed automatically if left empty
        [Tooltip("the Health script associated to this Character, will be grabbed automatically if left empty")]
        public FPHealth FPCharacterHealth;
        
        [Header("Events")]
        [MMInformation("Here you can define whether or not you want to have that character trigger events when changing state. See the MMTools' State Machine doc for more info.",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
        /// If this is true, the Character's state machine will emit events when entering/exiting a state
        [Tooltip("If this is true, the Character's state machine will emit events when entering/exiting a state")]
        public bool SendStateChangeEvents = true;
        
        [Header("Abilities")]
        /// A list of gameobjects (usually nested under the Character) under which to search for additional abilities
        [Tooltip("A list of gameobjects (usually nested under the Character) under which to search for additional abilities")]
        public List<GameObject> AdditionalAbilityNodes;
        
        [Header("AI")]
        /// The Behavior Tree associated with this character. By default the engine will pick the one on this object, but you can attach another one if you'd like
        [Tooltip("The Behavior Tree associated with this character. By default the engine will pick the one on this object, but you can attach another one if you'd like")]
        public BehaviorManager.BehaviorTree CharacterBehaviorTree;
        
        /// Whether to optimize this character for mobile. Will disable its cone of vision on mobile
        [Tooltip("Whether to optimize this character for mobile. Will disable its cone of vision on mobile")]
        public bool OptimizeForMobile = false;
        
        /// State Machines
        public MMStateMachine<FPCharacterStates.MovementStates> FPMovementState;
        public MMStateMachine<FPCharacterStates.CharacterConditions> FPConditionState;

        /// associated camera and input manager
        public virtual InputManager LinkedFPInputManager { get; protected set; }
        
        /// the animator associated to this character
        public virtual Animator _animator { get; protected set; }
        /// a list of animator parameters
        public virtual HashSet<int> _animatorParameters { get; set; }
        
        public virtual FPCharacterOrientation FPOrientation { get; protected set; }
        /// an object to use as the camera's point of focus and follow target
        /// the direction of the camera associated to this character
        public virtual Vector3 CameraDirection { get; protected set; }

        protected CharacterAbility[] _characterAbilities;
        protected bool _abilitiesCachedOnce = false;
        protected FirstPersonController _fpController;
        protected float _animatorRandomNumber;
        protected bool _spawnDirectionForced = false; 
        
        protected const string _groundedAnimationParameterName = "Grounded";
        protected const string _aliveAnimationParameterName = "Alive";
        protected const string _currentSpeedAnimationParameterName = "CurrentSpeed";
        protected const string _xSpeedAnimationParameterName = "xSpeed";
        protected const string _ySpeedAnimationParameterName = "ySpeed";
        protected const string _zSpeedAnimationParameterName = "zSpeed";
        protected const string _xVelocityAnimationParameterName = "xVelocity";
        protected const string _yVelocityAnimationParameterName = "yVelocity";
        protected const string _zVelocityAnimationParameterName = "zVelocity";
        protected const string _idleAnimationParameterName = "Idle";
        protected const string _randomAnimationParameterName = "Random";
        protected const string _randomConstantAnimationParameterName = "RandomConstant";
        protected const string _transformVelocityXAnimationParameterName = "TransformVelocityX";
        protected const string _transformVelocityYAnimationParameterName = "TransformVelocityY";
        protected const string _transformVelocityZAnimationParameterName = "TransformVelocityZ";
        
        protected int _groundedAnimationParameter;
        protected int _aliveAnimationParameter;
        protected int _currentSpeedAnimationParameter;
        protected int _xSpeedAnimationParameter;
        protected int _ySpeedAnimationParameter;
        protected int _zSpeedAnimationParameter;
        protected int _xVelocityAnimationParameter;
        protected int _yVelocityAnimationParameter;
        protected int _zVelocityAnimationParameter;
        protected int _transformVelocityXAnimationParameter;
        protected int _transformVelocityYAnimationParameter;
        protected int _transformVelocityZAnimationParameter;
        
        protected int _idleAnimationParameter;
        protected int _randomAnimationParameter;
        protected int _randomConstantAnimationParameter;
        protected bool _animatorInitialized = false;
        protected FPCharacterPersistence _characterPersistence;
        protected bool _onReviveRegistered;
        protected Coroutine _conditionChangeCoroutine;
        protected FPCharacterStates.CharacterConditions _lastState;
        protected Vector3 _transformVelocity;
        protected Vector3 _thisPositionLastFrame;
        
        /// <summary>
        /// Initializes this instance of the character
        /// </summary>
        protected virtual void Awake()
        {		
            Initialization();
        }

        /// <summary>
        /// Gets and stores input manager, camera and components
        /// </summary>
        protected virtual void Initialization()
        {
            CharacterDimension = CharacterDimensions.Type3D; 
            
            // we initialize our state machines
            FPMovementState = new MMStateMachine<FPCharacterStates.MovementStates>(gameObject,SendStateChangeEvents);
            FPConditionState = new MMStateMachine<FPCharacterStates.CharacterConditions>(gameObject,SendStateChangeEvents);

            SetInputManager();

            FPCharacterState = new FPCharacterStates();
            _fpController = this.gameObject.GetComponent<FirstPersonController> ();

            if (FPCharacterHealth == null)
            {
                FPCharacterHealth = this.gameObject.GetComponent<FPHealth>();
            }

            CacheAbilitiesAtInit();
            
            
            if (CharacterBehaviorTree == null)
            {
                CharacterBehaviorTree = this.gameObject.GetComponent<BehaviorManager.BehaviorTree>(); 
            } 
            
            if (CharacterBehaviorTree != null)
            {
                // CharacterBrain.Owner = this.gameObject;
            }

            FPOrientation = FindAbility<FPCharacterOrientation>();
            _characterPersistence = FindAbility<FPCharacterPersistence>();
            _thisPositionLastFrame = this.transform.position;

            AssignAnimator();
            
            // instantiate camera target
            // if (CameraTarget == null)
            // {
            //     CameraTarget = new GameObject();
            // }            
            // CameraTarget.transform.SetParent(this.transform);
            // CameraTarget.transform.localPosition = Vector3.zero;
            // CameraTarget.name = "CameraTarget";
            
            if (LinkedFPInputManager != null)
            {
                if (OptimizeForMobile && LinkedFPInputManager.IsMobile)
                {
                    if (this.gameObject.MMGetComponentNoAlloc<MMConeOfVision2D>() != null)
                    {
                        this.gameObject.MMGetComponentNoAlloc<MMConeOfVision2D>().enabled = false;
                    }
                }
            }   
        }
        /// <summary>
        /// Caches abilities if necessary
        /// </summary>
        protected virtual void CacheAbilitiesAtInit()
        {
            if (_abilitiesCachedOnce)
            {
                return;
            }
            CacheAbilities();
        } 
        
        /// <summary>
        /// Grabs abilities and caches them for further use
        /// Make sure you call this if you add abilities at runtime
        /// Ideally you'll want to avoid adding components at runtime, it's costly,
        /// and it's best to activate/disable components instead.
        /// But if you need to, call this method.
        /// </summary>
        public virtual void CacheAbilities()
        {
            // we grab all abilities at our level
            _characterAbilities = this.gameObject.GetComponents<CharacterAbility>();

            // if the user has specified more nodes
            if ((AdditionalAbilityNodes != null) && (AdditionalAbilityNodes.Count > 0))
            {
                // we create a temp list
                List<CharacterAbility> tempAbilityList = new List<CharacterAbility>();

                // we put all the abilities we've already found on the list
                for (int i = 0; i < _characterAbilities.Length; i++)
                {
                    tempAbilityList.Add(_characterAbilities[i]);
                }

                // we add the ones from the nodes
                for (int j = 0; j < AdditionalAbilityNodes.Count; j++)
                {
                    CharacterAbility[] tempArray = AdditionalAbilityNodes[j].GetComponentsInChildren<CharacterAbility>();
                    foreach(CharacterAbility ability in tempArray)
                    {
                        tempAbilityList.Add(ability);
                    }
                }

                _characterAbilities = tempAbilityList.ToArray();
            }
            _abilitiesCachedOnce = true;
        }
        
        /// <summary>
        /// Forces the (re)initialization of the character's abilities
        /// </summary>
        public virtual void ForceAbilitiesInitialization()
        {
            for (int i = 0; i < _characterAbilities.Length; i++)
            {
                _characterAbilities[i].ForceInitialization();
            }
            for (int j = 0; j < AdditionalAbilityNodes.Count; j++)
            {
                CharacterAbility[] tempArray = AdditionalAbilityNodes[j].GetComponentsInChildren<CharacterAbility>();
                foreach(CharacterAbility ability in tempArray)
                {
                    ability.ForceInitialization();
                }
            }
        }
        
        /// <summary>
        /// A method to check whether a Character has a certain ability or not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T FindAbility<T>() where T:CharacterAbility
        {
            CacheAbilitiesAtInit();

            Type searchedAbilityType = typeof(T);
            
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability is T characterAbility)
                {
                    return characterAbility;
                }
            }

            return null;
        }
        
        /// <summary>
        /// A method to check whether a Character has a certain ability or not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public CharacterAbility FindAbilityByString(string abilityName)
        {
            CacheAbilitiesAtInit();
            
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability.GetType().Name == abilityName)
                {
                    return ability;
                }
            }

            return null;
        } 
        
        /// <summary>
        /// A method to check whether a Character has a certain ability or not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> FindAbilities<T>() where T:CharacterAbility
        {
            CacheAbilitiesAtInit();

            List<T> resultList = new List<T>();
            Type searchedAbilityType = typeof(T);

            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability is T characterAbility)
                {
                    resultList.Add(characterAbility);
                }
            }

            return resultList;
        }
        
        /// <summary>
        /// Binds an animator to this character
        /// </summary>
        public virtual void AssignAnimator(bool forceAssignation = false)
        {
            if (_animatorInitialized && !forceAssignation)
            {
                return;
            }
            
            _animatorParameters = new HashSet<int>();

            if (CharacterAnimator != null)
            {
                _animator = CharacterAnimator;
            }
            else
            {
                _animator = this.gameObject.GetComponent<Animator>();
            }

            if (_animator != null)
            {
                if (DisableAnimatorLogs)
                {
                    _animator.logWarnings = false;
                }
                InitializeAnimatorParameters();
            }

            _animatorInitialized = true;
        }
        
        		/// <summary>
		/// Initializes the animator parameters.
		/// </summary>
		protected virtual void InitializeAnimatorParameters()
		{
			if (_animator == null) { return; }
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _groundedAnimationParameterName, out _groundedAnimationParameter, AnimatorControllerParameterType.Bool, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _currentSpeedAnimationParameterName, out _currentSpeedAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _xSpeedAnimationParameterName, out _xSpeedAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _ySpeedAnimationParameterName, out _ySpeedAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _zSpeedAnimationParameterName, out _zSpeedAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _idleAnimationParameterName, out _idleAnimationParameter, AnimatorControllerParameterType.Bool, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _aliveAnimationParameterName, out _aliveAnimationParameter, AnimatorControllerParameterType.Bool, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _randomAnimationParameterName, out _randomAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _randomConstantAnimationParameterName, out _randomConstantAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _xVelocityAnimationParameterName, out _xVelocityAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _yVelocityAnimationParameterName, out _yVelocityAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _zVelocityAnimationParameterName, out _zVelocityAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _transformVelocityXAnimationParameterName, out _transformVelocityXAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _transformVelocityYAnimationParameterName, out _transformVelocityYAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
			MMAnimatorExtensions.AddAnimatorParameterIfExists(_animator, _transformVelocityZAnimationParameterName, out _transformVelocityZAnimationParameter, AnimatorControllerParameterType.Float, _animatorParameters);
            
			// we update our constant float animation parameter
			int randomConstant = UnityEngine.Random.Range(0, 1000);
			MMAnimatorExtensions.UpdateAnimatorInteger(_animator, _randomConstantAnimationParameter, randomConstant, _animatorParameters, RunAnimatorSanityChecks);
		}
                
        /// <summary>
        /// Gets (if it exists) the InputManager matching the Character's Player ID
        /// </summary>
        public virtual void SetInputManager()
        {
            if (CharacterType == CharacterTypes.AI)
            {
                LinkedFPInputManager = null;
                UpdateInputManagersInAbilities();
                return;
            }

            // we get the corresponding input manager
            if (!string.IsNullOrEmpty(PlayerID))
            {
                LinkedFPInputManager = null;
                InputManager[] foundInputManagers = FindObjectsByType(typeof(InputManager), FindObjectsSortMode.None) as InputManager[];
                foreach (InputManager foundInputManager in foundInputManagers)
                {
                    if (foundInputManager.PlayerID == PlayerID)
                    {
                        LinkedFPInputManager = foundInputManager;
                    }
                }
            }
            UpdateInputManagersInAbilities();
        }
        
        /// <summary>
        /// Sets a new input manager for this Character and all its abilities
        /// </summary>
        /// <param name="inputManager"></param>
        public virtual void SetInputManager(InputManager inputManager)
        {
            LinkedFPInputManager = inputManager;
            UpdateInputManagersInAbilities();
        }
         
        /// <summary>
        /// Updates the linked input manager for all abilities
        /// </summary>
        protected virtual void UpdateInputManagersInAbilities()
        {
            if (_characterAbilities == null)
            {
                return;
            }
            for (int i = 0; i < _characterAbilities.Length; i++)
            {
                _characterAbilities[i].SetInputManager(LinkedFPInputManager);
            }
        }
        
        protected void FixedUpdate()
        {
            _transformVelocity = (this.transform.position - _thisPositionLastFrame) / Time.deltaTime;
            _thisPositionLastFrame = this.transform.position;
        }
        
        /// <summary>
        /// Resets the input for all abilities
        /// </summary>
        public virtual void ResetInput()
        {
            if (_characterAbilities == null)
            {
                return;
            }
            foreach (CharacterAbility ability in _characterAbilities)
            {
                ability.ResetInput();
            }
        }
        
        /// <summary>
        /// Sets the player ID
        /// </summary>
        /// <param name="newPlayerID">New player ID.</param>
        public virtual void SetPlayerID(string newPlayerID)
        {
            PlayerID = newPlayerID;
            SetInputManager();
        }
        
        		
        /// <summary>
        /// This is called every frame.
        /// </summary>
        protected virtual void Update()
        {		
            EveryFrame();
				
        }
        
        /// <summary>
        /// We do this every frame. This is separate from Update for more flexibility.
        /// </summary>
        protected virtual void EveryFrame()
        {
            // we process our abilities
            EarlyProcessAbilities();
            ProcessAbilities();
            LateProcessAbilities();

            // we send our various states to the animator.		 
            UpdateAnimators ();
        }
        
        /// <summary>
        /// Calls all registered abilities' Early Process methods
        /// </summary>
        protected virtual void EarlyProcessAbilities()
        {
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability.enabled && ability.AbilityInitialized)
                {
                    ability.EarlyProcessAbility();
                }
            }
        }

        /// <summary>
        /// Calls all registered abilities' Process methods
        /// </summary>
        protected virtual void ProcessAbilities()
        {
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability.enabled && ability.AbilityInitialized)
                {
                    ability.ProcessAbility();
                }
            }
        }

        /// <summary>
        /// Calls all registered abilities' Late Process methods
        /// </summary>
        protected virtual void LateProcessAbilities()
        {
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability.enabled && ability.AbilityInitialized)
                {
                    ability.LateProcessAbility();
                }
            }
        }
        
        		/// <summary>
		/// This is called at Update() and sets each of the animators parameters to their corresponding State values
		/// </summary>
		protected virtual void UpdateAnimators()
		{
			UpdateAnimationRandomNumber();

			if ((UseDefaultMecanim) && (_animator!= null))
			{
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _groundedAnimationParameter, _fpController.Grounded,_animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _aliveAnimationParameter, (FPConditionState.CurrentState != FPCharacterStates.CharacterConditions.Dead),_animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _currentSpeedAnimationParameter, _fpController.CurrentMovement.magnitude, _animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _xSpeedAnimationParameter, _fpController.CurrentMovement.x,_animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _ySpeedAnimationParameter, _fpController.CurrentMovement.y,_animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _zSpeedAnimationParameter, _fpController.CurrentMovement.z,_animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _idleAnimationParameter,(FPMovementState.CurrentState == FPCharacterStates.MovementStates.Idle),_animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _randomAnimationParameter, _animatorRandomNumber, _animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _xVelocityAnimationParameter, _fpController.Velocity.x, _animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _yVelocityAnimationParameter, _fpController.Velocity.y, _animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _zVelocityAnimationParameter, _fpController.Velocity.z, _animatorParameters, RunAnimatorSanityChecks);
				
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _transformVelocityXAnimationParameter, _transformVelocity.x, _animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _transformVelocityYAnimationParameter, _transformVelocity.y, _animatorParameters, RunAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _transformVelocityZAnimationParameter, _transformVelocity.z, _animatorParameters, RunAnimatorSanityChecks);


				foreach (CharacterAbility ability in _characterAbilities)
				{
					if (ability.enabled && ability.AbilityInitialized)
					{	
						ability.UpdateAnimator();
					}
				}
			}
		}
                
        public virtual void RespawnAt(Vector3 spawnPosition, FacingDirections facingDirection)
        {
            transform.position = spawnPosition;
			
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
                //Debug.LogError("Spawn : your Character's gameobject is inactive");
            }

            // we raise it from the dead (if it was dead)
            FPConditionState.ChangeState(FPCharacterStates.CharacterConditions.Normal);

            // we re-enable its 3D collider
            if (this.gameObject.MMGetComponentNoAlloc<Collider>() != null)
            {
                this.gameObject.MMGetComponentNoAlloc<Collider>().enabled = true;
            }

            // we make it handle collisions again
            _fpController.enabled = true;
            _fpController.CollisionsOn();
            _fpController.Reset();
			
            Reset();
            UnFreeze();

            if (FPCharacterHealth != null)
            {
                FPCharacterHealth.StoreInitialPosition();
                if (_characterPersistence != null)
                {
                    if (_characterPersistence.Initialized)
                    {
                        if (FPCharacterHealth != null)
                        {
                            FPCharacterHealth.UpdateHealthBar(false);
                        }
                        return;
                    }
                }
                FPCharacterHealth.ResetHealthToMaxHealth();
                FPCharacterHealth.Revive();
            }

            // if (CharacterBehaviorTree != null)
            // {
            //     CharacterBehaviorTree.Initialize();
            // }


            // facing direction
            if (FindAbility<FPCharacterOrientation>() != null)
            {
                FindAbility<FPCharacterOrientation>().Face( facingDirection);
            }
        }
        
        /// <summary>
        /// Makes the player respawn at the location passed in parameters
        /// </summary>
        /// <param name="spawnPoint">The location of the respawn.</param>
        public virtual void RespawnAt(Transform spawnPoint, FacingDirections facingDirection)
        {
            RespawnAt(spawnPoint.position, facingDirection);
        }
        
        /// <summary>
        /// Calls flip on all abilities
        /// </summary>
        public virtual void FlipAllAbilities()
        {
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability.enabled)
                {
                    ability.Flip();
                }
            }
        }
        
        /// <summary>
        /// Generates a random number to send to the animator
        /// </summary>
        protected virtual void UpdateAnimationRandomNumber()
        {
            _animatorRandomNumber = UnityEngine.Random.Range(0f, 1f);
        }
        
        /// <summary>
        /// Use this method to change the character's condition for a specified duration, and resetting it afterwards.
        /// You can also use this to disable gravity for a while, and optionally reset forces too.
        /// </summary>
        /// <param name="newCondition"></param>
        /// <param name="duration"></param>
        /// <param name="resetControllerForces"></param>
        /// <param name="disableGravity"></param>
        public virtual void ChangeCharacterConditionTemporarily(FPCharacterStates.CharacterConditions newCondition,
            float duration, bool resetControllerForces, bool disableGravity)
        {
            if (_conditionChangeCoroutine != null)
            {
                StopCoroutine(_conditionChangeCoroutine);
            }
            _conditionChangeCoroutine = StartCoroutine(ChangeCharacterConditionTemporarilyCo(newCondition, duration, resetControllerForces, disableGravity));
        }
        
        /// <summary>
        /// Coroutine handling the temporary change of condition mandated by ChangeCharacterConditionTemporarily
        /// </summary>
        /// <param name="newCondition"></param>
        /// <param name="duration"></param>
        /// <param name="resetControllerForces"></param>
        /// <param name="disableGravity"></param>
        /// <returns></returns>
        protected virtual IEnumerator ChangeCharacterConditionTemporarilyCo(
            FPCharacterStates.CharacterConditions newCondition,
            float duration, bool resetControllerForces, bool disableGravity)
        {
            if (_lastState != newCondition) if ((_lastState != newCondition) && (this.FPConditionState.CurrentState != newCondition))
            {
                _lastState = this.FPConditionState.CurrentState;
            }
			
            this.FPConditionState.ChangeState(newCondition);
            if (resetControllerForces) { _fpController?.SetMovement(Vector2.zero); }
            if (disableGravity && (_fpController != null)) { _fpController.GravityActive = false; }
            yield return MMCoroutine.WaitFor(duration);
            this.FPConditionState.ChangeState(_lastState);
            if (disableGravity && (_fpController != null)) { _fpController.GravityActive = true; }
        }

        /// <summary>
        /// Freezes this character.
        /// </summary>
        public virtual void Freeze()
        {
            _fpController.SetGravityActive(false);
            _fpController.SetMovement(Vector2.zero);
            FPConditionState.ChangeState(FPCharacterStates.CharacterConditions.Frozen);
        } 
        
        /// <summary>
        /// Unfreezes this character
        /// </summary>
        public virtual void UnFreeze()
        {
            if (FPConditionState.CurrentState == FPCharacterStates.CharacterConditions.Frozen)
            {
                _fpController.SetGravityActive(true);
                FPConditionState.ChangeState(FPCharacterStates.CharacterConditions.Normal);
            }
        }
        
        /// <summary>
        /// Called to disable the player (at the end of a level for example. 
        /// It won't move and respond to input after this.
        /// </summary>
        public virtual void Disable()
        {
            this.enabled = false;
            _fpController.enabled = false;			
        }
        
        /// <summary>
        /// Called when the Character dies. 
        /// Calls every abilities' Reset() method, so you can restore settings to their original value if needed
        /// </summary>
        public virtual void Reset()
        {
            _spawnDirectionForced = false;
            if (_characterAbilities == null)
            {
                return;
            }
            if (_characterAbilities.Length == 0)
            {
                return;
            }
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability.enabled)
                {
                    ability.ResetAbility();
                }
            }
        }
        
        /// <summary>
        /// On revive, we force the spawn direction
        /// </summary>
        protected virtual void OnRevive()
        {
            if (CharacterBehaviorTree != null)
            {
                // TODO Reset behavior tree
                // CharacterBehaviorTree.Initialize();
                // CharacterBrain.enabled = true;
                // CharacterBrain.ResetBrain();
            }
        }

        protected virtual void OnDeath()
        {
            // TODO Disable behavior tree
            if (CharacterBehaviorTree != null)
            {
                // CharacterBrain.TransitionToState("");
                // CharacterBrain.enabled = false;
            }
            if (FPMovementState.CurrentState != FPCharacterStates.MovementStates.FallingDownHole)
            {
                FPMovementState.ChangeState(FPCharacterStates.MovementStates.Idle);
            }            
        }
        
        protected virtual void OnHit()
        {

        }

        /// <summary>
        /// OnEnable, we register our OnRevive event
        /// </summary>
        protected virtual void OnEnable ()
        {
            if (FPCharacterHealth != null)
            {
                if (!_onReviveRegistered)
                {
                    FPCharacterHealth.OnRevive += OnRevive;
                    _onReviveRegistered = true;
                }
                FPCharacterHealth.OnDeath += OnDeath;
                FPCharacterHealth.OnHit += OnHit;
            }
        }
        
        /// <summary>
        /// OnDisable, we unregister our OnRevive event
        /// </summary>
        protected virtual void OnDisable()
        {
            if (FPCharacterHealth != null)
            {
                FPCharacterHealth.OnDeath -= OnDeath;
                FPCharacterHealth.OnHit -= OnHit;
            }			
        }


    }
}
