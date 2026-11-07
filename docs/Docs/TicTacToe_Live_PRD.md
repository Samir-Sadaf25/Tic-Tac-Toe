# TicTacToe Live — Product Requirements Document (PRD)

**Version:** 1.0  
**Status:** Proposed MVP requirements  
**Product:** Simple online multiplayer Tic-Tac-Toe web game  
**Audience:** Student developer, teacher, and AI coding agents

> **Purpose of this document:** Explain **what** the game should do and how we will know it works. Technology choices, architecture, database design, and step-by-step implementation planning will be decided separately.

---

## 1. Product Overview

**TicTacToe Live** is a browser-based game where **two people play Tic-Tac-Toe against each other online** using separate browsers or devices.

One player creates a match. Another player finds the waiting match and joins it. Both see the same 3×3 board, and each move appears for both players without a manual page refresh. The game automatically checks whose turn it is and announces a win or draw.

This is a **small educational project**, not a commercial gaming platform. It should be easy to use, understand, test, and demonstrate in class.

### Why build it?

- **For players:** Play a quick game with another person without creating an account.
- **For the project:** Demonstrate a working real-time multiplayer application.
- **For the course:** Demonstrate responsible **agentic development**—using AI coding agents to help plan, implement, test, review, and improve a small product. The student remains responsible for understanding and verifying the code.

## 2. Goals and Success Criteria

### Product goals

1. Two real people can play together using different browsers or devices.
2. Creating and joining a match is simple and does not require registration.
3. Both players receive game updates automatically.
4. The game follows standard Tic-Tac-Toe rules and rejects invalid moves.
5. A player can refresh or briefly lose the connection and return to the same ongoing match from the same browser.
6. The project stays small enough to build and explain step by step.

### MVP is successful when

- Player A creates a match and Player B sees it in the lobby without refreshing.
- Player B joins; both players see the same board and player details.
- Players take turns, and a valid move appears in both browsers without refreshing.
- Invalid moves are rejected, and the winner or draw is shown correctly.
- Refreshing an ongoing match from the same browser restores its latest saved state.
- A student can demonstrate the complete flow and explain how the implementation was created and tested with AI assistance.

## 3. Users

| User | What they want to do |
|---|---|
| **Match creator (Player A)** | Enter a name, choose X or O, create a match, and wait for someone to join. |
| **Joining player (Player B)** | Enter a name, find an available match, join, and play. |
| **Teacher / demo viewer** | Watch two browsers play together and verify correct behavior and the student's development process. |

**Guest play:** Players enter display names. The MVP does not require sign-up, passwords, or profiles. Display names are **not unique identities**, so multiple people may use the same name.

## 4. MVP Scope

### Included in version 1 (must-have)

- Guest display name for each player.
- Create a match and select X or O.
- Lobby showing available waiting matches.
- Join an available match from the lobby.
- Waiting screen while the second player joins.
- Live 3×3 game board, turn indicator, and player names/symbols.
- Server-checked game rules, win detection, and draw detection.
- Automatic updates for match creation, joining, moves, and results.
- Clear error and connection messages.
- Restore an ongoing match after page refresh or a brief disconnection from the same browser.
- Start a **new** match after finishing (not an automatic rematch).

### Optional after the MVP (nice-to-have)

- Leaderboard showing wins, losses, draws, and total games.
- Shareable match link or short join code.
- Creator cancels a waiting match.
- Automatically expire old waiting matches.
- Play-again request / rematch with the same opponent.
- Basic game history.

### Out of scope

- Computer/AI opponent.
- Account registration, login, password reset, or social login.
- In-game chat, voice, video, spectators, or tournaments.
- Rankings, skill-based matchmaking, and player ratings.
- Payments, advertisements, or mobile apps.
- Complex animations, advanced graphics, or a game engine.

**Scope rule:** The first deliverable is the complete two-player game. Optional features should not delay it.

## 5. Player Experience

### Journey A — Create a game

