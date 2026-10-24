using System.Collections.Generic;
using UnityEngine;

namespace Game01
{
    /// <summary>
    /// Muñeco de práctica: ataca en ciclo con anticipación visible para probar
    /// bloqueo, parry y dash. Cada N ataques lanza uno imparable (violeta) que
    /// solo se puede esquivar. Si lo parrean queda aturdido y recibe daño extra.
    /// No se mueve; reaparece al morir.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour, IDamageable, IParryable
    {
        enum Phase { Idle, Windup, Active, Recovery, Stunned, Dead }

        [Header("Ataque")]
        [SerializeField] float idleTime = 1.2f;
        [Tooltip("Anticipación: el aviso para el jugador.")]
        [SerializeField] float windup = 0.55f;
        [SerializeField] float active = 0.1f;
        [SerializeField] float recovery = 0.7f;
        [SerializeField] int damage = 20;
        [SerializeField] float knockback = 6f;
        [SerializeField] float attackRange = 3.5f;
        [Tooltip("Cada cuántos ataques uno es imparable. 0 = nunca.")]
        [SerializeField] int unblockableEvery = 3;
        [SerializeField] Vector2 hitboxSize = new(2f, 1.6f);
        [SerializeField] Vector2 hitboxOffset = new(1.2f, 1.2f);
        [SerializeField] LayerMask playerMask;

        [Header("Parry")]
        [SerializeField] float stunTime = 1.5f;
        [SerializeField] float stunnedDamageMultiplier = 2f;

        [Header("Placeholder visual")]
        [SerializeField] Transform visualRoot;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] float respawnDelay = 2f;

        Health health;
        Transform player;
        Phase phase;
        float timer;
        float hurtFlash;
        int facing = -1;
        int attackCount;
        bool currentUnblockable;
        readonly HashSet<IDamageable> hitThisSwing = new();

        static readonly Color ColorIdle = new(0.54f, 0.54f, 0.44f);
        static readonly Color ColorWindup = new(1f, 0.85f, 0.3f);
        static readonly Color ColorUnblockable = new(0.75f, 0.1f, 0.6f);
        static readonly Color ColorActive = new(1f, 0.27f, 0.13f);
        static readonly Color ColorStunned = new(0.55f, 0.72f, 0.83f);
        static readonly Color ColorDead = new(0.2f, 0.2f, 0.2f);

        void Awake() => health = GetComponent<Health>();

        void Start()
        {
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller != null)
                player = controller.transform;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            timer += dt;
            hurtFlash -= dt;

            switch (phase)
            {
                case Phase.Idle:
                    FacePlayer();
                    if (timer >= idleTime && PlayerInRange())
                    {
                        attackCount++;
                        currentUnblockable = unblockableEvery > 0 && attackCount % unblockableEvery == 0;
                        SetPhase(Phase.Windup);
                        CombatEvents.RaiseEnemyWindup((Vector2)transform.position + Vector2.up * 2f, currentUnblockable);
                    }
                    break;
                case Phase.Windup:
                    if (timer >= windup) { hitThisSwing.Clear(); SetPhase(Phase.Active); }
                    break;
                case Phase.Active:
                    DoHitbox();
                    if (timer >= active) SetPhase(Phase.Recovery);
                    break;
                case Phase.Recovery:
                    if (timer >= recovery) SetPhase(Phase.Idle);
                    break;
                case Phase.Stunned:
                    if (timer >= stunTime) SetPhase(Phase.Idle);
                    break;
                case Phase.Dead:
                    if (timer >= respawnDelay) { health.ResetHealth(); SetPhase(Phase.Idle); }
                    break;
            }
            UpdateVisual();
        }

        public HitResult ReceiveHit(in HitInfo hit)
        {
            if (phase == Phase.Dead)
                return HitResult.Ignored;
            float multiplier = phase == Phase.Stunned ? stunnedDamageMultiplier : 1f;
            health.TakeDamage(Mathf.RoundToInt(hit.Damage * multiplier));
            hurtFlash = 0.08f;
            if (health.IsDead)
            {
                SetPhase(Phase.Dead);
                CombatEvents.RaiseEnemyDied((Vector2)transform.position + Vector2.up);
            }
            return HitResult.Hit;
        }

        public void OnParried()
        {
            if (phase != Phase.Dead)
                SetPhase(Phase.Stunned);
        }

        void DoHitbox()
        {
            var hit = new HitInfo
            {
                Damage = damage,
                SourcePosition = transform.position,
                Knockback = facing * knockback,
                Unblockable = currentUnblockable,
                Attacker = this,
            };
            var center = (Vector2)transform.position + new Vector2(hitboxOffset.x * facing, hitboxOffset.y);
            CombatUtils.HitBox(center, hitboxSize, playerMask, hit, hitThisSwing, this);
        }

        void SetPhase(Phase next)
        {
            phase = next;
            timer = 0f;
        }

        void FacePlayer()
        {
            if (player != null)
                facing = player.position.x >= transform.position.x ? 1 : -1;
        }

        bool PlayerInRange() =>
            player != null && Mathf.Abs(player.position.x - transform.position.x) <= attackRange &&
            Mathf.Abs(player.position.y - transform.position.y) <= 2.5f;

        void UpdateVisual()
        {
            if (visualRoot != null)
                visualRoot.localScale = new Vector3(facing, 1f, 1f);
            if (bodyRenderer == null)
                return;
            Color c = phase switch
            {
                Phase.Windup => currentUnblockable ? ColorUnblockable : ColorWindup,
                Phase.Active => currentUnblockable ? ColorUnblockable : ColorActive,
                Phase.Stunned => ColorStunned,
                Phase.Dead => ColorDead,
                _ => ColorIdle,
            };
            // En la anticipación parpadea más rápido a medida que se acerca el golpe.
            if (phase == Phase.Windup && Mathf.Repeat(timer, Mathf.Lerp(0.2f, 0.06f, timer / windup)) < 0.03f)
                c = Color.white;
            if (hurtFlash > 0f)
                c = Color.white;
            bodyRenderer.color = c;
        }

        void OnDrawGizmosSelected()
        {
            int f = Application.isPlaying ? facing : -1;
            var center = (Vector2)transform.position + new Vector2(hitboxOffset.x * f, hitboxOffset.y);
            CombatUtils.DrawBox(center, hitboxSize, new Color(1f, 0.2f, 0.2f, 0.7f));
        }
    }
}
