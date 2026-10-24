using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Cámara que sigue al objetivo con suavizado y mira un poco hacia adelante.
    /// La Pixel Perfect Camera se encarga de mantener la grilla de píxeles.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 offset = new(0f, 2f);
        [Tooltip("Cuánto se adelanta la cámara hacia donde mira el jugador.")]
        [SerializeField] float lookAhead = 2f;
        [SerializeField] float smoothTime = 0.15f;
        [Tooltip("Opcional: limita la cámara a la sala donde está el jugador.")]
        [SerializeField] LevelRooms rooms;

        static readonly Vector2 HalfView = new(
            PixelArtSettings.ReferenceWidth * 0.5f / PixelArtSettings.PixelsPerUnit,
            PixelArtSettings.ReferenceHeight * 0.5f / PixelArtSettings.PixelsPerUnit);

        PlayerController player;
        Vector3 velocity;
        float currentLookAhead;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            player = target != null ? target.GetComponent<PlayerController>() : null;
        }

        void Start() => SetTarget(target);

        void LateUpdate()
        {
            if (target == null)
                return;
            float desiredLookAhead = player != null ? player.Facing * lookAhead : 0f;
            currentLookAhead = Mathf.MoveTowards(currentLookAhead, desiredLookAhead, lookAhead * 2f * Time.deltaTime);

            var goal = new Vector3(target.position.x + offset.x + currentLookAhead, target.position.y + offset.y, transform.position.z);
            if (rooms != null)
            {
                Vector2 clamped = rooms.ClampCamera(goal, HalfView);
                goal = new Vector3(clamped.x, clamped.y, goal.z);
            }
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }
    }
}
