using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game01.EditorTools
{
    /// <summary>
    /// Arma la sala de prueba del combate con placeholders: capas de física,
    /// sprite blanco de 16×16, PlayerStats, prefabs del jugador y del muñeco,
    /// y la escena TestRoom. Los prefabs y assets existentes no se pisan.
    /// </summary>
    public static class TestRoomBuilder
    {
        const string ScenePath = "Assets/Scenes/TestRoom.unity";
        const string BaseScenePath = "Assets/Scenes/SampleScene.unity";
        const string SpritePath = "Assets/Art/Placeholders/placeholder_white.png";
        const string StatsPath = "Assets/Data/PlayerStats.asset";
        const string NoFrictionPath = "Assets/Data/NoFriction.physicsMaterial2D";
        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        const string DummyPrefabPath = "Assets/Prefabs/TrainingDummy.prefab";

        static readonly Color StoneColor = new(0.48f, 0.48f, 0.5f);

        [MenuItem("Tools/Game01/Crear sala de prueba")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[TestRoom] Salí del modo Play primero.");
                return;
            }
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Sala de prueba", "TestRoom ya existe. ¿Volver a crearla? Los prefabs no se modifican.", "Recrear", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            int groundLayer = EnsureLayer("Ground");
            int playerLayer = EnsureLayer("Player");
            int enemyLayer = EnsureLayer("Enemy");
            // El jugador atraviesa a los enemigos; el daño va por hitboxes.
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

            var sprite = EnsurePlaceholderSprite();
            var stats = EnsureAsset<PlayerStats>(StatsPath);
            var noFriction = EnsureNoFriction();

            var playerPrefab = File.Exists(PlayerPrefabPath)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath)
                : CreatePlayerPrefab(sprite, stats, noFriction, playerLayer, groundLayer, enemyLayer);
            var dummyPrefab = File.Exists(DummyPrefabPath)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefabPath)
                : CreateDummyPrefab(sprite, enemyLayer, playerLayer);

            BuildScene(sprite, groundLayer, playerPrefab, dummyPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[TestRoom] Sala de prueba lista: " + ScenePath);
        }

        // ------------------------------------------------------------ escena

        static void BuildScene(Sprite sprite, int groundLayer, GameObject playerPrefab, GameObject dummyPrefab)
        {
            var scene = SceneFiles.ResetFromTemplate(BaseScenePath, ScenePath);

            var level = new GameObject("Level").transform;
            // Piso con un hueco para probar salto y dash.
            Block("Floor_Left", new Vector2(-8f, -1f), new Vector2(48f, 2f));
            Block("Floor_Right", new Vector2(25f, -1f), new Vector2(10f, 2f));
            Block("Wall_Left", new Vector2(-32.5f, 9f), new Vector2(1f, 22f));
            Block("Wall_Right", new Vector2(30.5f, 9f), new Vector2(1f, 22f));
            Block("Platform_A", new Vector2(-14f, 3f), new Vector2(4f, 1f));
            Block("Platform_B", new Vector2(-8f, 5.5f), new Vector2(3f, 1f));
            Block("Platform_C", new Vector2(-2f, 3.5f), new Vector2(5f, 1f));
            Block("Ledge_High", new Vector2(24f, 7f), new Vector2(5f, 1f));

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(-20f, 0f, 0f);

            var dummyA = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab);
            dummyA.name = "TrainingDummy";
            dummyA.transform.position = new Vector3(4f, 0f, 0f);
            var dummyB = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab);
            dummyB.name = "TrainingDummy (al otro lado del hueco)";
            dummyB.transform.position = new Vector3(26f, 0f, 0f);

            var camera = Camera.main;
            if (camera != null)
            {
                if (!camera.TryGetComponent<CameraFollow2D>(out var follow))
                    follow = camera.gameObject.AddComponent<CameraFollow2D>();
                var so = new SerializedObject(follow);
                so.FindProperty("target").objectReferenceValue = player.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                camera.transform.position = new Vector3(-20f, 2f, -10f);
            }

            EditorSceneManager.SaveScene(scene);
            SceneFiles.EnsureInBuildSettings(ScenePath);

            void Block(string name, Vector2 center, Vector2 size)
            {
                var go = new GameObject(name) { layer = groundLayer };
                go.transform.SetParent(level);
                go.transform.position = center;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = size;
                sr.color = StoneColor;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = size;
            }
        }

        // ------------------------------------------------------------ prefabs

        static GameObject CreatePlayerPrefab(Sprite sprite, PlayerStats stats, PhysicsMaterial2D noFriction,
            int playerLayer, int groundLayer, int enemyLayer)
        {
            var root = new GameObject("Player") { layer = playerLayer, tag = "Player" };

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sharedMaterial = noFriction;

            // Cuerpo de 16×44 px con el pivote en los pies.
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 2.75f);
            col.offset = new Vector2(0f, 1.375f);

            root.AddComponent<Health>();
            root.AddComponent<PlayerInputReader>();
            var controller = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerDebugHUD>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var body = Quad("Body", visual, sprite, new Vector2(0f, 1.375f), new Vector2(1f, 2.75f), Color.white, 10);
            Quad("Eye", visual, sprite, new Vector2(0.25f, 2.3f), new Vector2(0.25f, 0.25f), new Color(0.8f, 0.09f, 0.19f), 11);
            Quad("Sword", visual, sprite, new Vector2(0.7f, 1.3f), new Vector2(0.75f, 0.125f), new Color(0.91f, 0.91f, 0.94f), 11);

            var so = new SerializedObject(controller);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.FindProperty("bodyCollider").objectReferenceValue = col;
            so.FindProperty("groundMask").intValue = 1 << groundLayer;
            so.FindProperty("enemyMask").intValue = 1 << enemyLayer;
            so.FindProperty("visualRoot").objectReferenceValue = visual;
            so.FindProperty("bodyRenderer").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, PlayerPrefabPath);
        }

        static GameObject CreateDummyPrefab(Sprite sprite, int enemyLayer, int playerLayer)
        {
            var root = new GameObject("TrainingDummy") { layer = enemyLayer };
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.25f, 2.5f);
            col.offset = new Vector2(0f, 1.25f);

            var health = root.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").intValue = 60;
            hso.ApplyModifiedPropertiesWithoutUndo();

            var dummy = root.AddComponent<TrainingDummy>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var body = Quad("Body", visual, sprite, new Vector2(0f, 1.25f), new Vector2(1.25f, 2.5f), Color.white, 5);
            Quad("Eye", visual, sprite, new Vector2(0.3f, 2.05f), new Vector2(0.25f, 0.25f), new Color(0.38f, 0.78f, 0.13f), 6);
            Quad("Club", visual, sprite, new Vector2(0.8f, 1.2f), new Vector2(0.25f, 1.5f), new Color(0.42f, 0.25f, 0.13f), 6);

            var so = new SerializedObject(dummy);
            so.FindProperty("playerMask").intValue = 1 << playerLayer;
            so.FindProperty("visualRoot").objectReferenceValue = visual;
            so.FindProperty("bodyRenderer").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, DummyPrefabPath);
        }

        static SpriteRenderer Quad(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            EnsureFolderFor(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------ assets

        static Sprite EnsurePlaceholderSprite()
        {
            if (!File.Exists(SpritePath))
            {
                EnsureFolderFor(SpritePath);
                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var pixels = new Color32[16 * 16];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
            }
            // Tiled necesita malla rectangular completa.
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            if (importer.spriteImportMode != SpriteImportMode.Single || GetMeshType(importer) != SpriteMeshType.FullRect)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        static SpriteMeshType GetMeshType(TextureImporter importer)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteMeshType;
        }

        static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            EnsureFolderFor(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static PhysicsMaterial2D EnsureNoFriction()
        {
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
            if (mat != null)
                return mat;
            EnsureFolderFor(NoFrictionPath);
            mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, NoFrictionPath);
            return mat;
        }

        static void EnsureFolderFor(string assetPath)
        {
            var folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
                return;
            EnsureFolderFor(folder); // crea primero la carpeta padre
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0)
                return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 6; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = name;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            }
            throw new System.InvalidOperationException("No quedan capas libres para " + name);
        }
    }
}
