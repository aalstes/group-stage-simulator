using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroupStageSimulator.Models;
using GroupStageSimulator.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

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
        public async Task SimulateGroupStageAsync_ShouldCreateCorrectNumberOfMatches()
        {
            // Arrange
            using var context = CreateContext();
            var service = new SimulationService(context);

            // Act
            var matches = await service.SimulateGroupStageAsync();

            // Assert
            Assert.Equal(6, matches.Count);
        }

        [Fact]
        public async Task GetStandingsAsync_ShouldReturnCorrectStandings()
        {
            // Arrange
            using var context = CreateContext();
            var service = new SimulationService(context);

            // Simulate some matches
            context.Matches.AddRange(
                new Match { SimulationId = 1, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 2, AwayScore = 1, Round = 1, HomeTeam = context.Teams.Find(1), AwayTeam = context.Teams.Find(2) },
                new Match { SimulationId = 1, HomeTeamId = 3, AwayTeamId = 4, HomeScore = 0, AwayScore = 3, Round = 1, HomeTeam = context.Teams.Find(3), AwayTeam = context.Teams.Find(4) },
                new Match { SimulationId = 1, HomeTeamId = 1, AwayTeamId = 3, HomeScore = 1, AwayScore = 1, Round = 2, HomeTeam = context.Teams.Find(1), AwayTeam = context.Teams.Find(3) },
                new Match { SimulationId = 1, HomeTeamId = 2, AwayTeamId = 4, HomeScore = 2, AwayScore = 2, Round = 2, HomeTeam = context.Teams.Find(2), AwayTeam = context.Teams.Find(4) },
                new Match { SimulationId = 1, HomeTeamId = 1, AwayTeamId = 4, HomeScore = 0, AwayScore = 2, Round = 3, HomeTeam = context.Teams.Find(1), AwayTeam = context.Teams.Find(4) },
                new Match { SimulationId = 1, HomeTeamId = 2, AwayTeamId = 3, HomeScore = 3, AwayScore = 0, Round = 3, HomeTeam = context.Teams.Find(2), AwayTeam = context.Teams.Find(3) }
            );
            await context.SaveChangesAsync();

            // Act
            var standings = await service.GetStandingsAsync(1);

            // Assert
            Assert.Equal(4, standings.Count);
            Assert.Equal(4, standings[0].Team.Id); // Team D should be first
            Assert.Equal(7, standings[0].Points);
            Assert.Equal(2, standings[1].Team.Id); // Team B should be second
            Assert.Equal(4, standings[1].Points);
            Assert.Equal(1, standings[2].Team.Id); // Team A should be third
            Assert.Equal(4, standings[2].Points);
            Assert.Equal(3, standings[3].Team.Id); // Team C should be last
            Assert.Equal(1, standings[3].Points);
        }

        [Fact]
        public async Task GetStandingsAsync_ShouldHandleHeadToHeadTiebreaker()
        {
            // Arrange
            using var context = CreateContext();
            var service = new SimulationService(context);

            // Simulate matches where Team A and Team B are tied on all criteria except head-to-head
            context.Matches.AddRange(
                new Match { SimulationId = 1, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 2, AwayScore = 1, Round = 1, HomeTeam = context.Teams.Find(1), AwayTeam = context.Teams.Find(2) },
                new Match { SimulationId = 1, HomeTeamId = 1, AwayTeamId = 3, HomeScore = 1, AwayScore = 0, Round = 2, HomeTeam = context.Teams.Find(1), AwayTeam = context.Teams.Find(3) },
                new Match { SimulationId = 1, HomeTeamId = 1, AwayTeamId = 4, HomeScore = 0, AwayScore = 2, Round = 3, HomeTeam = context.Teams.Find(1), AwayTeam = context.Teams.Find(4) },
                new Match { SimulationId = 1, HomeTeamId = 2, AwayTeamId = 3, HomeScore = 2, AwayScore = 1, Round = 2, HomeTeam = context.Teams.Find(2), AwayTeam = context.Teams.Find(3) },
                new Match { SimulationId = 1, HomeTeamId = 2, AwayTeamId = 4, HomeScore = 0, AwayScore = 2, Round = 3, HomeTeam = context.Teams.Find(2), AwayTeam = context.Teams.Find(4) }
            );
            await context.SaveChangesAsync();

            // Act
            var standings = await service.GetStandingsAsync(1);

            // Assert
            Assert.Equal(4, standings.Count);
            Assert.Equal(1, standings[0].Team.Id); // Team A should be first due to head-to-head
            Assert.Equal(2, standings[1].Team.Id); // Team B should be second
            Assert.Equal(6, standings[0].Points);
            Assert.Equal(6, standings[1].Points);
            Assert.Equal(3, standings[0].GoalsFor);
            Assert.Equal(3, standings[1].GoalsFor);
            Assert.Equal(3, standings[0].GoalsAgainst);
            Assert.Equal(3, standings[1].GoalsAgainst);
        }
    }
}
