using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebUploaderDemo.Models
{
    public class FileSlice
    {
        public int Sequence { get; set; }

        public string Md5 { get; set; }

        public string Path { get; set; }


    }
}
