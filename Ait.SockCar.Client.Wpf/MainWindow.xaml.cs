using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Ait.SockCar.Client.Core.SocketHelpers;

using System.Net;
using System.Net.Sockets;
using System.Windows.Threading;

namespace Ait.SockCar.Client.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Socket socket;
        IPEndPoint serverEndpoint;
        //DispatcherTimer timer;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(1);
            //timer.Tick += Timer_Tick;
            StartupConfig();
        }
        //public static void DoEvents()
        //{
        //    System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        //}


        private void StartupConfig()
        {
            cmbIPs.ItemsSource = IPv4.GetActiveIP4s();
            for (int port = 49200; port <= 49500; port++)
            {
                cmbPorts.Items.Add(port);
            }
            Config.GetConfig(out string myIP, out string serverIP, out int savedPort, out string myName);
            try
            {
                cmbIPs.SelectedItem = myIP;
            }
            catch
            {
                cmbIPs.SelectedItem = "127.0.0.1";
            }
            try
            {
                cmbPorts.SelectedItem = savedPort;
            }
            catch
            {
                cmbPorts.SelectedItem = 49200;
            }
            txtServerIP.Text = serverIP;
            txtMyCar.Text = myName;
            btnConnectToServer.Visibility = Visibility.Visible;
            btnDisconnectFromServer.Visibility = Visibility.Hidden;
            grpMyCar.Visibility = Visibility.Hidden;
        }

        private void btnConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            Config.WriteConfig(cmbIPs.SelectedItem.ToString(), txtServerIP.Text, int.Parse(cmbPorts.SelectedItem.ToString()), txtMyCar.Text);
            btnConnectToServer.Visibility = Visibility.Hidden;
            btnDisconnectFromServer.Visibility = Visibility.Visible;
            grpMyCar.Visibility = Visibility.Visible;

            cmbIPs.IsEnabled = false;
            cmbPorts.IsEnabled = false;
            txtMyCar.IsEnabled = false;
            txtServerIP.IsEnabled = false;

            btnStartEngine.Visibility = Visibility.Visible;
            btnStopEngine.Visibility = Visibility.Hidden;
            sldSpeed.IsEnabled = false;

            ContactServer();

        }

        private void ContactServer()
        {
            IPAddress serverIP = IPAddress.Parse(txtServerIP.Text);
            string myIP = cmbIPs.SelectedItem.ToString();
            int serverPort = int.Parse(cmbPorts.SelectedItem.ToString());
            string myName = txtMyCar.Text;

            serverEndpoint = new IPEndPoint(serverIP, serverPort);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string message = "IDENTIFICATION=" + myName + "##EOM";
            lblMyID.Content = SendMessageToServerWaitOnResponse(message);
        }
        private void btnDisconnectFromServer_Click(object sender, RoutedEventArgs e)
        {
            string message = "ID=" + lblMyID.Content + "|BYEBYE##EOM";
            SendMessageToServerDontWaitOnResponse(message);

            btnConnectToServer.Visibility = Visibility.Visible;
            btnDisconnectFromServer.Visibility = Visibility.Hidden;
            grpMyCar.Visibility = Visibility.Hidden;
            lblMyID.Content = "";

            cmbIPs.IsEnabled = true;
            cmbPorts.IsEnabled = true;
            txtMyCar.IsEnabled = true;
            txtServerIP.IsEnabled = true;

            //timer.Stop();

        }
        private void sldSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int speed = (int)sldSpeed.Value;
            string message = "ID=" + lblMyID.Content + "|SPEED=" + speed.ToString() + "##EOM";
            lblDistance.Content = SendMessageToServerWaitOnResponse(message);
        }
        private void btnGetDistance_Click(object sender, RoutedEventArgs e)
        {
            string message = "ID=" + lblMyID.Content + "|FETCH##EOM";
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            lblDistance.Content = SendMessageToServerWaitOnResponse(message);

        }
        private void btnStartEngine_Click(object sender, RoutedEventArgs e)
        {
            sldSpeed.Value = 0.0;
            string message = "ID=" + lblMyID.Content + "|START##EOM";
            btnStartEngine.Visibility = Visibility.Hidden;
            btnStopEngine.Visibility = Visibility.Visible;
            sldSpeed.IsEnabled = true;
            lblDistance.Content = SendMessageToServerWaitOnResponse(message);
        }
        private void btnStopEngine_Click(object sender, RoutedEventArgs e)
        {
            sldSpeed.Value = 0.0;
            string message = "ID=" + lblMyID.Content + "|STOP##EOM";
            btnStartEngine.Visibility = Visibility.Visible;
            btnStopEngine.Visibility = Visibility.Hidden;
            sldSpeed.IsEnabled = false;
            lblDistance.Content = SendMessageToServerWaitOnResponse(message);
        }
        private void SendMessageToServerDontWaitOnResponse(string message)
        {
            lstOut.Items.Insert(0, message);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(serverEndpoint);
                byte[] outMessage = Encoding.ASCII.GetBytes(message);
                socket.Send(outMessage);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }
            catch
            {
                btnDisconnectFromServer_Click(null, null);
            }
        }
        private string SendMessageToServerWaitOnResponse(string message)
        {
            lstOut.Items.Insert(0, message);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(serverEndpoint);
                byte[] outMessage = Encoding.ASCII.GetBytes(message);
                byte[] inMessage = new byte[8000];

                socket.Send(outMessage);
                int responseLength = socket.Receive(inMessage);
                string response = Encoding.ASCII.GetString(inMessage, 0, responseLength).ToUpper().Trim();
                lstIn.Items.Insert(0, response);
                //lblDistance.Content = response;
                return response;

            }
            catch (Exception fout)
            {
                btnDisconnectFromServer_Click(null, null);
                return "ERROR";
            }
            finally
            {
                if (socket != null)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    socket = null;
                }
            }
            
        }


        //private void Timer_Tick(object sender, EventArgs e)
        //{
        //    string message = "ID=" + lblMyID.Content + "|FETCH##EOM";
        //    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //    SendMessageToServerWaitOnResponse(message);
        //    DoEvents();
        //}
    }
}
