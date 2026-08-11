# Agent Task

## Task ID

<!-- Short slug, e.g. worktree-status-improvement -->

## Task objective

<!-- One paragraph: what success looks like -->

## Agent

<!-- Cursor | Claude -->

## Worktree

```text
V:\DevelopmentRepo\AOVIAS.Cursor
# or
V:\DevelopmentRepo\AOVIAS.Claude
```

## Task branch

```text
agent/cursor/<task-slug>
# or
agent/claude/<task-slug>
```

## Scope

<!-- What is in scope for this task -->

## Explicit non-goals

<!-- What must not be changed -->

-

## Files / areas allowed

-

## Dependencies

<!-- Other tasks, APIs, or human decisions required first -->

-

## Validation requirements

-

## Completion criteria

-

## Commit requirement

```text
Style: feat: | fix: | refactor: | test: | docs: | chore:
Message:
Commit SHA: (fill after commit)
Working tree after commit: CLEAN
```

Work is not complete until a commit SHA exists.

## Completion report

```text
TASK COMPLETE

Agent:
Worktree:
Branch:

Commit:
Commit SHA:

Summary:
-

Files changed:
-

Tests:
-

Build:
-

Known issues:
-

Ready for human review:
YES
```

## Known issues

-

## Human review

```text
Reviewer:
Diff reviewed: YES/NO
Merge decision: APPROVE / REQUEST CHANGES
Merge command (human, on main):
  git merge --no-ff <task-branch>
```
