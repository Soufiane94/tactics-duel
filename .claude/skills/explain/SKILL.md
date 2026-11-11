---
name: explain
description: Explain a file, function or game-dev concept from this project in simple bullet points, for a .NET developer new to game development. Only when the user types /explain.
disable-model-invocation: true
argument-hint: "<file, function or concept>"
---

Explain this to me: $ARGUMENTS

I'm a .NET developer (C#, ASP.NET Core, Angular/TypeScript) who is new to game development.

- If it's code, read the real code first. Use real file and function names in backticks.
- Use this format, with bullets, short sentences and plain English:
  - **In one sentence**
  - **Main functions** (if it's code): `name` — `file` — what it does · why it exists
  - **How the logic flows**: numbered steps from trigger to result
  - **Like in .NET/Angular**: one analogy
  - **Words to know**: each new term in one line
- Keep it under 40 lines. Don't change any files.
- If it's a concept the project doesn't use yet, say which step of `PLAN.md` introduces it.
