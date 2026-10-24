using UnityEditor;
using UnityEngine;

namespace Game01.EditorTools
{
    /// <summary>
    /// Atajos para probar niveles en modo Play: saltar de sala en sala y sumar
    /// fragmentos de gracia sin tener que jugar todo el recorrido.
    /// </summary>
    public static class LevelDebugMenu
    {
        const string Root = "Tools/Game01/Probar nivel/";

        [MenuItem(Root + "Ir a la sala siguiente %#.")]
        static void NextRoom() => Jump(+1);

        [MenuItem(Root + "Ir a la sala anterior %#,")]
        static void PreviousRoom() => Jump(-1);

        [MenuItem(Root + "Sumar un fragmento de gracia")]
        static void AddFragment()
        {
            PlayerProgress.AddFragment();
            LevelMessages.Banner($"[Prueba] Fragmentos: {PlayerProgress.Fragments}", 2f);
        }

        [MenuItem(Root + "Derrotar al jefe activo")]
        static void DefeatBoss()
        {
            foreach (var arena in Object.FindObjectsByType<BossArena>())
                foreach (var behaviour in arena.GetComponentsInChildren<MonoBehaviour>())
                    if (behaviour is IBoss boss && behaviour.isActiveAndEnabled)
                        boss.ReceiveHit(new HitInfo { Damage = 100000, SourcePosition = behaviour.transform.position });
        }

        [MenuItem(Root + "Ir a la sala siguiente %#.", true)]
        [MenuItem(Root + "Ir a la sala anterior %#,", true)]
        [MenuItem(Root + "Sumar un fragmento de gracia", true)]
        [MenuItem(Root + "Derrotar al jefe activo", true)]
        static bool InPlayMode() => Application.isPlaying;

        static void Jump(int step)
        {
            var rooms = Object.FindAnyObjectByType<LevelRooms>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (rooms == null || player == null || rooms.Rooms.Count == 0)
            {
                Debug.LogWarning("[Prueba] No hay salas o jugador en la escena.");
                return;
            }
            int index = (Mathf.Max(0, rooms.CurrentIndex) + step + rooms.Rooms.Count) % rooms.Rooms.Count;
            var room = rooms.Rooms[index];
            if (!TryFindStandingSpot(room.rect, out var spot))
            {
                Debug.LogWarning($"[Prueba] No encontré suelo en {room.name}.");
                return;
            }
            var rb = player.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            rb.position = spot;
            player.transform.position = spot;
            if (Camera.main != null)
                Camera.main.transform.position = new Vector3(spot.x, spot.y + 2f, Camera.main.transform.position.z);
            LevelMessages.Banner(room.name, 2f);
        }

        /// <summary>Primera columna desde la izquierda con aire arriba y suelo debajo.</summary>
        static bool TryFindStandingSpot(Rect room, out Vector2 spot)
        {
            int ground = LayerMask.GetMask("Ground");
            for (float x = room.xMin + 2.5f; x < room.xMax - 1f; x += 1f)
            {
                for (float y = room.yMax - 1.5f; y > room.yMin; y -= 1f)
                {
                    if (Physics2D.OverlapBox(new Vector2(x, y + 1.4f), new Vector2(0.9f, 2.6f), 0f, ground) != null)
                        continue;
                    var hit = Physics2D.Raycast(new Vector2(x, y), Vector2.down, room.height, ground);
                    if (hit.collider != null && hit.point.y >= room.yMin)
                    {
                        spot = new Vector2(x, hit.point.y + 0.05f);
                        return true;
                    }
                    break;
                }
            }
            spot = default;
            return false;
        }
    }
}
