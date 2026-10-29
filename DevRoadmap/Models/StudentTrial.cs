

namespace DevRoadmap.Models
{
    public class StudentTrial
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Usuario Usuario { get; set; } = default!;
        public int TrialId { get; set; }
        public Trials Trial { get; set; } = default!;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConcludedAt { get; set; } // null = em andamento

        public ICollection<ModuleProgress> Progress { get; set; } = new List<ModuleProgress>();

        public StudentTrial()
        {
        }

        public StudentTrial(int usuarioId, int trialId)
        {
            StudentId = usuarioId;
            TrialId = trialId;
            StartedAt = DateTime.UtcNow;
        }
    }
}