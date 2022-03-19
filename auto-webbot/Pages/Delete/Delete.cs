using auto_webbot.Model;
using OpenQA.Selenium;
using System;
using System.Linq;
using System.Threading;

namespace auto_webbot.Pages.Delete
{
    public class DeletePage
    {
        public IWebDriver WebDriver { get; set; }
        private AppSetting Config { get; set; }
        public DeletePage(IWebDriver webDriver, AppSetting config)
        {
            this.WebDriver = webDriver;
            this.Config = config;
        }

        private readonly By DeleteButtonLocator = By.XPath("//*[text()='Delete']");
        private readonly By ReasonToDeleteLocator = By.XPath("//*[text()='Prefer not to say']");
        private readonly By ProceedDeleteLocator = By.XPath("//*[text()='Delete My Ad']");

        public void DeleteAd(AdDetails adDetails)
        {
            for (var i = 0; i < Config.AdGlobalSetting.Retry.DeteleRetry; i++)
            {
                try
                {
                    Console.WriteLine($"DeleteAds try {i}");
                    proceedDeleteSingleAd(adDetails);
                    Console.WriteLine("DeleteAds succeed, break retry loop");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"There was an error during DeleteAds {e.Message} - proceed retry");
                }
            }
            
        }

        private void proceedDeleteSingleAd(AdDetails adDetails)
        {
            Thread.Sleep(Config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var proceededAdtitle = ProcessAdtitleSpecialCharacters(adDetails.AdTitle);
            var adUrlElements = WebDriver.FindElements(By.XPath($"//*[text()='{proceededAdtitle}']"));

            if (!adUrlElements.Any()) return;
            Thread.Sleep(Config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            adUrlElements.First().Click();

            Thread.Sleep(Config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var deleteButton = WebDriver.FindElements(DeleteButtonLocator);
            if (deleteButton.Any())
            {
                deleteButton.First().Click();
            }

            Thread.Sleep(Config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var reason = WebDriver.FindElements(ReasonToDeleteLocator);
            if (reason.Any())
            {
                reason.First().Click();
            }

            Thread.Sleep(Config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var proceedDeletes = WebDriver.FindElements(ProceedDeleteLocator);
            if (!proceedDeletes.Any()) return;
            var proceedDelete = proceedDeletes
                .FirstOrDefault(l => l.TagName == "button");
            if (proceedDelete is null)
            {
                throw new Exception("could not find the delete button");
            }

            if (Config.Mode == Mode.test)
            {
                Console.WriteLine("App is in testing mode, so no Ad will be deleted");
            }
            else
            {
                proceedDelete.Click();
            }
        }

        private string ProcessAdtitleSpecialCharacters(string adtitle)
        {
            string outputStr = adtitle.Split('\'')[0];
            return outputStr;
        }
    }
}
