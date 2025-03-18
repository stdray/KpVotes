using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace KpVotes;

public class SeleniumClient(KpVotesJobOptions jobOptions) : IKpClient
{
    public async Task<string> GetPage(Uri uri)
    {
        var options = new FirefoxOptions();
        if (jobOptions.UserAgent != null)
            options.AddArgument($"--user-agent={jobOptions.UserAgent}");
        options.AddArguments("--headless"); 
        using var driver = new FirefoxDriver(options);
        await driver.Navigate().GoToUrlAsync(uri);
        var captcha = driver.FindElements(By.CssSelector(".CheckboxCaptcha-Button")).SingleOrDefault();
        captcha?.Submit();
        await Task.Delay(TimeSpan.FromSeconds(30));
        return driver.PageSource;
    }
}