1. Open the website and see the lobby.
2. Enter a display name.
3. Choose **X** or **O**.
4. Click **Create Match**.
5. See a waiting screen showing the name, chosen symbol, and **Waiting for another player**.
6. When another player joins, automatically see the game begin.
7. Make moves when it is your turn and see the result at the end.
8. Return to the lobby or create a new match.

### Journey B — Join a game

1. Open the website in another browser or device.
2. See the list of available matches.
3. Enter a display name.
4. Click **Join** on a waiting match.
5. Automatically receive the symbol not chosen by the creator.
6. See the game board and whose turn it is.
7. Play until someone wins or the game ends in a draw.
8. Return to the lobby or start a new match.

## 6. Functional Requirements and Acceptance Criteria

### FR-01 — Enter a guest name

**Requirement:** A player can enter a display name before creating or joining a match.

- Names must not be blank or only spaces.
- Names are trimmed and limited to **20 characters**.
- Duplicate names are allowed; a name alone does not prove who a player is.

**Acceptance:** Blank names cannot be submitted; a valid name is shown to both players once the match starts.

### FR-02 — Create a match

**Requirement:** A player can create a new match by choosing X or O.

- The newly created match has status **Waiting**.
- Only its creator is assigned a symbol at this point.
- The creator is taken to a waiting screen.
- The match becomes visible in the lobby automatically.

**Acceptance:** Creating a match in Browser A displays it in an already-open lobby in Browser B without refreshing.

### FR-03 — View available matches

**Requirement:** The lobby lists matches that are waiting for a second player.

- Each item shows the creator's display name and selected symbol.
- A **Join** action is available for eligible matches.
- The list updates when matches are created or joined.
- When there are no available matches, show a friendly empty-state message.

**Acceptance:** A match appears when created and disappears from the available list when joined, without requiring a page refresh.

### FR-04 — Join a match

**Requirement:** A second player can join a waiting match.

- The second player receives the opposite symbol.
- The match changes to **In Progress** when two players are present.
- Both players see the same initial board and both names/symbols.
- The creator cannot join their own match as the opponent.
- No third player can join; simultaneous attempts to take the last slot must not result in three players.

**Acceptance:** Browser B joins Browser A's match; both show the same empty board, with one X and one O. If the creator chooses O, the joining player gets X and moves first.

### FR-05 — Play moves in real time

**Requirement:** A player can click or tap an empty square when it is their turn.

- Only valid moves are accepted.
- Accepted moves appear for both players automatically.
- Both players see whose turn is next.
- The game state is decided by the server, not by a browser.

**Acceptance:** A move made in Browser A appears in Browser B without refreshing, while wrong-turn and occupied-square moves are rejected.

### FR-06 — Finish a match

**Requirement:** The game detects wins and draws and displays the result to both players.

- A win shows the winner's display name and symbol.
- A draw shows a clear **It's a draw** message.
- The game board no longer accepts moves after completion.
- Both players can return to the lobby and start another match.

**Acceptance:** Horizontal, vertical, and diagonal wins and a draw all produce the correct result in both browsers.

### FR-07 — Reconnect and refresh

**Requirement:** A player can return to their ongoing match after refreshing the page or briefly losing their connection.

- The game keeps the latest accepted board and match status.
- The browser attempts to reconnect automatically.
- The player sees the current saved board and turn after returning.
- A disconnection does not automatically award a win or delete the match.
- Rejoining as the same player is supported from the **same browser with its guest session still available**; recovering a session on another device is not promised in the MVP.

**Acceptance:** After a few moves, refreshing Browser A displays the same match, moves, and current turn as Browser B.

### FR-08 — Show helpful feedback

**Requirement:** The interface explains important events and problems in simple language.

Examples:

- **Waiting for another player…**
- **Your turn** / **Opponent's turn**
- **Connecting…** / **Connection lost. Reconnecting…**
- **This match is already full.**
- **That square is already taken.**
- **The game has ended.**

**Acceptance:** An invalid action or temporary disconnection shows a clear message; the game does not silently change to an incorrect state.

