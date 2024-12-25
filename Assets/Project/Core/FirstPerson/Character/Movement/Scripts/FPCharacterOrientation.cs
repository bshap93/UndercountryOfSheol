using Character.Scripts;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Character.Movement.Scripts
{
    public class FPCharacterOrientation : CharacterOrientation3D
    {
        /// <summary>
        /// Forces the character's model to face in the specified direction
        /// </summary>
        /// <param name="direction"></param>
        public new void Face(FirstPersonCharacter.FacingDirections direction)
        {
            switch (direction)
            {
                case FirstPersonCharacter.FacingDirections.East:
                    _newMovementQuaternion = Quaternion.LookRotation(Vector3.right);
                    break;
                case FirstPersonCharacter.FacingDirections.North:
                    _newMovementQuaternion = Quaternion.LookRotation(Vector3.forward);
                    break;
                case FirstPersonCharacter.FacingDirections.South:
                    _newMovementQuaternion = Quaternion.LookRotation(Vector3.back);
                    break;
                case FirstPersonCharacter.FacingDirections.West:
                    _newMovementQuaternion = Quaternion.LookRotation(Vector3.left);
                    break;
            }
        } 
    }
}
