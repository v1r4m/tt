using System;
using System.IO;
using System.Windows;
using Tweetinvi;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Media;
//using System.Windows.Forms.KeyEventARgs;
using System.Windows.Input;

namespace tt{
    public partial class LogWindow : Window{

        private TwitterClient ac = new TwitterClient(APIkeys.consid, APIkeys.conskey); //my API
        private string pc;
        private SemaphoreSlim signal = new SemaphoreSlim(0,1);
        private Stack<char> pinStack = new Stack<char>();

        public LogWindow(){
            InitializeComponent();
            connectT1();
//            FontList();
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

        tbl.Text="Congratulation you have authenticated the user: " + user; //now do something with userClient
        }

        private void OnButtonSend(object sender, RoutedEventArgs e)
        {
            signal.Release();
            pc = Pincode.Text;
        }
        private void OnCloseClicked(object sender, RoutedEventArgs e){
            Close();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e){
            // if((e.KeyValue >= 48 && e.KeyValue <= 57) || (e.KeyValue >= 96 && e.KeyValue <= 105)){
            //     tbl.Text=e.KeyValue.ToString(); 
            // }
            int tmpkey = (int)e.Key;
            if(pinStack.Count==6 && tmpkey==6){ //line feed(lf)
                OnCloseClicked(sender, e);
                return;
            }
            if(pinStack.Count<6){
                if((tmpkey >= 34 && tmpkey <= 43)){
                pinStack.Push(((char)(tmpkey+14)));
                return;
                }
                if((tmpkey >= 72 && tmpkey <= 83)){
                pinStack.Push(((char)(tmpkey+22)));
                return;
                }
            }
            if(pinStack.Count>0&&tmpkey==2){//backspace(bs)
                pinStack.Pop();
                return;
            } 
            return;
        }
    }
}