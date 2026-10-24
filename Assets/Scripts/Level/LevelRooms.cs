using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Salas del nivel y sus límites. La cámara no muestra nada fuera de la sala
    /// donde está el jugador; al pasar a otra sala se desliza hasta los nuevos
    /// límites. Lo arma el constructor de niveles a partir de los archivos de texto.
    /// </summary>
    public class LevelRooms : MonoBehaviour
    {
        [Serializable]
        public struct Room
        {
            public string name;
            /// <summary>Rectángulo en unidades del mundo (1 unidad = 1 tile).</summary>
            public Rect rect;
            public bool boss;
        }

        [SerializeField] List<Room> rooms = new();
        [SerializeField] Transform player;

        public IReadOnlyList<Room> Rooms => rooms;
        public int CurrentIndex { get; private set; } = -1;
        public Room? Current => CurrentIndex >= 0 ? rooms[CurrentIndex] : null;

        public event Action<Room> RoomChanged;

        public void Configure(List<Room> list, Transform target)
        {
            rooms = list;
            player = target;
        }

        void Start()
        {
            if (player == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null)
                    player = pc.transform;
            }
            UpdateCurrent(force: true);
        }

        void LateUpdate() => UpdateCurrent(force: false);

        void UpdateCurrent(bool force)
        {
            if (player == null || rooms.Count == 0)
                return;
            // Se mide a la altura del pecho para no cambiar de sala al rozar un borde con los pies.
            Vector2 p = (Vector2)player.position + new Vector2(0f, 1f);
            if (!force && CurrentIndex >= 0 && rooms[CurrentIndex].rect.Contains(p))
                return;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (!rooms[i].rect.Contains(p))
                    continue;
                if (i != CurrentIndex)
                {
                    CurrentIndex = i;
                    RoomChanged?.Invoke(rooms[i]);
                }
                return;
            }
        }

        /// <summary>
        /// Lleva el centro deseado de la cámara adentro de la sala actual. Si la sala
        /// es más chica que la pantalla en un eje, la centra en ese eje.
        /// </summary>
        public Vector2 ClampCamera(Vector2 desired, Vector2 halfView)
        {
            if (CurrentIndex < 0)
                return desired;
            var r = rooms[CurrentIndex].rect;
            desired.x = r.width <= halfView.x * 2f ? r.center.x : Mathf.Clamp(desired.x, r.xMin + halfView.x, r.xMax - halfView.x);
            desired.y = r.height <= halfView.y * 2f ? r.center.y : Mathf.Clamp(desired.y, r.yMin + halfView.y, r.yMax - halfView.y);
            return desired;
        }

        void OnDrawGizmos()
        {
            foreach (var room in rooms)
            {
                Gizmos.color = room.boss ? new Color(1f, 0.3f, 0.3f, 0.8f) : new Color(0.4f, 0.9f, 0.4f, 0.6f);
                Gizmos.DrawWireCube(room.rect.center, room.rect.size);
            }
        }
    }
}
