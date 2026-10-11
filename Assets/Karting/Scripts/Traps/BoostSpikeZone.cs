using System.Collections.Generic;
using KartGame.KartSystems;
using UnityEngine;

namespace KartGame.Traps
{
    public enum LapZoneMode
    {
        [InspectorName("Inactiva")] Inactive,
        [InspectorName("Acelerador")] Boost,
        [InspectorName("Pinches")] Spikes
    }

    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("Kart/Trampas/Zona de acelerador y pinches")]
    public class BoostSpikeZone : LapTrap
    {
        [Header("Configuración por vuelta")]
        [Tooltip("Una entrada por vuelta. Si hay más vueltas, se conserva la última configuración.")]
        public LapZoneMode[] modesByLap = { LapZoneMode.Boost, LapZoneMode.Boost, LapZoneMode.Spikes };

        [Header("Pinches")]
        [Min(.1f), Tooltip("Frenado adicional en m/s². Se mantiene hasta detener el kart, incluso si sale de la zona.")]
        public float brakeDeceleration = 12f;

        [Header("Acelerador")]
        [Tooltip("Usa el mismo sistema de StatPowerup del SpeedPad original. Se crea una instancia por kart y pasada.")]
        public ArcadeKart.StatPowerup boostStats = new ArcadeKart.StatPowerup
        {
            MaxTime = 3f,
            modifiers = new ArcadeKart.Stats { TopSpeed = 5f, Acceleration = 5f }
        };

        [Header("Apariencia (sin colliders sólidos)")]
        public GameObject boostVisual;
        public GameObject spikesVisual;
        public GameObject inactiveVisual;

        public LapZoneMode CurrentMode { get; private set; }
        readonly Dictionary<ArcadeKart, HashSet<Collider>> m_Occupants = new Dictionary<ArcadeKart, HashSet<Collider>>();
        readonly Dictionary<ArcadeKart, ArcadeKart.StatPowerup> m_Boosts = new Dictionary<ArcadeKart, ArcadeKart.StatPowerup>();
        readonly List<ArcadeKart> m_ToRemove = new List<ArcadeKart>();

        void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        void Start()
        {
            // Standalone zones also use lap 1. A controller may already have applied another lap.
            if (!m_HasLap) ApplyLap(1);
        }

        bool m_HasLap;
        public LapZoneMode GetModeForLap(int lapNumber)
        {
            if (modesByLap == null || modesByLap.Length == 0) return LapZoneMode.Inactive;
            return modesByLap[Mathf.Clamp(lapNumber - 1, 0, modesByLap.Length - 1)];
        }

        public override void ApplyLap(int lapNumber)
        {
            m_HasLap = true;
            CurrentMode = GetModeForLap(lapNumber);
            SetVisual(boostVisual, CurrentMode == LapZoneMode.Boost);
            SetVisual(spikesVisual, CurrentMode == LapZoneMode.Spikes);
            SetVisual(inactiveVisual, CurrentMode == LapZoneMode.Inactive);
            // Keep the same trigger and occupants across a lap change. Only the next entry activates.
        }

        void SetVisual(GameObject visual, bool visible)
        {
            // A visual must not be the trigger root or an ancestor of it.
            if (visual != null && !transform.IsChildOf(visual.transform)) visual.SetActive(visible);
        }

        void OnTriggerEnter(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body == null || !body.TryGetComponent<ArcadeKart>(out var kart)) return;

            if (m_Occupants.TryGetValue(kart, out var colliders))
            {
                colliders.Add(other);
                return;
            }
            m_Occupants.Add(kart, new HashSet<Collider> { other });

            if (CurrentMode == LapZoneMode.Spikes)
                kart.ApplyTrapBrake(brakeDeceleration);
            else if (CurrentMode == LapZoneMode.Boost && boostStats != null)
            {
                // Re-entry refreshes this zone's boost; it cannot stack itself without limit.
                if (m_Boosts.TryGetValue(kart, out var previous)) kart.RemovePowerUp(previous);
                var powerup = new ArcadeKart.StatPowerup
                {
                    modifiers = boostStats.modifiers,
                    MaxTime = Mathf.Max(.01f, boostStats.MaxTime),
                    PowerUpID = "LapZone_" + GetInstanceID(),
                    ElapsedTime = 0f
                };
                m_Boosts[kart] = powerup;
                kart.AddPowerup(powerup);
            }
        }

        void OnTriggerExit(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body == null || !body.TryGetComponent<ArcadeKart>(out var kart)) return;
            if (!m_Occupants.TryGetValue(kart, out var colliders)) return;
            colliders.Remove(other);
            if (colliders.Count == 0) m_Occupants.Remove(kart);
        }

        void FixedUpdate()
        {
            // Unity may omit OnTriggerExit when a collider is disabled or destroyed.
            m_ToRemove.Clear();
            foreach (var entry in m_Occupants)
            {
                entry.Value.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
                if (entry.Key == null || entry.Value.Count == 0) m_ToRemove.Add(entry.Key);
            }
            foreach (var kart in m_ToRemove) m_Occupants.Remove(kart);

            m_ToRemove.Clear();
            foreach (var entry in m_Boosts)
                if (entry.Key == null || entry.Value.ElapsedTime > entry.Value.MaxTime) m_ToRemove.Add(entry.Key);
            foreach (var kart in m_ToRemove) m_Boosts.Remove(kart);
        }

        void OnDisable()
        {
            m_Occupants.Clear();
            foreach (var entry in m_Boosts)
                if (entry.Key != null) entry.Key.RemovePowerUp(entry.Value);
            m_Boosts.Clear();
        }

        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(6f, 2f, 8f);
            box.center = Vector3.up;
        }

        void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            Gizmos.color = CurrentMode == LapZoneMode.Spikes ? Color.red : Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
