using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Elige la animación del sprite según el estado del <see cref="PlayerController"/>.
    /// Los nombres de estado son los tags del archivo .aseprite (idle, run, ...).
    /// Mientras falten animaciones, los estados sin animación propia usan idle
    /// y se distinguen por el color de depuración del controlador.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [Tooltip("Velocidad horizontal a partir de la cual se usa la animación de correr.")]
        [SerializeField] float runThreshold = 0.5f;

        static readonly int Idle = Animator.StringToHash("idle");
        static readonly int Run = Animator.StringToHash("run");

        PlayerController player;
        Rigidbody2D rb;
        int current;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        void LateUpdate()
        {
            if (animator == null)
                return;

            bool running = player.Current == PlayerController.State.Dash ||
                           (player.Current == PlayerController.State.Normal && Mathf.Abs(rb.linearVelocity.x) > runThreshold);
            int next = running ? Run : Idle;
            if (next == current)
                return;
            animator.Play(next, 0, 0f);
            current = next;
        }
    }
}
