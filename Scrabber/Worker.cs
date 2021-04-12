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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var options = new ChromeOptions();
                options.AddArgument("window-size=1366x768");
                options.AddArgument("--headless");
                options.AddArgument("log-level=3");
                var _browser = new ChromeDriver(options);
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
                        var advert = new Advert();
                        Thread.Sleep(2 * 1000);
                        link.Click();
                        Thread.Sleep(2 * 1000);
                        _browser.SwitchTo().Window(_browser.WindowHandles.Last());
                        Thread.Sleep(2 * 1000);
                        advert.Address = _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[3]/span")).Text;
                        int price;
                        if (Int32.TryParse(
                            _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[4]/div/span[1]")).Text.Replace("р.", "").Replace(" ", "").Replace(".", ""),
                            out price))
                        {
                            advert.Price = price;
                        }
                        else
                        {
                            _browser.Close();
                            _browser.SwitchTo().Window(_browser.WindowHandles.First());
                            continue;
                        }

                        _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[5]/button[1]")).Click(); //телефона может не быть, сделать проверку
                        Thread.Sleep(2 * 1000);

                        advert.Phone = _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[5]/div[2]/div/div/a[1]"))
                            .Text.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+375", "80");
                        _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[5]/div[2]/span")).Click();
                        Thread.Sleep(2 * 1000);


                        var words = _browser.FindElement(By.XPath("//div[@data-name='size']/following::div"))
                            ?.Text.Replace("м²", "").Replace(" ", "").Split(new char[] { '.' });
                        int area;
                        if (Int32.TryParse(words[0], out area))
                        {
                            advert.Area = area;
                        }
                        else
                        {
                            _browser.Close();
                            _browser.SwitchTo().Window(_browser.WindowHandles.First());
                            continue;
                        }

                        advert.Description = _browser.FindElement(By.XPath("//div[@id='description']/*[2]")).Text;

                        var imgsLinksCollection = _browser
                            .ExecuteScript("return Array.prototype.slice.call(document.querySelector('#photo').querySelectorAll('img[src*=\"images\"]')).map(function (item){return item.getAttribute('src')})");
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
                        _browser.Close();
                        _browser.SwitchTo().Window(_browser.WindowHandles.First());
                    }
                }
                catch (NoSuchElementException )
                {
                    _browser.Close();
                    _browser.SwitchTo().Window(_browser.WindowHandles.First());
                }
                catch (Exception ex)
                {
                   Console.WriteLine(ex.Message, ex.StackTrace);
                }
                finally
                {
                    _browser.Quit();
                    _browser.Dispose();
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
