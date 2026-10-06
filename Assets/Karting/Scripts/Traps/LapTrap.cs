using UnityEngine;

namespace KartGame.Traps
{
    /// <summary>Implement this contract for any trap that changes with the player's lap.</summary>
    public abstract class LapTrap : MonoBehaviour
    {
        public abstract void ApplyLap(int lapNumber);
    }
}
