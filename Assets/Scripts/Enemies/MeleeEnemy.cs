using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Enemigo cuerpo a cuerpo genérico: patrulla, detecta al jugador (con línea
    /// de visión), lo persigue sin caerse de las plataformas y ataca con una
    /// anticipación clara. Un parry lo deja aturdido y recibe daño extra.
    /// Los golpes no cortan la anticipación salvo que <see cref="EnemyStats.windupInterruptible"/>
    /// esté activo. Reaparece cuando el jugador reaparece.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class MeleeEnemy : MonoBehaviour, IBoss, IParryable
    {
        public enum State { Patrol, Chase, Windup, Attack, Recovery, Stunned, Hurt, Dead }

        [SerializeField] EnemyStats stats;
        [SerializeField] BoxCollider2D bodyCollider;
        [SerializeField] LayerMask groundMask;
        [SerializeField] LayerMask playerMask;

        [Header("Visual")]
        [SerializeField] Transform visualRoot;
        [SerializeField] SpriteRenderer bodyRenderer;

        public State Current { get; private set; }
        public int Facing { get; private set; } = 1;
        public EnemyStats Stats => stats;
        public bool CurrentAttackUnblockable { get; private set; }
        public event Action<State> StateChanged;

        Rigidbody2D rb;
        Health health;
        PlayerController player;
        float stateTimer;
        float cooldownTimer;
        float patrolPauseTimer;
        float hurtFlash;
        int patrolDirection = 1;
        int attackCount;
        Vector2 home;
        Vector3 spawnPosition;
        int spawnFacing;
        readonly HashSet<IDamageable> hitThisSwing = new();

        static readonly Color HurtTint = new(1f, 0.55f, 0.55f);
        static readonly Color UnblockableTint = new(0.95f, 0.45f, 0.85f);

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            if (bodyCollider == null)
                bodyCollider = GetComponent<BoxCollider2D>();

            rb.gravityScale = stats.gravityScale;
            rb.freezeRotation = true;
            health.SetMax(stats.maxHealth);

            spawnPosition = transform.position;
            home = spawnPosition;
            spawnFacing = Facing;
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

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            stateTimer += dt;
            cooldownTimer -= dt;
            hurtFlash -= dt;

            switch (Current)
            {
                case State.Patrol: UpdatePatrol(dt); break;
                case State.Chase: UpdateChase(dt); break;
                case State.Windup: UpdateWindup(dt); break;
                case State.Attack: UpdateAttack(dt); break;
                case State.Recovery:
                    if (WaitFor(dt, stats.attackRecovery))
                    {
                        cooldownTimer = stats.attackCooldown;
                        SetState(State.Chase);
                    }
                    break;
                case State.Stunned:
                    if (WaitFor(dt, stats.stunTime)) SetState(State.Chase);
                    break;
                case State.Hurt:
                    if (WaitFor(dt, stats.hurtStagger)) SetState(State.Chase);
                    break;
                case State.Dead: rb.linearVelocity = Vector2.zero; break;
            }
            UpdateVisual();
        }

        // ---------------------------------------------------------------- estados

        void UpdatePatrol(float dt)
        {
            if (CanSeePlayer(stats.detectionRange, requireFacing: true))
            {
                SetState(State.Chase);
                return;
            }

            if (patrolPauseTimer > 0f)
            {
                patrolPauseTimer -= dt;
                MoveTowards(0f, dt);
                return;
            }

            Facing = patrolDirection;
            float offset = transform.position.x - home.x;
            bool tooFar = offset * patrolDirection >= stats.patrolDistance;
            if (tooFar || BlockedAhead(patrolDirection))
            {
                patrolDirection = -patrolDirection;
                patrolPauseTimer = stats.patrolPause;
                MoveTowards(0f, dt);
                return;
            }
            MoveTowards(patrolDirection * stats.patrolSpeed, dt);
        }

        void UpdateChase(float dt)
        {
            if (!CanSeePlayer(stats.loseRange, requireFacing: false))
            {
                home = transform.position; // patrulla donde perdió el rastro
                patrolPauseTimer = stats.patrolPause;
                SetState(State.Patrol);
                return;
            }

            Vector2 toPlayer = player.transform.position - transform.position;
            Facing = toPlayer.x >= 0f ? 1 : -1;

            if (Mathf.Abs(toPlayer.x) <= stats.attackRange && cooldownTimer <= 0f)
            {
                StartWindup();
                return;
            }

            bool closeEnough = Mathf.Abs(toPlayer.x) <= stats.attackRange * 0.8f;
            MoveTowards(closeEnough || BlockedAhead(Facing) ? 0f : Facing * stats.chaseSpeed, dt);
        }

        void StartWindup()
        {
            attackCount++;
            CurrentAttackUnblockable = stats.unblockableEvery > 0 && attackCount % stats.unblockableEvery == 0;
            SetState(State.Windup);
            CombatEvents.RaiseEnemyWindup((Vector2)transform.position + Vector2.up * stats.eyeHeight, CurrentAttackUnblockable);
        }

        void UpdateWindup(float dt)
        {
            MoveTowards(0f, dt);
            if (stateTimer < stats.attackWindup)
                return;
            hitThisSwing.Clear();
            if (!BlockedAhead(Facing))
                rb.linearVelocity = new Vector2(Facing * stats.attackLunge, rb.linearVelocity.y);
            SetState(State.Attack);
        }

        void UpdateAttack(float dt)
        {
            MoveTowards(0f, dt);
            if (BlockedAhead(Facing))
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            DoHitbox();
            if (stateTimer >= stats.attackActive)
                SetState(State.Recovery);
        }

        /// <summary>Frena y devuelve true cuando el estado actual cumplió su duración.</summary>
        bool WaitFor(float dt, float duration)
        {
            MoveTowards(0f, dt);
            return stateTimer >= duration;
        }

        void DoHitbox()
        {
            var hit = new HitInfo
            {
                Damage = stats.attackDamage,
                SourcePosition = transform.position,
                Knockback = Facing * stats.attackKnockback,
                Unblockable = CurrentAttackUnblockable,
                Attacker = this,
            };
            CombatUtils.HitBox(HitboxCenter(Facing), stats.hitboxSize, playerMask, hit, hitThisSwing, this);
        }

        // ---------------------------------------------------------------- daño

        public HitResult ReceiveHit(in HitInfo hit)
        {
            if (Current == State.Dead)
                return HitResult.Ignored;

            float multiplier = Current == State.Stunned ? stats.stunnedDamageMultiplier : 1f;
            health.TakeDamage(Mathf.RoundToInt(hit.Damage * multiplier));
            hurtFlash = 0.1f;

            if (health.IsDead)
            {
                Die();
                return HitResult.Hit;
            }

            bool armored = Current == State.Attack || Current == State.Stunned ||
                           (Current == State.Windup && !stats.windupInterruptible);
            if (!armored)
            {
                Facing = hit.SourcePosition.x >= transform.position.x ? 1 : -1; // se da vuelta hacia quien le pegó
                rb.linearVelocity = new Vector2(hit.Knockback * stats.hurtKnockbackMultiplier, rb.linearVelocity.y);
                SetState(State.Hurt);
            }
            return HitResult.Hit;
        }

        public void OnParried()
        {
            if (Current != State.Dead)
                SetState(State.Stunned);
        }

        void Die()
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            bodyCollider.enabled = false;
            SetState(State.Dead);
            CombatEvents.RaiseEnemyDied((Vector2)transform.position + Vector2.up);
        }

        // IBoss: un MeleeEnemy con stats altos también puede ser jefe de una arena.
        public void BeginFight() => ResetEnemy();
        public void ResetBoss() => ResetEnemy();
        public float DeathDuration => stats.deathDuration;

        /// <summary>Vuelve a su lugar con la vida llena (al reaparecer el jugador).</summary>
        public void ResetEnemy()
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = stats.gravityScale;
            bodyCollider.enabled = true;
            rb.position = spawnPosition;
            transform.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            health.ResetHealth();
            home = spawnPosition;
            Facing = patrolDirection = spawnFacing;
            cooldownTimer = 0f;
            attackCount = 0;
            SetState(State.Patrol);
        }

        // ---------------------------------------------------------------- utilidades

        void SetState(State next)
        {
            Current = next;
            stateTimer = 0f;
            StateChanged?.Invoke(next);
        }

        void MoveTowards(float targetSpeed, float dt)
        {
            var v = rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, targetSpeed, stats.acceleration * dt);
            rb.linearVelocity = v;
        }

        bool CanSeePlayer(float range, bool requireFacing)
        {
            if (player == null || player.Current == PlayerController.State.Dead)
                return false;
            Vector2 toPlayer = player.transform.position - transform.position;
            if (Mathf.Abs(toPlayer.x) > range || Mathf.Abs(toPlayer.y) > stats.verticalTolerance)
                return false;
            if (requireFacing && Mathf.Sign(toPlayer.x) != Facing && Mathf.Abs(toPlayer.x) > stats.backDetectionRange)
                return false;
            if (!stats.requireLineOfSight)
                return true;
            Vector2 eye = (Vector2)transform.position + Vector2.up * stats.eyeHeight;
            Vector2 target = (Vector2)player.transform.position + Vector2.up * 1.4f;
            return !Physics2D.Linecast(eye, target, groundMask);
        }

        /// <summary>Hay una pared adelante o se termina el piso.</summary>
        bool BlockedAhead(int direction)
        {
            var b = bodyCollider.bounds;
            float frontX = direction > 0 ? b.max.x + 0.05f : b.min.x - 0.05f;
            bool wall = Physics2D.Raycast(new Vector2(frontX, b.center.y), Vector2.right * direction, 0.2f, groundMask);
            bool ground = Physics2D.Raycast(new Vector2(frontX + direction * 0.15f, b.min.y + 0.2f), Vector2.down, 0.6f, groundMask);
            return wall || !ground;
        }

        Vector2 HitboxCenter(int facing) =>
            (Vector2)transform.position + new Vector2(stats.hitboxOffset.x * facing, stats.hitboxOffset.y);

        void UpdateVisual()
        {
            if (visualRoot != null)
                visualRoot.localScale = new Vector3(Facing, 1f, 1f);
            if (bodyRenderer == null)
                return;
            Color c = Color.white;
            if (CurrentAttackUnblockable && Current == State.Windup)
                c = UnblockableTint;
            if (hurtFlash > 0f)
                c = HurtTint;
            bodyRenderer.color = c;
        }

        void OnDrawGizmosSelected()
        {
            if (stats == null)
                return;
            int facing = Application.isPlaying ? Facing : 1;
            CombatUtils.DrawBox(HitboxCenter(facing), stats.hitboxSize, new Color(1f, 0.2f, 0.2f, 0.7f));
            Vector3 p = transform.position + Vector3.up * stats.eyeHeight;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
            Gizmos.DrawLine(p, p + Vector3.right * facing * stats.detectionRange);
            Vector3 h = Application.isPlaying ? (Vector3)home : transform.position;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawLine(h + Vector3.left * stats.patrolDistance, h + Vector3.right * stats.patrolDistance);
        }
    }
}
