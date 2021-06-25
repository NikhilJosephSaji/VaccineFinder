﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.Globalization;
using System.Windows.Media.Animation;
using System.Threading;
using System.Media;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VaccineFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string getstates = "https://cdn-api.co-vin.in/api/v2/admin/location/states";
        private const string getdistrict = "https://cdn-api.co-vin.in/api/v2/admin/location/districts/";
        private const string Getalllist = "https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/findByDistrict?district_id={0}&date={1}";
        private DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private DispatcherTimer dispatcherTimersms = new System.Windows.Threading.DispatcherTimer();
        private const string InternetErr = "No Internet Connection. Please Connect to Internet";
        private bool LoopSMS = false;

        public MainWindow()
        {
            InitializeComponent();
            ((Storyboard)Resources["Rotation"]).Begin();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!InternetError(InternetErr))
            {
                return;
            }
            if (District.SelectedValue == null || State.SelectedValue == null)
            {
                ErrorDisplay("Select State and District");
                return;
            }
            LayoutRoot.Visibility = Visibility.Visible;
            VaccineGrid.ItemsSource = new List<Session>();
            ClearCount();
            CultureInfo esC = new CultureInfo("es-ES");
            var tempdate = DatePicker.SelectedDate.Value.ToString("d", esC);
            var newdate = tempdate.Replace('/', '-');
            var dist = District.SelectedValue;
            var url = string.Format(Getalllist, dist, newdate);
            var result = await Task.Run(() => ReturnHttpResult(url));
            var list = JsonConvert.DeserializeObject<SearchRoot>(result);
            var newlist = list.sessions;
            int count = 0;
            if (Avilable.IsChecked.Value)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.available_capacity >= 1).ToList() : new List<Session>();
                count++;
            }

            if (AgeEight.IsChecked.Value && AgeFouty.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.min_age_limit == "18").ToList() : new List<Session>();
                count++;
            }

            if (AgeFouty.IsChecked.Value && AgeEight.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.min_age_limit != "18").ToList() : new List<Session>();
                count++;
            }

            if (Free.IsChecked.Value && Paid.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.fee_type == "Free").ToList() : new List<Session>();
                count++;
            }

            if (DosOne.IsChecked.Value && DosTwo.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.available_capacity_dose1 > 1).ToList() : new List<Session>();
                count++;
            }

            if (DosTwo.IsChecked.Value && DosOne.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.available_capacity_dose2 > 1).ToList() : new List<Session>();
                count++;
            }

            if (Paid.IsChecked.Value && Free.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.fee_type == "Paid").ToList() : new List<Session>();
                count++;
            }

            if (count == 0)
            {
                newlist = list.sessions;
            }

            if (newlist != null)
            {
                foreach (var x in newlist)
                {
                    x.min_age_limit = x.min_age_limit + "+";
                }
                await Task.Run(() => Thread.Sleep(1000));
                VaccineGrid.ItemsSource = newlist;
                SetCount(newlist);
            }

            LayoutRoot.Visibility = Visibility.Collapsed;
        }

        private async void ErrorDisplay(string Error)
        {
            TextGrid.Visibility = Visibility.Visible;
            TextContent.Text = Error;
            await Task.Run(() =>
            {
                Thread.Sleep(4000);
            });
            TextGrid.Visibility = Visibility.Collapsed;
        }

        private void SetCount(List<Session> newlist)
        {
            totalcount.Content = newlist.Count.ToString();
            totalavacount.Content = newlist.Where(x => x.available_capacity >= 1).Count();
            totaldoscount.Content = newlist.Where(x => x.available_capacity_dose1 >= 1).Sum(x => x.available_capacity_dose1);
            totaldos2count.Content = newlist.Where(x => x.available_capacity_dose2 >= 1).Sum(x => x.available_capacity_dose2);
            totalvacccount.Content = newlist.Where(x => x.available_capacity >= 1).Sum(x => x.available_capacity);
        }

        private void ClearCount()
        {
            totalcount.Content = "0";
            totalavacount.Content = "0";
            totaldoscount.Content = "0";
            totalvacccount.Content = "0";
            totaldos2count.Content = "0";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!InternetError(InternetErr))
            {
                return;
            }
            DatePicker.SelectedDate = DateTime.Today;
            var result = await Task.Run(() => ReturnHttpResult(getstates));
            var list = JsonConvert.DeserializeObject<StateRoot>(result);
            var states = list.states;
            State.ItemsSource = states;
            State.DisplayMemberPath = "state_name";
            State.SelectedValuePath = "state_id";
        }

        private bool InternetError(string Error)
        {
            if (!InternetAvailability.IsInternetAvailable())
            {
                ErrorDisplay(Error);
                return false;
            }

            return true;
        }

        private string ReturnHttpResult(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.ContentType = "application/json";
            string responseData = string.Empty;

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    responseData = reader.ReadToEnd();
                    reader.Close();
                }
                response.Close();
            }
            return responseData;
        }

        private async void State_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var getdist = getdistrict + State.SelectedValue;
            var result = await Task.Run(() => ReturnHttpResult(getdist));
            var list = JsonConvert.DeserializeObject<DistrictRoot>(result);
            District.ItemsSource = list.districts;
            District.DisplayMemberPath = "district_name";
            District.SelectedValuePath = "district_id";
        }

        private async void AppNOtify_Click(object sender, RoutedEventArgs e)
        {
            if (!InternetError(InternetErr))
            {
                return;
            }
            if (District.SelectedValue == null || State.SelectedValue == null)
            {
                ErrorDisplay("Select State and District");
                return;
            }
            AppNOtify.IsEnabled = false;
            AppNOtify.Background = System.Windows.Media.Brushes.Green;
            AppNOtify.Foreground = System.Windows.Media.Brushes.White;
            ErrorDisplay("Activated App Notification Dont Close Application");
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 20);
            dispatcherTimer.Start();
        }

        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            CultureInfo esC = new CultureInfo("es-ES");
            var tempdate = DatePicker.SelectedDate.Value.ToString("d", esC);
            var newdate = tempdate.Replace('/', '-');
            var dist = District.SelectedValue;
            var url = string.Format(Getalllist, dist, newdate);
            var result = await Task.Run(() => ReturnHttpResult(url));
            var list = JsonConvert.DeserializeObject<SearchRoot>(result);
            var newlist = list.sessions;
            newlist = newlist.Any() ? newlist.Where(x => x.available_capacity >= 1).ToList() : new List<Session>();

            if (AgeEight.IsChecked.Value && AgeFouty.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.min_age_limit == "18").ToList() : new List<Session>();
            }

            if (AgeFouty.IsChecked.Value && AgeEight.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.min_age_limit != "18").ToList() : new List<Session>();
            }

            if (Free.IsChecked.Value && Paid.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.fee_type == "Free").ToList() : new List<Session>(); ;
            }

            if (DosOne.IsChecked.Value && DosTwo.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.available_capacity_dose1 > 1).ToList() : new List<Session>();
            }

            if (DosTwo.IsChecked.Value && DosOne.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.available_capacity_dose2 > 1).ToList() : new List<Session>();
            }

            if (Paid.IsChecked.Value && Free.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.fee_type == "Paid").ToList() : new List<Session>();
            }
            if (newlist.Any())
            {
                new Thread(delegate () { PlayNotificationSound(); }).Start();
                var li = newlist.Select(c => c.name + " : " + c.available_capacity).ToList();
                var str = "";
                foreach (var x in li)
                {
                    str = str + " , " + x;
                }
                dispatcherTimer.Stop();
                await Task.Run(() => MessageBox.Show("Vaccines Found in these Hospitals :" + str));
                AppNOtify.IsEnabled = true;
                AppNOtify.Background = System.Windows.Media.Brushes.LightGreen;
                AppNOtify.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        public async void PlayNotificationSound()
        {
            bool found = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue(null); // pass null to get (Default)
                        if (o != null)
                        {
                            SoundPlayer theSound = new SoundPlayer((String)o);
                            theSound.PlayLooping();
                            await Task.Run(() => Thread.Sleep(20000));
                            theSound.Stop();
                            found = true;
                        }
                    }
                }
            }
            catch
            { }
            if (!found)
            {
                int i = 0;
                while (i < 20)
                {
                    SystemSounds.Beep.Play();
                    await Task.Run(() => Thread.Sleep(1200));
                    i++;
                }
            }
        }
        private void SMSNotify_Click(object sender, RoutedEventArgs e)
        {
            if (!InternetError(InternetErr))
            {
                return;
            }
            if (District.SelectedValue == null || State.SelectedValue == null)
            {
                ErrorDisplay("Select State and District");
                return;
            }

            if (VaccineHelper.Instance.IsShown)
            {
                Sms();
            }
            else
            {
                new SmsPopUp() { ShowInTaskbar = false, Owner = this }.ShowDialog();
                if (VaccineHelper.Instance.IsShown)
                    Sms();
            }
        }

        private void Sms(bool loop = false)
        {
            if (loop)
            {
                SMSLoop.Background = System.Windows.Media.Brushes.Green;
                SMSLoop.Foreground = System.Windows.Media.Brushes.White;
                loopbtncontent.Text = "STOP";
                SMSNotify.IsEnabled = false;
                ErrorDisplay("Activated SMS Notification Don't Close Application.\r\n NOTE : Repeated SMS is Send From Application in Each 20 Sec unless you Stop.");
            }
            else
            {
                SMSNotify.IsEnabled = false;
                SMSLoop.IsEnabled = false;
                SMSNotify.Background = System.Windows.Media.Brushes.Green;
                SMSNotify.Foreground = System.Windows.Media.Brushes.White;
                ErrorDisplay("Activated SMS Notification Don't Close Application");
            }            
            dispatcherTimersms = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimersms.Tick += new EventHandler(SMSTimer_tick);
            dispatcherTimersms.Interval = new TimeSpan(0, 0, 20);
            dispatcherTimersms.Start();
        }

        private async void SMSTimer_tick(object sender, EventArgs e)
        {
            CultureInfo esC = new CultureInfo("es-ES");
            var tempdate = DatePicker.SelectedDate.Value.ToString("d", esC);
            var newdate = tempdate.Replace('/', '-');
            var dist = District.SelectedValue;
            var url = string.Format(Getalllist, dist, newdate);
            var result = await Task.Run(() => ReturnHttpResult(url));
            var list = JsonConvert.DeserializeObject<SearchRoot>(result);
            var newlist = list.sessions;
            newlist = newlist.Any() ? newlist.Where(x => x.available_capacity >= 1).ToList() : new List<Session>();

            if (AgeEight.IsChecked.Value && AgeFouty.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.min_age_limit == "18").ToList() : new List<Session>();
            }

            if (AgeFouty.IsChecked.Value && AgeEight.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.min_age_limit != "18").ToList() : new List<Session>();
            }

            if (Free.IsChecked.Value && Paid.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.fee_type == "Free").ToList() : new List<Session>(); ;
            }

            if (DosOne.IsChecked.Value && DosTwo.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.available_capacity_dose1 > 1).ToList() : new List<Session>();
            }

            if (DosTwo.IsChecked.Value && DosOne.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.available_capacity_dose2 > 1).ToList() : new List<Session>();
            }

            if (Paid.IsChecked.Value && Free.IsChecked.Value == false)
            {
                newlist = newlist.Any() ? newlist.Where(x => x.fee_type == "Paid").ToList() : new List<Session>();
            }
            if (newlist.Any())
            {
                var li = newlist.Select(c => c.name + " : " + c.available_capacity).ToList();
                var str = "";
                foreach (var x in li)
                {
                    str = str + " , " + x;
                }
                if (!LoopSMS)
                    dispatcherTimersms.Stop();
                bool res = await Task.Run(() => SendSMS("Vaccine Found in These Hospitals : " + str));
                if (res)
                {
                    if (!LoopSMS)
                    {
                        MessageBox.Show("Send SMS");
                    }
                    else
                    {
                        ErrorDisplay("Send Successfully");
                    }
                }
                else
                {

                    if (!LoopSMS)
                    {
                        MessageBox.Show("Failed");
                    }
                    else
                    {
                        ErrorDisplay("Failed");
                    }
                }
                if (!LoopSMS)
                {
                    SMSNotify.IsEnabled = true;
                    SMSLoop.IsEnabled = true;
                    SMSNotify.Background = System.Windows.Media.Brushes.LightGreen;
                    SMSNotify.Foreground = System.Windows.Media.Brushes.Black;
                }
            }
        }

        private bool SendSMS(string msg)
        {
            var smsurl = "";
            try
            {
                if (!(bool)VaccineHelper.Instance.SMSstringFormat)
                {
                    smsurl = string.Format(VaccineHelper.Instance.SmsUrl, msg);
                }
                else
                {
                    smsurl = VaccineHelper.Instance.SmsUrl.Replace("+custmessage+", msg);
                }
                HttpWebRequest request = WebRequest.Create(smsurl) as HttpWebRequest;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK)
                { return true; }
                else
                { return false; }

            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("chrome.exe", "https://selfregistration.cowin.gov.in/");
            }
            catch (Exception)
            {
                Process.Start("iexplore.exe", "https://selfregistration.cowin.gov.in/");
            }
        }

        private void SMSLoop_Click(object sender, RoutedEventArgs e)
        {
            if (!InternetError(InternetErr))
            {
                return;
            }
            if (District.SelectedValue == null || State.SelectedValue == null)
            {
                ErrorDisplay("Select State and District");
                return;
            }

            if (LoopSMS)
            {
                SMSLoop.Background = System.Windows.Media.Brushes.LightGreen;
                SMSLoop.Foreground = System.Windows.Media.Brushes.Black;
                loopbtncontent.Text = "SMS Notify Loop";
                dispatcherTimersms.Stop();
                SMSNotify.IsEnabled = true;
                LoopSMS = false;
            }
            else
            {
                if (VaccineHelper.Instance.IsShown)
                {
                    LoopSMS = true;
                    Sms(true);
                }
                else
                {
                    new SmsPopUp() { ShowInTaskbar = false, Owner = this }.ShowDialog();
                    if (VaccineHelper.Instance.IsShown)
                    {
                        LoopSMS = true;
                        Sms(true);
                    }
                }
            }
        }
    }

    public class InternetAvailability
    {
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);

        public static bool IsInternetAvailable()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
        }
    }
    public class State
    {
        public int state_id { get; set; }
        public string state_name { get; set; }
    }

    public class StateRoot
    {
        public List<State> states { get; set; }
        public int ttl { get; set; }
    }

    public class District
    {
        public int district_id { get; set; }
        public string district_name { get; set; }
    }

    public class DistrictRoot
    {
        public List<District> districts { get; set; }
        public int ttl { get; set; }
    }

    public class Session
    {
        public string name { get; set; }
        public string address { get; set; }
        public string fee_type { get; set; }
        public string date { get; set; }
        public int available_capacity { get; set; }
        public int available_capacity_dose1 { get; set; }
        public int available_capacity_dose2 { get; set; }
        public string fee { get; set; }
        public string min_age_limit
        {
            get;
            set;
        }
        public string vaccine { get; set; }
    }

    public class SearchRoot
    {
        public List<Session> sessions { get; set; }
    }
}