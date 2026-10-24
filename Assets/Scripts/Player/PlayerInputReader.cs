using UnityEngine;
using UnityEngine.InputSystem;

namespace Game01
{
    /// <summary>
    /// Lee las acciones del mapa "Player" (Assets/Settings/InputSystem_Actions)
    /// y guarda las pulsaciones durante un margen corto para que no se pierdan
    /// entre frames de física.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public float BufferTime { get; set; } = 0.12f;

        InputAction move, jump, attack, dash, block, heal;
        float jumpTimer, attackTimer, dashTimer, blockTimer, healTimer;

        public Vector2 Move { get; private set; }
        public bool JumpHeld => jump != null && jump.IsPressed();
        public bool BlockHeld => block != null && block.IsPressed();

        void Awake()
        {
            var actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError("[Input] No hay acciones globales configuradas en Project Settings > Input System Package.");
                enabled = false;
                return;
            }
            move = actions.FindAction("Player/Move", true);
            jump = actions.FindAction("Player/Jump", true);
            attack = actions.FindAction("Player/Attack", true);
            dash = actions.FindAction("Player/Dash", true);
            block = actions.FindAction("Player/Block", true);
            heal = actions.FindAction("Player/Heal", true);
        }

        void OnEnable() => InputSystem.actions?.FindActionMap("Player")?.Enable();

        void Update()
        {
            Move = move.ReadValue<Vector2>();
            if (jump.WasPressedThisFrame()) jumpTimer = BufferTime;
            if (attack.WasPressedThisFrame()) attackTimer = BufferTime;
            if (dash.WasPressedThisFrame()) dashTimer = BufferTime;
            if (block.WasPressedThisFrame()) blockTimer = BufferTime;
            if (heal.WasPressedThisFrame()) healTimer = BufferTime;
        }

        /// <summary>Lo llama el controlador al final de cada paso de física.</summary>
        public void Tick(float dt)
        {
            jumpTimer -= dt;
            attackTimer -= dt;
            dashTimer -= dt;
            blockTimer -= dt;
            healTimer -= dt;
        }

        public bool ConsumeJump() => Consume(ref jumpTimer);
        public bool ConsumeAttack() => Consume(ref attackTimer);
        public bool ConsumeDash() => Consume(ref dashTimer);
        public bool ConsumeBlockPress() => Consume(ref blockTimer);
        public bool ConsumeHeal() => Consume(ref healTimer);

        static bool Consume(ref float timer)
        {
            if (timer <= 0f)
                return false;
            timer = 0f;
            return true;
        }
    }
}
