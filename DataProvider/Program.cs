using Microsoft.Extensions.Configuration;
using PolygonData;
using System.Text.Json;
using SQLDatabase;
using System.Numerics;

namespace DataProvider {
    internal class Program {
        static async Task Main() {
            bool running = true;

            while (running) {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1: Get New Data from API");
                Console.WriteLine("2: Read Data from database");
                Console.WriteLine("3: Write Data to database");
                Console.WriteLine("4: Exit\n");

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
            Console.WriteLine("Data loaded\n");

            /*
             * string fileName = "C:\\Users\\willi\\OneDrive\\Documents\\SPS\\Computer Science\\NEA\\Backend\\DataProvider\\DataProvider\\DataFiles\\PolygonData.txt";
             * await SaveDataToFileAsync(polygonPriceData, fileName);
            */

            Console.WriteLine("Would you like to save data to the local database?");
            Console.WriteLine("1: Yes");
            Console.WriteLine("Any Key: No\n");

            string option = Console.ReadLine() ?? string.Empty;
            if (option == "1") {
                DatabaseHandler databaseHandler = ConnectToDatabase();

                await SaveDataToDatabaseFromAPIAsync(polygonPriceData, databaseHandler);
            }
        }

        private static async Task SaveDataToDatabaseFromAPIAsync(PolygonStockPriceData polygonPriceData, DatabaseHandler handler) {
            if (polygonPriceData != null) {
                string symbol = polygonPriceData.Ticker;

                List<Result> results = polygonPriceData.Results;
                int count = 0;
                //Temporary values, these are all in the results
                foreach (Result result in results) {
                    DateTimeOffset offset = DateTimeOffset.FromUnixTimeMilliseconds(result.Timestamp);
                    string pxDate = offset.Date.ToShortDateString();
                    double openPx = result.Open;
                    double closePx = result.Close;
                    double highPx = result.High;
                    double lowPx = result.Low;
                    double volume = result.Volume;
                    
                    try {
                        await handler.AddPriceDataAsync(symbol, pxDate, openPx, closePx, highPx, lowPx, volume);
                        count++;
                    } catch (Exception ex) {
                        Console.WriteLine($"An error occurred while adding price data: {ex.Message}\n");
                    }
                }
                Console.WriteLine($"{count} entries saved to database");
            }
        }

        public static async Task ReadFromDatabaseAsync() {
            DatabaseHandler databaseHandler = ConnectToDatabase();
            bool connected = true;

            while (connected) {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1: View Instrument data");
                Console.WriteLine("2: View Price data");
                Console.WriteLine("3: Exit\n");

                //Need to validate the instrument symbols somehow but not sure yet
                string option = Console.ReadLine() ?? string.Empty;

                switch (option) {
                    case "1":
                        await GetInstrumentsAsync(databaseHandler);
                        break;
                    case "2":
                        await GetPriceDataAsync(databaseHandler);
                        break;
                    case "3":
                        connected = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.\n");
                        break;
                }
            }
        }

        public static async Task WriteToDatabaseAsync() {
            DatabaseHandler databaseHandler = ConnectToDatabase();
            bool connected = true;

            while (connected) {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1: Add Instrument Data");
                Console.WriteLine("2: Add Price Data");
                Console.WriteLine("3: Exit\n");

                //Need to validate the instrument symbols somehow but not sure yet
                string option = Console.ReadLine() ?? string.Empty;

                switch (option) {
                    case "1":
                        await AddInstrumentsAsync(databaseHandler);
                        break;
                    case "2":
                        await AddPriceDataAsync(databaseHandler);
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

        private static DatabaseHandler ConnectToDatabase() {
            var config = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();

            string connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;

            Console.WriteLine("Connecting to database...\n");
            DatabaseHandler databaseHandler = new DatabaseHandler(connectionString);
            return databaseHandler;
        }
        private static async Task AddInstrumentsAsync(DatabaseHandler handler) {
            // Need to add validation for inputs later
            Console.Write("Enter Instrument Name: ");
            string name = Console.ReadLine() ?? string.Empty;

            Console.Write("Enter Instrument Symbol: ");
            string symbol = Console.ReadLine() ?? string.Empty;

            Console.Write("Enter Instrument Type (e.g., Stock, Bond): ");
            string type = Console.ReadLine() ?? string.Empty;

            Console.Write("Enter Instrument Currency (e.g., USD, EUR): ");
            string currency = Console.ReadLine() ?? string.Empty;

            try {
                await handler.AddInstrumentDataAsync(name, symbol, type, currency);
                Console.WriteLine("Instrument data added successfully.\n");
            } catch (Exception ex) {
                Console.WriteLine($"An error occurred while adding instrument data: {ex.Message}\n");
            }
        }

        private static async Task AddPriceDataAsync(DatabaseHandler handler) {
            Console.Write("Enter Instrument Symbol: ");
            string symbol = Console.ReadLine() ?? string.Empty;

            Console.Write("Enter Price Date (yyyy-MM-dd): ");
            string pxDate = Console.ReadLine() ?? string.Empty;

            Console.Write("Enter Opening Price: ");
            double openPx;
            while (!double.TryParse(Console.ReadLine(), out openPx)) {
                Console.WriteLine("Invalid input. Please enter a valid opening price: ");
            }

            Console.Write("Enter Closing Price: ");
            double closePx;
            while (!double.TryParse(Console.ReadLine(), out closePx)) {
                Console.WriteLine("Invalid input. Please enter a valid closing price: ");
            }

            Console.Write("Enter High Price: ");
            double highPx;
            while (!double.TryParse(Console.ReadLine(), out highPx)) {
                Console.WriteLine("Invalid input. Please enter a valid high price: ");
            }

            Console.Write("Enter Low Price: ");
            double lowPx;
            while (!double.TryParse(Console.ReadLine(), out lowPx)) {
                Console.WriteLine("Invalid input. Please enter a valid low price: ");
            }

            Console.Write("Enter Volume: ");
            int volume;
            while (!int.TryParse(Console.ReadLine(), out volume)) {
                Console.WriteLine("Invalid input. Please enter a valid volume: ");
            }

            try {
                await handler.AddPriceDataAsync(symbol, pxDate, openPx, closePx, highPx, lowPx, volume);
                Console.WriteLine("Price data added successfully.\n");
            } catch (Exception ex) {
                Console.WriteLine($"An error occurred while adding price data: {ex.Message}\n");
            }
        }



        private static async Task GetInstrumentsAsync(DatabaseHandler handler) {
            Console.Write("Enter Instrument Symbol: ");
            string symbol = Console.ReadLine() ?? string.Empty;

            List<Instrument> instruments = await handler.GetInstrumentDataAsync(symbol);

            if (instruments.Any()) {
                Console.WriteLine($"{symbol} Data:\n");

                foreach (Instrument instrument in instruments) {
                    Console.WriteLine($"ID: {instrument.InstrumentID}");
                    Console.WriteLine($"Name: {instrument.InstrumentName}");
                    Console.WriteLine($"Symbol: {instrument.InstrumentSymbol}");
                    Console.WriteLine($"Type: {instrument.InstrumentType}");
                    Console.WriteLine($"Currency: {instrument.InstrumentCurrency}\n");
                }
            } else {
                Console.WriteLine("No data found\n");
            }
        }

        private static async Task GetPriceDataAsync(DatabaseHandler handler) {
            Console.Write("Enter Instrument Symbol: ");
            string symbol = Console.ReadLine() ?? string.Empty;

            List<PriceData> priceDataList = await handler.GetPriceDataAsync(symbol);

            if (priceDataList.Any()) {
                Console.WriteLine($"{symbol} Data:\n");

                foreach (PriceData priceData in priceDataList) {
                    Console.WriteLine($"PriceID: {priceData.PriceID}");
                    Console.WriteLine($"InstrumentID: {priceData.InstrumentID}");
                    Console.WriteLine($"Price Timestamp: {priceData.PxDate}");
                    Console.WriteLine($"Opening Price: {priceData.OpenPx}");
                    Console.WriteLine($"Closing Price: {priceData.ClosePx}");
                    Console.WriteLine($"High Price: {priceData.HighPx}");
                    Console.WriteLine($"Low Price: {priceData.LowPx}");
                    Console.WriteLine($"Volume: {priceData.Volume}\n");
                }
            } else {
                Console.WriteLine("No data found\n");
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
                    Console.WriteLine($"Data saved to {filePath}\n");
                }
            } catch (IOException) {
                Console.WriteLine("\nFile already exists...\nAppending to file...\n");

                using (StreamWriter sw = File.AppendText(filePath)) {
                    await sw.WriteAsync(output);
                    Console.WriteLine($"Data saved to {filePath}\n");
                }
            }
        }
    }
}