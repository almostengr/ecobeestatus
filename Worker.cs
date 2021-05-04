using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Almostengr.EcobeeStatus.DataTransfer;
using Almostengr.InternetMonitor.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Almostengr.EcobeeStatus
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppSettings _appSettings;
        private HttpClient _httpClientHA;
        private StringContent _stringContent;
        private IWebDriver driver;

        public Worker(ILogger<Worker> logger, AppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }

        public override void Dispose()
        {
            _httpClientHA.Dispose();
            _stringContent.Dispose();
            base.Dispose();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Ecobee Status Monitor");

            _httpClientHA = new HttpClient();
            _httpClientHA.BaseAddress = new Uri(_appSettings.HomeAssistant.Url);

            ChromeOptions chromeOptions = new ChromeOptions();

#if RELEASE
            chromeOptions.AddArgument("--headless");
#endif

            driver = new ChromeDriver(chromeOptions);

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Ecobee Status Monitor");
            CloseBrowser();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
            driver.Manage().Window.Maximize();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking the status of Ecobee status");

                bool allOnline = CheckEcobeeApiStatus();
                await PostDataToHomeAssistant(allOnline.ToString());

                _logger.LogInformation("Done checking Ecobee status");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }

            CloseBrowser();
        }

        private void CloseBrowser() 
        {
            if (driver != null)
            {
                driver.Quit();
            }
        }

        private bool CheckEcobeeApiStatus()
        {
            try
            {
                driver.Navigate().GoToUrl(_appSettings.EcobeeStatusUrl);
                driver.PageSource.ToLower().Contains("all systems operational");

                _logger.LogInformation("Check completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        private async Task PostDataToHomeAssistant(string sensorData)
        {
            _logger.LogInformation("Sending data to Home Assistant");

            string route = "api/states/sensor.ecobee_api_status";

            try
            {
                SensorState sensorState = new SensorState(sensorData);
                var jsonState = JsonConvert.SerializeObject(sensorState).ToLower();
                _stringContent = new StringContent(jsonState, Encoding.ASCII, "application/json");

                _httpClientHA.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _appSettings.HomeAssistant.Token);

                HttpResponseMessage response = await _httpClientHA.PostAsync(route, _stringContent);

                if (response.IsSuccessStatusCode)
                {
                    HaApiResponse haApiResponse =
                        JsonConvert.DeserializeObject<HaApiResponse>(response.Content.ReadAsStringAsync().Result);
                    _logger.LogInformation(response.StatusCode.ToString());
                    _logger.LogInformation("Updated: " + haApiResponse.Last_Updated.ToString());
                }
                else
                {
                    _logger.LogError(response.StatusCode.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            if (_stringContent != null)
            {
                _stringContent.Dispose();
            }
        }

    }
}
