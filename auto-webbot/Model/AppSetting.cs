using System;
using System.Collections.Generic;
using System.Text;

namespace auto_webbot.Model
{
    public class AppSetting
    {
        public List<UserSetting> UserSettings { get; set; }
        public ErrorEmailSetting ErrorEmail {get;set;}
        public string OutputFilePrefix { get; set; }
        public AdGlobalSetting AdGlobalSetting { get; set; }
        public Mode Mode { get; set; }
    }

    public class UserSetting
    {
        public string Email { get; set; }
        public string Pass { get; set; }
    }
    public enum BrowserSetting
    { 
        chrome,
        firefox,
        internetexplorer
    }

    public class AdGlobalSetting
    {
        public SleepSetting Sleep { get; set; }
        public Position Position { get; set; }
        public bool PauseDuringRun { get; set; }
    }

    public enum Mode
    { 
        test,
        run
    }

    public class SleepSetting
    {
        public int ScanEvery { get; set; }
        public int DelayBetweenEachRead { get; set; }
        public int DelayAfterAllRead { get; set; }
        public int DelayBetweenEachDelete { get; set; }
        public int DelayAfterAllDeleted { get; set; } 
        public int DelayAfterEachPost { get; set; }
        public int DelayAfterAllPost { get; set; }
    }

    public class Position
    {
        public int From { get; set; }
        public int To { get; set; }
    }

    public class ErrorEmailSetting
    {
        public string Sender { get; set; }
        public string PassForSender { get; set; }
        public List<string> Receivers { get; set; }
    }
}
