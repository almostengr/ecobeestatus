namespace Almostengr.InternetMonitor.Model
{
    public class AppSettings
    {
        public HomeAssistant HomeAssistant { get; set; }
        public string EcobeeStatusUrl { get; set; }
    }

    public class HomeAssistant
    {
        public string Url { get; set; }
        public string Token { get; set; }
    }
}
