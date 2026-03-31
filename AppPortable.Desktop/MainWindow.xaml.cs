using System.Windows;
using AppPortable.Desktop.ViewModels;

namespace AppPortable.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
