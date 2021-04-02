﻿using SharpFish.Model;
using System;
using Xunit;

namespace SharpFish.Test
{
    public class BitBoardTest
    {
        [Fact]
        public void GetBit()
        {
            foreach (var square in Enum.GetValues<BoardSquares>())
            {
                var bitBoard = new BitBoard(1UL << (int)square);

                Assert.True(bitBoard.GetBit(square));
            }
        }

        [Fact]
        public void SetBit()
        {
            foreach (var square in Enum.GetValues<BoardSquares>())
            {
                var bitBoard = new BitBoard();

                bitBoard.SetBit(square);
                Assert.True(bitBoard.GetBit(square));

                // Making sure that setting it again doesn't flipt it
                bitBoard.SetBit(square);
                Assert.True(bitBoard.GetBit(square));
            }
        }

        [Fact]
        public void PopBit()
        {
            foreach (var square in Enum.GetValues<BoardSquares>())
            {
                var bitBoard = new BitBoard();
                bitBoard.SetBit(square);

                Assert.True(bitBoard.GetBit(square));

                bitBoard.PopBit(square);
                Assert.False(bitBoard.GetBit(square));

                // Making sure that popping it again doesn't flipt it
                bitBoard.PopBit(square);
                Assert.False(bitBoard.GetBit(square));
            }
        }

        [Fact]
        public void Empty()
        {
            var bitBoard = new BitBoard();
            Assert.True(bitBoard.Empty);

            bitBoard.SetBit(BoardSquares.e4);
            Assert.False(bitBoard.Empty);
        }

        [Fact]
        public void IsSinglePopulated()
        {
            var bitBoard = new BitBoard();
            Assert.False(bitBoard.IsSinglePopulated());

            bitBoard.SetBit(BoardSquares.e4);
            Assert.True(bitBoard.IsSinglePopulated());

            bitBoard.SetBit(BoardSquares.e5);
            Assert.False(bitBoard.IsSinglePopulated());
        }

        [Fact]
        public void CountBits()
        {
            var bitBoard = new BitBoard();
            Assert.Equal(0, bitBoard.CountBits());

            bitBoard.SetBit(BoardSquares.e4);
            Assert.Equal(1, bitBoard.CountBits());

            bitBoard.SetBit(BoardSquares.e4);
            Assert.Equal(1, bitBoard.CountBits());

            bitBoard.SetBit(BoardSquares.d4);
            Assert.Equal(2, bitBoard.CountBits());

            bitBoard.PopBit(BoardSquares.d4);
            Assert.Equal(1, bitBoard.CountBits());
        }

        [Fact]
        public void ResetLS1B()
        {
            // Arrange
            BitBoard bitBoard = new(new[] { BoardSquares.d5, BoardSquares.e4 });

            // Act
            bitBoard.ResetLS1B();

            // Assert
            Assert.True(bitBoard.GetBit(BoardSquares.e4));
            Assert.False(bitBoard.GetBit(BoardSquares.d5));
        }

        [Fact]
        public void ResetLS1B_ulong()
        {
            // Arrange
            BitBoard bitBoard = new(new[] { BoardSquares.d5, BoardSquares.e4 });

            // Act
            var result = new BitBoard(BitBoard.ResetLS1B(bitBoard.Board));

            // Assert
            Assert.True(bitBoard.GetBit(BoardSquares.e4));
            Assert.True(bitBoard.GetBit(BoardSquares.d5));

            Assert.True(result.GetBit(BoardSquares.e4));
            Assert.False(result.GetBit(BoardSquares.d5));
        }

        [Theory]
        [InlineData(new BoardSquares[] { }, -1)]
        [InlineData(new BoardSquares[] { BoardSquares.e4 }, (int)BoardSquares.e4)]
        [InlineData(new BoardSquares[] { BoardSquares.a8 }, (int)BoardSquares.a8)]
        [InlineData(new BoardSquares[] { BoardSquares.h1 }, (int)BoardSquares.h1)]
        [InlineData(new BoardSquares[] { BoardSquares.a8, BoardSquares.h1 }, (int)BoardSquares.a8)]
        [InlineData(new BoardSquares[] { BoardSquares.d5, BoardSquares.e4 }, (int)BoardSquares.d5)]
        [InlineData(new BoardSquares[] { BoardSquares.e4, BoardSquares.f4 }, (int)BoardSquares.e4)]
        public void GetLS1BIndex(BoardSquares[] occupiedSquares, int expectedLS1B)
        {
            Assert.Equal(expectedLS1B, new BitBoard(occupiedSquares).GetLS1BIndex());

            if (expectedLS1B != -1)
            {
                Assert.Equal(((BoardSquares)expectedLS1B).ToString(), Constants.Coordinates[expectedLS1B]);
            }
        }
    }
}
