# Setup Checklist - Blog Application

## ✅ Already Configured

1. **Supabase Authentication**
   - ✅ URL: `https://iubwoldtjrniuzxmvsfr.supabase.co`
   - ✅ Anon Key: Configured in both API and Client
   - ✅ Authentication service set up

2. **Database Connection**
   - ✅ SQL Server LocalDB connection string configured
   - ✅ Database will be auto-created on first run
   - ✅ Entity Framework Core configured

3. **CORS Configuration**
   - ✅ Client URL configured: `https://localhost:5001`

## 🔧 What You Need to Do

### 1. Verify SQL Server LocalDB is Installed
   - **Windows**: Usually comes with Visual Studio
   - **Check**: Open Command Prompt and run:
     ```cmd
     sqllocaldb info
     ```
   - If not installed, install SQL Server Express or use PostgreSQL option below

### 2. Run the Application
   ```bash
   # Terminal 1 - API
   cd BlogApp.Api
   dotnet run
   
   # Terminal 2 - Client
   cd BlogApp.Client
   dotnet run
   ```

### 3. Database Will Be Created Automatically
   - On first API startup, the database will be created automatically
   - No manual database setup required
   - Tables will be created based on your models

## 🔄 Optional: Switch to PostgreSQL (Supabase Database)

If you want to use Supabase's PostgreSQL database instead of SQL Server:

### Option A: Use Supabase PostgreSQL (Recommended for Production)
1. Get your Supabase database connection string from:
   - Supabase Dashboard → Settings → Database
   - Copy the "Connection string" (URI format)

2. Update `BlogApp.Api/BlogApp.Api.csproj`:
   ```xml
   <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
   ```

3. Update `BlogApp.Api/Program.cs`:
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

4. Update `BlogApp.Api/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=db.iubwoldtjrniuzxmvsfr.supabase.co;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;Port=5432"
   }
   ```

### Option B: Keep SQL Server LocalDB (Current - Good for Development)
   - No changes needed
   - Works out of the box on Windows
   - Database auto-creates on first run

## 📋 Additional Setup (Optional)

### Email Configuration (For Password Reset)
- Configure email templates in Supabase Dashboard
- Settings → Authentication → Email Templates

### File Storage (For Images)
- Currently using URLs for featured images
- Consider Supabase Storage for file uploads
- Or use any cloud storage service (Azure Blob, AWS S3, etc.)

### Production Deployment
- Update connection strings for production database
- Configure production URLs in appsettings
- Set up HTTPS certificates
- Configure environment variables

## ✅ Current Status

- ✅ Supabase authentication configured
- ✅ Database connection configured (SQL Server LocalDB)
- ✅ All NuGet packages installed
- ✅ Project builds successfully
- ✅ Ready to run!

## 🚀 Next Steps

1. **Start the API**: `cd BlogApp.Api && dotnet run`
2. **Start the Client**: `cd BlogApp.Client && dotnet run`
3. **Test Registration**: Navigate to `/register` in the client
4. **Create First Post**: Login and create a post
5. **Promote to Admin**: Update database to set first user as Admin

## ⚠️ Important Notes

- **SQL Server LocalDB** is Windows-only. For Mac/Linux, use PostgreSQL option
- **Database auto-creates** on first run - no manual setup needed
- **Supabase** is only for authentication - your app data is in SQL Server
- **Connection string** is already correct for LocalDB
