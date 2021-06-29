﻿using Lynx.Model;
using Lynx.UCI.Commands.GUI;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Lynx.Search.SearchAlgorithms;

namespace Lynx
{
    public class Engine
    {
        private readonly Logger _logger;
        private bool _isNewGameCommandSupported;
        private bool _isNewGameComing;
        private bool _isPondering;

        public RegisterCommand? Registration { get; set; }

        public Game Game { get; private set; }

        private bool _isReady;
        /// <summary>
        /// Ready for <see cref="IsReadyCommand"/> purposes. Normally true
        /// </summary>
        public bool IsReady
        {
            get => _isReady;
            private set
            {
                _isReady = value;
                if (value)
                {
                    OnReady?.Invoke();
                }
            }
        }

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (!_isSearching && value)
                {
                    _isSearching = value;
                    //_searchCancellationTokenSource.TryReset();
                    //OnSearchFinished?.Invoke(BestMove(_lastGoCommand, _searchCancellationTokenSource.Token), MoveToPonder()).Wait();
                }
                else
                {
                    _isSearching = value;
                }
            }
        }

        public bool PendingConfirmation { get; set; }

        public delegate Task NotifyReadyOKHandler();
        public event NotifyReadyOKHandler? OnReady;

        public delegate Task NotifyEndOfSearch(SearchResult searchResult, Move? moveToPonder);
        public event NotifyEndOfSearch? OnSearchFinished;

        private CancellationTokenSource _searchCancellationTokenSource;

        public Engine()
        {
            Game = new Game();
            IsReady = true;
            _isNewGameComing = true;
            _logger = LogManager.GetCurrentClassLogger();
            _searchCancellationTokenSource = new();
        }

        internal void SetGame(Game game)
        {
            Game = game;
        }

        public void NewGame()
        {
            _isNewGameComing = true;
            _isNewGameCommandSupported = true;
        }

        public void AdjustPosition(string rawPositionCommand)
        {
            if (_isNewGameComing || !_isNewGameCommandSupported)
            {
                ParseWholeGame(rawPositionCommand);
            }
            else
            {
                if (!PositionCommand.TryParseLastMove(rawPositionCommand, Game, out var lastMove)
                    || !Game.MakeMove(lastMove.Value))
                {
                    _logger.Warn(
                        $"Position couldn't be adjusted using last move in position command: {rawPositionCommand}" + Environment.NewLine +
                        "Retrying parsing the whole game");

                    ParseWholeGame(rawPositionCommand);
                }
            }

            void ParseWholeGame(string rawPositionCommand)
            {
                Game = PositionCommand.ParseGame(rawPositionCommand);
                _isNewGameComing = false;
            }
        }

        public void PonderHit()
        {
            Game.MakeMove(MoveToPonder()!.Value);   // TODO: do we also receive the position command? If so, remove this line
            _isPondering = false;
        }

        public SearchResult BestMove() => BestMove(null);

        public SearchResult BestMove(GoCommand? goCommand)
        {
            int? millisecondsLeft;
            int? millisecondsIncrement;

            if (Game.CurrentPosition.Side == Side.White)
            {
                millisecondsLeft = goCommand?.WhiteTime;
                millisecondsIncrement = goCommand?.WhiteIncrement;
            }
            else
            {
                millisecondsLeft = goCommand?.BlackTime;
                millisecondsIncrement = goCommand?.BlackIncrement;
            }

            if (goCommand is not null && millisecondsLeft != 0)
            {
                _searchCancellationTokenSource = new CancellationTokenSource();
                int decisionTime = Convert.ToInt32((0.9 * millisecondsLeft!.Value / goCommand!.MovesToGo) + millisecondsIncrement!.Value);
                _logger.Info($"Time to move: {0.001 * decisionTime}s");
                _searchCancellationTokenSource.CancelAfter(decisionTime);
            }

            var result = NegaMax_AlphaBeta_Quiescence_IDDFS(Game.CurrentPosition, goCommand?.MovesToGo, millisecondsLeft, _searchCancellationTokenSource.Token);

            _logger.Debug($"Evaluation: {result.Evaluation} (depth: {result.TargetDepth}, refutation: {string.Join(", ", result.Moves)})");

            Game.MakeMove(result.BestMove);

            return result;
        }

        public SearchResult BestMoveOld(int? millisecondsLeft, int? movesToGo)
        {
            // var bestMove =  FindRandomMove();

            //var game = FindBestMove_Naive(Game.CurrentPosition);
            //var bestMove = game.MoveHistory.Last();

            //var bestMove = FindBestMove_Depth1();

            //PrintMovesStaticEval();
            //var result = new Result();
            //var evaluation = MiniMax_InitialImplementation(Game.CurrentPosition, Configuration.Parameters?.Depth ?? 3, result);

            //var (evaluation, moveList) = MiniMax(Game.CurrentPosition);

            //var (evaluation, moveList) = MiniMax_AlphaBeta(Game.CurrentPosition);

            //var (evaluation, moveList) = MiniMax_AlphaBeta_Quiescence(Game.CurrentPosition);

            //var (evaluation, moveList) = NegaMax(Game.CurrentPosition);
            //var (evaluation, moveList) = NegaMax_AlphaBeta_Quiescence(Game.CurrentPosition);

            //var (evaluation, moveList) = NegaMax_AlphaBeta_Quiescence_InitialImplementation(Game.CurrentPosition);

            var (evaluation, moveList) = NegaMax_AlphaBeta_Quiescence(Game.CurrentPosition, Configuration.Parameters.Depth);

            _logger.Debug($"Evaluation: {evaluation}");
            var bestMove = moveList!.Moves.Last();   // TODO: MoveList can be empty if the initial position is stalement or checkmate
            Game.MakeMove(bestMove);


            return new SearchResult(bestMove, evaluation, Configuration.Parameters.Depth, moveList.MaxDepth ?? Configuration.Parameters.Depth, moveList.Moves);
        }

        public Move? MoveToPonder()
        {
            // TODO
            return default;
        }

        public void StartSearching(GoCommand goCommand)
        {
            _isPondering = goCommand.Ponder;
            IsSearching = true;
            Task.Run(() =>
            {
                var searchResult = BestMove(goCommand);
                OnSearchFinished?.Invoke(searchResult, searchResult.Moves.Count >= 2 ? searchResult.Moves.ElementAt(1) : null);
            });
            // TODO: if ponder, continue with PonderAction, which is searching indefinitely for a move
        }

        public void StopSearching()
        {
            _searchCancellationTokenSource.Cancel();
            IsSearching = false;
            // TODO
        }

        private Move FindRandomMove()
        {
            foreach (var move in Game.GetAllMoves().OrderBy(_ => Guid.NewGuid()))
            {
                if (Game.MakeMove(move))
                {
                    return move;
                }
            }

            return default;
        }

        private Game FindBestMove_Naive(Position position, int depth = 3, Game? game = null)
        {
            if (game is null)
            {
                game = new Game();
            }

            if (depth == 0)
            {
                game.PositionHistory.Add(position);
                return game;
            }

            var positionMoveList = new List<(Move Move, Position Position)>(150);

            foreach (var move in MoveGenerator.GenerateAllMoves(position))
            {
                var newPosition = new Position(position, move);
                if (newPosition.IsValid())
                {
                    positionMoveList.Add((move, newPosition));
                }
            }

            var optimalPair = positionMoveList.OrderBy(pair => EvaluatePosition_Naive(pair.Position, depth - 1)).First();
            game.MoveHistory.Add(optimalPair.Move);
            game.PositionHistory.Add(optimalPair.Position);

            return game;
        }

        private int EvaluatePosition_Naive(Position position, int depth)
        {
            if (depth == 0)
            {
                return position.EvaluateMaterial();
            }

            var positions = new List<Position>(150);

            foreach (var move in MoveGenerator.GenerateAllMoves(position))
            {
                var newPosition = new Position(position, move);
                if (newPosition.IsValid())
                {
                    positions.Add(newPosition);
                }
            }

            if (positions.Count == 0)
            {
                if (Attacks.IsSquaredAttackedBySide(
                    position.PieceBitBoards[(int)Piece.K + Utils.PieceOffset(position.Side)].GetLS1BIndex(),
                    position,
                    (Side)Utils.OppositeSide(position.Side)))
                {
                    return int.MinValue;
                }
                else
                {
                    return 0;
                }
            }

            return positions.Max(p => EvaluatePosition_Naive(p, depth - 1));
        }

        private Move FindBestMove_Depth1()
        {
            var evalMoveList = new List<(Move Move, int eval)>(150);

            foreach (var move in MoveGenerator.GenerateAllMoves(Game.CurrentPosition))
            {
                var newPosition = new Position(Game.CurrentPosition, move);
                if (newPosition.IsValid())
                {
                    var eval = newPosition.EvaluateMaterialAndPosition_MiniMax();
                    Console.WriteLine($"{move,-6} | {newPosition.EvaluateMaterial(),-5} | {eval,-5}");
                    evalMoveList.Add((move, eval));
                }
            }

            return evalMoveList.OrderByDescending(l => l.eval).First().Move;
        }

        private void PrintMovesStaticEval()
        {
            foreach (var move in MoveGenerator.GenerateAllMoves(Game.CurrentPosition))
            {
                var newPosition = new Position(Game.CurrentPosition, move);
                if (newPosition.IsValid())
                {
                    var eval = newPosition.EvaluateMaterialAndPosition_MiniMax();
                    Console.WriteLine($"{move,-6} | {newPosition.EvaluateMaterial(),-5} | {eval,-5}");
                }
            }
        }
    }
}