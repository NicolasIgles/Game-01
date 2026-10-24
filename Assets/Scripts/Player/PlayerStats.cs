using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Todos los valores ajustables del jugador. Se editan en el asset
    /// Assets/Data/PlayerStats sin tocar código, incluso con el juego en Play.
    /// Distancias en unidades (1 unidad = 1 tile = 16 px), tiempos en segundos.
    /// </summary>
    [CreateAssetMenu(menuName = "Game01/Player Stats", fileName = "PlayerStats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Vida")]
        public int maxHealth = 100;
        [Tooltip("Invulnerabilidad después de recibir daño.")]
        public float hurtInvulnerability = 0.8f;
        [Tooltip("Tiempo sin control después de recibir daño.")]
        public float hurtStunTime = 0.3f;
        public Vector2 hurtKnockback = new(6f, 6f);
        public float respawnDelay = 1.2f;

        [Header("Movimiento")]
        public float runSpeed = 7.5f;
        public float groundAcceleration = 110f;
        [Tooltip("Al frenar o darse vuelta en el piso. Más alto que la aceleración = paradas secas.")]
        public float groundDeceleration = 170f;
        public float airAcceleration = 60f;

        [Header("Salto")]
        [Tooltip("Altura máxima en tiles.")]
        public float jumpHeight = 3.5f;
        [Tooltip("Segundos hasta llegar al punto más alto.")]
        public float timeToApex = 0.33f;
        [Tooltip("Gravedad cerca del punto más alto mientras se mantiene el salto: < 1 = queda suspendido un instante.")]
        public float apexGravityMultiplier = 0.45f;
        [Tooltip("Velocidad vertical por debajo de la cual se considera que está en el punto más alto.")]
        public float apexThreshold = 2.5f;
        public float fallGravityMultiplier = 2.3f;
        public float maxFallSpeed = 26f;
        [Tooltip("Al soltar el botón mientras sube, la velocidad se multiplica por esto.")]
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
        [Tooltip("Margen para saltar justo después de dejar el borde.")]
        public float coyoteTime = 0.1f;
        [Tooltip("Margen para que una pulsación temprana igual cuente.")]
        public float inputBuffer = 0.12f;

        [Header("Salto de pared")]
        [Tooltip("Fragmentos de gracia necesarios. 0 = siempre disponible.")]
        public int wallJumpRequiredFragments = 1;
        [Tooltip("Velocidad máxima de caída mientras se desliza por la pared.")]
        public float wallSlideSpeed = 3f;
        [Tooltip("Altura del salto de pared, en tiles.")]
        public float wallJumpHeight = 3f;
        [Tooltip("Velocidad con la que se despega de la pared.")]
        public float wallJumpHorizontalSpeed = 7.5f;
        [Tooltip("Tiempo sin control horizontal después del salto de pared. Corto = se puede volver a la misma pared y escalarla.")]
        public float wallJumpInputLock = 0.12f;
        [Tooltip("Margen para saltar justo después de soltarse de la pared.")]
        public float wallCoyoteTime = 0.08f;

        [Header("Dash")]
        public float dashSpeed = 20f;
        public float dashDuration = 0.16f;
        [Tooltip("Ventana de invulnerabilidad dentro del dash.")]
        public float dashInvulnerableFrom = 0.02f;
        public float dashInvulnerableTo = 0.14f;
        public float dashCooldown = 0.35f;

        [Header("Ataque")]
        [Range(1, 3)] public int comboLength = 2;
        [Tooltip("Anticipación antes de que el golpe haga daño.")]
        public float attackWindup = 0.07f;
        [Tooltip("Tiempo en que la hitbox está activa.")]
        public float attackActive = 0.08f;
        [Tooltip("Recuperación; se puede cancelar con dash o encadenar el combo.")]
        public float attackRecovery = 0.25f;
        public int attackDamage = 10;
        public float attackLunge = 3f;
        public float attackKnockback = 4f;
        public Vector2 attackSize = new(1.75f, 1.5f);
        [Tooltip("Centro de la hitbox desde los pies, mirando a la derecha.")]
        public Vector2 attackOffset = new(1.1f, 1.4f);

        [Header("Bloqueo y parry")]
        [Tooltip("Primeros segundos del bloqueo en que un golpe se parrea.")]
        public float parryWindow = 0.15f;
        [Tooltip("Tiempo mínimo entre intentos de parry, para que no sirva apretar sin parar.")]
        public float parryCooldown = 0.35f;
        [Tooltip("Duración mínima del bloqueo aunque se suelte el botón.")]
        public float minBlockTime = 0.15f;
        public float blockPushback = 4f;
        public float parryInvulnerability = 0.3f;

        [Header("Curación")]
        public int healCharges = 3;
        public int healAmount = 35;
        [Tooltip("Tiempo vulnerable antes de recuperar vida. Un golpe la cancela y la carga se pierde.")]
        public float healCastTime = 0.6f;
        public float healRecovery = 0.2f;

        public float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);
        public float JumpVelocity => 2f * jumpHeight / timeToApex;
        public float WallJumpVelocity => Mathf.Sqrt(2f * Gravity * wallJumpHeight);
    }
}
