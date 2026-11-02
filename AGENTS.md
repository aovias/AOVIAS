# AOVIAS Agent Operating Contract

Agents work independently. `main` remains stable. Every meaningful change is committed. Integration is deliberate and reviewable.

**Two-worktree principle:** We maintain exactly two AI development worktrees — one for Cursor and one for Claude. Parallelism is achieved through isolated branches and worktrees, not through multiple agents sharing the same filesystem.

**Completion rule:** An agent's work is not considered complete until it exists in a Git commit.

**Canonical Git workflow:** Development stays in the Cursor or Claude worktree. Reviewed work is integrated on canonical `main` in `V:\DevelopmentRepo\AOVIAS`. Only that `main` branch is pushed to GitHub. Do not publish agent branches. Nested repositories (AccredaOne) keep their own history and are not committed into the parent tree. Full rules: [GIT-WORKFLOW.md](GIT-WORKFLOW.md).

---

## Repository Structure

```text
V:\DevelopmentRepo\
├── AOVIAS\                 → main (human integration)
├── AOVIAS.Cursor\          → Cursor agent worktree
└── AOVIAS.Claude\          → Claude agent worktree
```

Do not create additional permanent worktrees (`AOVIAS.Integration`, `AOVIAS.Dev`, etc.).

---

## Worktree Ownership

| Owner  | Path                         | Role                                      |
| ------ | ---------------------------- | ----------------------------------------- |
| Human  | `V:\DevelopmentRepo\AOVIAS`  | Integration, review, merge, validation    |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | Cursor implementation only          |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | Claude implementation only          |

- An agent must **never** intentionally modify another agent's worktree.
- An agent must **never** use `V:\DevelopmentRepo\AOVIAS` as its working directory for development.
- Do not share a dirty working tree across agents.

---

## Branch Model

Long-lived:

```text
main
agent/cursor/active
agent/claude/active
```

Task branches (short-lived):

```text
agent/cursor/<task-slug>
agent/claude/<task-slug>
```

Naming rules:

- lowercase, hyphen-separated
- short but meaningful
- no spaces
- no generic names (`work`, `test`, `changes`)
- identify the task

Examples (do not create unless assigned): `agent/cursor/oe-ownership`, `agent/claude/runtime-agent`.

---

## Task Lifecycle

```text
1. Identify task
2. Verify correct agent
3. Verify worktree clean
4. Create/activate task branch from up-to-date main
5. Confirm branch
6. Implement
7. Test
8. Review diff
9. Commit
10. Report commit SHA
11. Human reviews
12. Merge to main (human, from AOVIAS)
13. Validate main
14. Return agent worktree to active baseline
```

Task-start report (required before work):

```text
Agent:
Worktree:
Current branch:
Task branch:
HEAD:
Working tree:
```

Do not start product tasks without an explicit assignment. Do not invent task branches during governance-only work.

---

## Main Branch Rules

`main` is the stable integration source of truth.

Agents MUST NOT:

- check out `main` for development
- commit directly to `main`
- reset `main`
- rebase `main` destructively
- force-push `main`
- delete `main`

Before human integration: working tree clean, tests pass, commit exists, diff reviewed.

---

## Commit Rules

Commit logical units of work using conventional style where appropriate:

```text
feat:  fix:  refactor:  test:  docs:  chore:  build:  ci:  perf:
```

Avoid vague messages: `fix`, `changes`, `updates`, `stuff`, `test`, `final`.

Before commit, verify:

```text
git status
git diff
git diff --cached
```

Then run appropriate tests. Do not commit secrets, credentials, local configuration, generated junk, unrelated changes, temporary debug code, or another task's files.

---

## Testing Rules

Before reporting completion, run relevant verification:

- .NET: platform / service build entry points when touching backend
- Angular/unit tests when touching `WebApp`
- Targeted tests for the changed area

If tests cannot run, state why in the completion report. Do not claim green without evidence.

---

## Dirty Worktree Protection

An agent must **never** silently discard dirty work.

Forbidden without explicit authorization for that exact situation:

```text
git reset --hard
git clean -fd
git checkout -- .
git restore .
```

If unexpected modifications exist: **STOP → INSPECT → REPORT**. Do not assume they belong to the agent.

---

## Security Rules

Never commit:

```text
*.pfx  *.p12  *.key  *.pem
private certificates, credentials, tokens, passwords
.env / secret configuration
```

If secret material is discovered: **STOP**. Report path, classification, and action required. Do **not** print secret contents.

