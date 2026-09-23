<!-- Generated from workspace-wiki/meta/routers.yml by scripts/router.py. Do not edit by hand. -->

# Agent Router: Unity PmPrefs

Router for PmPrefs, a Unity package for encrypted PlayerPrefs. The
profile of all Unity tools lives in the vault.

## Always Read In This Order

1. `../workspace-wiki/agents/README.md`: the shared reading chain of the ProjectMakers vault
2. `../workspace-wiki/projects/unity-tools/README.md`: this project's page in the vault
3. [README.md](README.md): features and API
4. [CONTRIBUTING.md](CONTRIBUTING.md): how changes are made

If `../workspace-wiki` is missing, clone the vault there first. Without it the shared
half of the rules is missing. The vault is private, its clone URL is in the
router of every private ProjectMakers repository.

## Secrets

Never put a secret value into a page or a commit. Rules for every project:
`../workspace-wiki/agents/environment.md`, section 6. Cross-project keys:
`../workspace-wiki/infrastructure/secrets.md`.

## Rules

- The repo root is the Unity package. Every file in it needs a `.meta` file, these router files included.
