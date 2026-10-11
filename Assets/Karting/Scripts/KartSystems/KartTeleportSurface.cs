using Cinemachine;
using UnityEngine;

namespace KartGame.KartSystems
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider)), AddComponentMenu("Kart/Suelo que teletransporta")]
    public class KartTeleportSurface : MonoBehaviour
    {
        [Tooltip("GameObject vacío: su posición y rotación indican dónde y hacia dónde reaparece el kart.")]
        public Transform destination;
        [Tooltip("Únicamente este kart se teletransporta; las rocas y otros objetos se ignoran.")]
        public ArcadeKart player;
        [Min(0), Tooltip("Espera entre teletransportes para ignorar contactos duplicados.")]
        public float cooldown = .75f;

        Collider m_Surface;
        float m_NextTeleport;
        void Awake() => m_Surface = GetComponent<Collider>();

        void OnCollisionEnter(Collision collision) => Touch(collision.collider);
        void OnCollisionStay(Collision collision) => Touch(collision.collider);
        void OnTriggerEnter(Collider other) => Touch(other);
        void Touch(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body != null && body.TryGetComponent<ArcadeKart>(out var kart)) TryTeleport(kart);
        }

        // Wheel suspension can support the kart without its body ever colliding with the floor.
        void FixedUpdate()
        {
            if (player == null || destination == null || !player.isActiveAndEnabled || Time.time < m_NextTeleport) return;
            if (Touches(player.FrontLeftWheel) || Touches(player.FrontRightWheel) ||
                Touches(player.RearLeftWheel) || Touches(player.RearRightWheel)) TryTeleport(player);
        }
        bool Touches(WheelCollider wheel) => wheel != null && wheel.isGrounded &&
            wheel.GetGroundHit(out WheelHit hit) && hit.collider == m_Surface;

        public bool TryTeleport(ArcadeKart kart)
        {
            if (!isActiveAndEnabled || destination == null || kart == null || kart != player ||
                !kart.isActiveAndEnabled || kart.Rigidbody == null || Time.time < m_NextTeleport) return false;
            m_NextTeleport = Time.time + Mathf.Max(.02f, cooldown);
            Vector3 delta = destination.position - kart.transform.position;
            kart.TeleportTo(destination.position, destination.rotation);
            Physics.SyncTransforms();
            // Warp camera history too; the chase camera must not travel through the room.
            var cameras = CinemachineCore.Instance;
            cameras.OnTargetObjectWarped(kart.transform, delta);
            for (int i = 0; i < cameras.VirtualCameraCount; i++)
            {
                var camera = cameras.GetVirtualCamera(i);
                if (camera.Follow != null && (camera.Follow == kart.transform || camera.Follow.IsChildOf(kart.transform)))
                    camera.PreviousStateIsValid = false;
            }
            return true;
        }

        void OnDrawGizmosSelected()
        {
            if (destination == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(destination.position, .65f);
            Gizmos.DrawLine(destination.position, destination.position + destination.forward * 4);
        }
    }
}
