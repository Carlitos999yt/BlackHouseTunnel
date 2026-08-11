using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BlackHouseTunnel.Models;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class HostRulesView : Grid
    {
        private readonly AppConfig _config;
        private readonly DiscordApiService _apiService = new DiscordApiService();
        private readonly List<DiscordRole> _guildRoles = new();

        private ListBox _rulesListBox = new();
        private TextBox _ruleNameBox = new();
        private ComboBox _rolesCombo = new();
        private StackPanel _selectedRolesStack = new();
        private List<string> _selectedRoleIds = new();

        private TextBox _userIdInput = new();
        private StackPanel _userWhitelistStack = new();
        private List<string> _allowedUserIds = new();

        private CheckBox _requireKeyCheck = new();
        private TextBox _colorHexBox = new();
        private Border _colorPreviewBox = new();
        private TextBox _badgeLabelBox = new();
        private TextBlock _statusTxt = new();

        private CustomAccessRule? _currentRule = null;

        public event EventHandler? OnBackToHostRequested;
        public event EventHandler? OnRulesUpdated;

        public HostRulesView()
        {
            _config = ConfigManager.CurrentConfig;
            Margin = new Thickness(0);
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060609"));

            BuildUI();
            Loaded += async (s, e) => await InitializeAsync();
        }

        private void BuildUI()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header bar
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Main content grid

            // 1. HEADER BAR WITH BACK BUTTON & SVG ICON
            Border headerCard = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0E17")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F202D")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 16, 24, 16)
            };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Back button with SVG arrow icon
            Button backBtn = new Button
            {
                Height = 38,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1F2E")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B3D")),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Volver al Formulario de Host"
            };
            MainMenuView.SetButtonCornerRadius(backBtn, 8);

            StackPanel backContent = new StackPanel { Orientation = Orientation.Horizontal };
            Path backIcon = CreateSvgPath("M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z", "#9CA3AF", 16);
            backIcon.Margin = new Thickness(0, 0, 8, 0);

            TextBlock backTxt = new TextBlock
            {
                Text = "Volver al Formulario",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            backContent.Children.Add(backIcon);
            backContent.Children.Add(backTxt);
            backBtn.Content = backContent;
            backBtn.Click += (s, e) => OnBackToHostRequested?.Invoke(this, EventArgs.Empty);

            Grid.SetColumn(backBtn, 0);
            headerGrid.Children.Add(backBtn);

            // Title & Subtitle
            StackPanel titleStack = new StackPanel { Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            TextBlock titleTxt = new TextBlock
            {
                Text = "Configuración Completa de Reglas del Servidor",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            TextBlock subTxt = new TextBlock
            {
                Text = "Crea y personaliza reglas de visibilidad, control de acceso por rol/usuario y estilos para tus hostings.",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleStack.Children.Add(titleTxt);
            titleStack.Children.Add(subTxt);

            Grid.SetColumn(titleStack, 1);
            headerGrid.Children.Add(titleStack);
            headerCard.Child = headerGrid;
            Grid.SetRow(headerCard, 0);
            Children.Add(headerCard);

            // 2. MAIN 2-COLUMN CONTENT GRID
            Grid contentGrid = new Grid { Margin = new Thickness(24) };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) }); // Left: Saved rules list
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });  // Gap
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Right: Rules Editor Form

            // LEFT COLUMN: Rules List Card
            Border leftCard = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0E17")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F202D")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16)
            };

            StackPanel leftStack = new StackPanel();
            TextBlock leftTitle = new TextBlock
            {
                Text = "Mis Reglas Creadas",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            };

            _rulesListBox = new ListBox
            {
                Height = 420,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12131C")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B3D")),
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            };
            _rulesListBox.SelectionChanged += RulesListBox_SelectionChanged;

            Button newRuleBtn = new Button
            {
                Height = 38,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            MainMenuView.SetButtonCornerRadius(newRuleBtn, 8);

            StackPanel newRuleContent = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            Path addIcon = CreateSvgPath("M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z", "#FFFFFF", 16);
            addIcon.Margin = new Thickness(0, 0, 8, 0);
            TextBlock addTxt = new TextBlock { Text = "Nueva Regla", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
            newRuleContent.Children.Add(addIcon);
            newRuleContent.Children.Add(addTxt);
            newRuleBtn.Content = newRuleContent;
            newRuleBtn.Click += (s, e) => CreateNewRule();

            leftStack.Children.Add(leftTitle);
            leftStack.Children.Add(_rulesListBox);
            leftStack.Children.Add(newRuleBtn);
            leftCard.Child = leftStack;
            Grid.SetColumn(leftCard, 0);
            contentGrid.Children.Add(leftCard);

            // RIGHT COLUMN: Editor Card
            Border rightCard = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0E17")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F202D")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20)
            };

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel formStack = new StackPanel();

            // Form Title
            TextBlock formTitle = new TextBlock
            {
                Text = "Editar Parámetros de la Regla",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 16)
            };
            formStack.Children.Add(formTitle);

            // Rule Name
            formStack.Children.Add(CreateFieldLabel("Nombre de la Regla:"));
            _ruleNameBox = CreateStyledTextBox("Nombre de tu regla personalizada");
            formStack.Children.Add(_ruleNameBox);

            // Roles Filter (Discord ComboBox real-time)
            formStack.Children.Add(CreateFieldLabel("Seleccionar Rol de Discord Autorizado (Tiempo Real):"));

            Grid roleGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            roleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            roleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            roleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _rolesCombo = CreateStyledComboBox();
            Grid.SetColumn(_rolesCombo, 0);
            roleGrid.Children.Add(_rolesCombo);

            Button addRoleBtn = new Button
            {
                Content = "Añadir Rol",
                Height = 36,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            MainMenuView.SetButtonCornerRadius(addRoleBtn, 8);
            addRoleBtn.Click += (s, e) => AddSelectedRole();
            Grid.SetColumn(addRoleBtn, 2);
            roleGrid.Children.Add(addRoleBtn);
            formStack.Children.Add(roleGrid);

            _selectedRolesStack = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            formStack.Children.Add(_selectedRolesStack);

            // Allowed User Whitelist Table
            formStack.Children.Add(CreateFieldLabel("Usuarios Permitidos Específicos (IDs de Discord):"));

            Grid userAddGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            userAddGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            userAddGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            userAddGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _userIdInput = CreateStyledTextBox("Ingresa ID de usuario de Discord");
            Grid.SetColumn(_userIdInput, 0);
            userAddGrid.Children.Add(_userIdInput);

            Button addUserBtn = new Button
            {
                Content = "Añadir ID",
                Height = 36,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            MainMenuView.SetButtonCornerRadius(addUserBtn, 8);
            addUserBtn.Click += (s, e) => AddAllowedUser();
            Grid.SetColumn(addUserBtn, 2);
            userAddGrid.Children.Add(addUserBtn);
            formStack.Children.Add(userAddGrid);

            _userWhitelistStack = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            formStack.Children.Add(_userWhitelistStack);

            // Require Key CheckBox
            _requireKeyCheck = new CheckBox
            {
                Content = "🔑 Requerir Llave Secreta (Habilita la tarjeta lateral de contraseña)",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 16)
            };
            formStack.Children.Add(_requireKeyCheck);

            // Badge Label & Embed Color Selector
            Grid styleGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            styleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            styleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            styleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel badgeStack = new StackPanel();
            badgeStack.Children.Add(CreateFieldLabel("Etiqueta / Badge:"));
            _badgeLabelBox = CreateStyledTextBox("⚡ Regla Personalizada");
            badgeStack.Children.Add(_badgeLabelBox);
            Grid.SetColumn(badgeStack, 0);

            StackPanel colorStack = new StackPanel();
            colorStack.Children.Add(CreateFieldLabel("Color del Embed Hex:"));

            Grid colorPickerGrid = new Grid();
            colorPickerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colorPickerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            colorPickerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            _colorHexBox = CreateStyledTextBox("#A855F7");
            _colorHexBox.TextChanged += (s, e) => UpdateColorPreview();
            Grid.SetColumn(_colorHexBox, 0);
            colorPickerGrid.Children.Add(_colorHexBox);

            _colorPreviewBox = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A855F7")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B3D")),
                BorderThickness = new Thickness(1)
            };
            Grid.SetColumn(_colorPreviewBox, 2);
            colorPickerGrid.Children.Add(_colorPreviewBox);
            colorStack.Children.Add(colorPickerGrid);

            // Color preset buttons
            StackPanel presetsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            string[] presetColors = new string[] { "#A855F7", "#FFD700", "#EF4444", "#10B981", "#3B82F6", "#F59E0B", "#EC4899" };
            foreach (var hex in presetColors)
            {
                Button cBtn = new Button
                {
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(0, 0, 6, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = hex
                };
                MainMenuView.SetButtonCornerRadius(cBtn, 12);
                cBtn.Click += (s, e) => _colorHexBox.Text = hex;
                presetsRow.Children.Add(cBtn);
            }
            colorStack.Children.Add(presetsRow);

            Grid.SetColumn(colorStack, 2);
            styleGrid.Children.Add(badgeStack);
            styleGrid.Children.Add(colorStack);
            formStack.Children.Add(styleGrid);

            // Status message
            _statusTxt = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Margin = new Thickness(0, 0, 0, 16),
                TextWrapping = TextWrapping.Wrap
            };
            formStack.Children.Add(_statusTxt);

            // Buttons: Save & Delete
            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            Button deleteBtn = new Button
            {
                Height = 40,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 12, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            MainMenuView.SetButtonCornerRadius(deleteBtn, 8);
            deleteBtn.Content = "Eliminar Regla";
            deleteBtn.Click += (s, e) => DeleteRule();

            Button saveBtn = new Button
            {
                Height = 40,
                Padding = new Thickness(24, 0, 24, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            MainMenuView.SetButtonCornerRadius(saveBtn, 8);
            saveBtn.Content = "Guardar Cambios";
            saveBtn.Click += (s, e) => SaveRule();

            btnRow.Children.Add(deleteBtn);
            btnRow.Children.Add(saveBtn);
            formStack.Children.Add(btnRow);

            scroll.Content = formStack;
            rightCard.Child = scroll;
            Grid.SetColumn(rightCard, 2);

            contentGrid.Children.Add(rightCard);
            Grid.SetRow(contentGrid, 1);
            Children.Add(contentGrid);

            RefreshRulesList();
        }

        private async Task InitializeAsync()
        {
            _statusTxt.Text = "Obteniendo roles en tiempo real del servidor...";
            _statusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));

            string token = !string.IsNullOrWhiteSpace(_config.BotToken) ? _config.BotToken : TokenProtector.GetDefaultBotToken();
            var roles = await _apiService.FetchGuildRolesAsync(_config.GuildId, token);
            _guildRoles.Clear();
            _guildRoles.AddRange(roles);

            _rolesCombo.Items.Clear();
            foreach (var r in _guildRoles)
            {
                _rolesCombo.Items.Add($"[{r.Id}] @{r.Name}");
            }
            if (_rolesCombo.Items.Count > 0) _rolesCombo.SelectedIndex = 0;

            _statusTxt.Text = $"✓ {_guildRoles.Count} roles de Discord cargados en tiempo real.";
            _statusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }

        private void RefreshRulesList()
        {
            _rulesListBox.Items.Clear();
            foreach (var rule in _config.SavedCustomRules)
            {
                _rulesListBox.Items.Add($"⚡ {rule.RuleName}");
            }
            if (_config.SavedCustomRules.Count > 0 && _rulesListBox.SelectedIndex == -1)
            {
                _rulesListBox.SelectedIndex = 0;
            }
        }

        private void RulesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = _rulesListBox.SelectedIndex;
            if (idx >= 0 && idx < _config.SavedCustomRules.Count)
            {
                _currentRule = _config.SavedCustomRules[idx];
                LoadRuleToForm(_currentRule);
            }
        }

        private void LoadRuleToForm(CustomAccessRule rule)
        {
            _ruleNameBox.Text = rule.RuleName;
            _requireKeyCheck.IsChecked = rule.RequireAccessKey;
            _colorHexBox.Text = rule.EmbedColorHex;
            _badgeLabelBox.Text = rule.BadgeLabel;

            _selectedRoleIds = new List<string>(rule.AllowedRoleIds);
            RenderSelectedRoles();

            _allowedUserIds = new List<string>(rule.AllowedUserIds);
            RenderAllowedUsers();

            UpdateColorPreview();
        }

        private void CreateNewRule()
        {
            _currentRule = new CustomAccessRule
            {
                RuleName = $"Regla Personalizada #{_config.SavedCustomRules.Count + 1}"
            };
            _config.SavedCustomRules.Add(_currentRule);
            ConfigManager.SaveConfig(_config);

            RefreshRulesList();
            _rulesListBox.SelectedIndex = _config.SavedCustomRules.Count - 1;
            OnRulesUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void AddSelectedRole()
        {
            int idx = _rolesCombo.SelectedIndex;
            if (idx >= 0 && idx < _guildRoles.Count)
            {
                string rId = _guildRoles[idx].Id;
                if (!_selectedRoleIds.Contains(rId))
                {
                    _selectedRoleIds.Add(rId);
                    RenderSelectedRoles();
                }
            }
        }

        private void RenderSelectedRoles()
        {
            _selectedRolesStack.Children.Clear();
            foreach (var roleId in _selectedRoleIds)
            {
                var roleObj = _guildRoles.FirstOrDefault(r => r.Id == roleId);
                string rName = roleObj != null ? roleObj.Name : roleId;

                Border chip = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1F2E")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B3D")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 0, 6, 6)
                };

                StackPanel chipStack = new StackPanel { Orientation = Orientation.Horizontal };
                TextBlock chipTxt = new TextBlock
                {
                    Text = $"[{roleId}] @{rName}",
                    FontSize = 12,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Button removeBtn = new Button
                {
                    Content = "✕",
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(8, 0, 0, 0),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                string capturedId = roleId;
                removeBtn.Click += (s, e) =>
                {
                    _selectedRoleIds.Remove(capturedId);
                    RenderSelectedRoles();
                };

                chipStack.Children.Add(chipTxt);
                chipStack.Children.Add(removeBtn);
                chip.Child = chipStack;
                _selectedRolesStack.Children.Add(chip);
            }
        }

        private void AddAllowedUser()
        {
            string uid = _userIdInput.Text.Trim();
            if (!string.IsNullOrWhiteSpace(uid) && !_allowedUserIds.Contains(uid))
            {
                _allowedUserIds.Add(uid);
                _userIdInput.Text = "";
                RenderAllowedUsers();
            }
        }

        private void RenderAllowedUsers()
        {
            _userWhitelistStack.Children.Clear();
            foreach (var uid in _allowedUserIds)
            {
                Border chip = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12131C")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B3D")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 0, 6, 6)
                };

                StackPanel chipStack = new StackPanel { Orientation = Orientation.Horizontal };
                TextBlock chipTxt = new TextBlock
                {
                    Text = $"👤 ID: {uid}",
                    FontSize = 12,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Button removeBtn = new Button
                {
                    Content = "✕",
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(8, 0, 0, 0),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                string capturedUid = uid;
                removeBtn.Click += (s, e) =>
                {
                    _allowedUserIds.Remove(capturedUid);
                    RenderAllowedUsers();
                };

                chipStack.Children.Add(chipTxt);
                chipStack.Children.Add(removeBtn);
                chip.Child = chipStack;
                _userWhitelistStack.Children.Add(chip);
            }
        }

        private void SaveRule()
        {
            if (_currentRule == null)
            {
                CreateNewRule();
            }

            if (_currentRule == null) return;

            _currentRule.RuleName = string.IsNullOrWhiteSpace(_ruleNameBox.Text) ? "Regla Personalizada" : _ruleNameBox.Text.Trim();
            _currentRule.RequireAccessKey = _requireKeyCheck.IsChecked == true;
            _currentRule.EmbedColorHex = string.IsNullOrWhiteSpace(_colorHexBox.Text) ? "#A855F7" : _colorHexBox.Text.Trim();
            _currentRule.BadgeLabel = string.IsNullOrWhiteSpace(_badgeLabelBox.Text) ? "⚡ Regla Personalizada" : _badgeLabelBox.Text.Trim();

            _currentRule.AllowedRoleIds = new List<string>(_selectedRoleIds);
            _currentRule.AllowedUserIds = new List<string>(_allowedUserIds);

            ConfigManager.SaveConfig(_config);
            RefreshRulesList();
            OnRulesUpdated?.Invoke(this, EventArgs.Empty);

            _statusTxt.Text = $"✓ Regla '{_currentRule.RuleName}' guardada correctamente.";
            _statusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }

        private void DeleteRule()
        {
            if (_currentRule == null) return;

            _config.SavedCustomRules.Remove(_currentRule);
            ConfigManager.SaveConfig(_config);
            _currentRule = null;

            RefreshRulesList();
            OnRulesUpdated?.Invoke(this, EventArgs.Empty);

            _statusTxt.Text = "Regla eliminada.";
            _statusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        }

        private void UpdateColorPreview()
        {
            try
            {
                string hex = _colorHexBox.Text.Trim();
                if (hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 9))
                {
                    _colorPreviewBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                }
            }
            catch { }
        }

        private TextBlock CreateFieldLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                Margin = new Thickness(0, 8, 0, 4)
            };
        }

        private TextBox CreateStyledTextBox(string defaultText)
        {
            TextBox tb = new TextBox
            {
                Text = defaultText,
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

            ControlTemplate template = new ControlTemplate(typeof(TextBox));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

            FrameworkElementFactory scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.Name = "PART_ContentHost";
            scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(0));
            scrollViewer.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(scrollViewer);

            template.VisualTree = border;
            tb.Template = template;
            return tb;
        }

        private ComboBox CreateStyledComboBox()
        {
            ComboBox cb = new ComboBox
            {
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12131C")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B3D")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            return cb;
        }

        private Path CreateSvgPath(string svgData, string colorHex, double size)
        {
            return new Path
            {
                Data = Geometry.Parse(svgData),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }
    }
}
