# NuGet author signing with Azure Key Vault

The release-only `push-nuget` job in `.github/workflows/build.yml` signs and
verifies each non-preview `.nupkg` before obtaining a short-lived NuGet.org
publishing key through Trusted Publishing. Signing or verification failure
blocks all publishing in this job. Pull-request builds, uploaded build
artifacts, and GitHub Packages publishing do not use the signing identity and
remain unsigned at the package level.

NuGet author signing identifies the package publisher and protects package
integrity. It is separate from the existing assembly strong-name signing with
`Key.snk`, and from NuGet.org's repository signature. This workflow does not
Authenticode-sign or otherwise change the assemblies inside a package.

## Prerequisites and costs

You need an Azure subscription, permission to create resources and assign
vault-scoped roles, permission to create a Microsoft Entra application/service
principal, GitHub repository administration access, and control of the
NuGet.org package owner account.

Budget for a Key Vault **Premium** vault, HSM key operations, and a publicly
trusted **code-signing** certificate. A TLS certificate or a self-signed
certificate is not suitable for publishing signed packages to NuGet.org.
Azure Key Vault Premium is not the separate Azure Managed HSM service.

**Before purchasing a certificate**, confirm with the CA that its current
code-signing product supports an externally generated CSR and your Azure Key
Vault Premium HSM, including any hardware attestation it requires. Do not
assume ordinary SSL/TLS issuer integration provides code-signing issuance.
Follow the CA's identity validation, subject, key size, and hardware assurance
requirements. The private key must be generated in the HSM and stay
non-exportable; do not generate or upload a PFX or private key.

