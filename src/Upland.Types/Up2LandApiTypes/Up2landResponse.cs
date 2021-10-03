using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Up2LandApiTypes
{
    public class Up2landResponse<T>
    {
        public string Status { get; set; }
        public T Data { get; set; }
    }
}
