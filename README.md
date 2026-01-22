# Blog Application - Blazor WebAssembly & .NET 8 Web API

A full-featured blog application built with Blazor WebAssembly for the frontend and .NET 8 Web API for the backend, using Supabase for authentication.

## Features

### Authentication & User Management
- User Registration/Login via Supabase Auth
- User Profiles with avatar, bio, and social links
- Role-based Access Control (Admin, Editor, Author, Reader)
- Password Reset via email
- Email Verification

### Content Management
- Post Creation/Editing with Markdown support
- Draft System for saving posts before publishing
- Post Categories for organizing content
- Tagging System with multiple tags per post
- Featured Images for posts
- Post Scheduling for future publication
- SEO Optimization with meta descriptions, titles, and URL slugs

### Content Display & Navigation
- Homepage Feed with pagination
- Post Detail Pages with reading time calculation
- Category Pages for filtered content
- Tag Pages for tag-based filtering
- Search Functionality across posts
- Archive System (monthly/yearly)
- Related Posts suggestions

### Interaction Features
- Comments System with nested replies and moderation
- Like/Reaction System for post engagement
- Social Sharing capabilities
- Newsletter Subscription management
- Contact Form functionality

### Administrative Features
- Admin Dashboard with site statistics
- Content Moderation for comments and posts
- User Management with role assignment
- Analytics Dashboard with views and engagement metrics
- Site Settings configuration

## Tech Stack

- **Frontend**: Blazor WebAssembly (.NET 8)
- **Backend**: .NET 8 Web API
- **Database**: SQL Server (LocalDB)
- **Authentication**: Supabase Auth
- **ORM**: Entity Framework Core

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB or SQL Server Express)
- Supabase account (for authentication)

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd blog_app_blazor
```

### 2. Configure Supabase

1. Create a Supabase project at [supabase.com](https://supabase.com)
2. Get your project URL and anon key
3. Update `BlogApp.Api/appsettings.json`:
   ```json
   "Supabase": {
     "Url": "YOUR_SUPABASE_URL",
     "AnonKey": "YOUR_SUPABASE_ANON_KEY"
   }
   ```
4. Update `BlogApp.Client/appsettings.json` with the same values

### 3. Configure Database

The application uses SQL Server LocalDB by default. Update the connection string in `BlogApp.Api/appsettings.json` if needed:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BlogAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### 4. Restore Packages

```bash
dotnet restore
```

### 5. Run Database Migrations

The database will be created automatically on first run. Alternatively, you can use Entity Framework migrations:

```bash
cd BlogApp.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 6. Run the Application

#### Run API (Terminal 1):
```bash
cd BlogApp.Api
dotnet run
```

The API will be available at `https://localhost:7000` (or the port shown in the console).

#### Run Client (Terminal 2):
```bash
cd BlogApp.Client
dotnet run
```

The client will be available at `https://localhost:5001` (or the port shown in the console).

### 7. Update CORS Settings

Make sure the `BlazorClientUrl` in `BlogApp.Api/appsettings.json` matches your client URL:

```json
"BlazorClientUrl": "https://localhost:5001"
```

## Project Structure

```
blog_app_blazor/
├── BlogApp.Api/              # Backend API
│   ├── Controllers/          # API Controllers
│   ├── Data/                 # DbContext
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Models/               # Entity Models
│   ├── Services/             # Business Logic Services
│   └── Program.cs            # Application Entry Point
│
├── BlogApp.Client/           # Frontend Blazor App
│   ├── Layout/               # Layout Components
│   ├── Models/               # Client-side Models
│   ├── Pages/                # Blazor Pages
│   ├── Services/             # Client Services
│   └── Program.cs            # Client Entry Point
│
└── README.md
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `GET /api/auth/me` - Get current user

### Posts
- `GET /api/posts` - Get paginated posts
- `GET /api/posts/{id}` - Get post by ID
- `GET /api/posts/slug/{slug}` - Get post by slug
- `POST /api/posts` - Create new post
- `PUT /api/posts/{id}` - Update post
- `DELETE /api/posts/{id}` - Delete post
- `GET /api/posts/{id}/related` - Get related posts

### Categories
- `GET /api/categories` - Get all categories
- `GET /api/categories/{id}` - Get category by ID
- `POST /api/categories` - Create category (Admin/Editor)
- `PUT /api/categories/{id}` - Update category (Admin/Editor)
- `DELETE /api/categories/{id}` - Delete category (Admin/Editor)

### Tags
- `GET /api/tags` - Get all tags
- `POST /api/tags` - Create tag (Author+)

### Comments
- `GET /api/comments/post/{postId}` - Get post comments
- `POST /api/comments` - Create comment
- `PUT /api/comments/{id}/approve` - Approve comment (Admin/Editor)
- `PUT /api/comments/{id}/reject` - Reject comment (Admin/Editor)
- `DELETE /api/comments/{id}` - Delete comment

### Likes
- `POST /api/likes/post/{postId}` - Toggle like

### Newsletter
- `POST /api/newsletter/subscribe` - Subscribe to newsletter
- `POST /api/newsletter/unsubscribe` - Unsubscribe

### Contact
- `POST /api/contact` - Send contact message

### Admin
- `GET /api/admin/analytics` - Get analytics (Admin)

### Site Settings
- `GET /api/sitesettings` - Get site settings
- `PUT /api/sitesettings` - Update settings (Admin)

## Default Roles

- **Reader**: Can view published posts and comment
- **Author**: Can create and manage own posts
- **Editor**: Can manage all posts and moderate comments
- **Admin**: Full access to all features

## Development Notes

- The application uses Supabase for authentication. Make sure to configure email templates in Supabase dashboard.
- Markdown support is included for post content. Consider adding a rich text editor for better UX.
- Image uploads are handled via URLs. Consider implementing file upload functionality.
- The reading time is calculated based on 200 words per minute.

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
