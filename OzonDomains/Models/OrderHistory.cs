using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzonDomains.Models
{
    public class OrderHistory
    {
        public virtual Order Order { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}