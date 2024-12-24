﻿using System;
using Character.Movement.Scripts;
using Character.Scripts;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using CharacterHandleShield = Project.Gameplay.Combat.Shields.CharacterHandleShield;
using ShieldProtectionArea = Project.Gameplay.Combat.Shields.ShieldProtectionArea;

namespace FirstPerson.Combat.Shield.Scripts
{
    [Serializable]
    public class FPShield : MMMonoBehaviour 
    {
        public enum ShieldStates
        {
            Inactive,
            Starting,
            Active,
            Blocking,
            Broken,
            Interrupted
        }

        public ShieldProtectionArea ShieldProtectionArea;


        [Header("Shield Properties")] [Tooltip("Maximum health of the shield before it breaks")]
        public float MaxShieldHealth = 100f;
        [Tooltip("Time shield takes to recover after being broken")]
        public float RecoveryTime = 2f;
        [Tooltip("Amount of stamina consumed per block")]
        public float StaminaConsumption = 10f;

        [Tooltip("if this is true all movement will be prevented (even flip) while the weapon is active")]
        public bool PreventAllMovementWhileInUse;

        [Header("Feedbacks")] public MMFeedbacks ShieldRaiseFeedback;
        public MMFeedbacks ShieldLowerFeedback;
        public MMFeedbacks ShieldBlockFeedback;
        public MMFeedbacks ShieldBreakFeedback;
        protected Animator _animator;
        protected FPCharacterMovement _FpCharacterMovement;
        protected CharacterHandleShield _handler;

        protected FirstPersonCharacter _owner;
        protected float _recoveryTimer;
        protected int _shieldStateParameter;


        public float CurrentShieldHealth { get; protected set; }
        public ShieldStates CurrentState { get; protected set; }

        protected virtual void Update()
        {
            // Handle state transitions
            switch (CurrentState)
            {
                case ShieldStates.Starting:
                    CurrentState = ShieldStates.Active;
                    UpdateAnimator();
                    break;

                case ShieldStates.Blocking:
                    CurrentState = ShieldStates.Active;
                    UpdateAnimator();
                    break;

                case ShieldStates.Broken:
                    if (_recoveryTimer > 0)
                    {
                        _recoveryTimer -= Time.deltaTime;
                    }
                    else
                    {
                        CurrentState = ShieldStates.Inactive;
                        CurrentShieldHealth = MaxShieldHealth;
                        UpdateAnimator();
                    }

                    break;
            }
        }


        public virtual void SetOwner(FirstPersonCharacter owner, CharacterHandleShield handler)
        {
            _owner = owner;
            _handler = handler;
            _animator = _handler.CharacterAnimator;

            _FpCharacterMovement = _owner.GetComponent<FirstPersonCharacter>()?.FindAbility<FPCharacterMovement>();
        }

        public virtual void Initialization()
        {
            CurrentShieldHealth = MaxShieldHealth;
            CurrentState = ShieldStates.Inactive;
            _shieldStateParameter = Animator.StringToHash("ShieldState");
            InitializeFeedbacks();
        }

        protected virtual void InitializeFeedbacks()
        {
            ShieldRaiseFeedback?.Initialization(gameObject);
            ShieldLowerFeedback?.Initialization(gameObject);
            ShieldBlockFeedback?.Initialization(gameObject);
            ShieldBreakFeedback?.Initialization(gameObject);
        }

        public virtual void RaiseShield()
        {
            if (CurrentState != ShieldStates.Inactive) return;


            CurrentState = ShieldStates.Starting;
            UpdateAnimator();
            ShieldRaiseFeedback?.PlayFeedbacks();

            _FpCharacterMovement.SetMovement(Vector2.zero);
            _FpCharacterMovement.MovementForbidden = true;

            if (ShieldProtectionArea != null) ShieldProtectionArea.ShieldIsActive = true;
        }

        public virtual void LowerShield()
        {
            if (CurrentState == ShieldStates.Inactive || CurrentState == ShieldStates.Broken) return;


            CurrentState = ShieldStates.Inactive;
            UpdateAnimator();
            ShieldLowerFeedback?.PlayFeedbacks();

            _FpCharacterMovement.MovementForbidden = false;


            if (ShieldProtectionArea != null) ShieldProtectionArea.ShieldIsActive = false;
        }

        public virtual void ProcessDamage(float damage)
        {
            if (CurrentState != ShieldStates.Active && CurrentState != ShieldStates.Starting) return;

            CurrentState = ShieldStates.Blocking;
            UpdateAnimator();
            ShieldBlockFeedback?.PlayFeedbacks();

            CurrentShieldHealth -= damage;
            if (CurrentShieldHealth <= 0) BreakShield();
        }

        protected virtual void BreakShield()
        {
            CurrentState = ShieldStates.Broken;
            UpdateAnimator();
            ShieldBreakFeedback?.PlayFeedbacks();
            _recoveryTimer = RecoveryTime;
        }

        protected virtual void UpdateAnimator()
        {
            if (_animator != null) _animator.SetInteger(_shieldStateParameter, (int)CurrentState);
        }
    }
}
