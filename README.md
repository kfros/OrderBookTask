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

## Measured vs unmeasured work

The measured scope is only `OrderBookProcessor.Process`.

Unmeasured work includes file reading, big-endian parsing, allocations, processor construction, CSV writing, sample validation, and console output.

## Optimized mode: B0/A0 only

The final optimized implementation should compute only mandatory best bid (`B0`) and best ask (`A0`) values.

## Why optional aggregates are empty

`BQ0`, `BN0`, `AQ0`, and `AN0` remain in the CSV schema for compatibility, but optimized mode leaves them empty. The future implementation must avoid optional aggregate computation.

## Price 0 handling

Price `0` is valid. The internal empty best-price sentinel is `-1`, and only `-1` is written as an empty CSV field. A real price of `0` must be written as `0`.

## Validation strategy

Decoded input validation should compare the first 2,000 rows against `ticks_sample.csv` using `SourceTime`, `Side`, `Action`, `OrderId`, `Price`, and `Qty`.

Optimized result validation should compare original fields plus `B0` and `A0` against `ticks_result_sample.csv`. It must ignore `BQ0`, `BN0`, `AQ0`, and `AN0`.

## Benchmark strategy

The benchmark runner calls `Reset` before every warmup and measured pass. The stopwatch starts immediately before `Process` and stops immediately after it returns. It prints all measured timings plus best `ms`, `us/tick`, and `ns/tick`.

Do not call `GC.Collect` inside measured timing, and do not rely on the first input tick being `F` or `Y`.

## Implementation TODOs for the next agent

- Implement `OrderBookProcessor.Reset`.
- Implement `OrderBookProcessor.Process` using price-level count arrays and an OrderId index.
- Implement decoded input validation in `SampleValidator`.
- Implement optimized B0/A0 sample validation in `SampleValidator`.
- Enable validation calls in `Program.cs`.
- Keep optional aggregate columns empty in optimized mode.

## Further optimization candidates

- Use dense price-level count arrays while `MaxPrice <= Constants.MaxAllowedPrice`.
- Use a compact OrderId index appropriate for the observed id range.
- Preserve sequential single-threaded processing.
- Consider a documented sparse fallback only if future datasets exceed the configured price threshold.
