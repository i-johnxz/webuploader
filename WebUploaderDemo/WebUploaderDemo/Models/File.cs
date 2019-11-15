using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebUploaderDemo.Models
{
    public class File
    {
        public string Md5 { get; set; }

        public List<FileSlice> Slices { get; set; }
    }
}
