using System;
using System.Collections.Generic;

public class Board {
    public const int BoardSize = 8;
    public Piece[,] Cells { get; private set; }

    public Board() {
        Cells = new Piece[BoardSize, BoardSize];
        SetupDefaultPosition();
    }

    public void SetupDefaultPosition() {
        // clear board
        Cells = new Piece[BoardSize, BoardSize];

        // white pieces setup
        Cells[0, 0] = new Rook(PieceColor.White);
        Cells[0, 1] = new Knight(PieceColor.White);
        Cells[0, 2] = new Bishop(PieceColor.White);
        Cells[0, 4] = new Queen(PieceColor.White);
        Cells[0, 3] = new King(PieceColor.White);
        Cells[0, 5] = new Bishop(PieceColor.White);
        Cells[0, 6] = new Knight(PieceColor.White);
        Cells[0, 7] = new Rook(PieceColor.White);

        // white pawns
        for (int col = 0; col < BoardSize; col++) {
            Cells[1, col] = new Pawn(PieceColor.White);
        }

        // black pawns
        for (int col = 0; col < BoardSize; col++) {
            Cells[BoardSize - 2, col] = new Pawn(PieceColor.Black);
        }

        // black pieces setup
        Cells[BoardSize - 1, 0] = new Rook(PieceColor.Black);
        Cells[BoardSize - 1, 1] = new Knight(PieceColor.Black);
        Cells[BoardSize - 1, 2] = new Bishop(PieceColor.Black);
        Cells[BoardSize - 1, 4] = new Queen(PieceColor.Black);
        Cells[BoardSize - 1, 3] = new King(PieceColor.Black);
        Cells[BoardSize - 1, 5] = new Bishop(PieceColor.Black);
        Cells[BoardSize - 1, 6] = new Knight(PieceColor.Black);
        Cells[BoardSize - 1, 7] = new Rook(PieceColor.Black);
    }

    public Piece[,] CloneCells() {
        var copy = new Piece[BoardSize, BoardSize];
        for (int r = 0; r < BoardSize; r++) {
            for (int c = 0; c < BoardSize; c++) {
                var piece = Cells[r, c];
                if (piece != null) {
                    copy[r, c] = piece switch {
                        Pawn _   => new Pawn(piece.Color),
                        Rook _   => new Rook(piece.Color),
                        Knight _ => new Knight(piece.Color),
                        Bishop _ => new Bishop(piece.Color),
                        Queen _  => new Queen(piece.Color),
                        King _   => new King(piece.Color),
                        _        => null
                    };
                }
            }
        }
        return copy;
    }

    public bool IsInside(int row, int column) {
        return row >= 0 && row < BoardSize && column >= 0 && column < BoardSize;
    }

    public bool IsKingInCheck(PieceColor kingColor) {
        int kingRow = -1, kingCol = -1;
        // find king
        for (int r = 0; r < BoardSize; r++) {
            for (int c = 0; c < BoardSize; c++) {
                var piece = Cells[r, c];
                if (piece is King && piece.Color == kingColor) {
                    kingRow = r;
                    kingCol = c;
                    break;
                }
            }
            if (kingRow >= 0) break;
        }
        if (!IsInside(kingRow, kingCol)) return true;

        // checking
        for (int r = 0; r < BoardSize; r++) {
            for (int c = 0; c < BoardSize; c++) {
                var piece = Cells[r, c];
                if (piece != null && piece.Color != kingColor) {
                    if (piece.IsValidMove(r, c, kingRow, kingCol, Cells)) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool WouldKingBeInCheck(Piece[,] boardCopy, PieceColor playerColor) {
        int kingRow = -1, kingCol = -1;
        // find king on board
        for (int r = 0; r < BoardSize; r++) {
            for (int c = 0; c < BoardSize; c++) {
                var piece = boardCopy[r, c];
                if (piece is King && piece.Color == playerColor) {
                    kingRow = r;
                    kingCol = c;
                    break;
                }
            }
            if (kingRow >= 0) break;
        }
        if (!IsInside(kingRow, kingCol)) return true;


        // check moves
        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                var piece = boardCopy[r, c];
                if (piece != null && piece.Color != playerColor)
                {
                    if (piece.IsValidMove(r, c, kingRow, kingCol, boardCopy))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool IsCheckmate(PieceColor playerColor) {
        if (!IsKingInCheck(playerColor)) return false;

        // trying every move
        for (int sr = 0; sr < BoardSize; sr++) {
            for (int sc = 0; sc < BoardSize; sc++) {
                var piece = Cells[sr, sc];
                if (piece == null || piece.Color != playerColor) continue;

                for (int er = 0; er < BoardSize; er++) {
                    for (int ec = 0; ec < BoardSize; ec++) {
                        if (IsInside(er, ec) && piece.IsValidMove(sr, sc, er, ec, Cells)) {
                            var tempBoard = CloneCells();
                            tempBoard[er, ec] = tempBoard[sr, sc];
                            tempBoard[sr, sc] = null;
                            if (!WouldKingBeInCheck(tempBoard, playerColor)) {
                                return false;
                            }
                        }
                    }
                }
            }
        }
        return true;
    }

    public bool IsStalemate(PieceColor playerColor) {
        if (IsKingInCheck(playerColor)) return false;

        // trying every move
        for (int sr = 0; sr < BoardSize; sr++) {
            for (int sc = 0; sc < BoardSize; sc++) {
                var piece = Cells[sr, sc];
                if (piece == null || piece.Color != playerColor) continue;

                for (int er = 0; er < BoardSize; er++) {
                    for (int ec = 0; ec < BoardSize; ec++) {
                        if (IsInside(er, ec) && piece.IsValidMove(sr, sc, er, ec, Cells)) {
                            var tempBoard = CloneCells();
                            tempBoard[er, ec] = tempBoard[sr, sc];
                            tempBoard[sr, sc] = null;
                            if (!WouldKingBeInCheck(tempBoard, playerColor)) {
                                return false;
                            }
                        }
                    }
                }
            }
        }
        return true;
    }

    public bool IsInsufficientMaterial() {
        var pieces = new List<Piece>();
        for (int r = 0; r < BoardSize; r++) {
            for (int c = 0; c < BoardSize; c++) {
                var piece = Cells[r, c];
                if (piece != null) pieces.Add(piece);
            }
        }
        // only kings
        if (pieces.Count == 2) return true;

        // king + bishop/knight vs king
        if (pieces.Count == 3) {
            foreach (var p in pieces) {
                if (p is Bishop || p is Knight) return true;
            }
        }

        // two bishops vs kings
        if (pieces.Count == 4) {
            var bishops = new List<Bishop>();
            foreach (var p in pieces) {
                if (p is Bishop b) bishops.Add(b);
            }
            if (bishops.Count == 2 && bishops[0].Color != bishops[1].Color) {
                return true;
            }
        }
        return false;
    }
}
