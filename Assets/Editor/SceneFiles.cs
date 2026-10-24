using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game01.EditorTools
{
    /// <summary>
    /// Utilidades para escenas generadas por herramientas (TestRoom, Bosque).
    /// Regenerar una escena reemplaza su contenido pero conserva su identificador
    /// (GUID): así Git solo ve cambios reales y Build Settings no queda apuntando
    /// a una escena que ya no existe.
    /// </summary>
    public static class SceneFiles
    {
        /// <summary>
        /// Deja en <paramref name="path"/> una copia de <paramref name="template"/>
        /// (conservando el .meta si ya existía) y la abre.
        /// </summary>
        public static UnityEngine.SceneManagement.Scene ResetFromTemplate(string template, string path)
        {
            // Primero se cierra cualquier escena abierta para que Unity no pregunte
            // por una escena modificada desde afuera.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (File.Exists(path))
            {
                File.Copy(template, path, overwrite: true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            else if (!AssetDatabase.CopyAsset(template, path))
            {
                throw new System.InvalidOperationException($"No pude copiar {template} a {path}");
            }
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        /// <summary>Agrega la escena a Build Settings o corrige su GUID si cambió.</summary>
        public static void EnsureInBuildSettings(string path)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(s => s.path == path);
            if (index >= 0 && scenes[index].guid.ToString() == guid)
                return;
            // El constructor con ruta toma el GUID actual del archivo.
            var entry = new EditorBuildSettingsScene(path, index < 0 || scenes[index].enabled);
            if (index >= 0)
                scenes[index] = entry;
            else
                scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
