namespace DevRoadmap.Models
{
    public class Video
    {
        private int _id { get; set; }
        private int _videoId { get; set; }
        private int _contentId { get; set; }
        private string _title { get; set; }
        //private string? _description { get; set; }
        private string _url { get; set; }

        public Video(int videoId, int contentId, string title, string url)
        {
            _videoId = videoId;
            _contentId = contentId;
            _title = title;
            _url = url;
        }
    }
}