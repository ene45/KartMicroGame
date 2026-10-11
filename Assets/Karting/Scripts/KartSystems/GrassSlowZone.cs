using UnityEngine;

namespace KartGame.KartSystems
{
    [DisallowMultipleComponent, AddComponentMenu("Kart/Pasto que ralentiza")]
    public class GrassSlowZone : MonoBehaviour
    {
        [Range(.1f, 1f), Tooltip("0.8 conserva el 80% de la velocidad máxima habitual.")]
        public float speedMultiplier = .8f;
        [Range(.1f, 1f), Tooltip("0.9 conserva el 90% de la aceleración habitual.")]
        public float accelerationMultiplier = .9f;
        [Min(.1f), Tooltip("Desaceleración suave al entrar rápido al pasto, en metros por segundo al cuadrado.")]
        public float deceleration = 6f;
    }
}
