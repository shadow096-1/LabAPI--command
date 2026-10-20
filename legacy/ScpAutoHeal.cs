using System;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace SLTranslateCN
{
    /// <summary>
    /// SCP站立不动超过5秒后，每秒回复7点生命。
    /// </summary>
    public sealed class ScpAutoHeal : Plugin<Config>
    {
        public override string Name => "ScpAutoHeal";
        public override string Author => "Codex";
        public override Version Version => new(1, 0, 0);

        private CoroutineHandle _loopHandle;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Moving += OnMoving;
            _loopHandle = Timing.RunCoroutine(HealLoop());
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Moving -= OnMoving;
            if (_loopHandle.IsRunning)
                Timing.KillCoroutines(_loopHandle);

            _idleSeconds.Clear();
            base.OnDisabled();
        }

        private readonly System.Collections.Generic.Dictionary<int, float> _idleSeconds = new();

        private void OnMoving(MovingEventArgs ev)
        {
            if (!IsScp(ev.Player))
                return;

            float moveDistance = Vector3.Distance(ev.OldPosition, ev.NewPosition);
            if (moveDistance > 0.02f)
            {
                _idleSeconds[ev.Player.Id] = 0f;
            }
        }

        private IEnumerator<float> HealLoop()
        {
            while (true)
            {
                foreach (Player player in Player.List)
                {
                    if (!IsScp(player) || !player.IsAlive)
                        continue;

                    if (!_idleSeconds.ContainsKey(player.Id))
                        _idleSeconds[player.Id] = 0f;

                    _idleSeconds[player.Id] += 1f;

                    if (_idleSeconds[player.Id] >= 5f)
                    {
                        float newHealth = Mathf.Min(player.Health + 7f, player.MaxHealth);
                        player.Health = newHealth;
                    }
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private static bool IsScp(Player player)
        {
            return player.Role.Type.GetTeam() == Team.SCPs;
        }
    }

    public sealed class Config : Exiled.API.Interfaces.IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
    }
}

