using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Espíritu de Azazel. Modos:
    /// - Orbit: gira alrededor de Azazel; al vencer su demora destella (aviso) y sale
    ///   disparado hacia donde está el jugador.
    /// - Fall: cae en línea recta (lluvia de almas).
    /// - Marker: solo marca en el piso dónde va a caer uno (sin colisión).
    /// Un golpe del jugador lo destruye; un parry lo devuelve contra Azazel.
    /// </summary>
    public class SpiritProjectile : MonoBehaviour, IDamageable, IParryable
    {
        enum Mode { Orbit, Telegraph, Fly, Fall, Marker, Reflected }

        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Collider2D hurtbox;
        [SerializeField] LayerMask playerMask;
        [SerializeField] LayerMask enemyMask;
        [SerializeField] LayerMask groundMask;
        [SerializeField] float radius = 0.35f;

        static readonly Color ReflectedTint = new(1f, 0.85f, 0.4f);
        static readonly List<Collider2D> s_Results = new();

        AzazelBoss owner;
        Mode mode;
        float angle;
        float timer;
        float life;
        int damage;
        Vector2 velocity;

        AzazelStats S => owner.Stats;

        public void InitOrbit(AzazelBoss boss, float startAngle, float launchDelay)
        {
            owner = boss;
            mode = Mode.Orbit;
            angle = startAngle;
            timer = launchDelay;
            damage = S.spiritDamage;
            life = launchDelay + S.spiritTelegraph + S.spiritLifetime;
            Play("float");
            transform.position = OrbitPosition();
        }

        public void InitFall(AzazelBoss boss, Vector2 from)
        {
            owner = boss;
            mode = Mode.Fall;
            transform.position = from;
            velocity = Vector2.down * S.rainFallSpeed;
            damage = S.rainDamage;
            life = 4f;
            Play("float");
        }

        public void InitMarker(AzazelBoss boss, Vector2 floor, float duration)
        {
            owner = boss;
            mode = Mode.Marker;
            transform.position = floor + Vector2.up * 0.5f; // el dibujo está en el borde inferior del sprite
            life = duration;
            if (hurtbox != null)
                hurtbox.enabled = false;
            Play("marker");
        }

        public void Vanish()
        {
            if (mode != Mode.Marker)
                LevelFX.Play(LevelFX.Kind.Spark, transform.position);
            Destroy(gameObject);
        }

        void FixedUpdate()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }
            float dt = Time.fixedDeltaTime;
            life -= dt;
            if (life <= 0f)
            {
                Vanish();
                return;
            }

            switch (mode)
            {
                case Mode.Orbit:
                    angle += S.orbitSpeed * dt;
                    transform.position = OrbitPosition();
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        mode = Mode.Telegraph;
                        timer = S.spiritTelegraph;
                        Play("flash");
                        GameFeel.Play(SoundId.SpiritLaunch, transform.position);
                    }
                    TouchPlayer();
                    break;

                case Mode.Telegraph:
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        Vector2 target = owner.Player != null ? (Vector2)owner.Player.transform.position + Vector2.up * 1.4f : (Vector2)transform.position + Vector2.down;
                        velocity = (target - (Vector2)transform.position).normalized * S.spiritSpeed;
                        mode = Mode.Fly;
                        Play("float");
                    }
                    TouchPlayer();
                    break;

                case Mode.Fly:
                case Mode.Fall:
                    transform.position += (Vector3)(velocity * dt);
                    TouchPlayer();
                    TouchGround();
                    break;

                case Mode.Reflected:
                    transform.position += (Vector3)(velocity * dt);
                    TouchOwner();
                    TouchGround();
                    break;
            }

            if (spriteRenderer != null && velocity.x != 0f)
                spriteRenderer.flipX = velocity.x < 0f;
        }

        Vector3 OrbitPosition()
        {
            float a = angle * Mathf.Deg2Rad;
            return (Vector3)owner.Center + new Vector3(Mathf.Cos(a) * S.orbitRadius, S.orbitHeight - 2.2f + Mathf.Sin(a) * S.orbitRadius * 0.6f, 0f);
        }

        void TouchPlayer()
        {
            var target = Overlap(playerMask);
            if (target == null)
                return;
            var hit = new HitInfo
            {
                Damage = damage,
                SourcePosition = transform.position,
                Knockback = Mathf.Sign(velocity.x != 0f ? velocity.x : ((Component)target).transform.position.x - transform.position.x) * 5f,
                Attacker = this,
            };
            var result = target.ReceiveHit(hit);
            if (result != HitResult.Ignored)
                CombatEvents.RaiseHit(target, result, hit);
            if (result is HitResult.Hit or HitResult.Blocked)
                Vanish();
            // Parried: OnParried ya lo dio vuelta. Ignored (dash): sigue de largo.
        }

        void TouchOwner()
        {
            s_Results.Clear();
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(enemyMask);
            Physics2D.OverlapCircle(transform.position, radius, filter, s_Results);
            foreach (var col in s_Results)
            {
                if (col.GetComponentInParent<AzazelBoss>() != owner)
                    continue;
                var hit = new HitInfo { Damage = S.reflectedSpiritDamage, SourcePosition = transform.position };
                var result = owner.ReceiveHit(hit);
                if (result != HitResult.Ignored)
                    CombatEvents.RaiseHit(owner, result, hit);
                Vanish();
                return;
            }
        }

        void TouchGround()
        {
            if (Physics2D.OverlapCircle(transform.position, radius * 0.5f, groundMask) != null)
                Vanish();
        }

        IDamageable Overlap(LayerMask mask)
        {
            s_Results.Clear();
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(mask);
            Physics2D.OverlapCircle(transform.position, radius, filter, s_Results);
            foreach (var col in s_Results)
            {
                var d = col.GetComponentInParent<IDamageable>();
                if (d != null)
                    return d;
            }
            return null;
        }

        // ---------------------------------------------------------------- IDamageable / IParryable

        public HitResult ReceiveHit(in HitInfo hit)
        {
            if (mode is Mode.Marker or Mode.Reflected)
                return HitResult.Ignored;
            Vanish();
            return HitResult.Hit;
        }

        public void OnParried()
        {
            if (owner == null)
                return;
            mode = Mode.Reflected;
            velocity = (owner.Center - (Vector2)transform.position).normalized * S.spiritSpeed * 1.5f;
            life = 3f;
            if (spriteRenderer != null)
                spriteRenderer.color = ReflectedTint;
            Play("float");
        }

        void Play(string state)
        {
            if (animator != null)
                animator.Play(state, 0, 0f);
        }
    }
}
