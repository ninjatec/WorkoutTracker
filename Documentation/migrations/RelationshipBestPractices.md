# Entity Framework Core Relationship Best Practices

## Table of Contents
1. [Common EF Core Relationship Warnings](#common-ef-core-relationship-warnings)
2. [Relationship Configuration Guidelines](#relationship-configuration-guidelines)
3. [Global Query Filter Best Practices](#global-query-filter-best-practices)
4. [OrderBy with First/FirstOrDefault](#orderby-with-firstfirstordefault)
5. [Shadow Properties and Foreign Keys](#shadow-properties-and-foreign-keys)
6. [Required vs Optional Relationships](#required-vs-optional-relationships)
7. [Cascading Delete](#cascading-delete)
8. [Migration Workflow](#migration-workflow)

## Common EF Core Relationship Warnings

### Foreign Key Attributes on Both Navigations Warning
When both sides of a relationship have the `[ForeignKey]` attribute, EF Core separates them into two relationships, which can cause unexpected behavior.

**Warning Message:**
```
Navigations '[Navigation1]' and '[Navigation2]' were separated into two relationships since the [ForeignKey] attribute was specified on navigations on both sides.
```

**Solution:**
Use the `[ForeignKey]` attribute on only one side of the relationship, typically on the dependent entity, or configure relationships in `OnModelCreating` using Fluent API.

### Multiple Relationships Without Configured Foreign Keys
When multiple relationships exist between the same entity types without explicit foreign key configurations, EF Core creates shadow properties with names dependent on the discovery order.

**Warning Message:**
```
There are multiple relationships between '[Entity1]' and '[Entity2]' without configured foreign key properties. This will cause Entity Framework to create shadow properties with names dependent on the discovery order.
```

**Solution:**
Explicitly configure all foreign key properties using Fluent API in the `OnModelCreating` method.

### Global Query Filter with Required Navigation
When an entity has a global query filter and is the required end of a relationship, filtering out the required entity can lead to unexpected results.

**Warning Message:**
```
Entity '[Entity]' has a global query filter defined and is the required end of a relationship with the entity '[RelatedEntity]'. This may lead to unexpected results when the required entity is filtered out.
```

**Solution:**
Either make the navigation optional or define matching query filters for both sides of the relationship.

### First/FirstOrDefault Without OrderBy
Using `First` or `FirstOrDefault` operators without an `OrderBy` clause can lead to unpredictable results since SQL doesn't guarantee consistent ordering without an explicit `ORDER BY`.

**Warning Message:**
```
The query uses the 'First'/'FirstOrDefault' operator without 'OrderBy' and filter operators. This may lead to unpredictable results.
```

**Solution:**
Always include an `OrderBy` clause when using `First` or `FirstOrDefault`.

## Relationship Configuration Guidelines

### One-to-Many Relationships
```csharp
// Configure a one-to-many relationship
modelBuilder.Entity<Parent>()
    .HasMany(p => p.Children)
    .WithOne(c => c.Parent)
    .HasForeignKey(c => c.ParentId)
    .OnDelete(DeleteBehavior.Restrict);
```

### One-to-One Relationships
```csharp
// Configure a one-to-one relationship
modelBuilder.Entity<User>()
    .HasOne(u => u.Profile)
    .WithOne(p => p.User)
    .HasForeignKey<Profile>(p => p.UserId)
    .OnDelete(DeleteBehavior.Cascade);
```

### Many-to-Many Relationships
```csharp
// Configure a many-to-many relationship
modelBuilder.Entity<Student>()
    .HasMany(s => s.Courses)
    .WithMany(c => c.Students)
    .UsingEntity(j => j.ToTable("StudentCourses"));
```

### Self-Referencing Relationships
```csharp
// Configure a self-referencing relationship
modelBuilder.Entity<Employee>()
    .HasOne(e => e.Manager)
    .WithMany(e => e.DirectReports)
    .HasForeignKey(e => e.ManagerId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.Restrict);
```

## Global Query Filter Best Practices

### Defining Global Query Filters

Global query filters should be defined in the `OnModelCreating` method:

```csharp
modelBuilder.Entity<Entity>()
    .HasQueryFilter(e => e.TenantId == _currentTenantId);
```

### Synchronizing Filters for Related Entities

When entities with global query filters are involved in relationships, ensure that both sides of the relationship have compatible filters:

```csharp
// Parent filter
modelBuilder.Entity<Parent>()
    .HasQueryFilter(p => _currentUserId == null || p.UserId == _currentUserId);

// Child filter - ensure access follows the same rules as parent
modelBuilder.Entity<Child>()
    .HasQueryFilter(c => _currentUserId == null || c.Parent.UserId == _currentUserId);
```

### When to Use Required vs. Optional Navigation with Filters

When using global query filters, prefer optional relationships for filtered entities:

```csharp
modelBuilder.Entity<Parent>()
    .HasOne(p => p.Child)
    .WithOne(c => c.Parent)
    .HasForeignKey<Child>(c => c.ParentId)
    .IsRequired(false);  // Make optional to avoid issues with filtered out parents
```

## OrderBy with First/FirstOrDefault

Always use an `OrderBy` clause before `First`, `FirstOrDefault`, `Single`, or `SingleOrDefault` to ensure consistent results:

```csharp
// Incorrect
var result = await context.Users
    .Where(u => u.Email == email)
    .FirstOrDefaultAsync();

// Correct
var result = await context.Users
    .Where(u => u.Email == email)
    .OrderBy(u => u.Id)  // Ensure consistent ordering
    .FirstOrDefaultAsync();
```

## Shadow Properties and Foreign Keys

### Avoiding Shadow Property Conflicts

Explicitly define foreign key properties in your entity models:

```csharp
public class Child
{
    public int Id { get; set; }
    
    // Explicitly defined foreign key property
    public int? ParentId { get; set; }
    
    // Navigation property
    public Parent Parent { get; set; }
}
```

### Configuring Relationships with Shadow Properties

If you prefer shadow properties, explicitly configure them in `OnModelCreating`:

```csharp
modelBuilder.Entity<Child>()
    .HasOne(c => c.Parent)
    .WithMany(p => p.Children)
    .HasForeignKey("ParentId");  // Explicitly name the shadow property
```

## Required vs Optional Relationships

### When to Use Required Relationships

Use required relationships when the dependent entity cannot exist without the principal entity:

```csharp
modelBuilder.Entity<Order>()
    .HasOne(o => o.Customer)
    .WithMany(c => c.Orders)
    .HasForeignKey(o => o.CustomerId)
    .IsRequired(true);  // Order must have a Customer
```

### When to Use Optional Relationships

Use optional relationships when the dependent entity can exist without the principal entity, or when global query filters are in use:

```csharp
modelBuilder.Entity<User>()
    .HasOne(u => u.ProfilePhoto)
    .WithOne()
    .HasForeignKey<ProfilePhoto>(p => p.UserId)
    .IsRequired(false);  // User can exist without a ProfilePhoto
```

## Cascading Delete

### Available Delete Behaviors

- `Cascade`: Dependent entities are deleted when the principal is deleted
- `Restrict`: Prevents deletion of the principal if dependent entities exist
- `SetNull`: Sets foreign key values to null when the principal is deleted
- `NoAction`: No action is taken (similar to Restrict but deferred)
- `ClientSetNull`: Similar to SetNull but performed by EF Core (not the database)
- `ClientCascade`: Similar to Cascade but performed by EF Core

### Recommended Delete Behaviors

```csharp
// For entities that should be deleted together (e.g., Order and OrderItems)
modelBuilder.Entity<Order>()
    .HasMany(o => o.Items)
    .WithOne(i => i.Order)
    .HasForeignKey(i => i.OrderId)
    .OnDelete(DeleteBehavior.Cascade);

// For entities that should remain independent (e.g., Customer and Order)
modelBuilder.Entity<Order>()
    .HasOne(o => o.Customer)
    .WithMany(c => c.Orders)
    .HasForeignKey(o => o.CustomerId)
    .OnDelete(DeleteBehavior.Restrict);
```

## Migration Workflow

### Creating and Applying Migrations

```bash
# Create a new migration
dotnet ef migrations add [MigrationName] --context [DbContextName]

# Apply migrations to the database
dotnet ef database update --context [DbContextName]
```

### Safe Migration Practices

1. **Test migrations on non-production environments** before applying them to production.
2. **Create backup scripts** before migrating production databases.
3. **Avoid data loss migrations** by using multiple migrations for complex changes.
4. **Review migration scripts** before applying them to ensure they don't contain unexpected changes.
5. **Use transactions** to ensure migrations are atomic.

### Migration Rollback Strategy

```bash
# Roll back to a specific migration
dotnet ef database update [MigrationName] --context [DbContextName]

# Roll back all migrations
dotnet ef database update 0 --context [DbContextName]
```

### Managing Multiple DbContexts

For applications with multiple DbContexts (like our WorkoutTracker with ApplicationDbContext and WorkoutTrackerWebContext):

1. **Always specify the --context flag** when running EF Core commands
2. **Maintain separate migrations folders** for each DbContext
3. **Apply migrations in the correct order** when cross-context dependencies exist
4. **Document context relationships** to make the migration process clearer