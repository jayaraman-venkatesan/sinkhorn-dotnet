# Project issue tracker

Implementation tickets live in [sinkhorn-dotnet](https://github.com/jayaraman-venkatesan/sinkhorn-dotnet/issues).
The approved plan IDs are preserved in issue titles. The central
[research-to-repo idea](https://github.com/jayaraman-venkatesan/research-to-repo/issues/2)
holds lifecycle approvals and recovery links; it is not the application ticket host.

Before working a ticket, read it and its native blockers, verify all prerequisites
are met, and assign it to the driving developer. Use GitHub Issues through gh:

```sh
gh issue view ISSUE_NUMBER --repo jayaraman-venkatesan/sinkhorn-dotnet --json body,state,assignees
gh api repos/jayaraman-venkatesan/sinkhorn-dotnet/issues/ISSUE_NUMBER/dependencies/blocked_by
```

Replace ISSUE_NUMBER with the verified ticket number. Resolve REST IDs with
`gh api repos/jayaraman-venkatesan/sinkhorn-dotnet/issues/ISSUE_NUMBER` before adding
native dependencies; repository issue numbers are not REST IDs.

Record task test and review evidence in a resolution comment and close only
after acceptance is met. Read back mutations, reconcile uncertain outcomes
before retrying, and preserve unfinished work. A closed implementation ticket
does not imply an approved merge or shipped project.

For code-review, use the task's originating issue and the approved specification
at `docs/superpowers/specs/2026-09-19-sinkhorn-learning-design.md`; the controller
supplies the fixed base commit for each task and final branch review.
All work stays on isolated feature branches. Human approval precedes PR creation,
merge, release, public deployment and consequential scope/dependency changes.
