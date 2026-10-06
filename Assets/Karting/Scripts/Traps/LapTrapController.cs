using System;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.Traps
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Kart/Trampas/Controlador por vueltas")]
    public class LapTrapController : MonoBehaviour
    {
        [Tooltip("Objetivo que cuenta las vueltas del jugador de esta carrera.")]
        public ObjectiveCompleteLaps lapObjective;
        [Tooltip("Zonas y futuras trampas que reciben la vuelta actual (1, 2, 3...).")]
        public List<LapTrap> traps = new List<LapTrap>();

        public int CurrentLap { get; private set; } = 1;
        public event Action<int> LapChanged;
        ObjectiveCompleteLaps m_SubscribedObjective;
        bool m_HasAppliedLap;

        void OnEnable()
        {
            if (lapObjective == null)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    foreach (var candidate in root.GetComponentsInChildren<ObjectiveCompleteLaps>())
                        if (candidate.isActiveAndEnabled && candidate.gameMode == GameMode.Laps)
                        {
                            lapObjective = candidate;
                            break;
                        }
                    if (lapObjective != null) break;
                }

            m_SubscribedObjective = lapObjective;
            if (m_SubscribedObjective != null)
                m_SubscribedObjective.LapStarted += ApplyLap;
            ApplyLap(lapObjective != null ? lapObjective.CurrentRaceLap : 1);
        }

        void Start()
        {
            int current = lapObjective != null ? lapObjective.CurrentRaceLap : 1;
            if (!m_HasAppliedLap || CurrentLap != current) ApplyLap(current);
            if (lapObjective == null)
                Debug.LogWarning("Asigná un ObjectiveCompleteLaps al controlador de trampas.", this);
        }

        void OnDisable()
        {
            if (m_SubscribedObjective != null)
                m_SubscribedObjective.LapStarted -= ApplyLap;
            m_SubscribedObjective = null;
        }

        /// <summary>Also usable by future race modes; the number is one-based.</summary>
        public void ApplyLap(int lapNumber)
        {
            int next = Mathf.Max(1, lapNumber);
            bool changed = !m_HasAppliedLap || CurrentLap != next;
            CurrentLap = next;
            m_HasAppliedLap = true;
            foreach (var trap in traps)
                if (trap != null) trap.ApplyLap(CurrentLap);
            if (changed) LapChanged?.Invoke(CurrentLap);
        }
    }
}
