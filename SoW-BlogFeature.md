# WorkoutTracker Blog Feature Implementation

## Statement of Work (SoW)

This document outlines the implementation plan for adding a blog functionality to the WorkoutTracker application. This feature will allow administrators to create and manage blog posts, while all users (including anonymous visitors) will be able to view the blog content.

## Feature: Blog System for Workout Advice and Community Engagement

### Overview:
The blog system will enable administrators to share workout tips, nutrition advice, success stories, and other fitness-related content with users. This will enhance user engagement, provide additional value, and potentially attract new users to the platform.

### Requirements:
1. Admin-only blog creation and management
2. Public visibility for all blog posts (including anonymous users)
3. Rich text editing for blog content
4. Categorization and tagging of blog posts
5. Comment functionality (optional for future enhancement)
6. Mobile-responsive design
7. Integration with existing user authentication system
8. SEO-friendly URLs and metadata

### Phase 1: Data Models and Infrastructure

1. **Create Blog-related Data Models:**
   - Create `BlogPost` entity:
     - `Id` (Primary Key)
     - `Title` (string, required)
     - `Slug` (string, required, URL-friendly version of title)
     - `Content` (string, required, HTML content)
     - `Summary` (string, optional, plain text summary for previews)
     - `ImageUrl` (string, optional, featured image path)
     - `AuthorId` (Foreign Key to AppUser)
     - `Published` (bool, default false)
     - `PublishedOn` (datetime, nullable)
     - `CreatedOn` (datetime)
     - `UpdatedOn` (datetime, nullable)
     - `ViewCount` (int, default 0)

   - Create `BlogCategory` entity:
     - `Id` (Primary Key)
     - `Name` (string, required)
     - `Slug` (string, required, URL-friendly version)
     - `Description` (string, optional)

   - Create `BlogTag` entity:
     - `Id` (Primary Key)
     - `Name` (string, required)
     - `Slug` (string, required, URL-friendly version)

   - Create many-to-many relationship models:
     - `BlogPostCategory` (for post-category relationships)
     - `BlogPostTag` (for post-tag relationships)

2. **Update Database Context:**
   - Add DbSet properties for new entities to `WorkoutTrackerWebContext`
   - Configure entity relationships and constraints

3. **Create and Apply Database Migrations:**
   - Generate migration: `dotnet ef migrations add AddBlogEntities --context WorkoutTrackerWeb.Data.WorkoutTrackerWebContext`
   - Apply migration: `dotnet ef database update --context WorkoutTrackerWeb.Data.WorkoutTrackerWebContext`

### Phase 2: Core Blog Services

1. **Create Repository and Service Layer:**
   - Implement `IBlogRepository` interface and concrete implementation
   - Implement `IBlogService` interface and concrete implementation with CRUD operations
   - Implement additional services for categories and tags

2. **Create Admin Blog Management:**
   - Implement Razor Pages for admin blog management in Areas/Admin:
     - `/Areas/Admin/Pages/Blog/Index.cshtml` - List all blog posts
     - `/Areas/Admin/Pages/Blog/Create.cshtml` - Create new blog post
     - `/Areas/Admin/Pages/Blog/Edit.cshtml` - Edit existing blog post
     - `/Areas/Admin/Pages/Blog/Delete.cshtml` - Delete blog post
     - `/Areas/Admin/Pages/Blog/Categories/Index.cshtml` - Manage categories
     - `/Areas/Admin/Pages/Blog/Tags/Index.cshtml` - Manage tags

3. **Implement Authorization Policies:**
   - Create blog-specific authorization policy for admin access
   - Apply policy to admin blog management pages

### Phase 3: Public-facing Blog Pages

1. **Create Public Blog Pages:**
   - `/Pages/Blog/Index.cshtml` - Blog home with list of posts
   - `/Pages/Blog/Post.cshtml` - Individual blog post display
   - `/Pages/Blog/Category.cshtml` - Filter posts by category
   - `/Pages/Blog/Tag.cshtml` - Filter posts by tag
   - `/Pages/Blog/Search.cshtml` - Search blog posts

2. **Implement View Components:**
   - Create `RecentPostsViewComponent` for displaying recent posts in sidebars
   - Create `PopularPostsViewComponent` for displaying most viewed posts
   - Create `CategoriesViewComponent` for displaying category list
   - Create `TagsViewComponent` for displaying tag cloud

3. **Create Partial Views for Reusable Components:**
   - `_BlogPostSummary.cshtml` - For post previews
   - `_BlogSidebar.cshtml` - For blog sidebar elements

### Phase 4: Rich Text Editing and Image Uploading

1. **Integrate Rich Text Editor:**
   - Add TinyMCE or CKEditor via libman.json
   - Configure editor with appropriate features
   - Create JavaScript integration for the editor

2. **Implement Image Upload Functionality:**
   - Create controller action for image uploads
   - Implement server-side validation and processing
   - Configure secure storage location for blog images

### Phase 5: Performance Optimization and Testing

1. **Implement Caching:**
   - Add output caching for blog pages
   - Configure cache profiles for different blog page types

2. **SEO Optimization:**
   - Implement proper meta tags for blog posts
   - Create sitemap generation for blog content
   - Ensure friendly URLs with slugs

3. **Performance Testing:**
   - Test blog page load times
   - Optimize database queries
   - Add pagination for blog lists

4. **Cross-browser and Mobile Testing:**
   - Verify responsive design works on all target devices
   - Test on major browsers (Chrome, Firefox, Safari, Edge)

### Phase 6: Integration and Deployment

1. **Update Navigation:**
   - Add blog to main navigation
   - Update site map

2. **Documentation:**
   - Update README.md with blog feature details
   - Update inventory.md with new components
   - Document admin usage instructions

3. **Final Review and Deployment:**
   - Code review
   - Final testing
   - Deploy to production

## Detailed Task Breakdown:

1. Set up data models and database migrations
2. Create repository and service layer for blog functionality
3. Implement admin area blog management pages
4. Configure rich text editor integration
5. Implement image upload functionality
6. Create public-facing blog pages
7. Build view components for blog features
8. Add caching and performance optimization
9. Implement SEO features
10. Update navigation and site integration
11. Perform testing across devices and browsers
12. Update documentation
13. Deploy to production

## Technologies and Libraries:

1. ASP.NET Core 9 Razor Pages
2. Entity Framework Core for data access
3. Bootstrap 5 for responsive UI
4. TinyMCE/CKEditor for rich text editing
5. Output caching for performance
6. Microsoft.Data.SqlClient for database access

## Estimated Timeline:

- **Phase 1 (Data Models):** 1-2 days
- **Phase 2 (Admin Features):** 3-4 days
- **Phase 3 (Public Pages):** 2-3 days
- **Phase 4 (Rich Text):** 1-2 days
- **Phase 5 (Optimization):** 1-2 days
- **Phase 6 (Integration):** 1 day

Total estimated time: 9-14 days
