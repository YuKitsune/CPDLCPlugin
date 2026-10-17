using System.Windows.Controls;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Windows;

public partial class ConnectionRequestView : UserControl
{
    public ConnectionRequestView(ConnectionRequestViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
