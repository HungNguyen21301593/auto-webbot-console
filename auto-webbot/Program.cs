using auto_webbot.Model;
using auto_webbot.Pages;
using auto_webbot.Pages.Delete;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chrome.ChromeDriverExtensions;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace AutoBot
{
    class Program
    {
        private static IWebDriver GlobalWebDriver;
        private static WebDriverWait WebWaiter => new WebDriverWait(GlobalWebDriver, TimeSpan.FromSeconds(60));
        private static Random random = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            var JsonText = File.ReadAllText("AppSetting.json");
            var Config = JsonConvert.DeserializeObject<AppSetting>(JsonText);
            SetConsoleOutput(Config.OutputFilePrefix);
            List<AdDetails> adDetails = new List<AdDetails>();

            while (true)
            {
                foreach (var userSetting in Config.UserSettings)
                {
                    try
                    {
                        GlobalWebDriver = SetupDriver(userSetting);
                        var homePage = new HomePage(GlobalWebDriver, Config);
                        LoginAndWait(userSetting, homePage);
                        var readAdPage = new ReadAdPage(GlobalWebDriver);
                        adDetails = readAdPage.ReadAds(Config.AdGlobalSetting);
                        if (!adDetails.Any())
                        {
                            Console.WriteLine($"Could not find any Ads from {Config.AdGlobalSetting.Position.From}" +
                                $" to {Config.AdGlobalSetting.Position.To}");
                            Console.WriteLine($"Wait ScanWhenFoundNothing {Config.AdGlobalSetting.Sleep.ScanWhenFoundNothing} minutes");
                            Thread.Sleep(TimeSpan.FromMinutes(Config.AdGlobalSetting.Sleep.ScanWhenFoundNothing));
                            GlobalWebDriver.Quit();
                            continue;
                        }
                        Console.WriteLine("******************************************************");
                        Console.WriteLine($"ReadAd Done, found {adDetails.Count()} Ads: {JsonConvert.SerializeObject(adDetails)}");
                        Console.WriteLine("******************************************************");
                        if (Config.AdGlobalSetting.PauseDuringRun)
                        {
                            Console.WriteLine("PauseDuringRun is activated, press enter to continue");
                            Console.ReadLine();
                        }
                        Console.WriteLine($"Wait DelayAfterAllRead {Config.AdGlobalSetting.Sleep.DelayAfterAllRead} minutes");
                        Thread.Sleep(TimeSpan.FromMinutes(Config.AdGlobalSetting.Sleep.DelayAfterAllRead));

                        LoginAndWait(userSetting, homePage);
                        homePage.DeleteAds(adDetails);
                        Console.WriteLine("******************************************************");
                        Console.WriteLine("DeleteAd Done");
                        Console.WriteLine("******************************************************");
                        if (Config.AdGlobalSetting.PauseDuringRun)
                        {
                            Console.WriteLine("PauseDuringRun is activated, press enter to continue");
                            Console.ReadLine();
                        }
                        Console.WriteLine($"Wait DelayAfterAllDeleted {Config.AdGlobalSetting.Sleep.DelayAfterAllDeleted} minutes");
                        Thread.Sleep(TimeSpan.FromMinutes(Config.AdGlobalSetting.Sleep.DelayAfterAllDeleted));

                        LoginAndWait(userSetting, homePage);
                        homePage.PostAds(adDetails);
                        Console.WriteLine("******************************************************");
                        Console.WriteLine("PostAd Done");
                        Console.WriteLine("******************************************************");
                        if (Config.AdGlobalSetting.PauseDuringRun)
                        {
                            Console.WriteLine("PauseDuringRun is activated, press enter to continue");
                            Console.ReadLine();
                        }
                        Console.WriteLine($"Wait DelayAfterAllPost {Config.AdGlobalSetting.Sleep.DelayAfterAllPost} minutes");
                        Thread.Sleep(TimeSpan.FromMinutes(Config.AdGlobalSetting.Sleep.DelayAfterAllPost));

                        var scanValue = GetRandomScanEvery(Config.AdGlobalSetting.Sleep.ScanEvery);
                        Console.WriteLine($"Wait ScanEvery {scanValue} minutes");
                        Thread.Sleep(TimeSpan.FromMinutes(scanValue));
                        GlobalWebDriver.Quit();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Something went wrong | {e.Message} | {e.StackTrace}");
                        SendErrorEmails(Config, adDetails, e);
                        GlobalWebDriver.Quit();
                        continue;
                    }
                }
            }
        }

        private static int GetRandomScanEvery(List<ScanEvery> scanEverys)
        {
            var timeUtc = DateTime.UtcNow;
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);
            var scanSetting =  scanEverys.Where(s => s.DayOfWeek == easternTime.DayOfWeek).FirstOrDefault();
            if (scanSetting is null)
            {
                throw new Exception($"Could not found scan setting for {easternTime.DayOfWeek}");
            }
            return random.Next(scanSetting.RandomFrom, scanSetting.RandomTo);
        }

        

        private static void SendErrorEmails(AppSetting Config, List<AdDetails> adDetails, Exception e)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(Config.ErrorEmail.Sender, Config.ErrorEmail.PassForSender),
                EnableSsl = true,
            };
            foreach (var Receiver in Config.ErrorEmail.Receivers)
            {
                smtpClient.Send(Config.ErrorEmail.Sender, Receiver,
                    "There was some thing wrong with Auto-Kijiji",
                    $"ADS:{JsonConvert.SerializeObject(adDetails)}," +
                    $" ERROR : {e.Message}," +
                    $" STACK: {e.StackTrace}");
            }
        }

        private static void LoginAndWait(UserSetting userSetting, HomePage homePage)
        {
            homePage.Login(userSetting.Email, userSetting.Pass);
            Console.WriteLine("Wait a while in case there is capcha");
            Thread.Sleep(15000);
        }

        private static IWebDriver SetupDriver(UserSetting userSetting)
        {
            var chromeArguments = new List<string> {
                "--disable-notifications",
                "--start-maximized",
                "--incognito",
                "--ignore-ssl-errors",
                "--ignore-certificate-errors",
                "--disable-extensions",
                //"--headless",
                "no-sandbox",
                "--disable-logging",
                "--disable-popup-blocking",
                "disable-blink-features=AutomationControlled",
                "--log-level=3",
            };

            if (userSetting.UserAgent != null)
            {
                chromeArguments.Add($"--user-agent={userSetting.UserAgent}");
            }
            var options = new ChromeOptions();
            options.AddArguments(chromeArguments);
            if (userSetting.Proxy != null
                && userSetting.Proxy.Host != null
                && userSetting.Proxy.Port != 0
                && userSetting.Proxy.UserName != null
                && userSetting.Proxy.Password != null)
            {
                options.AddHttpProxy(userSetting.Proxy.Host, userSetting.Proxy.Port, userSetting.Proxy.UserName, userSetting.Proxy.Password);
            }
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
            return new ChromeDriver(options);
        }

        private static void SetConsoleOutput(string prefix)
        {
            var path = $"output\\{prefix}{DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}.txt";
            Console.WriteLine($"Setup OutputPath: {path}");
            Directory.CreateDirectory("output");
            Directory.CreateDirectory("AdPics");
            FileStream outfilestream = new FileStream(path, FileMode.CreateNew);

            Console.OutputEncoding = System.Text.Encoding.Unicode;
            var outstreamwriter = new StreamWriter(outfilestream)
            {
                AutoFlush = true
            };
            //Console.SetOut(outstreamwriter);
        }
    }
}
