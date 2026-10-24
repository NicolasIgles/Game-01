using System;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Eventos globales de combate. Los que pegan y reciben solo avisan; la
    /// respuesta (sonido, pausa de impacto, temblor) la decide <see cref="CombatFeedback"/>.
    /// </summary>
    public static class CombatEvents
    {
        public delegate void HitHandler(IDamageable target, HitResult result, in HitInfo hit);

        /// <summary>Un golpe de hitbox conectó (no se dispara si el objetivo era invulnerable).</summary>
        public static event HitHandler HitLanded;
        /// <summary>Un enemigo empezó la anticipación de un ataque: (posición, imparable).</summary>
        public static event Action<Vector2, bool> EnemyWindup;
        /// <summary>Murió un enemigo, en esta posición.</summary>
        public static event Action<Vector2> EnemyDied;

        internal static void RaiseHit(IDamageable target, HitResult result, in HitInfo hit) => HitLanded?.Invoke(target, result, hit);
        public static void RaiseEnemyWindup(Vector2 position, bool unblockable) => EnemyWindup?.Invoke(position, unblockable);
        public static void RaiseEnemyDied(Vector2 position) => EnemyDied?.Invoke(position);

        // Sin recarga de dominio al entrar en Play, los eventos estáticos sobreviven.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            HitLanded = null;
            EnemyWindup = null;
            EnemyDied = null;
        }
    }
}
