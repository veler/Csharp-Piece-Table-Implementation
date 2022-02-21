using BenchmarkDotNet.Attributes;
using CsharpPieceTableImplementation;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [InProcess]
    public class GetTextDocumentBufferBenchmark
    {
        private TextDocumentBuffer _textDocumentBuffer;

        [Params(1_000, 10_000, 100_000, 1_000_000)]
        public int InitialCharacterCount;

        [IterationSetup]
        public void IterationSetup()
        {
            _textDocumentBuffer = new TextDocumentBuffer(Array.Empty<char>());
            for (int i = 0; i < InitialCharacterCount; i++)
            {
                _textDocumentBuffer.Insert(0, "Hello");
            }
        }

        [Benchmark(Description = "Get some text near the end of the document")]
        public void GetTextAtNearOfDocument()
        {
            _ = _textDocumentBuffer.GetText(new Span(_textDocumentBuffer.DocumentLength - 20, 9));
        }

        [Benchmark(Description = "Get some text near the beginning of the document")]
        public void GetTextNearBeginningOfDocument()
        {
            _ = _textDocumentBuffer.GetText(new Span(20, 9));
        }

        [Benchmark(Description = "Get some text in the middle of the document")]
        public void GetTextInMiddleOfDocument()
        {
            int middle = _textDocumentBuffer.DocumentLength / 2;
            _ = _textDocumentBuffer.GetText(new Span(middle - 3, 3));
        }

        [Benchmark(Description = "Get a character near the end of the document")]
        public void GetCharacterAtEndOfDocument()
        {
            _ = _textDocumentBuffer[_textDocumentBuffer.DocumentLength - 20];
        }

        [Benchmark(Description = "Get a character near the beginning of the document")]
        public void GetCharacterAtBeginningOfDocument()
        {
            _ = _textDocumentBuffer[20];
        }

        [Benchmark(Description = "Get a character in the middle of the document")]
        public void GetCharacterInMiddleOfDocument()
        {
            int middle = _textDocumentBuffer.DocumentLength / 2;
            _ = _textDocumentBuffer[middle];
        }

        [Benchmark(Description = "Get all the text in the document")]
        public void GetFullTextDocument()
        {
            _ = _textDocumentBuffer.GetText(new Span(0, _textDocumentBuffer.DocumentLength));
        }
    }
}
