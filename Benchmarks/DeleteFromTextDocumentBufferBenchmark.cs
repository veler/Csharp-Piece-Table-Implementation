using BenchmarkDotNet.Attributes;
using CsharpPieceTableImplementation;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [InProcess]
    public class DeleteFromTextDocumentBufferBenchmark
    {
        private TextDocumentBuffer _textDocumentBuffer;

        [Params(1_000, 10_000, 100_000, 1_000_000)]
        public int InitialNumberOfPiecesInPieceTable;

        [IterationSetup]
        public void IterationSetup()
        {
            _textDocumentBuffer = new TextDocumentBuffer(Array.Empty<char>());
            for (int i = 0; i < InitialNumberOfPiecesInPieceTable; i++)
            {
                _textDocumentBuffer.Insert(0, "Hello");
            }
        }

        [Benchmark(Description = "Delete near the end of the document")]
        public void DeleteNearEndOfDocument()
        {
            _textDocumentBuffer.Delete(new Span(_textDocumentBuffer.DocumentLength - 3, 1));
        }

        [Benchmark(Description = "Delete near the beginning of the document")]
        public void DeleteNearBeginningOfDocument()
        {
            _textDocumentBuffer.Delete(new Span(3, 1));
        }

        [Benchmark(Description = "Delete in the middle of the document")]
        public void DeleteInMiddleOfDocument()
        {
            int middle = _textDocumentBuffer.DocumentLength / 2;
            _textDocumentBuffer.Delete(new Span(Math.Max(0, middle - 2), 4));
        }
    }
}
