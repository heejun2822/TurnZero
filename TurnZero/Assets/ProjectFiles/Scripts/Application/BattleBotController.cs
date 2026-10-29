namespace TurnZero
{
    // One controller belongs to one match; no deferred requests survive a rematch.
    public sealed class BattleBotController
    {
        public int Player { get; }
        public BattleBotPlan LastPlan { get; private set; }
        public int PlanningCount { get; private set; }

        private readonly LocalBattleMatch match;
        private readonly BattleBotPlanner planner = new BattleBotPlanner();
        private int plannedTurn;

        public BattleBotController(LocalBattleMatch match, int player)
        {
            this.match = match;
            Player = player;
        }

        public bool Deploy(double now, out string error)
        {
            return match.AutoDeploy(Player, now, out error);
        }

        public bool Prepare(double now)
        {
            if (match.Phase != BattlePhase.Planning || plannedTurn == match.Turn || now >= match.Deadline)
            {
                return false;
            }

            BattleBotObservation observation = BattleBotObservation.Capture(match, Player, now);

            if (observation.Ready)
            {
                return false;
            }

            plannedTurn = match.Turn;
            PlanningCount++;
            LastPlan = planner.Plan(observation, match.Rules.Seed);

            foreach (BattleBotDecision decision in LastPlan.Decisions)
            {
                if (match.Phase != BattlePhase.Planning || match.Turn != plannedTurn)
                {
                    break;
                }

                decision.Accepted = match.Queue(Player, plannedTurn, decision.Order, out string error, now);
                decision.Result = decision.Accepted ? decision.Score.ToString() : error;
            }

            if (match.Phase == BattlePhase.Planning && match.Turn == plannedTurn && !match.View(Player, now).Ready)
            {
                match.Ready(Player, now);
            }

            return true;
        }
    }
}
