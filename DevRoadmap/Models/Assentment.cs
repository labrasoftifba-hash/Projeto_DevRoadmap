namespace DevRoadmap.Models
{
    public enum AssentmentType
    {
        Quiz = 1,
        Assignment = 2
    }
    public class Assentment
    {
        public int Id {get; set;}
        public int ModuleId {get; set;}
        public string Text { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public decimal MaxGrade { get; set; }
        public decimal MinGrade { get; set; }
        public DateTime CreatedAt { get; set; }
        public AssentmentType AssentmentType { get; set; }
        
    }
}