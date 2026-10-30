namespace DevRoadmap.Models
{
    public class AttemptAnswer
    {
        public int Id {get; set;}
        public int AssentmentAttemptId {get; set;}
        public int QuestionId { get; set; } 
        public int ? AlternativeId { get; set; }
        public string ? TextAnswer { get; set; }
    
    }
}