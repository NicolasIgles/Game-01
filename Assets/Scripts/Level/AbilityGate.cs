using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Obstáculo de habilidad (Biblia, Habilidades por fragmento). Con menos
    /// fragmentos de los necesarios muestra un aviso al acercarse; al alcanzarlos:
    /// - Barrier: se desvanece y deja pasar (velo de espíritus).
    /// - Reveal: hace aparecer objetos (salientes de luz en la pared lisa, provisorios
    ///   hasta que exista el salto de pared).
    /// </summary>
    public class AbilityGate : MonoBehaviour
    {
        public enum Mode { Barrier, Reveal }

        [SerializeField] Mode mode = Mode.Barrier;
        [SerializeField, Range(1, PlayerProgress.TotalFragments)] int requiredFragments = 1;
        [SerializeField] Collider2D barrier;
        [SerializeField] List<GameObject> reveal = new();
        [SerializeField, TextArea] string lockedHint;
        [SerializeField, TextArea] string openedBanner;

        bool open;

        public void Configure(Mode m, int required, Collider2D barrierCollider, List<GameObject> targets, string hint, string banner)
        {
            mode = m;
            requiredFragments = required;
            barrier = barrierCollider;
            reveal = targets;
            lockedHint = hint;
            openedBanner = banner;
        }

        void Start()
        {
            foreach (var go in reveal)
                if (go != null)
                    go.SetActive(false);
            PlayerProgress.FragmentsChanged += OnFragments;
            if (PlayerProgress.Fragments >= requiredFragments)
                Open(instant: true);
        }

        void OnDestroy() => PlayerProgress.FragmentsChanged -= OnFragments;

        void OnFragments(int count)
        {
            if (!open && count >= requiredFragments)
                Open(instant: false);
        }

        void Open(bool instant)
        {
            open = true;
            LevelMessages.HideNear(this);
            if (!instant && !string.IsNullOrEmpty(openedBanner))
                LevelMessages.Banner(openedBanner, 4f);
            StartCoroutine(OpenRoutine(instant));
        }

        IEnumerator OpenRoutine(bool instant)
        {
            if (mode == Mode.Barrier)
            {
                if (barrier != null)
                    barrier.enabled = false;
                var renderers = GetComponentsInChildren<SpriteRenderer>();
                for (float t = 0f; t < 1f && !instant; t += Time.deltaTime)
                {
                    foreach (var sr in renderers)
                    {
                        var c = sr.color;
                        c.a = 1f - t;
                        sr.color = c;
                    }
                    yield return null;
                }
                foreach (var sr in renderers)
                    sr.enabled = false;
            }
            else
            {
                foreach (var go in reveal)
                {
                    if (go == null)
                        continue;
                    go.SetActive(true);
                    if (!instant)
                    {
                        LevelFX.Play(LevelFX.Kind.Parry, go.transform.position);
                        yield return new WaitForSeconds(0.12f);
                    }
                }
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!open && other.GetComponentInParent<PlayerController>())
                LevelMessages.ShowNear(this, $"{lockedHint}\n({PlayerProgress.Fragments}/{requiredFragments} fragmentos de gracia)");
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>())
                LevelMessages.HideNear(this);
        }
    }
}
