using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupStageSimulator.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                            table: "Teams",
                            columns: new[] { "Name", "Strength" },
                            values: new object[,]
                            {
                    { "Team A", 3 },
                    { "Team B", 4 },
                    { "Team C", 2 },
                    { "Team D", 5 }
                            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                            table: "Teams",
                            keyColumn: "Name",
                            keyValues: new object[] { "Team A", "Team B", "Team C", "Team D" });
        }
    }
}
