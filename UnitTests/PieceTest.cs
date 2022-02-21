using CsharpPieceTableImplementation;
using Xunit;

namespace UnitTests
{
    public class PieceTest
    {
        [Fact]
        public void Piece_CanCheckEquality()
        {
            var piece 
                = new Piece(
                    isOriginal: true, 0, 5);

            var same
                = new Piece(
                    isOriginal: true, 0, 5);

            var diffType
                = new Piece(
                    isOriginal: false, 0, 5);

            var diffOffset
                = new Piece(
                    isOriginal: true, 5, 5);

            var diffLength
                = new Piece(
                    isOriginal: true, 0, 10);

            Assert.True(piece.Equals(same));
            Assert.False(piece.Equals(diffType));
            Assert.False(piece.Equals(diffOffset));
            Assert.False(piece.Equals(diffLength));
        }
    }
}
