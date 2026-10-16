namespace DevRoadmap.Models
{
    public class StudentTrial
    {
        private int _id { get; set; }
        private int _studentId { get; set; }
        private int _trialId { get; set; }
        private bool _isConcluded { get; set; }
        private int _modulesConcluded { get; set; }
        private DateTime _startDate { get; set; }
        private DateTime _endDate { get; set; }
    }
}