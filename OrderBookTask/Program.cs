using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OrderBookTask;

// Parse named options (--mode <optimized|full>) first, then map remaining args to file paths
var mode = "optimized";
var positionalArgsList = new List<string>();

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--mode")
    {
        if (i + 1 < args.Length)
        {
            mode = args[i + 1];
            i++;
        }
        else
        {
            throw new ArgumentException("Missing value for --mode option.");
        }
    }
    else
    {
        positionalArgsList.Add(args[i]);
    }
}

var positionalArgs = positionalArgsList.ToArray();

if (mode != "optimized" && mode != "full")
{
    throw new ArgumentException($"Invalid mode '{mode}'. Supported modes are: optimized, full.");
}

Console.WriteLine($"Mode: {mode}");

const string inputFileName = "ticks.raw";
const string outputFileName = "ticks_result.csv";
const string sampleInputFileName = "ticks_sample.csv";
const string sampleResultFileName = "ticks_result_sample.csv";
const int sampleRows = 1_999;
const int warmupRuns = 1;
const int measuredRuns = 5;

var baseDirectory = AppContext.BaseDirectory;
var inputPath = ResolvePath(positionalArgs, 0, inputFileName, baseDirectory);
var outputPath = ResolvePath(positionalArgs, 1, outputFileName, baseDirectory);
var sampleInputPath = ResolvePath(positionalArgs, 2, sampleInputFileName, baseDirectory);
var sampleResultPath = ResolvePath(positionalArgs, 3, sampleResultFileName, baseDirectory);

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

var validator = new SampleValidator();
if (File.Exists(sampleInputPath))
{
    validator.ValidateDecodedInput(sampleInputPath, readResult.Ticks, sampleRows);
    Console.WriteLine("Decoded input validation: PASSED");
}
else
{
    Console.WriteLine("Decoded input validation: SKIPPED");
}

Console.WriteLine("Benchmark scope: official build stage = Reset + Process");
Console.WriteLine("Excluded from official timing: read, parse, validation, CSV write, allocation, console output");

var writer = new CsvResultWriter();
var benchmarkRunner = new BenchmarkRunner();

if (mode == "optimized")
{
    var bestBidByTick = new int[readResult.TickCount];
    var bestAskByTick = new int[readResult.TickCount];
    Array.Fill(bestBidByTick, Constants.EmptyPriceSentinel);
    Array.Fill(bestAskByTick, Constants.EmptyPriceSentinel);

    var processor = new OrderBookProcessor(readResult.MaxPrice, readResult.TickCount);
    var benchmark = benchmarkRunner.Run(
        processor,
        readResult.Ticks,
        bestBidByTick,
        bestAskByTick,
        warmupRuns,
        measuredRuns);

    PrintBenchmark(benchmark, warmupRuns, measuredRuns);

    if (File.Exists(sampleResultPath))
    {
        validator.ValidateOptimizedResult(sampleResultPath, readResult.Ticks, bestBidByTick, bestAskByTick, sampleRows);
        Console.WriteLine("Optimized result validation: PASSED");
    }
    else
    {
        Console.WriteLine("Optimized result validation: SKIPPED");
    }

    writer.Write(outputPath, readResult.Ticks, bestBidByTick, bestAskByTick);
}
else // mode == "full"
{
    var results = new FullResultArrays(readResult.TickCount);
    var processor = new FullOrderBookProcessor(readResult.MaxPrice, readResult.TickCount);
    var benchmark = benchmarkRunner.Run(
        processor,
        readResult.Ticks,
        results,
        warmupRuns,
        measuredRuns);

    PrintBenchmark(benchmark, warmupRuns, measuredRuns);

    if (File.Exists(sampleResultPath))
    {
        validator.ValidateFullResult(sampleResultPath, readResult.Ticks, results, sampleRows);
        Console.WriteLine("Full result validation: PASSED");
    }
    else
    {
        Console.WriteLine("Full result validation: SKIPPED");
    }

    writer.WriteFull(outputPath, readResult.Ticks, results);
}

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

static void PrintBenchmark(BenchmarkResult benchmark, int warmup, int measured)
{
    Console.WriteLine($"Warmup runs: {warmup}");
    Console.WriteLine($"Measured runs: {measured}");

    for (var i = 0; i < benchmark.MeasuredRuns.Length; i++)
    {
        Console.WriteLine($"Run {i + 1}: {benchmark.MeasuredRuns[i].TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} ms");
    }

    Console.WriteLine($"Best: {benchmark.BestElapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} ms");
    Console.WriteLine($"Best: {benchmark.BestMicroseconds.ToString("F3", CultureInfo.InvariantCulture)} us");
    Console.WriteLine($"Best: {benchmark.BestMicrosecondsPerTick.ToString("F6", CultureInfo.InvariantCulture)} us/tick");
    Console.WriteLine($"Best: {benchmark.BestNanosecondsPerTick.ToString("F3", CultureInfo.InvariantCulture)} ns/tick");
}