---

## Conflict Rules

Do not blindly resolve merge conflicts.

1. Identify conflicting files and commits
2. Identify ownership and intent
3. If both agents' active work is involved: **STOP → REPORT → HUMAN DECISION**

One agent must never silently overwrite another's work.

---

## Completion Report

Every completed task must produce:

```text
TASK COMPLETE

Agent:
Worktree:
Branch:

Commit:
Commit SHA:

Summary:
- ...

Files changed:
- ...

Tests:
- ...

Build:
- ...

Known issues:
- ...

Ready for human review:
YES
```

Do not claim completion without a commit SHA.

---

## Human Integration

Human works in `V:\DevelopmentRepo\AOVIAS` on `main` (clean).

Before merge:

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" log --oneline main..<agent-branch>
git -C "V:\DevelopmentRepo\AOVIAS" diff main...<agent-branch>
```

Prefer visible integration points:

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" merge --no-ff <agent-branch>
```

Traceability over history aesthetics. Squash is not required.

After merge: build/tests/validation on `main`. Preserve task branches until validation completes; then optionally delete.

Return agent worktree to `agent/<agent>/active` only after: task merged, worktree clean, commits preserved, human confirms. Never destructive-reset an unmerged task.

---

## Forbidden Git Operations

Agents must not:

- force-push `main` or rewrite `main` history
- delete another agent's branch
- modify remotes without authorization
- prune/remove worktrees they do not own
- amend published history without explicit human authorization
- run destructive Git operations without explicit human authorization
- develop or commit on `main`

Use read-only helpers when available:

```text
scripts/dev/worktree-status.ps1
scripts/dev/agent-preflight.ps1
scripts/dev/agent-completion-check.ps1
```

---

## Production Task Ownership

Every production development task must have **one primary owner**: Cursor, Claude, or Human.

- No task may be simultaneously assigned to Cursor and Claude unless explicitly designed as a coordinated task.
- Either agent may receive any task when explicitly assigned; capability guidance below is preference, not a hard restriction.

Typical strengths (capabilities, not restrictions):

| Agent  | Often well suited for                                      |
| ------ | ----------------------------------------------------------- |
| Cursor | Implementation-heavy UI/backend, refactoring, tests         |
| Claude | Architecture analysis, diagnostics, complex reasoning, infra |
| Human  | Integration, merges, product decisions, conflict arbitration |

Task assignment should use `docs/development/AGENT-TASK-TEMPLATE.md` (Task ID, objective, primary agent, worktree, branch, scope, non-goals, validation, completion criteria).

---

## Parallel Tasks And Shared Files

Cursor and Claude may work simultaneously when tasks are independently scoped and overlap is low.

If both tasks must touch the same important files/subsystem:

1. **Serialize** the tasks, or
2. Define **explicit ownership boundaries**, or
3. One agent **implements** while the other **reviews**

Do not assign two agents to independently redesign the same area and hope Git resolves it. Conflict resolution is a fallback, not the strategy.

Task branches must start from the **latest validated `main`**, not from obsolete task branches. Physical worktrees stay fixed (`AOVIAS.Cursor` / `AOVIAS.Claude`); checked-out branch may temporarily be a task branch, then return to `active`.

---

## Abandoned And Interrupted Work

### Abandoned task

Do not delete the branch immediately. Record:

```text
Task:
Agent:
Branch:
Last commit:
Reason abandoned:
Work preserved:
```

Then decide: resume / archive / merge partial work / discard. **Discard requires explicit human approval.**

### Interrupted agent

If Cursor or Claude stops unexpectedly: **do not clean the worktree**. Inspect `git status`, current branch, `git log`, and `git diff`. Assume dirty work may be valuable. Never use `git reset --hard` or `git clean -fd` as a first response.

---

## Do Not Over-Automate

Remain human-controlled:

```text
merge
branch deletion
destructive reset
worktree cleanup
remote configuration
push
history rewriting
```

Automation should primarily provide inspection, validation, preflight, and reporting.

---

## Production Workflow

Canonical production loop:

```text
1. Roadmap identifies task
2. Human assigns agent
3. Agent preflight
4. Task branch created from main
5. Agent implements
6. Agent tests
7. Agent reviews diff
8. Agent commits
9. Agent reports commit
10. Human reviews
11. Human merges (--no-ff preferred)
12. Main validated
13. Agent worktree returned to active
14. Next task assigned
```

No meaningful development work should be performed without Git branch isolation and a reviewable commit.
