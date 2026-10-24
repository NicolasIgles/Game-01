using UnityEngine;

namespace Game01
{
    /// <summary>Fragmento de gracia: suma uno al tocarlo y anuncia la habilidad nueva.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GraceFragment : MonoBehaviour
    {
        static readonly string[] AbilityText =
        {
            "Tu mano derecha recuerda la luz: salto de pared",
            "Las micro-alas empujan: paso de gracia",
            "Alas pequeñas, pero reales: doble salto",
            "Las piernas se arman: pisón de luz",
            "Alas grandes y el halo restaurado: planeo",
            "El halo ilumina lo invisible: senda de luz",
            "Gracia completa: la elección",
        };

        bool taken;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || !other.GetComponentInParent<PlayerController>())
                return;
            taken = true;
            PlayerProgress.AddFragment();
            int n = PlayerProgress.Fragments;
            LevelFX.Play(LevelFX.Kind.Parry, (Vector2)transform.position + Vector2.up * 0.5f);
            LevelMessages.Banner($"Fragmento de gracia {n}/{PlayerProgress.TotalFragments}\n{AbilityText[Mathf.Clamp(n - 1, 0, AbilityText.Length - 1)]}", 5f);
            gameObject.SetActive(false);
        }
    }
}
