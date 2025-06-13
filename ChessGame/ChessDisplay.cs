using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

public class ChessDisplay : Form {
    const int size = 80;
    Board gameBoard = new Board();
    Dictionary<string, Image> pieceImages = new Dictionary<string, Image>();
    Image boardBackground;
    Point selectedSquare = new Point(-1, -1);
    bool isSquareSelected = false;
    PieceColor currentTurn = PieceColor.White;
    bool isGameOver = false;
    Point enPassantSquare = new Point(-1, -1);
    string saveFilePath;

    public ChessDisplay() {
        Text = "Chess Game";
        ClientSize = new Size(size * 8, size * 8 + 50);
        DoubleBuffered = true;

        saveFilePath = Path.Combine(AppContext.BaseDirectory, "save.json");
        LoadPieceImages();
        LoadGameState();

        Paint += OnPaint;
        MouseClick += OnMouseClick;

        //restart button
        var restartButton = new Button {
            Text = "Restart",
            Size = new Size(80, 30),
            Location = new Point(10, size * 8 + 10)
        };
        restartButton.Click += (_, __) => RestartGame();
        Controls.Add(restartButton);

        //save button
        var saveButton = new Button {
            Text = "Save",
            Size = new Size(80, 30),
            Location = new Point(100, size * 8 + 10)
        };
        saveButton.Click += (_, __) => {
            SaveGameState();
            MessageBox.Show("Game saved", "Save");
        };
        Controls.Add(saveButton);

        // exit button
        var exitButton = new Button {
            Text = "Exit",
            Size = new Size(80, 30),
            Location = new Point(190, size * 8 + 10)
        };
        exitButton.Click += (_, __) => {
            if (MessageBox.Show("Save before exit?", "Exit", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                SaveGameState();
            }
            Application.Exit();
        };
        Controls.Add(exitButton);
    }

    // loads the board background and piece images from /pngs
    void LoadPieceImages() {
        string imagesFolder = Path.Combine(AppContext.BaseDirectory, "pngs");
        boardBackground = Image.FromFile(Path.Combine(imagesFolder, "board.png"));
        string[] colors = { "white", "black" };
        string[] pieces = { "pawn", "rook", "knight", "bishop", "queen", "king" };
        foreach (var color in colors) {
            foreach (var piece in pieces) {
                pieceImages[$"{color}_{piece}"] = Image.FromFile(Path.Combine(imagesFolder, $"{color}_{piece}.png"));
            }
        }
        pieceImages["white_king_check"] = Image.FromFile(Path.Combine(imagesFolder, "white_king_check.png"));
        pieceImages["black_king_check"] = Image.FromFile(Path.Combine(imagesFolder, "black_king_check.png"));
    }

    // draws the board, pieces, and selection highlight
    void OnPaint(object sender, PaintEventArgs e) {
        var graphics = e.Graphics;
        graphics.DrawImage(boardBackground, 0, 0, size * 8, size * 8);

        if (isSquareSelected) {
            using var highlightPen = new Pen(Color.White, 4);
            graphics.DrawRectangle(highlightPen, selectedSquare.X * size, selectedSquare.Y * size, size, size);
        }

        bool isWhiteInCheck = gameBoard.IsKingInCheck(PieceColor.White);
        bool isBlackInCheck = gameBoard.IsKingInCheck(PieceColor.Black);

        for (int row = 0; row < 8; row++) {
            for (int col = 0; col < 8; col++) {
                var piece = gameBoard.Cells[row, col];
                if (piece == null) continue;
                string key = (piece.Color == PieceColor.White ? "white_" : "black_") + piece.GetType().Name.ToLower();
                if (piece is King) {
                    if (piece.Color == PieceColor.White && isWhiteInCheck) key = "white_king_check";
                    if (piece.Color == PieceColor.Black && isBlackInCheck) key = "black_king_check";
                }
                if (pieceImages.TryGetValue(key, out var img)) {
                    graphics.DrawImage(img, col * size, row * size, size, size);
                }
            }
        }
    }

    // handles mouse clicks for selecting and moving pieces
    void OnMouseClick(object sender, MouseEventArgs e) {
        if (isGameOver) return;
        int col = e.X / size;
        int row = e.Y / size;
        if (col < 0 || col > 7 || row < 0 || row > 7) return;

        if (!isSquareSelected) {
            var clickedPiece = gameBoard.Cells[row, col];
            if (clickedPiece != null && clickedPiece.Color == currentTurn) {
                selectedSquare = new Point(col, row);
                isSquareSelected = true;
                Invalidate();
            }
            return;
        }

        var from = selectedSquare;
        var selectedPiece = gameBoard.Cells[from.Y, from.X];
        bool isMoveValid = false;
        bool isEnPassant = false;

        if (selectedPiece != null && selectedPiece.Color == currentTurn) {
            if (selectedPiece.IsValidMove(from.Y, from.X, row, col, gameBoard.Cells)) {
                isMoveValid = true;
            } else if (selectedPiece is Pawn && enPassantSquare.X >= 0) {
                int direction = currentTurn == PieceColor.White ? 1 : -1;
                if (row == enPassantSquare.Y && col == enPassantSquare.X && Math.Abs(from.X - col) == 1 && from.Y + direction == row &&
                    gameBoard.Cells[from.Y, col] is Pawn adjacentPawn && adjacentPawn.Color != currentTurn) {
                    isMoveValid = true;
                    isEnPassant = true;
                }
            }
        }

        if (isMoveValid) {
            var simulation = gameBoard.CloneCells();
            simulation[row, col] = simulation[from.Y, from.X];
            simulation[from.Y, from.X] = null;
            if (gameBoard.WouldKingBeInCheck(simulation, currentTurn)) {
                isSquareSelected = false;
                Invalidate();
                return;
            }

            if (isEnPassant) {
                int captureRow = currentTurn == PieceColor.White ? row - 1 : row + 1;
                gameBoard.Cells[captureRow, col] = null;
            }

            if (selectedPiece is King && Math.Abs(col - from.X) == 2) {
                HandleCastling(from, col, selectedPiece);
            }

            // execute move and update flags
            gameBoard.Cells[row, col] = selectedPiece;
            gameBoard.Cells[from.Y, from.X] = null;
            if (selectedPiece is King movedKing) movedKing.HasMoved = true;
            if (selectedPiece is Rook movedRook) movedRook.HasMoved = true;

            if (selectedPiece is Pawn && Math.Abs(row - from.Y) == 2) {
                enPassantSquare = new Point(col, (row + from.Y) / 2);
            } else {
                enPassantSquare = new Point(-1, -1);
            }

            if (selectedPiece is Pawn && ((selectedPiece.Color == PieceColor.White && row == 7) || (selectedPiece.Color == PieceColor.Black && row == 0))) {
                ShowPromotion(row, col, selectedPiece.Color);
            }

            currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            isSquareSelected = false;
            Invalidate();
            Refresh();

            if (gameBoard.IsCheckmate(currentTurn)) {
                MessageBox.Show(currentTurn + " is checkmated!", "Checkmate");
                isGameOver = true;
            } else if (gameBoard.IsStalemate(currentTurn)) {
                MessageBox.Show("Stalemate!", "Draw");
                isGameOver = true;
            } else if (gameBoard.IsInsufficientMaterial()) {
                MessageBox.Show("Insufficient material!", "Draw");
                isGameOver = true;
            }
        }

        isSquareSelected = false;
        Invalidate();
    }

    // moving the rook when castling
    void HandleCastling(Point from, int newCol, Piece king) {
        int rookCol = newCol > from.X ? 7 : 0;
        int targetCol = newCol > from.X ? newCol - 1 : newCol + 1;
        if (gameBoard.Cells[from.Y, rookCol] is Rook rook && !((King)king).HasMoved && !rook.HasMoved) {
            gameBoard.Cells[from.Y, targetCol] = rook;
            gameBoard.Cells[from.Y, rookCol] = null;
            rook.HasMoved = true;
        }
    }

    // promotion dialog
    void ShowPromotion(int row, int col, PieceColor color) {
        var promotion = new Promotion(color) { ClientSize = new Size(400, 120) };
        if (promotion.ShowDialog() == DialogResult.OK) {
            switch (promotion.SelectedPiece) {
                case "Rook":   gameBoard.Cells[row, col] = new Rook(color);   break;
                case "Bishop": gameBoard.Cells[row, col] = new Bishop(color); break;
                case "Knight": gameBoard.Cells[row, col] = new Knight(color); break;
                default:        gameBoard.Cells[row, col] = new Queen(color);  break;
            }
        }
    }

    //resets the board
    void RestartGame() {
        gameBoard.SetupDefaultPosition();
        currentTurn = PieceColor.White;
        isSquareSelected = false;
        isGameOver = false;
        enPassantSquare = new Point(-1, -1);
        Invalidate();
    }

    // saveing the current game
    void SaveGameState() {
        var piecesList = new List<object>();
        for (int r = 0; r < 8; r++) {
            for (int c = 0; c < 8; c++) {
                var piece = gameBoard.Cells[r, c];
                if (piece != null) {
                    piecesList.Add(new {
                        Row = r,
                        Column = c,
                        Type = piece.GetType().Name,
                        Color = piece.Color.ToString()
                    });
                }
            }
        }
        var state = new { Turn = currentTurn.ToString(), Pieces = piecesList };
        File.WriteAllText(saveFilePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    // loads the game state from a JSON file if it exists
    void LoadGameState() {
        if (!File.Exists(saveFilePath)) return;
        try {
            var json = File.ReadAllText(saveFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // clearing the board
            for (int r = 0; r < 8; r++) {
                for (int c = 0; c < 8; c++) {
                    gameBoard.Cells[r, c] = null;
                }
            }

            // loading the pieces
            foreach (var ps in root.GetProperty("Pieces").EnumerateArray()) {
                int r = ps.GetProperty("Row").GetInt32();
                int c = ps.GetProperty("Column").GetInt32();
                string typeName = ps.GetProperty("Type").GetString();
                PieceColor pc = Enum.Parse<PieceColor>(ps.GetProperty("Color").GetString());
                Piece newPiece = typeName switch {
                    "Pawn"   => new Pawn(pc),
                    "Rook"   => new Rook(pc),
                    "Knight" => new Knight(pc),
                    "Bishop" => new Bishop(pc),
                    "Queen"  => new Queen(pc),
                    "King"   => new King(pc),
                    _         => null
                };
                if (newPiece != null) gameBoard.Cells[r, c] = newPiece;
            }

            currentTurn = Enum.Parse<PieceColor>(root.GetProperty("Turn").GetString());
            Invalidate();
        } catch {
        }
    }

    // checks the end of the game situations
    void CheckEnd() {
        if (gameBoard.IsCheckmate(currentTurn)) {
            MessageBox.Show(currentTurn + " is checkmated!", "Checkmate");
            isGameOver = true;
        } else if (gameBoard.IsStalemate(currentTurn)) {
            MessageBox.Show("Stalemate!", "Draw");
            isGameOver = true;
        } else if (gameBoard.IsInsufficientMaterial()) {
            MessageBox.Show("Insufficient material!", "Draw");
            isGameOver = true;
        }
    }
}

//pawn promotion choice
public class Promotion : Form {
    public string SelectedPiece = "Queen";
    public Promotion(PieceColor c) {
        Text = "Promote Pawn";
        Size = new Size(420, 160);
        StartPosition = FormStartPosition.CenterParent;
        var names = new[] { "Queen", "Rook", "Bishop", "Knight" };
        for (int i = 0; i < 4; i++) {
            var button = new Button {
                Text = names[i],
                Size = new Size(100, 50),
                Location = new Point(10 + i * 105, 40)
            };
            button.Click += (s, e) => { SelectedPiece = button.Text; DialogResult = DialogResult.OK; };
            Controls.Add(button);
        }
    }
}
