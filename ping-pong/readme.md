# Two-Player Pong Game with Socket.IO

A classic Pong game built with Node.js and Socket.IO where two players control paddles from different clients. The game includes a "Ready" button and a countdown before starting.

---

## Features

- Real-time multiplayer gameplay using WebSockets (Socket.IO)
- Two players control paddles:
  - Player 1 paddle (red) on the top
  - Player 2 paddle (blue) on the bottom
- Paddle movement controlled via mouse or touch
- "I am ready" button to synchronize game start
- Countdown (3, 2, 1) before the game begins
- Score tracking, first to 5 points wins
- Game reset after each match
- Responsive canvas and UI

---

## Demo

## Installation

1. Clone the repository:

2. Install dependencies:

   ```bash
   npm install
   ```

3. Start the server:

   ```bash
   npm run start
   ```

4. Open your browser at `http://localhost:3000` (open on two devices/browsers to play)

note: in mobile you need to replace the localhost by your PC IP.

---

## How to Play

- Each player opens the game URL on their device.
- Players see a paddle on their screen (red for Player 1, blue for Player 2).
- Move your finger or mouse horizontally to control your paddle.
- Click the **"I am ready"** button when you’re ready to start.
- Once both players are ready, a countdown begins and the game starts.
- First player to reach 5 points wins the match.
- After the game ends, players can click **"I am ready"** to play again.

---

## Project Structure

- `server.js` — Node.js server handling player connections, game logic, and synchronization
- `public/index.html` — Client UI with canvas and controls
- `public/style.css` — Styles for the game UI (optional if styles inline)
- `public/client.js` — Client-side JavaScript for handling rendering and socket events (optional if inline in HTML)

---

## Dependencies

- [Express](https://expressjs.com/) — Web server
- [Socket.IO](https://socket.io/) — Real-time WebSocket communication

---

## License

MIT License © Mauricio Soto
