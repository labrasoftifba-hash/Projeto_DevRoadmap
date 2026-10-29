namespace DevRoadmap.Models
{
    public class Module
    {
        public int Id { get; set; }
        public int TrialId { get; set; }
        public Trials Trial { get; set; } = null!;
        public int Order { get; set; }            // antigo _moduleIdentifier
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        

        //public Content? Content { get; set; }
         
        // public Module(int id, string title, string description, string tags)
        // {
        //     _id = id;
        //     _title = title;
        //     _description = description;
        //     _tags = tags;
        // }

        // public Module()
        // {
            
        // }

        // public void setContent(Content content)
        // {
        //     _content = content;
        // }
    }
}