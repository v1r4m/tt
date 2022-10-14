using System;
using System.IO;
using System.Windows;
using Tweetinvi;
using System.Diagnostics;
using System.Threading;

namespace tt{
    public partial class LogWindow : Window{

        TwitterClient ac = new TwitterClient(APIkeys.consid, APIkeys.conskey);
        private string pc;
        private SemaphoreSlim signal = new SemaphoreSlim(0,1);

        public LogWindow(){
            InitializeComponent();
            connectT1();
        }
        public async void connectT1(){
        // Start the authentication process
        var authenticationRequest = await ac.Auth.RequestAuthenticationUrlAsync();

        // Go to the URL so that Twitter authenticates the user and gives him a PIN code.
        Process.Start(new ProcessStartInfo(authenticationRequest.AuthorizationURL)
        {
            UseShellExecute = true
        });

        await signal.WaitAsync();
        var userCredentials = await ac.Auth.RequestCredentialsFromVerifierCodeAsync(pc, authenticationRequest);
        var userClient = new TwitterClient(userCredentials);
        var user = await userClient.Users.GetAuthenticatedUserAsync();

        tbl.Text="Congratulation you have authenticated the user: " + user;
        }

        private void OnButtonSend(object sender, RoutedEventArgs e)
        {
            signal.Release();
            pc = Pincode.Text;
        }
    }
}