# Rollback for task 0001

If `verify.sh` failed, do this:

1. Inspect the failure output and confirm whether it is recoverable.
2. If unrecoverable: `git restore --staged --worktree .` to undo all changes from this task.
3. Do NOT mark this taskId as applied in `.app-template-version.json`.
4. Surface the failure to the user with the exact command output.

Task-specific rollback overrides go below this line.

- This task only creates `.app-template-version.json`. If it was created by this run and
  verification failed, delete it: `rm -f .app-template-version.json`. Never delete a marker
  that already existed before the task ran — it carries the repo's `appliedTasks` history.
- A build failure here is almost never caused by this task (it writes no code). Fix the
  build first, then re-run `bash .ai/tasks/0001-baseline/verify.sh`.
