using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Game01.EditorTools
{
    /// <summary>
    /// Mantiene Assets/Resources/Feedback/FeedbackSettings: lo crea si falta y le
    /// asigna a cada <see cref="SoundId"/> el clip Assets/Audio/SFX/&lt;id_en_snake_case&gt;.wav
    /// (por ejemplo EnemyWindup → enemy_windup.wav). Corre al abrir el proyecto y
    /// cada vez que se importa un sonido en esa carpeta. No pisa clips asignados a mano.
    /// </summary>
    public class FeedbackSetup : AssetPostprocessor
    {
        const string AssetPath = "Assets/Resources/Feedback/FeedbackSettings.asset";
        const string SfxFolder = "Assets/Audio/SFX/";

        [InitializeOnLoadMethod]
        static void OnLoad() => EditorApplication.delayCall += () => Sync(false);

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Concat(moved).Any(p => p.StartsWith(SfxFolder)))
                EditorApplication.delayCall += () => Sync(false);
        }

        [MenuItem("Tools/Game01/Reasignar sonidos")]
        static void SyncFromMenu() => Sync(true);

        static void Sync(bool log)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var settings = AssetDatabase.LoadAssetAtPath<FeedbackSettings>(AssetPath);
            if (settings == null)
            {
                EnsureFolder(Path.GetDirectoryName(AssetPath));
                settings = ScriptableObject.CreateInstance<FeedbackSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
            }

            int assigned = 0, missing = 0;
            foreach (SoundId id in System.Enum.GetValues(typeof(SoundId)))
            {
                var entry = settings.Get(id);
                if (entry == null)
                {
                    entry = new FeedbackSettings.Sound { id = id, volume = DefaultVolume(id) };
                    settings.sounds.Add(entry);
                }
                if (entry.clip != null)
                    continue;
                entry.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + SnakeCase(id) + ".wav");
                if (entry.clip != null) assigned++; else missing++;
            }

            if (assigned > 0 || EditorUtility.IsDirty(settings))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
            if (log || assigned > 0)
                Debug.Log($"[Feedback] Sonidos asignados: {assigned}. Sin archivo: {missing}.");
        }

        static float DefaultVolume(SoundId id) => id switch
        {
            SoundId.Jump => 0.45f,
            SoundId.Land => 0.6f,
            SoundId.Swing or SoundId.Dash => 0.6f,
            SoundId.HealStart => 0.6f,
            _ => 0.8f,
        };

        static string SnakeCase(SoundId id) => Regex.Replace(id.ToString(), "(?<=[a-z])([A-Z])", "_$1").ToLowerInvariant();

        static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
