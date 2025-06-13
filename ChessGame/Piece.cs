using System;

public enum PieceColor { White, Black }

public abstract class Piece {
    public PieceColor Color { get; }


    protected Piece(PieceColor color)
    {
        Color = color;
    }

    public abstract char Symbol { get; }
    public abstract bool IsValidMove(int startRow, int startColumn, int endRow, int endColumn, Piece[,] cells); //!
}

public static class BoardBounds {
    // returns true if position is between 0-7
    public static bool IsWithinBounds(int row, int column) {
        return row >= 0 && row < 8 && column >= 0 && column < 8;
    }
}



public class Pawn : Piece
{
    public Pawn(PieceColor color) : base(color) { }
   
    // P - white p - black
    public override char Symbol => Color == PieceColor.White ? 'P' : 'p';

    public override bool IsValidMove(int startRow, int startColumn, int endRow, int endColumn, Piece[,] cells)
    {

        if (!BoardBounds.IsWithinBounds(endRow, endColumn)) return false;

        int direction = (Color == PieceColor.White) ? 1 : -1;

        // forward moves
        if (startColumn == endColumn)
        {
            // one square
            if (endRow - startRow == direction && cells[endRow, endColumn] == null) return true;
            // two squares from initial position 
            if (Color == PieceColor.White && startRow == 1 && endRow == 3
                && cells[2, startColumn] == null && cells[3, startColumn] == null) return true;  //!
            if (Color == PieceColor.Black && startRow == 6 && endRow == 4
                && cells[5, startColumn] == null && cells[4, startColumn] == null) return true;
        }

        // diagonal capture (special case)
        if (Math.Abs(endColumn - startColumn) == 1 && endRow - startRow == direction)
        {
            var target = cells[endRow, endColumn];
            if (target != null && target.Color != Color) return true;
        }


        return false;
    }


}



public class Rook : Piece
{
    public bool HasMoved { get; set; }

    public Rook(PieceColor color) : base(color)
    {
        HasMoved = false;
    }

    public override char Symbol => Color == PieceColor.White ? 'R' : 'r';

    public override bool IsValidMove(int startRow, int startColumn, int endRow, int endColumn, Piece[,] cells)
    {

        if (!BoardBounds.IsWithinBounds(endRow, endColumn)) return false;
        if (startRow != endRow && startColumn != endColumn) return false;

        // check if the path is clear
        if (startRow == endRow)
        {
            int step = (endColumn > startColumn) ? 1 : -1;
            for (int col = startColumn + step; col != endColumn; col += step)
                if (cells[startRow, col] != null) return false;
        }
        else
        {
            int step = (endRow > startRow) ? 1 : -1;
            for (int row = startRow + step; row != endRow; row += step)
                if (cells[row, startColumn] != null) return false;
        }

        var dest = cells[endRow, endColumn];
        return dest == null || dest.Color != Color;
    }

    
}

public class Knight : Piece
{
    public Knight(PieceColor color) : base(color) { }

    public override char Symbol => Color == PieceColor.White ? 'N' : 'n';

    public override bool IsValidMove(int startRow, int startColumn, int endRow, int endColumn, Piece[,] cells)
    {
        if (!BoardBounds.IsWithinBounds(endRow, endColumn)) return false;

        int dr = Math.Abs(endRow - startRow);
        int dc = Math.Abs(endColumn - startColumn);
        if (!((dr == 2 && dc == 1) || (dr == 1 && dc == 2))) return false;

        var dest = cells[endRow, endColumn];
        return dest == null || dest.Color != Color;
    }
    

}

public class Bishop : Piece
{
    public Bishop(PieceColor color) : base(color) { }

    public override char Symbol => Color == PieceColor.White ? 'B' : 'b';

    public override bool IsValidMove(int startRow, int startColumn, int endRow, int endColumn, Piece[,] cells)
    {
        if (!BoardBounds.IsWithinBounds(endRow, endColumn)) return false;

        int dr = Math.Abs(endRow - startRow);
        int dc = Math.Abs(endColumn - startColumn);
        if (dr != dc) return false;

        int stepR = (endRow > startRow) ? 1 : -1;
        int stepC = (endColumn > startColumn) ? 1 : -1;
        for (int i = 1; i < dr; i++)
            if (cells[startRow + i * stepR, startColumn + i * stepC] != null) return false;

        var dest = cells[endRow, endColumn];
        return dest == null || dest.Color != Color;
    }

}


public class Queen : Piece
{
    public Queen(PieceColor color) : base(color) { }

    public override char Symbol => Color == PieceColor.White ? 'Q' : 'q';

    // queen = rock + bishop
    public override bool IsValidMove(int startRow, int startColumn, int endRow, int endColumn, Piece[,] cells)
    {
        // using rook logic
        if (new Rook(Color).IsValidMove(startRow, startColumn, endRow, endColumn, cells)) return true;
        // using bishop logic
        return new Bishop(Color).IsValidMove(startRow, startColumn, endRow, endColumn, cells);
    }
}



public class King : Piece
{
    public bool HasMoved { get; set; }

    public King(PieceColor color) : base(color)
    {
        HasMoved = false;
    }

    public override char Symbol => Color == PieceColor.White ? 'K' : 'k';

    public override bool IsValidMove(int startRow, int startColumn, int endRow, int endColumn, Piece[,] cells)
    {
        if (!BoardBounds.IsWithinBounds(endRow, endColumn)) return false;

        int dr = Math.Abs(endRow - startRow);
        int dc = Math.Abs(endColumn - startColumn);

        // one square move
        if (dr <= 1 && dc <= 1)
        {
            var dest = cells[endRow, endColumn];
            return dest == null || dest.Color != Color;
        }

        // castling move
        if (!HasMoved && dr == 0 && dc == 2)
        {
            int rookCol = (endColumn > startColumn) ? 7 : 0;
            if (cells[startRow, rookCol] is Rook r && r.Color == Color && !r.HasMoved)
            {
                int step = (endColumn > startColumn) ? 1 : -1;
                for (int c = startColumn + step; c != rookCol; c += step)
                {
                    if (!BoardBounds.IsWithinBounds(startRow, c) || cells[startRow, c] != null) return false;
                }
                return true; // check passed elsewhere
            }
        }

        return false;
    }
}



