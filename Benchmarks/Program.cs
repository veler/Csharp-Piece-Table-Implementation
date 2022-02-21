using BenchmarkDotNet.Running;
using Benchmarks;

_ = BenchmarkRunner.Run<InsertInTextDocumentBufferBenchmark>();
_ = BenchmarkRunner.Run<DeleteFromTextDocumentBufferBenchmark>();
_ = BenchmarkRunner.Run<GetTextDocumentBufferBenchmark>();