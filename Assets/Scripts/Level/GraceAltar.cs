using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game01
{
    /// <summary>
    /// Altar de gracia (Biblia, Objetos comunes): al tocarlo guarda el punto de
    /// reaparición, recarga vida y curas, y hace reaparecer a los enemigos.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GraceAltar : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] Light2D glow;
        [SerializeField] float idleIntensity = 0.5f;
        [SerializeField] float activeIntensity = 1.4f;

        static GraceAltar active;

        public void Configure(Animator anim, Light2D light)
        {
            animator = anim;
            glow = light;
        }

        void Start() => SetLit(false);

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null)
                return;

            player.SetSpawnPoint(transform.position);
            player.RefillHeals();
            if (player.TryGetComponent<Health>(out var health))
                health.ResetHealth();
            EnemyReset.ResetAll();

            if (active != this)
            {
                if (active != null)
                    active.SetLit(false);
                active = this;
                LevelMessages.Banner("Altar de gracia · vida y curas recuperadas", 2.5f);
            }
            SetLit(true);
        }

        void SetLit(bool lit)
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.Play(lit ? "activo" : "idle", 0, 0f);
            if (glow != null)
                glow.intensity = lit ? activeIntensity : idleIntensity;
        }
    }

    /// <summary>Hace reaparecer a todos los enemigos del nivel (altares y muerte del jugador).</summary>
    public static class EnemyReset
    {
        public static void ResetAll()
        {
            // Los jefes los maneja su arena: no vuelven por tocar un altar.
            foreach (var e in Object.FindObjectsByType<MeleeEnemy>(FindObjectsInactive.Exclude))
                if (e.GetComponentInParent<BossArena>() == null)
                    e.ResetEnemy();
            foreach (var e in Object.FindObjectsByType<PlaceholderEnemy>(FindObjectsInactive.Include))
                e.ResetEnemy();
        }
    }
}
