# ship · project config

## Parameters

| Key | Value | Notes |
|-----|-------|-------|
| `bundle.source` | `<tbd — set by /ship:charter>` | Set by `/ship:charter` from §1's delivery surface |
| `bundle.platform` | `<tbd — set by /ship:charter>` | Set by `/ship:charter` from §1's delivery surface |
| `release.target` | `v0.1.0` | Current release the pipeline is driving toward; names the dossier folder |
| `dossier.dir` | `ship/` | Where per-release dossiers live (`ship/<release>/…`) |
| `release.recipe` | `ship/recipes/release-recipe.md` | Project-specific release steps `/ship:release` runs |
| `recipe.constitution` | `ship/recipes/constitution.md` | Drafted/confirmed by `/ship:charter` |
| `recipe.standards` | `ship/recipes/engineering-standards.md` | Drafted/confirmed by `/ship:charter` |
| `recipe.docs` | `ship/recipes/docs-recipe.md` | What `/ship:docs` generates for this project |
| `agent.model` | *(empty)* | Empty = the caller's default |
| `agent.effort` | *(empty)* | Empty = the caller's default |
| `mode` | `forward` | Driving a new/unbuilt release idea to ship |

## Release history

| Release | Status | Dossier |
|---------|--------|---------|
| v0.1.0 | Framing (Definition phase, in progress) | `ship/v0.1.0/` |
