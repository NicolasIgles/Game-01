using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Recuerda el último lugar firme donde estuvo parado el jugador, lejos de
    /// peligros y plataformas frágiles. Los peligros y las caídas fuera del mapa
    /// lo devuelven ahí.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class SafeGroundTracker : MonoBehaviour
    {
        [Tooltip("Por debajo de esta altura se considera que cayó fuera del mapa.")]
        [SerializeField] float killY = -80f;
        [SerializeField] float hazardMargin = 1.5f;

        PlayerController player;
        Rigidbody2D rb;
        Vector2 lastSafe;
        float sampleTimer;
        static readonly List<Collider2D> s_Hits = new();
        static readonly ContactFilter2D s_Filter = new() { useTriggers = true };

        public void Configure(float minY) => killY = minY;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
            lastSafe = transform.position;
        }

        void FixedUpdate()
        {
            if (transform.position.y < killY)
            {
                ReturnToSafeGround();
                return;
            }
            sampleTimer -= Time.fixedDeltaTime;
            if (sampleTimer > 0f || !player.IsGrounded || player.Current == PlayerController.State.Dead)
                return;
            sampleTimer = 0.15f;

            Vector2 feet = transform.position;
            s_Hits.Clear();
            Physics2D.OverlapBox(feet + Vector2.down * 0.3f, new Vector2(hazardMargin * 2f, 1f), 0f, s_Filter, s_Hits);
            foreach (var hit in s_Hits)
                if (hit.GetComponentInParent<Hazard>() || hit.GetComponentInParent<FragilePlatform>())
                    return;
            lastSafe = feet;
        }

        public void ReturnToSafeGround()
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = lastSafe;
            transform.position = lastSafe;
        }
    }
}
