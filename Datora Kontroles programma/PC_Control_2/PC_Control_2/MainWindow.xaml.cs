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
using System.Data;
using System.Timers;
using Xceed.Wpf.Toolkit;
using ControlzEx.Theming;

namespace PC_Control_2
{
    /// <summary>
    /// Datoru kontroles programma, kas spēj noteikt laiku un pieslēgt datora izmantošanu, sūtot komandas uz klienta programmu.
    /// 
    /// Ar programmu var ieslēgt 30 minūtes, 1 studnu, 2 stundas vai bezgalīgu laiku datoriem.
    /// 
    /// Papildus informācija atroda dokumentacijā.
    /// </summary>
    public partial class MainWindow
    {
        ///****** Vispārīgie objekti, galvenā klase klientiem un sākuma inicializācijas ******///

        public class ClientInfo : INotifyPropertyChanged
        {
            private string clientIP;
            private string status;
            private string name;
            private bool isTimerActive;
            private int remainingTime;
            private string remainingTimeString;
            private DispatcherTimer clientTimer;
            private double moneyCounter;

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

            public double MoneyCounter
            {
                get => moneyCounter;
                set
                {
                    moneyCounter = value;
                    OnPropertyChanged(nameof(MoneyCounter));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        ///Izvedotais serveris tīklā. Ja savienojas klients, tas tiek ielikts klases sarakstā
        private TcpListener server;
        private ObservableCollection<ClientInfo> clients;
        private Dictionary<string, TcpClient> clientConnections;


        public bool closeSetting;
        public bool moneySetting;
        public double moneyMultiplier;

        public MainWindow()
        {
            InitializeComponent();
            clients = new ObservableCollection<ClientInfo>();
            clientConnections = new Dictionary<string, TcpClient>();
            ClientsDataGrid.ItemsSource = clients;
            StartServer();
            LoadClientNames(); ///Ielādē saglabātos nosaukumus no saraksta
        }



        ///****** Servera uzsākšana un klientu saņemšana/uzturēšana ******///

        private void StartServer()
        {
            ///Izveido severi, piešķirot tam 5000 portu.
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
                Dispatcher.Invoke(() => System.Windows.MessageBox.Show($"(Admin 1)Error accepting clients: {ex.Message}"));
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
                    Dispatcher.Invoke(() => System.Windows.MessageBox.Show($"(Admin 2)Error handling client {clientIP}: {ex.Message}"));
                }
            }
            finally
            {
                client.Close();
                Dispatcher.Invoke(() => RemoveClient(clientIP));
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



        ///****** Klietu laika un statusa atjaunoana ******///

        private void UpdateClientRemainingTime(string clientIP, int remainingTime)
        {
            var client = clients.FirstOrDefault(c => c.ClientIP == clientIP);
            if (client != null)
            {
                client.RemainingTime = remainingTime;
            }

            ///Ja beidzas laiks tad statusa nosaukums nomainās uz Locked
            if (remainingTime == 0)
            {
                client.Status = "Bloķēts";
                refreshDataGrid();
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



        ///****** Labā peles pogas izvēlnes opcijas ******///

        ///Vārda iestatīšana/maiņa
        private void SetNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ///Izpildot labo klikšķi uz kāda no pieslēgtiem klientiem.
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                ///Saņem pašreizējo vārdu(Ja tāds ir) un nosūta to uz vārda dialogu.
                string currentName = NameStorage.GetName(selectedClient.ClientIP);
                SetNameDialog dialog = new SetNameDialog(currentName);
                if (dialog.ShowDialog() == true)
                {
                    ///Saņem atpakaļ vārdu un pesaista to.
                    string newName = dialog.ClientName;
                    NameStorage.SaveName(selectedClient.ClientIP, newName);
                    selectedClient.Name = newName;
                }
            }
        }

        private void SendRestartMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SendCloseClientMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }



        ///****** Galvenās klietu laika pievienošanas pogas un laika sākšanas/nosūtīšana funkcijas ******///

        public enum CommandType
        {
            ///Komandu sūtīšanas tipi
            Lock,
            Unlock,
            SetTimeLimit,
            StartInfiniteTimer,
            StartTimer,
            ShutDown,
            SendMessage
        }

        private void SendCommand(TcpClient client, CommandType commandType, int duration, string? message = null)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                string command = message == null ? $"{commandType}:{duration}" : $"{commandType}:{duration}:{message}";
                byte[] data = Encoding.ASCII.GetBytes(command);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"(Admin 3)Error sending command: {ex.Message}");
            }
        }

