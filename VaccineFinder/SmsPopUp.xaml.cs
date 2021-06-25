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
using System.Windows.Shapes;

namespace VaccineFinder
{
    /// <summary>
    /// Interaction logic for SmsPopUp.xaml
    /// </summary>
    public partial class SmsPopUp : Window
    {
        public SmsPopUp()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Url.Text = Url.Text != null ? Url.Text.Trim() : Url.Text;
            if (Url.Text != null && Url.Text != string.Empty)
            {
                if (checkcorecturl())
                {
                    VaccineHelper.Instance.SmsUrl = Url.Text;
                    VaccineHelper.Instance.IsShown = true;
                    this.Close();
                }
                else
                { ErrorText.Text = "Invalid URL"; }
            }
            else
            {
                ErrorText.Text = "Insert SMS URL";
            }
        }

        private bool checkcorecturl()
        {
            var temp = Url.Text;
            object ob = "+" + "custmessage" + "+";
            var ob2 = "{0}";
            var res = temp.Contains(ob.ToString());
            var res2 = temp.Contains(ob2.ToString());
            if (res || res2)
            {
                VaccineHelper.Instance.SMSstringFormat = res ? true : res2 ? false : false;
                return true;
            }
            return false;
        }
    }
}
