# Sith Policy README Badge — Design Exploration

**Status:** Prototype/design exploration  
**Repository branch policy:** exactly `master` and `development`

## Intent

Put a compact, unmistakable Sith Policy status badge alongside the existing README badges. It should communicate whether a repository is currently following the Workshop branch policy, and it should change when the branch set changes.

## The rule

Do not implement this as a count-only check.

```csharp
var compliant =
    branches.Count == 2 &&
    branches.Contains("master") &&
    branches.Contains("development");
```

The equivalent set comparison is:

```text
actualBranches.SetEquals(["master", "development"])
```

The complete, paginated branch list must be checked. Two arbitrary branches are not compliant.

## Badge states

| State | Label | Meaning |
|---|---|---|
| Enacted | **SITH POLICY ENACTED** | The only branches are `master` and `development`. |
| Violated | **SITH POLICY VIOLATED** | A branch is missing, an extra branch exists, or a branch has been renamed. |

Color should reinforce—not carry—the meaning. The state text and accessible SVG title/description must remain explicit. Suggested visual language: dark graphite body, sharp red Sith accent, restrained metallic framing; enacted and violated should be distinguishable at a glance.

## Why an SVG cannot do the branch lookup itself

An SVG embedded by Markdown as an image is passive content. It cannot safely run JavaScript, call the GitHub API, or evaluate a repository's branch list. The conditional logic must run elsewhere; the SVG is the rendered result.

## Architecture options

### A. Repository-local GitHub Actions generator

A workflow calls the GitHub API, follows pagination, evaluates the exact branch set, then writes an SVG. It can commit the generated file to a designated status branch/path.

**Advantages:** simple, transparent, no external service, easy to audit.

**Important limitation:** scheduled workflows and repository branch lifecycle events are constrained by the workflow file's presence on the default branch. If the default branch is `master`, adding the workflow only to `development` does not provide a dependable schedule/branch-event trigger. Committing an automatically refreshed status file to `master` would also create noise on the release branch. This needs an explicit design decision before implementation.

A workflow triggered only by pushes to `development` and manual dispatch is not truly live: creating or deleting a branch does not necessarily push to `development`.

### B. Shared central badge service

A central service reads the repository's complete branch list and serves the corresponding SVG at a stable URL. Each README embeds the same simple image URL with its repository identity.

**Advantages:** one implementation and consistent visual design across all Workshop repositories; no generated commits on each repository's release branch.

**Trade-offs:** service hosting, authentication/rate limits, caching/refresh latency, availability, and safe handling of public/private repository access. Public repos may be queried anonymously; private repos require an appropriately scoped credential or an authenticated central workflow.

### C. Reusable workflow plus central status hosting

A reusable workflow can standardize the branch-set check, while a central Pages/API endpoint hosts the SVG. This separates policy logic from presentation and avoids committing status changes into every repository.

**Trade-offs:** more setup than a local generator, but likely the cleanest long-term Workshop-wide solution if a shared service is acceptable.

## Required behavior before rollout

1. Enumerate every branch page; never silently truncate.
2. Compare the exact set, not just the count.
3. Render both states with explicit text and accessible metadata.
4. If GitHub cannot be queried, do not display a green/enacted state. Show an unmistakable **STATUS UNKNOWN** state or preserve the last state with a visible stale indicator.
5. Define and publish refresh/cache latency.
6. Test fixtures: exact pair; extra branch; missing `master`; missing `development`; two wrong names; empty result; API failure; pagination.
7. Keep badge generation read-only with respect to repository branches. It must never delete branches, merge, change defaults, or publish packages.

## README integration concept

Once the live endpoint exists, the README snippet should be one line, for example:

```markdown
![Sith Policy](<stable-badge-endpoint-for-this-repository>)
```

The endpoint must be stable and must not point at an unverified generated file.

## Current prototype scope

This document records the design and constraints only. The repository currently has exactly `master` and `development`. No live badge is claimed yet, because a reliable refresh mechanism has not been selected. See [Issue #2](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/issues/2) for the implementation investigation.

## Decision recommendation

Prefer **C** for the Workshop-wide end state if the Workshop is willing to own a small shared status endpoint. Until then, prototype the visual design and exact-set evaluator independently, without modifying `master` or implying that a manually refreshed SVG is live.
