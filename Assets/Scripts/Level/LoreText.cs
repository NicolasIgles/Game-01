using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Texto que aparece al acercarse: inscripciones, Sarah, el relicario, el
    /// final del slice. Opcionalmente cambia de sprite la primera vez (relicario abierto).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LoreText : MonoBehaviour
    {
        [SerializeField, TextArea] string text;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite spriteAfterFirstVisit;
        [Tooltip("Además del texto de cerca, muestra un cartel la primera vez.")]
        [SerializeField] string bannerOnFirstVisit;

        bool visited;

        public void Configure(string body, SpriteRenderer sr = null, Sprite after = null, string banner = null)
        {
            text = body;
            spriteRenderer = sr;
            spriteAfterFirstVisit = after;
            bannerOnFirstVisit = banner;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.GetComponentInParent<PlayerController>())
                return;
            LevelMessages.ShowNear(this, text);
            if (visited)
                return;
            visited = true;
            if (spriteRenderer != null && spriteAfterFirstVisit != null)
                spriteRenderer.sprite = spriteAfterFirstVisit;
            if (!string.IsNullOrEmpty(bannerOnFirstVisit))
                LevelMessages.Banner(bannerOnFirstVisit, 4f);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>())
                LevelMessages.HideNear(this);
        }
    }
}
