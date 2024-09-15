namespace GroupStageSimulator.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public int RoundId { get; set; }  // 1-3
        public int HomeTeamId { get; set; }
        public Team HomeTeam { get; set; }
        public int AwayTeamId { get; set; }
        public Team AwayTeam { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }
}
