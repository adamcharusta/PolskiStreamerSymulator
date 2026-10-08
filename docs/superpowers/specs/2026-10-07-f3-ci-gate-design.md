# F3 design: CI build and test gate

| Field | Value |
| --- | --- |
| Work package | F3 in the [delivery plan](../../delivery-plan.md) |
| Date | 2026-10-07 |
| Status | Approved by the creator on 2026-10-07. |
| Authoritative documents | [Testing strategy](../../testing-strategy.md), [deployment](../../deployment.md), [decisions](../../decisions.md) |

This spec records how F3 will be built. The documents above stay authoritative for rules and contracts; this file does not replace them.

## Goal

Every push to GitHub gets an automatic verdict on whether the commit builds, passes its tests, keeps the code format, and still produces a container that starts. F3 is done when:

1. `.github/workflows/ci.yml` runs on a push to any branch and on a pull request into `main`.
2. A clean checkout restores, passes the format check, builds in Release with warnings treated as errors, and passes every test, including the F1 health-endpoint tests.
3. The same run builds the production container image from `deploy/Dockerfile`, starts it, and gets status 200 from `/health/live`, `/health/ready`, and the client page at `/`.
4. The first run on GitHub, for the pushed feature branch, is green.

## Inputs

Stated in the delivery plan and earlier work packages:

- F3 in the delivery plan: "Add a CI build/test gate and the first Server integration test for the health endpoint; add other test projects alongside the behavior they verify." Acceptance: a clean checkout builds and runs the health test, and the test-project layout follows `testing-strategy.md` without empty placeholder projects.
- F1 already wrote the health-endpoint integration tests in `tests/Server.IntegrationTests`, so F3 owes only the CI gate.
- D-60: tests run on Microsoft Testing Platform v2, and `dotnet` commands run from `PolskiStreamerSymulatorApp/` so that `global.json` applies.
- `Directory.Build.props` treats warnings as errors. `dotnet format --verify-no-changes` passes on `main` today.
- `deploy.yml` pins `actions/checkout` and `actions/setup-dotnet` by commit SHA and builds the image with `docker buildx build --platform linux/amd64 --file deploy/Dockerfile`.
- The repository is public, so GitHub-hosted standard runners cost no Actions minutes. `main` has no branch protection.

Chosen by the creator during brainstorming on 2026-10-07:

- Triggers: a push to any branch, and a pull request into `main`.
- Checks: build and tests, the format check, and a parallel container job.
- `deploy.yml` stays unchanged.
- Verification: the feature branch is pushed to `origin`, and its run is watched with `gh run watch`. Pushing `main` stays with the creator.

## Non-goals

These are out of scope for F3:

- Changes to `deploy.yml`, including its obsolete "Require deployable server project" guard.
- Branch protection or required status checks on `main`. The creator merges locally and pushes `main` directly. A required check would reject that push, because the new commit has no check result yet.
- A NuGet cache. The repository has no `packages.lock.json`, which `actions/setup-dotnet` needs for caching.
- Code coverage reporting, test-result artifacts, and a CI status badge.
- An operating-system matrix. The image and the deployment target are Linux; `.gitattributes` keeps line endings stable on Windows checkouts.
- New test projects. No new behavior needs one.
- Skipping documentation-only commits with a paths filter. Runs are free, and a filter would leave some commits without a result.

## Approaches considered

1. **A separate `ci.yml`; `deploy.yml` unchanged (chosen).** Both workflows check out the code and install .NET. Production deployment still tests the exact commit it ships, and the deploy pipeline, which has never run against a real server, stays untouched.
2. **A reusable `ci.yml` that `deploy.yml` calls through `workflow_call`.** There is one gate definition. But a deployment would build the image twice, once without pushing it and once with pushing it, and the untested deploy pipeline would change.
3. **Option 2, plus removing the obsolete guard from `deploy.yml`.** Same trade-offs as option 2, with a larger change to the deploy pipeline.

## Design

### Workflow `.github/workflows/ci.yml`

- **Name:** `CI`.
- **Triggers:** `push` with `branches: ['**']`, which excludes tag pushes, and `pull_request` with `branches: [main]`. A pull request from a branch in this repository therefore runs twice, once for the push and once for the pull request. That duplicate is accepted.
- **Permissions:** `contents: read` at the workflow level. The workflow uses no secrets and no environment.
- **Concurrency:** group `ci-${{ github.ref }}` with `cancel-in-progress: true`. A newer push to the same branch cancels the older run.
- **Jobs:** two jobs with no dependency between them, so they run in parallel. Each runs on `ubuntu-latest` with `timeout-minutes: 15`.
- **Action pins:** `actions/checkout` and `actions/setup-dotnet` use the same commit SHAs and version comments as `deploy.yml`. Checkout sets `persist-credentials: false`.

