using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Valores de la pelea contra Azazel. Distancias en unidades (1 = 16 px),
    /// tiempos en segundos. Los tiempos de los ataques tienen que coincidir con
    /// las animaciones de azazel.aseprite (ver Tools/placeholder-art/README.md).
    /// </summary>
    [CreateAssetMenu(menuName = "Game01/Azazel Stats", fileName = "AzazelStats")]
    public class AzazelStats : ScriptableObject
    {
        [Header("Vida y fases")]
        public int maxHealth = 300;
        [Tooltip("Con esta fracción de vida pasa a la fase 2.")]
        [Range(0.1f, 0.9f)] public float phase2Threshold = 0.5f;
        [Tooltip("En la fase 2 los ataques (y sus animaciones) duran esto: 0.8 = 20% más rápidos.")]
        public float phase2TimeMultiplier = 0.8f;
        public float phase2SpeedMultiplier = 1.25f;
        [Tooltip("Rugido de transición: invulnerable mientras dura.")]
        public float roarDuration = 1.5f;
        public float deathDuration = 1.35f;

        [Header("Ritmo")]
        public float pauseBetweenMoves = 0.7f;
        public float phase2PauseBetweenMoves = 0.4f;
        public float walkSpeed = 2.2f;
        public float acceleration = 25f;
        public float gravityScale = 4f;
        [Tooltip("Tiempo máximo caminando hacia el jugador antes de atacar igual.")]
        public float approachTimeout = 2f;

        [Header("Zarpazo (parreable)")]
        public float clawRange = 3f;
        public float clawWindup = 0.6f;
        public float clawActive = 0.12f;
        public float clawRecovery = 0.7f;
        [Tooltip("Segundo golpe del combo: ascendente y con menos aviso.")]
        public float fastClawWindup = 0.35f;
        public float fastClawRecovery = 0.5f;
        [Tooltip("Pausa entre el primer y el segundo golpe del combo.")]
        public float comboGap = 0.25f;
        public int clawDamage = 25;
        public float clawKnockback = 8f;
        [Tooltip("Impulso hacia adelante en el golpe: corto y violento.")]
        public float clawLunge = 9f;
        public Vector2 clawHitboxSize = new(3.6f, 3f);
        [Tooltip("Centro de la hitbox desde los pies, mirando a la derecha.")]
        public Vector2 clawHitboxOffset = new(2f, 2.2f);

        [Header("Parry")]
        public float stunTime = 1.2f;
        public float stunnedDamageMultiplier = 1.5f;

        [Header("Embestida (imparable)")]
        public float chargeWindup = 0.7f;
        public float chargeSpeed = 14f;
        public float chargeMaxDuration = 2.5f;
        public float chargeRecovery = 0.8f;
        public int chargeDamage = 30;
        public float chargeKnockback = 10f;
        public Vector2 chargeHitboxSize = new(2.6f, 3.6f);
        public Vector2 chargeHitboxOffset = new(0.8f, 2f);
        [Tooltip("Distancia mínima al jugador para elegir la embestida.")]
        public float chargeMinDistance = 5f;

        [Header("Espíritus")]
        public int spiritsPhase1 = 3;
        public int spiritsPhase2 = 5;
        public float summonDuration = 0.8f;
        public float summonCooldown = 7f;
        public float orbitRadius = 3f;
        public float orbitHeight = 2.8f;
        [Tooltip("Grados por segundo.")]
        public float orbitSpeed = 110f;
        [Tooltip("Cuánto orbita el primer espíritu antes de lanzarse.")]
        public float firstLaunchDelay = 1.2f;
        [Tooltip("Tiempo entre lanzamientos de un espíritu y el siguiente.")]
        public float launchInterval = 1f;
        [Tooltip("Aviso (destello) antes de que un espíritu salga disparado.")]
        public float spiritTelegraph = 0.45f;
        public float spiritSpeed = 9f;
        public float spiritLifetime = 4f;
        public int spiritDamage = 15;
        [Tooltip("Daño a Azazel de un espíritu devuelto con parry.")]
        public int reflectedSpiritDamage = 30;

        [Header("Lluvia de almas (fase 2)")]
        public float rainCooldown = 10f;
        public float rainHoverHeight = 2.5f;
        public int rainWaves = 3;
        [Tooltip("Separación entre columnas; siempre queda un hueco para esquivar.")]
        public float rainColumnSpacing = 3f;
        [Tooltip("Aviso en el piso antes de que caigan.")]
        public float rainWarning = 0.6f;
        public float rainWaveInterval = 0.8f;
        public float rainFallSpeed = 12f;
        public int rainDamage = 15;
    }
}
