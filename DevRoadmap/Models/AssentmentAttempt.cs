namespace DevRoadmap.Models
{
    public enum AssentmentAttemptStatus
    {
        NotSubmitted,
        Submitted,
        Corrected
    }
    public class AssentmentAttempt
    {
        public int Id {get; set;}
        public int StudentTrialId {get; set;}
        public int AssentmentId { get; set; } 
        public int CorrectedById { get; set; }
        public AssentmentAttemptStatus Status { get; set; }
        public bool IsCorrected { get; set; }
        public decimal Grade { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime CorrectedAt { get; set; }
    }
}