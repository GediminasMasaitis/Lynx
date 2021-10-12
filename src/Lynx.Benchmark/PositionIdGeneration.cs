﻿/*
 * 
 *  | Method |         newPosition |       Mean |   Error |  StdDev | Ratio |  Gen 0 | Allocated |
 *  |------- |-------------------- |-----------:|--------:|--------:|------:|-------:|----------:|
 *  |    FEN | Lynx.Model.Position | 1,149.0 ns | 2.03 ns | 1.90 ns |  1.00 | 0.0210 |     408 B |
 *  |    FEN | Lynx.Model.Position | 1,205.7 ns | 2.52 ns | 2.23 ns |  1.05 | 0.0229 |     432 B |
 *  |    FEN | Lynx.Model.Position | 1,217.1 ns | 4.07 ns | 3.61 ns |  1.06 | 0.0229 |     432 B |
 *  |    FEN | Lynx.Model.Position | 1,215.4 ns | 1.23 ns | 1.09 ns |  1.06 | 0.0210 |     424 B |
 *  |     Id | Lynx.Model.Position |   910.5 ns | 6.59 ns | 5.84 ns |  0.79 | 0.0763 |   1,440 B |
 *  |     Id | Lynx.Model.Position |   953.3 ns | 3.51 ns | 3.28 ns |  0.83 | 0.0782 |   1,488 B |
 *  |     Id | Lynx.Model.Position |   986.9 ns | 8.48 ns | 7.93 ns |  0.86 | 0.0820 |   1,536 B |
 *  |     Id | Lynx.Model.Position |   871.7 ns | 6.84 ns | 6.40 ns |  0.76 | 0.0734 |   1,384 B |
 *
 *
 */

using BenchmarkDotNet.Attributes;
using Lynx.Model;
using System.Collections.Generic;

namespace Lynx.Benchmark
{
    public class PositionIdGeneration : BaseBenchmark
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public string FEN(Position newPosition)
        {
            return newPosition.CalculateFEN();
        }

        /// <summary>
        /// Twice as faster, but allocates three times more
        /// </summary>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public string Id(Position newPosition)
        {
            return newPosition.CalculateId();
        }

        public static Position[] Positions => new Position[] {
            new (Constants.InitialPositionFEN),
            new (Constants.TrickyTestPositionFEN),
            new ("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq - 0 1"),
            new ("rnbqkb1r/pp1p1pPp/8/2p1pP2/1P1P4/3P3P/P1P1P3/RNBQKBNR w KQkq e6 0 1"),
            new ("r2q1rk1/ppp2ppp/2n1bn2/2b1p3/3pP3/3P1NPP/PPP1NPB1/R1BQ1RK1 b - - 0 1)")
        };

        public static IEnumerable<Position> Data => new[] {
            //Constants.EmptyBoardFEN,
            new Position(Positions[0], Positions[0].AllPossibleMoves()[0]),
            new Position(Positions[1], Positions[1].AllPossibleMoves()[0]),
            new Position(Positions[2], Positions[2].AllPossibleMoves()[0]),
            new Position(Positions[3], Positions[3].AllPossibleMoves()[0])
        };
    }
}