using System.Collections;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Plataforma frágil (Biblia, Objetos comunes): tiembla al pisarla, cae
    /// 0,5 s después y vuelve a los 3 s.
    /// </summary>
    public class FragilePlatform : MonoBehaviour
    {
        [SerializeField] Collider2D solid;
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Frame 0 = entera; los siguientes, rompiéndose.")]
        [SerializeField] Sprite[] frames;
        [SerializeField] float breakDelay = 0.5f;
        [SerializeField] float respawnTime = 3f;

        bool breaking;
        Vector3 restPosition;

        public void Configure(Collider2D col, SpriteRenderer sr, Sprite[] breakFrames)
        {
            solid = col;
            spriteRenderer = sr;
            frames = breakFrames;
        }

        void Awake() => restPosition = spriteRenderer != null ? spriteRenderer.transform.localPosition : Vector3.zero;

        void OnCollisionStay2D(Collision2D collision)
        {
            if (breaking)
                return;
            var player = collision.collider.GetComponentInParent<PlayerController>();
            // Solo cuando lo pisa desde arriba.
            if (player != null && player.IsGrounded && player.transform.position.y >= solid.bounds.max.y - 0.1f)
                StartCoroutine(Break());
        }

        IEnumerator Break()
        {
            breaking = true;
            for (float t = 0f; t < breakDelay; t += Time.deltaTime)
            {
                if (spriteRenderer != null)   // temblor de 1 px
                    spriteRenderer.transform.localPosition = restPosition +
                        new Vector3(Mathf.Repeat(t, 0.1f) < 0.05f ? 1f / 16f : -1f / 16f, 0f, 0f);
                yield return null;
            }
            if (spriteRenderer != null)
                spriteRenderer.transform.localPosition = restPosition;
            solid.enabled = false;
            for (int i = 1; i < frames.Length; i++)
            {
                spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(0.07f);
            }
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(respawnTime);
            spriteRenderer.sprite = frames[0];
            spriteRenderer.enabled = true;
            solid.enabled = true;
            breaking = false;
        }
    }
}
