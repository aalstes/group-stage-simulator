namespace GroupStageSimulator.Models
{
    public class Team
    {
        public int Id { get; set; }
        required public string Name { get; set; }
        required public int Strength { get; set; }
    }
}
