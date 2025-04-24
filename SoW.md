# Statement of Works
## Admin Functions
[x] Log Level
- [x] Add the Ability to manage the log level and control from a link under the System Settings link from the admin dashboard
 - [x] Implement log level configuration system
   - [x] Create LogLevel configuration model/entity to store settings
   - [x] Add LoggingService to manage log level changes at runtime
   - [x] Implement configuration persistence in database
   - [x] Create interface for logging providers to adapt to level changes
   - [x] Add configuration reload mechanism for real-time changes

 - [ ] Develop admin UI for log management
   - [x] Create LogLevel admin page accessible from System Settings
   - [x] Implement dropdown selection for global log level (Debug, Info, Warning, Error, Critical)
   - [x] Add category-specific log level controls for granular management
   - [ ] Create UI to view recent logs filtered by level and category
   - [ ] Add search functionality to quickly find specific log entries

 - [ ] Enhance logging infrastructure
   - [x] Implement structured logging with Serilog
   - [x] Add log enrichment with contextual information (user, request, etc.)
   - [x] Create log sinks for different output targets (file, console, database)
   - [ ] Implement log rotation and retention policies
   - [ ] Add log compression for archived logs

 - [ ] Implement security and access controls
   - [x] Restrict log management to admin users only
   - [x] Add audit logging for log level changes
   - [ ] Implement role-based permissions for log viewing
   - [ ] Create secure log viewing with sensitive data redaction
   - [ ] Restrict access to certain log categories based on user role

 - [ ] Add monitoring and notification features
   - [ ] Implement alert system for critical log events
   - [ ] Add dashboard for log metrics and trends
   - [ ] Create scheduled log summary reports
   - [ ] Implement log anomaly detection
   - [ ] Add integration with external monitoring systems

 - [ ] Update documentation
   - [x] Document logging architecture in README.md
   - [x] Update inventory.md with new logging components
   - [ ] Create admin guide for log level management
   - [ ] Add developer documentation for logging best practices
   - [ ] Document log format and structure for analysis


---
---

[x] Update the Import page DataPortability/Import to use background jobs worker hangfire
 - [x] Add background job support to standard JSON import functionality
   - [x] Refactor WorkoutDataPortabilityService to support async background processing
   - [x] Create job progress model similar to existing ImportTrainAI implementation
   - [x] Add support for SignalR progress updates in the WorkoutDataPortabilityService
   - [x] Implement proper error handling and job status reporting
   - [x] Update service to support both direct and background processing modes
 
 - [x] Enhance BackgroundJobService to handle JSON imports
   - [x] Add method to queue JSON imports as background jobs 
   - [x] Implement progress tracking for JSON imports
   - [x] Create job completion notification mechanism
   - [x] Add error handling and job failure recovery
   - [x] Ensure proper cleanup of resources after job completion

 - [x] Update Import page UI for background processing
   - [x] Add real-time progress tracking with SignalR
   - [x] Create progress bar and status indicators 
   - [x] Implement connection state display (connected/disconnected)
   - [x] Add fallback polling for job status when SignalR is unavailable
   - [x] Display detailed import progress information

 - [x] Integrate with ImportProgressHub for real-time updates
   - [x] Extend ImportProgressHub to support standard JSON imports
   - [x] Implement client-side SignalR connection in Import.cshtml
   - [x] Add code to register for job-specific updates
   - [x] Handle reconnection scenarios and progress synchronization
   - [x] Ensure proper job group management in the hub

 - [x] Update Import.cshtml.cs page model
   - [x] Refactor OnPostAsync to use background job processing
   - [x] Add JobId property to track background jobs
   - [x] Implement job status checking functionality 
   - [x] Add error message retrieval from failed jobs
   - [x] Provide initial progress update when job starts

 - [x] Implement job status persistence and monitoring
   - [x] Save job metadata for user reference
   - [x] Add job status polling endpoint for fallback scenarios
   - [x] Create job cancellation functionality
   - [x] Implement job cleanup for completed/failed imports
   - [x] Add job history display for users to track past imports

 - [x] Add testing and documentation
   - [x] Create unit tests for background job processing
   - [x] Test with large import files to verify performance
   - [x] Test error scenarios and recovery mechanisms
   - [x] Update inventory.md with new components
   - [x] Document the implementation in code comments

---
---


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

---
---


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

---
---

[x] Optimize for multi-container deployment
 - [x] Implement Redis for distributed session state
   - [x] Replace SQL Server session state with Redis distributed cache
   - [x] Update `Program.cs` to use Redis for session state
   - [x] Configure proper serialization for session state
 - [x] Add database connection pooling optimization
   - [x] Configure connection pooling settings in DbContext
   - [x] Add connection resiliency with retry policies
   - [x] Implement circuit breaker pattern for database connections
 - [x] Update documentation
   - [x] Update `README.md` with multi-container architecture details
   - [x] Update `inventory.md` with new components and relationships
   - [x] Create architecture diagram showing container interactions

---
---

[ ] Add swagger for API endpoints but disable in production
 - [ ] Set up Swagger documentation
   - [ ] Install required NuGet packages (Swashbuckle.AspNetCore)
   - [ ] Configure Swagger in Program.cs with appropriate API information
   - [ ] Add XML documentation file configuration to project file
   - [ ] Set up security definitions for authentication methods
 - [ ] Enhance API endpoints for Swagger
   - [ ] Add appropriate XML documentation comments to API controllers and methods
   - [ ] Configure proper response types and status codes for better Swagger docs
   - [ ] Implement example values for request and response models
   - [ ] Add operation IDs for improved client generation
 - [ ] Configure environment-specific behavior
   - [ ] Enable Swagger UI only in development/test environments
   - [ ] Implement security measures to prevent production access
   - [ ] Add environment tag to API documentation
   - [ ] Configure CORS for Swagger UI in development only
 - [ ] Implement proper authentication in Swagger
   - [ ] Configure JWT authentication flow in Swagger UI
   - [ ] Add OAuth2 configuration for token-based endpoints
   - [ ] Set up proper security requirements for protected endpoints
   - [ ] Test authentication flows through Swagger UI
 - [ ] Create Swagger documentation portal
   - [ ] Style Swagger UI with application branding
   - [ ] Add helpful descriptions and examples for API usage
   - [ ] Configure ReDoc as an alternative documentation viewer
   - [ ] Implement rate-limiting on Swagger endpoints in non-production