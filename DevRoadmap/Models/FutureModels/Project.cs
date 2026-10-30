namespace DevRoadmap.Models
{
    public class Project
    {
        public int Id {get; set;}
        public int ModuleId {get; set;}
        public string Title {get; set;} = string.Empty;
        public string Description {get; set;} = string.Empty;
        public string EvaluetionCriteria { get; set; } = string.Empty;
        public string RequiredDeliverables { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public bool IsRequired { get; set; }
        public decimal MaxGrade { get; set; }
        public decimal RequiredGrade { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}