using BenchmarkDotNet.Attributes;
using CsharpPieceTableImplementation;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [InProcess]
    public class InsertInTextDocumentBufferBenchmark
    {
        private TextDocumentBuffer _textDocumentBuffer;

        [Params(1_000, 10_000, 100_000, 1_000_000)]
        public int InitialNumberOfPiecesInPieceTable;

        [IterationSetup]
        public void IterationSetup()
        {
            _textDocumentBuffer
                = new TextDocumentBuffer(Array.Empty<char>());

            for (int i = 0; i < InitialNumberOfPiecesInPieceTable; i++)
            {
                _textDocumentBuffer.Insert(_textDocumentBuffer.DocumentLength, "Hello");
            }
        }

        [Benchmark(Description = "Insertion near the end of the document")]
        public void InsertNearEndOfDocument()
        {
            _textDocumentBuffer.Insert(_textDocumentBuffer.DocumentLength - 3, 'A');
        }

        [Benchmark(Description = "Insertion near the beginning of the document")]
        public void InsertNearBeginningOfDocument()
        {
            _textDocumentBuffer.Insert(3, 'A');
        }

        [Benchmark(Description = "Insertion in the middle of the document")]
        public void InsertInMiddleOfDocument()
        {
            int middle = _textDocumentBuffer.DocumentLength / 2;
            _textDocumentBuffer.Insert(middle, 'A');
        }
    }
}
