// FROM: https://github.com/microsoft/vs-editor-api, under MIT license.

namespace CsharpPieceTableImplementation
{
    /// <summary>
    /// An immutable integer interval that describes a range of values from <see cref="Start"/> to <see cref="End"/> that is closed on 
    /// the left and open on the right: [Start .. End). A zpan is usually applied to an <see cref="ITextSnapshot"/> to denote a span of text,
    /// but it is independent of any particular text buffer or snapshot. 
    /// </summary>
    public readonly struct Span
    {
        private readonly int _start;
        private readonly int _length;

        /// <summary>
        /// Initializes a new instance of a <see cref="Span"/> with the given start point and length.
        /// </summary>
        /// <param name="start">
        /// The starting point of the span.
        /// </param>
        /// <param name="length">
        /// The length of the span.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="length"/> is less than zero, or
        /// start + length is greater than the length of the text snapshot.</exception>
        public Span(int start, int length)
        {
            Guard.IsGreaterThanOrEqualTo(start, 0);
            Guard.IsGreaterThanOrEqualTo(start + length, start);
            _start = start;
            _length = length;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="Span"/> with the given start and end positions.
        /// </summary>
        /// <param name="start">The start position of the new span.</param>
        /// <param name="end">The end position of the new Span.</param>
        /// <returns>The new span.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is less than zero, or
        /// <paramref name="end"/> is less than <paramref name="start"/>.</exception>
        public static Span FromBounds(int start, int end)
        {
            // We don't need to check arguments, as the Span constructor will check for us.
            return new Span(start, end - start);
        }

        /// <summary>
        /// The starting index of the span.
        /// </summary>
        public int Start => _start;

        /// <summary>
        /// The end of the span.  The span is open-ended on the right side, which is to say
        /// that Start + Length = End.
        /// </summary>
        public int End => _start + _length;

        /// <summary>
        /// The length of the span, which is always non-negative.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Determines whether or not this span is empty.
        /// </summary>
        /// <value><c>true</c> if the length of the span is zero, otherwise <c>false</c>.</value>
        public bool IsEmpty => _length == 0;

        /// <summary>
        /// Determines whether the position lies within the span.
        /// </summary>
        /// <param name="position">
        /// The position to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the position is greater than or equal to Start and strictly less 
        /// than End, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(int position)
        {
            return position >= _start && position < End;
        }

        /// <summary>
        /// Determines whether <paramref name="span"/> falls completely within this span.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified span falls completely within this span, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(Span span)
        {
            return span.Start >= _start && span.End <= End;
        }

        /// <summary>
        /// Determines whether <paramref name="span"/> overlaps this span. Two spans are considered to overlap 
        /// if they have positions in common and neither is empty. Empty spans do not overlap with any 
        /// other span.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans overlap, otherwise <c>false</c>.
        /// </returns>
        public bool OverlapsWith(Span span)
        {
            int overlapStart = Math.Max(_start, span.Start);
            int overlapEnd = Math.Min(End, span.End);

            return overlapStart < overlapEnd;
        }

        /// <summary>
        /// Returns the overlap with the given span, or null if there is no overlap.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// The overlap of the spans, or null if the overlap is empty.
        /// </returns>
        public Span? Overlap(Span span)
        {
            int overlapStart = Math.Max(_start, span.Start);
            int overlapEnd = Math.Min(End, span.End);

            if (overlapStart < overlapEnd)
            {
                return FromBounds(overlapStart, overlapEnd);
            }

            return null;
        }

        /// <summary>
        /// Determines whether <paramref name="span"/> intersects this span. Two spans are considered to 
        /// intersect if they have positions in common or the end of one span 
        /// coincides with the start of the other span.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans intersect, otherwise <c>false</c>.
        /// </returns>
        public bool IntersectsWith(Span span)
        {
            return span.Start <= End && span.End >= _start;
        }

        /// <summary>
        /// Returns the intersection with the given span, or null if there is no intersection.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// The intersection of the spans, or null if the intersection is empty.
        /// </returns>
        public Span? Intersection(Span span)
        {
            int intersectStart = Math.Max(_start, span.Start);
            int intersectEnd = Math.Min(End, span.End);

            if (intersectStart <= intersectEnd)
            {
                return FromBounds(intersectStart, intersectEnd);
            }

            return null;
        }

        /// <summary>
        /// Provides a string representation of the span.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "[{0}..{1})",
                _start,
                _start + _length);
        }

        /// <summary>
        /// Provides a hash function for the type.
        /// </summary>
        public override int GetHashCode()
        {
            return Length.GetHashCode() ^ Start.GetHashCode();
        }

        /// <summary>
        /// Determines whether two spans are the same.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object? obj)
        {
            if (obj is Span other)
            {
                return other.Start == _start && other.Length == _length;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether two spans are the same
        /// </summary>
        public static bool operator ==(Span left, Span right)
        {
            return left.Start == right.Start && left.Length == right.Length;
        }

        /// <summary>
        /// Determines whether two spans are different.
        /// </summary>
        public static bool operator !=(Span left, Span right)
        {
            return !(left == right);
        }
    }
}
