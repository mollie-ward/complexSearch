using BenchmarkDotNet.Running;

// Run performance benchmarks
var summary = BenchmarkRunner.Run<VehicleSearch.Performance.Tests.SearchPerformanceBenchmarks>();
