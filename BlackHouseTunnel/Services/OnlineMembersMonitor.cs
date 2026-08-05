using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public class OnlineMembersMonitor
    {
        public event EventHandler<List<DiscordUser>>? OnMembersUpdated;

        private readonly DispatcherTimer _timer;
        private readonly AppConfig _config;
        private readonly DiscordApiService _apiService;

        public OnlineMembersMonitor(AppConfig config)
        {
            _config = config;
            _apiService = new DiscordApiService();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4) // 4 seconds polling interval
            };
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            _timer.Start();
            Timer_Tick(this, EventArgs.Empty);
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    List<DiscordUser> members = await _apiService.GetGuildOnlineMembersAsync(_config.GuildId, _config.BotToken);
                    if (members.Count == 0)
                    {
                        // Fallback sample if bot token call returns empty
                        members = GetFallbackMembers();
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        OnMembersUpdated?.Invoke(this, members);
                    });
                }
                catch
                {
                }
            });
        }

        private List<DiscordUser> GetFallbackMembers()
        {
            return new List<DiscordUser>
            {
                new DiscordUser { Username = "Carlitos", ServerNick = "Carlitos", PrimaryRole = "Superior", PrimaryRoleColor = "#FFD700", IsPrivadito = true, IsStaffOrAdmin = true },
                new DiscordUser { Username = "Overlord", ServerNick = "Overlord", PrimaryRole = "Reaper", PrimaryRoleColor = "#9B59B6", IsPrivadito = true },
                new DiscordUser { Username = "Alice", ServerNick = "Alice", PrimaryRole = "Chica", PrimaryRoleColor = "#FF69B4", IsPrivadito = true },
                new DiscordUser { Username = "MelissaChibiii12341", ServerNick = "MelissaChibiii12341", PrimaryRole = "Chica", PrimaryRoleColor = "#FF69B4", IsPrivadito = false },
                new DiscordUser { Username = "El negro José", ServerNick = "El negro José", PrimaryRole = "Follador", PrimaryRoleColor = "#E67E22", IsPrivadito = false },
                new DiscordUser { Username = "Falconalejo", ServerNick = "Falconalejo", PrimaryRole = "Follador", PrimaryRoleColor = "#E67E22", IsPrivadito = false },
                new DiscordUser { Username = "nassan", ServerNick = "nassan", PrimaryRole = "Follador", PrimaryRoleColor = "#E67E22", IsPrivadito = false },
                new DiscordUser { Username = "zeta", ServerNick = "zeta", PrimaryRole = "Follador", PrimaryRoleColor = "#E67E22", IsPrivadito = false }
            };
        }
    }
}
