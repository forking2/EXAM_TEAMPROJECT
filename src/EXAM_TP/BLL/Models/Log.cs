using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Models
{
    public class Log
    {
        public string FilePath { get; set; }
        public float Size { get; set; }
        public int ReplaceAmount { get; set; }

        public override string ToString()
        {
            return $"{FilePath}; Size: {Size}, replaced: {ReplaceAmount}";
        }
    }
}
