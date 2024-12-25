﻿using Combat.Scripts;
using Project.Gameplay.Combat.Shields;
using ShieldProtectionArea = Combat.Scripts.ShieldProtectionArea;

namespace Player.Scripts
{
    using System;
    using System.Collections.Generic;
    using MoreMountains.TopDownEngine;
    using UnityEngine;

    namespace Project.Gameplay.Player.Health
    {
        [Serializable]
        public class FPHealth : MoreMountains.TopDownEngine.Health
        {
            GameObject _shield;
            Shield _shieldComponent;
            ShieldProtectionArea _shieldProtection;

            protected override void Awake()
            {
                base.Awake();
                ShieldProtectionArea.OnShieldEquipped += AssignShield;
            }

            void OnDestroy()
            {
                ShieldProtectionArea.OnShieldEquipped -= AssignShield;
            }


            public override void Damage(float damage, GameObject instigator, float flickerDuration,
                float invincibilityDuration, Vector3 damageDirection, List<TypedDamage> typedDamages = null)
            {
                // Check if the shield blocks the damage
                if (_shieldProtection != null && _shieldProtection.IsBlocking(instigator.transform.position))
                {
                    Debug.Log(
                        $"Shield blocked damage from {instigator.name}, _shieldProtection: {_shieldProtection != null}, _shieldProtection.ISBlocking: {_shieldProtection.IsBlocking(instigator.transform.position)}");


                    return; // Exit early if shield blocks damage
                }

                // Otherwise, apply damage as usual
                base.Damage(damage, instigator, flickerDuration, invincibilityDuration, damageDirection, typedDamages);
            }

            void AssignShield(ShieldProtectionArea shield)
            {
                _shieldProtection = shield;
                _shield = shield.gameObject;
                _shieldComponent = _shield.GetComponent<Shield>();
                Debug.Log("Shield assigned to ShieldedHealth.");
            }
            public void SetMaximumHealth(float savedMaxHealth)
            {
                MaximumHealth = savedMaxHealth;
            }
        }
    }

}
