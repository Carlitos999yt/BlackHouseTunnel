using System.Windows.Media;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public static class ThemeManager
    {
        public static bool IsLight => ConfigManager.CurrentConfig.ThemeMode == "Light";

        // Backgrounds
        public static SolidColorBrush MainBgBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#F3F4F6" : "#060609"));
        public static SolidColorBrush HeaderBgBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#FFFFFF" : "#0A0A10"));
        public static SolidColorBrush SidebarBgBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#FFFFFF" : "#0A0A10"));
        public static SolidColorBrush CardBgBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#FFFFFF" : "#0F0F18"));
        public static SolidColorBrush CardBorderBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#E5E7EB" : "#1F1F30"));

        // Text Colors (Inverted cleanly for Light mode)
        public static SolidColorBrush TextPrimaryBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#111827" : "#FFFFFF"));
        public static SolidColorBrush TextMutedBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#4B5563" : "#A0A0C0"));

        // Inputs
        public static SolidColorBrush InputBgBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#F9FAFB" : "#141420"));
        public static SolidColorBrush InputBorderBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#D1D5DB" : "#2A2A3D"));

        // Accents
        public static SolidColorBrush AccentBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2"));
    }
}