The workflow pins the .NET Foundation
[Sign CLI](https://github.com/dotnet/sign) to
[`0.9.1-beta.26475.3`](https://www.nuget.org/packages/sign/0.9.1-beta.26475.3).
It is a prerelease tool, chosen explicitly for its NuGet and Key Vault support.
Do not replace the pin with an unbounded `--prerelease` install or a numerically
higher version without checking its provenance and supported options.
AzureSignTool's Authenticode support alone is not NuGet author signing.

## 1. Create a dedicated Premium vault

In the Azure portal, create a Key Vault in the intended subscription, resource
group, and region:

| Setting | Value |
| --- | --- |
| Pricing tier | Premium |
| Permission model | Azure role-based access control (RBAC) |
| Soft delete | Enabled |
| Purge protection | Enabled |
| Contents | Only this publishing identity's signing certificate and key |

Record the vault URI, such as `https://your-signing-vault.vault.azure.net`.
Use the URI shown by Azure, not a certificate or key version URL.

The GitHub-hosted `windows-latest` runner needs HTTPS access to the vault's
data-plane and Azure authentication endpoints, access to certificate
revocation/chain endpoints, and HTTP access to the RFC 3161 timestamp service
`http://timestamp.digicert.com`. The timestamp token itself is signed and
validated cryptographically; this is the service's documented HTTP endpoint.
A private endpoint or vault firewall is not
bypassed by Azure login or RBAC. "Allow trusted Microsoft services" does not
automatically admit GitHub-hosted runners. For this hosted-runner workflow,
enable public network access to the vault; authorization is still required.
If policy forbids that, provision a suitably networked runner and change the
runner configuration before using this workflow.

For provisioning only, give the operator **Key Vault Certificates Officer**
on this vault, plus the ability to assign roles where needed. That role is
not the CI signing role. Record the vault's full resource ID for role
assignments.

## 2. Generate the key and obtain the certificate

Follow Microsoft's
[create and merge a CSR procedure](https://learn.microsoft.com/en-us/azure/key-vault/certificates/create-certificate-signing-request)
with the CA's code-signing requirements:

1. In the vault, open **Certificates > Generate/Import**, choose **Generate**,
   and select **Certificate issued by a non-integrated CA**.
2. Choose a stable certificate name, for example `nuget-author-signing`.
   Enter the CA-approved legal identity in the subject; do not use a website
   hostname as a substitute for publisher identity.
3. In the certificate's advanced policy, choose **RSA-HSM**, a CA-approved
   key size of at least **3072 bits**, and **Exportable Private Key: No**.
   Set key usage to **Digital Signature** and enhanced key usage to
   **Code Signing** (`1.3.6.1.5.5.7.3.3`), not TLS server authentication.
   Match the requested validity and any additional fields to the CA's policy.
4. Create the pending certificate and download its CSR from
   **Certificate Operation > Download CSR**. Only send the CSR and the CA's
   required public attestation/validation material to the CA.
5. Complete the CA's validation and obtain the issued certificate and
   intermediate chain. In the same pending certificate operation, choose
   **Merge Signed Request**. Use the CA's chain-containing response, such
   as a `.p7b`, following Azure's supported formats.
6. Check that the resulting certificate is enabled, within its validity
   period, has the code-signing EKU and expected issuer/subject, and links
   to the intended enabled RSA-HSM key with signing permitted. Confirm
   the certificate policy still has a non-exportable private key.

Do not discard or replace the pending request while waiting for the CA:
its public key must match the key in the issued certificate. Do not use
the self-signed certificate creation path as a shortcut.

Download the issued **public certificate in CER format**, not PFX. This
does not export the HSM private key. Save it locally as `signing.cer`.
Calculate its SHA-256 fingerprint in PowerShell:

```powershell
$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
    (Resolve-Path .\signing.cer).Path)
$certificate.GetCertHashString([System.Security.Cryptography.HashAlgorithmName]::SHA256)
$certificate.Dispose()
```

Keep the resulting 64 hexadecimal characters for GitHub configuration.
The common SHA-1 "thumbprint" displayed by certificate viewers is **not**
the SHA-256 fingerprint used here.

## 3. Configure the Azure signing identity and OIDC trust

Create a single-tenant **Microsoft Entra app registration** for this
repository's NuGet signing job. Its service principal appears under
**Enterprise applications**. Record the application's **Application
(client) ID** and **Directory (tenant) ID**. Do not create a client secret.

Under the app registration's **Certificates & secrets > Federated
credentials**, add a GitHub Actions credential with these exact values:

| Field | Value |
| --- | --- |
| Issuer | `https://token.actions.githubusercontent.com` |
| Organization | `no-ordinary-dev` |
| Repository | `fluent-validation-smart-enum` |
| Entity type | Environment |
| Environment | `NuGet` (case-sensitive) |
| Subject | `repo:no-ordinary-dev/fluent-validation-smart-enum:environment:NuGet` |
| Audience | `api://AzureADTokenExchange` |

These values assume GitHub's default OIDC subject format. If your
organization customizes that format, configure the matching subject instead.

On the **dedicated vault's** Access control (IAM) page, assign the
application's service principal these
[RBAC roles recommended by Sign CLI](https://github.com/dotnet/sign#best-practices):

| Role | Purpose |
| --- | --- |
| Key Vault Reader | Read public certificate and key metadata |
| Key Vault Crypto User | Perform the cryptographic signing operation |

Scope both roles to this dedicated vault, not the subscription or resource
group. Crypto User includes more cryptographic operations than signing;
these are the tool's documented built-in roles, not a sign-only custom role.
No **Key Vault Administrator**, secret-reading role, certificate-management
role, or subscription **Contributor** role is needed by CI.
Allow time for RBAC and federation changes to propagate.

The workflow uses `azure/login` with `allow-no-subscriptions: true` because
this identity does not need subscription-level access. Sign CLI explicitly
uses `--azure-credential-type azure-cli` to use that OIDC-authenticated
Azure CLI session. Neither an Azure client secret nor a subscription ID
is required in GitHub.

## 4. Configure the GitHub NuGet environment

In repository **Settings > Environments > NuGet**, add the following
**environment variables** (not secrets):

| Variable | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | Signing app registration's application/client ID |
| `AZURE_TENANT_ID` | Signing app registration's directory/tenant ID |
| `AZURE_KEY_VAULT_URL` | Vault URI, for example `https://your-signing-vault.vault.azure.net` |
| `AZURE_KEY_VAULT_CERTIFICATE_NAME` | Certificate name, for example `nuget-author-signing`, not its URL |
| `NUGET_SIGNING_CERTIFICATE_SHA256` | Public certificate SHA-256 fingerprint: 64 hex characters without spaces or colons |
| `NUGET_USER` | Existing NuGet.org account used by Trusted Publishing |

These are public identifiers, not credentials. Never store the HSM private
key, a PFX, or `AZURE_CLIENT_SECRET` in GitHub.

The environment is a signing authorization boundary: any workflow able to
use it could request an Azure token with this subject. Restrict its
deployment branches/tags to the intended release refs and, where supported
by your GitHub plan, require reviewers and prevent self-review. Protect the
release tags and workflow changes as well. A branch-only restriction can
block release jobs, which run against tags.

Keep the existing NuGet.org Trusted Publishing policy for `build.yml` and
the `NuGet` environment. Azure OIDC signs packages; NuGet OIDC authorizes
publishing. They are separate trusts using the job's existing
`id-token: write` permission.

## 5. Register the public certificate on NuGet.org

Before publishing a release, sign in to the account owning the package
(or manage the owning organization), open **Account settings >
Certificates > Register new**, and upload the binary DER `signing.cer`.
Check the package's required signer settings if it has multiple owners.

See [NuGet certificate registration and signing requirements](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package).
Registering a certificate can change signing requirements for **other
packages owned by the same account**; coordinate that change with their
maintainers before registration.

## 6. Publish and maintain

After provisioning and registration, publish a GitHub release using the
existing process. The workflow:

1. Downloads the Windows build artifact and validates signing configuration.
2. Installs .NET 8 and the pinned Sign CLI, then logs into Azure using OIDC.
3. Selects the same non-preview `.nupkg` files as the existing publisher.
   No matching packages is an error; the existing `preview` filename
   exclusion remains unchanged.
4. Signs each package with SHA-256 and an RFC 3161 SHA-256 timestamp.
   `--recurse-containers false` explicitly prevents signing embedded
   assemblies or other nested contents.
5. Runs `dotnet nuget verify --all --certificate-fingerprint` against the
   configured SHA-256 fingerprint. Every package must succeed before the
   NuGet login or any push can run.
6. Obtains a NuGet publishing key and pushes the signed packages.
   A push error stops the job; previously accepted packages are not rolled
   back, and `--skip-duplicate` supports rerunning a partially published release.

There is no unsigned fallback. Missing configuration, expired or untrusted
certificates, a fingerprint mismatch, unavailable timestamp/revocation
services, or Azure authorization/network failures must be corrected before
rerunning. Neither local workflow checks nor an OIDC login alone prove
that real signing works: the first provisioned release must exercise the
HSM, timestamp service, chain validation, and NuGet.org registration.

For a `403` from the vault, check RBAC **and** networking. For OIDC failures,
check issuer, audience, exact environment subject, and environment rules.
For verification failures, inspect the tool output and compare the issued
certificate's SHA-256 fingerprint, trust chain, validity, and revocation
status. Do not disable verification to make publishing succeed.

Track expiry and CA renewal deadlines. A non-integrated CA does not
automatically renew the certificate. Obtain and merge the renewed
certificate using the CA's current requirements, register its public CER
with NuGet.org, and update `NUGET_SIGNING_CERTIFICATE_SHA256` before the next
release. Sign CLI resolves the latest version under the configured
certificate name; a new version intentionally fails verification until
the expected fingerprint is updated. Retain old public certificates as
needed for validation, and follow the CA's revocation/incident procedure
if the signing identity or key is compromised.

## References

- [Sign CLI and its GitHub Actions example](https://github.com/dotnet/sign/blob/main/docs/gh-build-and-sign.yml)
- [Azure login using GitHub OIDC](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure-openid-connect)
- [Key Vault RBAC guidance](https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-guide)
- [Create and merge a Key Vault CSR](https://learn.microsoft.com/en-us/azure/key-vault/certificates/create-certificate-signing-request)
- [NuGet author signing and certificate registration](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package)
