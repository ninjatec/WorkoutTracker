# Multi-Container Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                  Kubernetes Cluster                         │
│                                                             │
│  ┌────────────────┐   ┌─────────────────┐  ┌────────────┐   │
│  │ Ambassador     │   │                 │  │            │   │
│  │ API Gateway    │   │  Load Balancer  │  │ Prometheus │   │
│  │                │   │                 │  │ Monitoring │   │
│  └───────┬────────┘   └────────┬────────┘  └────────────┘   │
│          │                     │                            │
│          ▼                     ▼                            │
│  ┌───────────────────────────────────────────────────┐      │
│  │                                                   │      │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐│      │
│  │  │ Web App     │  │ Web App     │  │ Web App     ││      │
│  │  │ Container 1 │  │ Container 2 │  │ Container N ││      │
│  │  │             │  │             │  │             ││      │
│  │  │ ASP.NET Core│  │ ASP.NET Core│  │ ASP.NET Core││      │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘│      │
│  │         │                │                │       │      │
│  └─────────┼────────────────┼────────────────┼───────┘      │
│            │                │                │              │
│            │                │                │              │
│  ┌─────────┴────────────────┴────────────────┴───────┐      │
│  │                                                   │      │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐│      │
│  │  │ Hangfire    │  │ Hangfire    │  │ Hangfire    ││      │
│  │  │ Worker 1    │  │ Worker 2    │  │ Worker N    ││      │
│  │  │             │  │             │  │             ││      │
│  │  │ Background  │  │ Background  │  │ Background  ││      │
│  │  │ Processing  │  │ Processing  │  │ Processing  ││      │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘│      │
│  │         │                │                │       │      │
│  └─────────┼────────────────┼────────────────┼───────┘      │
│            │                │                │              │
│            ▼                ▼                ▼              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                                                     │    │
│  │  ┌─────────────────┐        ┌───────────────────┐   │    │
│  │  │ Redis           │        │ SQL Server        │   │    │
│  │  │                 │        │                   │   │    │
│  │  │ - Distributed   │        │ - Persistent Data │   │    │
│  │  │   Cache         │        │ - Connection      │   │    │
│  │  │ - SignalR       │        │   Pooling         │   │    │
│  │  │   Backplane     │        │ - Hangfire Jobs   │   │    │
│  │  └─────────────────┘        └───────────────────┘   │    │
│  │                                                     │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Communication Flows

1. **Client → Ambassador API Gateway → Web App Containers**
   - HTTP/HTTPS requests with sticky sessions for SignalR
   - Load balanced across multiple containers

2. **Web App Containers → Redis**
   - SignalR backplane for real-time communication between containers
   - Distributed cache for shared session state and report data
   - Health checks and connection resilience via RedisResilienceMiddleware

3. **Web App Containers → SQL Server**
   - Entity Framework Core data access with connection pooling
   - Transactional operations with retry policies
   - Health checks and connection resilience via DbConnectionResilienceMiddleware

4. **Container-to-Container Communication via Redis**
   - Real-time updates for background jobs propagate to all instances
   - Shared cache invalidation across containers
   - Distributed locking for concurrency-sensitive operations

5. **Web App Containers → Hangfire Workers**
   - Job scheduling (web apps) to job processing (workers)
   - Web apps enqueue jobs but don't process them
   - Workers dedicated to background processing with higher resource allocation

## Role-Based Deployments

The application uses a role-based deployment model that separates job processing from web request handling:

1. **Web App Pods**
   - Handle HTTP requests, render UI, and API calls
   - Enqueue background jobs but don't process them
   - Configure with `HANGFIRE_PROCESSING_ENABLED=false`
   - Optimized for frontend responsiveness

2. **Hangfire Worker Pods**
   - Dedicated to background job processing
   - No HTTP traffic handling, only job execution
   - Configure with `HANGFIRE_PROCESSING_ENABLED=true`
   - Higher worker count and resource allocation
   - Can be scaled independently from web pods

This separation provides several benefits:
- Better resource utilization - web pods can focus on handling user requests
- Improved scalability - scale workers based on job queue size
- Enhanced reliability - worker pods can have higher resource limits
- Isolated failures - problems in job processing don't affect web response

## Hangfire Configuration

### Web Pod Configuration
```yaml
env:
- name: HANGFIRE_PROCESSING_ENABLED
  value: "false"
- name: HANGFIRE_WORKER_COUNT
  value: "0"
```

### Worker Pod Configuration
```yaml
env:
- name: HANGFIRE_PROCESSING_ENABLED
  value: "true"
- name: HANGFIRE_WORKER_COUNT
  value: "8"
```

## Resilience Patterns

1. **Circuit Breaker**
   - Prevents cascading failures when downstream services are unavailable
   - Implemented for both Redis and SQL Server connections
   - Auto-recovery when services are restored

2. **Connection Pooling**
   - Optimized database connections with customizable settings
   - Efficient connection reuse across requests
   - Monitored via specialized health checks

3. **Health Monitoring**
   - Kubernetes probes (liveness, readiness, startup)
   - Prometheus metrics for container health
   - Database connection pool health checks
   - Redis connectivity monitoring
   - Hangfire server status monitoring

4. **Graceful Degradation**
   - Fallback mechanisms when Redis is unavailable
   - Local caching as backup for distributed cache
   - API polling fallback when SignalR connections fail

## Scaling Considerations

1. **Horizontal Scaling**
   - Add more Web App containers as load increases
   - Scale Hangfire worker pods based on job queue size
   - SignalR backplane maintains connection consistency
   - Session persistence regardless of container instances

2. **Redis Scaling**
   - Consider Redis cluster for high-volume deployments
   - Sentinel configuration for high availability
   - Redis persistence for data recovery

3. **SQL Server Scaling**
   - Connection pooling optimization for efficient use of resources
   - Consider read replicas for reporting-heavy workloads
   - Database sharding for very large datasets

4. **Worker Pod Scaling**
   - Use Horizontal Pod Autoscaler (HPA) to scale workers based on CPU/memory
   - Consider custom metrics for scaling based on job queue size
   - Separate worker pods for different job types using queue configuration