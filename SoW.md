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