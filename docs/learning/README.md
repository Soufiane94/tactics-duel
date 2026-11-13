# Learning reports

One report per step: my course notes for learning game development. New terms go in [GLOSSARY.md](GLOSSARY.md).

| Step | Report | In one line |
|---|---|---|
| 0 | [Project setup & tooling](step-00-setup.md) | `aspire run` starts the API and the client, and a Phaser canvas shows the API's message. |
| 1 | [Game state model](step-01-game-state.md) | The whole game is immutable C# data, loaded and validated from JSON files you edit. |
| 2 | [Movement & pathfinding](step-02-movement.md) | Units move: a BFS finds every reachable tile, and the engine accepts or rejects each move with a code. |
| 3 | [Attack & heal](step-03-attack-heal.md) | Units hit enemies and heal allies in range, a unit at 0 HP leaves the board, and every formula lives in `Combat.cs`. |
| 4 | [Turns, victory & replay](step-04-turns-victory-replay.md) | Turns alternate, the last side with units wins, and `Replay` rebuilds any game from its action log. |
