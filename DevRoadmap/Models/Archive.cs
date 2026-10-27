using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevRoadmap.Models;

namespace DevRoadmap.Models
{
    public class Archive
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }

        // public Archive(int contentId, string? name, string? storagePath, string? contentType, long size)
        // {
        //     _contentId = contentId;
        //     _name = name;
        //     _storagePath = storagePath;
        //     _contentType = contentType;
        //     _size = size;
        // }

        // public Archive()
        // {
            
        // }
    }
}

