using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PC_Control_2
{
    /// <summary>
    /// Datoru kontroles programma, kas spēj noteikt laiku un ieslēgt datora izmantošanu, sūtot komandas uz klienta programmu.
    /// 
    /// Ar programmu var ieslēgt 30 minūtes, 1 studnu, 2 stundas vai bezgalīgu laiku datoriem.
    /// 
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpListener server;
        private ObservableCollection<ClientInfo> clients;
        private Dictionary<string, TcpClient> clientConnections;

        public MainWindow()
        {
            InitializeComponent();
            clients = new ObservableCollection<ClientInfo>();
            clientConnections = new Dictionary<string, TcpClient>();
            ClientsDataGrid.ItemsSource = clients;
            StartServer();
            LoadClientNames();
            Closing += OnClosing;
        }

        private void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Thread serverThread = new Thread(AcceptClients);
            serverThread.IsBackground = true;
            serverThread.Start();
        }
        private void LoadClientNames()
        {
            foreach (var client in clients)
            {
                client.Name = NameStorage.GetName(client.ClientIP);

            }
        }

        private void AcceptClients()
        {
            try
            {
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    string clientName = NameStorage.GetName(clientIP);
                    Dispatcher.Invoke(() => clients.Add(new ClientInfo { ClientIP = clientIP, Name = clientName, Status = "Savienots" }));
                    clientConnections[clientIP] = client;

                    Thread clientThread = new Thread(() => HandleClient(client, clientIP));
                    clientThread.IsBackground = true;
                    clientThread.Start();

                    SendCommand(client, CommandType.Lock, 0);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Error accepting clients: {ex.Message}"));
            }
        }

        private void HandleClient(TcpClient client, string clientIP)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (message.StartsWith("TIME_UPDATE:"))
                    {
                        string timeString = message.Substring("TIME_UPDATE:".Length);
                        if (int.TryParse(timeString, out int remainingTime))
                        {
                            Dispatcher.Invoke(() => UpdateClientRemainingTime(clientIP, remainingTime));
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() => UpdateClientStatus(clientIP, message));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException || ex is IOException))
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error handling client {clientIP}: {ex.Message}"));
                }
            }
            finally
            {
                client.Close();
                Dispatcher.Invoke(() => RemoveClient(clientIP));
            }
        }


        private void UpdateClientRemainingTime(string clientIP, int remainingTime)
        {
            var client = clients.FirstOrDefault(c => c.ClientIP == clientIP);
            if (client != null)
            {
                client.RemainingTime = remainingTime;
            }

            //  Ja beidzas laiks tad statusa nosaukums nomainās uz Locked
            if (remainingTime == 0)
            {
                client.Status = "Bloķēts";
                refreshDataGrid();
            }

        }
        private void SetNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                string currentName = NameStorage.GetName(selectedClient.ClientIP);
                SetNameDialog dialog = new SetNameDialog(currentName);
                if (dialog.ShowDialog() == true)
                {
                    string newName = dialog.ClientName;
                    NameStorage.SaveName(selectedClient.ClientIP, newName);
                    selectedClient.Name = newName;
                }
            }
        }


        private void UpdateClientStatus(string clientIP, string status)
        {
            var client = clients.FirstOrDefault(c => c.ClientIP == clientIP);
            if (client != null)
            {
                client.Status = status;
                client.Name = NameStorage.GetName(clientIP);
            }
        }


        private void RemoveClient(string clientIP)
        {
            var client = clients.FirstOrDefault(c => c.ClientIP == clientIP);
            if (client != null)
            {
                clients.Remove(client);
            }

            if (clientConnections.ContainsKey(clientIP))
            {
                clientConnections.Remove(clientIP);
            }
        }


        private void SendCommand(TcpClient client, CommandType commandType, int duration)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                string command = $"{commandType}:{duration}";
                byte[] data = Encoding.ASCII.GetBytes(command);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending command: {ex.Message}");
            }
        }


        private void SetTimeButton_Click(object sender, RoutedEventArgs e)
        {



            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                //  Ja tekstboxā neieraksta nekādu laiku tad ieslēdzas infinite time

                if (CustomTimeBox.Text == "" && clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    SendCommand(client, CommandType.StartInfiniteTimer, 0);
                    selectedClient.Status = "Bezgalīgs laiks uzsākts";
                }
                // ja tekstboxā ieraksta laiku tad vai nu pieliekas ierakstītais laiks,  vai arī pieplusojās laiks pie esošā laika.
                else
                {
                    SetTimer(CommandType.StartTimer, Convert.ToInt32(this.CustomTimeBox.Text) * 60 + selectedClient.RemainingTime);

                }
            }
        }

        private void RemoveTimeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    if (int.TryParse(CustomTimeBox.Text, out int customTime) && customTime > 0)
                    {
                        int newDuration = selectedClient.RemainingTime - customTime * 60;
                        if (newDuration < 0)
                        {
                            newDuration = 0;
                        }
                        SetTimer(CommandType.StartTimer, newDuration);
                    }
                    else
                    {
                        MessageBox.Show("Nav ievadīts pareizs skaitlis.");
                    }
                }
            }


        }


        private void Min30Button_Click(object sender, RoutedEventArgs e)
        {
            SetTimer(CommandType.StartTimer, 30 * 60);
        }

        private void Min60Button_Click(object sender, RoutedEventArgs e)
        {
            SetTimer(CommandType.StartTimer, 60 * 60);
        }

        private void Min120Button_Click(object sender, RoutedEventArgs e)
        {
            SetTimer(CommandType.StartTimer, 120 * 60);
        }


        private DispatcherTimer globalTimer; //Atlikušā laika String atjaunināšanai

        private void SetTimer(CommandType commandType, int duration)
        {
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    TimeSpan time = TimeSpan.FromSeconds(duration);
                    string str = time.ToString(@"hh\:mm\:ss");

                    selectedClient.IsTimerActive = true;
                    selectedClient.RemainingTimeString = str;
                    selectedClient.RemainingTime = duration;

                    SendCommand(client, commandType, duration);
                    selectedClient.Status = $"{duration / 60} minūtes pieslēgtas";

                    TimerEllipse.Fill = new SolidColorBrush(Colors.Green);

                    if (selectedClient.ClientTimer != null)
                    {
                        selectedClient.ClientTimer.Stop();
                    }

                    selectedClient.ClientTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };

                    selectedClient.ClientTimer.Tick += (s, e) =>
                    {
                        if (selectedClient.RemainingTime > 0)
                        {
                            selectedClient.RemainingTime--;
                            TimeSpan remainingTimeSpan = TimeSpan.FromSeconds(selectedClient.RemainingTime);
                            selectedClient.RemainingTimeString = remainingTimeSpan.ToString(@"hh\:mm\:ss");
                            ClientsDataGrid.Items.Refresh();
                        }
                        else
                        {
                            selectedClient.ClientTimer.Stop();
                            selectedClient.IsTimerActive = false;
                            TimerEllipse.Fill = new SolidColorBrush(Colors.Transparent);
                        }
                    };
                    selectedClient.ClientTimer.Start();
                }
            }
        }

        private void ClientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool clientSelected = ClientsDataGrid.SelectedItem != null;
            min30.IsEnabled = clientSelected;
            min60.IsEnabled = clientSelected;
            min120.IsEnabled = clientSelected;

           
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                if (!clientSelected)
                {
                    TimerEllipse.Fill = new SolidColorBrush(Colors.Transparent);
                }
                var client = clients.FirstOrDefault();
                if (client.Status == "Bloķēts" || client.RemainingTime < 0)
                {
                    TimerEllipse.Fill = new SolidColorBrush(Colors.Transparent);
                }
                else if(client.RemainingTime > 0)
                {
                    TimerEllipse.Fill = new SolidColorBrush(Colors.Green);
                }
            }

        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    SendCommand(client, CommandType.Lock, 0);
                    selectedClient.Status = "Bloķēts";
                    selectedClient.RemainingTime = 0;

                    TimerEllipse.Fill = new SolidColorBrush(Colors.Transparent);

                    if (selectedClient.ClientTimer != null)
                    {
                        selectedClient.ClientTimer.Stop();
                        selectedClient.ClientTimer = null;
                    }

                    refreshDataGrid();
                    selectedClient.RemainingTimeString = "00:00:00";
                }
            }
        }

        private void refreshDataGrid()
        {
            ClientsDataGrid.ItemsSource = null;        //  Refresho datagridu
            ClientsDataGrid.ItemsSource = clients;

            var view = CollectionViewSource.GetDefaultView(ClientsDataGrid.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Nosaukums", ListSortDirection.Ascending));
                view.Refresh();
            }

            // Disable sorting on all columns except the Name column
            foreach (var column in ClientsDataGrid.Columns)
            {
                if (column.Header.ToString() == "Nosaukums")
                {
                    column.CanUserSort = true;
                }
                else
                {
                    column.CanUserSort = false;
                }
            }
        }
        public class ClientInfo : INotifyPropertyChanged
        {
            private string clientIP;
            private string status;
            private string name;
            private bool isTimerActive;
            private int remainingTime;
            private string remainingTimeString;
            private DispatcherTimer clientTimer;

            public string ClientIP
            {
                get => clientIP;
                set
                {
                    clientIP = value;
                    OnPropertyChanged(nameof(ClientIP));
                }
            }

            public string Status
            {
                get => status;
                set
                {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }

            public string RemainingTimeString
            {
                get => remainingTimeString;
                set
                {
                    remainingTimeString = value;
                    OnPropertyChanged(nameof(remainingTimeString));
                }
            }


            public string Name
            {
                get => name;
                set
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }

            public bool IsTimerActive
            {
                get => isTimerActive;
                set
                {
                    isTimerActive = value;
                    OnPropertyChanged(nameof(IsTimerActive));
                }
            }

            public int RemainingTime
            {
                get => remainingTime;
                set
                {
                    remainingTime = value;
                    OnPropertyChanged(nameof(RemainingTime));
                }
            }

            public DispatcherTimer ClientTimer
            {
                get => clientTimer;
                set
                {
                    clientTimer = value;
                    OnPropertyChanged(nameof(ClientTimer));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                //  Ja tekstboxā neieraksta nekādu laiku tad ieslēdzas infinite time

                if (CustomTimeBox.Text == "" && clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    SendCommand(client, CommandType.StartInfiniteTimer, 0);
                    selectedClient.Status = "Infinite Timer Started";
                }
                // ja tekstboxā ieraksta laiku tad vai nu pieliekas ierakstītais laiks,  vai arī pieplusojās laiks pie esošā laika.
                else
                {
                    SetTimer(CommandType.StartTimer, Convert.ToInt32(this.CustomTimeBox.Text) * 60 + selectedClient.RemainingTime);

                }
            }
        }

        private void ShutDownPC_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    SendCommand(client, CommandType.ShutDown, 0);
                }
            }
        }

        public enum CommandType
        {
            Lock,
            Unlock,
            SetTimeLimit,
            StartInfiniteTimer,
            StartTimer,
            ShutDown,
            SendMessage
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    SendCommand(client, CommandType.SendMessage, 0);
                }
            }
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            if (MessageBox.Show(this, "Aizvērt logu?", "Apstiprinājums", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                cancelEventArgs.Cancel = true;
            }
        }

        private void SendMessageText_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SelectAllPcsShtdw_Checked(object sender, RoutedEventArgs e)
        {
            ClientsDataGrid.SelectAll();
        }

        private void ClientsDataGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;

            while (clickedElement != null && !(clickedElement is DataGridRow))
            {
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }
            if (clickedElement == null)
            {
                ClientsDataGrid.SelectedItem = null;
                TimerEllipse.Fill = new SolidColorBrush(Colors.Transparent);
            }
        }
    }
}