using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Mecanismos de "sensación": pausa de impacto, temblor de cámara y sonidos.
    /// Se crea solo al iniciar el juego (no hace falta ponerlo en las escenas) y
    /// sobrevive a los cambios de escena. Uso: GameFeel.HitStop(0.05f),
    /// GameFeel.Shake(0.3f), GameFeel.Play(SoundId.Hit, posición).
    /// </summary>
    public class GameFeel : MonoBehaviour
    {
        const int Voices = 12;

        public static GameFeel Instance { get; private set; }
        /// <summary>Desplazamiento actual de la cámara por temblor (lo suma CameraFollow2D).</summary>
        public static Vector2 ShakeOffset { get; private set; }

        public FeedbackSettings Settings { get; private set; }

        AudioSource[] voices;
        int nextVoice;
        float hitStopRemaining;
        float timeScaleBeforeStop = 1f;
        bool stopped;
        float trauma;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            var go = new GameObject("[GameFeel]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameFeel>();
            go.AddComponent<CombatFeedback>();
        }

        void Awake()
        {
            Instance = this;
            Settings = Resources.Load<FeedbackSettings>(FeedbackSettings.ResourcePath);
            if (Settings == null)
            {
                Debug.LogWarning("[GameFeel] Falta Resources/" + FeedbackSettings.ResourcePath + "; se usan valores por defecto y sin sonido.");
                Settings = ScriptableObject.CreateInstance<FeedbackSettings>();
            }

            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                voices[i] = source;
            }
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;
            if (stopped)
                Time.timeScale = timeScaleBeforeStop;
            ShakeOffset = Vector2.zero;
            Instance = null;
        }

        /// <summary>Congela el juego unos instantes. Si ya hay una pausa, se queda con la más larga.</summary>
        public static void HitStop(float duration)
        {
            var g = Instance;
            if (g == null || duration <= 0f)
                return;
            if (!g.stopped)
            {
                g.timeScaleBeforeStop = Time.timeScale;
                Time.timeScale = 0f;
                g.stopped = true;
            }
            g.hitStopRemaining = Mathf.Max(g.hitStopRemaining, duration);
        }

        /// <summary>Suma temblor (0 a 1). El desplazamiento crece con el cuadrado, así lo leve se nota poco.</summary>
        public static void Shake(float amount)
        {
            var g = Instance;
            if (g != null && amount > 0f)
                g.trauma = Mathf.Clamp01(g.trauma + amount);
        }

        /// <summary>Reproduce un sonido. Con posición, baja el volumen según la distancia a la cámara.</summary>
        public static void Play(SoundId id, Vector2? position = null)
        {
            var g = Instance;
            if (g == null)
                return;
            var sound = g.Settings.Get(id);
            if (sound == null || sound.clip == null)
                return;

            float volume = sound.volume * g.Settings.masterVolume;
            if (position.HasValue && Camera.main != null)
            {
                float distance = Vector2.Distance(position.Value, Camera.main.transform.position);
                volume *= 1f - Mathf.InverseLerp(g.Settings.fullVolumeDistance, g.Settings.silentDistance, distance);
            }
            if (volume <= 0.01f)
                return;

            var source = g.voices[g.nextVoice];
            g.nextVoice = (g.nextVoice + 1) % Voices;
            source.pitch = 1f + Random.Range(-sound.pitchVariance, sound.pitchVariance);
            source.PlayOneShot(sound.clip, volume);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (stopped)
            {
                hitStopRemaining -= dt;
                if (hitStopRemaining <= 0f)
                {
                    stopped = false;
                    Time.timeScale = timeScaleBeforeStop;
                }
            }

            if (trauma > 0f)
            {
                trauma = Mathf.Max(0f, trauma - Settings.shakeDecay * dt);
                float strength = Settings.maxShakeOffset * trauma * trauma;
                float t = Time.unscaledTime * 25f;
                ShakeOffset = new Vector2((Mathf.PerlinNoise(t, 0.3f) * 2f - 1f) * strength,
                                          (Mathf.PerlinNoise(0.7f, t) * 2f - 1f) * strength);
            }
            else
            {
                ShakeOffset = Vector2.zero;
            }
        }
    }
}
