using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Startup.Commands
{
    public class PremiumCommands : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;

        public PremiumCommands()
        {
            _random = new Random();
        }
    }
}
