# Statement of Works

[ ] Update sessions pages to support sequence numbers
 - [ ] Add migration to formally add SequenceNum column to Set table with default value
   - [ ] Create new migration for adding SequenceNum column to the Set table
   - [ ] Set default value to 0 for existing records
   - [ ] Add appropriate index for performance optimization
   - [ ] Update database schema documentation

 - [ ] Enhance Session UI to support exercise ordering
   - [ ] Update Sets/Create.cshtml to include sequence number input
   - [ ] Update Sets/Edit.cshtml to allow changing sequence number
   - [ ] Add drag-and-drop reordering capability to Sessions/Details.cshtml
   - [ ] Add "move up/down" buttons for accessibility
   - [ ] Add batch reordering functionality

 - [ ] Update sorting logic in controllers and views
   - [ ] Modify Sessions/Details.cshtml.cs to use SequenceNum in sorting
   - [ ] Update Set controller logic to maintain sequence numbers when adding/deleting sets
   - [ ] Ensure proper sequence when duplicating sets
   - [ ] Add auto-increment logic for new sets in a session

 - [ ] Implement sequence number management
   - [ ] Create service to handle reordering and renumbering sets
   - [ ] Add API endpoints for AJAX reordering
   - [ ] Implement optimistic concurrency for sequence updates
   - [ ] Cache sequence data for better performance

 - [ ] Enhance reporting to respect sequence order
   - [ ] Update Reports and Calculator pages to display sets in sequence order
   - [ ] Ensure Shared workout views display exercises in correct sequence
   - [ ] Add sequence number column to workout exports

[ ] Migrate to Bootstrap 5
   - [ ] Update DataTables integration from Bootstrap 4 to Bootstrap 5 in Views/Shared/_SharedLayout.cshtml
   - [ ] Ensure all Bootstrap CSS and JS references are using version 5
   - [ ] Update any Bootstrap 4 specific classes in custom CSS to Bootstrap 5 equivalents
   - [ ] Verify all modal dialogs use Bootstrap 5 syntax
   - [ ] Check form components for Bootstrap 5 compatibility
 - [] remove unused pages
   - [ ] Remove /Pages/TestEmail.cshtml and its code-behind file
   - [ ] Remove /Pages/Users/ directory and all its contents as these are redundant with the Admin area
   - [ ] Remove /Pages/BackgroundJobs/Index.cshtml as it duplicates functionality in Controllers
   - [ ] Update any references to the removed pages
   - [ ] Verify all navigation links after removing the unused pages
 
 [ ] Improve code quality
   - [ ] Enable nullable reference types throughout the project
   - [ ] Add XML documentation comments to public APIs
   - [ ] Fix code style issues using .editorconfig rules
   - [ ] Update to latest package versions for all NuGet dependencies
   - [ ] Consider enabling compiler warnings as errors for better code quality
 
 [ ] Migrate MVC to razorpages
   - [x] Migrate BackgroundJobsController to Razor Pages:
     - [x] Create Razor Pages for JobHistory, ServerStatus, and other BackgroundJobs views
     - [x] Update navigation references to point to new Razor Pages
     - [x] Remove MVC controller and views after migration
   - [x] Migrate HangfireDiagnosticsController to Razor Pages:
     - [x] Create equivalent Razor Pages for diagnostics and test jobs
     - [x] Ensure all functionality is preserved in the migration
     - [x] Update references and navigation links
   - [x] Migrate JobStatusController to Razor Pages API:
     - [x] Convert REST API endpoints to Razor Pages handlers with JSON responses
     - [x] Update client-side code to use new endpoints
   - [x] Migrate ShareTokenController to Razor Pages:
     - [x] Create Razor Pages API endpoints using handlers
     - [x] Ensure authentication and authorization are preserved
   - [ ] Update shared layouts to remove redundant MVC-specific layouts:
     - [ ] Consolidate _Layout.cshtml and _SharedLayout.cshtml
     - [ ] Ensure consistent styling and navigation
     - [ ] Remove files assotiated with now replaced MVC function
  

[x] clean up unused code
  - [x] Remove unused methods in BackgroundJobService:
    - [x] ProcessImportAsync (used only for testing)
    - [x] ProcessReportAsync (placeholder method)
    - [x] ValidateHangfireConfiguration (unused)
  - [x] Clean up unused CSS classes in site.css and shared.css
  - [x] Remove unused JavaScript functions in site.js
  - [x] Remove unnecessary commented code across the solution

[x] Add distributed cache using a redis pod as part of the k8s deployment
 - [x] Configure the application to use Redis for distributed caching.
   - [x] Update `Program.cs` to register Redis as the distributed cache provider.

[x] Set the reports pages to use redis cache
 - [x] Identify the reports pages that will use Redis cache.
 - [x] Implement caching logic in the application for the identified reports pages.
   - [x] Add methods to check for cached data before querying the database.
   - [x] Add methods to store data in Redis after fetching from the database.
 - [x] Update the application to invalidate or update the cache when relevant data changes.
 - [x] Test the caching implementation to ensure it improves performance and works as expected.
 - [x] Update `README.md` with details on how the reports pages use Redis cache.
 - [x] Update `inventory.md` to reflect the caching implementation for reports pages.



[ ] Implement workout data sharing functionality
 - [x] Create shared workout link system
   - [x] Design and implement ShareToken model with expiry and access controls
   - [x] Create API endpoints for generating and managing share tokens
   - [x] Implement secure token validation mechanism
 - [x] Develop read-only views for shared workout data
   - [x] Create dedicated controllers and views for shared workout history
   - [x] Implement read-only reports page for shared access
   - [x] Add 1RM calculator view for shared access
   - [x] Fix type conversion issues in shared controllers
 - [x] Add user management for shared access
   - [x] Create UI for users to manage shared access tokens
   - [x] Implement ability to revoke access at any time
   - [x] Add optional expiry date/time for temporary sharing
 - [ ] Build guest access functionality
   - [ ] Create anonymous access mechanism with proper security controls
   - [ ] Implement session tracking for guest users
   - [ ] Add option to convert guest access to registered user
 - [ ] Update documentation
   - [ ] Update README.md with sharing functionality details
   - [ ] Update inventory.md with new sharing components
   - [ ] Create user guide for sharing workout data

[ ] Optimize for multi-container deployment
 - [ ] Implement Redis for distributed session state
   - [ ] Replace SQL Server session state with Redis distributed cache
   - [ ] Update `Program.cs` to use Redis for session state
   - [ ] Configure proper serialization for session state
 - [ ] Enhance Redis deployment for high availability
   - [ ] Update Redis deployment to use Redis Sentinel or Redis Cluster
   - [ ] Configure Redis persistence to prevent data loss
   - [ ] Add Redis health checks for pod readiness and liveness
 - [ ] Extract Hangfire to separate worker containers
   - [ ] Create dedicated Hangfire worker deployment in Kubernetes
   - [ ] Configure worker containers to only process jobs without web UI
   - [ ] Update main application to disable background processing
 - [ ] Implement proper sticky sessions for SignalR
   - [ ] Configure service with session affinity for SignalR connections
   - [ ] Update SignalR configuration for backplane operations
   - [ ] Add connection resilience handling for SignalR clients
 - [ ] Add database connection pooling optimization
   - [ ] Configure connection pooling settings in DbContext
   - [ ] Add connection resiliency with retry policies
   - [ ] Implement circuit breaker pattern for database connections
 - [ ] Update documentation
   - [ ] Update `README.md` with multi-container architecture details
   - [ ] Update `inventory.md` with new components and relationships
   - [ ] Create architecture diagram showing container interactions
