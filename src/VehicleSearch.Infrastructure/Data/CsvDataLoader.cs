using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;

namespace VehicleSearch.Infrastructure.Data;

/// <summary>
/// Service for loading data from CSV files.
/// </summary>
public class CsvDataLoader
{
    private readonly ILogger<CsvDataLoader> _logger;
    private const string NullDateValue = "01/01/0001";

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvDataLoader"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public CsvDataLoader(ILogger<CsvDataLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads vehicles from a CSV stream.
    /// </summary>
    /// <param name="csvStream">The CSV stream to parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing vehicles, equipment, and declarations.</returns>
    public async Task<(IEnumerable<Vehicle> Vehicles, Dictionary<string, string> Equipment, Dictionary<string, string> Declarations)> 
        LoadFromStreamAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var vehicles = new List<Vehicle>();
        var equipment = new Dictionary<string, string>();
        var declarations = new Dictionary<string, string>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = context =>
            {
                _logger.LogWarning("Malformed row at line {RowNumber}: {RawRecord}", 
                    context.Context.Parser.Row, context.RawRecord);
            },
            Encoding = Encoding.UTF8
        };

        using var reader = new StreamReader(csvStream, Encoding.UTF8, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        // Register custom converters
        csv.Context.TypeConverterCache.AddConverter<DateTime>(new UkDateTimeConverter());
        csv.Context.TypeConverterCache.AddConverter<DateTime?>(new UkNullableDateTimeConverter());
        csv.Context.TypeConverterCache.AddConverter<bool>(new YesNoBooleanConverter());

        csv.Context.RegisterClassMap<VehicleCsvMap>();

        try
        {
            await csv.ReadAsync();
            csv.ReadHeader();
            
            var equipmentIndex = -1;
            var declarationsIndex = -1;
            
            // Find Equipment and Declarations column indices
            if (csv.HeaderRecord != null)
            {
                for (int i = 0; i < csv.HeaderRecord.Length; i++)
                {
                    if (csv.HeaderRecord[i] == "Equipment")
                        equipmentIndex = i;
                    else if (csv.HeaderRecord[i] == "Declarations")
                        declarationsIndex = i;
                }
            }

            while (await csv.ReadAsync())
            {
                var vehicle = csv.GetRecord<Vehicle>();
                if (vehicle != null)
                {
                    vehicles.Add(vehicle);

                    // Store raw equipment and declarations
                    if (equipmentIndex >= 0 && equipmentIndex < csv.Parser.Record?.Length)
                    {
                        equipment[vehicle.Id] = csv.Parser.Record[equipmentIndex] ?? string.Empty;
                    }

                    if (declarationsIndex >= 0 && declarationsIndex < csv.Parser.Record?.Length)
                    {
                        declarations[vehicle.Id] = csv.Parser.Record[declarationsIndex] ?? string.Empty;
                    }
                }
            }

            _logger.LogInformation("Successfully loaded {Count} vehicles from CSV", vehicles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicles from CSV");
            throw;
        }

        return (vehicles, equipment, declarations);
    }

    /// <summary>
    /// CSV class map for Vehicle entity.
    /// </summary>
    private sealed class VehicleCsvMap : ClassMap<Vehicle>
    {
        public VehicleCsvMap()
        {
            Map(m => m.Id).Name("Registration Number");
            Map(m => m.Make).Name("Make");
            Map(m => m.Model).Name("Model");
            Map(m => m.Derivative).Name("Derivative").Optional();
            Map(m => m.BodyType).Name("Body").Optional();
            Map(m => m.Price).Name("Buy Now Price");
            Map(m => m.Mileage).Name("Mileage");
            Map(m => m.EngineSize).Name("Engine Size").Optional();
            Map(m => m.FuelType).Name("Fuel").Optional();
            Map(m => m.TransmissionType).Name("Transmission").Optional();
            Map(m => m.Colour).Name("Colour").Optional();
            Map(m => m.NumberOfDoors).Name("Number Of Doors").Optional();
            Map(m => m.RegistrationDate).Name("Registration Date").Optional();
            Map(m => m.SaleLocation).Name("Sale Location").Optional();
            Map(m => m.Channel).Name("Channel").Optional();
            Map(m => m.SaleType).Name("Sale Type").Optional();
            Map(m => m.Grade).Name("Grade").Optional();
            Map(m => m.ServiceHistoryPresent).Name("Service History Present").Optional();
            Map(m => m.NumberOfServices).Name("Number of Services").Optional();
            Map(m => m.LastServiceDate).Name("Last Service Date").Optional();
            Map(m => m.MotExpiryDate).Name("MOT Expiry").Optional();
            Map(m => m.VatType).Name("VAT Type").Optional();
            Map(m => m.AdditionalInfo).Name("Additional Information").Optional();
            Map(m => m.CapRetailPrice).Name("Cap Retail Price").Optional();
            Map(m => m.CapCleanPrice).Name("Cap Clean Price").Optional();
            
            // These fields will be handled by normalizer
            Map(m => m.Features).Ignore();
            Map(m => m.Declarations).Ignore();
            Map(m => m.Description).Ignore();
            Map(m => m.ProcessedDate).Ignore();
        }
    }

    /// <summary>
    /// Custom DateTime converter for UK date format (dd/MM/yyyy).
    /// </summary>
    private class UkDateTimeConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Handle the special null date value
            if (text == NullDateValue)
                return null;

            if (DateTime.TryParseExact(text, new[] { "dd/MM/yyyy", "d/M/yyyy" }, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            return null;
        }
    }

    /// <summary>
    /// Custom nullable DateTime converter for UK date format (dd/MM/yyyy).
    /// </summary>
    private class UkNullableDateTimeConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Handle the special null date value
            if (text == NullDateValue)
                return null;

            if (DateTime.TryParseExact(text, new[] { "dd/MM/yyyy", "d/M/yyyy" }, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return (DateTime?)date;
            }

            return null;
        }
    }

    /// <summary>
    /// Custom Boolean converter for Yes/No values.
    /// </summary>
    private class YesNoBooleanConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            return text.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                   text == "1";
        }
    }
}
