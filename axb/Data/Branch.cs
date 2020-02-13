using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Data
{
    public class Branch
    {
        public int RecId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int LastDeployedChangeset { get; set; }
        public int BuildStartChangeset { get; set; }
        public int BuildEndChangeset { get; set; }
    }
}
