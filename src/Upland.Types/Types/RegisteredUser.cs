using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class RegisteredUser
    {
        public int Id { get; set; }
        public decimal DiscordUserId { get; set; }
        public string DiscordUsername { get; set; }
        public string UplandUsername { get; set; }
        public int RunCount { get; set; }
        public bool Paid { get; set; }
        public long PropertyId { get; set; }
        public int Price { get; set; }
        public bool Verified { get; set; }
        public int SentUPX { get; set; }
    }
}
