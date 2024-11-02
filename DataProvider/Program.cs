using Microsoft.Extensions.Configuration;
using PolygonData;
using System.Text.Json;
using SQLDatabase;

namespace DataProvider {
    internal class Program {
        static async Task Main() {
            bool running = true;

            while (running) {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1: Get New Data from API");
                Console.WriteLine("2: Get Data from database");
                Console.WriteLine("3: Write Data to database");
                Console.WriteLine("4: Exit");

                string option = Console.ReadLine() ?? string.Empty;

                switch (option) {
                    case "1":
                        await LoadDataAsync();
                        break;
                    case "2":
                        await ReadFromDatabaseAsync();
                        break;
                    case "3":
                        await WriteToDatabaseAsync();
                        break;
                    case "4":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid input. Please try again.");
                        break;
                }
            }
        }


        public static async Task LoadDataAsync() {
            var config = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();

            string baseUrl = config["BaseUrl"] ?? string.Empty;
            string apiKey = config["ApiKey"] ?? string.Empty;

            List<object> dataParameters = GetDataParameters();

            Console.WriteLine("Loading data...");
            PolygonDataLoader polygonLoader = new PolygonDataLoader(
                baseUrl,
                apiKey,
                Convert.ToString(dataParameters[0]) ?? string.Empty,
                Convert.ToInt32(dataParameters[1]),
                Convert.ToString(dataParameters[2]) ?? string.Empty,
                Convert.ToString(dataParameters[3]) ?? string.Empty,
                Convert.ToString(dataParameters[4]) ?? string.Empty
            );

            PolygonStockPriceData polygonPriceData = await polygonLoader.loadPolygonStockDataAsync();
            Console.WriteLine("Data loaded");

            string fileName = "C:\\Users\\willi\\OneDrive\\Documents\\SPS\\Computer Science\\NEA\\Backend\\DataProvider\\DataProvider\\DataFiles\\PolygonData.txt";
            await SaveDataToFileAsync(polygonPriceData, fileName);
        }

        public static async Task ReadFromDatabaseAsync() {
            var config = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();

            string connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;

            Console.WriteLine("Connecting to database...");
            MarketDataDb database = new MarketDataDb(connectionString);
            bool connected = true;

            while (connected) {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1: View Instrument data");
                Console.WriteLine("2: View Price data");
                Console.WriteLine("3: Exit");

                //Need to validate the instrument symbols somehow but not sure yet
                string option = Console.ReadLine() ?? string.Empty;

                switch (option) {
                    case "1":
                        await GetInstrumentsAsync(database);
                        break;
                    case "2":
                        await GetPriceDataAsync(database);
                        break;
                    case "3":
                        connected = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        public static async Task WriteToDatabaseAsync() {

        }

        private static async Task GetInstrumentsAsync(MarketDataDb db) {
            Console.Write("Enter Instrument Symbol: ");
            string symbol = Console.ReadLine() ?? string.Empty;

            List<Instrument> instruments = await db.GetInstrumentDataAsync(symbol);

            if (instruments != null) {
                Console.WriteLine($"{symbol} Data:");

                foreach (Instrument instrument in instruments) {
                    Console.WriteLine($"ID: {instrument.InstrumentID}");
                    Console.WriteLine($"Name: {instrument.InstrumentName}");
                    Console.WriteLine($"Symbol: {instrument.InstrumentID}");
                    Console.WriteLine($"Type: {instrument.InstrumentID}");
                    Console.WriteLine($"Currency: {instrument.InstrumentID}");
                }
            } else {
                Console.WriteLine("No data found");
            }
        }

        private static async Task GetPriceDataAsync(MarketDataDb db) {
            Console.Write("Enter Instrument Symbol: ");
            string symbol = Console.ReadLine() ?? string.Empty;

            List<PriceData> priceDataList = await db.GetPriceDataAsync(symbol);

            if (priceDataList != null) {
                Console.WriteLine($"{symbol} Data:");

                foreach (PriceData priceData in priceDataList) {
                    Console.WriteLine($"PriceID: {priceData.PriceID}");
                    Console.WriteLine($"InstrumentID: {priceData.InstrumentID}");
                    Console.WriteLine($"Price Timestamp: {priceData.PxDate}");
                    Console.WriteLine($"Opening Price: {priceData.OpenPx}");
                    Console.WriteLine($"Closing Price: {priceData.ClosePx}");
                    Console.WriteLine($"High Price: {priceData.HighPx}");
                    Console.WriteLine($"Low Price: {priceData.LowPx}");
                    Console.WriteLine($"Volume: {priceData.Volume}");
                }
            } else {
                Console.WriteLine("No data found");
            }
        }

        private static List<object> GetDataParameters() {
            //Need to add validation for these but assume they are correct for now
            /*Examples
             * ticker: "AAPL"
             * multiplier: 1
             * timespan: day
             * dateFrom: 2023-01-09
             * dateTo: 2023-02-10
            */

            Console.Write("Ticker: ");
            string ticker = Console.ReadLine() ?? string.Empty;

            Console.Write("Multiplier: ");
            string multiplier = Console.ReadLine() ?? string.Empty;
            int convertedMultiplier = int.TryParse(multiplier, out int result) == true ? result : 1;

            Console.Write("Timespan: ");
            string timespan = Console.ReadLine() ?? string.Empty;

            Console.Write("Date From: ");
            string dateFrom = Console.ReadLine() ?? string.Empty;

            Console.Write("Date To: ");
            string dateTo = Console.ReadLine() ?? string.Empty;

            return new List<object> { ticker, convertedMultiplier, timespan, dateFrom, dateTo };
        }


        private static async Task SaveDataToFileAsync(PolygonStockPriceData data, string filePath) {
            string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }


            string jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            string output = $"Data retrieved on {DateTime.Now}:\n\n" + jsonData + "\n\n";

            try {
                FileStream fs = new FileStream(filePath, FileMode.CreateNew);
                using (StreamWriter sw = new StreamWriter(fs)) {
                    Console.WriteLine("Saving data...");
                    await sw.WriteAsync(output);
                    Console.WriteLine($"Data saved to {filePath}");
                }
            } catch (IOException) {
                Console.WriteLine("\nFile already exists...\nAppending to file...\n");

                using (StreamWriter sw = File.AppendText(filePath)) {
                    await sw.WriteAsync(output);
                    Console.WriteLine($"Data saved to {filePath}");
                }
            }
        }
    }
}