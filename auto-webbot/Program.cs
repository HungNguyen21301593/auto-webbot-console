using AutoBot.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;

namespace AutoBot
{
    class Program
    {
        private static ChromeDriver GlobalWebDriver;
        private static WebDriverWait WebWaiter => new WebDriverWait(GlobalWebDriver, TimeSpan.FromSeconds(20));

        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            // Read option from file.
            //var config = new ConfigurationBuilder()
            //    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            //    .AddJsonFile("appsettings.json").Build();

            //var credential = config.GetSection(nameof(Credential));
            //var email = credential.GetSection(nameof(Credential.Email)).Value;
            //var pass = credential.GetSection(nameof(Credential.Pass)).Value;
            //var outputFilePrefix = config.GetSection(nameof(AppSetting.OutputFilePrefix));
            var JsonText = File.ReadAllText("appsettings.json");
            var Config = JsonConvert.DeserializeObject<AppSetting>(JsonText);
            Console.WriteLine($"Credential: {Config.Credential.Email}, {Config.Credential.Pass}");
            SetConsoleOutput(Config.OutputFilePrefix);
            // Start Driver
            SetupChromeDriver();
            try
            {
                // Login
                Login(Config.Credential.Email, Config.Credential.Pass);
                // Set filter
                Login(Config.Credential.Email, Config.Credential.Pass);
                SetFilter(Config.Filter);
                // Loop through page
                // Lopp through link
                // Bid
            }
            catch (Exception e)
            {
                Console.WriteLine("Login failed, retrying...");
                Login(Config.Credential.Email, Config.Credential.Pass);
            }
            finally
            {
            }
            GlobalWebDriver.Quit();
            Console.ReadLine();
        }

        private static void SetFilter(Filter filter)
        {
            throw new NotImplementedException();
        }

        private static void SetupChromeDriver()
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
                "--disable-popup-blocking"};
            var options = new ChromeOptions();
            options.AddArguments(chromeArguments);
            GlobalWebDriver = new ChromeDriver(options);
        }

        private static void Login(string email, string pass)
        {
            GlobalWebDriver.Navigate().GoToUrl("https://www.vn.freelancer.com/job-search/projects");
            string loginButtonLocatorPath = "/html/body/div[1]/header/div/div/div[2]/fl-login-signup-angular/a[1]";
            string loginEmailLocatorPath = "/html/body/fl-ui-modal/div/div[1]/div/div/fl-compiled/fl-login-signup-modal/div/div/fl-login/fl-login-form/div[2]/form[2]/fieldset/ol/li[1]/input";
            string loginErrorLocalorPath = "/html/body/fl-ui-modal/div/div[1]/div/div/fl-compiled/fl-login-signup-modal/div/div/fl-login/fl-login-form/div[2]/form[2]/fieldset/ol/li[1]/div/div";
            string loginPassLocaltorPath = "/html/body/fl-ui-modal/div/div[1]/div/div/fl-compiled/fl-login-signup-modal/div/div/fl-login/fl-login-form/div[2]/form[2]/fieldset/ol/li[2]/input";
            string loginsubmitLocaltorPath = "/html/body/fl-ui-modal/div/div[1]/div/div/fl-compiled/fl-login-signup-modal/div/div/fl-login/fl-login-form/div[2]/form[2]/fieldset/ol/li[4]/button";

            var loginButton = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .ElementIsVisible(By.XPath(loginButtonLocatorPath)));
            loginButton.Click();
            var emailElement = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .ElementIsVisible(By.XPath(loginEmailLocatorPath)));
            emailElement.Clear();
            emailElement.SendKeys(email);

            var passElement = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .ElementIsVisible(By.XPath(loginPassLocaltorPath)));

            passElement.Click();
            passElement.Clear();
            passElement.SendKeys(pass);
            var submitElement = WebWaiter
                 .Until(SeleniumExtras
                     .WaitHelpers
                     .ExpectedConditions
                     .ElementIsVisible(By.XPath(loginsubmitLocaltorPath)));

            var errors = GlobalWebDriver.FindElements(By.XPath(loginErrorLocalorPath));
            if (errors.Count > 0)
            {
                throw new Exception("login error");
            }
            submitElement.Click();
        }

        private static void SetConsoleOutput(string prefix)
        {
            var path = $"output\\{prefix}{DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}.txt";
            Console.WriteLine($"Setup OutputPath: {path}");
            Directory.CreateDirectory("output");
            FileStream outfilestream = new FileStream(path, FileMode.CreateNew);

            Console.OutputEncoding = System.Text.Encoding.Unicode;
            var outstreamwriter = new StreamWriter(outfilestream)
            {
                AutoFlush = true
            };
            Console.SetOut(outstreamwriter);
        }
    }
}
