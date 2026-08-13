using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly DiscordUser _currentUser;
        private const int MaxDisplayMembersLimit = 26; // 26 members max limit for UI performance

        public OnlineMembersMonitor(AppConfig config, DiscordUser currentUser)
        {
            _config = config;
            _currentUser = currentUser;
            _apiService = new DiscordApiService();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8) // 8-second refresh interval
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
            OnlinePresenceRegistry.RegisterHeartbeat(_currentUser);

            Task.Run(async () =>
            {
                try
                {
                    List<DiscordUser> resultList = new List<DiscordUser>();

                    List<DiscordUser> guildMembers = await _apiService.GetGuildOnlineMembersAsync(_config.GuildId, _config.BotToken);
                    if (guildMembers.Count == 0 && !string.IsNullOrWhiteSpace(_config.GuildId))
                    {
                        guildMembers = await _apiService.GetGuildWidgetMembersAsync(_config.GuildId);
                    }

                    List<DiscordUser> appUsers = OnlinePresenceRegistry.GetActiveAppUsers();

                    foreach (var u in appUsers)
                    {
                        if (!u.IsBot && !resultList.Any(r => r.Username.Equals(u.Username, StringComparison.OrdinalIgnoreCase)))
                        {
                            resultList.Add(u);
                        }
                    }

                    foreach (var g in guildMembers)
                    {
                        if (!g.IsBot && !resultList.Any(r => r.Username.Equals(g.Username, StringComparison.OrdinalIgnoreCase)))
                        {
                            resultList.Add(g);
                        }
                    }

                    // Remove current user if present, then re-insert at top
                    resultList.RemoveAll(m => m.IsBot || m.Username.Equals(_currentUser.Username, StringComparison.OrdinalIgnoreCase));
                    resultList.Insert(0, _currentUser);

                    // Limit to 26 members max
                    if (resultList.Count > MaxDisplayMembersLimit)
                    {
                        resultList = resultList.Take(MaxDisplayMembersLimit).ToList();
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        OnMembersUpdated?.Invoke(this, resultList);
                    });
                }
                catch
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        OnMembersUpdated?.Invoke(this, new List<DiscordUser> { _currentUser });
                    });
                }
            });
        }
    }
}
