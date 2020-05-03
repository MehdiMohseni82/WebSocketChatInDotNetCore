using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
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

namespace DotNetCoreSimpleChatViaWebSocket.UI.Wpf
{
    public partial class MainWindow : Window
    {
        private ClientWebSocket _client;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSend_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtMessageToSend.Text))
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(TxtMessageToSend.Text);

            Task.Run(async () =>
            {
                await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            });
        }

        private async void BtnConnect_OnClick(object sender, RoutedEventArgs e)
        {
            await RunWebSocket();
        }

        private async Task RunWebSocket()
        {
            try
            {
                _client = new ClientWebSocket();
                await _client.ConnectAsync(new Uri($"wss://{TxtServerAddress.Text}/chat/{Guid.NewGuid()}"), CancellationToken.None);

                BtnSend.IsEnabled = true;
                TxtMessageToSend.IsEnabled = true;
                BtnConnect.IsEnabled = false;
                TxtServerAddress.IsEnabled = false;

                var receiving = Listen(_client);
                await Task.WhenAll(receiving);
            }
            catch (WebSocketException exception)
            {
                BtnSend.IsEnabled = false;
                TxtMessageToSend.IsEnabled = false;
                BtnConnect.IsEnabled = true;
                TxtServerAddress.IsEnabled = true;

                MessageBox.Show("Cannot connect to the server!");
            }
        }

        private async Task Listen(ClientWebSocket client)
        {
            var buffer = new byte[1024 * 4];

            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    TxtMessages.Text = $"{TxtMessages.Text} \r\n {message}";
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
    }
}