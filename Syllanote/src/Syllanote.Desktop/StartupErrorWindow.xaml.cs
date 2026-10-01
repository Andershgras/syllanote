using Microsoft.UI.Xaml;
using System;
using Windows.Graphics;

namespace Syllanote.Desktop;

public sealed partial class StartupErrorWindow : Window
{
    public StartupErrorWindow(string databasePath, Exception exception)
    {
        InitializeComponent();

        Title = "Syllanote startup error";
        DatabasePathTextBox.Text = databasePath;
        TechnicalDetailsTextBox.Text = exception.Message;
        AppWindow.Resize(new SizeInt32(700, 500));

        Closed += (_, _) => Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
