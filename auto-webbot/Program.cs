using auto_webbot.Model;
using auto_webbot.Pages;
using auto_webbot.Pages.Delete;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chrome.ChromeDriverExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Exception = System.Exception;
using FireBaseAuthenticator.KijijiHelperServices;
using FireBaseAuthenticator.Extensions;

namespace AutoBot
{
    class Program
    {
        private static IWebDriver _globalWebDriver;
        private static IDeviceRegistrationService _deviceRegistrationService;
        private static AppSetting _globalSetting;
        private static readonly Random Random = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            var services = new ServiceCollection().AddFireBaseRegistrationDependencies().BuildServiceProvider();
            _deviceRegistrationService = services.GetRequiredService<IDeviceRegistrationService>();
            var jsonText = File.ReadAllText("AppSetting.json");
            var config = JsonConvert.DeserializeObject<AppSetting>(jsonText);
            //Console.WriteLine($"Config: {JsonConvert.SerializeObject(config)}");
            _globalSetting = config;
            Verify().Wait();
            SetConsoleOutput(config.OutputFilePrefix);
            Console.CancelKeyPress += delegate
            {
                if (_globalWebDriver == null) return;
                _globalWebDriver.Quit();
                Environment.Exit(0);
            };
            var adDetails = new List<AdDetails>();
            if (config.AdGlobalSetting.SpecicalAdtitleSetting is null)
            {
                throw new ApplicationException("SpecicalAdtitleSetting is not specified");
            }

            System.Diagnostics.Process myProcess = System.Diagnostics.Process.GetCurrentProcess();
            myProcess.PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            while (true)
            {
                
                foreach (var userSetting in config.UserSettings)
                {
                    try
                    {
                        SetupGlobalWebDriver(config, userSetting);
                        var homePage = new HomePage(_globalWebDriver, config);
                        LoginAndWait(userSetting, homePage, config.AdGlobalSetting.Retry.LoginRetry);
                        adDetails = ReadAdAndRetryIfFailed(config, userSetting, homePage);

                        if (!adDetails.Any())
                        {
                            Console.WriteLine($"Could not find any Ads from {config.AdGlobalSetting.Position.From}" +
                                 $" to {config.AdGlobalSetting.Position.To}");
                            Console.WriteLine($"Wait ScanEvery {config.AdGlobalSetting.Sleep.ScanWhenFoundNothing} minutes");
                            NonBlockedSleepInMinutes(config.AdGlobalSetting.Sleep.ScanWhenFoundNothing);
                            continue;
                        }
                        Console.WriteLine("******************************************************");
                        Console.WriteLine($"ReadAd Done, found {adDetails.Count()} Ads: {JsonConvert.SerializeObject(adDetails)}");
                        Console.WriteLine("******************************************************");
                        if (config.AdGlobalSetting.PauseDuringRun)
                        {
                            Console.WriteLine("PauseDuringRun is activated, press enter to continue");
                            Console.ReadLine();
                        }
                        Console.WriteLine($"Wait DelayAfterAllRead {config.AdGlobalSetting.Sleep.DelayAfterAllRead} minutes");
                        NonBlockedSleepInMinutes(config.AdGlobalSetting.Sleep.DelayAfterAllRead);

                        DeleteAdAndRetryIfFailed(adDetails, homePage);
                        Console.WriteLine("******************************************************");
                        Console.WriteLine("DeleteAd Done");
                        Console.WriteLine("******************************************************");
                        if (config.AdGlobalSetting.PauseDuringRun)
                        {
                            Console.WriteLine("PauseDuringRun is activated, press enter to continue");
                            Console.ReadLine();
                        }
                        Console.WriteLine($"Wait DelayAfterAllDeleted {config.AdGlobalSetting.Sleep.DelayAfterAllDeleted} minutes");
                        NonBlockedSleepInMinutes(config.AdGlobalSetting.Sleep.DelayAfterAllDeleted);


                        PostAdAndRetryIfFailed(adDetails, homePage);
                        Console.WriteLine("******************************************************");
                        Console.WriteLine("PostAd Done");
                        Console.WriteLine("******************************************************");
                        if (config.AdGlobalSetting.PauseDuringRun)
                        {
                            Console.WriteLine("PauseDuringRun is activated, press enter to continue");
                            Console.ReadLine();
                        }
                        Console.WriteLine($"Wait DelayAfterAllPost {config.AdGlobalSetting.Sleep.DelayAfterAllPost} minutes");
                        NonBlockedSleepInMinutes(config.AdGlobalSetting.Sleep.DelayAfterAllPost);
                        var scanValue = GetRandomScanEvery(config.AdGlobalSetting.Sleep.ScanEvery);
                        Console.WriteLine($"Wait ScanEvery {scanValue} minutes");
                        NonBlockedSleepInMinutes(scanValue);

                        var numberOfPost = GetNumberOfAllowAds().Result;
                        var remainingNumberOfPosts = numberOfPost - 1;
                        UpdateNumberOfAllowAds(remainingNumberOfPosts).Wait();
                        Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                        Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                        Console.WriteLine($"Posted successfully! You have {remainingNumberOfPosts} posts remaining.");
                        Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                        Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                        Verify().Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Something went wrong | {e.Message} | {e.StackTrace}");
                        SendErrorEmails(config, adDetails, e, userSetting);
                        Console.WriteLine($"Sent emails to {string.Join(", ", config.ErrorEmail.Receivers)}");
                    }
                }
            }
        }

