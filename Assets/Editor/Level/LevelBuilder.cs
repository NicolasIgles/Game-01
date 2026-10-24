using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Game01.EditorTools
{
    /// <summary>
    /// Arma una zona a partir de sus salas escritas como texto (Assets/Levels/&lt;Zona&gt;/*.txt,
    /// ver Assets/Levels/README.md): tilemaps con autotile, fondos con parallax,
    /// objetos, enemigos, efectos, luces y límites de cámara. La escena se
    /// regenera entera en cada construcción; lo que se edita son los .txt.
    /// </summary>
    public static class LevelBuilder
    {
        const string ForestFolder = "Assets/Levels/Bosque";
        const string ForestScene = "Assets/Scenes/Bosque.unity";
        const string BaseScene = "Assets/Scenes/SampleScene.unity";

        const string TilesPng = "Assets/Art/Environment/Tiles/forest_tiles.png";
        const string HazardsPng = "Assets/Art/Environment/Tiles/forest_hazards.png";
        const string BgFolder = "Assets/Art/Environment/Backgrounds/";
        const string PropsPng = "Assets/Art/Environment/Props/forest_props.png";
        const string CommonPng = "Assets/Art/Environment/Props/common_objects.png";
        const string CastPng = "Assets/Art/Characters/Placeholders/forest_cast.png";
        const string VfxPng = "Assets/Art/VFX/vfx_forest.png";
        const string AltarModel = "Assets/Art/Environment/Props/altar.aseprite";
        const string LanternModel = "Assets/Art/Environment/Props/spirit_lantern.aseprite";
        const string VeilModel = "Assets/Art/Environment/Props/spirit_veil.aseprite";
        const string FragmentModel = "Assets/Art/Environment/Props/grace_fragment.aseprite";

        const string PlayerPrefab = "Assets/Prefabs/Player.prefab";
        const string GhoulPrefab = "Assets/Prefabs/Enemies/Ghoul.prefab";
        const string DummyPrefab = "Assets/Prefabs/TrainingDummy.prefab";
        const string GhoulStatsPath = "Assets/Data/Enemies/GhoulStats.asset";
        const string AzazelStatsPath = "Assets/Data/Enemies/AzazelStats.asset";
        const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        static readonly Color SpiritGreen = new(0.38f, 0.78f, 0.13f);
        static readonly Color GraceGold = new(0.91f, 0.78f, 0.38f);
        static readonly Color NightLight = new(0.55f, 0.62f, 0.85f);

        const string SolidChars = "#RCW";

        // Orden de dibujo: fondo < decorado < tiles < objetos < enemigos < jugador < efectos < primer plano.
        const int OrderSky = -100, OrderFar = -90, OrderMid = -80, OrderDecorBig = -3, OrderDecor = -2,
            OrderTiles = 0, OrderObjects = 2, OrderFog = 7, OrderAmbient = 6, OrderFront = 20, OrderForeground = 50;

        [MenuItem("Tools/Game01/Construir Bosque de la Caída")]
        public static void BuildForest() => Build(ForestFolder, ForestScene);

        // ================================================================ datos

        class Room
        {
            public string file, name, background = "bosque";
            public Vector2Int origin;
            public int width, height;
            public bool boss;
            public readonly List<string> effects = new();
            public readonly Dictionary<string, string> texts = new();
            public Rect Rect => new(origin.x, origin.y, width, height);
        }

        [Serializable] class SheetJson { public SpriteJson[] sprites; }
        [Serializable] class SpriteJson { public string name; public int x, y, w, h; public float px, py; }

        class Ctx
        {
            public readonly List<Room> rooms = new();
            public readonly Dictionary<Vector2Int, char> cells = new();
            public readonly Dictionary<Vector2Int, Room> owner = new();
            public readonly Dictionary<string, Sprite> sprites = new();
            public Material unlit;
            public int ground, enemy, player;
            public Transform root, objects, enemies, decor, effects, backgrounds;
            public readonly List<GameObject> rootSteps = new(), bossDoors = new(), fog = new();
            public readonly List<PixelParticles> wisps = new();
            public readonly List<Lever> levers = new();
            public readonly List<(Room room, MeleeEnemy boss)> bosses = new();
            public PlayerController playerInstance;
            public string generatedFolder;
            public int warnings;

            public Sprite S(string name)
            {
                if (sprites.TryGetValue(name, out var s))
                    return s;
                Debug.LogWarning($"[Nivel] Falta el sprite '{name}'.");
                warnings++;
                return null;
            }

            public char At(int x, int y) => cells.TryGetValue(new Vector2Int(x, y), out var c) ? c : '.';
            public bool Solid(int x, int y) => SolidChars.IndexOf(At(x, y)) >= 0;
        }

        // ================================================================ entrada

        static void Build(string folder, string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Nivel] Salí del modo Play primero.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var ctx = new Ctx
            {
                ground = LayerMask.NameToLayer("Ground"),
                enemy = LayerMask.NameToLayer("Enemy"),
                player = LayerMask.NameToLayer("Player"),
                generatedFolder = folder + "/Generated",
            };
            if (ctx.ground < 0 || ctx.enemy < 0 || ctx.player < 0)
            {
                Debug.LogError("[Nivel] Faltan las capas Ground, Player o Enemy; corré antes Tools > Game01 > Crear sala de prueba.");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Construyendo nivel", "Leyendo salas", 0.1f);
                ReadRooms(folder, ctx);
                EditorUtility.DisplayProgressBar("Construyendo nivel", "Preparando el arte", 0.25f);
                LoadArt(ctx);
                EditorUtility.DisplayProgressBar("Construyendo nivel", "Armando la escena", 0.5f);
                BuildScene(scenePath, ctx);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ================================================================ lectura de salas

        static void ReadRooms(string folder, Ctx ctx)
        {
            var files = Directory.GetFiles(folder, "*.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray();
            if (files.Length == 0)
                throw new InvalidOperationException($"No hay salas (.txt) en {folder}.");

            foreach (var file in files)
            {
                var room = new Room { file = Path.GetFileName(file), name = Path.GetFileNameWithoutExtension(file) };
                var lines = File.ReadAllLines(file);
                int i = 0;
                for (; i < lines.Length && lines[i].Trim() != "---"; i++)
                {
                    var line = lines[i];
                    int colon = line.IndexOf(':');
                    if (colon < 0)
                        continue;
                    string key = line[..colon].Trim().ToLowerInvariant();
                    string value = line[(colon + 1)..].Trim();
                    switch (key)
                    {
                        case "nombre": room.name = value; break;
                        case "origen":
                            var parts = value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            room.origin = new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
                            break;
                        case "fondo": room.background = value.ToLowerInvariant(); break;
                        case "efectos":
                            room.effects.AddRange(value.Split(',').Select(e => e.Trim().ToLowerInvariant()).Where(e => e.Length > 0));
                            break;
                        case "jefe": room.boss = value.StartsWith("s", StringComparison.OrdinalIgnoreCase); break;
                        default:
                            if (key.StartsWith("texto "))
                                room.texts[key[6..].Trim().ToUpperInvariant()] = value;
                            break;
                    }
                }

                var rows = lines.Skip(i + 1).Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToList();
                if (rows.Count == 0)
                {
                    Debug.LogWarning($"[Nivel] {room.file} no tiene mapa.");
                    continue;
                }
                room.height = rows.Count;
                room.width = rows.Max(r => r.Length);
                if (rows.Any(r => r.Length != room.width))
                {
                    Debug.LogWarning($"[Nivel] {room.file}: no todas las filas miden {room.width}; las cortas se completan con vacío.");
                    ctx.warnings++;
                }

                for (int row = 0; row < rows.Count; row++)
                {
                    int y = room.origin.y + room.height - 1 - row;
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        char c = rows[row][col];
                        if (c == '.')
                            continue;
                        var key = new Vector2Int(room.origin.x + col, y);
                        if (ctx.cells.TryGetValue(key, out var prev) && prev != c)
                        {
                            Debug.LogWarning($"[Nivel] {room.file}: la casilla {key} pisa a otra sala ('{prev}' → '{c}').");
                            ctx.warnings++;
                        }
                        ctx.cells[key] = c;
                        ctx.owner[key] = room;
                    }
                }
                ctx.rooms.Add(room);
            }
        }

        // ================================================================ arte

        static void LoadArt(Ctx ctx)
        {
            foreach (var png in new[] { TilesPng, HazardsPng, PropsPng, CommonPng, CastPng, VfxPng, BgFolder + "forest_fg.png" })
                foreach (var kv in SliceSheet(png))
                    ctx.sprites[kv.Key] = kv.Value;
            foreach (var name in new[] { "forest_sky", "forest_far", "forest_mid", "cemetery_mid", "forest_fog" })
                ctx.sprites[name] = SingleSprite(BgFolder + name + ".png");
            ctx.unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (ctx.unlit == null)
                Debug.LogWarning("[Nivel] No encontré Sprite-Unlit-Default; los fondos quedan iluminados por la luz global.");
        }

        static void BaseImportSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelArtSettings.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // necesario para el modo Tiled
            importer.SetTextureSettings(settings);
        }

        /// <summary>Corta una hoja según su .sprites.json. Conserva los IDs de los sprites que ya existían.</summary>
        static Dictionary<string, Sprite> SliceSheet(string pngPath)
        {
            var jsonPath = Path.ChangeExtension(pngPath, null) + ".sprites.json";
            if (!File.Exists(pngPath) || !File.Exists(jsonPath))
                throw new FileNotFoundException($"Falta {pngPath} o su .sprites.json. Generá el arte con Tools/placeholder-art.");
            var sheet = JsonUtility.FromJson<SheetJson>(File.ReadAllText(jsonPath));

            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            BaseImportSettings(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = new Dictionary<string, SpriteRect>();
            foreach (var r in provider.GetSpriteRects())
                existing[r.name] = r;

            var rects = new List<SpriteRect>();
            foreach (var s in sheet.sprites)
            {
                if (!existing.TryGetValue(s.name, out var rect))
                    rect = new SpriteRect { name = s.name, spriteID = GUID.Generate() };
                rect.rect = new Rect(s.x, s.y, s.w, s.h);
                rect.alignment = SpriteAlignment.Custom;
                rect.pivot = new Vector2(s.px, s.py);
                rects.Add(rect);
            }
            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            provider.Apply();
            importer.SaveAndReimport();

            var result = new Dictionary<string, Sprite>();
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>())
                result[sprite.name] = sprite;
            return result;
        }

        static Sprite SingleSprite(string pngPath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            if (importer == null)
                throw new FileNotFoundException($"Falta {pngPath}. Generá el arte con Tools/placeholder-art.");
            BaseImportSettings(importer);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        // ================================================================ escena

        static void BuildScene(string scenePath, Ctx ctx)
        {
            // Conserva el GUID de la escena: Git solo ve cambios reales y Build Settings no se rompe.
            var scene = SceneFiles.ResetFromTemplate(BaseScene, scenePath);

            ctx.root = new GameObject("Nivel").transform;
            ctx.backgrounds = Child("Fondos", ctx.root);
            ctx.decor = Child("Decorado", ctx.root);
            ctx.objects = Child("Objetos", ctx.root);
            ctx.enemies = Child("Enemigos", ctx.root);
            ctx.effects = Child("Efectos", ctx.root);

            var systems = new GameObject("Sistemas");
            systems.AddComponent<LevelMessages>();
            var fx = systems.AddComponent<LevelFX>();
            fx.Configure(Frames(ctx, "dust_", 4), Frames(ctx, "spark_", 3), Frames(ctx, "parry_", 4), ctx.unlit);

            BuildTilemaps(ctx);
            BuildObjects(ctx);
            BuildGroups(ctx);
            BuildEffects(ctx);
            BuildBackgrounds(ctx);
            var globalLight = SetupLighting();
            var afterKing = BuildAfterKing(ctx, globalLight);
            BuildBossArenas(ctx, afterKing);
            var rooms = BuildRooms(ctx);
            SetupCamera(ctx, rooms);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SceneFiles.EnsureInBuildSettings(scenePath);
            AssetDatabase.SaveAssets();

            string warn = ctx.warnings > 0 ? $" Avisos: {ctx.warnings} (ver arriba)." : "";
            Debug.Log($"[Nivel] {Path.GetFileNameWithoutExtension(scenePath)} listo: {ctx.rooms.Count} salas, {ctx.cells.Count} casillas.{warn}");
        }

        static Transform Child(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static Sprite[] Frames(Ctx ctx, string prefix, int count) =>
            Enumerable.Range(0, count).Select(i => ctx.S(prefix + i)).Where(s => s != null).ToArray();

        // ---------------------------------------------------------------- tilemaps

        static void BuildTilemaps(Ctx ctx)
        {
            var grid = new GameObject("Grid", typeof(Grid)).transform;
            grid.SetParent(ctx.root, false);

            var solid = MakeTilemap("Suelo", grid, ctx.ground, OrderTiles);
            AddCollider(solid, trigger: false, oneWay: false);
            var platforms = MakeTilemap("Plataformas", grid, ctx.ground, OrderTiles);
            AddCollider(platforms, trigger: false, oneWay: true);
            var hazards = MakeTilemap("Peligros", grid, 0, OrderTiles + 1);
            AddCollider(hazards, trigger: true, oneWay: false);
            hazards.gameObject.AddComponent<Hazard>();

            var embers = EnsureAnimatedTile(ctx, "embers", Frames(ctx, "embers_", 4));
            foreach (var (pos, c) in ctx.cells)
            {
                var cell = new Vector3Int(pos.x, pos.y, 0);
                switch (c)
                {
                    case '#': case 'R': case 'C': case 'W':
                        solid.SetTile(cell, EnsureTile(ctx, SolidSprite(ctx, pos.x, pos.y, c), Tile.ColliderType.Grid));
                        break;
                    case '=':
                        bool l = ctx.At(pos.x - 1, pos.y) == '=', r = ctx.At(pos.x + 1, pos.y) == '=';
                        platforms.SetTile(cell, EnsureTile(ctx, !l && r ? "log_l" : l && !r ? "log_r" : "log_m", Tile.ColliderType.Grid));
                        break;
                    case '~':
                        hazards.SetTile(cell, embers);
                        break;
                    case '^':
                        hazards.SetTile(cell, EnsureTile(ctx, "stakes", Tile.ColliderType.Grid));
                        break;
                }
            }
            foreach (var tm in new[] { solid, platforms, hazards })
                tm.CompressBounds();
        }

        static Tilemap MakeTilemap(string name, Transform grid, int layer, int order)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)) { layer = layer };
            go.transform.SetParent(grid, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }

        static void AddCollider(Tilemap tm, bool trigger, bool oneWay)
        {
            var go = tm.gameObject;
            var col = go.AddComponent<TilemapCollider2D>();
            if (trigger)
            {
                col.isTrigger = true;
                return;
            }
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (oneWay)
            {
                composite.usedByEffector = true;
                var effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 170f;
            }
        }

        /// <summary>Autotile simple: borde superior, laterales e inferior según los vecinos sólidos.</summary>
        static string SolidSprite(Ctx ctx, int x, int y, char c)
        {
            int hash = ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;
            bool top = !ctx.Solid(x, y + 1), bottom = !ctx.Solid(x, y - 1);
            bool left = !ctx.Solid(x - 1, y), right = !ctx.Solid(x + 1, y);
            switch (c)
            {
                case 'W':
                    return "smooth_" + (hash % 2);
                case 'C':
                    return top ? "crater_top_" + (hash % 2) : "crater_0";
                case 'R':
                    if (top) return "rock_top_" + (hash % 2);
                    if (bottom) return "rock_bottom";
                    if (left) return "rock_l";
                    if (right) return "rock_r";
                    return "rock_" + (hash % 2);
                default:
                    if (top)
                        return left && right ? "dirt_top_lr" : left ? "dirt_top_l" : right ? "dirt_top_r" : "dirt_top_" + (hash % 3);
                    if (bottom) return "dirt_bottom";
                    if (left) return "dirt_l";
                    if (right) return "dirt_r";
                    return "dirt_" + (hash % 4);
            }
        }

        static TileBase EnsureTile(Ctx ctx, string spriteName, Tile.ColliderType collider)
        {
            string path = $"{ctx.generatedFolder}/Tiles/{spriteName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                EnsureFolder(Path.GetDirectoryName(path));
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = ctx.S(spriteName);
            tile.colliderType = collider;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static TileBase EnsureAnimatedTile(Ctx ctx, string name, Sprite[] frames)
        {
            string path = $"{ctx.generatedFolder}/Tiles/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(path);
            if (tile == null)
            {
                EnsureFolder(Path.GetDirectoryName(path));
                tile = ScriptableObject.CreateInstance<AnimatedTile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.m_AnimatedSprites = frames;
            tile.m_MinSpeed = 5f;
            tile.m_MaxSpeed = 7f;
            tile.m_TileColliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        // ---------------------------------------------------------------- objetos de una casilla

        static void BuildObjects(Ctx ctx)
        {
            foreach (var (pos, c) in ctx.cells)
            {
                var room = ctx.owner[pos];
                var at = new Vector2(pos.x + 0.5f, pos.y); // apoyado en el borde inferior de la casilla
                switch (c)
                {
                    case 'P': PlacePlayer(ctx, at); break;
                    case 'A': PlaceAltar(ctx, at); break;
                    case 'S': PlaceLore(ctx, room, 'S', at, "Sarah", "sarah", radius: 2.5f); break;
                    case 'K': PlaceLore(ctx, room, 'K', at, "Relicario", "reliquary_closed", radius: 1.5f, after: "reliquary_open", banner: "Relicario"); break;
                    case 'X': PlaceEnd(ctx, room, at); break;
                    case 'L': PlaceLever(ctx, at); break;
                    case 'h': PlaceGhoul(ctx, at); break;
                    case 'c': PlacePlaceholder(ctx, at, "Cuervo de ceniza", "crow", 20, new Vector2(1.5f, 1f), 2.5f, null); break;
                    case 'f': PlacePlaceholder(ctx, at, "Fuego fatuo", "wisp_enemy", 20, new Vector2(1f, 1f), 2.5f, SpiritGreen); break;
                    case 'Z': PlaceBoss(ctx, room, at); break;
                    case 't': Decor(ctx, "Lápida", ((pos.x + pos.y) % 2 == 0) ? "gravestone_a" : "gravestone_b", at, OrderDecor); break;
                    case '+': Decor(ctx, "Cruz", "stone_cross", at, OrderDecor); break;
                    case 'M': Decor(ctx, "Mausoleo", "mausoleum", at, OrderDecorBig); break;
                    case 'T': Decor(ctx, "Árbol muerto", "dead_tree", at, OrderDecorBig - 1); break;
                    case 'H': Decor(ctx, "Cabaña", "cabin", at, OrderDecorBig); break;
                    case 'o': Decor(ctx, "Tronco caído", "fallen_log", at, OrderDecor); break;
                    case 'r': Decor(ctx, "Roca fundida", "molten_rock", at, OrderDecor); break;
                    case 'p': Decor(ctx, "Plumas quemadas", "burnt_feathers", at, OrderDecor); break;
                    case 'l': PlaceLantern(ctx, at); break;
                    case 's': PlaceSmoke(ctx, at); break;
                    default:
                        if (c >= '1' && c <= '9')
                            PlaceLore(ctx, room, c, at, "Inscripción " + c, "inscription", radius: 2f);
                        else if ("#RCW=~^FEwVB".IndexOf(c) < 0)
                        {
                            Debug.LogWarning($"[Nivel] {room.file}: carácter desconocido '{c}' en {pos}.");
                            ctx.warnings++;
                        }
                        break;
                }
            }
        }

        static GameObject Obj(string name, Transform parent, Vector2 pos, int layer = 0)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go;
        }

        static SpriteRenderer SpriteChild(GameObject parent, Sprite sprite, int order, Material mat = null, string name = "Sprite")
        {
            var go = new GameObject(name) { layer = parent.layer };
            go.transform.SetParent(parent.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (mat != null)
                sr.sharedMaterial = mat;
            return sr;
        }

        static GameObject Model(string path, Transform parent, int order)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[Nivel] Falta {path}.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.localPosition = Vector3.zero;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingOrder = order;
            if (go.TryGetComponent<UnityEngine.Rendering.SortingGroup>(out var group))
                group.sortingOrder = order;
            return go;
        }

        static Light2D PointLight(GameObject parent, Vector2 offset, Color color, float radius, float intensity, bool flicker)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = offset;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = radius * 0.2f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.7f;
            if (flicker)
                go.AddComponent<LightFlicker>();
            return light;
        }

        static void Decor(Ctx ctx, string name, string sprite, Vector2 at, int order) =>
            SpriteChild(Obj(name, ctx.decor, at), ctx.S(sprite), order);

        static void PlacePlayer(Ctx ctx, Vector2 at)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (prefab == null)
                throw new FileNotFoundException("Falta " + PlayerPrefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = at;
            ctx.playerInstance = go.GetComponent<PlayerController>();
            float lowest = ctx.rooms.Min(r => r.origin.y);
            // Caer fuera del mapa devuelve al último suelo firme; morir queda como red de seguridad más abajo.
            var tracker = go.AddComponent<SafeGroundTracker>();
            tracker.Configure(lowest - 12f);
            var so = new SerializedObject(ctx.playerInstance);
            so.FindProperty("killHeight").floatValue = lowest - 40f;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<PlayerFX>();
        }

        static void PlaceAltar(Ctx ctx, Vector2 at)
        {
            var go = Obj("Altar de gracia", ctx.objects, at);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3f);
            col.offset = new Vector2(0f, 1.5f);
            var model = Model(AltarModel, go.transform, OrderObjects);
            var light = PointLight(go, new Vector2(0f, 1.6f), GraceGold, 5f, 0.5f, flicker: true);
            go.AddComponent<GraceAltar>().Configure(model != null ? model.GetComponentInChildren<Animator>(true) : null, light);
        }

        static void PlaceLore(Ctx ctx, Room room, char key, Vector2 at, string name, string sprite, float radius,
            string after = null, string banner = null)
        {
            var go = Obj(name, ctx.objects, at);
            var sr = SpriteChild(go, ctx.S(sprite), OrderObjects);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;
            col.offset = new Vector2(0f, 1f);
            if (!room.texts.TryGetValue(key.ToString(), out var text))
            {
                Debug.LogWarning($"[Nivel] {room.file}: '{key}' no tiene 'texto {key}' en la cabecera.");
                ctx.warnings++;
                text = "…";
            }
            go.AddComponent<LoreText>().Configure(text, sr, after != null ? ctx.S(after) : null, banner);
        }

        static void PlaceEnd(Ctx ctx, Room room, Vector2 at)
        {
            var go = Obj("Fin del slice", ctx.objects, at);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 3f);
            col.offset = new Vector2(0f, 1.5f);
            PointLight(go, new Vector2(0f, 1.5f), GraceGold, 4f, 0.8f, flicker: true);
            room.texts.TryGetValue("X", out var text);
            go.AddComponent<LoreText>().Configure(text ?? "Fin del slice.", banner: "Fin del vertical slice");
        }

        static void PlaceLever(Ctx ctx, Vector2 at)
        {
            var go = Obj("Palanca", ctx.objects, at, ctx.enemy); // capa Enemy: la detecta el ataque del jugador
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);
            col.offset = new Vector2(0f, 1f);
            var sr = SpriteChild(go, ctx.S("lever_up"), OrderObjects);
            var lever = go.AddComponent<Lever>();
            lever.Configure(sr, ctx.S("lever_down"), new List<GameObject>()); // los escalones se asignan al armar los grupos
            ctx.levers.Add(lever);
        }

        static void PlaceLantern(Ctx ctx, Vector2 at)
        {
            var go = Obj("Farol de espíritu", ctx.decor, at);
            Model(LanternModel, go.transform, OrderDecor + 1);
            PointLight(go, new Vector2(0f, 1.5f), SpiritGreen, 4f, 0.9f, flicker: true);
        }

        static void PlaceSmoke(Ctx ctx, Vector2 at)
        {
            var go = Obj("Humo", ctx.effects, at + new Vector2(0f, 0.5f));
            go.AddComponent<PixelParticles>().Configure(Frames(ctx, "smoke_", 3), PixelParticles.Shape.Point,
                new Rect(0f, 0f, 2f, 2f), 2.5f, 14, new Vector2(2f, 3.5f), new Vector2(-0.15f, 0.5f), new Vector2(0.15f, 1f),
                0.25f, 0.4f, new Color(1f, 1f, 1f, 0.6f), 0.25f, OrderObjects - 1, ctx.unlit, animate: true);
        }

        static void PlaceGhoul(Ctx ctx, Vector2 at)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GhoulPrefab);
            if (prefab == null)
            {
                Debug.LogWarning("[Nivel] Falta el prefab del necrófago; uso el muñeco.");
                PlacePlaceholder(ctx, at, "Necrófago (muñeco)", "hueco", 40, new Vector2(1f, 2f), 3f, null);
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctx.enemies);
            go.name = "Necrófago";
            go.transform.position = at;
            go.AddComponent<DamageSpark>();
        }

        /// <summary>Enemigos sin comportamiento propio todavía: el muñeco de práctica con su sprite.</summary>
        static void PlacePlaceholder(Ctx ctx, Vector2 at, string name, string sprite, int health, Vector2 body, float range, Color? glow)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctx.enemies);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name + " (provisorio)";
            go.transform.position = at;

            var visual = go.transform.Find("Visual");
            var bodySr = visual.Find("Body").GetComponent<SpriteRenderer>();
            foreach (var extra in new[] { "Eye", "Club" })
            {
                var t = visual.Find(extra);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }
            bodySr.sprite = ctx.S(sprite);
            bodySr.drawMode = SpriteDrawMode.Simple;
            bodySr.transform.localPosition = Vector3.zero;
            bodySr.transform.localScale = Vector3.one;

            var col = go.GetComponent<BoxCollider2D>();
            col.size = body;
            col.offset = new Vector2(0f, body.y * 0.5f);

            var hso = new SerializedObject(go.GetComponent<Health>());
            hso.FindProperty("maxHealth").intValue = health;
            hso.ApplyModifiedPropertiesWithoutUndo();
            var dso = new SerializedObject(go.GetComponent<TrainingDummy>());
            dso.FindProperty("attackRange").floatValue = range;
            dso.FindProperty("damage").intValue = 10;
            dso.FindProperty("windup").floatValue = 0.45f;
            dso.FindProperty("unblockableEvery").intValue = 0;
            dso.FindProperty("hitboxSize").vector2Value = new Vector2(1.6f, 1.2f);
            dso.FindProperty("hitboxOffset").vector2Value = new Vector2(0.9f, body.y * 0.5f);
            dso.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<PlaceholderEnemy>();
            if (glow.HasValue)
                PointLight(go, new Vector2(0f, 0.6f), glow.Value, 2.5f, 0.8f, flicker: true);
        }

        static void PlaceBoss(Ctx ctx, Room room, Vector2 at)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GhoulPrefab);
            var baseStats = AssetDatabase.LoadAssetAtPath<EnemyStats>(GhoulStatsPath);
            if (prefab == null || baseStats == null)
            {
                Debug.LogWarning("[Nivel] Falta el necrófago o sus stats; Azazel no se coloca.");
                ctx.warnings++;
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctx.enemies);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "Azazel (provisorio)";
            go.transform.position = at;

            // Sprite estático provisorio en lugar del modelo animado del necrófago.
            var visual = go.transform.Find("Visual");
            for (int i = visual.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(visual.GetChild(i).gameObject);
            var sr = SpriteChild(visual.gameObject, ctx.S("azazel"), 5);
            var anim = go.GetComponent<EnemyAnimator>();
            if (anim != null)
                Object.DestroyImmediate(anim);

            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(2.5f, 5f);
            col.offset = new Vector2(0f, 2.5f);

            var so = new SerializedObject(go.GetComponent<MeleeEnemy>());
            so.FindProperty("stats").objectReferenceValue = EnsureBossStats(baseStats);
            so.FindProperty("bodyRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<DamageSpark>();
            PointLight(go, new Vector2(0f, 3f), SpiritGreen, 6f, 0.9f, flicker: true);
            ctx.bosses.Add((room, go.GetComponent<MeleeEnemy>()));
        }

        /// <summary>Valores iniciales de Azazel en fase 1. Si el asset ya existe no se toca: se ajusta a mano.</summary>
        static EnemyStats EnsureBossStats(EnemyStats baseStats)
        {
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStats>(AzazelStatsPath);
            if (stats != null)
                return stats;
            stats = Object.Instantiate(baseStats);
            stats.maxHealth = 260;
            stats.patrolSpeed = 1.2f;
            stats.chaseSpeed = 3.2f;
            stats.patrolDistance = 3f;
            stats.detectionRange = 30f;
            stats.loseRange = 40f;
            stats.verticalTolerance = 6f;
            stats.requireLineOfSight = false;
            stats.backDetectionRange = 30f;
            stats.attackRange = 3.2f;
            stats.attackWindup = 0.6f;
            stats.attackRecovery = 0.8f;
            stats.attackCooldown = 0.5f;
            stats.attackDamage = 25;
            stats.attackKnockback = 8f;
            stats.hitboxSize = new Vector2(3.4f, 2.6f);
            stats.hitboxOffset = new Vector2(1.9f, 1.4f);
            stats.unblockableEvery = 3;
            stats.hurtKnockbackMultiplier = 0.2f;
            stats.stunTime = 1.8f;
            EnsureFolder(Path.GetDirectoryName(AzazelStatsPath));
            AssetDatabase.CreateAsset(stats, AzazelStatsPath);
            return stats;
        }

        // ---------------------------------------------------------------- objetos de varias casillas

        static List<RectInt> Groups(Ctx ctx, char c)
        {
            var seen = new HashSet<Vector2Int>();
            var result = new List<RectInt>();
            foreach (var start in ctx.cells.Where(kv => kv.Value == c).Select(kv => kv.Key).OrderBy(p => p.y).ThenBy(p => p.x))
            {
                if (!seen.Add(start))
                    continue;
                var stack = new Stack<Vector2Int>();
                stack.Push(start);
                int minX = start.x, maxX = start.x, minY = start.y, maxY = start.y;
                while (stack.Count > 0)
                {
                    var p = stack.Pop();
                    minX = Math.Min(minX, p.x); maxX = Math.Max(maxX, p.x);
                    minY = Math.Min(minY, p.y); maxY = Math.Max(maxY, p.y);
                    foreach (var d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                    {
                        var n = p + d;
                        if (ctx.At(n.x, n.y) == c && seen.Add(n))
                            stack.Push(n);
                    }
                }
                result.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
            }
            return result;
        }

        static GameObject OneWayPlatform(Ctx ctx, string name, RectInt g, Sprite sprite, float spriteY, float colliderTop, float thickness)
        {
            var go = Obj(name, ctx.objects, new Vector2(g.x + g.width * 0.5f, g.y), ctx.ground);
            var sr = SpriteChild(go, sprite, OrderObjects);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(g.width, sprite != null ? sprite.rect.height / PixelArtSettings.PixelsPerUnit : 1f);
            sr.transform.localPosition = new Vector3(0f, spriteY, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(g.width, thickness);
            col.offset = new Vector2(0f, colliderTop - thickness * 0.5f);
            col.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;
            return go;
        }

        static void BuildGroups(Ctx ctx)
        {
            // E: escalones de raíces que baja la palanca (sprite de 16×16 con pivote al centro).
            foreach (var g in Groups(ctx, 'E'))
                ctx.rootSteps.Add(OneWayPlatform(ctx, "Escalón de raíces", g, ctx.S("root_step"), 0.5f, 1f, 0.4f));
            foreach (var lever in ctx.levers)
            {
                var so = new SerializedObject(lever);
                var list = so.FindProperty("reveal");
                list.arraySize = ctx.rootSteps.Count;
                for (int i = 0; i < ctx.rootSteps.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = ctx.rootSteps[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (ctx.rootSteps.Count > 0 && ctx.levers.Count == 0)
            {
                Debug.LogWarning("[Nivel] Hay escalones E pero ninguna palanca L: nunca van a aparecer.");
                ctx.warnings++;
            }
            foreach (var step in ctx.rootSteps)
                step.SetActive(false);

            // w: los salientes de luz se eliminaron (los reemplaza el salto de pared).
            if (Groups(ctx, 'w').Count > 0)
            {
                Debug.LogWarning("[Nivel] 'w' (saliente de luz) ya no existe: se escala con el salto de pared. Se ignora.");
                ctx.warnings++;
            }

            // W: pared lisa que se escala con el salto de pared (primer fragmento). Aviso al pie.
            foreach (var g in Groups(ctx, 'W'))
            {
                var go = Obj("Pared lisa (1 fragmento)", ctx.objects, new Vector2(g.x - 1.5f, g.y), 0);
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(3f, 4f);
                col.offset = new Vector2(0f, 2f);
                go.AddComponent<AbilityGate>().Configure(AbilityGate.Mode.Reveal, 1, null, new List<GameObject>(),
                    "Una pared lisa con vetas de plata. No hay de dónde agarrarse.",
                    "Las vetas de plata responden a tu gracia: ya podés saltar de pared en pared");
            }

            // V: velo de espíritus, se abre con 2 fragmentos.
            foreach (var g in Groups(ctx, 'V'))
            {
                var go = Obj("Velo de espíritus (2 fragmentos)", ctx.objects, new Vector2(g.x + 0.5f, g.y), 0);
                var hint = go.AddComponent<BoxCollider2D>();
                hint.isTrigger = true;
                hint.size = new Vector2(5f, g.height);
                hint.offset = new Vector2(-2f, g.height * 0.5f);
                var barrier = Obj("Barrera", go.transform, go.transform.position, ctx.ground);
                var block = barrier.AddComponent<BoxCollider2D>();
                block.size = new Vector2(g.width, g.height);
                block.offset = new Vector2(0f, g.height * 0.5f);
                for (int y = 0; y < g.height; y += 4)
                {
                    var veil = Model(VeilModel, go.transform, OrderObjects + 1);
                    if (veil != null)
                        veil.transform.localPosition = new Vector3(0f, y, 0f);
                }
                PointLight(go, new Vector2(0f, g.height * 0.5f), SpiritGreen, 3.5f, 0.7f, flicker: true);
                go.AddComponent<AbilityGate>().Configure(AbilityGate.Mode.Barrier, 2, block, new List<GameObject>(),
                    "Un velo de espíritus. Del otro lado se oyen voces.", "El velo de espíritus se abre");
            }

            // B: puertas de jefe (reja de 2×4), las maneja la arena.
            foreach (var g in Groups(ctx, 'B'))
            {
                var go = Obj("Puerta de jefe", ctx.objects, new Vector2(g.x + g.width * 0.5f, g.y), ctx.ground);
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(g.width, g.height);
                col.offset = new Vector2(0f, g.height * 0.5f);
                SpriteChild(go, ctx.S("boss_door"), OrderObjects + 1);
                ctx.bossDoors.Add(go);
            }

            // F: plataformas frágiles de 2 tiles (sprite de 32×16 con pivote abajo al centro).
            foreach (var g in Groups(ctx, 'F'))
            {
                for (int x = g.x; x < g.xMax; x += 2)
                {
                    var go = Obj("Plataforma frágil", ctx.objects, new Vector2(x + 1f, g.y), ctx.ground);
                    var sr = SpriteChild(go, ctx.S("fragile_0"), OrderObjects);
                    var col = go.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(2f, 0.45f);
                    col.offset = new Vector2(0f, 0.65f);
                    col.usedByEffector = true;
                    var effector = go.AddComponent<PlatformEffector2D>();
                    effector.useOneWay = true;
                    effector.surfaceArc = 170f;
                    go.AddComponent<FragilePlatform>().Configure(col, sr, Frames(ctx, "fragile_", 4));
                }
            }
        }

        // ---------------------------------------------------------------- efectos ambientales

        static void BuildEffects(Ctx ctx)
        {
            foreach (var room in ctx.rooms)
            {
                var r = room.Rect;
                var parent = Child("Efectos · " + room.name, ctx.effects);
                foreach (var effect in room.effects)
                {
                    var go = new GameObject(effect);
                    go.transform.SetParent(parent, false);
                    var p = go.AddComponent<PixelParticles>();
                    switch (effect)
                    {
                        case "ceniza":
                            p.Configure(Frames(ctx, "ash_", 2), PixelParticles.Shape.Area, new Rect(r.x - 4f, r.y + 2f, r.width + 8f, r.height + 2f),
                                r.width * r.height * 0.004f, 90, new Vector2(6f, 10f), new Vector2(-0.3f, -0.9f), new Vector2(0.2f, -0.4f),
                                0.4f, 0.3f, new Color(1f, 1f, 1f, 0.8f), 0.15f, OrderFront, ctx.unlit);
                            break;
                        case "brasas":
                            p.Configure(Frames(ctx, "ember_", 2), PixelParticles.Shape.Area, new Rect(r.x, r.y + 1f, r.width, 7f),
                                r.width * 0.12f, 50, new Vector2(1.5f, 3.5f), new Vector2(-0.2f, 0.6f), new Vector2(0.3f, 1.4f),
                                0.2f, 0.8f, Color.white, 0.2f, OrderAmbient, ctx.unlit);
                            break;
                        case "fatuos":
                            p.Configure(new[] { ctx.S("wisp_0"), ctx.S("wisp_1") }, PixelParticles.Shape.Area,
                                new Rect(r.x + 1f, r.y + 5f, r.width - 2f, Mathf.Min(r.height - 6f, 9f)),
                                r.width * 0.015f, 10, new Vector2(6f, 10f), new Vector2(-0.3f, -0.2f), new Vector2(0.3f, 0.2f),
                                0.5f, 0.2f, Color.white, 0.25f, OrderAmbient, ctx.unlit, flee: 6f);
                            ctx.wisps.Add(p);
                            break;
                        case "niebla":
                            Object.DestroyImmediate(p);
                            BuildFog(ctx, go, room);
                            break;
                        default:
                            Debug.LogWarning($"[Nivel] {room.file}: efecto desconocido '{effect}'.");
                            ctx.warnings++;
                            Object.DestroyImmediate(p);
                            break;
                    }
                }
            }
        }

        /// <summary>Banda de niebla al ras del suelo que se mueve un poco más lento que el mundo.</summary>
        static void BuildFog(Ctx ctx, GameObject go, Room room)
        {
            var r = room.Rect;
            float floorY = r.yMin + 5f;
            var sr = SpriteChild(go, ctx.S("forest_fog"), OrderFog, ctx.unlit);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(r.width + 20f, 4f);
            sr.color = new Color(1f, 1f, 1f, 0.75f);
            var pos = new Vector2(r.center.x, floorY + 1f);
            go.transform.position = pos;
            go.AddComponent<ParallaxLayer>().Configure(0.85f, 1f, pos, new Vector2(r.center.x, r.center.y));
            ctx.fog.Add(go);
        }

        // ---------------------------------------------------------------- fondos

        static void BuildBackgrounds(Ctx ctx)
        {
            var first = ctx.rooms[0].Rect;
            var levelCamMinX = ctx.rooms.Min(r => r.Rect.xMin) + 15f;
            var levelCamMaxX = ctx.rooms.Max(r => r.Rect.xMax) - 15f;

            // Cielo: fijo en pantalla, uno para toda la zona.
            var sky = Obj("Cielo", ctx.backgrounds, first.center);
            SpriteChild(sky, ctx.S("forest_sky"), OrderSky, ctx.unlit);
            sky.AddComponent<ParallaxLayer>().Configure(0f, 0f, CameraCenter(first), CameraCenter(first));

            foreach (var group in ctx.rooms.GroupBy(r => r.background))
            {
                float minX = group.Min(r => r.Rect.xMin), maxX = group.Max(r => r.Rect.xMax);
                var refRoom = group.First().Rect;
                var refCam = CameraCenter(refRoom);
                string midSprite = group.Key == "cementerio" ? "cemetery_mid" : "forest_mid";
                float span = Mathf.Max(30f, levelCamMaxX - levelCamMinX);

                var far = Layer(ctx, $"Fondo lejano · {group.Key}", "forest_far", OrderFar, 0.2f, 0f, refCam, span);
                var mid = Layer(ctx, $"Fondo medio · {group.Key}", midSprite, OrderMid, 0.5f, 0f, refCam, span);
                // Cada grupo de fondo se ve solo mientras la cámara está en sus salas.
                if (ctx.rooms.Select(r => r.background).Distinct().Count() > 1)
                {
                    far.LimitTo(minX, maxX, 6f);
                    mid.LimitTo(minX, maxX, 6f);
                }
            }

            BuildForeground(ctx);
        }

        static Vector2 CameraCenter(Rect room) => new(room.xMin + Mathf.Min(15f, room.width * 0.5f), room.center.y);

        static ParallaxLayer Layer(Ctx ctx, string name, string sprite, int order, float fx, float fy, Vector2 refCam, float span)
        {
            var go = Obj(name, ctx.backgrounds, refCam);
            var sr = SpriteChild(go, ctx.S(sprite), order, ctx.unlit);
            sr.drawMode = SpriteDrawMode.Tiled;
            float h = sr.sprite != null ? sr.sprite.rect.height / PixelArtSettings.PixelsPerUnit : 17f;
            // Ancho suficiente para todo el recorrido de la cámara (la capa se desplaza fx por unidad).
            sr.size = new Vector2((fx * span + 40f) * 2f, h);
            var layer = go.AddComponent<ParallaxLayer>();
            layer.Configure(fx, fy, refCam, refCam);
            return layer;
        }

        static void BuildForeground(Ctx ctx)
        {
            var parent = Child("Primer plano", ctx.backgrounds);
            var rnd = new System.Random(7);
            string[] pieces = { "fg_branch", "fg_fern", "fg_trunk" };
            foreach (var room in ctx.rooms.Where(rm => !rm.boss && rm.height <= 26))
            {
                var r = room.Rect;
                int k = 0;
                for (float x = r.xMin + 6f + (float)rnd.NextDouble() * 6f; x < r.xMax - 4f; x += 14f + (float)rnd.NextDouble() * 10f, k++)
                {
                    string piece = pieces[(k + rnd.Next(2)) % pieces.Length];
                    float y = piece == "fg_branch" ? r.yMax : SurfaceY(ctx, Mathf.FloorToInt(x), room);
                    var pos = new Vector2(x, y);
                    var go = Obj(piece, parent, pos);
                    // La rama tiene el pivote arriba: cuelga desde el borde superior de la sala.
                    SpriteChild(go, ctx.S(piece), OrderForeground, ctx.unlit);
                    go.AddComponent<ParallaxLayer>().Configure(1.2f, 1f, pos, new Vector2(x, r.center.y));
                }
            }
        }

        static float SurfaceY(Ctx ctx, int x, Room room)
        {
            for (int y = room.origin.y + room.height - 1; y >= room.origin.y; y--)
                if (ctx.Solid(x, y))
                    return y + 1f;
            return room.origin.y;
        }

        // ---------------------------------------------------------------- luz, jefe, salas y cámara

        static Light2D SetupLighting()
        {
            var global = Object.FindObjectsByType<Light2D>().FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
            if (global == null)
            {
                global = new GameObject("Global Light 2D").AddComponent<Light2D>();
                global.lightType = Light2D.LightType.Global;
            }
            global.color = NightLight;   // noche azul; la luz cálida sale de altares y faroles
            global.intensity = 0.8f;
            return global;
        }

        static AfterKing BuildAfterKing(Ctx ctx, Light2D globalLight)
        {
            var go = new GameObject("Después del Rey");
            go.transform.SetParent(ctx.root, false);
            var after = go.AddComponent<AfterKing>();
            after.Configure(globalLight, ctx.wisps, ctx.fog);
            return after;
        }

        static void BuildBossArenas(Ctx ctx, AfterKing afterKing)
        {
            foreach (var room in ctx.rooms.Where(r => r.boss))
            {
                var rect = room.Rect;
                var go = Obj("Arena · " + room.name, ctx.root, rect.center);
                var boss = ctx.bosses.FirstOrDefault(b => b.room == room).boss;
                if (boss == null)
                {
                    Debug.LogWarning($"[Nivel] {room.file} es sala de jefe pero no tiene 'Z'.");
                    ctx.warnings++;
                    continue;
                }
                boss.transform.SetParent(go.transform, true);
                var doors = ctx.bossDoors.Where(d => rect.Contains(d.transform.position + Vector3.up)).ToList();

                var reward = Obj("Fragmento de gracia", go.transform, new Vector2(rect.center.x, rect.yMin + 6f));
                Model(FragmentModel, reward.transform, OrderObjects + 2);
                var col = reward.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.9f;
                col.offset = new Vector2(0f, 0.7f);
                reward.AddComponent<GraceFragment>();
                PointLight(reward, new Vector2(0f, 0.7f), GraceGold, 4f, 1.2f, flicker: true);

                go.AddComponent<BossArena>().Configure(rect, boss, doors, reward, afterKing,
                    "Azazel, Rey de los Espíritus", "Azazel cae. La niebla se levanta.");
            }
        }

        static LevelRooms BuildRooms(Ctx ctx)
        {
            var go = new GameObject("Salas");
            go.transform.SetParent(ctx.root, false);
            var rooms = go.AddComponent<LevelRooms>();
            rooms.Configure(ctx.rooms.Select(r => new LevelRooms.Room { name = r.name, rect = r.Rect, boss = r.boss }).ToList(),
                ctx.playerInstance != null ? ctx.playerInstance.transform : null);
            return rooms;
        }

        static void SetupCamera(Ctx ctx, LevelRooms rooms)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[Nivel] La escena base no tiene Main Camera.");
                ctx.warnings++;
                return;
            }
            if (!camera.TryGetComponent<CameraFollow2D>(out var follow))
                follow = camera.gameObject.AddComponent<CameraFollow2D>();
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = ctx.playerInstance != null ? ctx.playerInstance.transform : null;
            so.FindProperty("rooms").objectReferenceValue = rooms;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (ctx.playerInstance != null)
            {
                var start = ctx.playerInstance.transform.position;
                camera.transform.position = new Vector3(start.x, start.y + 2f, -10f);
            }
        }

        // ---------------------------------------------------------------- utilidades

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