## 7. Game Rules

1. Each match has **exactly two player slots**: one X and one O.
2. The board contains **nine squares** arranged in a 3×3 grid.
3. The creator chooses X or O. The joining player automatically gets the other symbol.
4. **X always plays first**, even when the joining player is X.
5. Players alternate turns, placing one symbol in an empty square.
6. A player wins by placing three of their symbols in the same row, column, or diagonal.
7. If all nine squares are filled and nobody has won, the match is a draw.
8. A move is invalid when it is not the player's turn, the square is occupied, the square is outside the board, the match has not started, or the match has ended.
9. Only the two assigned players can make moves in their match.
10. The server verifies every move and decides the updated board, next turn, and outcome. A rejected move must not change the game.
11. After a win or draw, the match is finished and no more moves are accepted.

### Match states

| State | Meaning |
|---|---|
| **Waiting** | Created; one player is waiting for an opponent. |
| **In Progress** | Two players have joined and are playing. |
| **Completed** | Someone won or the game ended in a draw. |

A cancelled or expired state is only needed if those optional features are added later.

## 8. Screens (Simple UI)

| Screen | What it shows |
|---|---|
| **Home / Lobby** | Display-name input, symbol selection for creating a game, **Create Match** button, available matches, and **Join** buttons. |
| **Waiting Screen** | Creator's name and symbol, waiting message, and match details. Automatically transitions when an opponent joins. |
| **Game Screen** | Player names and symbols, 3×3 board, turn status, connection status, result message, and **Back to Lobby / New Match** action. |

**UI guidelines:** Keep the design clean and mobile-friendly. Clearly distinguish X and O, show whose turn it is in text, and make squares easy to tap. Do not require the player to understand technical terms.

## 9. Quality Requirements

- **Real time:** Lobby and game changes should appear quickly in a normal, stable internet connection, without manual refresh. The classroom demo should visibly update both browsers.
- **Correctness:** Invalid or nearly simultaneous actions must not create illegal board states or allow more than two players in a match.
- **Persistence:** Accepted moves and match status survive a page refresh or temporary disconnection; restoring the state must not depend on missed live messages.
- **Security:** Only the appropriate guest player may make moves for their symbol. Never trust a browser's claim about the winner, board, or turn. Keep guest access credentials private.
- **Usability:** Clear messages, readable layout, and basic keyboard/touch support on modern desktop and mobile browsers.
- **Testing:** Automated tests cover all winning lines, draws, turn order, occupied squares, invalid squares, late moves, and key join/move behaviors.
- **Maintainability:** Keep the solution understandable to a student and avoid features or complexity not needed for the demo.

## 10. Demo / Final Acceptance Checklist

The project is ready for the MVP classroom demonstration when all the following can be shown:

- [ ] Open the lobby in two separate browsers or devices.
- [ ] Create a match as **Player A** with X.
- [ ] See the waiting match appear in Browser B automatically.
- [ ] Join as **Player B** and receive O.
- [ ] Confirm the board, player names, and turn match in both browsers.
- [ ] Play valid moves and see them update automatically in both browsers.
- [ ] Try an invalid move and confirm it is rejected.
- [ ] Refresh one browser during the match and confirm it restores the saved board and turn.
- [ ] Finish a match and see the same win or draw result on both screens.
- [ ] Show relevant automated test results.
- [ ] Explain which tasks were supported by AI agents, what code was reviewed, and how correctness was verified.

## 11. Assumptions and Limitations

- One game contains two **human** players; each joins from a separate browser or browser session.
- A guest name is for display only; it is not a secure account.
- The MVP is meant for a small classroom demonstration, not thousands of simultaneous users.
- Losing a connection does not cause an automatic forfeit; abandoned matches may remain until optional cleanup is implemented.
- A new match starts on a new, empty board. Ongoing-match history and automated rematches are not required.
- Leaderboard statistics are deliberately **not part of MVP**. They can be added later, with a clear decision about how guest players are identified.