        private static async Task Verify()
        {
            try
            {
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                var deviceInfo = await _deviceRegistrationService.GetDeviceInformation();
                Console.WriteLine($"ExpiredDate: {deviceInfo.ExpiredDate}, RemainingPostLimit: {deviceInfo.RemainingPostLimit}");
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                await _deviceRegistrationService.VerifyDevice();
            }
            catch (Exception)
            {
                Console.WriteLine("I regret to inform you that your device is currently expired or has surpassed the allowed re-post limit. If you wish to make additional re-posts, I kindly suggest contacting the developer to request an extension (5 USD ~ 1 month ~ 30 re-posts). The fee is small and will help me continue to work on the app, hope you understand. Payment can be made via PayPal (paypal.me/hunghung1404), and an extension will be applied to your account.");
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                Console.WriteLine($"Please press enter to close");
                Console.ReadLine();
                Environment.Exit(0);
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }

        private static async Task<int> GetNumberOfAllowAds()
        {
            try
            {
                var deviceInfo = await _deviceRegistrationService.GetDeviceInformation();
                return deviceInfo.RemainingPostLimit;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static async Task UpdateNumberOfAllowAds(int remainningPost)
        {
            try
            {
                await _deviceRegistrationService.UpdateNumberOfAllowAds(remainningPost);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static List<AdDetails> ReadAdAndRetryIfFailed(AppSetting Config, UserSetting userSetting, HomePage homePage)
        {
            for (var i = 0; i < Config.AdGlobalSetting.Retry.ReadRetry; i++)
            {
                try
                {
                    var adDetails = new List<AdDetails>();
                    Console.WriteLine($"ReadAds try {i}");
                    var readAdPage = new ReadAdPage(_globalWebDriver, Config);
                    return readAdPage.ReadAds(Config.AdGlobalSetting);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"There was an error during ReadAds {e.Message} - proceed retry");
                }
            }
            return new List<AdDetails>();
        }

        private static void DeleteAdAndRetryIfFailed( List<AdDetails> adDetails, HomePage homePage)
        {
            homePage.DeleteAds(adDetails);
        }

        private static void PostAdAndRetryIfFailed(List<AdDetails> adDetails, HomePage homePage)
        {
            homePage.PostAds(adDetails);
        }

        private static void SetupGlobalWebDriver(AppSetting config, UserSetting userSetting)
        {
            if (config.UserSettings.Count() == 1 && _globalWebDriver is null)
            {
                _globalWebDriver = SetupDriverInstance(config.UserSettings.First(), config);
            }

            if (config.UserSettings.Count() > 1)
            {
                _globalWebDriver = SetupDriverInstance(userSetting, config);
            }
        }

        private static int GetRandomScanEvery(IReadOnlyCollection<ScanEvery> scanEverys)
        {
            var timeUtc = DateTime.UtcNow;
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);

            if (scanEverys == null) { throw new Exception($"Could not found scan setting for {easternTime.DayOfWeek}"); }

            var scanSetting = scanEverys.FirstOrDefault(s => s.DayOfWeek == easternTime.DayOfWeek);
            if (scanSetting is null)
            {
                throw new Exception($"Could not found scan setting for {easternTime.DayOfWeek}");
            }
            return Random.Next(scanSetting.RandomFrom, scanSetting.RandomTo);
        }

        private static void SendErrorEmails(AppSetting config, List<AdDetails> adDetails, Exception e, UserSetting userSetting)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(config.ErrorEmail.Sender, config.ErrorEmail.PassForSender),
                    EnableSsl = true,
                };
                foreach (var receiver in config.ErrorEmail.Receivers)
                {
                    smtpClient.Send(config.ErrorEmail.Sender, receiver,
                        $"There was some thing wrong with Auto-Kijiji - Account {userSetting.Email}",
                        $"*******ADS:{JsonConvert.SerializeObject(adDetails)}," +
                        Environment.NewLine +
                        Environment.NewLine +
                        $"*******ERROR : {e.Message}," +
                        Environment.NewLine +
                        Environment.NewLine +
                        $"*******STACK: {e.StackTrace}");
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Warning | could not send error emails, this might happens b/c the Gmail server is down");
            }

        }

        private static void LoginAndWait(UserSetting userSetting, HomePage homePage, int retry)
        {
            for (var i = 0; i < retry; i++)
            {
                try
                {
                    Console.WriteLine($"Logging in as {userSetting.Email} - {userSetting.Pass} - try {i}");
                    var signedInAlready = homePage.Login(userSetting.Email, userSetting.Pass);
                    if (signedInAlready)
                    {
                        break;
                    }
                    Console.WriteLine("Wait a while. If there is a captcha, please resolve it manually.");
                    Thread.Sleep(15000);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"There was an error during login {e.Message} - proceed retry");
                }
            }
        }

