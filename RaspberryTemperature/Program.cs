using Iot.Device.OneWire;
using CsvHelper;
using System.Globalization;
using Newtonsoft.Json;

namespace RaspberryTemperature
{
    class RaspberryTemperature
    {         
        private static readonly string filePath = "/home/pi/Desktop/measurements.csv";
        private static readonly string url = "https://webhook.site/3eff33aa-03e4-488f-9986-ac485104e09d";
        static void Main(string[] args)
        {
            //Wczytanie pliku z pamięci
            var measurements = new List<Measurement>();
            if(File.Exists(filePath))
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    measurements = csv.GetRecords<Measurement>().ToList();
                } 
            }
            if(measurements.Any())
            {
                var lastTemperature = measurements.OrderByDescending(m => m.Timestamp).Last().Temperature;
                Console.WriteLine($"Poprzednia temperatura: {lastTemperature.ToString("F3")}\u00B0C");
            }
            
            //Pomiar tempratury z sensora DS18B20
            var sensor = OneWireThermometerDevice.EnumerateDevices().FirstOrDefault();
            var temperature = sensor?.ReadTemperature() ?? new UnitsNet.Temperature();
            Console.WriteLine($"Aktualna temperatura: {temperature.DegreesCelsius.ToString("F3")}\u00B0C");

            //Zapis wyniku do pliku
            var record = new Measurement
            {
                Temperature = temperature.DegreesCelsius,
                Timestamp = DateTime.Now
            };
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(new List<Measurement>{record});
            }

            //Wysłanie zapytania HTTP
            var json = JsonConvert.SerializeObject(record);
            var requestContent = new StringContent(json);
            var httpClient = new HttpClient();
            var httpResult = httpClient.PostAsync(url, requestContent).Result;
            if(httpResult.IsSuccessStatusCode)
            {
                Console.WriteLine("Pomyślnie przesłano dane");
            }
            else
            {
                Console.WriteLine($"Błąd podczas przesyłania danych, kod HTTP: {httpResult.StatusCode}");
            }
        }
    }

    public class Measurement
    {
        public DateTime Timestamp {get;set;}
        public double Temperature {get;set;}
    }
}
