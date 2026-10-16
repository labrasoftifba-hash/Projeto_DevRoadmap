using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevRoadmap.Models;

namespace DevRoadmap.Models
{
    public class Archive
    {
        private int _archiveId {get; set;}
        private int _contentId {get; set;}
        private string? _name {get; set;}
        private string? _storagePath {get; set;}
        private string? _contentType {get; set;} 
        private long _size {get; set;}

        public Archive(int contentId, string? name, string? storagePath, string? contentType, long size)
        {
            _contentId = contentId;
            _name = name;
            _storagePath = storagePath;
            _contentType = contentType;
            _size = size;
        }
    }
}

