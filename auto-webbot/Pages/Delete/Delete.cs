using auto_webbot.Model;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using System.Threading;

namespace auto_webbot.Pages.Delete
{
    public class DeletePage
    {
        public IWebDriver webDriver { get; set; }
        private AppSetting config { get; set; }
        public DeletePage(IWebDriver webDriver, AppSetting config)
        {
            this.webDriver = webDriver;
            this.config = config;
        }

        private WebDriverWait WebWaiter => new WebDriverWait(webDriver, TimeSpan.FromSeconds(20));
        private By DeleteButtonLocator = By.XPath("//*[text()='Delete']");
        private By ReaonToDeleteLocator = By.XPath("//*[text()='Prefer not to say']");
        private By ProceedDeleteLocator = By.XPath("//*[text()='Delete My Ad']");

        public void DeleteAd(AdDetails adDetails)
        {
            Thread.Sleep(1000);
            var adUrlElements = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .VisibilityOfAllElementsLocatedBy(By.PartialLinkText(adDetails.AdTitle)));

            if (adUrlElements.Any())
            {
                Thread.Sleep(1000);
                adUrlElements.First().Click();
                var deleteButton = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .ElementIsVisible(DeleteButtonLocator));
                deleteButton.Click();

                var reason = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .ElementIsVisible(ReaonToDeleteLocator));
                reason.Click();

                Thread.Sleep(1000);
                var ProceedDeletes = WebWaiter
                .Until(SeleniumExtras
                    .WaitHelpers
                    .ExpectedConditions
                    .VisibilityOfAllElementsLocatedBy(ProceedDeleteLocator));
                var proceedDelete = ProceedDeletes
                    .Where(l => l.TagName == "button")
                    .FirstOrDefault();
                if (proceedDelete is null)
                {
                    throw new Exception("could not find the delete button");
                }
                if (config.Mode == Mode.test)
                {
                    Console.WriteLine("App is in testing mode, so no Ad will be deleted");
                }
                else
                {
                    proceedDelete.Click();
                }
            }
        }
    }
}
