using UnityEngine;

namespace Game01
{
    /// <summary>HUD provisorio para probar el combate. Se reemplaza por la UI final.</summary>
    public class PlayerDebugHUD : MonoBehaviour
    {
        [SerializeField] bool showControls = true;

        PlayerController player;
        Health health;
        GUIStyle style;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            health = GetComponent<Health>();
        }

        void OnGUI()
        {
            if (player == null || health == null)
                return;
            style ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 45) };

            GUILayout.BeginArea(new Rect(16, 16, Screen.width - 32, Screen.height / 2f));
            GUILayout.Label($"Vida {health.Current}/{health.Max}    Curas {player.HealCharges}/{player.Stats.healCharges}    Estado {player.Current}", style);
            if (showControls)
            {
                GUILayout.Label("Teclado: A/D mover · Espacio saltar · J atacar · K o Shift dash · L bloquear (tocar justo = parry) · F curar", style);
                GUILayout.Label("Gamepad: stick · A saltar · X atacar · RT dash · LT bloquear · LB curar", style);
            }
            GUILayout.EndArea();
        }
    }
}
