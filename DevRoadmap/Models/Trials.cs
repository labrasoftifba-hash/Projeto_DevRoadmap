namespace DevRoadmap.Models
{
public class Trials
    {
        private int _id { get; set; }
        private string _name { get; set; }
        private string _description { get; set; }
        private string _difficulty{ get; set; }
        private bool _isActive { get; set; }
        private List<string> _habilities { get; set; }
        private DateTime _registerDate { get; set; }
        private DateTime _lastUpdateDate { get; set; }

        public Trials(int id, string name, string description, string difficulty, List<string> habilities, DateTime registerDate)
        {
            _id = id;
            _name = name;
            _description = description;
            _difficulty = difficulty;
            _habilities = habilities;
            _registerDate = _lastUpdateDate = registerDate;
        }
    }
}