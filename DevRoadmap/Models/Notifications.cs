

namespace DevRoadmap.Models
{
        public class Notifications
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // public Notifications(int id, int teacherId, string message, bool isRead, DateTime createdAt)
        // {
        //     _id = id;
        //     _teacherId = teacherId;
        //     _message = message;
        //     _isRead = isRead;
        //     _createdAt = createdAt;
        // }

        // public Notifications(){

        // }
    }
}
