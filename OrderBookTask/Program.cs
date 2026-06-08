using OrderBookTask;

const string inputFileName = "ticks.raw";
const string outputFileName = "ticks_result.csv";
const string sampleInputFileName = "ticks_sample.csv";
const string sampleResultFileName = "ticks_result_sample.csv";
const int sampleRows = 2_000;
const int warmupRuns = 1;
const int measuredRuns = 3;

var baseDirectory = AppContext.BaseDirectory;
var inputPath = ResolvePath(args, 0, inputFileName, baseDirectory);
var outputPath = ResolvePath(args, 1, outputFileName, baseDirectory);
var sampleInputPath = ResolvePath(args, 2, sampleInputFileName, baseDirectory);
var sampleResultPath = ResolvePath(args, 3, sampleResultFileName, baseDirectory);

Console.WriteLine($"Reading {inputPath}");
var reader = new RawTickReader();
var readResult = reader.Read(inputPath);

Console.WriteLine($"Read {readResult.TickCount} ticks. Max price: {readResult.MaxPrice}");
if (readResult.MaxPrice > Constants.MaxAllowedPrice)
{
    throw new InvalidOperationException(
        $"Max price {readResult.MaxPrice} exceeds configured threshold {Constants.MaxAllowedPrice}. " +
        "Sparse fallback is intentionally not implemented in this scaffold.");
}

var bestBidByTick = new int[readResult.TickCount];
var bestAskByTick = new int[readResult.TickCount];
Array.Fill(bestBidByTick, Constants.EmptyPriceSentinel);
Array.Fill(bestAskByTick, Constants.EmptyPriceSentinel);

var validator = new SampleValidator();
if (File.Exists(sampleInputPath))
{
    Console.WriteLine($"Decoded input validation available for the first {sampleRows} rows: {sampleInputPath}");
    // TODO: Enable after SampleValidator is implemented.
    // validator.ValidateDecodedInput(sampleInputPath, readResult.Ticks, sampleRows);
}

var processor = new OrderBookProcessor(readResult.MaxPrice, readResult.TickCount);
var benchmarkRunner = new BenchmarkRunner();
var benchmark = benchmarkRunner.Run(
    processor,
    readResult.Ticks,
    bestBidByTick,
    bestAskByTick,
    warmupRuns,
    measuredRuns);

if (File.Exists(sampleResultPath))
{
    Console.WriteLine($"Optimized result validation available: {sampleResultPath}");
    // TODO: Enable after OrderBookProcessor.Process and SampleValidator are implemented.
    // validator.ValidateOptimizedResult(sampleResultPath, readResult.Ticks, bestBidByTick, bestAskByTick, sampleRows);
}

var writer = new CsvResultWriter();
writer.Write(outputPath, readResult.Ticks, bestBidByTick, bestAskByTick);

PrintBenchmark(benchmark);
Console.WriteLine($"Wrote {outputPath}");

static string ResolvePath(string[] args, int index, string fileName, string baseDirectory)
{
    if (args.Length > index)
    {
        return Path.GetFullPath(args[index]);
    }

    var currentDirectoryPath = Path.GetFullPath(fileName);
    if (File.Exists(currentDirectoryPath))
    {
        return currentDirectoryPath;
    }

    return Path.Combine(baseDirectory, fileName);
}

static void PrintBenchmark(BenchmarkResult benchmark)
{
    for (var i = 0; i < benchmark.MeasuredRuns.Length; i++)
    {
        Console.WriteLine($"Run {i + 1}: {benchmark.MeasuredRuns[i].TotalMilliseconds:F3} ms");
    }

    Console.WriteLine($"Best: {benchmark.BestElapsed.TotalMilliseconds:F3} ms");
    Console.WriteLine($"Best: {benchmark.BestMicrosecondsPerTick:F6} us/tick");
    Console.WriteLine($"Best: {benchmark.BestNanosecondsPerTick:F3} ns/tick");
}
