using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game01
{
    /// <summary>
    /// Azazel, Rey de los Espíritus. Elige movimientos según la distancia y la fase:
    /// - Zarpazo / combo de dos zarpazos (parreables: un parry lo aturde).
    /// - Embestida imparable que cruza la arena hasta la pared (se esquiva con dash).
    /// - Espíritus que orbitan y se lanzan de a uno (se destruyen de un golpe; con
    ///   parry vuelven contra él).
    /// - Fase 2: rugido, alas, todo más rápido y lluvia de almas en columnas marcadas.
    /// Cada movimiento es una corrutina; un parry o la muerte las cortan todas.
    /// Valores en <see cref="AzazelStats"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class AzazelBoss : MonoBehaviour, IBoss, IParryable
    {
        [SerializeField] AzazelStats stats;
        [SerializeField] BoxCollider2D bodyCollider;
        [SerializeField] LayerMask groundMask;
        [SerializeField] LayerMask playerMask;
        [SerializeField] SpiritProjectile spiritPrefab;

        [Header("Visual")]
        [SerializeField] Transform visualRoot;
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer bodyRenderer;
        [Tooltip("Alas de espíritus; aparecen en la fase 2.")]
        [SerializeField] GameObject wings;

        [Header("Pruebas")]
        [Tooltip("Empieza a pelear solo al dar Play, sin arena.")]
        [SerializeField] bool fightOnStart;

        public AzazelStats Stats => stats;
        public PlayerController Player => player;
        public int Phase { get; private set; } = 1;
        /// <summary>Nombre del movimiento actual (para depurar en el Inspector).</summary>
        public string CurrentMove { get; private set; } = "";
        public float DeathDuration => stats.deathDuration;
        public Vector2 Center => (Vector2)transform.position + Vector2.up * 2.2f;

        Rigidbody2D rb;
        Health health;
        PlayerController player;
        Vector3 spawnPosition;
        int facing = -1;
        bool dead, invulnerable, stunned, parryable, phase2Pending, unblockableTint;
        float hurtFlash;
        float lastSummonTime = float.NegativeInfinity, lastRainTime = float.NegativeInfinity;
        string lastMove;
        int lastMoveRepeats;
        readonly List<SpiritProjectile> spirits = new();
        readonly HashSet<IDamageable> hitThisSwing = new();

        static readonly Color HurtTint = new(1f, 0.55f, 0.55f);
        static readonly Color UnblockableTint = new(0.95f, 0.45f, 0.85f);

        float Dt => Time.fixedDeltaTime;
        float TimeScale => Phase == 2 ? stats.phase2TimeMultiplier : 1f;
        float SpeedScale => Phase == 2 ? stats.phase2SpeedMultiplier : 1f;

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
        }

        void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (fightOnStart)
                BeginFight();
        }

        // La arena lo desactiva si el jugador muere: los espíritus sueltos se van con él.
        void OnDisable() => ClearSpirits();

        // ---------------------------------------------------------------- IBoss

        public void BeginFight()
        {
            ResetBoss();
            StartCoroutine(Brain());
        }

        public void ResetBoss()
        {
            StopAllCoroutines();
            ClearSpirits();
            dead = invulnerable = stunned = parryable = phase2Pending = unblockableTint = false;
            Phase = 1;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = stats.gravityScale;
            bodyCollider.enabled = true;
            rb.position = spawnPosition;
            transform.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            health.ResetHealth();
            if (wings != null)
                wings.SetActive(false);
            lastSummonTime = lastRainTime = float.NegativeInfinity;
            lastMove = null;
            CurrentMove = "";
            Play("idle");
        }

        // ---------------------------------------------------------------- cerebro

        IEnumerator Brain()
        {
            yield return Pause(0.6f);
            while (!dead)
            {
                if (phase2Pending)
                {
                    yield return PhaseTransition();
                    continue;
                }
                yield return ChooseAndRun();
                yield return Pause(Phase == 2 ? stats.phase2PauseBetweenMoves : stats.pauseBetweenMoves);
            }
        }

        IEnumerator ChooseAndRun()
        {
            spirits.RemoveAll(s => s == null);
            float distance = player != null ? Mathf.Abs(player.transform.position.x - transform.position.x) : 0f;
            var options = new List<(string move, float weight)>();

            if (spirits.Count == 0 && Time.time - lastSummonTime > stats.summonCooldown)
                options.Add(("summon", 2.5f));
            if (distance <= stats.clawRange + 1.5f)
            {
                options.Add(("claw", 3f));
                options.Add(("combo", Phase == 2 ? 3f : 1.5f));
            }
            else
            {
                options.Add(("approach", 2f));
                if (distance >= stats.chargeMinDistance)
                    options.Add(("charge", 2.5f));
            }
            if (Phase == 2 && Time.time - lastRainTime > stats.rainCooldown)
                options.Add(("rain", 2f));

            // No repetir el mismo movimiento más de dos veces seguidas.
            if (lastMoveRepeats >= 2 && options.Count > 1)
                options.RemoveAll(o => o.move == lastMove);

            string move = Pick(options);
            lastMoveRepeats = move == lastMove ? lastMoveRepeats + 1 : 1;
            lastMove = move;

            switch (move)
            {
                case "summon": yield return Summon(); break;
                case "charge": yield return Charge(); break;
                case "rain": yield return SoulRain(); break;
                case "combo": yield return Claws(2); break;
                case "approach": yield return Claws(Phase == 2 && Random.value < 0.5f ? 2 : 1); break;
                default: yield return Claws(1); break;
            }
        }

        static string Pick(List<(string move, float weight)> options)
        {
            float total = 0f;
            foreach (var o in options) total += o.weight;
            float r = Random.value * total;
            foreach (var o in options)
            {
                r -= o.weight;
                if (r <= 0f) return o.move;
            }
            return options[^1].move;
        }

        // ---------------------------------------------------------------- movimientos

        IEnumerator Claws(int hits)
        {
            CurrentMove = hits > 1 ? "combo" : "zarpazo";

            // Se acerca caminando si está lejos.
            Play("walk", SpeedScale);
            for (float t = 0f; DistanceToPlayer() > stats.clawRange && t < stats.approachTimeout; t += Dt)
            {
                FacePlayer();
                if (WallAhead())
                    break;
                Move(facing * stats.walkSpeed * SpeedScale);
                yield return new WaitForFixedUpdate();
            }

            float scale = TimeScale;
            for (int i = 0; i < hits; i++)
            {
                bool fast = i > 0;
                FacePlayer();
                Play(fast ? "claw_fast" : "claw", 1f / scale);
                CombatEvents.RaiseEnemyWindup(EyePosition, false);
                parryable = true;
                yield return Hold((fast ? stats.fastClawWindup : stats.clawWindup) * scale);

                hitThisSwing.Clear();
                if (!WallAhead())
                    rb.linearVelocity = new Vector2(facing * stats.clawLunge, rb.linearVelocity.y);
                for (float t = 0f; t < stats.clawActive * scale; t += Dt)
                {
                    HitBox(stats.clawDamage, stats.clawKnockback, stats.clawHitboxSize, stats.clawHitboxOffset, unblockable: false);
                    yield return new WaitForFixedUpdate();
                }
                parryable = false;

                bool last = i == hits - 1;
                yield return Hold((last ? (fast ? stats.fastClawRecovery : stats.clawRecovery) : stats.comboGap) * scale);
            }
        }

        IEnumerator Charge()
        {
            CurrentMove = "embestida";
            FacePlayer();
            float scale = TimeScale;
            Play("charge_windup", 1f / scale);
            unblockableTint = true;
            CombatEvents.RaiseEnemyWindup(EyePosition, true);
            yield return Hold(stats.chargeWindup * scale);

            Play("charge");
            hitThisSwing.Clear();
            for (float t = 0f; t < stats.chargeMaxDuration && !WallAhead(); t += Dt)
            {
                rb.linearVelocity = new Vector2(facing * stats.chargeSpeed * SpeedScale, rb.linearVelocity.y);
                HitBox(stats.chargeDamage, stats.chargeKnockback, stats.chargeHitboxSize, stats.chargeHitboxOffset, unblockable: true);
                yield return new WaitForFixedUpdate();
            }
            unblockableTint = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (WallAhead())
            {
                // Choca contra la pared.
                GameFeel.Shake(0.4f);
                GameFeel.Play(SoundId.Land, Center);
                LevelFX.Play(LevelFX.Kind.Dust, (Vector2)transform.position + new Vector2(facing * 1.2f, 0.5f), facing < 0);
            }
            Play("idle");
            yield return Hold(stats.chargeRecovery);
        }

        IEnumerator Summon()
        {
            CurrentMove = "espíritus";
            lastSummonTime = Time.time;
            FacePlayer();
            Play("summon");
            yield return Hold(stats.summonDuration * 0.5f);

            int count = Phase == 2 ? stats.spiritsPhase2 : stats.spiritsPhase1;
            float interval = stats.launchInterval * TimeScale;
            for (int i = 0; i < count; i++)
            {
                var spirit = Instantiate(spiritPrefab, Center, Quaternion.identity);
                spirit.InitOrbit(this, i * 360f / count, stats.firstLaunchDelay + i * interval);
                spirits.Add(spirit);
            }
            GameFeel.Play(SoundId.SpiritLaunch, Center);
            yield return Hold(stats.summonDuration * 0.5f);
        }

        IEnumerator SoulRain()
        {
            CurrentMove = "lluvia de almas";
            lastRainTime = Time.time;
            Play("hover");

            // Límites de la arena: paredes a los lados y techo.
            Vector2 c = Center;
            float left = RayX(c, Vector2.left, c.x - 12f);
            float right = RayX(c, Vector2.right, c.x + 12f);
            var up = Physics2D.Raycast(c, Vector2.up, 30f, groundMask);
            float ceiling = up ? up.point.y : c.y + 8f;
            float floor = spawnPosition.y;

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            Vector2 start = rb.position;
            yield return Glide(start, start + Vector2.up * stats.rainHoverHeight, 0.5f);

            for (int wave = 0; wave < stats.rainWaves; wave++)
            {
                var columns = new List<float>();
                float offset = Random.Range(0f, stats.rainColumnSpacing);
                for (float x = left + 1f + offset; x < right - 0.5f; x += stats.rainColumnSpacing)
                    columns.Add(Mathf.Floor(x) + 0.5f);

                foreach (float x in columns)
                    Track(Instantiate(spiritPrefab)).InitMarker(this, new Vector2(x, floor), stats.rainWarning);
                GameFeel.Play(SoundId.SpiritLaunch, Center);
                yield return Hold(stats.rainWarning * TimeScale);

                foreach (float x in columns)
                    Track(Instantiate(spiritPrefab)).InitFall(this, new Vector2(x, ceiling - 0.6f));
                yield return Hold(stats.rainWaveInterval * TimeScale);
            }

            yield return Glide(rb.position, new Vector2(rb.position.x, start.y), 0.4f);
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = stats.gravityScale;
        }

        IEnumerator PhaseTransition()
        {
            phase2Pending = false;
            Phase = 2;
            invulnerable = true;
            CurrentMove = "transición";
            ClearSpirits();
            Play("roar");
            GameFeel.Play(SoundId.BossRoar, Center);
            GameFeel.Shake(0.6f);
            yield return Hold(stats.roarDuration * 0.4f);
            if (wings != null)
                wings.SetActive(true);
            GameFeel.Shake(0.4f);
            yield return Hold(stats.roarDuration * 0.6f);
            invulnerable = false;
        }

        IEnumerator Stunned()
        {
            stunned = true;
            CurrentMove = "aturdido";
            Play("stunned");
            yield return Hold(stats.stunTime);
            stunned = false;
            StartCoroutine(Brain());
        }

        // ---------------------------------------------------------------- daño

        public HitResult ReceiveHit(in HitInfo hit)
        {
            if (dead || invulnerable)
                return HitResult.Ignored;

            float multiplier = stunned ? stats.stunnedDamageMultiplier : 1f;
            health.TakeDamage(Mathf.RoundToInt(hit.Damage * multiplier));
            hurtFlash = 0.08f;

            if (health.IsDead)
                Die();
            else if (Phase == 1 && health.Current <= health.Max * stats.phase2Threshold)
                phase2Pending = true;
            return HitResult.Hit;
        }

        public void OnParried()
        {
            if (dead || !parryable || invulnerable)
                return;
            parryable = false;
            StopAllCoroutines();
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            StartCoroutine(Stunned());
        }

        void Die()
        {
            dead = true;
            StopAllCoroutines();
            ClearSpirits();
            parryable = unblockableTint = false;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.position = new Vector2(rb.position.x, spawnPosition.y); // si flotaba, cae al piso
            bodyCollider.enabled = false;
            if (wings != null)
                wings.SetActive(false);
            CurrentMove = "muerto";
            Play("death");
            CombatEvents.RaiseEnemyDied(Center);
        }

        // ---------------------------------------------------------------- utilidades

        Vector2 EyePosition => (Vector2)transform.position + Vector2.up * 4f;

        void Play(string state, float speed = 1f)
        {
            if (animator == null)
                return;
            animator.speed = speed;
            animator.Play(state, 0, 0f);
        }

        void FacePlayer()
        {
            if (player != null)
                facing = player.transform.position.x >= transform.position.x ? 1 : -1;
        }

        float DistanceToPlayer() => player != null ? Mathf.Abs(player.transform.position.x - transform.position.x) : 0f;

        void Move(float targetSpeed)
        {
            var v = rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, targetSpeed, stats.acceleration * Dt);
            rb.linearVelocity = v;
        }

        /// <summary>Se queda en el lugar (frenando) durante un tiempo de juego.</summary>
        IEnumerator Hold(float duration)
        {
            for (float t = 0f; t < duration; t += Dt)
            {
                if (rb.bodyType == RigidbodyType2D.Dynamic)
                    Move(0f);
                yield return new WaitForFixedUpdate();
            }
        }

        IEnumerator Pause(float duration)
        {
            CurrentMove = "pausa";
            Play("idle");
            for (float t = 0f; t < duration; t += Dt)
            {
                FacePlayer();
                Move(0f);
                yield return new WaitForFixedUpdate();
            }
        }

        IEnumerator Glide(Vector2 from, Vector2 to, float duration)
        {
            for (float t = 0f; t < duration; t += Dt)
            {
                rb.MovePosition(Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(to);
        }

        void HitBox(int damage, float knockback, Vector2 size, Vector2 offset, bool unblockable)
        {
            var hit = new HitInfo
            {
                Damage = damage,
                SourcePosition = transform.position,
                Knockback = facing * knockback,
                Unblockable = unblockable,
                Attacker = unblockable ? null : this,
            };
            var center = (Vector2)transform.position + new Vector2(offset.x * facing, offset.y);
            CombatUtils.HitBox(center, size, playerMask, hit, hitThisSwing, this);
        }

        bool WallAhead()
        {
            var b = bodyCollider.bounds;
            float frontX = facing > 0 ? b.max.x + 0.05f : b.min.x - 0.05f;
            return Physics2D.Raycast(new Vector2(frontX, b.center.y), Vector2.right * facing, 0.3f, groundMask);
        }

        float RayX(Vector2 from, Vector2 direction, float fallback)
        {
            var hit = Physics2D.Raycast(from, direction, 40f, groundMask);
            return hit ? hit.point.x : fallback;
        }

        SpiritProjectile Track(SpiritProjectile spirit)
        {
            spirits.Add(spirit);
            return spirit;
        }

        void ClearSpirits()
        {
            foreach (var s in spirits)
                if (s != null)
                    s.Vanish();
            spirits.Clear();
        }

        void LateUpdate()
        {
            hurtFlash -= Time.deltaTime;
            if (visualRoot != null)
                visualRoot.localScale = new Vector3(facing, 1f, 1f);
            if (bodyRenderer != null)
                bodyRenderer.color = hurtFlash > 0f ? HurtTint : unblockableTint ? UnblockableTint : Color.white;
        }

        void OnDrawGizmosSelected()
        {
            if (stats == null)
                return;
            int f = Application.isPlaying ? facing : 1;
            Vector2 p = transform.position;
            CombatUtils.DrawBox(p + new Vector2(stats.clawHitboxOffset.x * f, stats.clawHitboxOffset.y), stats.clawHitboxSize, new Color(1f, 0.2f, 0.2f, 0.8f));
            CombatUtils.DrawBox(p + new Vector2(stats.chargeHitboxOffset.x * f, stats.chargeHitboxOffset.y), stats.chargeHitboxSize, new Color(0.9f, 0.3f, 0.9f, 0.8f));
        }
    }
}
