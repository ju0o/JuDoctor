# Releasing JuDoctor

JuDoctor public releases are designed to be downloadable directly from **GitHub Releases**.

## Release assets

Every public Windows release should contain:

```text
JuDoctor-<version>-Setup.exe
JuDoctor-<version>-Setup.exe.sha256
```

The `.sha256` file is generated from the exact installer uploaded to the release.

## Automated release workflow

The repository contains:

`.github/workflows/release.yml`

The workflow:

1. checks out the tagged source
2. installs .NET 9
3. restores the solution
4. runs the full test suite
5. installs Inno Setup
6. runs `installer/publish.ps1 -Installer`
7. finds the produced setup executable
8. copies/renames it to `JuDoctor-<version>-Setup.exe`
9. generates SHA256
10. uploads both files as a GitHub Actions artifact
11. creates or updates the matching GitHub Release

## Recommended v1.0.0 sequence

Do not tag a release until the sanitized public source and installer scripts are present on `main` and the public build is green.

```powershell
# from a clean public-source checkout
git checkout main
git pull

dotnet test -c Release

# then create the public release tag
git tag v1.0.0
git push origin v1.0.0
```

Pushing the tag triggers the release workflow.

Alternatively, the workflow can be started manually from GitHub Actions by providing a version such as `1.0.0`.

## Release notes

If a matching file exists:

`RELEASE_NOTES_v<version>.md`

the workflow uses it for the GitHub Release body. Otherwise GitHub-generated notes are used.

## Release failure policy

If tests, publishing, installer compilation, or asset generation fail, do not manually upload a random local build under the same tag. Fix the release pipeline or document the exceptional process explicitly so the uploaded binary remains traceable to a known source revision.

## Signing

Early V1 builds may be unsigned. Code signing should be added when distribution expands, but signing secrets/certificates must never be committed to this repository.
