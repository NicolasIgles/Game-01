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
        /// <summary>En el aire, contra una pared y empujando hacia ella: cae despacio.</summary>
        public bool IsWallSliding { get; private set; }
        /// <summary>El salto de pared se desbloquea con fragmentos de gracia (ver PlayerStats).</summary>
        public bool CanWallJump => stats.wallJumpRequiredFragments <= 0 || PlayerProgress.Fragments >= stats.wallJumpRequiredFragments;
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
        public event Action Jumped;
        /// <summary>Saltó desde una pared; el valor es de qué lado estaba la pared (-1 izquierda, 1 derecha).</summary>
        public event Action<int> WallJumped;
        /// <summary>Al tocar el piso, con la velocidad de caída que traía.</summary>
        public event Action<float> Landed;
        /// <summary>Recibió daño (de enemigos o trampas), con la cantidad.</summary>
        public event Action<int> Damaged;
        /// <summary>La curación terminó de canalizarse y recuperó vida.</summary>
        public event Action HealApplied;

        Rigidbody2D rb;
        Health health;
        PlayerInputReader input;

        float stateTimer;
        float coyoteTimer;
        float wallCoyoteTimer;
        float wallJumpLockTimer;
        int lastWallSide;
        float dashCooldownTimer;
        float invulnerableTimer;
        float lastParryTime = float.NegativeInfinity;
        bool airDashAvailable;
        bool jumpCutAvailable;
        bool parryWindowOpen;
        bool comboQueued;
        bool healApplied;
        float lastFallSpeed;
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
            lastFallSpeed = Mathf.Max(0f, -rb.linearVelocity.y);
            UpdatePlaceholderVisual();
        }

        // ---------------------------------------------------------------- estados

        void UpdateNormal(float dt)
        {
            float x = HorizontalInput();
            // Justo después de un salto de pared el impulso manda: así el jugador
            // se despega aunque siga empujando hacia la pared.
            bool inputLocked = wallJumpLockTimer > 0f;
            if (x != 0f && !inputLocked)
                Facing = (int)x;

            var v = rb.linearVelocity;
            // En el piso, frenar y darse vuelta es más seco que arrancar.
            float target = x * stats.runSpeed;
            bool speedingUp = Mathf.Abs(target) > Mathf.Abs(v.x) && (v.x == 0f || Mathf.Sign(target) == Mathf.Sign(v.x));
            float accel = !IsGrounded ? stats.airAcceleration : speedingUp ? stats.groundAcceleration : stats.groundDeceleration;
            if (!inputLocked)
                v.x = Mathf.MoveTowards(v.x, target, accel * dt);

            // Pared: se puede agarrar empujando hacia ella mientras cae.
            int wallSide = !IsGrounded && CanWallJump ? WallSide() : 0;
            if (wallSide != 0)
            {
                wallCoyoteTimer = stats.wallCoyoteTime;
                lastWallSide = wallSide;
            }
            IsWallSliding = wallSide != 0 && !inputLocked && x == wallSide && v.y <= 0f;
            if (IsWallSliding)
            {
                Facing = -wallSide; // de espaldas a la pared: el ataque y el dash salen hacia afuera
                airDashAvailable = true;
            }

            if (coyoteTimer > 0f && input.ConsumeJump())
            {
                v.y = stats.JumpVelocity;
                coyoteTimer = 0f;
                jumpCutAvailable = true;
                IsGrounded = false;
                Jumped?.Invoke();
            }
            else if (wallCoyoteTimer > 0f && input.ConsumeJump())
            {
                v = new Vector2(-lastWallSide * stats.wallJumpHorizontalSpeed, stats.WallJumpVelocity);
                Facing = -lastWallSide;
                wallCoyoteTimer = 0f;
                wallJumpLockTimer = stats.wallJumpInputLock;
                jumpCutAvailable = true;
                IsWallSliding = false;
                WallJumped?.Invoke(lastWallSide);
            }
            if (jumpCutAvailable && !input.JumpHeld && v.y > 0f)
            {
                v.y *= stats.jumpCutMultiplier;
                jumpCutAvailable = false;
            }
            if (v.y <= 0f)
                jumpCutAvailable = false;

            ApplyGravity(ref v, dt);
            if (IsWallSliding)
                v.y = Mathf.Max(v.y, -stats.wallSlideSpeed);
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
                HealApplied?.Invoke();
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
            Damaged?.Invoke(hit.Damage);
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
            IsWallSliding = false;
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

        static readonly System.Collections.Generic.List<Collider2D> s_WallHits = new();

        /// <summary>-1 si hay pared a la izquierda, 1 a la derecha, 0 si no. Ignora plataformas de una vía.</summary>
        int WallSide()
        {
            if (TouchingWall(1)) return 1;
            if (TouchingWall(-1)) return -1;
            return 0;
        }

        bool TouchingWall(int side)
        {
            var b = bodyCollider.bounds;
            var center = new Vector2(b.center.x + side * (b.extents.x + 0.05f), b.center.y + 0.2f);
            var size = new Vector2(0.1f, b.size.y * 0.5f);
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundMask);
            s_WallHits.Clear();
            Physics2D.OverlapBox(center, size, 0f, filter, s_WallHits);
            foreach (var col in s_WallHits)
                if (!col.usedByEffector)
                    return true;
            return false;
        }

        void ApplyGravity(ref Vector2 v, float dt)
        {
            // Sube rápido, queda un instante suspendido arriba (si mantiene el salto) y cae más rápido.
            float multiplier = 1f;
            bool nearApex = !IsGrounded && Current == State.Normal && input.JumpHeld && Mathf.Abs(v.y) < stats.apexThreshold;
            if (nearApex)
                multiplier = stats.apexGravityMultiplier;
            else if (v.y < 0f)
                multiplier = stats.fallGravityMultiplier;
            v.y = Mathf.Max(v.y - stats.Gravity * multiplier * dt, -stats.maxFallSpeed);
        }

        void UpdateGrounded()
        {
            var bounds = bodyCollider.bounds;
            var center = new Vector2(bounds.center.x, bounds.min.y - 0.05f);
            var size = new Vector2(bounds.size.x * 0.9f, 0.1f);
            bool wasGrounded = IsGrounded;
            IsGrounded = rb.linearVelocity.y <= 0.01f && Physics2D.OverlapBox(center, size, 0f, groundMask) != null;
            if (IsGrounded && !wasGrounded)
                Landed?.Invoke(lastFallSpeed);
            if (IsGrounded)
            {
                coyoteTimer = stats.coyoteTime;
                airDashAvailable = true;
            }
        }

        void TickTimers(float dt)
        {
            coyoteTimer -= dt;
            wallCoyoteTimer -= dt;
            wallJumpLockTimer -= dt;
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
