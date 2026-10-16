namespace DevRoadmap.Models
{
    public class Module
    {
        private int _id { get; set; }
        private int _trialId { get; set; }
        private string _title { get; set; }
        private string _description { get; set; }
        private string _tags { get; set; }
        private Content? _content { get; set; }
         
        public Module(int id, string title, string description, string tags)
        {
            _id = id;
            _title = title;
            _description = description;
            _tags = tags;
        }

        public void setContent(Content content)
        {
            _content = content;
        }
    }
}