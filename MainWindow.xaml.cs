using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tweetinvi;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
 
namespace TwitterViewer
{
    // Xaml 의 screenName 입력창과의 DataBinding
    public class ScreenName
    {
        public string Name { get; set; }
    }
 
    // Json 포멧의 response 로부터 bearer token 를 얻기 위한 클래스
    public class TwitAuthenticateResponse
    {
        public string token_type { get; set; }
        public string access_token { get; set; }
    }
 
    // Xaml 의 ListBox item 용 DataBinding
    public class TwitItem
    {
        public string created_at { get; set; }
        public string text { get; set; }
        public string user { get; set; }
        public string profile_image_url { get; set; }
    }
 
    public class TwitItems : ObservableCollection<TwitItem>
    {
    }
 
    public partial class MainWindow : Window
    {
        TwitItems _twitItems = new TwitItems();
        TwitterClient appClient = new TwitterClient(APIkeys.consid, APIkeys.conskey);
        private SemaphoreSlim signal = new SemaphoreSlim(0,1);
        private string pincode;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _twitItems;  // ListBox 와의 DataBinding
            ConnectTwitter(twitScreenName.Text);
            connectT1();
        }
        public async void connectT1(){
            // Start the authentication process
            var authenticationRequest = await appClient.Auth.RequestAuthenticationUrlAsync();

            // Go to the URL so that Twitter authenticates the user and gives him a PIN code.
            Process.Start(new ProcessStartInfo(authenticationRequest.AuthorizationURL)
            {
                UseShellExecute = true
            });

            await signal.WaitAsync();
            var userCredentials = await appClient.Auth.RequestCredentialsFromVerifierCodeAsync(pincode, authenticationRequest);
            var userClient = new TwitterClient(userCredentials);
            var user = await userClient.Users.GetAuthenticatedUserAsync();

            tb.Text="Congratulation you have authenticated the user: " + user;
        }

        public void ConnectTwitter(string screenName)
        {
            _twitItems.Clear();

            var authHeader = string.Format("Basic {0}",
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        Uri.EscapeDataString(APIkeys.consid)
                        + ":"
                        + Uri.EscapeDataString(APIkeys.conskey)
                    )
                )
            );
 
            // Step 2: Obtain a bearer token
            HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create("https://api.twitter.com/oauth2/token");
            authRequest.Headers.Add("Authorization", authHeader);
            authRequest.Method = "POST";
            authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (Stream stream = authRequest.GetRequestStream())
            {
                var postBody = "grant_type=client_credentials";
                byte[] content = ASCIIEncoding.ASCII.GetBytes(postBody);
                stream.Write(content, 0, content.Length);
            }
 
            authRequest.Headers.Add("Accept-Encoding", "gzip");
 
            // deserialize into an object
            TwitAuthenticateResponse twitAuthResponse;
            using (WebResponse authResponse = authRequest.GetResponse())
            {
                using (var reader = new StreamReader(authResponse.GetResponseStream()))
                {
                    var objectText = reader.ReadToEnd();
                    twitAuthResponse = JsonConvert.DeserializeObject<TwitAuthenticateResponse>(objectText);
                }
            }
 
            // Step 3: Authenticate API requests with the bearer token and Do the timeline
            var timelineFormat = "https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name={0}&include_rts=1&exclude_replies=1&count=50";
            var timelineUrl = string.Format(timelineFormat, screenName);
            HttpWebRequest timeLineRequest = (HttpWebRequest)WebRequest.Create(timelineUrl);
            timeLineRequest.Headers.Add("Authorization", string.Format("{0} {1}", twitAuthResponse.token_type, twitAuthResponse.access_token));
            timeLineRequest.Method = "Get";
            JArray twitterLines;
            using (WebResponse timeLineResponse = timeLineRequest.GetResponse())
            {
                using (var reader = new StreamReader(timeLineResponse.GetResponseStream()))
                {
                    twitterLines = JArray.Parse(reader.ReadToEnd());
                }
            }
 
            // Json 은 http://james.newtonking.com/json 를 참고
            // Newtonsoft.Json.dll 을 References 에 추가해야 한다.
            IList<TwitItem> twitList = twitterLines.Select(p => new TwitItem
            {
                created_at = (string)p["created_at"],
                text = (string)p["text"],
                user = (string)p["user"]["name"],
                profile_image_url = (string)p["user"]["profile_image_url"]
            }).ToList();
            twitList.ToList().ForEach(_twitItems.Add);
        }
 
        private void OnButtonCrawling(object sender, RoutedEventArgs e)
        {
            signal.Release();
            pincode = twitScreenName.Text;
//            ConnectTwitter(twitScreenName.Text);
        }
    }
}