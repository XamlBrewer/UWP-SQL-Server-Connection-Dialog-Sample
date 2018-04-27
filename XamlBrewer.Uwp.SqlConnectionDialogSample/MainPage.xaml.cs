using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using XamlBrewer.SqlClient;

namespace XamlBrewer.Uwp.SqlConnectionDialogSample
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private string status = "You are not connected";

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            if (titleBar != null)
            {
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.DarkSlateGray;
            }
        }

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var previousStatus = status;
            Status = "Connecting";
            var dialog = new ConnectionDialog();
            var result = await dialog.ShowAsync();

            // User Cancelled
            if (result == ContentDialogResult.None)
            {
                Status = previousStatus;
                return;
            }

            var connectionString = dialog.ConnectionString;
            var builder = new SqlConnectionStringBuilder(connectionString);
            Status = string.Format("You are connected to {0} on {1} as {2}", builder.InitialCatalog, builder.DataSource, builder.UserID);
        }
    }
}
