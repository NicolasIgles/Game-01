using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Capa de fondo o primer plano con parallax. Con factor 0 la capa queda fija
    /// en pantalla (cielo); con 1 se mueve con el mundo; entre 0 y 1 queda atrás
    /// (más lejos cuanto más chico); mayor que 1 pasa por delante del jugador.
    /// Puede desvanecerse fuera de un tramo del nivel, para cambiar de fondo entre zonas.
    /// </summary>
    [DefaultExecutionOrder(100)] // después de la cámara
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Velocidad aparente en X respecto del mundo (Biblia: lejano 0,2 · medio 0,5 · primer plano 1,2).")]
        [SerializeField] float factorX = 0.5f;
        [SerializeField] float factorY = 0.5f;
        [Tooltip("Dónde está la capa cuando la cámara está en Reference Camera.")]
        [SerializeField] Vector2 basePosition;
        [SerializeField] Vector2 referenceCamera;

        [Header("Tramo visible (cámara en X)")]
        [SerializeField] bool limitRange;
        [SerializeField] float visibleFromX;
        [SerializeField] float visibleToX;
        [SerializeField] float fadeWidth = 6f;

        Transform cam;
        SpriteRenderer[] renderers;
        float[] baseAlpha;
        float lastAlpha = -1f;

        public void Configure(float fx, float fy, Vector2 position, Vector2 cameraReference)
        {
            factorX = fx;
            factorY = fy;
            basePosition = position;
            referenceCamera = cameraReference;
        }

        public void LimitTo(float fromX, float toX, float fade)
        {
            limitRange = true;
            visibleFromX = fromX;
            visibleToX = toX;
            fadeWidth = fade;
        }

        void Start()
        {
            if (Camera.main != null)
                cam = Camera.main.transform;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseAlpha[i] = renderers[i].color.a;
        }

        void LateUpdate()
        {
            if (cam == null)
                return;
            Vector2 c = cam.position;
            var p = new Vector2(
                basePosition.x + (c.x - referenceCamera.x) * (1f - factorX),
                basePosition.y + (c.y - referenceCamera.y) * (1f - factorY));
            // Redondeo a la grilla de píxeles para que la capa no tiemble.
            const float px = 1f / PixelArtSettings.PixelsPerUnit;
            transform.position = new Vector3(Mathf.Round(p.x / px) * px, Mathf.Round(p.y / px) * px, transform.position.z);

            if (!limitRange)
                return;
            float a = Mathf.Clamp01(Mathf.Min(c.x - visibleFromX, visibleToX - c.x) / Mathf.Max(0.01f, fadeWidth) + 0.5f);
            if (Mathf.Approximately(a, lastAlpha))
                return;
            lastAlpha = a;
            for (int i = 0; i < renderers.Length; i++)
            {
                var col = renderers[i].color;
                col.a = baseAlpha[i] * a;
                renderers[i].color = col;
                renderers[i].enabled = a > 0.001f;
            }
        }
    }
}
