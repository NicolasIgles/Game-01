using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game01.EditorTools
{
    /// <summary>
    /// Reemplaza los rectángulos provisorios del prefab Player por el sprite
    /// animado de Assets/Art/Characters/Player/player.aseprite.
    /// </summary>
    public static class PlayerSpriteSetup
    {
        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        const string SpritePath = "Assets/Art/Characters/Player/player.aseprite";
        static readonly string[] Placeholders = { "Body", "Eye", "Sword" };

        [MenuItem("Tools/Game01/Conectar sprite del jugador")]
        public static void Apply()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SpritePath);
            if (model == null)
            {
                Debug.LogError("[PlayerSprite] No encontré " + SpritePath);
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    Debug.LogError("[PlayerSprite] El prefab no tiene el hijo Visual.");
                    return;
                }

                foreach (var name in Placeholders)
                {
                    var child = visual.Find(name);
                    if (child != null)
                        Object.DestroyImmediate(child.gameObject);
                }

                var existing = visual.Find("Sprite");
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);

                var sprite = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
                sprite.name = "Sprite";
                sprite.transform.localPosition = Vector3.zero;
                if (sprite.TryGetComponent<SortingGroup>(out var group))
                    group.sortingOrder = 10;
                foreach (var sr in sprite.GetComponentsInChildren<SpriteRenderer>(true))
                    sr.sortingOrder = 10;

                var renderer = sprite.GetComponentInChildren<SpriteRenderer>(true);
                var animator = sprite.GetComponentInChildren<Animator>(true);

                var controller = root.GetComponent<PlayerController>();
                var so = new SerializedObject(controller);
                so.FindProperty("bodyRenderer").objectReferenceValue = renderer;
                so.ApplyModifiedPropertiesWithoutUndo();

                if (!root.TryGetComponent<PlayerAnimator>(out var playerAnimator))
                    playerAnimator = root.AddComponent<PlayerAnimator>();
                var aso = new SerializedObject(playerAnimator);
                aso.FindProperty("animator").objectReferenceValue = animator;
                aso.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log($"[PlayerSprite] Sprite conectado. Animator: {(animator != null ? "ok" : "falta")}, SpriteRenderer: {(renderer != null ? "ok" : "falta")}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
