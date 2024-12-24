using MoreMountains.Tools;
using UnityEngine;

namespace Combat.Weapons.Scripts
{
    [AddComponentMenu("Roguelike/Weapon Attachment Type")]
    public class FPWeaponAttachmentTypeComponent : MMMonoBehaviour 
    {
        [Tooltip("Defines the attachment type for this weapon.")]
        public WeaponAttachmentType AttachmentType;
        [Tooltip("Determine if WeaponIK should be used for this weapon.")]
        public bool UseWeaponIK;
    }
}
