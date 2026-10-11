using System.Windows;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Windows;

public partial class ConnectionRequestWindow : Window
{
    public ConnectionRequestWindow(ConnectionRequestViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
