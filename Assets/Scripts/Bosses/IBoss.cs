namespace Game01
{
    /// <summary>Jefe que puede pelear dentro de una <see cref="BossArena"/>.</summary>
    public interface IBoss : IDamageable
    {
        /// <summary>Vuelve a su lugar con la vida llena y empieza a pelear.</summary>
        void BeginFight();
        /// <summary>Vuelve al estado inicial sin pelear (por ejemplo, si el jugador muere).</summary>
        void ResetBoss();
        /// <summary>Cuánto dura su animación de muerte, para esperar antes de abrir la arena.</summary>
        float DeathDuration { get; }
    }
}
