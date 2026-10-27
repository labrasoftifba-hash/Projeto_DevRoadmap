namespace DevRoadmap.Models
{
    public class Video
    {
        public int Id { get; set; }
        public int ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string YoutubeId { get; set; } = string.Empty;   // era _videoId (int)

        // public Video(int videoId, int contentId, string title, string url)
        // {
        //     _videoId = videoId;
        //     _contentId = contentId;
        //     _title = title;
        //     _url = url;
        // }

        // public Video()
        // {
            
        // }
    }
}