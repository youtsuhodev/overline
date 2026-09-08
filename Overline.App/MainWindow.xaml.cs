using System.Windows;
using System.Windows.Controls;

namespace Overline.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Persist the edited settings section (checkbox Tag = "time" or "media").</summary>
    private void SettingsCheckChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel
            && sender is FrameworkElement { Tag: string section })
        {
            viewModel.SaveSettings(section);
        }
    }

    // ===================== Custom title bar controls =====================

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}