const express = require("express");
const http = require("http");
const { Server } = require("socket.io");

const app = express();
const server = http.createServer(app);
const io = new Server(server);

app.use(express.static("public"));

let players = {};
let readyPlayers = {};
let scores = { p1: 0, p2: 0 };
let paddleWidth = 80;
let paddleHeight = 10;
let paddles = { p1: 260, p2: 260 };
let ball = { x: 300, y: 200, vx: 5, vy: 5, radius: 10 };
let gameInterval = null;

function resetBall() {
  ball.x = 300;
  ball.y = 200;
  ball.vx = (Math.random() > 0.5 ? 1 : -1) * 2;
  ball.vy = (Math.random() > 0.5 ? 1 : -1) * 2;
}

function updateGame() {
  ball.x += ball.vx;
  ball.y += ball.vy;

  if (ball.x - ball.radius < 0 || ball.x + ball.radius > 600) {
    ball.vx *= -1;
  }

  if (
    ball.y - ball.radius < 20 &&
    ball.x > paddles.p1 &&
    ball.x < paddles.p1 + paddleWidth
  ) {
    ball.vy *= -1;
  }

  if (
    ball.y + ball.radius > 380 &&
    ball.x > paddles.p2 &&
    ball.x < paddles.p2 + paddleWidth
  ) {
    ball.vy *= -1;
  }

  if (ball.y < 0) {
    scores.p2++;
    resetBall();
  } else if (ball.y > 400) {
    scores.p1++;
    resetBall();
  }

  if (scores.p1 === 5 || scores.p2 === 5) {
    io.emit("gameOver", scores);
    clearInterval(gameInterval);
    gameInterval = null;
    return;
  }

  io.emit("gameState", { ball, paddles, scores });
}

function tryStartGame() {
  if (Object.keys(readyPlayers).length === 2 && !gameInterval) {
    let countdown = 3;
    io.emit("countdown", countdown);
    const countdownInterval = setInterval(() => {
      countdown--;
      if (countdown > 0) {
        io.emit("countdown", countdown);
      } else {
        clearInterval(countdownInterval);
        resetBall();
        gameInterval = setInterval(updateGame, 1000 / 60);
        io.emit("gameStarted");
      }
    }, 1000);
  }
}

io.on("connection", (socket) => {
  console.log("Player connected:", socket.id);

  if (!players.p1) {
    players.p1 = socket.id;
    socket.emit("playerNumber", 1);
  } else if (!players.p2) {
    players.p2 = socket.id;
    socket.emit("playerNumber", 2);
  } else {
    socket.emit("full");
    return;
  }

  io.emit("playerCount", Object.keys(players).length);

  socket.on("paddleMove", (xPos) => {
    if (socket.id === players.p1) paddles.p1 = xPos;
    if (socket.id === players.p2) paddles.p2 = xPos;
  });

  function resetGameState() {
    scores = { p1: 0, p2: 0 };
    paddles = { p1: 260, p2: 260 };
    readyPlayers = {};
    clearInterval(gameInterval);
    gameInterval = null;
  }

  socket.on("playerReady", () => {
    readyPlayers[socket.id] = true;
    io.emit("playerReadyStatus", Object.keys(readyPlayers).length);
    tryStartGame();
    if (scores.p1 === 5 || scores.p2 === 5) {
      io.emit("gameOver", scores);
      resetGameState();
      return;
    }
  });

  socket.on("disconnect", () => {
    console.log("Player disconnected:", socket.id);

    if (socket.id === players.p1) delete players.p1;
    if (socket.id === players.p2) delete players.p2;
    if (readyPlayers[socket.id]) delete readyPlayers[socket.id];

    io.emit("playerCount", Object.keys(players).length);
    io.emit("playerReadyStatus", Object.keys(readyPlayers).length);

    clearInterval(gameInterval);
    gameInterval = null;
    scores = { p1: 0, p2: 0 };
    paddles = { p1: 260, p2: 260 };
  });
});

server.listen(3000, "0.0.0.0", () => {
  console.log("Server running on http://localhost:3000");
});
