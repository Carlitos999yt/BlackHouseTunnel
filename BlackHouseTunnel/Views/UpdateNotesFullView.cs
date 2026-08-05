using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BlackHouseTunnel.Views
{
    public class UpdateNotesFullView : UserControl
    {
        public event EventHandler? OnBackRequested;

        public UpdateNotesFullView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Grid mainGrid = new Grid
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060609"))
            };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 1. TOP BAR WITH BACK BUTTON
            Border topBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C0C12")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1C2C")),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            Grid headerGrid = new Grid { Margin = new Thickness(20, 0, 20, 0) };

            Button backBtn = new Button
            {
                Content = "← Volver al Dashboard",
                Height = 34,
                Padding = new Thickness(12, 0, 12, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181826")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            ControlTemplate btnTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            btnTemplate.VisualTree = borderFactory;
            backBtn.Template = btnTemplate;

            backBtn.Click += (s, e) => OnBackRequested?.Invoke(this, EventArgs.Empty);

            TextBlock title = new TextBlock
            {
                Text = "🚀 NOTAS DE ACTUALIZACIÓN & NOVEDADES",
                FontFamily = new FontFamily("Segoe UI, sans-serif"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerGrid.Children.Add(backBtn);
            headerGrid.Children.Add(title);
            topBar.Child = headerGrid;
            Grid.SetRow(topBar, 0);
            mainGrid.Children.Add(topBar);

            // 2. MAIN SELECTABLE TEXT CONTENT
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(32)
            };

            StackPanel contentPanel = new StackPanel { MaxWidth = 800 };

            // Date Badge
            TextBlock dateBadge = new TextBlock
            {
                Text = "Publicado: 05 de Agosto de 2026",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            contentPanel.Children.Add(dateBadge);

            // Selectable TextBox for Update Notes
            TextBox notesBox = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI, sans-serif"),
                Text = @"🔥 ¡ACTUALIZACIÓN MASIVA DE HOY EN BLACKHOUSETUNNEL! 🔥

Nos complace presentar las últimas mejoras integradas en la infraestructura y la interfaz de BlackHouseTunnel:

1. 🛡️ Autenticación e Inspección Infalsificable de Discord:
   - Validación cruzada en el servidor oficial (ID: 1529015986135502951).
   - Verificación remota directa entre el Host y el Cliente para prevenir salteos por modificación de ejecutables.

2. 🎛️ Sistema de Creación de Host Avanzado por Roles:
   - Si eres Staff, Hoster o posee rol Superior, se desbloquean opciones exclusivas:
     * Visibilidad Pública para todos los miembros.
     * Restricción exclusiva para miembros con el Rol Privadito.
     * Lista blanca (Whitelist) personalizada por Discord IDs.

3. ⏱️ Monitor de Miembros en Línea en Tiempo Real:
   - Actualización dinámica en vivo cada 4 segundos.
   - Desglose de perfil en 4 niveles: Apodo de servidor, @usuario original, Rol de jerarquía e insignia especial Privadito.

4. ⚡ Persistencia de Sesión & Auto-Login:
   - Ingreso silencioso automático sin requerir autenticación repetida en el navegador.

---
© BlackHouseTunnel 2026 - Todos los derechos reservados."
            };

            contentPanel.Children.Add(notesBox);
            scroll.Content = contentPanel;
            Grid.SetRow(scroll, 1);
            mainGrid.Children.Add(scroll);

            this.Content = mainGrid;
        }
    }
}
