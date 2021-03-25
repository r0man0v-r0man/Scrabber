using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Scrabber
{
    public class Worker : BackgroundService
    {
        public static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var _browser = new ChromeDriver();
                try
                {
                    _browser.Manage().Window.Maximize();
                    _browser.Navigate().GoToUrl(config["scrapper:kufar:baselink"]);
                    SignInToKufar(_browser);
                    _browser.Navigate().GoToUrl(config["scrapper:kufar:secondaryLink"]);
                    Thread.Sleep(3 * 1000);

                    var links = _browser
                        .FindElements(By.XPath("/html/body/div[1]/div/div[1]/main/div/div/div[3]/div[1]/article/div/div/a[*]"));
                    foreach (var link in links)
                    {
                        Thread.Sleep(2 * 1000);
                        link.Click();
                        Thread.Sleep(2 * 1000);
                        _browser.SwitchTo().Window(_browser.WindowHandles.Last());
                        Thread.Sleep(2 * 1000);
                        _browser.Close();
                        _browser.SwitchTo().Window(_browser.WindowHandles.First());
                    }
                }
                catch (WebDriverException)
                {
                    throw;
                }
                finally
                {
                    _browser.Quit();
                    _browser.Dispose();
                }

                await Task.Delay(10 * 1000, stoppingToken);
            }
        }
        private void SignInToKufar(IWebDriver driver)
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
