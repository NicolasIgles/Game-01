using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Controlador del protagonista: movimiento, salto, dash, ataque en combo,
    /// bloqueo/parry y curas. Máquina de estados simple; toda la lógica corre en
    /// FixedUpdate y los valores ajustables viven en <see cref="PlayerStats"/>.
    /// El objeto raíz tiene el pivote en los pies.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public enum State { Normal, Dash, Attack, Block, Heal, Hurt, Dead }

        [SerializeField] PlayerStats stats;
        [SerializeField] BoxCollider2D bodyCollider;
        [SerializeField] LayerMask groundMask;
        [SerializeField] LayerMask enemyMask;
        [Tooltip("Por debajo de esta altura el jugador muere (cayó fuera del mapa).")]
        [SerializeField] float killHeight = -30f;

        [Header("Visual")]
        [Tooltip("Se espeja según hacia dónde mira el personaje.")]
        [SerializeField] Transform visualRoot;
        [SerializeField] SpriteRenderer bodyRenderer;
        [Tooltip("Tiñe el sprite según el estado (anticipación, golpe activo, ventana de parry...). Útil para ajustar el combate.")]
        [SerializeField] bool debugStateColors;

        public State Current { get; private set; }
        public int Facing { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public int HealCharges { get; private set; }
        public PlayerStats Stats => stats;

        public bool IsInvulnerable =>
            invulnerableTimer > 0f ||
            (Current == State.Dash && stateTimer >= stats.dashInvulnerableFrom && stateTimer <= stats.dashInvulnerableTo);

        /// <summary>Fase del ataque actual, útil para animación y efectos.</summary>
        public bool AttackIsActive => Current == State.Attack && stateTimer >= stats.attackWindup &&
                                      stateTimer < stats.attackWindup + stats.attackActive;
        public bool InParryWindow => Current == State.Block && parryWindowOpen && stateTimer <= stats.parryWindow;
        public int ComboIndex { get; private set; }

        public event Action<State> StateChanged;
        public event Action<int> HealChargesChanged;
        public event Action Parried;
        public event Action Blocked;
        /// <summary>Después de morir y reaparecer. Los enemigos lo usan para volver a su lugar.</summary>
        public event Action Respawned;

        Rigidbody2D rb;
        Health health;
        PlayerInputReader input;

        float stateTimer;
        float coyoteTimer;
        float dashCooldownTimer;
        float invulnerableTimer;
        float lastParryTime = float.NegativeInfinity;
        bool airDashAvailable;
        bool jumpCutAvailable;
        bool parryWindowOpen;
        bool comboQueued;
        bool healApplied;
        Vector3 spawnPoint;
        readonly HashSet<IDamageable> hitThisSwing = new();

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            input = GetComponent<PlayerInputReader>();
            if (bodyCollider == null)
                bodyCollider = GetComponent<BoxCollider2D>();

            rb.gravityScale = 0f; // la gravedad la aplica este script
            rb.freezeRotation = true;

            input.BufferTime = stats.inputBuffer;
            health.SetMax(stats.maxHealth);
            HealCharges = stats.healCharges;
            spawnPoint = transform.position;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateGrounded();
            TickTimers(dt);
            stateTimer += dt;

            switch (Current)
            {
                case State.Normal: UpdateNormal(dt); break;
                case State.Dash: UpdateDash(); break;
                case State.Attack: UpdateAttack(dt); break;
                case State.Block: UpdateBlock(dt); break;
                case State.Heal: UpdateHeal(dt); break;
                case State.Hurt: UpdateHurt(dt); break;
                case State.Dead: UpdateDead(); break;
            }

            if (transform.position.y < killHeight && Current != State.Dead)
                Die();

            input.Tick(dt);
            UpdatePlaceholderVisual();
        }

        // ---------------------------------------------------------------- estados

        void UpdateNormal(float dt)
        {
            float x = HorizontalInput();
            if (x != 0f)
                Facing = (int)x;

            var v = rb.linearVelocity;
            float accel = IsGrounded ? stats.groundAcceleration : stats.airAcceleration;
            v.x = Mathf.MoveTowards(v.x, x * stats.runSpeed, accel * dt);

            if (coyoteTimer > 0f && input.ConsumeJump())
            {
                v.y = stats.JumpVelocity;
                coyoteTimer = 0f;
                jumpCutAvailable = true;
                IsGrounded = false;
            }
            if (jumpCutAvailable && !input.JumpHeld && v.y > 0f)
            {
                v.y *= stats.jumpCutMultiplier;
                jumpCutAvailable = false;
            }
            if (v.y <= 0f)
                jumpCutAvailable = false;

            ApplyGravity(ref v, dt);
            rb.linearVelocity = v;

            // Transiciones, en orden de prioridad.
            if (CanDash() && input.ConsumeDash()) { StartDash(); return; }
            if (input.ConsumeAttack()) { StartAttack(0); return; }
            if (IsGrounded)
            {
                bool freshBlock = input.ConsumeBlockPress();
                if (freshBlock || input.BlockHeld) { StartBlock(freshBlock); return; }
                if (HealCharges > 0 && health.Current < health.Max && input.ConsumeHeal()) { StartHeal(); return; }
            }
        }

        void StartDash()
        {
            float x = HorizontalInput();
            if (x != 0f)
                Facing = (int)x;
            if (!IsGrounded)
                airDashAvailable = false;
            dashCooldownTimer = stats.dashCooldown;
            SetState(State.Dash);
        }

        void UpdateDash()
        {
            rb.linearVelocity = new Vector2(Facing * stats.dashSpeed, 0f);
            if (stateTimer >= stats.dashDuration)
            {
                rb.linearVelocity = new Vector2(Facing * stats.runSpeed, 0f);
                SetState(State.Normal);
            }
        }

        void StartAttack(int comboIndex)
        {
            ComboIndex = comboIndex;
            comboQueued = false;
            hitThisSwing.Clear();
            float x = HorizontalInput();
            if (x != 0f)
                Facing = (int)x;
            if (IsGrounded)
                rb.linearVelocity = new Vector2(Facing * stats.attackLunge, rb.linearVelocity.y);
            SetState(State.Attack);
        }

        void UpdateAttack(float dt)
        {
            var v = rb.linearVelocity;
            if (IsGrounded)
                v.x = Mathf.MoveTowards(v.x, 0f, stats.groundAcceleration * dt);
            else
                v.x = Mathf.MoveTowards(v.x, HorizontalInput() * stats.runSpeed, stats.airAcceleration * dt);
            ApplyGravity(ref v, dt);
            rb.linearVelocity = v;

            float activeStart = stats.attackWindup;
            float activeEnd = activeStart + stats.attackActive;
            float end = activeEnd + stats.attackRecovery;

            if (stateTimer >= activeStart && stateTimer < activeEnd)
                DoAttackHitbox();

            if (stateTimer >= activeStart && ComboIndex + 1 < stats.comboLength && input.ConsumeAttack())
                comboQueued = true;

            if (stateTimer >= activeEnd)
            {
                if (comboQueued) { StartAttack(ComboIndex + 1); return; }
                if (CanDash() && input.ConsumeDash()) { StartDash(); return; }
            }
            if (stateTimer >= end)
                SetState(State.Normal);
        }

        void DoAttackHitbox()
        {
            var hit = new HitInfo
            {
                Damage = stats.attackDamage,
                SourcePosition = transform.position,
                Knockback = Facing * stats.attackKnockback,
            };
            CombatUtils.HitBox(AttackCenter(), stats.attackSize, enemyMask, hit, hitThisSwing, this);
        }

        void StartBlock(bool freshPress)
        {
            parryWindowOpen = freshPress && Time.fixedTime - lastParryTime >= stats.parryCooldown;
            if (parryWindowOpen)
                lastParryTime = Time.fixedTime;
            SetState(State.Block);
        }

        void UpdateBlock(float dt)
        {
            float x = HorizontalInput();
            if (x != 0f)
                Facing = (int)x;

            var v = rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, 0f, stats.groundAcceleration * dt);
            ApplyGravity(ref v, dt);
            rb.linearVelocity = v;

            if (!input.BlockHeld && stateTimer >= stats.minBlockTime)
                SetState(State.Normal);
        }

        void StartHeal()
        {
            HealCharges--;
            HealChargesChanged?.Invoke(HealCharges);
            healApplied = false;
            SetState(State.Heal);
        }

        void UpdateHeal(float dt)
        {
            var v = rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, 0f, stats.groundAcceleration * dt);
            ApplyGravity(ref v, dt);
            rb.linearVelocity = v;

            if (!healApplied && stateTimer >= stats.healCastTime)
            {
                health.Heal(stats.healAmount);
                healApplied = true;
            }
            if (stateTimer >= stats.healCastTime + stats.healRecovery)
                SetState(State.Normal);
        }

        void UpdateHurt(float dt)
        {
            var v = rb.linearVelocity;
            ApplyGravity(ref v, dt);
            rb.linearVelocity = v;
            if (stateTimer >= stats.hurtStunTime)
                SetState(State.Normal);
        }

        void Die()
        {
            rb.linearVelocity = Vector2.zero;
            SetState(State.Dead);
        }

        void UpdateDead()
        {
            rb.linearVelocity = Vector2.zero;
            if (stateTimer >= stats.respawnDelay)
                Respawn();
        }

        void Respawn()
        {
            rb.position = spawnPoint;
            transform.position = spawnPoint;
            rb.linearVelocity = Vector2.zero;
            health.ResetHealth();
            RefillHeals();
            invulnerableTimer = 1f;
            SetState(State.Normal);
            Respawned?.Invoke();
        }

        // ---------------------------------------------------------------- daño

        public HitResult ReceiveHit(in HitInfo hit)
        {
            if (Current == State.Dead || IsInvulnerable)
                return HitResult.Ignored;

            int attackerSide = hit.SourcePosition.x >= transform.position.x ? 1 : -1;
            bool facingAttacker = attackerSide == Facing;

            if (Current == State.Block && facingAttacker && !hit.Unblockable)
            {
                if (InParryWindow)
                {
                    hit.Attacker?.OnParried();
                    invulnerableTimer = stats.parryInvulnerability;
                    Parried?.Invoke();
                    return HitResult.Parried;
                }
                rb.linearVelocity = new Vector2(-attackerSide * stats.blockPushback, rb.linearVelocity.y);
                Blocked?.Invoke();
                return HitResult.Blocked;
            }

            health.TakeDamage(hit.Damage);
            if (health.IsDead)
            {
                Die();
                return HitResult.Hit;
            }

            invulnerableTimer = stats.hurtInvulnerability;
            rb.linearVelocity = new Vector2(-attackerSide * stats.hurtKnockback.x, stats.hurtKnockback.y);
            SetState(State.Hurt); // cancela la curación: la carga ya se gastó
            return HitResult.Hit;
        }

        /// <summary>Recarga las curas; lo van a usar los puntos de guardado.</summary>
        public void RefillHeals()
        {
            HealCharges = stats.healCharges;
            HealChargesChanged?.Invoke(HealCharges);
        }

        public void SetSpawnPoint(Vector3 position) => spawnPoint = position;

        public float KillHeight
        {
            get => killHeight;
            set => killHeight = value;
        }

        // ---------------------------------------------------------------- utilidades

        void SetState(State next)
        {
            Current = next;
            stateTimer = 0f;
            StateChanged?.Invoke(next);
        }

        float HorizontalInput()
        {
            float x = input.Move.x;
            return Mathf.Abs(x) < 0.3f ? 0f : Mathf.Sign(x);
        }

        bool CanDash() => dashCooldownTimer <= 0f && (IsGrounded || airDashAvailable);

        void ApplyGravity(ref Vector2 v, float dt)
        {
            float multiplier = v.y < 0f ? stats.fallGravityMultiplier : 1f;
            v.y = Mathf.Max(v.y - stats.Gravity * multiplier * dt, -stats.maxFallSpeed);
        }

        void UpdateGrounded()
        {
            var bounds = bodyCollider.bounds;
            var center = new Vector2(bounds.center.x, bounds.min.y - 0.05f);
            var size = new Vector2(bounds.size.x * 0.9f, 0.1f);
            IsGrounded = rb.linearVelocity.y <= 0.01f && Physics2D.OverlapBox(center, size, 0f, groundMask) != null;
            if (IsGrounded)
            {
                coyoteTimer = stats.coyoteTime;
                airDashAvailable = true;
            }
        }

        void TickTimers(float dt)
        {
            coyoteTimer -= dt;
            dashCooldownTimer -= dt;
            invulnerableTimer -= dt;
        }

        Vector2 AttackCenter() =>
            (Vector2)transform.position + new Vector2(stats.attackOffset.x * Facing, stats.attackOffset.y);

        // ---------------------------------------------------------------- visual

        static readonly Color ColorNormal = Color.white;
        static readonly Color ColorDash = new(0.25f, 0.82f, 1f);
        static readonly Color ColorWindup = new(1f, 0.85f, 0.3f);
        static readonly Color ColorActive = new(0.8f, 0.09f, 0.19f);
        static readonly Color ColorRecovery = new(0.9f, 0.55f, 0.35f);
        static readonly Color ColorBlock = new(0.35f, 0.5f, 0.9f);
        static readonly Color ColorParry = new(0.91f, 0.78f, 0.38f);
        static readonly Color ColorHeal = new(0.4f, 0.9f, 0.45f);
        static readonly Color ColorHurt = new(1f, 0.3f, 0.3f);
        static readonly Color ColorDead = new(0.3f, 0.3f, 0.35f);

        void UpdatePlaceholderVisual()
        {
            if (visualRoot != null)
                visualRoot.localScale = new Vector3(Facing, 1f, 1f);
            if (bodyRenderer == null)
                return;

            Color c = !debugStateColors ? (Current == State.Hurt ? ColorHurt : Color.white) : Current switch
            {
                State.Dash => ColorDash,
                State.Attack => stateTimer < stats.attackWindup ? ColorWindup : AttackIsActive ? ColorActive : ColorRecovery,
                State.Block => InParryWindow ? ColorParry : ColorBlock,
                State.Heal => ColorHeal,
                State.Hurt => ColorHurt,
                State.Dead => ColorDead,
                _ => ColorNormal,
            };
            // Parpadeo mientras es invulnerable después de un golpe.
            if (invulnerableTimer > 0f && Current != State.Dead && Mathf.Repeat(Time.time, 0.12f) < 0.06f)
                c.a = 0.35f;
            bodyRenderer.color = c;
        }

        void OnDrawGizmosSelected()
        {
            if (stats == null)
                return;
            int facing = Application.isPlaying ? Facing : 1;
            var center = (Vector2)transform.position + new Vector2(stats.attackOffset.x * facing, stats.attackOffset.y);
            CombatUtils.DrawBox(center, stats.attackSize, AttackIsActive ? Color.red : new Color(1f, 0.5f, 0f, 0.6f));
        }
    }
}
