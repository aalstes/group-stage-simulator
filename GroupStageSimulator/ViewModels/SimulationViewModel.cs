using GroupStageSimulator.Models;

namespace GroupStageSimulator.ViewModels
{
    public class SimulationViewModel
    {
        public Dictionary<int, List<Match>> MatchesByRound { get; set; } = new Dictionary<int, List<Match>>();
        public List<TeamStanding> Standings { get; set; } = new List<TeamStanding>();
    }
}
