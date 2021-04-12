using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly IDbContextFactory<ScrabberContext> _contextFactory;

        public Worker(IDbContextFactory<ScrabberContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        private ChromeDriver GetChromeDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("window-size=1366x768");
            options.AddArgument("--headless");
            options.AddArgument("log-level=3");
            return new ChromeDriver(options);
        }
        private void FinilizeChromeDriver(ChromeDriver browser)
        {
            browser.Quit();
            browser.Dispose();
        }
        private void CloseTab(ChromeDriver browser)
        {
            browser.Close();
            browser.SwitchTo().Window(browser.WindowHandles.First());
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var _browser = GetChromeDriver();
                try
                {
                    _browser.Manage().Window.Maximize();
                    _browser.Navigate().GoToUrl(config["scrapper:kufar:baselink"]);
                    SignInToKufar(_browser);
                    _browser.Navigate().GoToUrl(config["scrapper:kufar:secondaryLink"]);
                    Thread.Sleep(3 * 1000);

                    var links = _browser
                        .FindElements(By.XPath(config["scrapper:kufar:elements:links"]));
                    foreach (var link in links)
                    {
                        var advert = new Advert();
                        Thread.Sleep(2 * 1000);
                        link.Click();
                        Thread.Sleep(2 * 1000);
                        _browser.SwitchTo().Window(_browser.WindowHandles.Last());
                        Thread.Sleep(2 * 1000);
                        advert.Address = _browser.FindElement(By.XPath(config["scrapper:kufar:elements:address"])).Text;
                        int price;
                        if (Int32.TryParse(_browser.FindElement(By.XPath(config["scrapper:kufar:elements:price"])).Text.Replace("р.", "").Replace(" ", "").Replace(".", ""), out price))
                        {
                            advert.Price = price;
                        }
                        else
                        {
                            CloseTab(_browser);
                            continue;
                        }

                        _browser.FindElement(By.XPath(config["scrapper:kufar:elements:phoneShowBtn"])).Click();
                        Thread.Sleep(2 * 1000);

                        advert.Phone = _browser.FindElement(By.XPath(config["scrapper:kufar:elements:phone"]))
                            .Text.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+375", "80");
                        _browser.FindElement(By.XPath(config["scrapper:kufar:elements:phoneCloseBtn"])).Click();
                        Thread.Sleep(2 * 1000);


                        var words = _browser.FindElement(By.XPath(config["scrapper:kufar:elements:area"]))
                            ?.Text.Replace("м²", "").Replace(" ", "").Split(new char[] { '.' });
                        int area;
                        if (Int32.TryParse(words[0], out area))
                        {
                            advert.Area = area;
                        }
                        else
                        {
                            CloseTab(_browser);
                            continue;
                        }

                        advert.Description = _browser.FindElement(By.XPath(config["scrapper:kufar:elements:desciption"])).Text;

                        var imgsLinksCollection = _browser.ExecuteScript(config["scrapper:kufar:scripts:getPhoto"]);

                        var listOfLinks = new List<Image>();
                        foreach (var item in (ReadOnlyCollection<Object>)imgsLinksCollection)
                        {
                            listOfLinks.Add(new Image
                            {
                                Link = item as string
                            });
                        }

                        var imgsSrcList = listOfLinks.Distinct();
                        advert.Images.AddRange(imgsSrcList);
                        using var context = _contextFactory.CreateDbContext();
                        await context.Adverts.AddAsync(advert);
                        await context.SaveChangesAsync();
                        CloseTab(_browser);
                    }
                }
                catch (NoSuchElementException)
                {
                    CloseTab(_browser);
                }
                catch (Exception ex)
                {
                   Console.WriteLine(ex.Message, ex.StackTrace);
                }
                finally
                {
                    FinilizeChromeDriver(_browser);
                }

                await Task.Delay(10 * 10000, stoppingToken);
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
