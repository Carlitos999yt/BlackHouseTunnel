using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackHouseTunnel.Models;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class CustomRulesEditorModal : Window
    {
        private readonly AppConfig _config;
        private readonly DiscordApiService _apiService = new DiscordApiService();
        private readonly List<DiscordRole> _guildRoles = new();
        private ListBox _rulesList = new();
        private TextBox _ruleNameBox = new();
        private StackPanel _rolesCheckPanel = new();
        private TextBox _userIdsBox = new();
        private CheckBox _requireKeyCheck = new();
        private TextBox _keyBox = new();
        private TextBox _colorHexBox = new();
        private TextBox _badgeLabelBox = new();
        private TextBlock _statusTxt = new();
        private CustomAccessRule? _selectedRule = null;

        public CustomRulesEditorModal()
        {
            _config = ConfigManager.CurrentConfig;

            Title = "⚙️ Editor de Reglas Personalizadas y Control de Acceso";
            Width = 820;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060609"));
            Foreground = Brushes.White;
            ResizeMode = ResizeMode.NoResize;

            BuildUI();
            Loaded += async (s, e) => await LoadDiscordRolesAsync();
        }

        private void BuildUI()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(24) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) }); // Left: Saved rules list
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });  // Gap
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Right: Editor form

            // LEFT PANEL: List of Rules
            Border leftCard = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0E17")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F202D")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16)
            };

            StackPanel leftPanel = new StackPanel();
            TextBlock leftTitle = new TextBlock
            {
                Text = "📋 Reglas Creadas",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            };

            _rulesList = new ListBox
            {
                Height = 440,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12131C")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B3D")),
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            };
            _rulesList.SelectionChanged += RulesList_SelectionChanged;

            Button newRuleBtn = new Button
            {
                Content = "➕ Crear Nueva Regla",
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            MainMenuView.SetButtonCornerRadius(newRuleBtn, 8);
            newRuleBtn.Click += (s, e) => CreateNewRule();

            leftPanel.Children.Add(leftTitle);
            leftPanel.Children.Add(_rulesList);
            leftPanel.Children.Add(newRuleBtn);
            leftCard.Child = leftPanel;
            Grid.SetColumn(leftCard, 0);

            // RIGHT PANEL: Rule Form Editor
            Border rightCard = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0E17")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F202D")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20)
            };

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel formPanel = new StackPanel();

            TextBlock rightTitle = new TextBlock
            {
                Text = "🛠️ Configuración de la Regla",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 16)
            };
            formPanel.Children.Add(rightTitle);

            // Rule Name
            formPanel.Children.Add(CreateLabel("Nombre de la Regla (ej: Solo Mods, VIP Torneo):"));
            _ruleNameBox = CreateStyledTextBox();
            formPanel.Children.Add(_ruleNameBox);

            // Roles Filter (Real-Time Discord Roles)
            formPanel.Children.Add(CreateLabel("Roles de Discord Permitidos (Inspección en Tiempo Real):"));
            _rolesCheckPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 16) };
            formPanel.Children.Add(_rolesCheckPanel);

            // User Whitelist
            formPanel.Children.Add(CreateLabel("IDs de Usuarios Específicos (Separados por coma):"));
            _userIdsBox = CreateStyledTextBox();
            formPanel.Children.Add(_userIdsBox);

            // Access Key / Password Requirement
            _requireKeyCheck = new CheckBox
            {
                Content = "🔑 Requerir Llave Secreta para esta Regla",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 8, 0, 8)
            };
            _requireKeyCheck.Checked += (s, e) => _keyBox.IsEnabled = true;
            _requireKeyCheck.Unchecked += (s, e) => _keyBox.IsEnabled = false;
            formPanel.Children.Add(_requireKeyCheck);

            _keyBox = CreateStyledTextBox();
            _keyBox.IsEnabled = false;
            formPanel.Children.Add(_keyBox);

            // Badge Label & Embed Color
            Grid colorGrid = new Grid { Margin = new Thickness(0, 8, 0, 16) };
            colorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            colorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel badgeStack = new StackPanel();
            badgeStack.Children.Add(CreateLabel("Etiqueta / Badge (ej: ⚡ Solo Mods):"));
            _badgeLabelBox = CreateStyledTextBox();
            _badgeLabelBox.Text = "⚡ Regla Personalizada";
            badgeStack.Children.Add(_badgeLabelBox);
            Grid.SetColumn(badgeStack, 0);

            StackPanel colorStack = new StackPanel();
            colorStack.Children.Add(CreateLabel("Color del Embed (Hex):"));
            _colorHexBox = CreateStyledTextBox();
            _colorHexBox.Text = "#A855F7";
            colorStack.Children.Add(_colorHexBox);
            Grid.SetColumn(colorStack, 2);

            colorGrid.Children.Add(badgeStack);
            colorGrid.Children.Add(colorStack);
            formPanel.Children.Add(colorGrid);

            // Status message
            _statusTxt = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Margin = new Thickness(0, 4, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };
            formPanel.Children.Add(_statusTxt);

            // Buttons: Save & Delete
            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            Button deleteBtn = new Button
            {
                Content = "🗑️ Eliminar Regla",
                Height = 38,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 12, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            MainMenuView.SetButtonCornerRadius(deleteBtn, 8);
            deleteBtn.Click += (s, e) => DeleteSelectedRule();

            Button saveBtn = new Button
            {
                Content = "💾 Guardar Regla",
                Height = 38,
                Padding = new Thickness(20, 0, 20, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            MainMenuView.SetButtonCornerRadius(saveBtn, 8);
            saveBtn.Click += (s, e) => SaveCurrentRule();

            btnRow.Children.Add(deleteBtn);
            btnRow.Children.Add(saveBtn);
            formPanel.Children.Add(btnRow);

            scroll.Content = formPanel;
            rightCard.Child = scroll;
            Grid.SetColumn(rightCard, 2);

            mainGrid.Children.Add(leftCard);
            mainGrid.Children.Add(rightCard);

            Content = mainGrid;
            RefreshRulesList();
        }

        private async Task LoadDiscordRolesAsync()
        {
            _statusTxt.Text = "⏳ Cargando roles de Discord en tiempo real...";
            _statusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));

            string token = !string.IsNullOrWhiteSpace(_config.BotToken) ? _config.BotToken : TokenProtector.GetDefaultBotToken();
            var roles = await _apiService.FetchGuildRolesAsync(_config.GuildId, token);
            _guildRoles.Clear();
            _guildRoles.AddRange(roles);

            RenderRolesChecklist();

            _statusTxt.Text = $"✓ {_guildRoles.Count} roles obtenidos en tiempo real de Discord.";
            _statusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }

        private void RenderRolesChecklist()
        {
            _rolesCheckPanel.Children.Clear();
            if (_guildRoles.Count == 0)
            {
                _rolesCheckPanel.Children.Add(new TextBlock
                {
                    Text = "No se pudieron obtener roles del servidor de Discord.",
                    Foreground = Brushes.Gray,
                    FontSize = 12
                });
                return;
            }

            foreach (var role in _guildRoles)
            {
                CheckBox chk = new CheckBox
                {
                    Content = $"[{role.Id}] @{role.Name}",
                    Tag = role.Id,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                if (_selectedRule != null && _selectedRule.AllowedRoleIds.Contains(role.Id))
                {
                    chk.IsChecked = true;
                }
                _rolesCheckPanel.Children.Add(chk);
            }
        }

        private void RefreshRulesList()
        {
            _rulesList.Items.Clear();
            foreach (var rule in _config.SavedCustomRules)
            {
                _rulesList.Items.Add(rule.RuleName);
            }
        }

        private void RulesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = _rulesList.SelectedIndex;
            if (idx >= 0 && idx < _config.SavedCustomRules.Count)
            {
                _selectedRule = _config.SavedCustomRules[idx];
                LoadRuleIntoEditor(_selectedRule);
            }
        }

        private void LoadRuleIntoEditor(CustomAccessRule rule)
        {
            _ruleNameBox.Text = rule.RuleName;
            _userIdsBox.Text = string.Join(", ", rule.AllowedUserIds);
            _requireKeyCheck.IsChecked = rule.RequireAccessKey;
            _keyBox.Text = rule.CustomAccessKey;
            _keyBox.IsEnabled = rule.RequireAccessKey;
            _colorHexBox.Text = rule.EmbedColorHex;
            _badgeLabelBox.Text = rule.BadgeLabel;

            RenderRolesChecklist();
        }

        private void CreateNewRule()
        {
            _selectedRule = new CustomAccessRule
            {
                RuleName = $"Regla #{_config.SavedCustomRules.Count + 1}"
            };
            _config.SavedCustomRules.Add(_selectedRule);
            ConfigManager.SaveConfig(_config);

            RefreshRulesList();
            _rulesList.SelectedIndex = _config.SavedCustomRules.Count - 1;
        }

        private void SaveCurrentRule()
        {
            if (_selectedRule == null)
            {
                CreateNewRule();
            }

            if (_selectedRule == null) return;

            _selectedRule.RuleName = string.IsNullOrWhiteSpace(_ruleNameBox.Text) ? "Regla Personalizada" : _ruleNameBox.Text.Trim();
            _selectedRule.RequireAccessKey = _requireKeyCheck.IsChecked == true;
            _selectedRule.CustomAccessKey = _keyBox.Text.Trim();
            _selectedRule.EmbedColorHex = string.IsNullOrWhiteSpace(_colorHexBox.Text) ? "#A855F7" : _colorHexBox.Text.Trim();
            _selectedRule.BadgeLabel = string.IsNullOrWhiteSpace(_badgeLabelBox.Text) ? "⚡ Regla Personalizada" : _badgeLabelBox.Text.Trim();

            _selectedRule.AllowedUserIds = _userIdsBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            _selectedRule.AllowedRoleIds.Clear();
            foreach (UIElement child in _rolesCheckPanel.Children)
            {
                if (child is CheckBox chk && chk.IsChecked == true && chk.Tag is string roleId)
                {
                    _selectedRule.AllowedRoleIds.Add(roleId);
                }
            }

            ConfigManager.SaveConfig(_config);
            RefreshRulesList();
            _statusTxt.Text = $"✓ Regla '{_selectedRule.RuleName}' guardada correctamente.";
            _statusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }

        private void DeleteSelectedRule()
        {
            if (_selectedRule == null) return;

            _config.SavedCustomRules.Remove(_selectedRule);
            ConfigManager.SaveConfig(_config);
            _selectedRule = null;

            RefreshRulesList();
            _statusTxt.Text = "Regla eliminada.";
            _statusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                Margin = new Thickness(0, 10, 0, 4)
            };
        }

        private TextBox CreateStyledTextBox()
        {
            return new TextBox
            {
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12131C")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B3D")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
        }
    }
}
