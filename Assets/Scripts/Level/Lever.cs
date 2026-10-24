using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Palanca que se activa con un golpe de espada y hace aparecer objetos
    /// (en el Bosque: la escalera de raíces del Barranco, que funciona de atajo).
    /// Va en la capa Enemy para que el ataque del jugador la detecte.
    /// </summary>
    public class Lever : MonoBehaviour, IDamageable
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite pulledSprite;
        [SerializeField] List<GameObject> reveal = new();
        [SerializeField] float revealInterval = 0.08f;
        [SerializeField, TextArea] string message = "Algo se movió en el barranco.";

        bool pulled;

        public void Configure(SpriteRenderer sr, Sprite pulled, List<GameObject> targets)
        {
            spriteRenderer = sr;
            pulledSprite = pulled;
            reveal = targets;
        }

        void Start()
        {
            foreach (var go in reveal)
                if (go != null)
                    go.SetActive(false);
        }

        public HitResult ReceiveHit(in HitInfo hit)
        {
            if (pulled)
                return HitResult.Ignored;
            pulled = true;
            if (spriteRenderer != null && pulledSprite != null)
                spriteRenderer.sprite = pulledSprite;
            LevelFX.Play(LevelFX.Kind.Spark, (Vector2)transform.position + Vector2.up);
            LevelMessages.Banner(message, 3f);
            StartCoroutine(Reveal());
            return HitResult.Hit;
        }

        IEnumerator Reveal()
        {
            // De abajo hacia arriba, para que se vea el camino formándose.
            reveal.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
            foreach (var go in reveal)
            {
                if (go == null)
                    continue;
                go.SetActive(true);
                LevelFX.Play(LevelFX.Kind.Dust, go.transform.position);
                yield return new WaitForSeconds(revealInterval);
            }
        }
    }
}
