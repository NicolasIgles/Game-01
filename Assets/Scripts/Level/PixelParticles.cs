using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Partículas ambientales en pixel art: ceniza, brasas, fuegos fatuos, humo,
    /// niebla. Cada partícula es un SpriteRenderer con la posición redondeada a la
    /// grilla de píxeles. Solo simula cuando su área está cerca de la cámara.
    /// </summary>
    public class PixelParticles : MonoBehaviour
    {
        public enum Shape { Area, Point }

        [SerializeField] Sprite[] sprites;
        [Tooltip("Si está activo, cada partícula recorre los sprites en orden durante su vida.")]
        [SerializeField] bool animateOverLife;
        [SerializeField] Shape shape = Shape.Area;
        [Tooltip("Área de aparición en coordenadas del mundo (Shape = Area).")]
        [SerializeField] Rect area = new(0f, 0f, 30f, 17f);
        [SerializeField] float ratePerSecond = 6f;
        [SerializeField] int maxParticles = 60;
        [SerializeField] Vector2 lifetime = new(3f, 6f);
        [SerializeField] Vector2 velocityMin = new(-0.3f, -0.8f);
        [SerializeField] Vector2 velocityMax = new(0.3f, -0.4f);
        [Tooltip("Vaivén horizontal (amplitud en unidades, frecuencia en Hz).")]
        [SerializeField] float swayAmplitude = 0.3f;
        [SerializeField] float swayFrequency = 0.5f;
        [SerializeField] Color color = Color.white;
        [Tooltip("Fracción de la vida que tarda en aparecer y en desaparecer.")]
        [SerializeField, Range(0f, 0.5f)] float fade = 0.2f;
        [Tooltip("Las que escapan de la cámara se apartan: los fuegos fatuos huyen del jugador.")]
        [SerializeField] float fleeFromPlayer;
        [SerializeField] int sortingOrder = 20;
        [SerializeField] Material material;

        struct Particle
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float age, life, phase;
            public bool alive;
        }

        readonly List<Particle> particles = new();
        float spawnAccumulator;
        Transform cam, player;

        public Color Color
        {
            get => color;
            set => color = value;
        }

        public void Configure(Sprite[] spriteSet, Shape s, Rect spawnArea, float rate, int max, Vector2 life,
            Vector2 vMin, Vector2 vMax, float sway, float swayHz, Color tint, float fadePortion, int order,
            Material mat, bool animate = false, float flee = 0f)
        {
            sprites = spriteSet;
            shape = s;
            area = spawnArea;
            ratePerSecond = rate;
            maxParticles = max;
            lifetime = life;
            velocityMin = vMin;
            velocityMax = vMax;
            swayAmplitude = sway;
            swayFrequency = swayHz;
            color = tint;
            fade = fadePortion;
            sortingOrder = order;
            material = mat;
            animateOverLife = animate;
            fleeFromPlayer = flee;
        }

        void Start()
        {
            if (Camera.main != null)
                cam = Camera.main.transform;
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                player = pc.transform;
            if (shape == Shape.Area)   // arranca con el área ya poblada
                for (int i = 0; i < maxParticles / 2; i++)
                    Spawn(Random.value);
        }

        Rect WorldArea => shape == Shape.Area ? area : new Rect((Vector2)transform.position - area.size * 0.5f, area.size);

        bool NearCamera()
        {
            if (cam == null)
                return true;
            var view = new Rect((Vector2)cam.position - new Vector2(20f, 12f), new Vector2(40f, 24f));
            return view.Overlaps(WorldArea);
        }

        void Update()
        {
            if (sprites == null || sprites.Length == 0 || !NearCamera())
                return;
            float dt = Time.deltaTime;
            spawnAccumulator += ratePerSecond * dt;
            while (spawnAccumulator >= 1f)
            {
                spawnAccumulator -= 1f;
                Spawn(0f);
            }

            const float px = 1f / PixelArtSettings.PixelsPerUnit;
            for (int i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                if (!p.alive)
                    continue;
                p.age += dt;
                if (p.age >= p.life)
                {
                    p.alive = false;
                    p.sr.enabled = false;
                    particles[i] = p;
                    continue;
                }
                if (fleeFromPlayer > 0f && player != null)
                {
                    Vector2 away = p.pos - ((Vector2)player.position + Vector2.up);
                    float d = away.magnitude;
                    if (d < 3f && d > 0.01f)
                        p.vel += away / d * (fleeFromPlayer * (3f - d) * dt);
                    p.vel = Vector2.ClampMagnitude(p.vel, 3f);
                }
                p.pos += p.vel * dt;
                float t = p.age / p.life;
                float sway = Mathf.Sin((p.age * swayFrequency + p.phase) * Mathf.PI * 2f) * swayAmplitude;
                var draw = p.pos + new Vector2(sway, 0f);
                p.sr.transform.position = new Vector3(Mathf.Round(draw.x / px) * px, Mathf.Round(draw.y / px) * px, 0f);

                float a = fade <= 0f ? 1f : Mathf.Min(1f, t / fade, (1f - t) / fade);
                var c = color;
                c.a *= a;
                p.sr.color = c;
                if (animateOverLife && sprites.Length > 1)
                    p.sr.sprite = sprites[Mathf.Min(sprites.Length - 1, (int)(t * sprites.Length))];
                particles[i] = p;
            }
        }

        void Spawn(float preAged)
        {
            int slot = -1;
            for (int i = 0; i < particles.Count; i++)
                if (!particles[i].alive) { slot = i; break; }
            if (slot < 0)
            {
                if (particles.Count >= maxParticles)
                    return;
                var go = new GameObject("p");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = sortingOrder;
                if (material != null)
                    sr.sharedMaterial = material;
                particles.Add(new Particle { sr = sr });
                slot = particles.Count - 1;
            }

            var r = WorldArea;
            var p = particles[slot];
            p.pos = shape == Shape.Area
                ? new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax))
                : (Vector2)transform.position + new Vector2(Random.Range(-0.3f, 0.3f), 0f);
            p.vel = new Vector2(Random.Range(velocityMin.x, velocityMax.x), Random.Range(velocityMin.y, velocityMax.y));
            p.life = Random.Range(lifetime.x, lifetime.y);
            p.age = p.life * preAged;
            p.phase = Random.value;
            p.alive = true;
            p.sr.sprite = sprites[Random.Range(0, sprites.Length)];
            if (animateOverLife)
                p.sr.sprite = sprites[0];
            p.sr.enabled = true;
            particles[slot] = p;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            var r = WorldArea;
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
