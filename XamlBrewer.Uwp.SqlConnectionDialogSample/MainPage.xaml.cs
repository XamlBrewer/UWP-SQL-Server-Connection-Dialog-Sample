using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using XamlBrewer.SqlClient;

namespace XamlBrewer.Uwp.SqlConnectionDialogSample
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private string status = "You are not connected.";
        private List<SqlTable> tables = new List<SqlTable>();

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

        public List<SqlTable> Tables
        {
            get { return tables; }
            set
            {
                tables = value;
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
            Status = string.Format("You are connected to {0} on {1} as {2}.", builder.InitialCatalog, builder.DataSource, builder.UserID);

            var query = @"
                SELECT SCHEMA_NAME(t.schema_id) AS [Schema]  
                        ,t.name AS [Table]  
                        ,(SELECT [rows]  
                            FROM sys.sysindexes i  
                           WHERE (i.id = t.object_id)  
                             AND (i.indid < 2)   
                             AND (OBJECTPROPERTY(i.id, 'IsUserTable') = 1)) AS [Rows]  
                    FROM sys.tables t   
                   ORDER BY 2";

            var newTables = new List<SqlTable>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                newTables.Add(new SqlTable
                                {
                                    Schema = reader.GetString(0),
                                    Name = reader.GetString(1),
                                    NumberOfRows = reader.GetInt32(2)
                                });
                            }
                        }
                    }
                }

                Tables = newTables;
            }
            catch (Exception ex)
            {
                var msg = new MessageDialog(ex.Message)
                {
                    Title = "Error"
                };

                await msg.ShowAsync();
            }
        }
    }

    public class SqlTable
    {
        public string Schema { get; set; }

        public string Name { get; set; }

        public int NumberOfRows { get; set; }
    }
}
