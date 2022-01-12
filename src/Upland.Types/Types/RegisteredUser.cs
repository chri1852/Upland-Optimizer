using System;

namespace Upland.Types.Types
{
    public class RegisteredUser
    {
        public int Id { get; set; }
        public decimal? DiscordUserId { get; set; }
        public string DiscordUsername { get; set; }
        public string UplandUsername { get; set; }
        public int RunCount { get; set; }
        public bool Paid { get; set; }
        public long PropertyId { get; set; }
        public int Price { get; set; }
        public int SendUPX { get; set; }
        public string PasswordSalt { get; set; }
        public string PasswordHash { get; set; }
        public bool DiscordVerified { get; set; }
        public bool WebVerified { get; set; }
        public string VerifyType { get; set; }
        public DateTime VerifyExpirationDateTime { get; set; }
    }
}
