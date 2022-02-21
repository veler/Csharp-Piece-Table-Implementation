namespace CsharpPieceTableImplementation
{
    /// <summary>
    /// Represents the metadata for a piece.
    /// </summary>
    public record Piece : IEquatable<Piece>
    {
        public Piece(bool isOriginal, int start, int length)
            : this (isOriginal, new Span(start, length))
        {
        }

        public Piece(bool isOriginal, Span span)
        {
            Guard.IsGreaterThan(span.Length, 0);
            IsOriginal = isOriginal;
            Span = span;
        }

        /// <summary>
        /// Gets whether this is the `original` buffer or `add` buffer.
        /// </summary>
        public bool IsOriginal { get; }

        /// <summary>
        /// Gets the span this piece is in the appropriate buffer.
        /// </summary>
        public Span Span { get; }

        /// <summary>
        /// Determines whether two pieces are the same.
        /// </summary>
        /// <param name="other">The piece to compare.</param>
        public virtual bool Equals(Piece? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other is not null && other.Span == Span && other.IsOriginal == IsOriginal;
        }

        /// <summary>
        /// Provides a hash function for the type.
        /// </summary>
        public override int GetHashCode()
        {
            return Span.GetHashCode() + IsOriginal.GetHashCode();
        }
    }
}
