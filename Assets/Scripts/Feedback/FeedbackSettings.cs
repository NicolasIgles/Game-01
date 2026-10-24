using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    public enum SoundId
    {
        Swing, Hit, Hurt, Block, Parry, Dash, Jump, Land, HealStart, Heal,
        EnemyWindup, EnemyWindupUnblockable, EnemyDeath, PlayerDeath,
        BossRoar, SpiritLaunch,
    }

    /// <summary>
    /// Sonidos, pausas de impacto y temblores de cámara. Vive en
    /// Assets/Resources/Feedback/FeedbackSettings para que se cargue sin
    /// referencias en las escenas. Se ajusta desde el Inspector, también en Play.
    /// </summary>
    [CreateAssetMenu(menuName = "Game01/Feedback Settings", fileName = "FeedbackSettings")]
    public class FeedbackSettings : ScriptableObject
    {
        public const string ResourcePath = "Feedback/FeedbackSettings";

        [Serializable]
        public class Sound
        {
            public SoundId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.8f;
            [Tooltip("Variación aleatoria de tono para que no suene siempre igual.")]
            [Range(0f, 0.3f)] public float pitchVariance = 0.05f;
        }

        [Header("Sonido")]
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Tooltip("Hasta esta distancia a la cámara, los sonidos del mundo suenan a volumen completo.")]
        public float fullVolumeDistance = 10f;
        [Tooltip("Desde esta distancia ya no se escuchan.")]
        public float silentDistance = 22f;
        public List<Sound> sounds = new();

        [Header("Pausa de impacto (segundos)")]
        [Tooltip("El jugador golpea a un enemigo.")]
        public float hitStopHit = 0.05f;
        [Tooltip("El jugador recibe daño.")]
        public float hitStopHurt = 0.09f;
        public float hitStopBlock = 0.03f;
        public float hitStopParry = 0.12f;
        public float hitStopEnemyDeath = 0.07f;

        [Header("Temblor de cámara (0 a 1)")]
        [Tooltip("Desplazamiento máximo en unidades (0.25 = 4 px).")]
        public float maxShakeOffset = 0.25f;
        [Tooltip("Cuánto temblor se pierde por segundo.")]
        public float shakeDecay = 2.5f;
        public float shakeHit = 0.2f;
        public float shakeHurt = 0.45f;
        public float shakeBlock = 0.12f;
        public float shakeParry = 0.35f;
        public float shakeEnemyDeath = 0.3f;
        public float shakeHardLanding = 0.15f;

        [Header("Aterrizaje")]
        [Tooltip("Velocidad de caída mínima para que suene el aterrizaje.")]
        public float landingSoundSpeed = 4f;
        [Tooltip("Velocidad de caída desde la que el aterrizaje hace temblar la cámara.")]
        public float hardLandingSpeed = 18f;

        public Sound Get(SoundId id)
        {
            foreach (var s in sounds)
                if (s.id == id)
                    return s;
            return null;
        }
    }
}
