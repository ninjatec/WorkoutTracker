# Statement of Works

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
 - [ ] Create shared workout link system
   - [x] Design and implement ShareToken model with expiry and access controls
   - [ ] Create API endpoints for generating and managing share tokens
   - [ ] Implement secure token validation mechanism
 - [ ] Develop read-only views for shared workout data
   - [ ] Create dedicated controllers and views for shared workout history
   - [ ] Implement read-only reports page for shared access
   - [ ] Add 1RM calculator view for shared access
 - [ ] Add user management for shared access
   - [ ] Create UI for users to manage shared access tokens
   - [ ] Implement ability to revoke access at any time
   - [ ] Add optional expiry date/time for temporary sharing
 - [ ] Enhance security for shared access
   - [ ] Implement rate limiting for shared access endpoints
   - [ ] Create proper authorization middleware for shared routes
   - [ ] Add logging for all shared access events
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