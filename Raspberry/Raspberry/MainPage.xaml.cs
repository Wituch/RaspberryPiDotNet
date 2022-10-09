using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using CsvHelper;
using Windows.Storage;

namespace Raspberry
{
    public sealed partial class MainPage : Page
    {
        private static string filePath;
        private static readonly string url = "https://webhook.site/3eff33aa-03e4-488f-9986-ac485104e09d";

        public MainPage()
        {
            this.InitializeComponent();
            var localFolder = ApplicationData.Current.LocalFolder;
            filePath = $"{localFolder.Path}\\measurements.csv";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Wczytanie pliku z pamięci
            var measurements = new List<Measurement>();
            if (File.Exists(filePath))
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    measurements = csv.GetRecords<Measurement>().ToList();
                }
            }
            if (measurements.Any())
            {
                var lastTemperature = measurements.OrderByDescending(m => m.Timestamp).Last().Temperature;
                txtBoxPrevTemp.Text = lastTemperature.ToString("F3") + "\u00B0C";
            }

            //Symulowany pomiar tempratury z sensora DS18B20
            var random = new Random();
            var temperature = random.Next(20, 35) + random.NextDouble();
            txtBoxTemp.Text = temperature.ToString("F3") + "\u00B0C";

            //Zapis wyniku do pliku
            var record = new Measurement
            {
                Temperature = temperature,
                Timestamp = DateTime.Now
            };
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(new List<Measurement> { record });
            }

            //Wysłanie zapytania HTTP
            var json = JsonConvert.SerializeObject(record);
            var requestContent = new StringContent(json);
            var httpClient = new HttpClient();
            var httpResult = httpClient.PostAsync(url, requestContent).Result;
            if (httpResult.IsSuccessStatusCode)
            {
                txtBlockStatus.Text = "Pomyślnie przesłano dane";
            }
            else
            {
                txtBlockStatus.Text = $"Błąd podczas przesyłania danych, kod HTTP: {httpResult.StatusCode}";
            }
        }

    }

    public class Measurement
    {
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
    }
}
