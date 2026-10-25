using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FlightRecorder.Client;

public enum DialogVariant
{
    Warning,
    Danger,
    Info
}

public class ConfirmDialogOptions
{
    public string Title { get; set; } = "Confirmação";
    public string Message { get; set; } = string.Empty;
    public string ConfirmText { get; set; } = "Confirmar";
    public string? CancelText { get; set; } = "Cancelar";
    public DialogVariant Variant { get; set; } = DialogVariant.Warning;
}

public partial class FluxConfirmDialog : Window
{
    public FluxConfirmDialog(ConfirmDialogOptions options)
    {
        InitializeComponent();
        Configure(options);
    }

    private void Configure(ConfirmDialogOptions options)
    {
        TitleText.Text = options.Title;
        MessageText.Text = options.Message;
        BtnConfirm.Content = options.ConfirmText;

        if (string.IsNullOrEmpty(options.CancelText))
        {
            BtnCancel.Visibility = Visibility.Collapsed;
        }
        else
        {
            BtnCancel.Content = options.CancelText;
            BtnCancel.Visibility = Visibility.Visible;
        }

        var brushConverter = new BrushConverter();

        switch (options.Variant)
        {
            case DialogVariant.Danger:
                IconWrapper.Background = (Brush)brushConverter.ConvertFrom("#3b1219")!;
                IconWrapper.BorderBrush = (Brush)brushConverter.ConvertFrom("#881337")!;
                // Circle with exclamation mark (Error/Danger)
                IconPath.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z");
                IconPath.Fill = (Brush)brushConverter.ConvertFrom("#ef4444")!;
                BtnConfirm.Style = (Style)FindResource("FluxDangerButtonStyle");
                break;

            case DialogVariant.Info:
                IconWrapper.Background = (Brush)brushConverter.ConvertFrom("#3a190b")!;
                IconWrapper.BorderBrush = (Brush)brushConverter.ConvertFrom("#8c2800")!;
                // Info circle
                IconPath.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z");
                IconPath.Fill = (Brush)brushConverter.ConvertFrom("#EE4203")!;
                BtnConfirm.Style = (Style)FindResource("FluxPrimaryButtonStyle");
                break;

            case DialogVariant.Warning:
            default:
                IconWrapper.Background = (Brush)brushConverter.ConvertFrom("#2d2212")!;
                IconWrapper.BorderBrush = (Brush)brushConverter.ConvertFrom("#78350f")!;
                // Warning triangle
                IconPath.Data = Geometry.Parse("M12,2L1,21H23L12,2M12,6L19.53,19H4.47L12,6M11,10V14H13V10H11M11,16V18H13V16H11Z");
                IconPath.Fill = (Brush)brushConverter.ConvertFrom("#f59e0b")!;
                BtnConfirm.Style = (Style)FindResource("FluxWarningButtonStyle");
                break;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
