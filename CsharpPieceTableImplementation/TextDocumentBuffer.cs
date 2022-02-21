using System.Text;

namespace CsharpPieceTableImplementation
{
    /// <summary>
    /// Represents a buffer that contains the original text of the document and all the changes made by the user.
    /// This is an public implementation of the PieceTable data structure.
    /// </summary>
    public sealed class TextDocumentBuffer
    {
        private readonly ReadOnlyMemory<char> _originalDocumentBuffer;
        private readonly List<char> _appendBuffer = new(); // max size is 2GB (approximately 1 billion characters)
        private readonly TextPieceTable _pieceTable;
        private readonly StringSpanPool _stringSpanPool = new();

        public TextDocumentBuffer(char[] originalDocument)
        {
            Guard.IsNotNull(originalDocument, nameof(originalDocument));

            _originalDocumentBuffer = originalDocument;
            _pieceTable = new TextPieceTable(originalDocument.Length);
        }

        /// <summary>
        /// Gets the character at the given text document position.
        /// </summary>
        public char this[int textDocumentPosition]
        {
            get
            {
                _pieceTable.FindPieceFromTextDocumentPosition(textDocumentPosition, out Piece piece, out int pieceStartPositionInDocument);

                int bufferPosition = piece.Span.Start + (textDocumentPosition - pieceStartPositionInDocument);

                if (piece.IsOriginal)
                {
                    return _originalDocumentBuffer.Span[bufferPosition];
                }
                else
                {
                    return _appendBuffer[bufferPosition];
                }
            }
        }

        /// <summary>
        /// Gets the current length of the document.
        /// </summary>
        public int DocumentLength => _pieceTable.DocumentLength;

        /// <summary>
        /// Get the text that corresponds to the given <paramref name="spanInTextDocument"/>.
        /// </summary>
        /// <remarks>
        /// This method can allocate a lot of memory because it rebuilds a string from the piece table. Use it caution.
        /// </remarks>
        public string GetText(Span spanInTextDocument)
        {
            if (spanInTextDocument.IsEmpty)
            {
                return string.Empty;
            }

            // Check whether this span has already been asked in the past. If yes, we already
            // have a string for it, no need to instantiate a new one.
            string? result = _stringSpanPool.GetStringFromCache(spanInTextDocument);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            // Let's find all the pieces that overlap the given text document span.
            _pieceTable.FindPiecesCoveringTextDocumentSpan(spanInTextDocument, out IReadOnlyList<Piece> pieces, out int pieceStartPositionInDocument);

            var builder = new StringBuilder();

            for (int i = 0; i < pieces.Count; i++)
            {
                Piece piece = pieces[i];

                // By default, we retrieve the full piece's span from the buffer.
                int bufferPositionStart = piece.Span.Start;
                int bufferLength = piece.Span.Length;

                // But if we're on the first or last piece, it's possible that the piece start and end may not corresponds
                // to spanInTextDocument's boundaries. We need to adjust the start and length of what to grab from the piece.
                if (i == 0)
                {
                    bufferPositionStart = piece.Span.Start + (spanInTextDocument.Start - pieceStartPositionInDocument);
                    bufferLength = piece.Span.Start + piece.Span.Length - bufferPositionStart;
                }

                pieceStartPositionInDocument += piece.Span.Length;

                if (i == pieces.Count - 1 && pieceStartPositionInDocument > spanInTextDocument.End)
                {
                    int bufferPositionEnd = piece.Span.End - (pieceStartPositionInDocument - spanInTextDocument.End);
                    bufferLength = bufferPositionEnd - bufferPositionStart;
                }

                // Pick up the characters from the right buffer.
                if (piece.IsOriginal)
                {
                    builder.Append(_originalDocumentBuffer.Span.Slice(bufferPositionStart, bufferLength));
                }
                else
                {
                    for (int j = bufferPositionStart; j < bufferPositionStart + bufferLength; j++)
                    {
                        builder.Append(_appendBuffer[j]);
                    }
                }
            }

            // Generate the final string.
            result = builder.ToString()!;

            // Cache it, so we don't have to instantiate it again if we ask multiple time the same span.
            _stringSpanPool.Cache(spanInTextDocument, result);

            return result;
        }

        /// <summary>
        /// Inserts a character at a given position in the text document.
        /// </summary>
        /// <remarks>
        /// The character will be added to the append buffer.
        /// </remarks>
        public void Insert(int textDocumentPosition, char @char)
        {
            // TODO: Potential optimization:
            //       It's likely possible that inserted characters to a text document are very redundant. For example,
            //       a descriptive text in English likely use alphnumeric characters (a-zA-Z0-9). Therefore, to economize
            //       some memory, we could lookup in the _appendBuffer whether the @char already exist, and if yes,
            //       simply pass its location as a Span and not add the character to the buffer.
            //
            //       This optimization is potentially possible in the method using string instead of char too, but is likely
            //       more expensive for less improvement.
            //
            //       A (potential) drawback of this optimization is that several spans may have the same start and length, which
            //       could potentially open a door to maintainability madness and bugs.
            //
            //       Overall, this optimization would be benifical when the user types a character after another in the document.

            if (textDocumentPosition != DocumentLength)
            {
                // Reset the cache of string, since the insert will compromized it.
                _stringSpanPool.Reset();
            }

            _pieceTable.Insert(textDocumentPosition, new Span(_appendBuffer.Count, 1));
            _appendBuffer.Add(@char);
        }

        /// <summary>
        /// Inserts a string at a given position in the text document.
        /// </summary>
        /// <remarks>
        /// The text will be added to the append buffer.
        /// </remarks>
        public void Insert(int textDocumentPosition, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (textDocumentPosition != DocumentLength)
            {
                // Reset the cache of string, since the insert will compromized it.
                _stringSpanPool.Reset();
            }

            _pieceTable.Insert(textDocumentPosition, new Span(_appendBuffer.Count, text.Length));
            _appendBuffer.AddRange(text);
        }

        /// <summary>
        /// Deletes the given span from the text document.
        /// </summary>
        /// <remarks>
        /// This does not remove the text from the buffer. With that in mind, some scenarios like Undo/Redo can be designed
        /// on top of the piece table.
        /// </remarks>
        public void Delete(Span textDocumentSpan)
        {
            _pieceTable.Delete(textDocumentSpan);

            // Reset the cache of string, since the delete will compromized it.
            _stringSpanPool.Reset();
        }
    }
}
