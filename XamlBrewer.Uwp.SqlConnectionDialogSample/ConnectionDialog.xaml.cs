using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace XamlBrewer.SqlClient
{
    /// <summary>
    /// A dialog to configure and test a connection to a SQL Server database.
    /// </summary>
    public sealed partial class ConnectionDialog : ContentDialog, INotifyPropertyChanged
    {
        private bool isBusy;
        private List<string> mostRecentConnections;
        private string server;
        private bool? integratedSecurity;
        private string userId;
        private string password;
        private string connectionString;

        private List<string> databases = new List<string>();
        private string database;

        private SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

        public event PropertyChangedEventHandler PropertyChanged;

        public ConnectionDialog()
        {
            this.InitializeComponent();

            mostRecentConnections = ReadMostRecentConnections();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is busy.
        /// </summary>
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the most recently succesfully used server names.
        /// </summary>
        public List<string> MostRecentConnections
        {
            get { return mostRecentConnections; }
        }

        /// <summary>
        /// Gets or sets the most recently succesfully used server name.
        /// </summary>
        public string MostRecentConnection
        {
            get { return MostRecentConnections.FirstOrDefault(); }
            set
            {
                if (mostRecentConnections.Contains(value))
                {
                    if (mostRecentConnections.FirstOrDefault() == value)
                    {
                        return; // Already on top
                    }

                    mostRecentConnections.Remove(value);
                }

                mostRecentConnections.Insert(0, value);
                SaveMostRecentConnections();
            }
        }

        /// <summary>
        /// Gets or sets the current server name.
        /// </summary>
        public string Server
        {
            get { return server; }
            set
            {
                server = value;
                builder.DataSource = server;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether integrated security is used for authentication.
        /// </summary>
        public bool? IntegratedSecurity
        {
            get { return integratedSecurity; }
            set
            {
                integratedSecurity = value;
                if (integratedSecurity.HasValue)
                {
                    builder.IntegratedSecurity = integratedSecurity.Value;
                    OnPropertyChanged(nameof(SqlSecurity));
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the SQL user identifier.
        /// </summary>
        public string UserId
        {
            get { return userId; }
            set
            {
                userId = value;
                builder.UserID = userId;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the SQL password.
        /// </summary>
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                builder.Password = password;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the list of databases.
        /// </summary>
        public List<string> Databases
        {
            get { return databases; }
            set
            {
                databases = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the current database.
        /// </summary>
        public string Database
        {
            get { return database; }
            set
            {
                database = value;
                builder.InitialCatalog = database;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the current connection string.
        /// </summary>
        public string ConnectionString
        {
            get { return connectionString; }
            set
            {
                connectionString = value;
                builder.ConnectionString = connectionString;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets a value indicating whether SQL security is used for authentication.
        /// </summary>
        public bool SqlSecurity
        {
            get { return IntegratedSecurity.HasValue && !IntegratedSecurity.Value; }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Tests the current connection and shows the result in a message dialog.
        /// </summary>
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            IsBusy = true;
            args.Cancel = true;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                IsBusy = true;
            });

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(builder.ConnectionString))
                    {
                        con.Open();
                    }

                    var msg = new MessageDialog("Connection Successful")
                    {
                        Title = "OK"
                    };

                    await msg.ShowAsync();
                }
                catch (Exception ex)
                {
                    args.Cancel = true;

                    var msg = new MessageDialog(ex.Message)
                    {
                        Title = "Error"
                    };

                    await msg.ShowAsync();
                }
                finally
                {
                    IsBusy = false;
                }
            });
        }

        /// <summary>
        /// Connects to the database. Closes the dialog and returns the connection string if successful.
        /// </summary>
        private async void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            IsBusy = true;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                IsBusy = true;
            });

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(builder.ConnectionString))
                    {
                        con.Open();
                    }

                    MostRecentConnection = Server;
                    ConnectionString = builder.ConnectionString;
                }
                catch (Exception ex)
                {
                    args.Cancel = true;
                    var msg = new MessageDialog(ex.Message)
                    {
                        Title = "Error"
                    };

                    await msg.ShowAsync();
                }
                finally
                {
                    IsBusy = false;
                }

                IsBusy = false;
            });
        }

        private void Authentication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IntegratedSecurity = (sender as ComboBox).SelectedIndex == 0;
        }

        private void Database_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Database = e.AddedItems[0].ToString();
                builder.InitialCatalog = Database;
            }
            catch (Exception)
            {
                builder.InitialCatalog = string.Empty;
            }
        }

        /// <summary>
        /// Shows the default input panel.
        /// </summary>
        private void DefaultButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Server = builder.DataSource;
            AuthenticationComboBox.SelectedIndex = builder.IntegratedSecurity ? 0 : 1;
            UserId = builder.UserID;
            Password = builder.Password;
            DatabaseComboBox.SelectedValue = builder.InitialCatalog;

            DefaultGrid.Opacity = 1;
            DirectGrid.Opacity = 0;
            Host.Children.Remove(DirectGrid);
            Host.Children.Insert(0, DirectGrid);
        }

        /// <summary>
        /// Shows the direct input panel (connectionstring).
        /// </summary>
        private void DirectButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ConnectionString = builder.ConnectionString;

            DirectGrid.Opacity = 1;
            DefaultGrid.Opacity = 0;
            Host.Children.Remove(DefaultGrid);
            Host.Children.Insert(0, DefaultGrid);
        }

        /// <summary>
        /// Connects to the server and fetches the list of databases.
        /// </summary>
        private async void DatabaseComboBox_DropDownOpened(object sender, object e)
        {
            if (string.IsNullOrWhiteSpace(Server))
            {
                return;
            }

            IsBusy = true;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                IsBusy = true;
            });

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                var databases = new List<string>();

                try
                {
                    using (var connection = new SqlConnection(builder.ConnectionString))
                    {
                        connection.Open();

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT [name] FROM sys.databases ORDER BY [name]";

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    databases.Add(reader.GetString(0));
                                }
                            }
                        }
                    }

                    Databases = databases;
                }
                catch (Exception)
                {
                    Databases = new List<string>();
                }
                finally
                {
                    IsBusy = false;
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Reads the most recent connections from local settings.
        /// </summary>
        private List<String> ReadMostRecentConnections()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                var fullString = localSettings.Values["SqlConnectionDialogServers"].ToString();
                return fullString.Split("###").ToList();
            }
            catch (Exception)
            {
                // Probably corrupt settings.
            }

            return new List<string>();
        }

        /// <summary>
        /// Writes the most recent connections to local settings.
        /// </summary>
        private void SaveMostRecentConnections()
        {
            try
            {
                var fullString = string.Join("###", mostRecentConnections);
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["SqlConnectionDialogServers"] = fullString;
            }
            catch (Exception)
            {
                // No need to crash the app for this.
            }
        }
    }
}
