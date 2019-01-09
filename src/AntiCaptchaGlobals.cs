using System.Net.Http;

namespace AntiCaptcha
{
    public class AntiCaptchaGlobals
    {
        internal static readonly HttpClient HttpClient;
        public static int SoftId { get; set; }
        public static int CaptchaRetryLimit { get; set; }

        static AntiCaptchaGlobals()
        {
            HttpClient = new HttpClient();
            SoftId = 865;
            CaptchaRetryLimit = 10;
        }

    }
}
