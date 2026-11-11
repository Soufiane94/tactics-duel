---
name: next-step
description: Start the next step of PLAN.md using the step workflow from CLAUDE.md (brief, wait for ok, build, verify, learning report, stop). Only when the user types /next-step.
disable-model-invocation: true
argument-hint: "[step number]"
---

Run the **Step workflow** from CLAUDE.md.

1. Read `PLAN.md` (Progress list, the step's section, Contracts) and `docs/learning/README.md` (if it exists) to see what's already done.
2. The step to run is: $ARGUMENTS
   - If that's empty, take the first unchecked step in the Progress list.
   - If the previous step isn't ticked, tell me and stop.
3. Do **Phase 1 only**: post the step brief, then wait for my `ok` before changing any files.
