# AGENTS.md

AI-assisted development contract for the AOVIAS repository.

`main` is the protected integration branch.

Agents must never directly develop on `main`.

---

## Purpose

Enable parallel Cursor and Claude development without colliding on files, branches, or `main`.

---

## Main

- **Never work directly on `main`.**
- Do not commit to `main`.
- Do not force-push `main`.
- Do not reset `main`.
- Integration into `main` is a human/integration responsibility.

---

## Agent branches

Use short-lived task branches only:

```text
agent/cursor/<task-slug>
agent/claude/<task-slug>
```

Rules:

- One task per branch
- Create from up-to-date `main`
- Delete after successful merge (human may delete)
- Never commit on another agent's branch prefix

---

## Worktree ownership

Physical model (future):

```text
V:\DevelopmentRepo\AOVIAS          → main / integration
V:\DevelopmentRepo\AOVIAS.Cursor   → agent/cursor/<task>
V:\DevelopmentRepo\AOVIAS.Claude   → agent/claude/<task>
```

- Cursor operates **only** in `AOVIAS.Cursor`
- Claude operates **only** in `AOVIAS.Claude`
- Do not modify another agent's worktree
- Do not share a dirty working tree across agents

---

## Commits

- Commit completed logical units of work
- Use clear conventional messages (`feat:`, `fix:`, `docs:`, `chore:`, `test:`)
- Never commit secrets, keys, `.env`, certificates, or local credentials
- Do not amend published history without explicit human authorization

---

## Tests

Before reporting completion, run relevant verification:

- `scripts/verify-build.sh` / platform build entry points when touching .NET
- Angular/unit tests when touching `WebApp`
- Targeted tests for the changed area

If tests cannot run, state why in the completion report.

---

## Git safety

Agents must **not**:

- `git reset --hard` on shared branches
- `git clean -fdx` without explicit authorization
- force-push `main` or rewrite `main` history
- delete another agent's branch
- modify remotes
- prune/remove worktrees they do not own
- run destructive Git operations without explicit human authorization

---

## Scope control

- Stay within the assigned task
- No drive-by refactors outside task scope
- Stop and report if secrets or production credentials are discovered in the tree

---

## Completion report

Every agent must report:

```text
Branch:
Commit SHA:
Commit message:
Files changed:
Tests executed:
Known issues:
```

---

## Conflict handling

If conflicts with `main` or another agent appear:

1. Stop
2. Report the conflict
3. Do not invent cross-agent merges without human direction
