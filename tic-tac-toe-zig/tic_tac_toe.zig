const std = @import("std");
const print = std.debug.print;

var player1Color: []const u8 = "";
var player2Color: []const u8 = "";
const resetColor: []const u8 = "\x1b[0m";

var board: [3][3]u8 = .{
    .{ '1', '2', '3' },
    .{ '4', '5', '6' },
    .{ '7', '8', '9' },
};

var currentPlayer: u8 = 'X';
var movesMade: u8 = 0;

const clearScreenSequence = "\x1b[2J\x1b[H";

fn clearScreen(writer: anytype) !void {
    try writer.print(clearScreenSequence, .{});
}

fn getColorCode(colorName: []const u8) []const u8 {
    if (std.mem.eql(u8, colorName, "red")) {
        return "\x1b[31m";
    } else if (std.mem.eql(u8, colorName, "green")) {
        return "\x1b[32m";
    } else if (std.mem.eql(u8, colorName, "yellow")) {
        return "\x1b[33m";
    } else if (std.mem.eql(u8, colorName, "blue")) {
        return "\x1b[34m";
    } else if (std.mem.eql(u8, colorName, "magenta")) {
        return "\x1b[35m";
    } else if (std.mem.eql(u8, colorName, "cyan")) {
        return "\x1b[36m";
    } else if (std.mem.eql(u8, colorName, "white")) {
        return "\x1b[37m";
    }
    return "";
}

fn printCell(writer: anytype, cell_char: u8) !void {
    var color_to_use: []const u8 = "";
    if (cell_char == 'X') {
        color_to_use = player1Color;
    } else if (cell_char == 'O') {
        color_to_use = player2Color;
    }

    if (color_to_use.len > 0) {
        try writer.print("{s}{c}{s}", .{ color_to_use, cell_char, resetColor });
    } else {
        try writer.print("{c}", .{cell_char});
    }
}

fn printBoard(writer: anytype) !void {
    try writer.print("\n", .{});
    for (board) |row_data| {
        try writer.print(" ", .{});
        try printCell(writer, row_data[0]);
        try writer.print(" | ", .{});
        try printCell(writer, row_data[1]);
        try writer.print(" | ", .{});
        try printCell(writer, row_data[2]);
        try writer.print(" \n", .{});

        if (&row_data != &board[board.len - 1]) {
            try writer.print("---|---|---\n", .{});
        }
    }
    try writer.print("\n", .{});
}

fn isMoveValid(row: usize, column: usize) bool {
    return board[row][column] != 'X' and board[row][column] != 'O';
}

const MoveResult = enum {
    Success,
    InvalidChoice,
    PositionTaken,
};

fn processMove(choice: u8) MoveResult {
    var row: usize = 0;
    var column: usize = 0;

    switch (choice) {
        '1' => {
            row = 0;
            column = 0;
        },
        '2' => {
            row = 0;
            column = 1;
        },
        '3' => {
            row = 0;
            column = 2;
        },
        '4' => {
            row = 1;
            column = 0;
        },
        '5' => {
            row = 1;
            column = 1;
        },
        '6' => {
            row = 1;
            column = 2;
        },
        '7' => {
            row = 2;
            column = 0;
        },
        '8' => {
            row = 2;
            column = 1;
        },
        '9' => {
            row = 2;
            column = 2;
        },
        else => {
            return .InvalidChoice;
        },
    }

    if (isMoveValid(row, column)) {
        board[row][column] = currentPlayer;
        movesMade += 1;
        return .Success;
    } else {
        return .PositionTaken;
    }
}

fn checkWin() bool {
    for (board) |row_data| {
        if (row_data[0] == currentPlayer and row_data[1] == currentPlayer and row_data[2] == currentPlayer) {
            return true;
        }
    }

    for (0..3) |i| {
        if (board[0][i] == currentPlayer and board[1][i] == currentPlayer and board[2][i] == currentPlayer) {
            return true;
        }
    }

    if (board[0][0] == currentPlayer and board[1][1] == currentPlayer and board[2][2] == currentPlayer) {
        return true;
    }
    if (board[0][2] == currentPlayer and board[1][1] == currentPlayer and board[2][0] == currentPlayer) {
        return true;
    }

    return false;
}

fn checkDraw() bool {
    return movesMade == 9;
}

pub fn main() !void {
    const allocator = std.heap.page_allocator;
    const args_list = std.process.argsAlloc(allocator) catch |err| {
        print("Error allocating memory for arguments: {any}\n", .{err});
        std.process.exit(1);
    };
    defer std.process.argsFree(allocator, args_list);

    for (0..args_list.len) |i| {
        const arg = args_list[i];
        if (i == 0) continue;

        if (std.mem.indexOfScalar(u8, arg, '=')) |equals_idx| {
            const key = arg[0..equals_idx];
            const value = arg[equals_idx + 1 ..];

            if (std.mem.eql(u8, key, "p1")) {
                player1Color = getColorCode(value);
            } else if (std.mem.eql(u8, key, "p2")) {
                player2Color = getColorCode(value);
            }
        }
    }

    const stdout_writer = std.io.getStdOut().writer();
    const stdin_reader = std.io.getStdIn().reader();

    var gameOver = false;
    var buffer: [10]u8 = undefined;

    try clearScreen(stdout_writer);
    print("Welcome to Tic-Tac-Toe in Zig!\n", .{});

    while (!gameOver) {
        try printBoard(stdout_writer);
        try stdout_writer.print("Player {c}, your turn. Choose a position (1-9): ", .{currentPlayer});

        const bytesRead = try stdin_reader.read(buffer[0..]);
        if (bytesRead == 0) {
            try clearScreen(stdout_writer);
            try stdout_writer.print("\nEmpty input, exiting.\n", .{});
            return;
        }

        const choice = buffer[0];
        try clearScreen(stdout_writer);

        const move_result = processMove(choice);

        switch (move_result) {
            .Success => {
                if (checkWin()) {
                    try printBoard(stdout_writer);
                    try stdout_writer.print("Player {c} Won!\n", .{currentPlayer});
                    gameOver = true;
                } else if (checkDraw()) {
                    try printBoard(stdout_writer);
                    try stdout_writer.print("It's a draw!\n", .{});
                    gameOver = true;
                } else {
                    if (currentPlayer == 'X') {
                        currentPlayer = 'O';
                    } else {
                        currentPlayer = 'X';
                    }
                }
            },
            .InvalidChoice => {
                try stdout_writer.print("Invalid choice. Try again.\n", .{});
            },
            .PositionTaken => {
                try stdout_writer.print("Position already taken. Try again.\n", .{});
            },
        }
    }

    if (gameOver) {
        try stdout_writer.print("Press q to quit.\n", .{});
        while (true) {
            const bytesRead_quit = try stdin_reader.read(buffer[0..]);
            if (bytesRead_quit > 0 and buffer[0] == 'q') {
                try clearScreen(stdout_writer);
                print("Thanks for playing!\n", .{});
                break;
            }
        }
    }
}
