using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Animaciones de un <see cref="MeleeEnemy"/>: idle, walk, attack (anticipación +
    /// golpe + recuperación en un solo clip), hurt, stunned y death. Los nombres son
    /// los tags del .aseprite.
    /// </summary>
    [RequireComponent(typeof(MeleeEnemy))]
    public class EnemyAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [Tooltip("Velocidad de la animación de caminar mientras persigue.")]
        [SerializeField] float chaseAnimationSpeed = 1.8f;

        static readonly int Idle = Animator.StringToHash("idle");
        static readonly int Walk = Animator.StringToHash("walk");
        static readonly int Attack = Animator.StringToHash("attack");
        static readonly int Hurt = Animator.StringToHash("hurt");
        static readonly int Stunned = Animator.StringToHash("stunned");
        static readonly int Death = Animator.StringToHash("death");

        MeleeEnemy enemy;
        Rigidbody2D rb;
        int current;
        bool restart;

        void Awake()
        {
            enemy = GetComponent<MeleeEnemy>();
            rb = GetComponent<Rigidbody2D>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        void OnEnable() => enemy.StateChanged += OnStateChanged;
        void OnDisable() => enemy.StateChanged -= OnStateChanged;

        // El ataque arranca en la anticipación; golpe y recuperación siguen el mismo clip.
        void OnStateChanged(MeleeEnemy.State state) =>
            restart |= state is MeleeEnemy.State.Windup or MeleeEnemy.State.Hurt or MeleeEnemy.State.Dead or MeleeEnemy.State.Stunned;

        void LateUpdate()
        {
            if (animator == null)
                return;

            var state = enemy.Current;
            int next = state switch
            {
                MeleeEnemy.State.Windup or MeleeEnemy.State.Attack or MeleeEnemy.State.Recovery => Attack,
                MeleeEnemy.State.Hurt => Hurt,
                MeleeEnemy.State.Stunned => Stunned,
                MeleeEnemy.State.Dead => Death,
                _ => Mathf.Abs(rb.linearVelocity.x) > 0.1f ? Walk : Idle,
            };

            animator.speed = next == Walk && state == MeleeEnemy.State.Chase ? chaseAnimationSpeed : 1f;
            if (next == current && !restart)
                return;
            animator.Play(next, 0, 0f);
            current = next;
            restart = false;
        }
    }
}
