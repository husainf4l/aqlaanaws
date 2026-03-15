# S3 Storage Manager

A .NET Razor Pages app to browse and manage your AWS S3 storage: view folders and files, upload, download, delete, and create folders. Use it when the same S3 bucket is shared across many applications and you need a single place to inspect and control contents.

## Features

- **Authentication** – ASP.NET Core Identity: register, log in, log out. Passwords are hashed; lockout after failed attempts. All S3 pages require sign-in.
- **Browse buckets** – List all buckets (if no default is set) or go straight to your default bucket.
- **Browse folders and files** – Navigate by prefix (folders), see file size and last modified.
- **Upload files** – Upload into the current folder.
- **Download files** – Download any object.
- **Delete objects** – Delete files with confirmation.
- **Create folders** – Create new “folders” (prefixes) in the current path.

## Configuration

### Authentication (Identity)

- User data is stored in SQLite by default (`appsettings.json` → `ConnectionStrings:DefaultConnection`, default file `app.db`).
- On first run, the app applies EF Core migrations and creates the database.
- **First use:** open the app, click **Register**, create an account (email + password). Then log in. Password rules: at least 8 characters, digit, lowercase, uppercase, non-alphanumeric.
- To use another database (e.g. SQL Server), change the connection string and add the appropriate EF package (e.g. `Microsoft.EntityFrameworkCore.SqlServer`).

### AWS credentials

The app uses the default AWS credential chain. Use any of:

- **Environment variables**: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, and optionally `AWS_REGION`
- **Shared credentials file**: `~/.aws/credentials` and `~/.aws/config`
- **IAM role** (when running on EC2/ECS/Lambda)

Do not put access keys in `appsettings.json`. Prefer environment variables or the shared credentials file.

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "AWS": {
    "Region": "us-east-1",
    "DefaultBucket": "my-bucket-name"
  }
}
```

- **ConnectionStrings:DefaultConnection** – Database for Identity (default: SQLite `app.db`).
- **AWS:Region** – AWS region for S3 (e.g. `us-east-1`, `eu-west-1`).
- **AWS:DefaultBucket** – Optional. If set, the app opens this bucket on the home page. If empty, the home page lists all buckets and you choose one.

You can override these in `appsettings.Development.json` for local use.

## Run

```bash
dotnet run
```

Open `https://localhost:5xxx` or `http://localhost:5xxx` (see console for the URL). Register a user if the database is new, then log in to use the S3 browser.

## Requirements

- .NET 10 SDK
- AWS account with S3 access; IAM user/role must have at least:
  - `s3:ListAllMyBuckets`
  - `s3:ListBucket`, `s3:GetObject`, `s3:PutObject`, `s3:DeleteObject` on the buckets you use
