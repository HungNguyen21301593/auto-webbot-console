using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using auto_webbot.Model;

namespace auto_webbot.Pages
{
    public class SigninPage
    {
        private readonly AppSetting Config;
        public IWebDriver webDriver { get; set; }
        public SigninPage(IWebDriver webDriver, Model.AppSetting config)
        {
            Config = config;
            this.webDriver = webDriver;
        }
        private WebDriverWait WebWaiter => new WebDriverWait(webDriver, TimeSpan.FromSeconds(120));
        private By EmailLocaltor = By.Id("emailOrNickname");
        private By PassLocaltor = By.Id("password");
        private By SubmitLocaltor = By.CssSelector("button[class*='signInButton']");


        public void Login(string email, string pass)
        {
            Thread.Sleep(Config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var emailElement = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .ElementIsVisible(EmailLocaltor));
            emailElement.SendKeys(email);

            Thread.Sleep(Config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var passElement = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .ElementIsVisible(PassLocaltor));
            passElement.SendKeys(pass);

            Thread.Sleep(Config.AdGlobalSetting.Sleep.SleepBetweenEachAction);
            var submitElement = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .ElementToBeClickable(SubmitLocaltor));
            submitElement.Click();
        }
    }
}
