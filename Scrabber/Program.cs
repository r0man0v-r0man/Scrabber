using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Linq;
using System.Threading;

namespace webScrabber
{
    class Program
    {
        public static IWebDriver driver = new ChromeDriver();
        public static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();
        static void Main(string[] args)
        {
            driver.Manage().Window.Maximize();
            driver.Navigate().GoToUrl(config["scrapper:kufar:baselink"]);
            loginToKufarWebSite();
            driver.Navigate().GoToUrl(config["scrapper:kufar:secondaryLink"]);
            Thread.Sleep(3 * 1000);

            var links = driver
                .FindElements(By.XPath("/html/body/div[1]/div/div[1]/main/div/div/div[3]/div[1]/article/div/div/a[*]"));
            foreach (var link in links)
            {
                Thread.Sleep(3 * 1000);
                link.Click();
                Thread.Sleep(3 * 1000);
                var browserTabs = driver.WindowHandles;
                driver.SwitchTo().Window(browserTabs.Last());
                Thread.Sleep(3 * 1000);
                driver.Close();
                driver.SwitchTo().Window(browserTabs.First());
            }
        }

        private static void loginToKufarWebSite()
        {
            var popupCloseBtn = driver.FindElement(By.XPath("/html/body/div[2]/div/div[2]/div[1]/div/img"));
            popupCloseBtn.Click();
            Thread.Sleep(3 * 1000);
            var enterBtn = driver.FindElement(By.XPath("/html/body/div[1]/div[1]/div[1]/div[1]/div/div/div[2]/div[3]/div/div/button"));
            enterBtn.Click();
            Thread.Sleep(2 * 1000);
            var emailInput = driver.FindElement(By.Id("email"));
            emailInput.SendKeys(config["scrapper:kufar:login"]);
            Thread.Sleep(4 * 1000);
            var passwordInput = driver.FindElement(By.Id("password"));
            passwordInput.SendKeys(config["scrapper:kufar:password"]);
            Thread.Sleep(1 * 1000);
            var submitEnterCredentialsBtn = driver.FindElement(By.XPath("/html/body/div[1]/div[3]/div/div/div/form/div[4]/button"));
            submitEnterCredentialsBtn.Click();
        }
    }
}
