using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game01.EditorTools
{
    /// <summary>
    /// Configura el proyecto para pixel art a 480×270 y PPU 16.
    /// Corre sola una vez por máquina al abrir el proyecto y se puede repetir desde
    /// Tools > Pixel Art > Aplicar configuración del proyecto. Es idempotente.
    /// </summary>
    [InitializeOnLoad]
    public static class PixelArtProjectSetup
    {
        const string MenuPath = "Tools/Pixel Art/Aplicar configuración del proyecto";
        const string MainScenePath = "Assets/Scenes/SampleScene.unity";
        static readonly Color32 BackgroundColor = new Color32(0x0A, 0x0A, 0x14, 0xFF); // negro profundo del GDD

        static readonly string[] Folders =
        {
            "Assets/Art/Characters/Player",
            "Assets/Art/Enemies",
            "Assets/Art/Bosses",
            "Assets/Art/NPCs",
            "Assets/Art/Environment/Tiles",
            "Assets/Art/Environment/Props",
            "Assets/Art/Environment/Backgrounds",
            "Assets/Art/VFX",
            "Assets/Art/UI",
            "Assets/Art/Palettes",
            "Assets/Audio/Music",
            "Assets/Audio/SFX",
            "Assets/Data",
            "Assets/Prefabs",
            "Assets/Scripts/Player",
            "Assets/Scripts/Combat",
            "Assets/Scripts/Enemies",
        };

        static string DoneKey => "Game01.PixelArtSetup.v1@" + Application.dataPath;

        static PixelArtProjectSetup()
        {
            if (EditorPrefs.GetBool(DoneKey, false))
                return;
            EditorApplication.delayCall += () =>
            {
                if (Apply())
                    EditorPrefs.SetBool(DoneKey, true);
            };
        }

        [MenuItem(MenuPath)]
        static void ApplyFromMenu()
        {
            if (Apply())
                EditorPrefs.SetBool(DoneKey, true);
        }

        static bool Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[PixelArt] Salí del modo Play y volvé a correr " + MenuPath);
                return false;
            }

            CreateFolders();
            ConfigurePlayer();
            bool cameraOk = ConfigureCamera();
            AssetDatabase.SaveAssets();

            if (cameraOk)
                Debug.Log($"[PixelArt] Proyecto configurado: {PixelArtSettings.ReferenceWidth}×{PixelArtSettings.ReferenceHeight}, PPU {PixelArtSettings.PixelsPerUnit}.");
            return cameraOk;
        }

        static void CreateFolders()
        {
            foreach (var folder in Folders)
            {
                EnsureFolder(folder);
                // Git no guarda carpetas vacías; Unity ignora archivos que empiezan con punto.
                if (Directory.GetFiles(folder).Length == 0)
                    File.WriteAllText(Path.Combine(folder, ".gitkeep"), string.Empty);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.defaultScreenWidth = PixelArtSettings.ReferenceWidth * 4;   // 1920
            PlayerSettings.defaultScreenHeight = PixelArtSettings.ReferenceHeight * 4; // 1080
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        static bool ConfigureCamera()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != MainScenePath && File.Exists(MainScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return false;
                scene = EditorSceneManager.OpenScene(MainScenePath);
            }

            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("[PixelArt] No encontré una cámara en " + scene.path);
                return false;
            }

            Undo.RecordObject(camera, "Pixel Art Camera");
            camera.orthographic = true;
            camera.orthographicSize = PixelArtSettings.ReferenceHeight / (2f * PixelArtSettings.PixelsPerUnit); // 8.4375
            camera.backgroundColor = BackgroundColor;

            var ppc = camera.GetComponent<PixelPerfectCamera>();
            if (ppc == null)
                ppc = Undo.AddComponent<PixelPerfectCamera>(camera.gameObject);
            Undo.RecordObject(ppc, "Pixel Perfect Camera");
            ppc.assetsPPU = PixelArtSettings.PixelsPerUnit;
            ppc.refResolutionX = PixelArtSettings.ReferenceWidth;
            ppc.refResolutionY = PixelArtSettings.ReferenceHeight;
            // Todo se dibuja a 480×270 y después se escala: sprites rotados, partículas
            // y efectos también quedan en la grilla de píxeles.
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            // Pantalla completa sin bordes: ×4 exacto en 1080p, ×8 en 4K; en 1440p
            // escala ×5 con píxeles nítidos y un ajuste suave final.
            ppc.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
            EditorUtility.SetDirty(ppc);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }
    }
}
