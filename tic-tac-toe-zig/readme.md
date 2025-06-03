# Zig Tic-Tac-Toe

A simple command-line Tic-Tac-Toe game written in the Zig programming language, with customizable player colors.

## Description

This is a classic Tic-Tac-Toe (also known as Noughts and Crosses) game where two players take turns marking spaces in a 3x3 grid. The player who succeeds in placing three of their marks in a horizontal, vertical, or diagonal row wins the game. If all nine squares are filled and no player has three in a row, the game is a draw.

This implementation runs in the console/terminal and allows players to customize the colors of the 'X' and 'O' marks using command-line arguments.

## Features

- Two-player gameplay (X and O).
- Customizable colors for 'X' (Player 1) and 'O' (Player 2) via command-line arguments.
- Input validation for moves.
- Win and draw detection.
- Simple text-based interface with screen clearing.

## Demo

<video controls width="600">
  <source src="demo/demo.mp4" type="video/mp4">
  Your browser does not support the video tag. You can <a href="demo.mp4">download the demo video here</a>.
</video>

## Requirements

- **Zig Compiler:** You need to have the Zig compiler installed on your system. You can download it from the [official Zig website](https://ziglang.org/download/).

## How to Build

1.  **Clone the repository or download the source code.**
    If you have the `tic_tac_toe.zig` file (or whatever you named your source file), navigate to its directory in your terminal.

2.  **Compile the code:**
    Open your terminal or command prompt, navigate to the directory containing the `.zig` source file, and run the following command:

    ```bash
    zig build-exe tic_tac_toe.zig
    ```

    This will generate an executable file (e.g., `tic_tac_toe` on Linux/macOS or `tic_tac_toe.exe` on Windows) in the current directory.

    To specify an output name (e.g., `my_game`):

    ```bash
    zig build-exe tic_tac_toe.zig --name my_game
    ```

## How to Play

1.  **Run the executable:**
    After successful compilation, run the generated executable from your terminal.

    - On Linux/macOS:

      ```bash
      ./tic_tac_toe
      ```

      (or `./my_game` if you specified that name)

    - On Windows:
      ```bash
      .\tic_tac_toe.exe
      ```
      (or `.\my_game.exe`)

2.  **Running with Custom Colors (Optional):**
    You can specify colors for Player 1 ('X') and Player 2 ('O') using command-line arguments in the format `p1=colorname` and `p2=colorname`.

    - **Available color names:** `red`, `green`, `yellow`, `blue`, `magenta`, `cyan`, `white`.
    - If a color name is not recognized or not provided, the default terminal color will be used for that player.

    **Examples:**

    - Make Player 1 (X) red and Player 2 (O) green:

      ```bash
      ./tic_tac_toe p1=red p2=green
      ```

      (On Windows: `.\tic_tac_toe.exe p1=red p2=green`)

    - Make Player 1 (X) blue (Player 2 will use default color):

      ```bash
      ./tic_tac_toe p1=blue
      ```

    - Make Player 2 (O) cyan (Player 1 will use default color):
      ```bash
      ./tic_tac_toe p2=cyan
      ```

3.  **Gameplay:**
    - The screen will clear, and the game board will be displayed, with cells numbered 1 through 9.
    - Players 'X' and 'O' will take turns.
    - When prompted, enter the number (1-9) corresponding to the cell where you want to place your mark and press Enter.
    - The screen will update after each move.
    - The game will announce if a player wins or if the game is a draw.
    - After the game ends, press 'q' to quit.

## Example Code File Name

This README assumes your Zig source file might be named `tic_tac_toe.zig`. Please adjust the build and run commands if your file has a different name.

Enjoy the game!
