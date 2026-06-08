# OrderBookTask

## Task summary

This is a compact .NET console solution scaffold for a performance-oriented order book reconstruction task. It reads `ticks.raw`, decodes binary mutation ticks, prepares top-of-book result arrays, and defines the orchestration for writing `ticks_result.csv`.

This scaffold intentionally does not implement the hot-path order book logic yet.

## How to build

```powershell
dotnet build -c Release
```

## How to run

```powershell
dotnet run -c Release --project OrderBookTask
```

The program expects these files in the working directory or beside the application:

- `ticks.raw`
- `ticks_sample.csv`
- `ticks_result_sample.csv`

Optional positional arguments can override input, output, sample input, and sample result paths.

## Project structure

- `Program.cs`: high-level read / benchmark / validate / write orchestration.
- `Tick.cs`: compact decoded tick contract.
- `OrderState.cs`: future active order state containing side and price only.
- `ReadResult.cs`: raw read stage result.
- `RawTickReader.cs`: big-endian `ticks.raw` decoder.
- `OrderBookProcessor.cs`: future optimized B0/A0 processor entry point.
- `BenchmarkRunner.cs`: warmup and measured run orchestration.
- `BenchmarkResult.cs`: measured timing summary.
- `CsvResultWriter.cs`: CSV output writer for optimized mode schema.
- `SampleValidator.cs`: validation entry points for sample files.
- `Constants.cs`: byte constants, record size, output header, sentinel values.

## Pipeline: read / build / write

1. Read and decode `ticks.raw` outside benchmark timing.
2. Validate decoded input against `ticks_sample.csv` once implemented.
3. Allocate `bestBidByTick` and `bestAskByTick` outside benchmark timing.
4. Create `OrderBookProcessor` outside benchmark timing.
5. Run warmup and measured passes.
6. Validate B0/A0 against `ticks_result_sample.csv` once implemented.
7. Write `ticks_result.csv` outside benchmark timing.

## Measured vs unmeasured scope

The official measured scope is `BenchmarkRunner`, which measures both `processor.Reset()` and `processor.Process(...)`. 
All other work, such as disk reading, binary parsing, console I/O, writing the CSV output, validation checks, and one-time allocations of arrays/processor objects, is unmeasured.

## B0/A0-only optimized mode

The implementation compiles and processes ticks in optimized B0/A0-only mode. It only tracks the top-of-book prices and does not compute order book depths or quantities at other levels.

## Why optional aggregate columns are empty

The aggregate columns `BQ0`, `BN0`, `AQ0`, and `AN0` are left empty in the output CSV. Since the task only requires B0 and A0, computing these aggregate values would add substantial performance overhead (tracking quantities and order queues), which is avoided to minimize measured run time.

## Why arrays are used

We use dense arrays (`_bidCountByPrice` and `_askCountByPrice`) indexed by price value to store the number of active orders at each price. Because the maximum price is guaranteed to be small (`<= 2,000_000`), these arrays provide $O(1)$ updates and extremely fast sequential scans for repair, with no heap allocations or pointer-chasing during the hot path.

## Why OrderId ordering is not modeled

We do not maintain price-time queues or order priorities for each price level. Since we only need to track whether a price level has *any* active orders (count > 0) to compute the top of the book, keeping a simple count of active orders per price is sufficient.

## Price 0 handling

Price `0` is a valid price in the dataset and is represented as `0`. We use `-1` as the internal sentinel for empty/no best price. If the best price is `-1`, it is written as an empty field in the output CSV, whereas a real price of `0` is written as `0`.

## Clear strategy trade-off

When a clear action (`'Y'` or `'F'`) is encountered, we call `ClearBook()`. It uses `Array.Clear` to reset the price arrays. This is the baseline clear strategy. It is isolated in `ClearBook()`, which allows swapping it for a touched-price index list or generation-based clears in the future if the price arrays become significantly larger.

## Validation methodology

We validate both the decoded binary ticks and the optimized B0/A0 results against `ticks_sample.csv` and `ticks_result_sample.csv` for the first 1,999 data rows (data rows 0 to 1998, excluding the CSV header). This ensures the parser and order book logic are completely correct before running the benchmark.

## Benchmark methodology

Warmup runs JIT-compile the code and expand the dictionary capacity. The official stopwatch wraps both `Reset()` (which clears the book state without reallocating) and `Process(...)`. We report all measured runs and calculate microseconds per tick and nanoseconds per tick with decimal precision.

## Further optimizations

- **Custom Hash Map**: Replacing the standard .NET `Dictionary<long, OrderState>` with a flat, open-addressed hash map optimized for `long` keys to eliminate lookup overhead.
- **Touched-Price Clear**: Tracking which price indices were modified and only clearing those indices during resets/clears, avoiding the cost of scanning/clearing the entire array.
- **Sparse Fallback**: Implementing a sparse map fallback structure only if future datasets exceed the maximum allowed price threshold.
