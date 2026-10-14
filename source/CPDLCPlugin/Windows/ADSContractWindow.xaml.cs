using System.Windows;
using System.Windows.Input;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Windows;

public partial class ADSContractWindow : Window
{
    readonly ADSContractViewModel _viewModel;

    public ADSContractWindow(ADSContractViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Closed += (_, _) => _viewModel.Dispose();
    }

    void CallsignButton_Click(object sender, RoutedEventArgs e)
    {
        CallsignButton.Visibility = Visibility.Collapsed;
        CallsignTextBox.Visibility = Visibility.Visible;
        CallsignTextBox.Focus();
        CallsignTextBox.SelectAll();
    }

    void CallsignTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        _viewModel.LoadContractCommand.Execute(null);
        CallsignTextBox.Visibility = Visibility.Collapsed;
        CallsignButton.Visibility = Visibility.Visible;

        e.Handled = true;
    }

    void SelectedPeriodButton_Click(object sender, RoutedEventArgs e)
    {
        CustomPeriodInput.Value = string.Empty;
        PeriodPopup.IsOpen = true;
    }

    void PeriodButton_Click(object sender, RoutedEventArgs e)
    {
        PeriodPopup.IsOpen = false;
    }

    void CustomPeriodInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        _viewModel.SelectCustomPeriodCommand.Execute(CustomPeriodInput.Value);
        PeriodPopup.IsOpen = false;

        e.Handled = true;
    }
}

