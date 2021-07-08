using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace VaccineFinder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }

    public class VaccineHelper
    {
        public bool IsShown { get; set; }

        public bool? SMSstringFormat { get; set; }
        public string SmsUrl { get; set; }
        public bool IsUserLogedIn { get; set; }

        public string UserName { get; set; }
        VaccineHelper()
        {
            IsShown = false;
            IsUserLogedIn = false;
        }

        private static VaccineHelper instance = null;
        public static VaccineHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VaccineHelper();
                }
                return instance;
            }
        }
    }
}
