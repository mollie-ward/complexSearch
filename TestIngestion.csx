using Microsoft.Extensions.Logging;
using VehicleSearch.Infrastructure.Data;

// Simple end-to-end test
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

var csvLoader = new CsvDataLoader(loggerFactory.CreateLogger<CsvDataLoader>());
var normalizer = new DataNormalizer(loggerFactory.CreateLogger<DataNormalizer>());
var validator = new DataValidator(loggerFactory.CreateLogger<DataValidator>());
var ingestionService = new DataIngestionService(
    csvLoader,
    normalizer,
    validator,
    loggerFactory.CreateLogger<DataIngestionService>());

var csvPath = args.Length > 0 ? args[0] : "sample_vehicles.csv";

Console.WriteLine($"Testing CSV ingestion from: {csvPath}");
Console.WriteLine();

var result = await ingestionService.IngestFromCsvAsync(csvPath);

Console.WriteLine($"✓ Ingestion completed!");
Console.WriteLine($"  Total rows: {result.TotalRows}");
Console.WriteLine($"  Valid rows: {result.ValidRows}");
Console.WriteLine($"  Invalid rows: {result.InvalidRows}");
Console.WriteLine($"  Processing time: {result.ProcessingTimeMs}ms");
Console.WriteLine($"  Completed at: {result.CompletedAt:O}");

if (result.Errors.Any())
{
    Console.WriteLine();
    Console.WriteLine($"Errors ({result.Errors.Count}):");
    foreach (var error in result.Errors.Take(5))
    {
        Console.WriteLine($"  Row {error.RowNumber}, {error.FieldName}: {error.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("✓ CSV ingestion pipeline test completed successfully!");
