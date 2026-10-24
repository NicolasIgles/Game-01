using System.Collections;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Efectos de un solo disparo comunes a todas las zonas (Biblia: polvo,
    /// chispa de golpe, destello de parry). Los sprites los asigna el constructor.
    /// </summary>
    public class LevelFX : MonoBehaviour
    {
        public enum Kind { Dust, Spark, Parry }

        [SerializeField] Sprite[] dust;
        [SerializeField] Sprite[] spark;
        [SerializeField] Sprite[] parry;
        [SerializeField] Material material;
        [SerializeField] float frameTime = 0.06f;

        static LevelFX instance;

        void Awake() => instance = this;

        public void Configure(Sprite[] dustFrames, Sprite[] sparkFrames, Sprite[] parryFrames, Material mat)
        {
            dust = dustFrames;
            spark = sparkFrames;
            parry = parryFrames;
            material = mat;
        }

        public static void Play(Kind kind, Vector2 position, bool flipX = false)
        {
            if (instance == null)
                return;
            var frames = kind switch { Kind.Dust => instance.dust, Kind.Spark => instance.spark, _ => instance.parry };
            if (frames == null || frames.Length == 0)
                return;
            instance.StartCoroutine(instance.Run(frames, position, flipX, kind == Kind.Parry ? 30 : 25));
        }

        IEnumerator Run(Sprite[] frames, Vector2 position, bool flipX, int order)
        {
            var go = new GameObject("FX");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            sr.flipX = flipX;
            if (material != null)
                sr.sharedMaterial = material;
            foreach (var f in frames)
            {
                sr.sprite = f;
                yield return new WaitForSeconds(frameTime);
            }
            Destroy(go);
        }
    }
}
