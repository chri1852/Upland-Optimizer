using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class NFTHistory
    {
        public int Id { get; set; }
        public int DGoodId { get; set; }
        public string Owner { get; set; }
        public DateTime ObtainedOn { get; set; }
        public DateTime? DisposedOn { get; set; }
    }
}
