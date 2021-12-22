using System;

namespace Upland.Types.Types
{
    public class ErrorLogEntry
    {
        public int Id { get; set; }
        public DateTime Datetime { get; set; }
        public string Location { get; set; }
        public string Message { get; set; }
    }
}
