using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    public enum HitResult { Ignored, Hit, Blocked, Parried }

    /// <summary>Datos de un golpe.</summary>
    public struct HitInfo
    {
        public int Damage;
        /// <summary>Posición de quien golpea; define de qué lado viene el golpe.</summary>
        public Vector2 SourcePosition;
        /// <summary>Velocidad de empuje sobre quien recibe (x con signo).</summary>
        public float Knockback;
        /// <summary>No se puede bloquear ni parrear: hay que esquivarlo.</summary>
        public bool Unblockable;
        /// <summary>Quien atacó, para avisarle si lo parrearon. Puede ser null.</summary>
        public IParryable Attacker;
    }

    /// <summary>Algo que puede recibir golpes.</summary>
    public interface IDamageable
    {
        HitResult ReceiveHit(in HitInfo hit);
    }

    /// <summary>Un atacante que reacciona cuando le hacen parry.</summary>
    public interface IParryable
    {
        void OnParried();
    }

    public static class CombatUtils
    {
        static readonly List<Collider2D> s_Results = new();

        /// <summary>
        /// Aplica un golpe a todo lo que esté dentro de la caja. <paramref name="alreadyHit"/>
        /// evita golpear dos veces al mismo objetivo durante un mismo ataque.
        /// </summary>
        public static void HitBox(Vector2 center, Vector2 size, LayerMask mask, in HitInfo hit,
            HashSet<IDamageable> alreadyHit, IDamageable self = null)
        {
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(mask);
            s_Results.Clear();
            Physics2D.OverlapBox(center, size, 0f, filter, s_Results);

            foreach (var col in s_Results)
            {
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || ReferenceEquals(target, self) || !alreadyHit.Add(target))
                    continue;
                var result = target.ReceiveHit(hit);
                if (result != HitResult.Ignored)
                    CombatEvents.RaiseHit(target, result, hit);
            }
        }

        public static void DrawBox(Vector2 center, Vector2 size, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
