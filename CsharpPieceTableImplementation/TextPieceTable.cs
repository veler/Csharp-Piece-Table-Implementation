namespace CsharpPieceTableImplementation
{
    /// <summary>
    /// Provides a representation of the current text document by keeping track of what pieces of
    /// the original and append buffer should be used to build the document.
    /// The text document may be up to 2GB in size (approximately 1 billion characters), which is
    /// the maximum limit of the C# String data type.
    /// </summary>
    public sealed class TextPieceTable
    {
        private readonly LinkedList<Piece> _pieces = new();

        public TextPieceTable(int originalDocumentLength)
        {
            Guard.IsGreaterThanOrEqualTo(originalDocumentLength, 0);

            if (originalDocumentLength > 0)
            {
                _pieces.AddLast(new Piece(isOriginal: true, 0, originalDocumentLength));
            }

            DocumentLength = originalDocumentLength;
        }

        /// <summary>
        /// Gets the length of the text document.
        /// </summary>
        public int DocumentLength { get; private set; }

        /// <summary>
        /// Inserts into the piece table.
        /// </summary>
        /// <param name="textDocumentPosition">The position in the text document where the piece will correspond to.</param>
        /// <param name="pieceTableBufferSpan">A span representing the part of the `append` buffer to insert at the given position in the text document.</param>
        public void Insert(int textDocumentPosition, Span pieceTableBufferSpan)
        {
            if (pieceTableBufferSpan.IsEmpty)
            {
                return;
            }

            Guard.IsGreaterThanOrEqualTo(textDocumentPosition, 0);

            var pieceToInsert
                = new Piece(
                    isOriginal: false,
                    pieceTableBufferSpan);

            if (textDocumentPosition == DocumentLength)
            {
                // Fast path. We're adding something at the end of the document. Simply add the piece at the very end of the table.
                _pieces.AddLast(pieceToInsert);
            }
            else if (textDocumentPosition == 0)
            {
                // Fast path. We're adding something at the very beginning of the document.
                _pieces.AddFirst(pieceToInsert);
            }
            else
            {
                // We're adding in the middle of the document.
                // Let's find what piece is at the insertion position.
                FindPieceFromTextDocumentPosition(
                    textDocumentPosition,
                    out LinkedListNode<Piece> existingPieceAtPositionInTextDocument,
                    out int pieceStartPositionInDocument);

                if (textDocumentPosition == pieceStartPositionInDocument)
                {
                    // If we are at the start boundary of the existing piece, we can simply insert the new piece at the beginning of it.
                    _pieces.AddBefore(existingPieceAtPositionInTextDocument, pieceToInsert);
                }
                else if (textDocumentPosition == pieceStartPositionInDocument + existingPieceAtPositionInTextDocument.Value.Span.Length)
                {
                    // If we are the end boundary, we can simply insert after the existing piece.
                    _pieces.AddAfter(existingPieceAtPositionInTextDocument, pieceToInsert);
                }
                else
                {
                    // The insertion position is in the middle of an existing piece. Therefore, we need to split this piece
                    // in 3:
                    //  1. A piece that represents the text that is before the inserted piece.
                    //  2. The insertion piece itself.
                    //  3. A piece that represents the text that is after the inserted piece.

                    // Find the position at which to split, relative to the start of the current piece.
                    int insertionPositionInExistingPiece = textDocumentPosition - pieceStartPositionInDocument;

                    // Calculate length of the piece before and after the insertion.
                    int beforeInsertionPieceLength = insertionPositionInExistingPiece;
                    int afterInsertionPieceLength = existingPieceAtPositionInTextDocument.Value.Span.Length - beforeInsertionPieceLength;

                    // Create the pieces.
                    var beforeInsertionPiece
                        = new Piece(
                            existingPieceAtPositionInTextDocument.Value.IsOriginal,
                            existingPieceAtPositionInTextDocument.Value.Span.Start,
                            beforeInsertionPieceLength);

                    var afterInsertionPiece
                        = new Piece(
                            existingPieceAtPositionInTextDocument.Value.IsOriginal,
                            insertionPositionInExistingPiece + existingPieceAtPositionInTextDocument.Value.Span.Start,
                            afterInsertionPieceLength);

                    // Insert we have three pieces: |piece before insertion| |insertion| |piece after insertion|
                    existingPieceAtPositionInTextDocument.Value = beforeInsertionPiece;
                    LinkedListNode<Piece> insertedPieceNode = _pieces.AddAfter(existingPieceAtPositionInTextDocument, pieceToInsert);
                    _pieces.AddAfter(insertedPieceNode, afterInsertionPiece);
                }
            }

            // Update the document lenght.
            DocumentLength += pieceToInsert.Span.Length;
        }

        /// <summary>
        /// Delete from the piece table.
        /// </summary>
        /// <param name="textDocumentSpan">A span representing a part of the text document to remove.</param>
        public void Delete(Span textDocumentSpan)
        {
            if (textDocumentSpan.IsEmpty)
            {
                return;
            }

            // Let's find all the pieces that overlap the given text document span.
            FindPiecesCoveringTextDocumentSpan(
                textDocumentSpan,
                out IReadOnlyList<LinkedListNode<Piece>> existingPiecesNodeCoveringSpanInTextDocument,
                out _,
                out int startPositionInTextDocumentOfFirstPiece);

            if (existingPiecesNodeCoveringSpanInTextDocument.Count == 1)
            {
                // The span to delete is within the boundaries of a single piece in the table. Let's use a faster path here.
                LinkedListNode<Piece> existingPieceAtStartPositionInTextDocument = existingPiecesNodeCoveringSpanInTextDocument[0];
                DeleteSpanFittingBoundariesOfASinglePiece(textDocumentSpan, existingPieceAtStartPositionInTextDocument, startPositionInTextDocumentOfFirstPiece);
            }
            else
            {
                // The span to delete overlaps many pieces in the table.
                DeleteSpanOverlapingManyPieces(textDocumentSpan, existingPiecesNodeCoveringSpanInTextDocument, startPositionInTextDocumentOfFirstPiece);
            }

            // Update the document lenght.
            DocumentLength -= textDocumentSpan.Length;
        }

        /// <summary>
        /// Finds the piece corresponding to the given position in the text document.
        /// </summary>
        /// <param name="textDocumentPosition">The position in the text document where the piece will correspond to.</param>
        /// <param name="piece">The piece that contains the <paramref name="textDocumentPosition"/>.</param>
        /// <param name="startPositionInTextDocumentOfPiece">The position in the text document where the <paramref name="piece"/> starts.</param>
        public void FindPieceFromTextDocumentPosition(int textDocumentPosition, out Piece piece, out int startPositionInTextDocumentOfPiece)
        {
            FindPieceFromTextDocumentPosition(textDocumentPosition, out LinkedListNode<Piece> pieceNode, out startPositionInTextDocumentOfPiece);
            piece = pieceNode.Value;
        }

        /// <summary>
        /// Finds all the pieces that cover the given span in the text document.
        /// </summary>
        /// <param name="textDocumentSpan">A span representing a part of the text document.</param>
        /// <param name="pieces">The list of pieces that overlap the <paramref name="textDocumentSpan"/>.</param>
        /// <param name="startPositionInTextDocumentOfFirstPiece">The position in the text document where the first <paramref name="pieces"/> starts.</param>
        public void FindPiecesCoveringTextDocumentSpan(Span textDocumentSpan, out IReadOnlyList<Piece> pieces, out int startPositionInTextDocumentOfFirstPiece)
        {
            FindPiecesCoveringTextDocumentSpan(textDocumentSpan, out _, out pieces, out startPositionInTextDocumentOfFirstPiece);
        }

        private void FindPieceFromTextDocumentPosition(int textDocumentPosition, out LinkedListNode<Piece> pieceNode, out int startPositionInTextDocumentOfPiece)
        {
            Guard.IsGreaterThanOrEqualTo(textDocumentPosition, 0);

            // If the text document position is in the second half of the document, we get better chance to find the piece
            // faster by search backward in the linked list.
            bool searchFromTheEnd = textDocumentPosition > DocumentLength / 2;

            if (searchFromTheEnd)
            {
                // Search backward.
                SearchBackwardPieceFromTextDocumentPosition(textDocumentPosition, out pieceNode, out startPositionInTextDocumentOfPiece);
            }
            else
            {
                // Search forward.
                SearchForwardPieceFromTextDocumentPosition(textDocumentPosition, out pieceNode, out startPositionInTextDocumentOfPiece);
            }
        }

        private void SearchForwardPieceFromTextDocumentPosition(int textDocumentPosition, out LinkedListNode<Piece> pieceNode, out int startPositionInTextDocumentOfPiece)
        {
            int pieceEndPositionInDocument = 0;
            int pieceStartPositionInDocument = 0;
            int i = 0;
            LinkedListNode<Piece>? node = _pieces.First;
            while (i < _pieces.Count && node is not null)
            {
                pieceEndPositionInDocument += node.Value.Span.Length;
                if (pieceEndPositionInDocument > textDocumentPosition)
                {
                    pieceNode = node;
                    startPositionInTextDocumentOfPiece = pieceStartPositionInDocument;
                    return;
                }

                pieceStartPositionInDocument = pieceEndPositionInDocument;

                node = node.Next;
                i++;
            }

            throw new IndexOutOfRangeException("The given position is greater than the text document length.");
        }

        private void SearchBackwardPieceFromTextDocumentPosition(int textDocumentPosition, out LinkedListNode<Piece> pieceNode, out int startPositionInTextDocumentOfPiece)
        {
            int pieceStartPositionInDocument = DocumentLength;
            int i = _pieces.Count;
            LinkedListNode<Piece>? node = _pieces.Last;
            while (i >= 0 && node is not null)
            {
                pieceStartPositionInDocument -= node.Value.Span.Length;
                if (pieceStartPositionInDocument <= textDocumentPosition)
                {
                    pieceNode = node;
                    startPositionInTextDocumentOfPiece = pieceStartPositionInDocument;
                    return;
                }

                node = node.Previous;
                i--;
            }

            throw new IndexOutOfRangeException("The given position is greater than the text document length.");
        }

        private void FindPiecesCoveringTextDocumentSpan(Span textDocumentSpan, out IReadOnlyList<LinkedListNode<Piece>> pieceNodes, out IReadOnlyList<Piece> pieces, out int startPositionInTextDocumentOfFirstPiece)
        {
            if (textDocumentSpan.IsEmpty)
            {
                pieceNodes = Array.Empty<LinkedListNode<Piece>>();
                pieces = Array.Empty<Piece>();
                startPositionInTextDocumentOfFirstPiece = 0;
                return;
            }

            // If the text document span's start position is in the second half of the document, we get better chance to find the piece
            // faster by search backward in the linked list.
            bool searchFromTheEnd = textDocumentSpan.Start > DocumentLength / 2;

            if (searchFromTheEnd)
            {
                // Search backward.
                SearchBackwardPiecesCoveringTextDocumentSpan(textDocumentSpan, out pieceNodes, out pieces, out startPositionInTextDocumentOfFirstPiece);
            }
            else
            {
                // Search forward.
                SearchForwardPiecesCoveringTextDocumentSpan(textDocumentSpan, out pieceNodes, out pieces, out startPositionInTextDocumentOfFirstPiece);
            }
        }

        private void SearchForwardPiecesCoveringTextDocumentSpan(Span textDocumentSpan, out IReadOnlyList<LinkedListNode<Piece>> pieceNodes, out IReadOnlyList<Piece> pieces, out int startPositionInTextDocumentOfFirstPiece)
        {
            startPositionInTextDocumentOfFirstPiece = -1;
            var resultedPieceNodes = new List<LinkedListNode<Piece>>();
            var resultsPieces = new List<Piece>();
            int characterCount = 0;
            int characterBeforeNextPieceCount = 0;

            int i = 0;
            LinkedListNode<Piece>? node = _pieces.First;
            while (i < _pieces.Count && node is not null)
            {
                characterCount += node.Value.Span.Length;
                if (characterCount > textDocumentSpan.Start)
                {
                    if (resultedPieceNodes.Count == 0)
                    {
                        startPositionInTextDocumentOfFirstPiece = characterBeforeNextPieceCount;
                    }

                    resultedPieceNodes.Add(node);
                    resultsPieces.Add(node.Value);

                    if (characterCount >= textDocumentSpan.End)
                    {
                        pieceNodes = resultedPieceNodes;
                        pieces = resultsPieces;
                        Guard.IsGreaterThan(startPositionInTextDocumentOfFirstPiece, -1);
                        return;
                    }
                }

                characterBeforeNextPieceCount = characterCount;

                node = node.Next;
                i++;
            }

            throw new IndexOutOfRangeException("The span end position is greated than the text document length.");
        }

        private void SearchBackwardPiecesCoveringTextDocumentSpan(Span textDocumentSpan, out IReadOnlyList<LinkedListNode<Piece>> pieceNodes, out IReadOnlyList<Piece> pieces, out int startPositionInTextDocumentOfFirstPiece)
        {
            startPositionInTextDocumentOfFirstPiece = -1;
            var resultedPieceNodes = new List<LinkedListNode<Piece>>();
            var resultsPieces = new List<Piece>();
            int pieceCountToRead = 0;
            int pieceStartPositionInDocument = DocumentLength;

            int i = _pieces.Count;
            LinkedListNode<Piece>? node = _pieces.Last;

            while (i >= 0 && node is not null)
            {
                pieceStartPositionInDocument -= node.Value.Span.Length;
                if (pieceStartPositionInDocument <= textDocumentSpan.End && resultedPieceNodes.Count == 0)
                {
                    pieceCountToRead++;
                }

                if (pieceStartPositionInDocument <= textDocumentSpan.Start)
                {
                    Guard.IsNotNull(node);
                    LinkedListNode<Piece>? currentPieceToAddToResult = node;
                    int j = 0;

                    while (j < pieceCountToRead && currentPieceToAddToResult is not null)
                    {
                        resultedPieceNodes.Add(currentPieceToAddToResult);
                        resultsPieces.Add(currentPieceToAddToResult.Value);
                        currentPieceToAddToResult = currentPieceToAddToResult.Next;
                        j++;
                    }

                    startPositionInTextDocumentOfFirstPiece = pieceStartPositionInDocument;
                    pieceNodes = resultedPieceNodes;
                    pieces = resultsPieces;
                    Guard.IsGreaterThan(startPositionInTextDocumentOfFirstPiece, -1);
                    return;
                }

                if (resultedPieceNodes.Count > 0)
                {
                    pieceCountToRead++;
                }

                node = node.Previous;
                i--;
            }

            throw new IndexOutOfRangeException("The span end position is greated than the text document length.");
        }

        private void DeleteSpanFittingBoundariesOfASinglePiece(Span textDocumentSpan, LinkedListNode<Piece> pieceNodeToCut, int startPositionInTextDocumentOfPieceToCut)
        {
            int pieceToTextDocumentSpanOffset = textDocumentSpan.Start - startPositionInTextDocumentOfPieceToCut;
            if (pieceToTextDocumentSpanOffset == 0)
            {
                // Simple case. We're deleting the beginning of the piece. Let's just resize the piece by trimming the
                // span at the beginning.
                int newLength = pieceNodeToCut.Value.Span.Length - textDocumentSpan.Length;
                if (newLength != 0)
                {
                    int newStartPosition = pieceNodeToCut.Value.Span.End - newLength;
                    pieceNodeToCut.Value
                        = new Piece(
                            pieceNodeToCut.Value.IsOriginal,
                            newStartPosition,
                            newLength);
                }
                else
                {
                    // In fact, it looks like textDocumentSpan's length covers the entire pieceNodeToCut.
                    // Therefore, we can just remove the piece since we don't want to keep a piece with an empty span (length == 0).
                    _pieces.Remove(pieceNodeToCut);
                }
            }
            else if (textDocumentSpan.End == startPositionInTextDocumentOfPieceToCut + pieceNodeToCut.Value.Span.Length)
            {
                // Simple case too. We're deleting the end of the piece. Let's just resize the piece by trimming the
                // span at the end.
                int newLength = pieceNodeToCut.Value.Span.Length - textDocumentSpan.Length;
                if (newLength != 0)
                {
                    pieceNodeToCut.Value
                    = new Piece(
                        pieceNodeToCut.Value.IsOriginal,
                        pieceNodeToCut.Value.Span.Start,
                        newLength);
                }
                else
                {
                    // In fact, the entire piece is being removed.
                    _pieces.Remove(pieceNodeToCut);
                }
            }
            else
            {
                // We're removing text somewhere in the middle of the piece.
                // Therefore, let's split the piece in 2:
                //  1. A piece that represents what's before the removed span.
                //  2. A piece that represents what's after the removed span.

                // Let's resizing the original piece to something smaller
                // that only represents what was before removed span.
                int resizeLength = pieceToTextDocumentSpanOffset;
                Piece backupExistingPiece = pieceNodeToCut.Value;
                pieceNodeToCut.Value
                    = new Piece(
                        pieceNodeToCut.Value.IsOriginal,
                        pieceNodeToCut.Value.Span.Start,
                        resizeLength);

                // Now, let's insert a new piece that represents what's after the removed span.
                int newLength = backupExistingPiece.Span.Length - textDocumentSpan.Length - pieceToTextDocumentSpanOffset;
                if (newLength != 0)
                {
                    int newStartPosition = backupExistingPiece.Span.End - newLength;
                    _pieces.AddAfter(
                        pieceNodeToCut,
                        new Piece(
                            backupExistingPiece.IsOriginal,
                            newStartPosition,
                            newLength));
                }
            }
        }

        private void DeleteSpanOverlapingManyPieces(Span textDocumentSpan, IReadOnlyList<LinkedListNode<Piece>> piecesToCut, int startPositionInTextDocumentOfFirstPiece)
        {
            Guard.HasSizeGreaterThan(piecesToCut, 1);

            LinkedListNode<Piece> firstPiece = piecesToCut[0];
            LinkedListNode<Piece> lastPiece = piecesToCut[piecesToCut.Count - 1];
            int startPositionInTextDocumentOfLastPiece = startPositionInTextDocumentOfFirstPiece + firstPiece.Value.Span.Length;

            if (piecesToCut.Count > 2)
            {
                // Fast path. If there are 3 or more pieces to remove, let's delete all the pieces between the first and last one
                // since we know we have to remove them entirely.
                for (int i = 1; i < piecesToCut.Count - 1; i++)
                {
                    LinkedListNode<Piece> piece = piecesToCut[i];
                    startPositionInTextDocumentOfLastPiece += piece.Value.Span.Length;
                    _pieces.Remove(piece);
                }
            }

            // The first and last piece may need to be splitted because the text document span may not fit perfectly the boundaries of these pieces.
            DeleteSpanFittingBoundariesOfASinglePiece(
                new Span(
                    textDocumentSpan.Start,
                    startPositionInTextDocumentOfFirstPiece + firstPiece.Value.Span.Length - textDocumentSpan.Start),
                firstPiece,
                startPositionInTextDocumentOfFirstPiece);

            DeleteSpanFittingBoundariesOfASinglePiece(
                new Span(
                    startPositionInTextDocumentOfLastPiece,
                    textDocumentSpan.End - startPositionInTextDocumentOfLastPiece),
                lastPiece,
                startPositionInTextDocumentOfLastPiece);
        }
    }
}
