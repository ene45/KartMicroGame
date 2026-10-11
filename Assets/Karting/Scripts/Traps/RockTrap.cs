using System;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.Traps
{
    public enum RockSpawnOrder
    {
        [InspectorName("Secuencia editable")] Sequence,
        [InspectorName("Aleatorio por tandas")] Shuffle
    }

    [Serializable]
    public class RockLapSettings
    {
        public bool active = true;
        [Min(.1f)] public float rollingSpeed = 6f;
        [Min(.05f)] public float spawnInterval = 4f;
        [Min(0f)] public float stunDuration = 1f;
        [Tooltip("Vacío = todos los recorridos. Desactivá la vuelta para no generar rocas.")]
        public RockRoute[] enabledRoutes = new RockRoute[0];
    }

    [DisallowMultipleComponent, AddComponentMenu("Kart/Trampas/Rocas por vuelta")]
    public class RockTrap : LapTrap
    {
        public RockHazard rockPrefab;
        public List<RockRoute> routes = new List<RockRoute>();
        public RockSpawnOrder spawnOrder = RockSpawnOrder.Shuffle;
        [Tooltip("Orden de caída, con repeticiones si querés. Vacío = orden de Recorridos.")]
        public List<RockRoute> sequence = new List<RockRoute>();
        [Min(0f)] public float initialDelay = 1f;
        [Min(1)] public int maximumActiveRocks = 8;
        public bool waitForRaceStart = true;
        public TimeManager raceTimer;
        public RockLapSettings[] laps = {
            new RockLapSettings(),
            new RockLapSettings { rollingSpeed = 9, spawnInterval = 3, stunDuration = 1.5f },
            new RockLapSettings { rollingSpeed = 12, spawnInterval = 2, stunDuration = 2 }
        };
        [Range(0f, 1f)] public float damageDarkening = .55f;
        [Min(0f)] public float damageBlinksPerSecond = 5f;

        readonly List<RockHazard> m_Active = new List<RockHazard>();
        readonly List<RockRoute> m_Bag = new List<RockRoute>();
        RockRoute m_LastRoute;
        int m_SequenceIndex;
        float m_UntilNextSpawn;
        bool m_HasLap, m_RaceHasStarted;
        public int CurrentLap { get; private set; } = 1;
        public int SpawnedCount { get; private set; }
        public int ActiveRockCount { get { m_Active.RemoveAll(rock => rock == null); return m_Active.Count; } }
        public event Action<RockRoute> RockSpawned;

        public RockLapSettings GetSettingsForLap(int lap)
        {
            if (laps == null || laps.Length == 0) return null;
            return laps[Mathf.Clamp(lap - 1, 0, laps.Length - 1)];
        }

        public override void ApplyLap(int lapNumber)
        {
            int next = Mathf.Max(1, lapNumber);
            if (m_HasLap && CurrentLap == next) return;
            CurrentLap = next;
            m_Bag.Clear(); m_SequenceIndex = 0;
            float interval = Mathf.Max(.05f, GetSettingsForLap(CurrentLap)?.spawnInterval ?? 4f);
            m_UntilNextSpawn = m_HasLap ? Mathf.Max(m_UntilNextSpawn, interval) : Mathf.Max(0f, initialDelay);
            m_HasLap = true;
        }

        void Start()
        {
            if (raceTimer == null)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    raceTimer = root.GetComponentInChildren<TimeManager>();
                    if (raceTimer != null) break;
                }
            if (!m_HasLap) ApplyLap(1);
            if (rockPrefab == null) Debug.LogWarning("Asigná el prefab de roca al controlador.", this);
            if (waitForRaceStart && raceTimer == null)
                Debug.LogWarning("Asigná el TimeManager de la carrera o desactivá Esperar inicio de carrera.", this);
        }

        void FixedUpdate()
        {
            if (waitForRaceStart && (raceTimer == null || !raceTimer.IsRaceStarted))
            {
                if (m_RaceHasStarted) ClearRocks();
                return;
            }
            m_RaceHasStarted = true;
            var settings = GetSettingsForLap(CurrentLap);
            if (settings == null || !settings.active || rockPrefab == null) return;
            m_UntilNextSpawn -= Time.fixedDeltaTime;
            if (m_UntilNextSpawn > 0f || ActiveRockCount >= Mathf.Max(1, maximumActiveRocks)) return;
            // One emission per step, never a catch-up burst after a pause or lap change.
            m_UntilNextSpawn = Mathf.Max(.05f, settings.spawnInterval);
            RockRoute route = SelectRoute(settings);
            if (route == null) return;
            var rock = Instantiate(rockPrefab, route.spawnPoint.position, Quaternion.identity);
            rock.name = "Roca_" + route.name + "_" + (SpawnedCount + 1);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rock.gameObject, gameObject.scene);
            if (!rock.Initialize(route, settings.rollingSpeed, settings.stunDuration, damageDarkening, damageBlinksPerSecond))
            {
                Destroy(rock.gameObject); return;
            }
            m_Active.Add(rock); m_LastRoute = route; SpawnedCount++;
            RockSpawned?.Invoke(route);
        }

        bool Eligible(RockRoute route, RockLapSettings settings)
        {
            if (route == null || !route.isActiveAndEnabled || !routes.Contains(route)) return false;
            if (settings.enabledRoutes != null && settings.enabledRoutes.Length > 0 &&
                Array.IndexOf(settings.enabledRoutes, route) < 0) return false;
            return route.TryGetPath(out _);
        }

        RockRoute SelectRoute(RockLapSettings settings)
        {
            if (spawnOrder == RockSpawnOrder.Sequence)
            {
                var order = sequence != null && sequence.Count > 0 ? sequence : routes;
                if (order == null || order.Count == 0) return null;
                for (int i = 0; i < order.Count; i++)
                {
                    var route = order[m_SequenceIndex++ % order.Count];
                    if (Eligible(route, settings)) return route;
                }
                return null;
            }
            m_Bag.RemoveAll(route => !Eligible(route, settings));
            if (m_Bag.Count == 0)
            {
                foreach (var route in routes)
                    if (Eligible(route, settings) && !m_Bag.Contains(route)) m_Bag.Add(route);
                for (int i = m_Bag.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    var swap = m_Bag[i]; m_Bag[i] = m_Bag[j]; m_Bag[j] = swap;
                }
                if (m_Bag.Count > 1 && m_Bag[0] == m_LastRoute)
                {
                    var swap = m_Bag[0]; m_Bag[0] = m_Bag[1]; m_Bag[1] = swap;
                }
            }
            if (m_Bag.Count == 0) return null;
            var selected = m_Bag[0]; m_Bag.RemoveAt(0); return selected;
        }

        void ClearRocks()
        {
            foreach (var rock in m_Active)
                if (rock != null) { rock.gameObject.SetActive(false); Destroy(rock.gameObject); }
            m_Active.Clear();
        }

        void OnDisable()
        {
            ClearRocks(); m_Bag.Clear(); m_RaceHasStarted = false;
            m_UntilNextSpawn = Mathf.Max(0f, initialDelay);
        }
    }
}
