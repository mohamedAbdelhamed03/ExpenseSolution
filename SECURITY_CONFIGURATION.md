# Security Configuration (Secrets via Environment Variables)

## Policy
- Do not commit secrets to the repository (no plaintext credentials in config, compose files, or code).
- Provide secrets via environment variables (or platform secret storage) using standard ASP.NET Core configuration binding.

## Required Environment Variables
### JWT
- `JwtSettings__Key` (required; at least 32 characters)
- `JwtSettings__Issuer` (required)
- `JwtSettings__Audience` (required)

### Database
- `ConnectionStrings__DefaultConnection` (required in non-Test environments)

### Cloudinary
- `Cloudinary__CloudName` (required)
- `Cloudinary__ApiKey` (required)
- `Cloudinary__ApiSecret` (required)

## Local Development
- Prefer setting environment variables in your shell/profile or via IDE launch profile.
- For Docker compose, set variables in a local `.env` file (do not commit it).

Example `.env` (local only):
- `CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=db;Database=ExpenseDB;User Id=sa;Password=...;TrustServerCertificate=True;`
- `JWTSETTINGS__KEY=...`
- `JWTSETTINGS__ISSUER=ExpenseAPI`
- `JWTSETTINGS__AUDIENCE=ExpenseUsers`
- `CLOUDINARY__CLOUDNAME=...`
- `CLOUDINARY__APIKEY=...`
- `CLOUDINARY__APISECRET=...`
- `MSSQL_SA_PASSWORD=...`

## Deployment Notes
- Render: set environment variables in the service settings; do not add them to `appsettings.json`.
- Azure App Service: set Application Settings with the same variable names.

## Operator Checklist
- App starts successfully with all required variables present.
- App fails fast at startup if required values are missing.
- Logs do not contain tokens, connection strings with passwords, or Cloudinary secrets.

