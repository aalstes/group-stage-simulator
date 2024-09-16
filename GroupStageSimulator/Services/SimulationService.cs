using GroupStageSimulator.Models;
using Microsoft.EntityFrameworkCore;

namespace GroupStageSimulator.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly ApplicationDbContext _context;
        private readonly Random _random = new Random();

        public SimulationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Match>> SimulateGroupStageAsync()
        {
            var teams = await _context.Teams.ToListAsync();
            var matches = new List<Match>();
            int simulationId = await GetNextSimulationIdAsync();
            int totalRounds = 3;

            for (int round = 1; round <= totalRounds; round++)
            {
                switch (round)
                {
                    case 1:
                        matches.Add(SimulateMatch(teams[0], teams[3], round, simulationId));
                        matches.Add(SimulateMatch(teams[2], teams[1], round, simulationId));
                        break;
                    case 2:
                        matches.Add(SimulateMatch(teams[1], teams[0], round, simulationId));
                        matches.Add(SimulateMatch(teams[3], teams[2], round, simulationId));
                        break;
                    case 3:
                        matches.Add(SimulateMatch(teams[3], teams[1], round, simulationId));
                        matches.Add(SimulateMatch(teams[2], teams[0], round, simulationId));
                        break;
                }
            }

            await _context.Matches.AddRangeAsync(matches);
            await _context.SaveChangesAsync();

            return matches;
        }

        public Match SimulateMatch(Team homeTeam, Team awayTeam, int round, int simulationId)
        {
            int homeScore = SimulateScore(homeTeam.Strength);
            int awayScore = SimulateScore(awayTeam.Strength);

            return new Match
            {
                HomeTeam = homeTeam,
                HomeTeamId = homeTeam.Id,
                AwayTeam = awayTeam,
                AwayTeamId = awayTeam.Id,
                HomeScore = homeScore,
                AwayScore = awayScore,
                Round = round,
                SimulationId = simulationId
            };
        }

        private int SimulateScore(int teamStrength)
        {
            return new Random().Next(0, teamStrength + 1);
        }

        private async Task<int> GetNextSimulationIdAsync()
        {
            return (await _context.Matches.MaxAsync(m => (int?)m.SimulationId) ?? 0) + 1;
        }

        public async Task<List<TeamStanding>> GetStandingsAsync(int simulationId)
        {
            var matches = await _context.Matches
                .Where(m => m.SimulationId == simulationId)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .ToListAsync();

            var standings = CalculateStandings(matches);

            standings.Sort((a, b) =>
            {
                // Primary sort by points (descending)
                int comparison = b.Points.CompareTo(a.Points);
                if (comparison != 0) return comparison;

                // Secondary sort by goal difference (descending)
                comparison = b.GoalDifference.CompareTo(a.GoalDifference);
                if (comparison != 0) return comparison;

                // Tertiary sort by goals for (descending)
                comparison = b.GoalsFor.CompareTo(a.GoalsFor);
                if (comparison != 0) return comparison;

                // Quaternary sort by goals against (ascending)
                comparison = a.GoalsAgainst.CompareTo(b.GoalsAgainst);
                if (comparison != 0) return comparison;

                // If still tied, use head-to-head result
                var headToHeadMatch = matches.FirstOrDefault(m =>
                    (m.HomeTeam.Id == a.Team.Id && m.AwayTeam.Id == b.Team.Id) ||
                    (m.HomeTeam.Id == b.Team.Id && m.AwayTeam.Id == a.Team.Id));

                if (headToHeadMatch != null)
                {
                    if (headToHeadMatch.HomeTeam.Id == a.Team.Id)
                    {
                        return headToHeadMatch.AwayScore.CompareTo(headToHeadMatch.HomeScore); // descending order
                    }
                    else
                    {
                        return headToHeadMatch.HomeScore.CompareTo(headToHeadMatch.AwayScore); // descending order
                    }
                }

                // If still tied
                return 0;
            });

            return standings;
        }

        private List<TeamStanding> CalculateStandings(List<Match> matches)
        {
            var standings = new Dictionary<int, TeamStanding>();

            foreach (var match in matches)
            {
                UpdateTeamStanding(standings, match.HomeTeam, match.HomeScore, match.AwayScore);
                UpdateTeamStanding(standings, match.AwayTeam, match.AwayScore, match.HomeScore);
            }

            return standings.Values.ToList();
        }

        private void UpdateTeamStanding(Dictionary<int, TeamStanding> standings, Team team, int goalsFor, int goalsAgainst)
        {
            if (!standings.TryGetValue(team.Id, out var standing))
            {
                standing = new TeamStanding { Team = team };
                standings[team.Id] = standing;
            }

            standing.MatchesPlayed++;
            standing.GoalsFor += goalsFor;
            standing.GoalsAgainst += goalsAgainst;

            if (goalsFor > goalsAgainst)
            {
                standing.Wins++;
                standing.Points += 3;
            }
            else if (goalsFor == goalsAgainst)
            {
                standing.Draws++;
                standing.Points += 1;
            }
            else
            {
                standing.Losses++;
            }
        }
    }
}
