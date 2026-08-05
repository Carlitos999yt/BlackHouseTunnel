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

        public OnlineMembersMonitor(AppConfig config, DiscordUser currentUser)
        {
            _config = config;
            _currentUser = currentUser;
            _apiService = new DiscordApiService();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
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
                    
                    members = members.Where(m => m.Id != _currentUser.Id && !m.Username.Equals(_currentUser.Username, StringComparison.OrdinalIgnoreCase)).ToList();
                    members.Insert(0, _currentUser);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        OnMembersUpdated?.Invoke(this, members);
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
