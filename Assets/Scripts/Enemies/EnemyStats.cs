using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Valores de un enemigo cuerpo a cuerpo. Cada tipo de enemigo tiene su asset
    /// en Assets/Data/Enemies; el mismo <see cref="MeleeEnemy"/> sirve para todos.
    /// Distancias en unidades (1 = 16 px), tiempos en segundos.
    /// </summary>
    [CreateAssetMenu(menuName = "Game01/Enemy Stats", fileName = "EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Vida")]
        public int maxHealth = 40;
        [Tooltip("Tiempo aturdido por un golpe (debe coincidir con la animación hurt).")]
        public float hurtStagger = 0.25f;
        public float hurtKnockbackMultiplier = 1f;
        [Tooltip("Duración de la animación de muerte.")]
        public float deathDuration = 0.9f;

        [Header("Movimiento")]
        public float patrolSpeed = 1.5f;
        public float chaseSpeed = 3.5f;
        public float acceleration = 30f;
        [Tooltip("Distancia máxima desde su punto de inicio mientras patrulla.")]
        public float patrolDistance = 4f;
        public float patrolPause = 1f;
        public float gravityScale = 4f;

        [Header("Percepción")]
        public float detectionRange = 7f;
        [Tooltip("Si el jugador se aleja más que esto, deja de perseguirlo.")]
        public float loseRange = 11f;
        [Tooltip("Diferencia de altura máxima para detectarlo o atacarlo.")]
        public float verticalTolerance = 2.5f;
        [Tooltip("Detecta aunque esté de espaldas si el jugador está más cerca que esto.")]
        public float backDetectionRange = 2f;
        public bool requireLineOfSight = true;
        public float eyeHeight = 1.4f;

        [Header("Ataque")]
        public float attackRange = 1.6f;
        [Tooltip("Anticipación: el aviso para el jugador. Debe coincidir con la animación.")]
        public float attackWindup = 0.5f;
        public float attackActive = 0.1f;
        public float attackRecovery = 0.6f;
        public float attackCooldown = 0.6f;
        public int attackDamage = 15;
        public float attackKnockback = 6f;
        [Tooltip("Impulso hacia adelante al lanzar el golpe.")]
        public float attackLunge = 4f;
        public Vector2 hitboxSize = new(1.6f, 1.4f);
        [Tooltip("Centro de la hitbox desde los pies, mirando a la derecha.")]
        public Vector2 hitboxOffset = new(0.9f, 0.8f);
        [Tooltip("Cada cuántos ataques uno es imparable. 0 = nunca.")]
        public int unblockableEvery;
        [Tooltip("Si es false, los golpes no cortan la anticipación: hay que parrear o esquivar.")]
        public bool windupInterruptible;

        [Header("Parry")]
        public float stunTime = 1.5f;
        public float stunnedDamageMultiplier = 2f;
    }
}
