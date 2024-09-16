using GroupStageSimulator.Models;
using GroupStageSimulator.Services;
using Microsoft.EntityFrameworkCore;

namespace GroupStageSimulator.Tests
{
    public class SimulationServiceTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);

            // Seed the database with teams
            context.Teams.AddRange(
                new Team { Id = 1, Name = "Team A", Strength = 3 },
                new Team { Id = 2, Name = "Team B", Strength = 4 },
                new Team { Id = 3, Name = "Team C", Strength = 2 },
                new Team { Id = 4, Name = "Team D", Strength = 5 }
            );
            context.SaveChanges();

            return context;
        }

        [Fact]
        public async Task GetStandingsAsync_ShouldUseHeadToHeadTiebreaker()
        {
            // Arrange
            using var context = CreateContext();
            var service = new SimulationService(context);
            var teamA = context.Teams.First(t => t.Id == 1);
            var teamB = context.Teams.First(t => t.Id == 2);
            var teamC = context.Teams.First(t => t.Id == 3);
            var teamD = context.Teams.First(t => t.Id == 4);

            // Create matches where each team wins once at home
            var matches = new[]
            {
                new Match { Id = 1, SimulationId = 1, HomeTeam = teamA, HomeTeamId = 1, AwayTeam = teamB, AwayTeamId = 2, HomeScore = 1, AwayScore = 0 },
                new Match { Id = 2, SimulationId = 1, HomeTeam = teamB, HomeTeamId = 2, AwayTeam = teamC, AwayTeamId = 3, HomeScore = 1, AwayScore = 0 },
                new Match { Id = 3, SimulationId = 1, HomeTeam = teamC, HomeTeamId = 3, AwayTeam = teamD, AwayTeamId = 4, HomeScore = 1, AwayScore = 0 },
                new Match { Id = 4, SimulationId = 1, HomeTeam = teamD, HomeTeamId = 4, AwayTeam = teamA, AwayTeamId = 1, HomeScore = 1, AwayScore = 0 },
            };

            context.Matches.AddRange(matches);
            await context.SaveChangesAsync();

            // Act
            var standings = await service.GetStandingsAsync(1);

            // Assert
            Assert.Equal(4, standings.Count);
            Assert.Equal("Team D", standings[0].Team.Name);
            Assert.Equal("Team A", standings[1].Team.Name);
            Assert.Equal("Team B", standings[2].Team.Name);
            Assert.Equal("Team C", standings[3].Team.Name);

            // Verify that all teams have the same points, goal difference, and goals for/against
            foreach (var standing in standings)
            {
                Assert.Equal(3, standing.Points);
                Assert.Equal(0, standing.GoalDifference);
                Assert.Equal(1, standing.GoalsFor);
                Assert.Equal(1, standing.GoalsAgainst);
            }
        }
    }
}
