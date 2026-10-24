using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Elige la animación del sprite según el estado del <see cref="PlayerController"/>.
    /// Los nombres son los tags del archivo .aseprite; Unity crea un estado del
    /// Animator por tag. Las duraciones de los clips de ataque, dash, cura y daño
    /// coinciden con los tiempos de <see cref="PlayerStats"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [Tooltip("Velocidad horizontal a partir de la cual se usa la animación de correr.")]
        [SerializeField] float runThreshold = 0.5f;
        [Tooltip("Cuánto se muestra el destello del parry antes de volver a la guardia.")]
        [SerializeField] float parryDisplayTime = 0.2f;

        static readonly int Idle = Animator.StringToHash("idle");
        static readonly int Run = Animator.StringToHash("run");
        static readonly int Jump = Animator.StringToHash("jump");
        static readonly int Fall = Animator.StringToHash("fall");
        static readonly int Attack01 = Animator.StringToHash("attack_01");
        static readonly int Attack02 = Animator.StringToHash("attack_02");
        static readonly int Dash = Animator.StringToHash("dash");
        static readonly int Block = Animator.StringToHash("block");
        static readonly int Parry = Animator.StringToHash("parry");
        static readonly int Heal = Animator.StringToHash("heal");
        static readonly int Hurt = Animator.StringToHash("hurt");

        PlayerController player;
        Rigidbody2D rb;
        int current;
        bool restart;
        float parryTimer;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        void OnEnable()
        {
            player.StateChanged += OnStateChanged;
            player.Parried += OnParried;
        }

        void OnDisable()
        {
            player.StateChanged -= OnStateChanged;
            player.Parried -= OnParried;
        }

        // Cada entrada a un estado reinicia su animación (por ejemplo, dos ataques seguidos).
        void OnStateChanged(PlayerController.State state)
        {
            restart = true;
            if (state != PlayerController.State.Block)
                parryTimer = 0f;
        }

        void OnParried()
        {
            parryTimer = parryDisplayTime;
            restart = true;
        }

        void LateUpdate()
        {
            if (animator == null)
                return;
            parryTimer -= Time.deltaTime;

            int next = Choose();
            if (next == current && !restart)
                return;

            // Al volver del parry a la guardia, se muestra directo la pose final del bloqueo.
            float startTime = next == Block && current == Parry ? 1f : 0f;
            animator.Play(next, 0, startTime);
            current = next;
            restart = false;
        }

        int Choose()
        {
            switch (player.Current)
            {
                case PlayerController.State.Dead:
                case PlayerController.State.Hurt:
                    return Hurt;
                case PlayerController.State.Heal:
                    return Heal;
                case PlayerController.State.Block:
                    return parryTimer > 0f ? Parry : Block;
                case PlayerController.State.Attack:
                    return player.ComboIndex == 0 ? Attack01 : Attack02;
                case PlayerController.State.Dash:
                    return Dash;
                default:
                    if (player.IsGrounded)
                        return Mathf.Abs(rb.linearVelocity.x) > runThreshold ? Run : Idle;
                    return rb.linearVelocity.y > 0f ? Jump : Fall;
            }
        }
    }
}
