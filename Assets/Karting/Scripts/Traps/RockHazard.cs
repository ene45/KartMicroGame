using System.Collections.Generic;
using KartGame.KartSystems;
using UnityEngine;

namespace KartGame.Traps
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    [AddComponentMenu("Kart/Trampas/Roca de movimiento controlado")]
    public class RockHazard : MonoBehaviour
    {
        [Tooltip("Solo esta parte gira visualmente; el recorrido permanece fijo.")]
        public Transform visual;
        public LayerMask kartLayers = ~0;
        readonly HashSet<ArcadeKart> m_HitKarts = new HashSet<ArcadeKart>();
        Rigidbody m_Body;
        Vector3 m_Spawn;
        Vector3[] m_Path;
        float m_FallDuration, m_FallElapsed, m_Stun, m_Darkening, m_Blinks, m_Radius;
        int m_NextPoint;
        bool m_Initialized, m_Finished;
        public float RollingSpeed { get; private set; }
        public bool IsRolling => m_Initialized && m_FallElapsed >= m_FallDuration;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Body.isKinematic = true;
            m_Body.useGravity = false;
            var sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true; sphere.center = Vector3.zero; sphere.radius = .5f;
        }

        public bool Initialize(RockRoute route, float speed, float stunSeconds, float darkening, float blinks)
        {
            if (route == null || !route.TryGetPath(out m_Path)) return false;
            m_Spawn = route.spawnPoint.position;
            m_FallDuration = Mathf.Max(.01f, route.fallDuration);
            m_FallElapsed = 0f; m_NextPoint = 1; m_Finished = false;
            RollingSpeed = Mathf.Max(.1f, speed);
            m_Stun = Mathf.Max(0f, stunSeconds);
            m_Darkening = Mathf.Clamp01(darkening); m_Blinks = Mathf.Max(0f, blinks);
            float diameter = Mathf.Max(.1f, route.diameter);
            transform.localScale = Vector3.one * diameter;
            m_Radius = diameter * .5f;
            m_Body.position = m_Spawn;
            m_HitKarts.Clear(); m_Initialized = true;
            return true;
        }

        void FixedUpdate()
        {
            if (!m_Initialized || m_Finished) return;
            float delta = Time.fixedDeltaTime;
            if (!IsRolling)
            {
                m_FallElapsed += delta;
                float t = Mathf.Clamp01(m_FallElapsed / m_FallDuration);
                MoveTo(Vector3.Lerp(m_Spawn, m_Path[0], t * t), false);
                delta = Mathf.Max(0f, m_FallElapsed - m_FallDuration);
                if (delta == 0f) return;
            }
            float distance = RollingSpeed * delta;
            while (distance > 0f && m_NextPoint < m_Path.Length)
            {
                float length = Vector3.Distance(m_Body.position, m_Path[m_NextPoint]);
                if (length <= distance)
                {
                    MoveTo(m_Path[m_NextPoint++], true); distance -= length;
                }
                else
                {
                    MoveTo(Vector3.MoveTowards(m_Body.position, m_Path[m_NextPoint], distance), true);
                    distance = 0f;
                }
            }
            if (m_NextPoint >= m_Path.Length)
            {
                m_Finished = true;
                GetComponent<SphereCollider>().enabled = false;
                Destroy(gameObject);
            }
        }

        void MoveTo(Vector3 position, bool roll)
        {
            Vector3 from = m_Body.position, movement = position - from;
            // Sweeps catch fast rocks that cross a kart between two simulation steps.
            foreach (var collider in Physics.OverlapSphere(from, m_Radius, kartLayers, QueryTriggerInteraction.Collide))
                Hit(collider);
            float distance = movement.magnitude;
            if (distance > .00001f)
                foreach (var hit in Physics.SphereCastAll(from, m_Radius, movement / distance, distance,
                    kartLayers, QueryTriggerInteraction.Collide)) Hit(hit.collider);
            foreach (var collider in Physics.OverlapSphere(position, m_Radius, kartLayers, QueryTriggerInteraction.Collide))
                Hit(collider);
            m_Body.position = position;
            if (roll && visual != null && visual != transform && distance > .00001f)
            {
                Vector3 axis = Vector3.Cross(Vector3.up, movement.normalized);
                if (axis.sqrMagnitude > .00001f)
                    visual.Rotate(axis.normalized, distance / m_Radius * Mathf.Rad2Deg, Space.World);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_Initialized && !m_Finished) Hit(other);
        }

        void Hit(Collider other)
        {
            if (other == null || (kartLayers.value & (1 << other.gameObject.layer)) == 0) return;
            var body = other.attachedRigidbody;
            if (body == null || !body.TryGetComponent<ArcadeKart>(out var kart) ||
                !kart.isActiveAndEnabled || kart.gameObject.scene != gameObject.scene || !m_HitKarts.Add(kart)) return;
            kart.ApplyTrapStun(m_Stun);
            if (!kart.TryGetComponent<KartDamageFeedback>(out var feedback))
                feedback = kart.gameObject.AddComponent<KartDamageFeedback>();
            feedback.Show(m_Stun, m_Darkening, m_Blinks);
        }
    }
}
