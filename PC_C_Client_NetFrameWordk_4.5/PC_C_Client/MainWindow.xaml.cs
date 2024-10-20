using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

namespace PC_C_Client
{
    /// <summary>
    /// Datoru kontroles "klients", kas atbildīgs par lietotāju datoru izmantošanas piekļuvi.
    /// šī programma savienojas ar kontroles programmu un pēc noklusējuma aizslēdz datoru, pārklājot ekrānu ar rectangle elementu. Ja tiek ieslēgts 
    /// laiks, parādās cits mazāks rectangle lements ar taimeri.
    /// 
    /// !!!!! Pagaidām klienta programma veido savienojumu cur absolūto ip adresi, kas ir lasītavas datoram izmantojot 5000 portu.
    /// 
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private DispatcherTimer timer;
        private DispatcherTimer connectionTimer;
        private bool isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            HideWindow();
            InitializeOverlay();
            StartConnectionAttempts();
            //this.Topmost = true;


            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
        }

        private void HideWindow()
        {
            this.Visibility = Visibility.Collapsed;
            this.ShowInTaskbar = false;
        }





        private void InitializeOverlay()
        {
            LockScreenImage.Visibility = Visibility.Collapsed;
            TimerRectangle.Visibility = Visibility.Collapsed;
            TimerTextBlock.Visibility = Visibility.Collapsed;
            bibliotekasLogo.Visibility = Visibility.Collapsed;
            EndTimeTextBlock.Visibility = Visibility.Collapsed;
            EndTimerRectangle.Visibility = Visibility.Collapsed;
            PazinojumaText.Visibility = Visibility.Collapsed;
            PazinojumaButton.Visibility = Visibility.Collapsed;
            AdminButton.Visibility = Visibility.Collapsed;
            LaiksBeidzas.Visibility = Visibility.Collapsed;
        }

        private void StartConnectionAttempts()
        {
            connectionTimer = new DispatcherTimer();
            connectionTimer.Interval = TimeSpan.FromSeconds(5);
            connectionTimer.Tick += (s, e) => ConnectToServer();
            connectionTimer.Start();
        }


        private void ConnectToServer()
        {
            if (isConnected)
                return;

            try
            {
                string adminIPAddress = File.ReadAllText(@"IP.txt");
                client = new TcpClient(adminIPAddress, 5000);
                isConnected = true;
                connectionTimer.Stop();
                Thread clientThread = new Thread(HandleServerCommunication);
                clientThread.IsBackground = true;
                clientThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to server: {ex.Message}");
            }
        }

        private void HandleServerCommunication()
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    string[] parts = message.Split(':');
                    CommandType commandType = (CommandType)Enum.Parse(typeof(CommandType), parts[0]);
                    int duration = int.Parse(parts[1]);

                    Dispatcher.Invoke(() => ExecuteCommand(commandType, duration));
                }
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException || ex is IOException))
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error communicating with server: {ex.Message}"));
                }
                ReconnectToServer();
            }
            finally
            {
                if (client != null)
                {
                    client.Close();
                    isConnected = false;
                }
            }
        }

        private void ReconnectToServer()
        {
            isConnected = false;
            StartConnectionAttempts();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                ReconnectToServer(); 
            }
        }

        private DispatcherTimer checkTimer;

        private void ExecuteCommand(CommandType commandType, int duration)
        {
            switch (commandType)
            {
                case CommandType.Lock:
                    LockComputer();
                    break;
                case CommandType.Unlock:
                    UnlockComputer(duration);
                    break;
                case CommandType.StartInfiniteTimer:
                    StartInfiniteTimer();
                    break;
                case CommandType.StartTimer:
                    StartTimer(duration);
                    break;
                case CommandType.SendMessage:
                    ShowCustomMessage();
                    break;
                case CommandType.ShutDown:
                    ShutDownComputer();
                    break;
            }
        }

        private void StopTimer()
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
        }
        private void LockComputer()
        {
            LockScreenImage.Visibility = Visibility.Visible;
            TimerRectangle.Visibility = Visibility.Collapsed;
            TimerTextBlock.Visibility = Visibility.Collapsed;
            EndTimerRectangle.Visibility = Visibility.Collapsed;
            EndTimeTextBlock.Visibility = Visibility.Collapsed;
            PazinojumaText.Visibility = Visibility.Collapsed;
            PazinojumaButton.Visibility = Visibility.Collapsed;
            bibliotekasLogo.Visibility = Visibility.Visible;
            AdminButton.Visibility = Visibility.Visible;
            LaiksBeidzas.Visibility = Visibility.Collapsed;
            StopTimer();
            //KeyboardHook.SetHook();   ///////////////////////////////Nobloķē tastatūru, izņemot ctrl alt delete

            if (checkTimer != null)
            {
                checkTimer.Stop();
                checkTimer = null;
            }
        }


        private void ShutDownComputer()
        {


            string filename = "ShutPC.bat";
            string parameters = $"/k \"{filename}\"";
            Process.Start("cmd", parameters);
        }
        private void ShowCustomMessage()
        {
            string pazinojums = "Lūdzu uzvedieties klusāk!";
            EndTimerRectangle.Visibility = Visibility.Visible;
            PazinojumaText.Visibility = Visibility.Visible;
            PazinojumaButton.Visibility = Visibility.Visible;
            PazinojumaText.Text = pazinojums; 

        }

        private void UnlockComputer(int duration)
        {
            LockScreenImage.Visibility = Visibility.Collapsed;
            TimerRectangle.Visibility = Visibility.Collapsed;
            TimerTextBlock.Visibility = Visibility.Collapsed;
            bibliotekasLogo.Visibility = Visibility.Collapsed;
            EndTimerRectangle.Visibility = Visibility.Collapsed;
            EndTimeTextBlock.Visibility = Visibility.Collapsed;
            PazinojumaText.Visibility = Visibility.Collapsed;
            PazinojumaButton.Visibility = Visibility.Collapsed;
            AdminButton.Visibility = Visibility.Collapsed;
            LaiksBeidzas.Visibility = Visibility.Collapsed;



            //KeyboardHook.RemoveHook();      ///////////////////////////////Nobloķē tastatūru, izņemot ctrl alt delete
            StartCheckTimer();
        }
        private void CloseParoleLogs()
        {
         
        }


        private void StartInfiniteTimer()
        {


            LockScreenImage.Visibility = Visibility.Collapsed;
            TimerRectangle.Visibility = Visibility.Visible;
            TimerTextBlock.Visibility = Visibility.Visible;
            EndTimerRectangle.Visibility = Visibility.Collapsed;
            EndTimeTextBlock.Visibility = Visibility.Collapsed;
            PazinojumaText.Visibility = Visibility.Collapsed;
            PazinojumaButton.Visibility = Visibility.Collapsed;
            bibliotekasLogo.Visibility = Visibility.Collapsed;
            AdminButton.Visibility = Visibility.Collapsed;
            LaiksBeidzas.Visibility = Visibility.Collapsed;

            StopTimer();
            KeyboardHook.RemoveHook();      ///////////////////////////////Nobloķē tastatūru, izņemot ctrl alt delete

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {

                TimerTextBlock.Text = DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss");
                //SendTimeUpdate(DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss")); // Send current time as infinite timer update
            };
            timer.Start();
            StartCheckTimer();
        }




        private void StartTimer(int duration)
        {
            LockScreenImage.Visibility = Visibility.Collapsed;
            TimerRectangle.Visibility = Visibility.Visible;
            TimerTextBlock.Visibility = Visibility.Visible;
            bibliotekasLogo.Visibility = Visibility.Collapsed;
            EndTimerRectangle.Visibility = Visibility.Collapsed;
            EndTimeTextBlock.Visibility = Visibility.Collapsed;
            PazinojumaText.Visibility = Visibility.Collapsed;
            PazinojumaButton.Visibility = Visibility.Collapsed;
            AdminButton.Visibility = Visibility.Collapsed;
            LaiksBeidzas.Visibility = Visibility.Collapsed;

            KeyboardHook.RemoveHook();      ///////////////////////////////Nobloķē tastatūru, izņemot ctrl alt delete

            StopTimer();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                duration--;

                if(duration < 300)
                {
                    EndTimerRectangle.Visibility = Visibility.Visible;
                    EndTimeTextBlock.Visibility = Visibility.Visible;
                    EndTimeTextBlock.Text = "Jūsu laiks beigsies pēc 5 minūtēm!";
                }
                if (duration < 292)
                {
                    EndTimerRectangle.Visibility = Visibility.Collapsed;
                    EndTimeTextBlock.Visibility = Visibility.Collapsed;
                    
                }

                if (duration < 60)
                {
                    EndTimerRectangle.Visibility = Visibility.Visible;
                    LaiksBeidzas.Visibility = Visibility.Visible;
                }
                if (duration < 50)
                {
                    EndTimerRectangle.Visibility = Visibility.Collapsed;
                    LaiksBeidzas.Visibility = Visibility.Collapsed;

                }

                if (duration < 0)
                {

                    timer.Stop();
                    LockComputer();

                    
                    
                }
                else
                {
                    TimerTextBlock.Text = TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss");

                    SendTimeUpdateToAdmin(duration);
                }
            };
            timer.Start();
            StartCheckTimer();
        }

        private void StartCheckTimer()
        {
            if (checkTimer == null)
            {
                checkTimer = new DispatcherTimer();
                checkTimer.Interval = TimeSpan.FromSeconds(3); //Check every 3 seconds
                checkTimer.Tick += (s, e) =>
                {
                    EnsureKeyboardUnlocked();
                };
                checkTimer.Start();
            }
        }
        private void EnsureKeyboardUnlocked()
        {
            if (LockScreenImage.Visibility == Visibility.Collapsed)
            {
                KeyboardHook.RemoveHook(); // Ensure the keyboard is unlocked
            }
        }

        private void SendTimeUpdateToAdmin(int remainingTime)
        {
            if (client != null && client.Connected)
            {
                NetworkStream stream = client.GetStream();
                string message = $"TIME_UPDATE:{remainingTime}";
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool BlockInput(bool fBlockIt);



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

        private void Grid_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var window = (Window)sender;
            window.Topmost = true;
        }

        private void Window_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var window = (Window)sender;
            window.Topmost = true;
        }

        private void PazinojumaButton_Click(object sender, RoutedEventArgs e)
        {
            EndTimerRectangle.Visibility = Visibility.Collapsed;
            PazinojumaText.Visibility = Visibility.Collapsed;
            PazinojumaButton.Visibility = Visibility.Collapsed;
        }


        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Parole PR = new Parole();
            PR.Show();
        }

    }



    public static class KeyboardHook
    {
        private static IntPtr hookId = IntPtr.Zero;
        private static LowLevelKeyboardProc proc = HookCallback;

        public static void SetHook()
        {
            //hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
        }

        public static void RemoveHook()
        {
            //UnhookWindowsHookEx(hookId);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Intercept and suppress key press
                return (IntPtr)1; // Block the key press
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private const int WH_KEYBOARD_LL = 13;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}