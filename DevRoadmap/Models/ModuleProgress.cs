namespace DevRoadmap.Models
{
    public class ModuleProgress
    {
        public int StudentTrialId { get; set; }
        public StudentTrial StudentTrial { get; set; } = null!;
        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;
        public DateTime ConcludedAt { get; set; } = DateTime.UtcNow;
    }
}