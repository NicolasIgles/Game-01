using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Efectos comunes del jugador (Biblia): polvo al aterrizar y al hacer dash,
    /// destello dorado de parry y chispa de bloqueo.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerFX : MonoBehaviour
    {
        [Tooltip("Velocidad de caída mínima para levantar polvo al aterrizar.")]
        [SerializeField] float landingSpeed = 8f;

        PlayerController player;
        Rigidbody2D rb;
        bool wasGrounded = true;
        float lastFallSpeed;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
        }

        void OnEnable()
        {
            player.StateChanged += OnState;
            player.Parried += OnParried;
            player.Blocked += OnBlocked;
        }

        void OnDisable()
        {
            player.StateChanged -= OnState;
            player.Parried -= OnParried;
            player.Blocked -= OnBlocked;
        }

        void FixedUpdate()
        {
            if (player.IsGrounded && !wasGrounded && lastFallSpeed > landingSpeed)
                LevelFX.Play(LevelFX.Kind.Dust, transform.position);
            wasGrounded = player.IsGrounded;
            lastFallSpeed = Mathf.Max(0f, -rb.linearVelocity.y);
        }

        void OnState(PlayerController.State state)
        {
            if (state == PlayerController.State.Dash && player.IsGrounded)
                LevelFX.Play(LevelFX.Kind.Dust, transform.position, player.Facing < 0);
        }

        Vector2 Front => (Vector2)transform.position + new Vector2(player.Facing * 0.8f, 1.4f);

        void OnParried() => LevelFX.Play(LevelFX.Kind.Parry, Front);
        void OnBlocked() => LevelFX.Play(LevelFX.Kind.Spark, Front);
    }
}
