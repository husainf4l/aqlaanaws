# S3 credential storage

The app stores per-user S3 credentials (access key and secret key) so each user can use their own bucket. How and where those secrets are stored is configurable.

## Default: Data Protection + database

Out of the box, the app uses **ASP.NET Core Data Protection** to encrypt the keys and stores the encrypted blobs in the database (`UserS3Profiles.AccessKeyEncrypted`, `UserS3Profiles.SecretKeyEncrypted`). This is implemented by `DataProtectionCredentialStore` and is suitable for:

- Single-server deployments
- Development and testing
- Environments where the DP key ring is persisted (e.g. shared key storage)

Data Protection keys are machine-bound by default. If you run multiple app instances, configure a **shared key ring** (e.g. in Redis or on a shared file share) so all instances can decrypt the same data.

## Abstraction: `IUserS3CredentialStore`

All credential read/write goes through `IUserS3CredentialStore`:

- **GetAsync(profileId)** – returns decrypted (access key, secret key) for the given profile.
- **SetAsync(profile, accessKey, secretKey)** – stores credentials for the profile (encrypts and updates the profile entity for the default implementation).

You can replace the default store with a custom implementation and register it in `Program.cs`:

```csharp
// builder.Services.AddScoped<IUserS3CredentialStore, DataProtectionCredentialStore>();
builder.Services.AddScoped<IUserS3CredentialStore, YourSecretsManagerCredentialStore>();
```

## Optional: AWS Secrets Manager

For multi-machine or stricter audit requirements you can implement `IUserS3CredentialStore` using **AWS Secrets Manager** (or Azure Key Vault, HashiCorp Vault, etc.):

1. **Create a custom store** that uses the AWS SDK (e.g. `Amazon.SecretsManager`):
   - **GetAsync**: look up a secret by a key such as `s3-profile-{profileId}` (or `s3-profile-{userId}` if one profile per user), parse JSON to get `AccessKey` and `SecretKey`.
   - **SetAsync**: create or update the secret with `{ "AccessKey": "...", "SecretKey": "..." }` (and optionally encrypt with a KMS key).

2. **Keep profile metadata in the database**: bucket name, region, display name, and optionally a **reference** to the secret (e.g. secret name or ARN) instead of storing encrypted blobs. Your store would use that reference to fetch from Secrets Manager.

3. **Config flag**: e.g. `SecretStorage:Provider = "SecretsManager"` and register the appropriate implementation so you can switch without code changes.

4. **Migration**: Existing profiles that use the DB+DP store would need a one-time migration (read via old store or direct DB + DP, write to Secrets Manager, then switch to the new store).

## Optional: Azure Key Vault

Same idea: implement `IUserS3CredentialStore` using `Azure.Security.KeyVault.Secrets`. Store one secret per profile (or per user) and map profile id to the secret name. Register your implementation in DI and optionally drive the choice with configuration.

## Summary

| Mode              | Implementation                  | When to use                          |
|-------------------|----------------------------------|--------------------------------------|
| Default           | `DataProtectionCredentialStore` | Single server, dev, shared DP keys   |
| Secrets Manager   | Custom `IUserS3CredentialStore` | Multi-machine, audit, AWS-centric    |
| Key Vault / Vault | Custom `IUserS3CredentialStore` | Same, for Azure or HashiCorp stacks |

The app does not ship with a built-in Secrets Manager or Key Vault implementation; you add the NuGet packages and the custom store in your solution.