        private static IWebDriver SetupDriverInstance(UserSetting userSetting, AppSetting config)
        {
            new DriverManager().SetUpDriver(new ChromeConfig(), "MatchingBrowser");
            var service = ChromeDriverService.CreateDefaultService();

            service.LogPath = "chromedriver.log";

            service.EnableVerboseLogging = true;

            var chromeArguments = GetGeneralSetting();

            if (userSetting.UserAgent != null)
            {
                chromeArguments.Add($"--user-agent={userSetting.UserAgent}");
            }
            var options = new ChromeOptions();
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
            options.AddArguments(chromeArguments);
            AddProxySeting(userSetting, options);
            options.PageLoadStrategy = PageLoadStrategy.Eager;
            return new ChromeDriver(service, options);
        }


        private static List<string> GetGeneralSetting()
        {
            var chromeArguments = new List<string> {
                "--disable-notifications",
                "--start-maximized",
                //"--incognito",
                "--ignore-ssl-errors",
                "--ignore-certificate-errors",
                //"--disable-extensions",
                //"--headless",
                "no-sandbox",
                "--disable-gpu",
                "--disable-logging",
                "--disable-popup-blocking",
                "disable-blink-features=AutomationControlled",
                "--disable-dev-shm-usage",
                "--log-level=3",
                "--disable-application-cache",
                "enable-features=NetworkServiceInProcess",
                "--disable-features=NetworkService"
            };
            return chromeArguments;
        }

        private static void AddProxySeting(UserSetting userSetting, ChromeOptions options)
        {
            if (userSetting.Proxy != null
                            && userSetting.Proxy.Host != null
                            && userSetting.Proxy.Port != 0
                            && userSetting.Proxy.UserName != null
                            && userSetting.Proxy.Password != null)
            {
                Console.WriteLine($"Proxy activated: {userSetting.Proxy.Host}:{userSetting.Proxy.Port}:{userSetting.Proxy.UserName}:{userSetting.Proxy.Password}");
                options.AddHttpProxy(userSetting.Proxy.Host, userSetting.Proxy.Port, userSetting.Proxy.UserName, userSetting.Proxy.Password);
            }
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
        }

        private static void NonBlockedSleepInMinutes(int sleep)
        {
            var minutesToSleep = _globalSetting.AdGlobalSetting.Sleep.SleepInterval;
            var numberOfSleeps = sleep / minutesToSleep;
            for (var i = 0; i < numberOfSleeps; i++)
            {
                Console.WriteLine($"Wait {minutesToSleep} minutes then reload the page to stay signed in | {i + 1}/{numberOfSleeps}");
                Thread.Sleep(TimeSpan.FromMinutes(minutesToSleep));
            }
        }
    }
}
