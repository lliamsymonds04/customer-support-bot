# Required Secrets Configuration

This document lists all required and optional secrets for the backend application. These should be configured using `dotnet user-secrets` for local development.

## Required Secrets

### 1. Groq API Configuration

```bash
dotnet user-secrets set "Groq:ApiKey" "your-groq-api-key"
```

**Purpose:** API key for the Groq AI service used by the Semantic Kernel.

### 2. Database Password

```bash
dotnet user-secrets set "DatabasePassword" "your-postgres-password"
```

**Purpose:** Password for PostgreSQL database connection. Replaces the `{DatabasePassword}` placeholder in the connection string.

**Note:** The application can run without a database in in-memory mode, but data will not be persisted.

### 3. JWT Secret Key

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "your-jwt-secret-key"
```

**Purpose:** Secret key used for signing and validating JWT tokens for authentication.

**Recommendation:** Use a strong, randomly generated key (minimum 32 characters).

### 4. Redis Password

```bash
dotnet user-secrets set "Redis:Password" "your-redis-password"
```

**Purpose:** Password for Redis cache connection used for session management.

**Note:** This is optional if your Redis instance doesn't require authentication.

## Optional OAuth Secrets

If you plan to use OAuth authentication with GitHub or Google, configure these secrets:

### GitHub OAuth

```bash
dotnet user-secrets set "Auth:GitHub:ClientId" "your-github-client-id"
dotnet user-secrets set "Auth:GitHub:ClientSecret" "your-github-client-secret"
```

### Google OAuth

```bash
dotnet user-secrets set "Auth:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Auth:Google:ClientSecret" "your-google-client-secret"
```

## Quick Setup

To set up all required secrets at once, run these commands in the `backend` directory:

```bash
cd backend

# Required secrets
dotnet user-secrets set "Groq:ApiKey" "your-groq-api-key"
dotnet user-secrets set "DatabasePassword" "your-postgres-password"
dotnet user-secrets set "JwtSettings:SecretKey" "your-jwt-secret-key"
dotnet user-secrets set "Redis:Password" "your-redis-password"

# Optional OAuth secrets (if needed)
dotnet user-secrets set "Auth:GitHub:ClientId" "your-github-client-id"
dotnet user-secrets set "Auth:GitHub:ClientSecret" "your-github-client-secret"
dotnet user-secrets set "Auth:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Auth:Google:ClientSecret" "your-google-client-secret"
```

## Viewing Configured Secrets

To list all currently configured secrets:

```bash
dotnet user-secrets list
```
