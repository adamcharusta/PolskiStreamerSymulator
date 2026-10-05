# Streamer name suggestions

## Player experience

The setup screen has one **streamer name** field. A published SQL Server catalogue supplies a suggested name made by concatenating two approved parts, for example `Random` + `Bruce` → `RandomBruce` or `Neon` + `Borsuk` → `NeonBorsuk`. The suggestion appears in the editable field. The player may accept it, request another suggestion, or replace it with a custom name. Regenerating never overwrites a name after the player has edited the field without an explicit click. Channel name remains a separate optional setup field if it is kept in the first UI.

The name is identity text, not an archetype: it grants no starting bonus, eligibility modifier, or scoring effect. There is no uniqueness guarantee because the game has no accounts or global name registry. The accepted name is stored in the browser career save; SQL Server stores only the shared vocabulary. A custom name is never written back into the vocabulary.

## Published vocabulary

`StreamerNamePart` belongs to the same immutable `CatalogVersion` as [game parameters and events](event-system.md). Each row has `VersionId`, stable `PartId`, `Kind` (`first` or `second`), `Text`, and `Enabled`. The initial seed below is a **draft example**, not a list of real creators to imitate.

| First parts | Second parts |
| --- | --- |
| `Random`, `Neon`, `Cichy`, `Turbo`, `Pixel` | `Bruce`, `Borsuk`, `Kret`, `Router`, `Piksel` |
| `Nocny`, `Mega`, `Kosmiczny`, `Dziki`, `Pogodny` | `Kabel`, `Kometa`, `Mikrofon`, `Pstryk`, `Ziemniak` |

The importer requires at least two enabled parts of each kind; unique stable IDs; no duplicate normalized text within a kind; and each enabled part to be 2–16 letters or digits with the intended casing. Concatenation has no separator and must fit the 32-character streamer-name limit. Review seed words for unintended offensive combinations, real creator impersonation, and readable capitalization before publishing. Editing the vocabulary creates a new catalogue version; old saved names do not change.

## Suggestion flow

1. `GetSetupOptions` reads the latest published catalogue version, its public starting parameters, and one name suggestion. The UI keeps that version ID while setup is open.
2. `GetStreamerNameSuggestion` takes that published version ID, loads enabled first and second parts in stable `PartId` order, independently selects one uniformly from each group, and concatenates their stored text. It returns the suggestion and version ID. A repeated suggestion is allowed.
3. The suggestion uses a random source **separate from the saved gameplay PRNG**. Requesting another name must not change event rolls, weekly-action rolls, the run seed, or a reproducible career.
4. `StartRun` receives the setup version ID and the final name chosen by the player. It validates that the version remains published, pins it for the run, and uses its starting parameters. A publication that happens while the setup screen is open cannot silently change the visible run length or starting values.

The player can edit the field directly. Proposed validation for both suggested and custom names: normalize Unicode to NFC, trim outer whitespace, accept 2–32 displayed characters made of letters, digits, single internal spaces, hyphens, or underscores, and reject control characters. Validate again on the server. Render the chosen text as escaped UI text, never as HTML. If the suggestion service is temporarily unavailable, leave the field editable and let the player start with a valid custom name once setup parameters are available.

## Tests to add with the feature

- An eligible published vocabulary produces a two-part suggestion with exact stored casing, a valid length, and no separator.
- A missing or invalid part group prevents publication; duplicate text and IDs fail import.
- A custom name overrides the suggestion and survives save/reload without entering SQL Server.
- Repeated suggestion requests do not advance the gameplay PRNG or alter the same-seed career outcomes.
- A new catalogue version changes future suggestions but leaves an open setup screen and an existing run pinned to their selected version.
