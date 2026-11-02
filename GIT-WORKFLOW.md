# AOVIAS Git workflow

This is the permanent operating model for commits, merges, and GitHub publication.

**Direction:** Cursor or Claude development worktree → canonical Main → GitHub remote.

Agents (Cursor and Claude) and human developers follow this document. Task-branch rules in [AGENTS.md](AGENTS.md) still apply. This file adds the publication boundary those rules do not replace.

## Repository map

| Role | Path | Publishes to GitHub? |
| --- | --- | --- |
| Canonical parent repository | `V:\DevelopmentRepo\AOVIAS` | **Yes — `main` only** |
| Cursor development worktree | `V:\DevelopmentRepo\AOVIAS.Cursor` | No |
| Claude development worktree | `V:\DevelopmentRepo\AOVIAS.Claude` | No |
| QA workspace | `V:\DevelopmentRepo\Aovias.QA` | No (not a publication source) |

The three parent paths above are Git worktrees of **one** repository. `origin` is configured only in the canonical Main worktree.

Read-only checks (they do not commit, merge, push, or clean):

```powershell
powershell -NoProfile -File V:\DevelopmentRepo\AOVIAS\scripts\dev\worktree-status.ps1
powershell -NoProfile -File V:\DevelopmentRepo\AOVIAS\scripts\dev\agent-preflight.ps1 -Agent cursor
powershell -NoProfile -File V:\DevelopmentRepo\AOVIAS\scripts\dev\agent-preflight.ps1 -Agent claude
```

---

## Rule A — Canonical repository

`V:\DevelopmentRepo\AOVIAS` is the canonical parent repository.

Its `main` branch is the authoritative integration branch and the only branch published to GitHub.

Development worktrees must not be treated as a second source of truth.

## Rule B — Development worktrees

Cursor and Claude worktrees are development environments, not publication destinations.

They may create feature branches and commits. Completed work is reviewed and integrated into canonical `main` before anything is pushed.

Do not configure `origin` on `AOVIAS.Cursor` or `AOVIAS.Claude` for publishing development branches.

Do not push `agent/cursor/*`, `agent/claude/*`, or other task branches to GitHub.

## Rule C — Commit procedure

Before every commit:

1. Confirm the repository root (`git rev-parse --show-toplevel`) and the current branch.
2. Inspect staged and unstaged changes (`git status`, `git diff`, `git diff --cached`).
3. Reject secrets (`.env`, credentials, tokens, private keys, certificates) and generated outputs (`node_modules`, `.next`, `bin`, `obj`, coverage, build logs).
4. Run the build and tests that apply to the change.
5. Run `git diff --cached --check`.
6. Review the exact staged paths. Do not stage unrelated work.
7. Use a meaningful commit message (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:` when appropriate).

Commits stay in the repository that owns the files. Do not add a nested repository's files into the parent index.

Agents commit on task branches in their own worktree. Integration commits on `main` happen in the canonical Main worktree after review.

## Rule D — Merge procedure

Before every merge into `main`:

1. Name the source branch and the target (`main`).
2. Confirm merge-base and divergence (`git merge-base`, `git log main..<source>`, `git log <source>..main`).
3. Review source commits and the file list (`git diff main...<source>`).
4. Confirm tests and build status for that change.
5. Leave unrelated local and staged changes untouched. Do not `git add -A` across a dirty tree.
6. Use a normal merge (`git merge --no-ff <source>`) unless a different strategy was explicitly approved.
7. Verify `main` after the merge (HEAD, status, and the checks that apply).

If Git reports "Already up to date", do not invent an empty merge.

Only reviewed, completed work enters `main`. Stop on conflicts whose intended result is not obvious.

## Rule C and D — Shared index warning

Worktrees share commits but **not** a license to sweep the index. A staged change that is not part of the task (including a preserved deletion) must not ride along in the next commit. Commit with an explicit path list when the index already holds unrelated staged paths:

```powershell
git add -- path\to\intended\file
git diff --cached --check -- path\to\intended\file
git commit -m "message" -- path\to\intended\file
```

## Rule E — Push procedure

Before every push:

1. Confirm the repository is canonical Main: `V:\DevelopmentRepo\AOVIAS`.
2. Confirm `origin` URL and owner. The approved parent remote is `https://github.com/aovias/AOVIAS.git` (GitHub account `aovias`, repository `AOVIAS`).
3. Confirm the branch to push is `main`.
4. Compare local and remote history. Do not push if the remote has unrelated initialized history (for example a generated README on an otherwise empty repo) until that is resolved without rewriting local history.
5. Confirm the commits being published contain no secrets or unintended files.
6. Push only `main`:

```powershell
git -C V:\DevelopmentRepo\AOVIAS push origin main
```

7. Verify remote HEAD matches local `main` (`git ls-remote origin refs/heads/main`).

Never force-push without explicit Product Owner authorization.

Never push from a development worktree as a shortcut.

## Rule F — Nested repositories

Each nested repository has its own Git history, branches, commits, remotes, and publication policy.

**AccredaOne** is a nested repository. Its active Git directory lives under the Cursor worktree:

`V:\DevelopmentRepo\AOVIAS.Cursor\AOVIAS.AccredaOne`

Parent operations must not:

- commit that tree into the parent repository
- collapse it with subtree or submodule migration unless the Product Owner authorizes that design
- publish `A1-ProjectSetup` (or any AccredaOne branch) as a substitute for parent `main`

Local parent exclude (not a substitute for a tracked ignore rule): `.git/info/exclude` contains `/AOVIAS.AccredaOne/`.

The canonical Main folder `V:\DevelopmentRepo\AOVIAS\AOVIAS.AccredaOne` is not the nested repository until it has its own `.git`. Do not copy or relocate that repository without Product Owner authorization.

AccredaOne's own `main` is its canonical branch. Giving it a GitHub remote is a separate decision from publishing parent `AOVIAS`.

## Rule G — No implicit authorization

When a task is already authorized, normal commits on task branches and reviewed merges to `main` follow this workflow.

These always need explicit Product Owner authorization:

- Deleting a repository
- Force push
- History rewrite
- Changing canonical repository ownership
- Changing remote ownership
- Making a repository public
- Removing or restructuring nested repositories
- Destructive cleanup, or deleting branches or worktrees

## Rule H — Preflight checklist

Use this list before integration or publication. Prefer the existing read-only scripts in Rule B's command block. Do not add automation that merges, pushes, or cleans the tree.

**Identity**

- [ ] Repository root is the one you intend
- [ ] Branch is the one you intend (`main` only when integrating or pushing)
- [ ] `git status` reviewed; unrelated staged and untracked files left alone

**Safety**

- [ ] No secrets or generated artifacts in the staged diff
- [ ] `git diff --cached --check` is clean for the paths being committed
- [ ] Nested AccredaOne paths are not in the parent commit

**Integration**

- [ ] Source branch reviewed; divergence understood
- [ ] Applicable build and tests recorded
- [ ] `main` HEAD recorded after the merge or commit

**Publication**

- [ ] Remote is `AOVIAS/AOVIAS`, private, default branch `main`
- [ ] Push is `git push origin main` from canonical Main only
- [ ] Remote `main` SHA matches local `main`

## Current publication target

| Item | Value |
| --- | --- |
| Owner | GitHub account `aovias` |
| Repository | `AOVIAS` |
| Visibility | Private |
| Default branch | `main` |
| Remote (canonical Main only) | `https://github.com/aovias/AOVIAS.git` |

Do not publish agent branches. Do not initialize the remote with a generated README, license, or `.gitignore` that conflicts with existing `main` history.
