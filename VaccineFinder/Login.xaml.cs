using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VaccineFinder
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private async void Resend_OTP_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Error_OTP_Confirm.Visibility = Visibility.Collapsed;
            Timer.Visibility = Visibility.Collapsed;
            if (!string.IsNullOrEmpty(MobileNo.Text))
            {
                if (MobileNo.Text.Length == 10)
                {
                    await SendOTP(MobileNo.Text);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OTP_Panel.Visibility = Visibility.Visible;
            OTP_Confirm_Panel.Visibility = Visibility.Collapsed;
            loginwindow.Height = 240;
            ErrorText.Visibility = Visibility.Collapsed;
            GetOtpBtn.Margin = new Thickness(0, 15, 0, 0);
            MobileNo.Focus();
        }

        private SMS SMSResult;
        private DispatcherTimer timer;
        private int count = 180;

        private async Task SendOTP(string mobile)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://cdn-api.co-vin.in/api/v2/auth/public/generateOTP"))
                {
                    request.Headers.TryAddWithoutValidation("accept", "application/json");

                    request.Content = new StringContent("{\"mobile\":\"" + mobile + "\"}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(request);
                    string jsonContent = response.Content.ReadAsStringAsync().Result;
                    if (IsValidJson(jsonContent))
                    {
                        SMSResult = JsonConvert.DeserializeObject<SMS>(jsonContent);
                        if (!string.IsNullOrEmpty(SMSResult.txnId))
                        {
                            SendSucessOTP();
                        }
                        else
                        {
                            ErrorText.Visibility = Visibility.Visible;
                            ErrorText.Content = SMSResult.error;
                            GetOtpBtn.Margin = new Thickness(0, 5, 0, 0);
                        }
                    }
                    else
                    {
                        ErrorText.Visibility = Visibility.Visible;
                        ErrorText.Content = jsonContent;
                        GetOtpBtn.Margin = new Thickness(0, 5, 0, 0);
                    }
                }
            }
        }

        private void SendSucessOTP()
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += timer_Tick;
            timer.Start();            
            OTP_Panel.Visibility = Visibility.Collapsed;
            OTP_Confirm_Panel.Visibility = Visibility.Visible;
            loginwindow.Height = 260;
            OTP_TextBox.Focusable = true;
            Keyboard.Focus(OTP_TextBox);
            ErrorText.Visibility = Visibility.Collapsed;
            Resend_OTP.Visibility = Visibility.Collapsed;
            SendMobileNo.Text = "XXX XXX " + MobileNo.Text.Substring(6);
            Timer.Visibility = Visibility.Visible;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Timer.Content = count + " sec";
            count--;
            if (count == 1)
            {
                count = 180;
                Timer.Content = count + " sec";
                timer.Stop();
                Timer.Visibility = Visibility.Collapsed;
                Resend_OTP.Visibility = Visibility.Visible;
                Error_OTP_Confirm.Visibility = Visibility.Collapsed;
                OTP_TextBox.Text = "";
            }
        }

        public async Task VerifyOTP(string str)
        {
            var otp = ComputeSha256Hash(str);
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://cdn-api.co-vin.in/api/v2/auth/public/confirmOTP"))
                {
                    request.Headers.TryAddWithoutValidation("accept", "application/json");
                    var content = "{\"otp\":" + "\"" + otp + "\"" + ",\"txnId\":" + "\"" + SMSResult.txnId + "\"" + "}";
                    request.Content = new StringContent(content);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(request);
                    string jsonContent = response.Content.ReadAsStringAsync().Result;
                    var sms = new SMS();
                    if (IsValidJson(jsonContent))
                    {
                        sms = JsonConvert.DeserializeObject<SMS>(jsonContent);
                        if (sms.error == null & string.IsNullOrEmpty(sms.error))
                        {
                            OTP_Confirm();
                        }
                        else
                        {
                            Error_OTP_Confirm.Visibility = Visibility.Visible;
                            Error_OTP_Confirm.Content = sms.error;
                        }
                    }
                    else
                    {
                        Error_OTP_Confirm.Visibility = Visibility.Visible;
                        Error_OTP_Confirm.Content = jsonContent;
                    }
                }
            }
        }

        private void OTP_Confirm()
        {
            timer.Stop();
            Timer.Visibility = Visibility.Collapsed;
            VaccineHelper.Instance.IsUserLogedIn = true;
            VaccineHelper.Instance.UserName = MobileNo.Text;
            this.Close();
        }

        private bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private async void GetOtpBtn_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            if (!string.IsNullOrEmpty(MobileNo.Text))
            {
                if (MobileNo.Text.Length == 10) { await SendOTP(MobileNo.Text); }
                else
                {
                    ErrorText.Visibility = Visibility.Visible;
                    ErrorText.Content = "Must be 10 digits.";
                    GetOtpBtn.Margin = new Thickness(0, 5, 0, 0);
                }
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
                ErrorText.Content = "No Mobile Number is Entered.";
                GetOtpBtn.Margin = new Thickness(0, 5, 0, 0);
            }
        }

        private void MobileNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private async void ConfrimBtn_Click(object sender, RoutedEventArgs e)
        {
            Error_OTP_Confirm.Visibility = Visibility.Collapsed;
            if (!string.IsNullOrEmpty(OTP_TextBox.Text))
            {
                if (OTP_TextBox.Text.Length == 6)
                {
                    await VerifyOTP(OTP_TextBox.Text);
                }
                else
                {
                    Error_OTP_Confirm.Visibility = Visibility.Visible;
                    Error_OTP_Confirm.Content = "Incorrect OTP";
                }
            }
            else
            {
                Error_OTP_Confirm.Visibility = Visibility.Visible;
                Error_OTP_Confirm.Content = "Enter The OTP";
            }
        }

        private void loginwindow_Closed(object sender, EventArgs e)
        {
            timer?.Stop();
        }
    }

    public class SMS
    {
        public string txnId { get; set; }
        public string errorCode { get; set; }
        public string error { get; set; }
    }
}
