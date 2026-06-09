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
# Run using the default optimized mode
dotnet run -c Release --project OrderBookTask

# Run explicitly in optimized mode
dotnet run -c Release --project OrderBookTask -- --mode optimized

# Full mode is reserved but currently not supported (will fail fast)
dotnet run -c Release --project OrderBookTask -- --mode full
```

### CLI Options

- `--mode <optimized|full>`: Specifies the run mode. Defaults to `optimized`. Choosing `full` will fail fast immediately as it is reserved for future updates.

Optional positional arguments (specified after named options) can override input, output, sample input, and sample result paths.

The program expects these files in the working directory or beside the application:

- `ticks.raw`
- `ticks_sample.csv`
- `ticks_result_sample.csv`

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

### Pipeline: read / build / write

1. Read and decode `ticks.raw` outside benchmark timing.
2. Validate decoded input against `ticks_sample.csv` (first 1,999 data rows).
3. Allocate result storage (either optimized arrays or `FullResultArrays`) outside benchmark timing.
4. Construct the corresponding processor (`OrderBookProcessor` or `FullOrderBookProcessor`) outside benchmark timing.
5. Run warmup and measured passes.
6. Validate results against `ticks_result_sample.csv` (first 1,999 data rows) using optimized or full validation rules.
7. Write `ticks_result.csv` outside benchmark timing.

## Measured vs unmeasured scope

Both optimized and full mode timings are measured inside `BenchmarkRunner`, capturing both `processor.Reset()` (which clears the book state without reallocating) and `processor.Process(...)`. 
All other work, such as disk reading, binary parsing, console output, CSV file writing, validation checks, and one-time allocations of result arrays and processor objects, is completely excluded.

## Run modes

- **Optimized mode (`--mode optimized`, Default)**: Computes only top-of-book prices `B0` and `A0`, and leaves aggregate quantity/count columns (`BQ0`, `BN0`, `AQ0`, `AN0`) empty in the CSV output.
- **Full mode (`--mode full`)**: Computes top-of-book prices `B0`/`A0` as well as aggregate quantity at best price (`BQ0`/`AQ0`) and active order counts at best price (`BN0`/`AN0`).

## Difference between optimized and full mode

- **Optimized Mode**: Optimized to minimize timing metrics. It tracks only order counts at each price to resolve the best price. It uses a lightweight `OrderState` structure (Side, Price) without tracking order quantities.
- **Full Mode**: Tracks both count and quantity aggregates per price level. It uses `FullOrderState` (Side, Price, Qty) and additional arrays (`bidQtyByPrice`, `askQtyByPrice`) to accumulate sizes. It is expected to be slower due to additional memory writes and tracking overhead.
- **Why they are kept separate**: To avoid any performance regressions in the optimized path, full mode is implemented as a completely separate `FullOrderBookProcessor` with its own `Process` loop, keeping the optimized processor free of branching checks or quantity states.

## Why optional aggregate columns are empty in optimized mode

In optimized mode, aggregate columns `BQ0`, `BN0`, `AQ0`, and `AN0` are left empty in the output CSV to save CPU cycles and allocations, conforming to the requirement to avoid aggregate calculation.

## Why arrays are used

Both processors use dense arrays indexed by price value to store order count and quantity aggregates. Since `maxPrice <= 2_000_000`, this provides O(1) updates and extremely fast sequential scans for repair, with no heap allocations or pointer-chasing during the hot path.

## Why OrderId ordering is not modeled

We do not maintain price-time queues or order priorities. Since B0/A0 top-of-book and aggregates only depend on counts and accumulated quantities per price, keeping counts and quantity arrays is sufficient.

## Price 0 handling

Price `0` is a valid price in the dataset. We use `-1` as the internal sentinel for missing best price. If the best price is `-1`, it is written as empty fields in the output CSV (e.g. `;;;` for missing B0/BQ0/BN0). If it is a real price of `0`, it is written as `0` along with its quantity and count.

## Clear strategy trade-off

When a clear action (`'Y'` or `'F'`) is encountered, both processors reset their dictionary and clear their price/quantity arrays. This is isolated in `ClearBook()`, which allows swapping it for a touched-price list or generation-based clears in the future if price arrays grow.

## Validation methodology

- **Optimized validation**: Compares original fields plus `B0`/`A0` only against `ticks_result_sample.csv` for the first 1,999 data rows.
- **Full validation**: Compares original fields plus all aggregate fields (`B0`, `BQ0`, `BN0`, `A0`, `AQ0`, `AN0`) against `ticks_result_sample.csv` for the first 1,999 data rows. Missing best side maps to `-1` for price, and aggregate columns are empty.

## Benchmark methodology

Warmup runs JIT-compile the code and expand dictionary capacity. The stopwatch starts before `processor.Reset()` and stops after `processor.Process()`. We report all measured runs and calculate us/tick and ns/tick using `CultureInfo.InvariantCulture`.

## Further optimizations

- **Custom Hash Map**: Replacing `Dictionary` with a flat, open-addressed hash map optimized for `long` keys to eliminate lookup overhead.
- **Touched-Price Clear**: Tracking modified price indices and clearing only those modified indices during resets.
- **Sparse Fallback**: Implementing a sparse map fallback structure only if future datasets exceed the maximum allowed price threshold.
