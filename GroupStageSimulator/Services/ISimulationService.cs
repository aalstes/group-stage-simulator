using GroupStageSimulator.Models;

namespace GroupStageSimulator.Services
{
    public interface ISimulationService
    {
        Task<List<Match>> SimulateGroupStageAsync();
        Match SimulateMatch(Team homeTeam, Team awayTeam, int roundId, int simulationId);
        Task<List<TeamStanding>> GetStandingsAsync(int simulationId);
    }
}
