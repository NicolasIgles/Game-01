using System;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Fragmentos de gracia recuperados. La habilidad de movimiento depende de
    /// cuántos hay, no de qué Rey los tenía (ver Biblia de niveles).
    /// Provisorio: todavía no se guarda en disco.
    /// </summary>
    public static class PlayerProgress
    {
        public const int TotalFragments = 7;

        public static int Fragments { get; private set; }

        /// <summary>Se dispara con la cantidad nueva de fragmentos.</summary>
        public static event Action<int> FragmentsChanged;

        public static void AddFragment()
        {
            Fragments = Mathf.Min(TotalFragments, Fragments + 1);
            FragmentsChanged?.Invoke(Fragments);
        }

        // Sin recarga de dominio al entrar en Play, los estáticos sobreviven: se limpian acá.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Fragments = 0;
            FragmentsChanged = null;
        }
    }
}
