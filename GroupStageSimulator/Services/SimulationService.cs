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

            // Sort standings
            standings = standings
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.GoalDifference)
                .ThenByDescending(s => s.GoalsFor)
                .ToList();

            // Handle head-to-head tiebreaker
            for (int i = 0; i < standings.Count - 1; i++)
            {
                if (standings[i].Points == standings[i + 1].Points &&
                    standings[i].GoalDifference == standings[i + 1].GoalDifference &&
                    standings[i].GoalsFor == standings[i + 1].GoalsFor)
                {
                    var headToHeadWinner = GetHeadToHeadWinner(matches, standings[i].Team.Id, standings[i + 1].Team.Id);
                    if (headToHeadWinner == standings[i + 1].Team.Id)
                    {
                        var temp = standings[i];
                        standings[i] = standings[i + 1];
                        standings[i + 1] = temp;
                    }
                }
            }

            return standings;
        }

        private int GetHeadToHeadWinner(List<Match> matches, int team1Id, int team2Id)
        {
            var headToHeadMatches = matches.Where(m =>
                (m.HomeTeamId == team1Id && m.AwayTeamId == team2Id) ||
                (m.HomeTeamId == team2Id && m.AwayTeamId == team1Id)).ToList();

            int team1Points = 0, team2Points = 0;

            foreach (var match in headToHeadMatches)
            {
                if (match.HomeTeamId == team1Id)
                {
                    team1Points += match.HomeScore > match.AwayScore ? 3 : (match.HomeScore == match.AwayScore ? 1 : 0);
                    team2Points += match.AwayScore > match.HomeScore ? 3 : (match.AwayScore == match.HomeScore ? 1 : 0);
                }
                else
                {
                    team2Points += match.HomeScore > match.AwayScore ? 3 : (match.HomeScore == match.AwayScore ? 1 : 0);
                    team1Points += match.AwayScore > match.HomeScore ? 3 : (match.AwayScore == match.HomeScore ? 1 : 0);
                }
            }

            return team1Points > team2Points ? team1Id : team2Id;
        }

        private int CompareTeamStandings(TeamStanding a, TeamStanding b, List<Match> matches)
        {
            // Compare points
            if (a.Points != b.Points)
                return b.Points.CompareTo(a.Points);

            // Compare goal difference
            if (a.GoalDifference != b.GoalDifference)
                return b.GoalDifference.CompareTo(a.GoalDifference);

            // Compare goals for
            if (a.GoalsFor != b.GoalsFor)
                return b.GoalsFor.CompareTo(a.GoalsFor);

            // Compare goals against
            if (a.GoalsAgainst != b.GoalsAgainst)
                return a.GoalsAgainst.CompareTo(b.GoalsAgainst);

            // Head-to-head result
            var headToHeadMatches = matches.Where(m =>
                (m.HomeTeamId == a.Team.Id && m.AwayTeamId == b.Team.Id) ||
                (m.HomeTeamId == b.Team.Id && m.AwayTeamId == a.Team.Id)).ToList();

            if (headToHeadMatches.Any())
            {
                int aScore = 0, bScore = 0;
                foreach (var match in headToHeadMatches)
                {
                    if (match.HomeTeamId == a.Team.Id)
                    {
                        aScore += match.HomeScore;
                        bScore += match.AwayScore;
                    }
                    else
                    {
                        aScore += match.AwayScore;
                        bScore += match.HomeScore;
                    }
                }

                if (aScore != bScore)
                    return bScore.CompareTo(aScore);
            }

            // If everything is equal, return 0
            return 0;
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
