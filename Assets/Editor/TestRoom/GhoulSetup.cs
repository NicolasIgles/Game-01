using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game01.EditorTools
{
    /// <summary>
    /// Crea el prefab del necrófago (MeleeEnemy + sprite de ghoul.aseprite), su
    /// asset de stats y lo coloca en TestRoom. El prefab y los stats existentes
    /// no se pisan; en la escena se reemplazan los necrófagos anteriores.
    /// </summary>
    public static class GhoulSetup
    {
        const string ScenePath = "Assets/Scenes/TestRoom.unity";
        const string SpritePath = "Assets/Art/Enemies/Ghoul/ghoul.aseprite";
        const string StatsPath = "Assets/Data/Enemies/GhoulStats.asset";
        const string PrefabPath = "Assets/Prefabs/Enemies/Ghoul.prefab";
        const string NoFrictionPath = "Assets/Data/NoFriction.physicsMaterial2D";
        const string DummyToReplace = "TrainingDummy (al otro lado del hueco)";

        static readonly Vector3[] Positions =
        {
            new(12f, 0f, 0f),   // en el piso, después del muñeco
            new(-2f, 4f, 0f),   // sobre Platform_C: prueba que no se caiga
            new(25f, 0f, 0f),   // del otro lado del hueco
        };

        [MenuItem("Tools/Game01/Agregar necrófagos a la sala")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Ghoul] Salí del modo Play primero.");
                return;
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SpritePath);
            if (model == null)
            {
                Debug.LogError("[Ghoul] No encontré " + SpritePath);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var stats = AssetDatabase.LoadAssetAtPath<EnemyStats>(StatsPath);
            if (stats == null)
            {
                EnsureFolder(Path.GetDirectoryName(StatsPath));
                stats = ScriptableObject.CreateInstance<EnemyStats>();
                AssetDatabase.CreateAsset(stats, StatsPath);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ?? CreatePrefab(model, stats);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == DummyToReplace || root.GetComponent<MeleeEnemy>() != null)
                    Object.DestroyImmediate(root);
            }
            for (int i = 0; i < Positions.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = $"Ghoul {i + 1}";
                go.transform.position = Positions[i];
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Ghoul] {Positions.Length} necrófagos en {ScenePath}.");
        }

        static GameObject CreatePrefab(GameObject model, EnemyStats stats)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int groundLayer = LayerMask.NameToLayer("Ground");
            int playerLayer = LayerMask.NameToLayer("Player");
            if (enemyLayer < 0 || groundLayer < 0 || playerLayer < 0)
                throw new System.InvalidOperationException("Faltan las capas; corré antes Tools > Game01 > Crear sala de prueba.");

            var root = new GameObject("Ghoul") { layer = enemyLayer };

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = stats.gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            // 14×28 px, pivote en los pies.
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.875f, 1.75f);
            col.offset = new Vector2(0f, 0.875f);

            root.AddComponent<Health>();
            var enemy = root.AddComponent<MeleeEnemy>();
            var enemyAnimator = root.AddComponent<EnemyAnimator>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var sprite = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
            sprite.name = "Sprite";
            sprite.transform.localPosition = Vector3.zero;
            if (sprite.TryGetComponent<SortingGroup>(out var group))
                group.sortingOrder = 5;
            foreach (var sr in sprite.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingOrder = 5;

            var so = new SerializedObject(enemy);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.FindProperty("bodyCollider").objectReferenceValue = col;
            so.FindProperty("groundMask").intValue = 1 << groundLayer;
            so.FindProperty("playerMask").intValue = 1 << playerLayer;
            so.FindProperty("visualRoot").objectReferenceValue = visual;
            so.FindProperty("bodyRenderer").objectReferenceValue = sprite.GetComponentInChildren<SpriteRenderer>(true);
            so.ApplyModifiedPropertiesWithoutUndo();

            var aso = new SerializedObject(enemyAnimator);
            aso.FindProperty("animator").objectReferenceValue = sprite.GetComponentInChildren<Animator>(true);
            aso.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(Path.GetDirectoryName(PrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

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
