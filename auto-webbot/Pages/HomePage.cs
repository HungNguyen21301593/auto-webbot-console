using auto_webbot.Model;
using auto_webbot.Pages.Delete;
using auto_webbot.Pages.Post;
using auto_webbot.Pages.Post.auto_webbot.Pages.Post;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace auto_webbot.Pages
{
    public class HomePage
    {
        public IWebDriver webDriver { get; set; }
        public AppSetting config { get; set; }
        public HomePage(IWebDriver webDriver, AppSetting config)
        {
            this.webDriver = webDriver;
            this.config = config;
        }
        public By SigninLocator = By.LinkText("Sign In");
        public By PostAdButtonLocator = By.CssSelector("a[class*='headerButtonPostAd']");


        public bool Login(string email, string pass)
        {
            webDriver.Navigate().GoToUrl("https://www.kijiji.ca/?siteLocale=en_CA");

            Thread.Sleep(config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var signinExist = webDriver.FindElements(SigninLocator);
            if (signinExist.Any())
            {
                signinExist.First().Click();
                var signinPage = new SigninPage(webDriver, config);
                signinPage.Login(email, pass);
                return false;
            }
            Console.WriteLine("Logged in already so skip");
            return true;
        }

        public void PostAds(List<AdDetails> adDetails)
        {
            foreach (var adDetail in adDetails)
            {
                if (config == null) throw new ArgumentNullException(nameof(config));
                for (var i = 0; i < config.AdGlobalSetting.Retry.PostRetry; i++)
                {
                    try
                    {
                        Console.WriteLine($"PostAds try {i}");
                        PostSingleAd(adDetail);
                        Console.WriteLine("PostAds succeed, so break retry loop");
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"There was an error during PostAds {e.Message}, {e.StackTrace} - proceed retry");
                    }
                }
            }
        }

        private void PostSingleAd(AdDetails adDetail)
        {
            var random = new Random();
            webDriver.Navigate().GoToUrl("https://www.kijiji.ca/?siteLocale=en_CA");
            ClickPost();
            Thread.Sleep(config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            SubmitAdTitle(adDetail);
            Thread.Sleep(config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            InputAdDetails(adDetail);
            var randomTime = random.Next(config.AdGlobalSetting.Sleep.DelayAfterEachPost.From,
                config.AdGlobalSetting.Sleep.DelayAfterEachPost.To);
            Console.WriteLine($"Wait DelayAfterEachPost {randomTime} minutes");
            NonBlockedSleepInMinutes(randomTime);
        }

        public void DeleteAds(List<AdDetails> adDetails)
        {
            var exceptionMessages = new List<string>();
            foreach (var adDetail in adDetails)
            {
                try
                {
                    webDriver.Navigate().GoToUrl("https://www.kijiji.ca/m-my-ads/active");
                    var deletePage = new DeletePage(webDriver, config);
                    deletePage.DeleteAd(adDetail);
                    Console.WriteLine($"Wait DelayAfterEachPost {config.AdGlobalSetting.Sleep.DelayBetweenEachDelete} minutes");
                    NonBlockedSleepInMinutes(config.AdGlobalSetting.Sleep.DelayBetweenEachDelete);
                }
                catch (Exception e)
                {
                    exceptionMessages.Add($"Error deleting Ad, error {e.Message}, object: {JsonConvert.SerializeObject(adDetail)}");
                }
            }
            if (exceptionMessages.Any())
            {
                throw new Exception(JsonConvert.SerializeObject(exceptionMessages));
            }
        }

        private void ClickPost()
        {
            Thread.Sleep(config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var postAd = webDriver.FindElements(PostAdButtonLocator);
            if (postAd.Any())
            {
                postAd.First().Click();
            }
        }

        private void InputAdDetails(AdDetails adDetails)
        {
            var adDetailInputPage = new AdDetailInputPage(webDriver,config);
                var success = adDetailInputPage.InputAdDetails(adDetails);
                if (!success)
                {
                    throw new Exception($"Posted ad failed");
                }
        }

        private void SubmitAdTitle(AdDetails adDetails)
        {
            var adTitlePage = new SelectCategoryPage(webDriver, config);
            adTitlePage.SubmitAdTitle(adDetails);
        }

        private void NonBlockedSleepInMinutes(int sleep)
        {
            var minutesToSleep = config.AdGlobalSetting.Sleep.SleepInterval;
            var numberOfSleeps = sleep / minutesToSleep;
            for (var i = 0; i < numberOfSleeps; i++)
            {
                Console.WriteLine($"Wait {minutesToSleep} minutes then reload the page to stay signed in | {i + 1}/{numberOfSleeps}");
                Thread.Sleep(TimeSpan.FromMinutes(minutesToSleep));
            }
        }
    }
}
