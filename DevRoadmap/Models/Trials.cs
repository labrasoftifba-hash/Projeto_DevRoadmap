namespace DevRoadmap.Models
{
public class Trials
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int EstimatedTime { get; set; }
        public string KnowledgeArea { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public List<Module> Modules { get; set; } = new();

        // public Trials(int id, string name, string description, string difficulty, List<string> habilities, DateTime registerDate)
        // {
        //     _id = id;
        //     _name = name;
        //     _description = description;
        //     _difficulty = difficulty;
        //     _habilities = habilities;
        //     _registerDate = _lastUpdateDate = registerDate;
        // }

        // public Trials()
        // {
            
        // }
    }
}