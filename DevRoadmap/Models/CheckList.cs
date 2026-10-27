namespace DevRoadmap.Models
{
    public class CheckList
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string ToDoList { get; set; } = string.Empty;

        public List<Archive> Archives { get; set; } = new();
        public List<Video> Videos { get; set; } = new();

        // public Content(string title, string toDoList, List<Archive> archives, List<Video> youtubeVideos)
        // {
        //     _title = title;
        //     _toDoList = toDoList;
        //     _archives = archives;
        //     _videos = youtubeVideos;
        // }

        // public Content()
        // {
        // }
    }
}