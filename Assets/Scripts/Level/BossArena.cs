using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Arena de jefe: al entrar se cierran las puertas y aparece el jefe. Si el
    /// jugador muere, la arena vuelve a su estado inicial; si gana, se abren las
    /// puertas, aparece el fragmento de gracia y la zona cambia (AfterKing).
    /// </summary>
    public class BossArena : MonoBehaviour
    {
        [SerializeField] Rect area;
        [Tooltip("Cuánto hay que entrar en la arena para que empiece la pelea.")]
        [SerializeField] float triggerDepth = 4f;
        [SerializeField] MeleeEnemy boss;
        [SerializeField] List<GameObject> doors = new();
        [SerializeField] GameObject reward;
        [SerializeField] AfterKing afterKing;
        [SerializeField] string bossTitle = "Azazel, Rey de los Espíritus";
        [SerializeField] string defeatedBanner = "Azazel cae. La niebla se levanta.";
        [SerializeField] float introTime = 1.2f;

        PlayerController player;
        Health playerHealth, bossHealth;
        bool started, defeated;

        public void Configure(Rect arena, MeleeEnemy bossEnemy, List<GameObject> doorObjects, GameObject rewardObject,
            AfterKing after, string title, string defeated)
        {
            area = arena;
            boss = bossEnemy;
            doors = doorObjects;
            reward = rewardObject;
            afterKing = after;
            bossTitle = title;
            defeatedBanner = defeated;
        }

        void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null && player.TryGetComponent(out playerHealth))
                playerHealth.Died += OnPlayerDied;
            if (boss != null)
            {
                bossHealth = boss.GetComponent<Health>();
                bossHealth.Died += OnBossDied;
                boss.gameObject.SetActive(false);
            }
            if (reward != null)
                reward.SetActive(false);
            SetDoors(false, instant: true);
        }

        void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.Died -= OnPlayerDied;
            if (bossHealth != null)
                bossHealth.Died -= OnBossDied;
        }

        void Update()
        {
            if (started || defeated || player == null || player.Current == PlayerController.State.Dead)
                return;
            var inner = new Rect(area.xMin + triggerDepth, area.yMin, area.width - triggerDepth, area.height);
            if (inner.Contains(player.transform.position))
                StartCoroutine(BeginFight());
        }

        IEnumerator BeginFight()
        {
            started = true;
            SetDoors(true, instant: false);
            LevelMessages.Banner(bossTitle, 3f);
            yield return new WaitForSeconds(introTime);
            if (!started)
                yield break;
            boss.gameObject.SetActive(true);
            boss.ResetEnemy();
        }

        void OnBossDied()
        {
            if (defeated)
                return;
            defeated = true;
            StartCoroutine(Victory());
        }

        IEnumerator Victory()
        {
            yield return new WaitForSeconds(boss.Stats != null ? boss.Stats.deathDuration + 0.3f : 1f);
            boss.gameObject.SetActive(false);
            SetDoors(false, instant: false);
            if (reward != null)
                reward.SetActive(true);
            if (afterKing != null)
                afterKing.Apply();
            LevelMessages.Banner(defeatedBanner, 4f);
        }

        void OnPlayerDied()
        {
            if (!started || defeated)
                return;
            started = false;
            StopAllCoroutines();
            StartCoroutine(ResetAfterDeath());
        }

        IEnumerator ResetAfterDeath()
        {
            yield return new WaitForSeconds(0.8f);
            if (boss != null)
                boss.gameObject.SetActive(false);
            SetDoors(false, instant: true);
        }

        void SetDoors(bool closed, bool instant)
        {
            foreach (var door in doors)
            {
                if (door == null)
                    continue;
                foreach (var col in door.GetComponentsInChildren<Collider2D>(true))
                    col.enabled = closed;
                var sr = door.GetComponentInChildren<SpriteRenderer>(true);
                if (sr == null)
                    continue;
                if (instant)
                    sr.enabled = closed;
                else
                    StartCoroutine(SlideDoor(sr, closed));
            }
        }

        static IEnumerator SlideDoor(SpriteRenderer sr, bool closing)
        {
            sr.enabled = true;
            var t = sr.transform;
            const float height = 4f;
            for (float k = 0f; k <= 1f; k += Time.deltaTime / 0.35f)
            {
                float y = closing ? (1f - k) * height : k * height;
                t.localPosition = new Vector3(0f, Mathf.Round(y * 16f) / 16f, 0f);
                yield return null;
            }
            t.localPosition = closing ? Vector3.zero : new Vector3(0f, height, 0f);
            sr.enabled = closing;
            t.localPosition = Vector3.zero;
            if (closing)
                LevelFX.Play(LevelFX.Kind.Dust, sr.transform.position);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
    }
}
