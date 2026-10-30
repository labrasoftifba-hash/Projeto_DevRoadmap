namespace DevRoadmap.Models
{
    public class Alternative
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Statment {get; set;} = string.Empty;
        public bool IsCorrectAnswer { get; set; }
        
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