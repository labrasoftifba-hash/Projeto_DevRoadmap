namespace DevRoadmap.Models
{
    public class Content
    {
        private int _contentId { get; set; }
        private string _title { get; set; }
        private string _toDoList { get; set; }
        private List<Archive> _archives { get; set; }
        private List<Video> _videos { get; set; }

        public Content(string title, string toDoList, List<Archive> archives, List<Video> youtubeVideos)
        {
            _title = title;
            _toDoList = toDoList;
            _archives = archives;
            _videos = youtubeVideos;
        }
    }
}