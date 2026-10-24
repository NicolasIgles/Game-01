using UnityEngine;

namespace Game01
{
    /// <summary>Chispa de golpe (Biblia, Efectos comunes) cada vez que este objeto pierde vida.</summary>
    [RequireComponent(typeof(Health))]
    public class DamageSpark : MonoBehaviour
    {
        [SerializeField] float height = 1f;

        Health health;
        int last;

        void Awake()
        {
            health = GetComponent<Health>();
            last = int.MaxValue;
        }

        void OnEnable() => health.Changed += OnChanged;
        void OnDisable() => health.Changed -= OnChanged;

        void OnChanged(int current, int max)
        {
            // Al aparecer o reaparecer la vida vuelve al máximo: eso no es un golpe.
            if (current < last && current < max)
                LevelFX.Play(LevelFX.Kind.Spark, (Vector2)transform.position + Vector2.up * height);
            last = current;
        }
    }
}
