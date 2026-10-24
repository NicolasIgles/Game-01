using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Peligro del escenario (brasas, estacas): hace daño que no se puede bloquear
    /// y devuelve al jugador al último suelo firme (Biblia, Objetos comunes).
    /// Va en un collider marcado como trigger, por ejemplo el tilemap de peligros.
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        [SerializeField] int damage = 15;
        [SerializeField] float cooldown = 0.5f;

        float nextHit;

        void OnTriggerEnter2D(Collider2D other) => Touch(other);
        void OnTriggerStay2D(Collider2D other) => Touch(other);

        void Touch(Collider2D other)
        {
            if (Time.time < nextHit)
                return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.Current == PlayerController.State.Dead)
                return;
            nextHit = Time.time + cooldown;

            player.ReceiveHit(new HitInfo
            {
                Damage = damage,
                SourcePosition = player.transform.position,
                Knockback = 0f,
                Unblockable = true,
            });
            if (player.Current != PlayerController.State.Dead && player.TryGetComponent<SafeGroundTracker>(out var safe))
                safe.ReturnToSafeGround();
        }
    }
}
