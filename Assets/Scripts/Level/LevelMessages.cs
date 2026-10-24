using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Textos provisorios del nivel: un cartel arriba (nombre de jefe, fragmento
    /// recuperado) y una caja abajo para lo que se lee de cerca (inscripciones,
    /// Sarah). Se reemplaza por la UI final.
    /// </summary>
    public class LevelMessages : MonoBehaviour
    {
        static LevelMessages instance;

        string banner;
        float bannerUntil;
        string nearText;
        Object nearOwner;
        GUIStyle bannerStyle, boxStyle;

        void Awake() => instance = this;

        public static void Banner(string text, float seconds = 3f)
        {
            if (instance == null)
                return;
            instance.banner = text;
            instance.bannerUntil = Time.time + seconds;
        }

        public static void ShowNear(Object owner, string text)
        {
            if (instance == null)
                return;
            instance.nearOwner = owner;
            instance.nearText = text;
        }

        public static void HideNear(Object owner)
        {
            if (instance != null && instance.nearOwner == owner)
            {
                instance.nearOwner = null;
                instance.nearText = null;
            }
        }

        void OnGUI()
        {
            int size = Mathf.Max(14, Screen.height / 42);
            bannerStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.91f, 0.78f, 0.38f) },
            };
            boxStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter, wordWrap = true,
                normal = { textColor = new Color(0.94f, 0.94f, 0.97f) },
            };
            bannerStyle.fontSize = Mathf.RoundToInt(size * 1.4f);
            boxStyle.fontSize = size;
            boxStyle.padding = new RectOffset(size, size, size / 2, size / 2);

            if (!string.IsNullOrEmpty(banner) && Time.time < bannerUntil)
                GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, size * 3), banner, bannerStyle);

            if (!string.IsNullOrEmpty(nearText))
            {
                float w = Mathf.Min(Screen.width * 0.7f, size * 40);
                float h = boxStyle.CalcHeight(new GUIContent(nearText), w);
                GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f - h, w, h), nearText, boxStyle);
            }
        }
    }
}
