using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game01
{
    /// <summary>
    /// "Después del Rey" (Biblia, Reglas comunes): al caer el jefe cambia la luz
    /// global de la zona, cambian de color algunas partículas y se reemplazan
    /// unos pocos objetos. Muestra que el avance del jugador sana el mundo.
    /// </summary>
    public class AfterKing : MonoBehaviour
    {
        [SerializeField] Light2D globalLight;
        [SerializeField] Color lightColorAfter = new(0.62f, 0.72f, 0.95f);
        [SerializeField] float lightIntensityAfter = 0.9f;
        [SerializeField] List<PixelParticles> recolor = new();
        [SerializeField] Color particleColorAfter = new(0.91f, 0.78f, 0.38f);
        [SerializeField] List<GameObject> disable = new();
        [SerializeField] List<GameObject> enable = new();
        [SerializeField] float duration = 2.5f;

        public void Configure(Light2D light, List<PixelParticles> particlesToRecolor, List<GameObject> toDisable)
        {
            globalLight = light;
            recolor = particlesToRecolor;
            disable = toDisable;
        }

        void Start()
        {
            foreach (var go in enable)
                if (go != null)
                    go.SetActive(false);
        }

        public void Apply() => StartCoroutine(Run());

        IEnumerator Run()
        {
            Color c0 = globalLight != null ? globalLight.color : Color.white;
            float i0 = globalLight != null ? globalLight.intensity : 1f;
            var p0 = new List<Color>();
            foreach (var p in recolor)
                p0.Add(p != null ? p.Color : Color.white);

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                if (globalLight != null)
                {
                    globalLight.color = Color.Lerp(c0, lightColorAfter, t);
                    globalLight.intensity = Mathf.Lerp(i0, lightIntensityAfter, t);
                }
                for (int i = 0; i < recolor.Count; i++)
                    if (recolor[i] != null)
                        recolor[i].Color = Color.Lerp(p0[i], particleColorAfter, t);
                yield return null;
            }
            foreach (var go in disable)
                if (go != null)
                    go.SetActive(false);
            foreach (var go in enable)
                if (go != null)
                    go.SetActive(true);
        }
    }
}
