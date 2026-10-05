# Deployment: single VPS, k3s, and manual GitHub Actions

## Status and target

The creator chose a VPS running Ubuntu 24.04 with 4 vCPU, 8 GB RAM, and 75 GB disk, the domain `polskistreamersymulator.pl`, Ansible provisioning, Kubernetes on that VPS, and a manually triggered GitHub Actions deployment. This is a **deployment design and infrastructure scaffold**, not a deployed service. The current solution has no `Server` project, container image, health endpoints, or event catalogue implementation yet.

Use a single-node k3s cluster. One ASP.NET Core `Server` container will serve the Blazor WebAssembly files and API from the same HTTPS origin. The application has one replica and a `Recreate` rollout strategy because its SQLite event catalogue is mounted from one node-local persistent volume. No player saves enter Kubernetes or SQLite; browser storage remains the only career save location. k3s's own default SQLite database is separate from the game's catalogue database.

The example pins [k3s `v1.36.5+k3s1`](https://docs.k3s.io/release-notes/v1.36.X) and cert-manager `v1.21.2`. [cert-manager 1.21 supports Kubernetes 1.33–1.36](https://cert-manager.io/docs/releases/), so the playbook rejects a different k3s minor line until the pair is reviewed together.

The VPS capacity is above [k3s's baseline of 2 CPU and 2 GB RAM for a server](https://docs.k3s.io/installation/requirements), but actual headroom must be measured after the application is running. Do not describe this one-node setup as highly available: loss of the VPS stops the game until it is restored.

## Traffic and trust boundaries

```text
Browser -- HTTPS 443 --> k3s ServiceLB/Traefik --> Server service --> Server pod
                                                       |                  |
                                                       |                  +-- SQLite catalogue PVC
                                                       +-- Blazor assets + API

GitHub Actions -- SSH 22 --> restricted deploy account --> local k3s kubectl
```

- DNS `A` record for `polskistreamersymulator.pl` points at the VPS public IPv4 address. Configure an `AAAA` record only if IPv6 routing and firewall rules are tested.
- Open inbound TCP 22, 80, and 443. Port 22 must be reachable by GitHub-hosted runners, whose source addresses are not fixed for this project. Use key-only SSH, disable root login after an admin account is verified, and run Fail2ban. Keep the Kubernetes API port 6443 closed externally. Do not copy kubeconfig to GitHub Actions.
- The bundled k3s Traefik and ServiceLB own ports 80/443. cert-manager uses HTTP-01 to obtain a TLS certificate from Let's Encrypt. The HTTP path must remain reachable for issuance and renewal.
- The app has no public NodePort and the container listens only through its ClusterIP service. API request limits and validation still matter because browser state is untrusted.
- Publish the GHCR image as public for the first deployment, or add an image pull secret and rotate its read-only token if the package must stay private. A GitHub Actions `GITHUB_TOKEN` that publishes an image does not automatically authenticate the VPS to pull it.

## Repository layout and responsibilities

| Path | Purpose |
| --- | --- |
| `deploy/ansible/` | Bootstrap Ubuntu, install pinned k3s and cert-manager versions, configure host security, install cluster manifests and restricted deploy command. |
| `deploy/k8s/` | Namespace, catalogue PVC, service, ingress, issuer, and an image-templated application Deployment. |
| `deploy/ansible/files/pss-deploy` | Root-owned script invoked through a forced SSH command; validates an exact GHCR digest, applies one image, waits for rollout, and rolls back on failure. |
| `.github/workflows/deploy.yml` | Manual `workflow_dispatch` on `main`: test, build, publish an immutable image digest, then request deployment by SSH. |
| `deploy/Dockerfile` | Future .NET 10 multi-stage publish of `Server`. It cannot build until that project exists. |

## Provisioning sequence

1. Point the domain to the VPS. Check that no existing service needs TCP 80/443 and that the provider firewall permits 22/80/443. Prepare a console or recovery path from the VPS provider before changing SSH/firewall settings.
2. Generate **separate** Ed25519 key pairs for the admin and GitHub deploy identities. Keep private keys off the repository. Copy `deploy/ansible/inventory.example.yml` to a private inventory and fill the VPS address, login account, public keys, ACME email, and a supported pinned k3s release in the form `vX.Y.Z+k3sN`. The inventory example must not contain real keys or passwords. The current image build targets `linux/amd64`; the playbook checks for `x86_64` and must be adapted if the VPS is ARM.
3. Run Ansible Core 2.17–2.19 from Linux or WSL (Ansible's controller is not native Windows): `ansible-galaxy collection install -r deploy/ansible/requirements.yml`, then `ansible-playbook -i /private/path/inventory.yml deploy/ansible/bootstrap.yml`. Supply `--ask-become-pass` if the existing bootstrap account requires a sudo password. It installs security updates, UFW, Fail2ban, k3s, cert-manager, and the app's base Kubernetes resources. k3s install uses its official installer with a pinned `INSTALL_K3S_VERSION`; review version changes before updating it.
4. **Verify a fresh SSH login as `pss-admin` with the new key, including `sudo -n true`.** Change `ansible_user` in the private inventory to `pss-admin`, then run `ansible-playbook -i /private/path/inventory.yml deploy/ansible/harden-ssh.yml -e admin_login_verified=true`. It disables SSH password and root login. This split prevents an unverified key or firewall rule from immediately locking out the operator.
5. Configure a GitHub `production` environment. Set its secret `VPS_DEPLOY_SSH_KEY` to the deploy private key and variables `VPS_HOST` and `VPS_SSH_HOST_KEY` (the complete verified `ssh-keyscan` host-key line; verify its fingerprint through the VPS console, never by trusting an unauthenticated first connection). Restrict environment deployment to `main`; optional environment approval is an operator preference in addition to the workflow's manual trigger.
6. Complete the `Server` project, health endpoints, and catalogue migration/import path described below. Make the GHCR package public or configure a pull secret. Run the workflow from `main` using **Actions → Deploy production → Run workflow**.

The Ansible inventory is deliberately not committed with real values. Keep it outside the repository or encrypt it with Ansible Vault. The administrator key grants passwordless sudo, so protect it as a root credential. Ansible manages the cluster's base resources; the deploy command only changes the application image. Re-run Ansible after intentional Kubernetes configuration changes.

## Release path and rollback

The workflow runs `dotnet test` and builds the .NET container image from the exact checked-out commit, pushes it to GHCR, and reads the resulting SHA-256 image digest. It sends only `deploy ghcr.io/...@sha256:...` over SSH. The server-side forced command rejects other commands and only accepts the configured repository image. The root-owned deploy script renders the Deployment, waits for readiness, and restores the previous image if the rollout fails. An initial failed rollout has no prior image to restore. `Recreate` may produce a short outage during each successful deployment.

Do not deploy automatically on every push. Keep `workflow_dispatch`, `main` restriction, and a production concurrency group. Deploying an older commit is allowed only as an explicit manual rollback using an already published digest through the same restricted command, with the database compatibility checked first. A code rollback cannot reverse a destructive catalogue migration.

## Catalogue, migrations, and backups

- Mount a local-path PVC at `/data`; the proposed app connection string is `Data Source=/data/events.db`. The `pss-local-retain` StorageClass keeps the PV if the claim is deleted, but that is still not a backup. The actual configuration key and process UID must be verified when `Server` and EF Core are implemented. The Deployment reserves 1 GiB for the catalogue; monitor the 75 GB host disk and adjust before the disk fills.
- Only one pod writes the catalogue. Publishing a new immutable event-catalogue version preserves older versions needed by existing browser saves. Content changes go through the validated import/publish procedure in [event system](event-system.md), not a direct production SQLite edit.
- Before every schema migration or content publication, take an SQLite-consistent catalogue backup using the SQLite backup API or a stopped writer, not a plain copy of a live WAL file. Apply and verify migrations in a separate controlled release step; do not make every pod start mutate the schema.
- Back up both `/var/lib/rancher/k3s/server/db/` **and** `/var/lib/rancher/k3s/server/token`, plus the separate application catalogue PVC. Encrypt and copy them off the VPS, set a retention policy, and test a full restore. A PVC on the same VPS is not an offsite backup. See the [k3s backup procedure](https://docs.k3s.io/datastore/backup-restore).
- The browser's career saves are outside these backups. Offer export/import later if cross-device recovery becomes a product requirement.

## Application readiness gates

The infrastructure files are intentionally ahead of the game implementation. Before the first real deployment, implement and verify:

1. `Server` and `Contracts` projects; `Server` hosts the Blazor client and API on port 8080. Add at least one real test project to the solution so the workflow has an actual test gate.
2. `/health/live` and `/health/ready`; readiness must fail if the published catalogue is unavailable, and liveness must avoid restarting healthy processes for a temporary database issue.
3. A writable `/data` mount for the runtime UID, an EF Core SQLite migration procedure, and a first published event catalogue.
4. A production container build and a successful pull from GHCR on the VPS.
5. TLS issuance, the weekly game request path, browser save behavior across a rollout, and a restore rehearsal.

## Operations

Use `sudo k3s kubectl -n pss get pods,svc,ingress,pvc` and `sudo k3s kubectl -n pss logs deployment/pss-server` on the VPS. Check `cert-manager` Certificate/Challenge resources when TLS fails. Watch disk free space, the SQLite file, PVC status, restarts, and certificate expiry. Keep Ubuntu security upgrades automatic; review k3s and cert-manager version upgrades in a scheduled maintenance window with backups. Avoid unbounded container logs and set an offsite backup destination before public release.