### Job `build-and-test` ("Build and test")

Steps:

1. Check out the commit.
2. Install .NET with `global-json-file: PolskiStreamerSymulatorApp/global.json`.
3. Run these steps with `working-directory: PolskiStreamerSymulatorApp`:
   1. `dotnet restore PolskiStreamerSymulatorApp.sln`
   2. `dotnet format PolskiStreamerSymulatorApp.sln --verify-no-changes --no-restore`
   3. `dotnet build PolskiStreamerSymulatorApp.sln -c Release --no-restore`
   4. `dotnet test --solution PolskiStreamerSymulatorApp.sln -c Release --no-build`

The format check runs before the build so that a format-only failure is reported quickly. The test step runs every test project in the solution: `Domain.Tests` and `Server.IntegrationTests`, the latter holding the F1 health-endpoint tests. A test project added later joins the gate automatically.

### Job `container` ("Container image")

Steps:

1. Check out the commit.
2. Build the image without pushing it:

   ```
   docker buildx build --platform linux/amd64 --file deploy/Dockerfile --tag pss-ci:<sha> --load .
   ```

   This matches the deploy workflow's build command apart from publishing.
3. Start the container in the background: `docker run -d --name pss-ci -p 8080:8080 pss-ci:<sha>`.
4. Poll `http://localhost:8080/health/live` with `curl --fail` for up to 30 seconds, until it returns 200.
5. Then require status 200 from:
   - `/health/ready`;
   - `/`, whose body must contain `<div id="app">`, the same marker the F1 client-hosting test checks.
6. If any step fails, print `docker logs pss-ci`.

The Release publish inside the image build also trims the Blazor client. A trimming warning is an error under `Directory.Build.props`, so this job catches trimming regressions too.

Why the job starts the container instead of only building it: F1's entrypoint bug named the wrong DLL. That image built successfully and failed only at startup.

## Testing and verification

A workflow cannot be developed test-first the way code can. Verification therefore has four layers:

1. **Static check.** Run the released `actionlint` binary, version 1.7.12 or later, against `.github/workflows/ci.yml` and `deploy.yml`, and expect no findings.
2. **Clean-clone rehearsal.** Clone the branch into a temporary folder and run the `build-and-test` steps from it in order. Then run the `container` steps, including the health and client-page checks, with local Docker. Every step must succeed.
3. **Local failure rehearsal.** In the temporary clone only:
   - the client-page check, run against `/health/live`, must fail;
   - an image rebuilt with F1's wrong `ENTRYPOINT` must build but fail the liveness wait.

   The format, build, and test steps fail through their non-zero exit codes. The creator chose not to push a deliberately broken commit.
4. **First GitHub run.** Push `feature/f3-ci-gate` to `origin` and watch the run with `gh run watch`. Both jobs must be green. If the runner warns about the Node.js runtime of the pinned actions, record that as a follow-up for both workflows; do not fix it here.

## Documentation updates

- **`docs/testing-strategy.md`:** a short CI paragraph under "Test platform and commands": what `ci.yml` runs, and when.
- **`docs/deployment.md`:**
  - a row for `.github/workflows/ci.yml` in the repository layout table;
  - a note under the application readiness gates that CI already builds and starts the production image on every push. Pulling the image from GHCR on the VPS remains open.
- **`docs/decisions.md`:** a new decision, D-63, with status Confirmed:
  - CI runs on pushes to any branch and on pull requests into `main`;
  - it checks format, Release build, tests, and the container's startup;
  - `deploy.yml` keeps its own test step;
  - `main` has no required status checks while the creator merges locally and pushes directly.
- **`docs/delivery-plan.md`:** the progress note records F3 as complete.

## Risks

- **First real run of the pinned actions.** `deploy.yml` has never run, so `ci.yml` is the first workflow to use these action pins and the `global-json-file` SDK install on GitHub. The first GitHub run is the check. If an action fails, update its pin in `ci.yml` and record the follow-up for `deploy.yml`.
- **SDK version drift.** `global.json` pins 10.0.201 with `rollForward: latestFeature`. `actions/setup-dotnet` installs the matching SDK, so the runner's preinstalled SDKs do not matter. A newer feature band can bring new analyzer warnings. If one fails the build, it gets fixed as an ordinary code change.
- **Format check across platforms.** `dotnet format` on Linux could disagree with a Windows checkout if line endings drift. `.gitattributes` enforces LF on checkout, which limits the risk. The clean-clone rehearsal runs on Windows, so the first GitHub run is the first Linux check. A disagreement found there is fixed in code, not by weakening the check.
- **Container startup time.** The 30-second poll is generous for this Server today. Later packages that add database startup work may need a longer wait. Raise the limit then, rather than removing the check.
