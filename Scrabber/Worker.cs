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
                    var listAdverts = new List<Advert>();
                    foreach (var link in links)
                    {
                        var advert = new Advert();
                        Thread.Sleep(2 * 1000);
                        link.Click();
                        Thread.Sleep(2 * 1000);
                        _browser.SwitchTo().Window(_browser.WindowHandles.Last());
                        Thread.Sleep(2 * 1000);
                        advert.Address = _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[3]/span")).Text;
                        advert.Price = Convert.ToInt32((_browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[4]/div/span[1]")).Text).Replace("р.", "").Replace(" ", ""));

                        _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[5]/button[1]")).Click(); //телефона может не быть, сделать проверку
                        Thread.Sleep(2 * 1000);

                       advert.Phone = _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[5]/div[2]/div/div/a[1]")).Text.Replace(" ", "").Replace("(", "").Replace(")", "");
                        _browser.FindElement(By.XPath("/html/body/div[1]/div/div[1]/main/div/div[3]/div[2]/div[2]/div[5]/div[2]/span")).Click();
                        Thread.Sleep(2 * 1000);

                        advert.Area = (int)Math.Round(Convert.ToDecimal(_browser.FindElement(By.XPath("//div[@data-name='size']/following::div")).Text.Replace("м²", "").Replace(" ", ""))); //сделать проверку
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
                        listAdverts.Add(advert);
                        _browser.Close();
                        _browser.SwitchTo().Window(_browser.WindowHandles.First());
                    }

                    using var context = _contextFactory.CreateDbContext();
                    await context.Adverts.AddRangeAsync(listAdverts);
                    await context.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                   Console.WriteLine(ex.Message);
                }
                finally
                {
                    _browser.Quit();
                    _browser.Dispose();
                }

                await Task.Delay(100 * 1000, stoppingToken);
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
