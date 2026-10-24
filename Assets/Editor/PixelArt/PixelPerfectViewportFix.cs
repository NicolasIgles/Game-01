using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game01.EditorTools
{
    /// <summary>
    /// En el Editor, la Pixel Perfect Camera ajusta el viewport de la cámara según
    /// el tamaño de la ventana Game, y ese valor terminaba guardado en la escena
    /// (cambios falsos en Git). Antes de guardar, se deja en pantalla completa;
    /// en el juego la cámara lo recalcula sola.
    /// </summary>
    [InitializeOnLoad]
    static class PixelPerfectViewportFix
    {
        static readonly Rect Full = new(0f, 0f, 1f, 1f);

        static PixelPerfectViewportFix() => EditorSceneManager.sceneSaving += OnSceneSaving;

        static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var ppc in root.GetComponentsInChildren<PixelPerfectCamera>(true))
                {
                    if (ppc.TryGetComponent<Camera>(out var cam) && cam.rect != Full)
                        cam.rect = Full;
                }
            }
        }
    }
}
