using UnityEngine;

namespace KartGame.KartSystems
{
    // Apply the modifier before ArcadeKart reads its active powerups each physics step.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent, RequireComponent(typeof(ArcadeKart))]
    public class KartGrassSlowdown : MonoBehaviour
    {
        [Range(1, 4), Tooltip("Cantidad mínima de ruedas sobre la misma zona de pasto.")]
        public int minimumGrassWheels = 2;
        public GrassSlowZone CurrentSurface { get; private set; }

        ArcadeKart m_Kart;
        ArcadeKart.StatPowerup m_Modifier;
        readonly GrassSlowZone[] m_Samples = new GrassSlowZone[4];
        readonly Vector3[] m_Normals = new Vector3[4];

        void Awake() { m_Kart = GetComponent<ArcadeKart>(); }

        void Sample(int index, WheelCollider wheel)
        {
            m_Samples[index] = null;
            if (wheel == null || !wheel.isGrounded || !wheel.GetGroundHit(out WheelHit hit) || hit.collider == null) return;
            var surface = hit.collider.GetComponentInParent<GrassSlowZone>();
            if (surface == null || !surface.isActiveAndEnabled) return;
            m_Samples[index] = surface; m_Normals[index] = hit.normal;
        }

        void FixedUpdate()
        {
            if (m_Kart == null || !m_Kart.isActiveAndEnabled) { Release(); return; }
            Sample(0, m_Kart.FrontLeftWheel); Sample(1, m_Kart.FrontRightWheel);
            Sample(2, m_Kart.RearLeftWheel); Sample(3, m_Kart.RearRightWheel);
            GrassSlowZone selected = null; int bestCount = 0; Vector3 normal = Vector3.up;
            for (int i = 0; i < 4; i++)
            {
                if (m_Samples[i] == null) continue;
                int count = 0; Vector3 sum = Vector3.zero;
                for (int j = 0; j < 4; j++) if (m_Samples[j] == m_Samples[i]) { count++; sum += m_Normals[j]; }
                if (count > bestCount) { selected = m_Samples[i]; bestCount = count; normal = sum.normalized; }
            }
            if (bestCount < Mathf.Clamp(minimumGrassWheels, 1, 4)) { Release(); return; }
            CurrentSurface = selected;
            if (m_Modifier == null)
            {
                m_Modifier = new ArcadeKart.StatPowerup { PowerUpID = "Grass_" + GetInstanceID(), MaxTime = float.PositiveInfinity };
                m_Kart.AddPowerup(m_Modifier);
            }
            float speed = Mathf.Clamp(selected.speedMultiplier, .1f, 1f);
            float acceleration = Mathf.Clamp(selected.accelerationMultiplier, .1f, 1f);
            m_Modifier.modifiers = new ArcadeKart.Stats
            {
                TopSpeed = m_Kart.baseStats.TopSpeed * (speed - 1f),
                ReverseSpeed = m_Kart.baseStats.ReverseSpeed * (speed - 1f),
                Acceleration = m_Kart.baseStats.Acceleration * (acceleration - 1f),
                ReverseAcceleration = m_Kart.baseStats.ReverseAcceleration * (acceleration - 1f)
            };
            if (m_Kart.IsTrapStunned || m_Kart.Rigidbody == null) return;
            var velocity = m_Kart.Rigidbody.linearVelocity;
            var alongGround = Vector3.ProjectOnPlane(velocity, normal);
            // Existing boosts remain additive. Do not brake the vertical motion of jumps.
            float limit = Vector3.Dot(alongGround, transform.forward) < 0
                ? m_Kart.baseStats.ReverseSpeed * speed : m_Kart.GetMaxSpeed();
            limit = Mathf.Max(.1f, limit);
            if (alongGround.magnitude > limit)
            {
                var reduced = Vector3.MoveTowards(alongGround, alongGround.normalized * limit,
                    Mathf.Max(.1f, selected.deceleration) * Time.fixedDeltaTime);
                m_Kart.Rigidbody.linearVelocity = velocity - alongGround + reduced;
            }
        }

        void Release()
        {
            if (m_Modifier != null && m_Kart != null) m_Kart.RemovePowerUp(m_Modifier);
            m_Modifier = null; CurrentSurface = null;
        }
        void OnDisable() { Release(); }
    }
}
