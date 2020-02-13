using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb
{
    public class CodeBlock
    {
        public string Path { get; set; }
        public string FilePrefix { get; set; }
        public int BlockSize { get; set; }
        public int BlocksCount { get; set; }
        public bool Exists { get; set; }
        public bool LoadFileByFile { get; set; }
    }
}
