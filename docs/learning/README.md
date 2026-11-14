# Learning reports

One report per step: my course notes for learning game development. New terms go in [GLOSSARY.md](GLOSSARY.md).

| Step | Report | In one line |
|---|---|---|
| 0 | [Project setup & tooling](step-00-setup.md) | `aspire run` starts the API and the client, and a Phaser canvas shows the API's message. |
| 1 | [Game state model](step-01-game-state.md) | The whole game is immutable C# data, loaded and validated from JSON files you edit. |
| 2 | [Movement & pathfinding](step-02-movement.md) | Units move: a BFS finds every reachable tile, and the engine accepts or rejects each move with a code. |
| 3 | [Attack & heal](step-03-attack-heal.md) | Units hit enemies and heal allies in range, a unit at 0 HP leaves the board, and every formula lives in `Combat.cs`. |
| 4 | [Turns, victory & replay](step-04-turns-victory-replay.md) | Turns alternate, the last side with units wins, and `Replay` rebuilds any game from its action log. |
| 5 | [Game server v1](step-05-game-server.md) | Two players with tokens create, join and play a whole game over HTTP; the server judges every action with `Tactics.Core`. |
| 6 | [Phaser 101: draw the board](step-06-draw-the-board.md) | `/?mode=local` starts a real game, and the browser draws the server's board and units, HP bars included, kept in sync by unit id. |
| 7 | [Input: select, move, act, end turn](step-07-input.md) | A whole local game plays with mouse and keyboard: an input state machine picks actions from `availableActions`, and the server judges them. |
