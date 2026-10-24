using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game01
{
    /// <summary>
    /// Decide qué sonido, pausa de impacto y temblor corresponde a cada evento del
    /// combate y del jugador. Es el único lugar a tocar para cambiar esa respuesta;
    /// los valores están en <see cref="FeedbackSettings"/>.
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        PlayerController player;

        FeedbackSettings S => GameFeel.Instance.Settings;

        void OnEnable()
        {
            CombatEvents.HitLanded += OnHitLanded;
            CombatEvents.EnemyWindup += OnEnemyWindup;
            CombatEvents.EnemyDied += OnEnemyDied;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            CombatEvents.HitLanded -= OnHitLanded;
            CombatEvents.EnemyWindup -= OnEnemyWindup;
            CombatEvents.EnemyDied -= OnEnemyDied;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Bind(null);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Bind(FindAnyObjectByType<PlayerController>());

        void Bind(PlayerController next)
        {
            if (ReferenceEquals(next, player))
                return;
            if (player is not null)
            {
                player.StateChanged -= OnPlayerState;
                player.Parried -= OnParried;
                player.Blocked -= OnBlocked;
                player.Jumped -= OnJumped;
                player.Landed -= OnLanded;
                player.Damaged -= OnDamaged;
                player.HealApplied -= OnHealApplied;
            }
            player = next;
            if (player is null)
                return;
            player.StateChanged += OnPlayerState;
            player.Parried += OnParried;
            player.Blocked += OnBlocked;
            player.Jumped += OnJumped;
            player.Landed += OnLanded;
            player.Damaged += OnDamaged;
            player.HealApplied += OnHealApplied;
        }

        // ---------------------------------------------------------------- golpes del jugador y enemigos

        void OnHitLanded(IDamageable target, HitResult result, in HitInfo hit)
        {
            // Bloqueo, parry y daño al jugador se manejan con los eventos del jugador.
            if (result != HitResult.Hit || target is PlayerController)
                return;
            var position = target is Component c ? (Vector2)c.transform.position + Vector2.up : (Vector2?)null;
            GameFeel.Play(SoundId.Hit, position);
            GameFeel.HitStop(S.hitStopHit);
            GameFeel.Shake(S.shakeHit);
        }

        void OnEnemyWindup(Vector2 position, bool unblockable) =>
            GameFeel.Play(unblockable ? SoundId.EnemyWindupUnblockable : SoundId.EnemyWindup, position);

        void OnEnemyDied(Vector2 position)
        {
            GameFeel.Play(SoundId.EnemyDeath, position);
            GameFeel.HitStop(S.hitStopEnemyDeath);
            GameFeel.Shake(S.shakeEnemyDeath);
        }

        // ---------------------------------------------------------------- jugador

        void OnPlayerState(PlayerController.State state)
        {
            switch (state)
            {
                case PlayerController.State.Attack: GameFeel.Play(SoundId.Swing); break;
                case PlayerController.State.Dash: GameFeel.Play(SoundId.Dash); break;
                case PlayerController.State.Heal: GameFeel.Play(SoundId.HealStart); break;
                case PlayerController.State.Dead: GameFeel.Play(SoundId.PlayerDeath); break;
            }
        }

        void OnParried()
        {
            GameFeel.Play(SoundId.Parry);
            GameFeel.HitStop(S.hitStopParry);
            GameFeel.Shake(S.shakeParry);
        }

        void OnBlocked()
        {
            GameFeel.Play(SoundId.Block);
            GameFeel.HitStop(S.hitStopBlock);
            GameFeel.Shake(S.shakeBlock);
        }

        void OnDamaged(int amount)
        {
            GameFeel.Play(SoundId.Hurt);
            GameFeel.HitStop(S.hitStopHurt);
            GameFeel.Shake(S.shakeHurt);
        }

        void OnJumped() => GameFeel.Play(SoundId.Jump);

        void OnLanded(float fallSpeed)
        {
            if (fallSpeed >= S.landingSoundSpeed)
                GameFeel.Play(SoundId.Land);
            if (fallSpeed >= S.hardLandingSpeed)
                GameFeel.Shake(S.shakeHardLanding);
        }

        void OnHealApplied() => GameFeel.Play(SoundId.Heal);
    }
}
