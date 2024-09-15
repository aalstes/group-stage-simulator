namespace GroupStageSimulator.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public int Round { get; set; }
        required public int HomeTeamId { get; set; }
        required public Team HomeTeam { get; set; }
        required public int AwayTeamId { get; set; }
        required public Team AwayTeam { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }
}
