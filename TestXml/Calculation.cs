using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestXml
{
    public enum Operand { Unknown, add, multiply, divide, subtract }

    class Calculation
    {
        public string uid;
        public Operand operand;
        public int? mod;
    }
}
