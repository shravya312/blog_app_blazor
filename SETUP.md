# Setup Guide

## Quick Start

### 1. Prerequisites
- Install .NET 8 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- Install SQL Server LocalDB (included with Visual Studio) or SQL Server Express
- Create a Supabase account at [supabase.com](https://supabase.com)

### 2. Supabase Configuration

1. Create a new project in Supabase
2. Go to Settings > API
3. Copy the following:
   - Project URL
   - Anon/Public Key

4. Update configuration files:
   - `BlogApp.Api/appsettings.json`
   - `BlogApp.Client/appsettings.json`

   Replace `YOUR_SUPABASE_URL` and `YOUR_SUPABASE_ANON_KEY` with your actual values.

### 3. Database Setup

The database will be created automatically on first run. The default connection string uses LocalDB:

```
Server=(localdb)\mssqllocaldb;Database=BlogAppDb;Trusted_Connection=True
```

To use a different SQL Server instance, update the connection string in `BlogApp.Api/appsettings.json`.

### 4. Run the Application

#### Option A: Run Both Projects Separately

**Terminal 1 - API:**
```bash
cd BlogApp.Api
dotnet run
```

**Terminal 2 - Client:**
```bash
cd BlogApp.Client
dotnet run
```

#### Option B: Use Visual Studio
1. Open `BlogApp.sln` in Visual Studio
2. Set multiple startup projects:
   - Right-click solution > Properties > Startup Project
   - Select "Multiple startup projects"
   - Set both BlogApp.Api and BlogApp.Client to "Start"
3. Press F5 to run

### 5. First Run

1. The API will start on `https://localhost:7000` (or similar)
2. The Client will start on `https://localhost:5001` (or similar)
3. Navigate to the client URL in your browser
4. Register a new account
5. The first user registered will have "Reader" role by default
6. To create an Admin user, update the database directly or use the API

### 6. Creating Admin User

After registering your first user, you can promote them to Admin:

**Option 1: Using SQL**
```sql
UPDATE Users SET Role = 3 WHERE Id = 1; -- 3 = Admin
```

**Option 2: Using API (if you have admin access)**
```http
PUT /api/users/{userId}/role
Content-Type: application/json

"Admin"
```

### 7. CORS Configuration

Make sure the `BlazorClientUrl` in `BlogApp.Api/appsettings.json` matches your client URL. Default is:
```json
"BlazorClientUrl": "https://localhost:5001"
```

## Troubleshooting

### Database Connection Issues
- Ensure SQL Server LocalDB is installed
- Check that the connection string is correct
- Verify SQL Server service is running

### Supabase Authentication Issues
- Verify Supabase URL and Anon Key are correct
- Check Supabase project settings
- Ensure email templates are configured in Supabase dashboard

### CORS Errors
- Verify `BlazorClientUrl` matches the actual client URL
- Check that both API and Client are running
- Ensure HTTPS is properly configured

### Build Errors
- Run `dotnet restore` in both projects
- Clear `obj` and `bin` folders
- Ensure .NET 8 SDK is installed

## Next Steps

1. Configure email templates in Supabase for password reset and verification
2. Set up file storage for image uploads (consider Supabase Storage)
3. Configure production database connection string
4. Set up CI/CD pipeline
5. Configure production URLs and CORS settings
