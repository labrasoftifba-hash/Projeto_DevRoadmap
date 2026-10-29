namespace DevRoadmap.Models
{
    public class CheckListItem
    {
        public int Id { get; set; }
        public int ChecklistId { get; set; }
        public int Order { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int VideoId = -1;
        public int ArchiveId = -1;
        public int AssetmentId = -1;
        public int ProjectId = -1;

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