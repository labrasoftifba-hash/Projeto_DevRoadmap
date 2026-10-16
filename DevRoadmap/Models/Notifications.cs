public class Notifications
{
    private int _id { get; set; }
    
    private int _teacherId { get; set; }
    private string _message { get; set; }
    private bool _isRead { get; set; }
    private DateTime _createdAt { get; set; }

    public Notifications(int id, int teacherId, string message, bool isRead, DateTime createdAt)
    {
        _id = id;
        _teacherId = teacherId;
        _message = message;
        _isRead = isRead;
        _createdAt = createdAt;
    }
}