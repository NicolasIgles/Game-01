using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game01
{
    /// <summary>Hace titilar una luz 2D (faroles, brasas, altar).</summary>
    [RequireComponent(typeof(Light2D))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float amount = 0.2f;
        [SerializeField] float speed = 3f;

        Light2D light2D;
        float baseIntensity;
        float seed;

        void Awake()
        {
            light2D = GetComponent<Light2D>();
            seed = Random.value * 100f;
        }

        void OnEnable() => baseIntensity = light2D.intensity;

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
            light2D.intensity = baseIntensity * (1f + n * amount);
        }
    }
}
