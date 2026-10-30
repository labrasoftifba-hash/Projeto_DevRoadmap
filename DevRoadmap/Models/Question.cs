namespace DevRoadmap.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int AssentmentId { get; set; }
        public int Order { get; set; }
        public string Statment {get; set;} = string.Empty;
        public bool IsMultipleChoice { get; set; }                                                                                                              
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