        ///CustomTimeBox elementa laika pielikšanas poga
        private void SetTimeButton_Click(object sender, RoutedEventArgs e)
        {

            foreach (ClientInfo selectedClient in ClientsDataGrid.SelectedItems)
            {
                ///CustomTimeBox elementa bezgalīgā laika pievienošana, atstājot tukšu laika lauku
                if (CustomTimeBox.Text == "" && clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    SendCommand(client, CommandType.StartInfiniteTimer, 0);
                    selectedClient.Status = "Bezgalīgs laiks uzsākts";
                }
                ///Ja CustomTimeBox elementā ieraksta laiku tad vai nu pieliekas ierakstītais laiks, vai arī pieplusojās laiks pie esošā laika
                else
                {
                    SetTimer(CommandType.StartTimer, Convert.ToInt32(this.CustomTimeBox.Text) * 60 + selectedClient.RemainingTime);

                }
            }
              
        }

        ///CustomTimeBox elementa laika noņemšanas poga
        private void RemoveTimeButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (ClientInfo selectedClient in ClientsDataGrid.SelectedItems)
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
                        System.Windows.MessageBox.Show("Nav ievadīts pareizs skaitlis.");
                    }
                }
            }
        }

        ///Galveno laika pogu komandas
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

        private DispatcherTimer globalTimer; ///Atlikušā laika String atjaunināšanai

        ///Laika skaitīšanas uzsākšanai
        private void SetTimer(CommandType commandType, int duration)
        {

            foreach (ClientInfo selectedClient in ClientsDataGrid.SelectedItems)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    TimeSpan time = TimeSpan.FromSeconds(duration);
                    string str = time.ToString(@"hh\:mm\:ss"); ///Atlikuša laika kolonas atjaunošana

                    selectedClient.IsTimerActive = true;
                    selectedClient.RemainingTimeString = str;
                    selectedClient.RemainingTime = duration;

                    SendCommand(client, commandType, duration);
                    selectedClient.Status = $"{duration / 60} minūtes pieslēgtas"; ///Klienta statusa kolonas atjaunošana ar laiku

                    TimerEllipse.Fill = new SolidColorBrush(Colors.Green); ///Pūlksteņa ikonas izgaismošana kad pieszlēdz laiku

                    if (selectedClient.ClientTimer != null) ///Ja ir jau aktīvas laika pieslēgums, to aptur
                    {
                        selectedClient.ClientTimer.Stop();
                    }

                    selectedClient.ClientTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };

                    if (moneySetting) ///Naudas skaitīšanas iestatījuma pārbaude un tā uzsākšana
                    {
                        bool startMoney = true;
                        SetMoney(selectedClient, startMoney);
                    }

                    ///Atlikušā laika aktīvā atjaunošana
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
                            ///Laika beigšanās nosacījumi
                            selectedClient.ClientTimer.Stop();
                            selectedClient.IsTimerActive = false;
                            TimerEllipse.Fill = new SolidColorBrush(Colors.Transparent);
                            if (moneySetting)
                            {
                                bool startMoney = false;
                                SetMoney(selectedClient, startMoney);
                            }
                        }
                    };
                    selectedClient.ClientTimer.Start();
                }
            }

        }

        ///Laika izslēgšanas poga
        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (ClientInfo selectedClient in ClientsDataGrid.SelectedItems)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    SendCommand(client, CommandType.Lock, 0); ///Nosūta bloķēšanas komandu
                    selectedClient.Status = "Bloķēts"; ///Iestata statusa kolonas tekstu
                    selectedClient.RemainingTime = 0;

                    TimerEllipse.Fill = new SolidColorBrush(Colors.Transparent); ///Pūlksteņa ikonas iekrāsošana uz caurspīdīgu

                    if (selectedClient.ClientTimer != null)
                    {
                        selectedClient.ClientTimer.Stop();
                        selectedClient.ClientTimer = null;
                    }

                    selectedClient.RemainingTimeString = "00:00:00"; ///Nomaina atlikušo laiku uz tukšu

                    if (moneySetting)
                    {
                        bool startMoney = false;
                        SetMoney(selectedClient, startMoney);
                    }
                }
            }
            refreshDataGrid(); ///Atjauno klientu saraksta elementu

            ///Visu klientu atzīmēšanas ķekša noņemšana ja izslēdz vairakus datorus
            if (SelectAllPcsShtdw.IsChecked == true)
            {
                SelectAllPcsShtdw.IsChecked = false;
            }
        }

        private void ShutDownPC_Click(object sender, RoutedEventArgs e)
        {
            foreach (ClientInfo selectedClient in ClientsDataGrid.SelectedItems)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    SendCommand(client, CommandType.ShutDown, 0);
                }
            }
        }



        ///****** Klietu tabulas lauku atjaunošanās un pārvalde ******///

        private void ClientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool clientSelected = ClientsDataGrid.SelectedItem != null;
            min30.IsEnabled = clientSelected;
            min60.IsEnabled = clientSelected;
            min120.IsEnabled = clientSelected;

            ///Izvēlētā klienta pūlksteņa ikonas attiecīga iekrāsošana un naudas skaitīšanas summas parādīšana
            if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
            {
                if (!clientSelected)
                {
                    TimerEllipse.Fill = new SolidColorBrush(Colors.Transparent);
                    moneySumText.Content = "0,00 €";
                }
                else
                {
                    moneySumText.Content = $"{selectedClient.MoneyCounter:F2} €";
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
       
        private void refreshDataGrid()
        {
            ClientsDataGrid.ItemsSource = null;    
            ClientsDataGrid.ItemsSource = clients;

            var view = CollectionViewSource.GetDefaultView(ClientsDataGrid.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Nosaukums", ListSortDirection.Ascending));
                view.Refresh();
            }

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
                moneySumText.Content = "0,00 €";
            }

            if (SelectAllPcsShtdw.IsChecked == true)
            {
                SelectAllPcsShtdw.IsChecked = false;
            }
        }

        private void SelectAllPcsShtdw_Unchecked(object sender, RoutedEventArgs e)
        {
            ClientsDataGrid.SelectedItem = null;
        }

        private void SelectAllPcsShtdw_Checked(object sender, RoutedEventArgs e)
        {
            ClientsDataGrid.SelectAll();
        }


        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
        //    {
        //        //  Ja tekstboxā neieraksta nekādu laiku tad ieslēdzas infinite time

        //        if (CustomTimeBox.Text == "" && clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
        //        {
        //            SendCommand(client, CommandType.StartInfiniteTimer, 0);
        //            selectedClient.Status = "Infinite Timer Started";
        //        }
        //        // ja tekstboxā ieraksta laiku tad vai nu pieliekas ierakstītais laiks,  vai arī pieplusojās laiks pie esošā laika.
        //        else
        //        {
        //            SetTimer(CommandType.StartTimer, Convert.ToInt32(this.CustomTimeBox.Text) * 60 + selectedClient.RemainingTime);

        //        }
        //    }
        //}





        ///****** Ziņojumu sūtīšana ******///

        public string customTextMessageToSend = "Noklusejums Administratora";

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            foreach (ClientInfo selectedClient in ClientsDataGrid.SelectedItems)
            {
                if (clientConnections.TryGetValue(selectedClient.ClientIP, out TcpClient client))
                {
                    string? messageToSend = !string.IsNullOrWhiteSpace(customTextMessageToSend) ? customTextMessageToSend : "Uzvesties Klusak!";
                    SendCommand(client, CommandType.SendMessage, 0, messageToSend);
                }
            }
        }

        private void SendMessageText_Click(object sender, RoutedEventArgs e)
        {
            SetCustomMessageDialog dialog = new SetCustomMessageDialog();
            if (dialog.ShowDialog() == true)
            {
                customTextMessageToSend = dialog.customTextMessage;
            }
        }



        ///****** Izvēlnes joslas Programmas iestatījumi ******///

        ///Aizvēršanas loga apstiprinājums
        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            if (System.Windows.MessageBox.Show(this, "Aizvērt logu?", "Apstiprinājums", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                cancelEventArgs.Cancel = true;
            }
        }

        private void IpColumnVisible_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            bool isChecked = menuItem.IsChecked;
            Properties.Settings.Default.ipColumnVisability = isChecked;
            Properties.Settings.Default.Save();
            ClientsDataGrid.Columns[2].Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void StatusColumnVisible__Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            bool isChecked = menuItem.IsChecked;
            Properties.Settings.Default.statusColumnVisable = isChecked;
            Properties.Settings.Default.Save();
            ClientsDataGrid.Columns[3].Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        ///Uz programmas ieslēgšanu, ielādē saglabātās lietotaja izvēles elementiem
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Application Loaded");
            ThemeManager.Current.ChangeTheme(this, "Dark.Blue");

            ClientsDataGrid.Columns[2].Visibility = Properties.Settings.Default.ipColumnVisability ? Visibility.Visible : Visibility.Collapsed;
            ClientsDataGrid.Columns[3].Visibility = Properties.Settings.Default.statusColumnVisable ? Visibility.Visible : Visibility.Collapsed;
            closeSetting = Properties.Settings.Default.closeConfirm;
            moneySetting = Properties.Settings.Default.moneyConfirm;
            IPColumnVisible_.IsChecked = Properties.Settings.Default.ipColumnVisability;
            StatusColumnVisible_.IsChecked = Properties.Settings.Default.statusColumnVisable;
            MoneyConfirm_.IsChecked = Properties.Settings.Default.moneyConfirm;

            if (moneySetting)
            {
                MoneyGrid.Visibility = Visibility.Visible;
                MoneyGridBorder.Visibility = Visibility.Visible;           
            }
            else
            {
                MoneyGrid.Visibility = Visibility.Hidden;
                MoneyGridBorder.Visibility = Visibility.Hidden;
            }
            CloseConfirm_.IsChecked = Properties.Settings.Default.closeConfirm;
            if (closeSetting)
            {
                Closing += OnClosing;
            }
            else
            {
                Closing -= OnClosing;
            }

            ///Primārās krāsas ielāde
            var primaryColorString = Properties.Settings.Default.PrimaryColor;
            if (!string.IsNullOrEmpty(primaryColorString))
            {
                var primaryColor = (Color)ColorConverter.ConvertFromString(primaryColorString);
                Application.Current.Resources["PrimaryColor"] = new SolidColorBrush(primaryColor);
                ColPicker.SelectedColor = primaryColor;
                Console.WriteLine($"Loaded PrimaryColor: {primaryColor}");
            }
            else
            {
                Console.WriteLine("No PrimaryColor saved; using default.");
            }

            ///Sekundārās krāsas ielāde
            var secondaryColorString = Properties.Settings.Default.SecondaryColor;
            if (!string.IsNullOrEmpty(secondaryColorString))
            {
                var secondaryColor = (Color)ColorConverter.ConvertFromString(secondaryColorString);
                Application.Current.Resources["SecondaryColor"] = new SolidColorBrush(secondaryColor);
                ColPicker1.SelectedColor = secondaryColor;
                Console.WriteLine($"Loaded SecondaryColor: {secondaryColor}");
            }
            else
            {
                Console.WriteLine("No SecondaryColor saved; using default.");
            }

            ///Ternārās krāsas ielāde
            var ternaryColorString = Properties.Settings.Default.TernaryColor;
            if (!string.IsNullOrEmpty(ternaryColorString))
            {
                var ternaryColor = (Color)ColorConverter.ConvertFromString(ternaryColorString);
                Application.Current.Resources["TernaryColor"] = new SolidColorBrush(ternaryColor);
                ColPicker2.SelectedColor = ternaryColor;
                Console.WriteLine($"Loaded TernaryColor: {ternaryColor}");
            }
            else
            {
                Console.WriteLine("No TernaryColor saved; using default.");
            }
        }

        ///Prasīšanas pirms aizvēršanas dialoga iestatījums
        private void CloseConfirm__Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            bool isChecked = menuItem.IsChecked;
            Properties.Settings.Default.closeConfirm = isChecked;
            Properties.Settings.Default.Save();
            closeSetting = isChecked;
            if (closeSetting)
            {
                Closing += OnClosing;
            }
            else
            {
                Closing -= OnClosing;
            }
        }

        ///Izvēlnes joslas aizvēršana bez prasīšanas dialoga
        private void CloseNow_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        ///Naudas skaitīšanas kkoeficienta izvēlas opcija
        private void MoneyMultiplierDialog__Click(object sender, RoutedEventArgs e)
        {
            double currentMultiplier = Properties.Settings.Default.moneyMultiplier;
            SetMoneyDialog dialog = new SetMoneyDialog(currentMultiplier);
            if (dialog.ShowDialog() == true)
            {
                Properties.Settings.Default.moneyMultiplier = dialog.MoneyMultiplier;
                Properties.Settings.Default.Save();
            }
        }

        ///Naudas skaitīšanas ieslēgšanas opcija. Parādīs skaitīšanas elementu logā
        private void MoneyConfirm__Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            bool isChecked = menuItem.IsChecked;

            Properties.Settings.Default.moneyConfirm = isChecked;
            Properties.Settings.Default.Save();

            moneySetting = isChecked;
            if (moneySetting)
            {
                MoneyGrid.Visibility = Visibility.Visible;
                MoneyGridBorder.Visibility = Visibility.Visible;
                
            }
            else
            {
                MoneyGrid.Visibility = Visibility.Hidden;
                MoneyGridBorder.Visibility = Visibility.Hidden;
            }
        }

        private void SetMoney(ClientInfo client, bool start)
        {
            if (start)
            {
                ///Jo laiks skaita uz leju, pašu nevar izmantot lai uzturētu skaitu. Tapēc ir pagaidu laiks un zemāk tiek izmantot izgājušais laiks, lai izņemtu sekundes priekš formulas
                int InitialDuration = client.RemainingTime;
                client.MoneyCounter = 0;

                ///katrā klienta iekšējā sekundes tikšķi izpilda summas formulu
                if (ClientsDataGrid.SelectedItem is ClientInfo selectedClient)
                {
                    selectedClient.ClientTimer.Tick += (s, e) =>
                    {
                        double elapsedSeconds = InitialDuration - client.RemainingTime;
                        double multiplier = Properties.Settings.Default.moneyMultiplier; ///Ielāde no iestatījumiem koeficientu
                        selectedClient.MoneyCounter = (multiplier * ((100 * elapsedSeconds) / 1800))/100; ///Dala ar 100 jo skaitītājs ir centos

                        ///Atjauno skaitītāju saskarnē
                        if (ClientsDataGrid.SelectedItem == client)
                        {
                            moneySumText.Content = $"{client.MoneyCounter:F2} €";
                        }
                    };
                }
            }
        }



        ///****** Loga krāsas elementu maiņa un saglabāšana ******///

        private void UpdatePrimaryColor(Color newColor)
        {
            Console.WriteLine($"Updating PrimaryColor to: {newColor}");

            Application.Current.Resources["PrimaryColor"] = new SolidColorBrush(newColor);
            Console.WriteLine("NewColorSet: "+ newColor);
            this.DataContext = null;
            this.DataContext = this;
            Properties.Settings.Default.PrimaryColor = newColor.ToString();
            Properties.Settings.Default.Save();
            Console.WriteLine($"PrimaryColor saved as: {Properties.Settings.Default.PrimaryColor}");

        }

        private void UpdateSecondaryColor(Color newColor)
        {
            Application.Current.Resources["SecondaryColor"] = new SolidColorBrush(newColor);
            this.DataContext = null;
            this.DataContext = this;
            Properties.Settings.Default.SecondaryColor = newColor.ToString();
            Properties.Settings.Default.Save();
        }

        private void UpdateTernaryColor(Color newColor)
        {
            Application.Current.Resources["TernaryColor"] = new SolidColorBrush(newColor);
            this.DataContext = null;
            this.DataContext = this;
            Properties.Settings.Default.TernaryColor = newColor.ToString();
            Properties.Settings.Default.Save();
        }

        private void PrimaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                UpdatePrimaryColor(e.NewValue.Value);
            }
        }

        private void SecondaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                UpdateSecondaryColor(e.NewValue.Value);
            }
        }

        private void TernaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                UpdateTernaryColor(e.NewValue.Value);
            }
        }

        ///Noklusējuma krāsu atgriešana
        private void DefaultColors__Click(object sender, RoutedEventArgs e)
        {
            Color primaryColor = (Color)ColorConverter.ConvertFromString("#6a994e");
            Color secondaryColor = (Color)ColorConverter.ConvertFromString("#bc4749");
            Color tertiaryColor = (Color)ColorConverter.ConvertFromString("#f2e8cf");

            UpdatePrimaryColor(primaryColor);
            UpdateSecondaryColor(secondaryColor);
            UpdateTernaryColor(tertiaryColor);
        }


        ///****** CustomTimeBox laika elementa ciparu ievades nosacījumi ******///

        private bool IsTextNumeric(string text)
        {
            return int.TryParse(text, out _);
        }

        private void CustomTimeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text); ///Pārbauda vai tikai teksts tiek ievadīts
        }

        private void CustomTimeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(CustomTimeBox.Text, out _))
            {
                if (CustomTimeBox.Text.Length > 3) ///Lai ievadīt varētu tikai 3 ciparu skaitļus
                {
                    CustomTimeBox.Text = CustomTimeBox.Text.Substring(0, 3);
                    CustomTimeBox.SelectionStart = CustomTimeBox.Text.Length;
                }
            }
            else if (!string.IsNullOrEmpty(CustomTimeBox.Text)) 
            {
                CustomTimeBox.Text = string.Empty; 
            }
        }


    }
}