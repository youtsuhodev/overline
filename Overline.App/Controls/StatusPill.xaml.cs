using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Overline.App.Controls;

/// <summary>
/// Small rounded status pill. <see cref="State"/> drives the color:
/// "running" (green), "faulted" (red), "busy" (amber), anything else (neutral).
/// </summary>
public partial class StatusPill : UserControl
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(string), typeof(StatusPill), new PropertyMetadata("stopped", OnVisualPropertyChanged));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StatusPill), new PropertyMetadata(string.Empty));

    public StatusPill()
    {
        InitializeComponent();
        ApplyVisual();
    }

    public string State
    {
        get => (string)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((StatusPill)d).ApplyVisual();

    private void ApplyVisual()
    {
        var (background, foreground) = State switch
        {
            "running" => (Color.FromArgb(0x33, 0x43, 0xD1, 0x7C), Color.FromRgb(0x43, 0xD1, 0x7C)),
            "faulted" => (Color.FromArgb(0x33, 0xFF, 0x6B, 0x6B), Color.FromRgb(0xFF, 0x6B, 0x6B)),
            "busy" => (Color.FromArgb(0x33, 0xFF, 0xB8, 0x6C), Color.FromRgb(0xFF, 0xB8, 0x6C)),
            _ => (Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF), Color.FromRgb(0xB3, 0xB3, 0xB3)),
        };

        Root.Background = new SolidColorBrush(background);
        // TextElement.Foreground is inheritable: setting it on the Border colors the pill text.
        TextElement.SetForeground(Root, new SolidColorBrush(foreground));
    }
}