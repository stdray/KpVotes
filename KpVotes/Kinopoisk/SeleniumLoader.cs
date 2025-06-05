using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace KpVotes.Kinopoisk;

public class SeleniumLoader(IOptionsSnapshot<SeleniumLoaderOptions> options) : IKpLoader
{
    public async Task<string> Load(Uri uri, CancellationToken cancellation)
    {
        var driverOptions = new FirefoxOptions();
        if (options.Value.Headless)
            driverOptions.AddArgument("--headless"); // Запуск без окна
        using var driver = new FirefoxDriver(driverOptions);
        await driver.Navigate().GoToUrlAsync(uri);
        var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(10));
        string[] selectors = [Const.VotesSelector];
        var cssSelectors = selectors.Select(By.CssSelector).ToList();
        wait.Until(drv => cssSelectors.Any(by => drv.FindElements(by).Any()));
        return driver.PageSource;
    }
}