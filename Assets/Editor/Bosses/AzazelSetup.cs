using System.IO;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game01.EditorTools
{
    /// <summary>
    /// Crea los prefabs de Azazel y de su espíritu a partir de los .aseprite y el
    /// asset de stats. El constructor del Bosque lo llama solo; los prefabs y stats
    /// existentes no se pisan salvo con el menú "Recrear prefabs de Azazel".
    /// </summary>
    public static class AzazelSetup
    {
        const string Folder = "Assets/Art/Bosses/Azazel/";
        const string BodyArt = Folder + "azazel.aseprite";
        const string WingsArt = Folder + "azazel_wings.aseprite";
        const string SpiritArt = Folder + "azazel_spirit.aseprite";
        const string StatsPath = "Assets/Data/Enemies/AzazelBossStats.asset";
        const string PrefabPath = "Assets/Prefabs/Enemies/Azazel.prefab";
        const string SpiritPrefabPath = "Assets/Prefabs/Enemies/AzazelSpirit.prefab";
        const string NoFrictionPath = "Assets/Data/NoFriction.physicsMaterial2D";

        [MenuItem("Tools/Game01/Recrear prefabs de Azazel")]
        static void RecreateFromMenu()
        {
            AssetDatabase.DeleteAsset(PrefabPath);
            AssetDatabase.DeleteAsset(SpiritPrefabPath);
            var prefab = EnsurePrefabs();
            if (prefab != null)
                Debug.Log("[Azazel] Prefabs recreados. Volvé a construir el Bosque para usarlos.");
        }

        /// <summary>Devuelve el prefab de Azazel, creándolo (y el del espíritu) si falta.</summary>
        public static GameObject EnsurePrefabs()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                return existing;

            var body = AssetDatabase.LoadAssetAtPath<GameObject>(BodyArt);
            var wings = AssetDatabase.LoadAssetAtPath<GameObject>(WingsArt);
            var spiritArt = AssetDatabase.LoadAssetAtPath<GameObject>(SpiritArt);
            if (body == null || wings == null || spiritArt == null)
            {
                Debug.LogError("[Azazel] Faltan los .aseprite en " + Folder);
                return null;
            }
            CenterPivot(SpiritArt);
            spiritArt = AssetDatabase.LoadAssetAtPath<GameObject>(SpiritArt);

            int enemy = LayerMask.NameToLayer("Enemy");
            int ground = LayerMask.NameToLayer("Ground");
            int player = LayerMask.NameToLayer("Player");
            if (enemy < 0 || ground < 0 || player < 0)
            {
                Debug.LogError("[Azazel] Faltan las capas Ground/Player/Enemy (Tools > Game01 > Crear sala de prueba las crea).");
                return null;
            }

            var stats = AssetDatabase.LoadAssetAtPath<AzazelStats>(StatsPath);
            if (stats == null)
            {
                EnsureFolder(Path.GetDirectoryName(StatsPath));
                stats = ScriptableObject.CreateInstance<AzazelStats>();
                AssetDatabase.CreateAsset(stats, StatsPath);
            }

            var spiritPrefab = CreateSpirit(spiritArt, enemy, ground, player);
            return CreateBoss(body, wings, spiritPrefab, stats, enemy, ground, player);
        }

        static SpiritProjectile CreateSpirit(GameObject art, int enemy, int ground, int player)
        {
            var root = new GameObject("AzazelSpirit") { layer = enemy };
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;
            var spirit = root.AddComponent<SpiritProjectile>();

            var sprite = Nested(art, root.transform, "Sprite", 7);
            var so = new SerializedObject(spirit);
            so.FindProperty("animator").objectReferenceValue = sprite.GetComponentInChildren<Animator>(true);
            so.FindProperty("spriteRenderer").objectReferenceValue = sprite.GetComponentInChildren<SpriteRenderer>(true);
            so.FindProperty("hurtbox").objectReferenceValue = col;
            so.FindProperty("playerMask").intValue = 1 << player;
            so.FindProperty("enemyMask").intValue = 1 << enemy;
            so.FindProperty("groundMask").intValue = 1 << ground;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = Save(root, SpiritPrefabPath);
            return prefab.GetComponent<SpiritProjectile>();
        }

        static GameObject CreateBoss(GameObject body, GameObject wings, SpiritProjectile spirit, AzazelStats stats, int enemy, int ground, int player)
        {
            var root = new GameObject("Azazel") { layer = enemy };
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = stats.gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            // ~36×72 px, pivote en los pies.
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.25f, 4.5f);
            col.offset = new Vector2(0f, 2.25f);

            root.AddComponent<Health>();
            var boss = root.AddComponent<AzazelBoss>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var wingsGo = Nested(wings, visual, "Alas (fase 2)", 4);
            wingsGo.SetActive(false);
            var bodyGo = Nested(body, visual, "Cuerpo", 5);

            var so = new SerializedObject(boss);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.FindProperty("bodyCollider").objectReferenceValue = col;
            so.FindProperty("groundMask").intValue = 1 << ground;
            so.FindProperty("playerMask").intValue = 1 << player;
            so.FindProperty("spiritPrefab").objectReferenceValue = spirit;
            so.FindProperty("visualRoot").objectReferenceValue = visual;
            so.FindProperty("animator").objectReferenceValue = bodyGo.GetComponentInChildren<Animator>(true);
            so.FindProperty("bodyRenderer").objectReferenceValue = bodyGo.GetComponentInChildren<SpriteRenderer>(true);
            so.FindProperty("wings").objectReferenceValue = wingsGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            return Save(root, PrefabPath);
        }

        static GameObject Nested(GameObject model, Transform parent, string name, int sortingOrder)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            if (go.TryGetComponent<SortingGroup>(out var group))
                group.sortingOrder = sortingOrder;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingOrder = sortingOrder;
            return go;
        }

        static GameObject Save(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>El espíritu es un proyectil: pivote al centro en vez de en los pies.</summary>
        static void CenterPivot(string path)
        {
            if (AssetImporter.GetAtPath(path) is AsepriteImporter importer && importer.pivotAlignment != SpriteAlignment.Center)
            {
                importer.pivotAlignment = SpriteAlignment.Center;
                importer.SaveAndReimport();
            }
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
