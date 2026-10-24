using System.Collections;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Envoltorio para los enemigos que todavía no tienen comportamiento propio
    /// (Cuervo de ceniza, Fuego fatuo): usan el muñeco de práctica, pero al morir
    /// desaparecen hasta que el jugador descansa en un altar o reaparece.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlaceholderEnemy : MonoBehaviour
    {
        [SerializeField] float vanishDelay = 0.6f;

        Health health;
        PlayerController player;
        Vector3 spawnPosition;

        void Awake()
        {
            health = GetComponent<Health>();
            spawnPosition = transform.position;
            health.Died += OnDied;
            health.Changed += OnChanged;
        }

        void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.Respawned += ResetEnemy;
        }

        void OnDestroy()
        {
            if (player != null)
                player.Respawned -= ResetEnemy;
        }

        int lastHealth = int.MaxValue;

        void OnChanged(int current, int max)
        {
            if (current < lastHealth && current < max && isActiveAndEnabled)
                LevelFX.Play(LevelFX.Kind.Spark, (Vector2)transform.position + Vector2.up);
            lastHealth = current;
        }

        void OnDied()
        {
            if (isActiveAndEnabled)
                StartCoroutine(Vanish());
        }

        IEnumerator Vanish()
        {
            yield return new WaitForSeconds(vanishDelay);
            gameObject.SetActive(false);
        }

        public void ResetEnemy()
        {
            transform.position = spawnPosition;
            health.ResetHealth();
            lastHealth = health.Max;
            gameObject.SetActive(true);
        }
    }
}
