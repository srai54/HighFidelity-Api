# Azure Key Vault — Complete Reference

> Comprehensive guide covering Key Vault concepts, RBAC, access policies, IAM, secrets management, .NET integration with `IOptions`, networking, certificates, security best practices, and interview Q&A.

---

## Table of Contents

1. [What is Azure Key Vault?](#what-is-azure-key-vault)
2. [Core Concepts](#core-concepts)
   - [Secrets](#secrets)
   - [Keys](#keys)
   - [Certificates](#certificates)
3. [Access Control Models](#access-control-models)
   - [Azure RBAC (Recommended)](#azure-rbac-recommended)
   - [Vault Access Policies (Legacy)](#vault-access-policies-legacy)
   - [RBAC vs Access Policies Comparison](#rbac-vs-access-policies-comparison)
4. [Authentication](#authentication)
   - [Managed Identity](#managed-identity)
   - [DefaultAzureCredential](#defaultazurecredential)
   - [Service Principal](#service-principal)
5. [.NET Implementation with IOptions](#net-implementation-with-ioptions)
   - [NuGet Packages](#nuget-packages)
   - [Secret Naming Convention](#secret-naming-convention)
   - [Program.cs Setup](#programcs-setup)
   - [IOptions / IOptionsSnapshot / IOptionsMonitor](#ioptions--ioptionssnapshot--ioptionsmonitor)
   - [Custom Secret Manager (Prefix Filtering)](#custom-secret-manager-prefix-filtering)
   - [Complete Example: HighFidelity.Api](#complete-example-highfidelityapi)
6. [Integration with Azure App Service](#integration-with-azure-app-service)
   - [Key Vault References in App Settings](#key-vault-references-in-app-settings)
   - [Managed Identity Setup for App Service](#managed-identity-setup-for-app-service)
7. [Networking](#networking)
   - [Firewall Rules](#firewall-rules)
   - [VNet Service Endpoints](#vnet-service-endpoints)
   - [Private Endpoints](#private-endpoints)
8. [IAM & Managed Identity Deep Dive](#iam--managed-identity-deep-dive)
   - [System-Assigned vs User-Assigned](#system-assigned-vs-user-assigned)
   - [Granting Access to Key Vault](#granting-access-to-key-vault)
9. [Certificates Management](#certificates-management)
   - [Generate vs Import](#generate-vs-import)
   - [Auto-Renewal & Rotation](#auto-renewal--rotation)
   - [App Service Certificate Integration](#app-service-certificate-integration)
10. [Security Best Practices](#security-best-practices)
    - [Soft-Delete & Purge Protection](#soft-delete--purge-protection)
    - [Monitoring & Logging](#monitoring--logging)
    - [Key Rotation](#key-rotation)
    - [Principle of Least Privilege](#principle-of-least-privilege)
11. [Troubleshooting](#troubleshooting)
12. [Interview Questions](#interview-questions)

---

## What is Azure Key Vault?

Azure Key Vault is a cloud service for securely storing and accessing **secrets, encryption keys, and certificates**. It eliminates the need to store sensitive information (connection strings, API keys, passwords) in code, config files, or environment variables.

### Key Benefits

| Benefit | Description |
|---|---|
| **Centralized Secret Storage** | One place for all secrets across environments |
| **Access Control** | Fine-grained permissions via RBAC or access policies |
| **Audit Logging** | All access is logged to Azure Monitor / Log Analytics |
| **Automatic Rotation** | Built-in certificate rotation; programmable secret rotation |
| **HSM Support** | FIPS 140-2 Level 2/3 validated hardware security modules |
| **Geo-Redundancy** | Region replication for disaster recovery |
| **Soft-Delete** | Recoverable deletion with configurable retention period |

### Typical Use Cases

- **Connection strings** — DB, Redis, Service Bus, etc.
- **API keys** — Third-party service credentials
- **JWT signing keys** — Asymmetric keys for token signing
- **TLS/SSL certificates** — App Service / CDN / Application Gateway
- **Storage account keys** — Access keys for Azure Storage
- **Encryption keys** — Customer-managed keys (CMK) for Azure services

---

## Core Concepts

### Secrets

A **secret** is any sensitive text value — connection strings, passwords, API keys.

```text
Key Vault Secret Name:  "DbConnectionString"
Value:                  "Server=prod-db;Database=Sales;User Id=..."
Content Type:           "text/plain" (optional metadata)
```

**Secret Operations:**

| Operation | Description |
|---|---|
| `Get Secret` | Read the current/versioned value |
| `Set Secret` | Create or update a secret |
| `List Secrets` | Enumerate secret names (not values) |
| `Delete Secret` | Soft-delete (recoverable within retention window) |
| `Backup / Restore` | Export/import secret across vaults |

**Versioning:** Each time you update a secret, Key Vault creates a new version. You can reference a specific version (`https://vault.vault.azure.net/secrets/MySecret/abc123`) or the latest (`.../secrets/MySecret`).

### Keys

A **key** is a cryptographic key (software-protected or HSM-protected) used for encryption, signing, or key wrapping.

| Key Type | RSA | EC (Elliptic Curve) | AES (Octet) |
|---|---|---|---|
| **Software** | RSA 2048–4096 | EC P-256 / P-384 / P-521K | AES 128 / 192 / 256 |
| **HSM** | RSA-HSM 2048–4096 | EC-HSM P-256 / P-384 / P-521K | AES-HSM 128 / 192 / 256 |

**Key Operations:**

| Operation | Description |
|---|---|
| `Encrypt / Decrypt` | Protect/unprotect data with a key |
| `Sign / Verify` | Sign data or verify signatures |
| `WrapKey / UnwrapKey` | Protect/unprotect another key (key encryption key) |
| `Rotate` | Create a new key version (with optional rotation policy) |

### Certificates

Key Vault certificates are **X.509 v3 certificates** managed by Key Vault. Key Vault can generate self-signed certificates, import existing ones, or auto-enroll with a Certificate Authority (CA).

**Certificate Policy Controls:**

- **Issuer**: Self-signed, DigiCert, GlobalSign, or custom CA
- **Subject**: `CN=example.com`
- **SANs**: `DNS Name=example.com, DNS Name=www.example.com`
- **Key Type**: RSA or EC
- **Lifetime Action**: Auto-renew at X% of lifetime or X days before expiry
- **Exportable Private Key**: Controlled flag (if enabled, private key can be exported as PFX/PEM)

---

## Access Control Models

Azure Key Vault supports two authorization systems. **Azure RBAC is the recommended and default model** for new vaults created with API version 2026-02-01 and later.

### Azure RBAC (Recommended)

Azure Role-Based Access Control (RBAC) provides a **unified access control model** across all Azure services. It operates on both the **control plane** (manage vault properties) and **data plane** (access secrets/keys/certificates).

**Built-in Roles for Key Vault Data Plane:**

| Role | Permissions | Use Case |
|---|---|---|
| **Key Vault Administrator** | Full data plane access to keys, secrets, certificates | Admins with broad responsibility |
| **Key Vault Secrets User** | Read secret values (`Get`, `List`) | Applications that need to read secrets |
| **Key Vault Secrets Officer** | Full CRUD on secrets (`Get`, `List`, `Set`, `Delete`) | DevOps managing secrets |
| **Key Vault Crypto User** | Use keys for crypto operations (`Encrypt`, `Decrypt`, `Sign`, `Verify`) | Applications that need to encrypt/decrypt |
| **Key Vault Crypto Officer** | Full CRUD on keys | Security team managing keys |
| **Key Vault Certificates Officer** | Full CRUD on certificates | DevOps managing certificates |
| **Key Vault Reader** | Read metadata only (not secret values) | Auditors, monitoring |
| **Key Vault Data Access Administrator** | Manage role assignments for KV data plane roles (with ABAC constraint) | Delegated access management |

**Scope Levels:**
- Management Group
- Subscription
- Resource Group
- Key Vault
- Individual Secret / Key / Certificate (advanced)

```azurecli
# Assign "Key Vault Secrets User" to an App Service managed identity at vault scope
az role assignment create \
  --assignee <managed-identity-object-id> \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/<vault-name>
```

### Vault Access Policies (Legacy)

Access policies are a **Key Vault-native** authorization model. Each policy grants specific permissions (Get, List, Set, Delete, etc.) to a security principal at the vault level only.

**Limitations:**
- Vault-level scope only (no per-secret granularity)
- Maximum 1024 access policies per vault
- No PIM integration
- No Conditional Access support
- No Deny assignments
- A `Contributor` on the vault can grant themselves data access by modifying policies

```azurecli
# Set an access policy for a principal
az keyvault set-policy \
  --name <vault-name> \
  --object-id <principal-object-id> \
  --secret-permissions get list
```

### RBAC vs Access Policies Comparison

| Dimension | Access Policies (Legacy) | Azure RBAC (Recommended) |
|---|---|---|
| **Scope granularity** | Vault-level only | Management group → Subscription → RG → Vault → Individual secret |
| **PIM Integration** | None | Full eligibility & time-bound activation |
| **Conditional Access** | Not supported | Supported via Entra ID |
| **Deny assignments** | Not supported | Supported |
| **Secret-level permissions** | Not supported | Supported |
| **Max entries** | 1024 per vault | 2000 role assignments per scope |
| **Control/Data plane separation** | Weak (Contributor can escalate) | Strong (separate RBAC for control vs data) |
| **Audit** | Azure Activity Log (coarse) | Activity Log + RBAC change events |
| **Default** | Opt-in (deprecated for new vaults) | Default for API 2026-02-01+ |

> **Migration Path:** Use `az keyvault update --name <vault> --enable-rbac-authorization true` after assigning equivalent RBAC roles. Never flip the flag before roles are assigned — it causes immediate outage.

---

## Authentication

### Managed Identity

**Managed Identity** is the recommended authentication method for Azure-hosted applications. Azure automatically manages the identity lifecycle — no credentials to store or rotate.

| Type | Description | Use Case |
|---|---|---|
| **System-Assigned** | Tied to a single resource (App Service, VM, Function). Created/destroyed with the resource. | Single app per resource |
| **User-Assigned** | Standalone identity resource. Can be shared across multiple resources. | Multi-app pattern, pre-provisioned access |

```azurecli
# Enable system-assigned managed identity on an App Service
az webapp identity assign --name <app-name> --resource-group <rg>

# Get the object ID of the managed identity
az webapp identity show --name <app-name> --resource-group <rg> --query principalId
```

### DefaultAzureCredential

`DefaultAzureCredential` chains multiple credential sources and tries each in order until one succeeds:

1. **Environment Credential** — reads from `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`
2. **Workload Identity Credential** — Kubernetes / Azure Container Apps
3. **Managed Identity Credential** — App Service, VM, Functions, etc.
4. **Azure CLI Credential** — user logged in via `az login` (local dev)
5. **Visual Studio / VS Code Credential** — logged-in IDE user
6. **Interactive Browser Credential** — fallback interactive login

**This makes it ideal for dev → production parity** — the same code works locally (Azure CLI) and in Azure (Managed Identity).

### Service Principal

When Managed Identity is unavailable (e.g., on-premises, cross-tenant), use a **Service Principal** with a certificate (preferred) or client secret.

```azurecli
# Create a service principal with a self-signed certificate
az ad sp create-for-rbac --name <sp-name> --create-cert
```

**Prefer certificate credentials over client secrets** — they're more secure and can be rotated independently.

---

## .NET Implementation with IOptions

### NuGet Packages

```xml
<PackageReference Include="Azure.Identity" Version="1.13.+" />
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.3.+" />
```

### Secret Naming Convention

Key Vault secret names **cannot contain colons (`:`)**. The configuration system uses `:` as a section/key delimiter. Use **double dashes (`--`)** instead:

| Key Vault Secret Name | Maps to Config Key | Binds to |
|---|---|---|
| `Jwt--Key` | `Jwt:Key` | `JwtOptions.Key` |
| `Jwt--Issuer` | `Jwt:Issuer` | `JwtOptions.Issuer` |
| `ConnectionStrings--HighFidelity` | `ConnectionStrings:HighFidelity` | Connection string lookup |
| `ApplicationInsights--ConnectionString` | `ApplicationInsights:ConnectionString` | AI telemetry config |

### Program.cs Setup

**Simple setup (all secrets loaded):**

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Key Vault as a configuration source
// Key Vault values override appsettings.json when keys collide
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
    new DefaultAzureCredential());

// ── Services rely on IOptions<T> as before ──
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

var app = builder.Build();
```

**With reload support (IOptionsMonitor):**

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
    new DefaultAzureCredential(),
    new AzureKeyVaultConfigurationOptions
    {
        ReloadInterval = TimeSpan.FromMinutes(10) // Poll vault every 10 min
    });
```

### IOptions / IOptionsSnapshot / IOptionsMonitor

| Interface | Lifetime | Reloads? | Use Case |
|---|---|---|---|
| `IOptions<T>` | Singleton | No*, read once at startup | Static config that doesn't change at runtime |
| `IOptionsSnapshot<T>` | Scoped | Yes, per request | Config that may change, but can accept per-request staleness |
| `IOptionsMonitor<T>` | Singleton | Yes, live reload | Config that must reflect changes immediately (e.g., feature flags) |

*`IOptions<T>` can reload if you use `configuration.GetSection()` + `Bind()` manually, but not out of the box.

**Example with IOptionsMonitor + Key Vault reload:**

```csharp
public class SecretRotationService : BackgroundService
{
    private readonly IOptionsMonitor<JwtOptions> _jwtOptions;

    public SecretRotationService(IOptionsMonitor<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // _jwtOptions.CurrentValue will reflect new values
        // when Key Vault secrets are updated and ReloadInterval triggers
        while (!stoppingToken.IsCancellationRequested)
        {
            var currentKey = _jwtOptions.CurrentValue.Key;
            // Use the current key...
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### Custom Secret Manager (Prefix Filtering)

Use a custom `KeyVaultSecretManager` to filter which secrets to load (e.g., by environment prefix):

```csharp
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

public class PrefixKeyVaultSecretManager : KeyVaultSecretManager
{
    private readonly string _prefix;

    public PrefixKeyVaultSecretManager(string prefix)
    {
        _prefix = $"{prefix}-";
    }

    public override bool Load(SecretProperties secret)
    {
        return secret.Name.StartsWith(_prefix);
    }

    public override string GetKey(KeyVaultSecret secret)
    {
        return secret.Name
            .Substring(_prefix.Length)
            .Replace("--", ConfigurationPath.KeyDelimiter);
    }
}

// Usage:
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{kvName}.vault.azure.net/"),
    new DefaultAzureCredential(),
    new PrefixKeyVaultSecretManager("Production"));
```

### Complete Example: HighFidelity.Api

Here's how this API would integrate Key Vault:

**1. Add packages:**

```xml
<PackageReference Include="Azure.Identity" />
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" />
```

**2. Store secrets in Key Vault:**

| Secret Name | Maps To |
|---|---|
| `Jwt--Key` | `Jwt:Key` |
| `Jwt--Issuer` | `Jwt:Issuer` |
| `ConnectionStrings--HighFidelity` | `ConnectionStrings:HighFidelity` |
| `ApplicationInsights--ConnectionString` | `ApplicationInsights:ConnectionString` |

**3. Update Program.cs:**

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Key Vault as configuration source (must come before IOptions bindings)
var keyVaultName = builder.Configuration["KeyVaultName"]
    ?? throw new InvalidOperationException("KeyVaultName is missing.");
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());

// ── Application Insights ──
builder.Services.AddApplicationInsightsTelemetry();

// ── Controllers ──
builder.Services.AddControllers()
    .AddJsonOptions(options => { /* ... */ });

// ── EF Core ──
var connectionString = builder.Configuration.GetConnectionString("HighFidelity")
    ?? throw new InvalidOperationException("Connection string missing.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3, ...)));

// ── JWT (IOptions pattern — binds from Key Vault seamlessly) ──
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
```

> **Key Point:** No `IOptions` code changes needed. The Key Vault provider feeds into `IConfiguration`, and `IOptions<T>` binds from `IConfiguration`. The binding is **transparent**.

---

## Integration with Azure App Service

### Key Vault References in App Settings

Azure App Service has built-in Key Vault support — you can reference a Key Vault secret directly as an App Setting value.

**Syntax:**
```
@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/MySecret/version)
```

![App Service Key Vault Reference](https://learn.microsoft.com/en-us/azure/app-service/media/app-service-key-vault-references/secret-uri.png)

**Steps (Azure Portal):**
1. App Service → Settings → Configuration
2. Add a new App Setting
3. Set **Value** to `@Microsoft.KeyVault(SecretUri=https://...)`
4. Enable **"Key Vault References"** (if prompted)
5. Ensure App Service has **Managed Identity** with **"Key Vault Secrets User"** role

**Advantages:**
- No code changes required
- Works across all Azure App Service stacks (.NET, Node, Python, Java)
- Automatic rotation when secret updates

### Managed Identity Setup for App Service

```azurecli
# 1. Enable managed identity on the App Service
az webapp identity assign \
  --name <app-name> \
  --resource-group <rg>

# 2. Grant the identity access to Key Vault
identityId=$(az webapp identity show \
  --name <app-name> \
  --resource-group <rg> \
  --query principalId \
  --output tsv)

az role assignment create \
  --assignee "$identityId" \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/.../providers/Microsoft.KeyVault/vaults/<vault>
```

**For .NET code-based approach** (not Key Vault References), the App Service managed identity is automatically picked up by `DefaultAzureCredential` — **zero additional config**.

---

## Networking

### Firewall Rules

Key Vault firewalls control access by IP address or VNet.

```azurecli
az keyvault update \
  --name <vault> \
  --default-action Deny \
  --bypass AzureServices

az keyvault network-rule add \
  --name <vault> \
  --ip-address "203.0.113.0/24"
```

| Setting | Description |
|---|---|
| `--default-action Deny` | Deny all traffic by default |
| `--bypass AzureServices` | Allow trusted Azure services (App Service, Azure DevOps, etc.) through the firewall |
| IP rules | Whitelist specific IP ranges |
| VNet rules | Allow traffic from specific virtual networks/subnets |

### VNet Service Endpoints

Service endpoints restrict Key Vault access to traffic from a specific virtual network.

```azurecli
az keyvault network-rule add \
  --name <vault> \
  --vnet-name <vnet> \
  --subnet <subnet>
```

**Prerequisites:**
- Enable `Microsoft.KeyVault` service endpoint on the subnet
- The subnet must not have any private endpoints

### Private Endpoints

Private Endpoints give Key Vault a **private IP in your VNet**, completely removing public internet exposure.

![Private Endpoint Architecture](https://learn.microsoft.com/en-us/azure/key-vault/media/private-link/architecture.png)

```azurecli
# Create a Private Endpoint for Key Vault
az network private-endpoint create \
  --name <pe-name> \
  --resource-group <rg> \
  --vnet-name <vnet> \
  --subnet <subnet> \
  --private-connection-resource-id /subscriptions/.../providers/Microsoft.KeyVault/vaults/<vault> \
  --group-id vault

# Create a Private DNS Zone entry
az network private-dns zone create \
  --resource-group <rg> \
  --name privatelink.vaultcore.azure.net

az network private-dns link vnet create \
  --resource-group <rg> \
  --zone-name privatelink.vaultcore.azure.net \
  --name <link> \
  --virtual-network <vnet>

az network private-endpoint dns-zone-group create \
  --resource-group <rg> \
  --endpoint-name <pe-name> \
  --name default \
  --private-dns-zone privatelink.vaultcore.azure.net \
  --zone-name privatelink.vaultcore.azure.net
```

**When using Private Endpoints:**
- Disable public network access: `az keyvault update --name <vault> --public-network-access Disabled`
- DNS resolution routes `*.vault.azure.net` → private IP in your VNet
- No public egress traffic for secret access

---

## IAM & Managed Identity Deep Dive

### System-Assigned vs User-Assigned

| Aspect | System-Assigned | User-Assigned |
|---|---|---|
| **Lifecycle** | Tied to resource (deleted when resource is deleted) | Independent (persists until explicitly deleted) |
| **Sharing** | Cannot be shared | Can be shared across multiple resources |
| **Creation** | Automatic (one per resource) | Manual (standalone resource) |
| **Management** | Less overhead | More flexibility |
| **Use Case** | Single app, simple deployments | Multi-app, pre-provisioned access, CI/CD pipelines |

### Granting Access to Key Vault

**Step-by-step for an App Service:**

```azurecli
# 1. Enable system-assigned MI
az webapp identity assign --name MyApp --resource-group MyRG

# 2. Get the principal ID
principalId=$(az webapp identity show --name MyApp --resource-group MyRG --query principalId -o tsv)

# 3. Assign RBAC role (RBAC model)
az role assignment create \
  --assignee "$principalId" \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/.../providers/Microsoft.KeyVault/vaults/MyVault

# --- OR for Access Policy model (legacy) ---
az keyvault set-policy \
  --name MyVault \
  --object-id "$principalId" \
  --secret-permissions get list
```

---

## Certificates Management

### Generate vs Import

| Method | Description |
|---|---|
| **Generate** | Key Vault creates a self-signed or CA-issued certificate based on a policy |
| **Import** | Bring your own certificate (BYOC) — upload PFX or PEM |

**Generate a self-signed certificate:**

```azurecli
az keyvault certificate create \
  --vault-name <vault> \
  --name MyCert \
  --policy "$(az keyvault certificate get-default-policy)"
```

**Import an existing PFX:**

```azurecli
az keyvault certificate import \
  --vault-name <vault> \
  --name MyImportedCert \
  --file ./certificate.pfx \
  --password <pfx-password>
```

### Auto-Renewal & Rotation

Configure a certificate's **lifetime action** to auto-renew before expiry:

```azurecli
az keyvault certificate update \
  --vault-name <vault> \
  --name MyCert \
  --set attributes.expires=<expiry> \
  --set policy.lifetimeActions[0].action=AutoRenew \
  --set policy.lifetimeActions[0].trigger.daysBeforeExpiry=30
```

**Key Vault certificate rotation triggers:**
- **X% lifetime** — e.g., renew at 80% of lifetime
- **X days before expiry** — e.g., renew 30 days before

### App Service Certificate Integration

1. Upload/import certificate to Key Vault
2. In App Service → TLS/SSL settings → Private Key Certificates → Import Key Vault Certificate
3. The App Service references the certificate from Key Vault — no local copy
4. Bind the certificate to a custom domain

---

## Security Best Practices

### Soft-Delete & Purge Protection

**Always enable both:**

```azurecli
az keyvault create \
  --name <vault> \
  --resource-group <rg> \
  --enable-soft-delete true \
  --enable-purge-protection true
```

| Feature | Description |
|---|---|
| **Soft-Delete** | Retains deleted vaults/secrets for 90 days (configurable 7–90). Recoverable. |
| **Purge Protection** | Prevents permanent deletion until retention period expires. Mandatory for compliance. |

> Microsoft enforces soft-delete on all new Key Vaults (cannot be disabled) as of Feb 2025.

### Monitoring & Logging

**Enable diagnostics:**

```azurecli
az monitor diagnostic-settings create \
  --name <diag-name> \
  --resource /subscriptions/.../providers/Microsoft.KeyVault/vaults/<vault> \
  --logs '[{"category":"AuditEvent","enabled":true}]' \
  --workspace <log-analytics-workspace-id>
```

**Key things to monitor:**
- `GetSecret` / `SetSecret` operations (who accessed what)
- `VaultAccessPoliciesSet` (unauthorized policy changes)
- Failed authentication attempts
- Throttling events (429 responses)

### Key Rotation

**For secrets** (connection strings, API keys):
- Use manual rotation via Key Vault + application restart, OR
- Use `IOptionsMonitor` + `ReloadInterval` for hot reload
- Implement a rotation function (Azure Function) that:
  - Creates a new secret version
  - Updates the downstream service
  - Removes old versions

**For keys** (cryptographic keys):
- Set an automatic rotation policy: `az keyvault key rotation-policy update ...`
- Key Vault can auto-rotate RSA/EC keys on a schedule

### Principle of Least Privilege

- **One vault per application per environment** — don't share vaults across apps or environments
- **Grant only the permissions needed** — `Key Vault Secrets User` for readers, not `Key Vault Administrator`
- **Use RBAC over Access Policies** — better granularity and control plane separation
- **Never use Contributor role on a vault** with access policies — it allows privilege escalation
- **Use Private Endpoints** to eliminate public network exposure
- **Monitor and alert** on access pattern anomalies

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `Access denied` when reading secret | Managed Identity lacks `Key Vault Secrets User` role | Assign the role at vault scope |
| `Key vault is behind firewall` | Vault firewall blocks the caller | Add the caller's IP or enable `--bypass AzureServices` |
| `Secret not found in configuration` | Secret name doesn't match naming convention | Use `--` instead of `:` in Key Vault secret names |
| `DefaultAzureCredential` fails locally | Not logged into Azure CLI | Run `az login` |
| `IOptions<T>` values are null/empty | Key Vault config source added **after** `Configure<T>` | Move `AddAzureKeyVault()` **before** options binding |
| Key Vault reference in App Service returns 404 | Reference syntax incorrect or MI lacks permission | Verify `@Microsoft.KeyVault(SecretUri=...)` syntax and RBAC |
| `429 Too Many Requests` | Key Vault throttling | Implement client-side caching; use `ReloadInterval` |
| Role assignment not taking effect | RBAC propagation delay (up to 5 min) | Wait and retry |
| Vault not found after recovery from soft-delete | Role assignments are NOT preserved | Recreate all RBAC role assignments after recovery |

---

## Interview Questions

### Basic Concepts

**Q1: What is Azure Key Vault and why would you use it?**

Azure Key Vault is a managed cloud service for securely storing and accessing secrets, encryption keys, and certificates. You use it to avoid hardcoding sensitive information in application code, config files, or environment variables. It provides centralized access control, audit logging, automatic rotation, and integration with other Azure services.

**Q2: What types of objects can you store in Key Vault?**

Three types: **Secrets** (connection strings, passwords, API keys), **Keys** (cryptographic keys for encryption/signing — software or HSM-backed), and **Certificates** (X.509 v3 certificates with full lifecycle management).

### Access Control

**Q3: What's the difference between Azure RBAC and Key Vault access policies?**

Azure RBAC is a unified Azure-wide authorization system that works at multiple scopes (management group → secret-level), integrates with PIM and Conditional Access, and supports deny assignments. Access policies are a legacy Key Vault-native system limited to vault-level scope with a flat permission set. RBAC is recommended and now the default for new vaults.

**Q4: Why is RBAC more secure than access policies?**

With access policies, a user with `Contributor` on the vault can modify policies to grant themselves data plane access — a privilege escalation path. RBAC separates control plane management from data plane access: `Contributor` can manage the vault but can't read secrets unless explicitly assigned `Key Vault Secrets User`.

**Q5: What is the maximum number of access policies per vault?**

1024. This is a hard limit that teams often hit in CI/CD environments with many service principals. RBAC doesn't have this limitation (2000 role assignments per scope).

### Authentication

**Q6: What is Managed Identity and how does it work with Key Vault?**

Managed Identity is an Azure service principal automatically managed by the platform. When an Azure resource (App Service, VM, Function) has managed identity enabled, it can authenticate to Key Vault without storing credentials. The platform handles token issuance and rotation transparently.

**Q7: What is `DefaultAzureCredential` and when would you use it?**

`DefaultAzureCredential` chains multiple credential sources (Environment, Managed Identity, Azure CLI, Visual Studio, etc.) and tries each in order. It's ideal for development → production parity — the same code works locally (via `az login`) and in Azure (via Managed Identity).

### .NET Implementation

**Q8: How do you integrate Key Vault with the `IOptions` pattern in ASP.NET Core?**

Install `Azure.Identity` and `Azure.Extensions.AspNetCore.Configuration.Secrets`. Call `builder.Configuration.AddAzureKeyVault()` with `DefaultAzureCredential`. Key Vault feeds into the `IConfiguration` system, and `IOptions<T>` binds from `IConfiguration` — no code changes needed in the options classes themselves.

**Q9: How do you handle secret naming when Key Vault doesn't allow colons?**

Use double dashes (`--`) in Key Vault secret names. The configuration provider automatically replaces `--` with `:` (the configuration key delimiter). For example, `Jwt--Key` maps to the config key `Jwt:Key`.

**Q10: What's the difference between `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` in the context of Key Vault?**

`IOptions<T>` is a singleton that reads values once at startup — values are fixed for the app's lifetime. `IOptionsSnapshot<T>` is scoped and re-reads per request (useful for secrets that rotate). `IOptionsMonitor<T>` is a singleton with live reload — combined with `ReloadInterval` on the Key Vault provider, it can pick up secret changes without restarting.

**Q11: How do you set up secret reloading from Key Vault?**

Set `ReloadInterval` on `AzureKeyVaultConfigurationOptions`:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri(vaultUri),
    new DefaultAzureCredential(),
    new AzureKeyVaultConfigurationOptions
    {
        ReloadInterval = TimeSpan.FromMinutes(10)
    });
```

Then use `IOptionsMonitor<T>` to access live-reloaded values.

### Networking

**Q12: How do you secure Key Vault network access?**

Three layers (from most restrictive):
1. **Private Endpoint** — Private IP in your VNet, no public exposure
2. **VNet Service Endpoint** — Restrict to a specific VNet/subnet
3. **Firewall Rules** — Allow only specific IP ranges

Combine with `--default-action Deny` and `--bypass AzureServices` to block public traffic while allowing trusted Azure services.

### Security

**Q13: What are soft-delete and purge protection?**

**Soft-delete** retains deleted vaults/secrets for 90 days, allowing recovery. **Purge protection** prevents permanent deletion during that retention period. Both are critical for compliance and data recovery. Microsoft now enforces soft-delete on all new vaults.

**Q14: What's the best practice for Key Vault per environment?**

**One vault per application per environment.** Never share a vault across development, staging, and production. This isolates blast radius and allows environment-specific access controls.

**Q15: How do you rotate secrets without application downtime?**

Approach depends on the secret type:
- **Connection strings**: Create a new version in Key Vault, set `ReloadInterval`, use `IOptionsMonitor` to pick up changes
- **API keys**: For services that support multiple keys, rotate one at a time (update app, remove old key)
- **Certificates**: Key Vault auto-renewal pre-creates a new cert before expiry; bind by Key Vault URI (no local copy)

### Scenario-Based

**Q16: Your app can't read secrets after deploying to App Service. What do you check?**

1. Is Managed Identity enabled on the App Service?
2. Does the MI have `Key Vault Secrets User` RBAC role at the vault scope?
3. Is the vault behind a firewall? Is the App Service bypassed?
4. Is the `AddAzureKeyVault()` call placed **before** `Configure<T>()` in Program.cs?
5. Check App Service logs for `Access denied` errors.

**Q17: How would you migrate a vault from Access Policies to RBAC without downtime?**

1. Document all existing access policies and principals
2. Create equivalent RBAC role assignments for each principal
3. Test that all applications can still access secrets
4. Flip the switch: `az keyvault update --name <vault> --enable-rbac-authorization true`
5. Validate all applications still work
6. Remove old access policies (optional cleanup step)

**Q18: Your team has 50+ microservices, each needing access to Key Vault. Design the approach.**

Use **User-Assigned Managed Identities** grouped by service boundary. Each microservice gets access to its own vault (one per service per environment). Use RBAC roles at the vault scope. For cross-service secret sharing, use RBAC scoped to individual secrets rather than a shared vault. Use Azure Policy to enforce RBAC-only vaults and audit compliance.

---

> **See also:**
> - [Architecture Decision Records](./ARCHITECTURE.md)
> - [API Testing Walkthrough](./API_TESTING.md)
> - [Running in Visual Studio](./RUNNING_IN_VISUAL_STUDIO.md)
