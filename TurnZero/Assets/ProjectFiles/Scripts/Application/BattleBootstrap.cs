using UnityEngine;

namespace TurnZero
{
    public sealed class BattleBootstrap : MonoBehaviour
    {
        [SerializeField] private BattleConfiguration configuration;
        [SerializeField] private BattleSession session;
        [SerializeField] private LocalBattleWorld world;
        [SerializeField] private LocalBattleHud hud;
        [SerializeField] private LocalBattleCamera cameraRig;
        [SerializeField] private BattleInput input;
        [SerializeField] private BattleAudio audioPlayer;
        public BattleSession Session => session;
        private void Awake()
        {
            session.Initialize(configuration);
            world.Bind(session, cameraRig.Camera);
            cameraRig.Bind(session, world);
            hud.Bind(session);
            input.Bind(session, world, cameraRig);
            audioPlayer.Bind(session);
            session.Events.Refresh();
        }
    }
}
