using System.Globalization;

namespace KeyboardLocker
{
    internal static class Program
    {
        private static Mutex? _mutex;
        [STAThread]
        static void Main()
        {
            const string mutexName = @"Local\KeyboardLocker_GermanEvangelista_SingleInstance";

            _mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                return;
            }

            ApplicationConfiguration.Initialize();

            var savedLanguage = Settings.Default.LanguageCode;

            CultureInfo culture;

            if (!string.IsNullOrWhiteSpace(savedLanguage))
            {
                culture = new CultureInfo(savedLanguage);
            }
            else
            {
                culture = CultureInfo.InstalledUICulture;
            }

            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;

            Application.Run(new TrayApplicationContext());
        }
    }
    
}
