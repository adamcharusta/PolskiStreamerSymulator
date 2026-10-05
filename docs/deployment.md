# Deployment: single VPS, k3s, and manual GitHub Actions

## Status and target

The creator chose a VPS running Ubuntu 24.04 with 4 vCPU, 8 GB RAM, and 75 GB disk, the domain `polskistreamersymulator.pl`, Ansible provisioning, Kubernetes on that VPS, and a manually triggered GitHub Actions deployment. This is a **deployment design and infrastructure scaffold**, not a deployed service. The current solution has no `Server` project, container image, health endpoints, or event catalogue implementation yet.

Use a single-node k3s cluster. One ASP.NET Core `Server` container will serve the public Blazor game and API at `https://polskistreamersymulator.pl` and the owner-only admin UI and APIs at `https://admin.polskistreamersymulator.pl`. The application has one replica and mounts a persistent local volume for the proposed `catalog.db` and `analytics.db` SQLite files. No chosen streamer names or player saves enter either file; browser storage remains the only career save location. k3s's own default SQLite datastore is independent of the application files.

The example pins [k3s `v1.36.5+k3s1`](https://docs.k3s.io/release-notes/v1.36.X) and cert-manager `v1.21.2`. [cert-manager 1.21 supports Kubernetes 1.33–1.36](https://cert-manager.io/docs/releases/), so the playbook rejects a different k3s minor line until the pair is reviewed together.

The VPS capacity is above [k3s's baseline of 2 CPU and 2 GB RAM for a server](https://docs.k3s.io/installation/requirements). SQLite has no separate database-server pod, but measure memory, storage, and write contention under a representative workload. Keep both database files on the same node's local persistent volume; [SQLite WAL mode is not suitable for a network filesystem shared by multiple hosts](https://www.sqlite.org/wal.html). Monitor the 75 GB host disk, including database, WAL, backups waiting for transfer, container images, and k3s data. The creator's own second machine is the confirmed destination for encrypted off-VPS backups. This one-node setup is not highly available: loss of the VPS stops the game until it is restored.

## Traffic and trust boundaries

```text
Player browser -- HTTPS 443, public host --> k3s ServiceLB/Traefik --+
Owner browser  -- HTTPS 443, admin host  --> k3s ServiceLB/Traefik --+--> Server service --> Server pod
                                                                                              |-- public game + API
                                                                                              |-- protected admin UI + API
                                                                                              +-- local persistent volume
                                                                                                  |-- catalog.db
                                                                                                  +-- analytics.db

GitHub Actions -- SSH 22 --> restricted deploy account --> local k3s kubectl
```

- DNS `A` records for `polskistreamersymulator.pl` and `admin.polskistreamersymulator.pl` point at the VPS public IPv4 address. Configure `AAAA` records only if IPv6 routing and firewall rules are tested. Issue valid TLS certificates for both hosts.
- Open inbound TCP 22, 80, and 443. Port 22 must be reachable by GitHub-hosted runners, whose source addresses are not fixed for this project. Use key-only SSH, disable root login after an admin account is verified, and run Fail2ban. Keep Kubernetes API port 6443 closed externally. SQLite exposes no network port. Do not copy kubeconfig to GitHub Actions.
- The bundled k3s Traefik and ServiceLB own ports 80/443. cert-manager uses HTTP-01 to obtain a TLS certificate from Let's Encrypt. The HTTP path must remain reachable for issuance and renewal.
- The app has no public NodePort and the container listens only through its ClusterIP service. API request limits and validation still matter because browser state is untrusted.
- Route both hosts to the same server only after configuring host-based endpoint separation. The admin host uses GitHub OAuth with one allowlisted stable user ID, a host-only admin cookie, and server-side authorization on all draft, preview, edit, and publish endpoints. Store the GitHub OAuth client secret and owner ID in server-only configuration; never commit them or include them in the Blazor assets. A separate host is not a substitute for authorization.
- Publish the GHCR image as public for the first deployment, or add an image pull secret and rotate its read-only token if the package must stay private. A GitHub Actions `GITHUB_TOKEN` that publishes an image does not automatically authenticate the VPS to pull it.

## Repository layout and responsibilities

| Path | Purpose |
| --- | --- |
| `deploy/ansible/` | Bootstrap Ubuntu, install pinned k3s and cert-manager versions, configure host security, install cluster manifests and restricted deploy command. |
| `deploy/k8s/` | Namespace, service, ingress, issuer, and an image-templated application Deployment. Add a local persistent-volume claim and mount for the SQLite files before production deployment. The current Deployment still contains obsolete database connection-string Secret references; replace them with file paths when implementing persistence. |
| `deploy/ansible/files/pss-deploy` | Root-owned script invoked through a forced SSH command; validates an exact GHCR digest, applies one image, waits for rollout, and rolls back on failure. |
| `.github/workflows/deploy.yml` | Manual `workflow_dispatch` on `main`: test, build, publish an immutable image digest, then request deployment by SSH. |
| `deploy/Dockerfile` | Future .NET 10 multi-stage publish of `Server`. It cannot build until that project exists. |

## Provisioning sequence

1. Point the public and admin DNS names to the VPS. Check that no existing service needs TCP 80/443 and that the provider firewall permits 22/80/443. Prepare a console or recovery path from the VPS provider before changing SSH/firewall settings.
2. Generate **separate** Ed25519 key pairs for the admin and GitHub deploy identities. Keep private keys off the repository. Copy `deploy/ansible/inventory.example.yml` to a private inventory and fill the VPS address, login account, public keys, ACME email, and a supported pinned k3s release in the form `vX.Y.Z+k3sN`. The inventory example must not contain real keys or passwords. The current image build targets `linux/amd64`; the playbook checks for `x86_64` and must be adapted if the VPS is ARM.
3. Run Ansible Core 2.17–2.19 from Linux or WSL (Ansible's controller is not native Windows): `ansible-galaxy collection install -r deploy/ansible/requirements.yml`, then `ansible-playbook -i /private/path/inventory.yml deploy/ansible/bootstrap.yml`. Supply `--ask-become-pass` if the existing bootstrap account requires a sudo password. It installs security updates, UFW, Fail2ban, k3s, cert-manager, and the app's base Kubernetes resources. k3s install uses its official installer with a pinned `INSTALL_K3S_VERSION`; review version changes before updating it.
4. **Verify a fresh SSH login as `pss-admin` with the new key, including `sudo -n true`.** Change `ansible_user` in the private inventory to `pss-admin`, then run `ansible-playbook -i /private/path/inventory.yml deploy/ansible/harden-ssh.yml -e admin_login_verified=true`. It disables SSH password and root login. This split prevents an unverified key or firewall rule from immediately locking out the operator.
5. Configure a GitHub `production` environment. Set its secret `VPS_DEPLOY_SSH_KEY` to the deploy private key and variables `VPS_HOST` and `VPS_SSH_HOST_KEY` (the complete verified `ssh-keyscan` host-key line; verify its fingerprint through the VPS console, never by trusting an unauthenticated first connection). Restrict environment deployment to `main`; optional environment approval is an operator preference in addition to the workflow's manual trigger.
6. Complete the `Server` project, health endpoints, SQLite volume mount and catalogue migration/import path described below. Make the GHCR package public or configure a pull secret. Run the workflow from `main` using **Actions → Deploy production → Run workflow** only after the readiness gates are satisfied.

The Ansible inventory is deliberately not committed with real values. Keep it outside the repository or encrypt it with Ansible Vault. The administrator key grants passwordless sudo, so protect it as a root credential. Ansible manages the cluster's base resources; the deploy command only changes the application image. Re-run Ansible after intentional Kubernetes configuration changes.

## Release path and rollback

The workflow runs `dotnet test` and builds the .NET container image from the exact checked-out commit, pushes it to GHCR, and reads the resulting SHA-256 image digest. It sends only `deploy ghcr.io/...@sha256:...` over SSH. The server-side forced command rejects other commands and only accepts the configured repository image. The root-owned deploy script renders the Deployment, waits for readiness, and restores the previous image if the rollout fails. An initial failed rollout has no prior image to restore. `Recreate` may produce a short outage during each successful deployment.

Do not deploy automatically on every push. Keep `workflow_dispatch`, `main` restriction, and a production concurrency group. Deploying an older commit is allowed only as an explicit manual rollback using an already published digest through the same restricted command, with SQLite schema compatibility checked first. A code rollback cannot reverse a destructive catalogue migration. The application image workflow must not replace or erase the persistent database volume.

## SQLite, migrations, and backups

- Mount one local-path PVC backed by the VPS disk into the single non-root server pod, for example at `/var/lib/pss`. Place `catalog.db` and `analytics.db` there, outside the application image and `wwwroot`. The mount must permit the configured pod UID to create the database and its journal/WAL files. Keep `replicas: 1` and the current `Recreate` deployment strategy; a second app pod must not independently mount and write these files. A local PVC is not an offsite backup.
- Use separate `CatalogDbContext` and `AnalyticsDbContext` instances with separate SQLite files and migration histories. The app has no SQL login or database Service. Restrict file access through volume permissions and restrict authoring APIs through server-side operator authentication and authorization. Store paths in server-only configuration; never expose database files through HTTP or copy them into the container image.
- Publishing a new immutable catalogue version freezes weekly actions, events, all game parameters, and name parts together, and preserves older versions needed by existing browser saves. Changes go through the validated, audited admin publication procedure in [event system](event-system.md), not direct production table edits. A private seed importer can bootstrap a first draft.
- Create consistent backups of both SQLite files daily and before each catalogue publication or schema migration, then transfer encrypted copies to the creator's second machine. A pre-change backup must complete successfully before the change begins. Apply and verify migrations as a controlled release step rather than on every app startup. [EF Core documents SQLite migration limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations), including operations that rebuild tables and a migration lock that may need operator recovery after an interrupted migration.
- For a live database use SQLite's [online backup API](https://www.sqlite.org/backup.html) or the [.NET `BackupDatabase` API](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup). Do not copy only a live `.db` file while a WAL may contain committed transactions; [SQLite describes the WAL files](https://www.sqlite.org/wal.html). Encrypt completed backup artifacts before transferring them to the second machine, retain copies there for the confirmed 30 days, verify transfer success, and rehearse a restore of both files and the pinned catalogue versions. Connection details and encryption key storage remain to be configured. Coordinate backup expiry with the separate 12-month analytics-counter retention; a restored older snapshot must reapply current retention before analytics reporting resumes.
- Include both `/var/lib/rancher/k3s/server/db/` **and** `/var/lib/rancher/k3s/server/token` in the daily off-VPS backup set, and back them up before cluster upgrades. See the [k3s backup procedure](https://docs.k3s.io/datastore/backup-restore). The browser's career saves are outside these backups; the confirmed first-version local file export/import gives players a manual transfer and recovery path.

## Application readiness gates

The infrastructure files are intentionally ahead of the game implementation. Before the first real deployment, implement and verify:

1. `Server` and `Contracts` projects; `Server` hosts the Blazor client and API on port 8080. Add at least one real test project to the solution so the workflow has an actual test gate.
2. `/health/live` and `/health/ready`; readiness must fail if the published game catalogue is unavailable, and liveness must avoid restarting healthy processes for a temporary database issue. A temporary analytics database failure should warn and drop counters, not take gameplay out of service.
3. A writable application PVC mount, server-only SQLite file paths, EF Core SQLite migrations for both files, tested daily and pre-change encrypted backups to the second machine with a restore rehearsal, and a first published catalogue with at least one weekly action, events, a valid parameter row, and both streamer-name part groups. The current Deployment manifest does not yet mount a database PVC and still references obsolete database Secrets.
4. An owner-only admin panel at the confirmed admin hostname with GitHub OAuth, a stable GitHub user ID allowlist, a protected session cookie, draft-only editing, bilingual preview, validation, audited publication, and tests that anonymous or non-allowlisted users cannot read drafts or mutate content. Configure exact OAuth callback and separate host routing before public release.
5. A production container build and a successful pull from GHCR on the VPS.
6. TLS issuance for both hostnames, the multi-event weekly request path, owner sign-in and publication path, browser save and file export/import behavior across a rollout, and a restore rehearsal.
7. JSON console logging with the confirmed 14-day node-log retention, aggregate analytics migrations and writer with 12-month counter retention, a private read-only statistics report, and Polish and English privacy notices with applicable consent requirements reviewed before telemetry is enabled in production. Do not expose raw metrics or statistics endpoints through the public ingress.

## Operations

Use `sudo k3s kubectl -n pss get pods,svc,ingress,pvc` and `sudo k3s kubectl -n pss logs deployment/pss-server` on the VPS. Check `cert-manager` Certificate/Challenge resources when TLS fails. Watch disk free space, SQLite file and WAL size, memory, analytics writer drops, PVC status, restarts, certificate expiry, the last successful transfer to the second machine, and 30-day backup expiry. Rotate and delete node logs after the confirmed 14 days; delete daily aggregate analytics after the confirmed 12 months. Keep Ubuntu security upgrades automatic; review k3s, .NET/SQLite provider, and cert-manager upgrades in a scheduled maintenance window with backups. Configure backup transfer credentials and rehearse restoration before public release. The operator report and telemetry data limits are in [logging and analytics](observability-and-analytics.md).
