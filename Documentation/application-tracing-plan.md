# Application Tracing Implementation Plan for WorkoutTracker

## Objective
Implement distributed application tracing across the WorkoutTracker application, with all traces sent to the existing Grafana Tempo installation running in the same Kubernetes cluster.

---

## 1. Prerequisites
- Ensure the Tempo service is accessible from the application pods (typically via a Kubernetes service, e.g., `tempo:4317`).
- Confirm that outbound network policies allow traffic from app pods to the Tempo endpoint.
- Identify the correct OTLP (OpenTelemetry Protocol) endpoint for Tempo (usually gRPC on port 4317).

---

## 2. NuGet Packages to Add
- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`
- `OpenTelemetry.Instrumentation.SqlClient`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- (Optional) `OpenTelemetry.Instrumentation.StackExchangeRedis` if Redis tracing is desired

---

## 3. Configuration Steps
### a. Add OpenTelemetry to Dependency Injection
- Register OpenTelemetry tracing in `Program.cs` using the builder pattern.
- Configure the OTLP exporter to point to the Tempo service (e.g., `http://tempo:4317`).
- Set up resource attributes (service name, environment, etc.).
- Enable instrumentation for ASP.NET Core, HTTP, SQL Client, and optionally Redis.

### b. Environment Configuration
- Add OTLP endpoint configuration to `appsettings.json` or use environment variables (recommended for k8s):
  - Example: `OTEL_EXPORTER_OTLP_ENDPOINT=http://tempo:4317`
- Ensure no hardcoded hostnames; use configuration/environment variables for portability.

---

## 4. Code Changes
### a. Instrumentation
- Add OpenTelemetry tracing setup in `Program.cs`.
- Ensure all incoming HTTP requests, outgoing HTTP calls, and SQL queries are automatically traced.
- For custom business logic, use `ActivitySource` to create custom spans where needed (e.g., long-running or critical operations).

### b. Correlation
- Ensure trace and span IDs are included in logs (Serilog enrichment) for cross-correlation.
- Update log output templates to include `{TraceId}` and `{SpanId}`.

---

## 5. Middleware
- (Optional but recommended) Add middleware to enrich traces with user identity, request context, and other useful tags.
- Ensure trace context is propagated across async calls and background jobs (e.g., Hangfire).

---

## 6. Kubernetes & DevOps
- Add OTEL environment variables to deployment manifests or Helm charts:
  - `OTEL_EXPORTER_OTLP_ENDPOINT=http://tempo:4317`
  - `OTEL_SERVICE_NAME=workouttracker`
  - (Optional) `OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production`
- Ensure the application container has the necessary CA certificates if using TLS.

---

## 7. Validation
- Deploy to a non-production environment first.
- Use Grafana to verify traces are being received and visualized in Tempo.
- Check that traces include expected spans for HTTP requests, SQL queries, and any custom operations.
- Confirm that trace IDs in logs match those in Tempo.

---

## 8. Documentation & Handover
- Document the tracing setup in the project README and architecture docs.
- Provide troubleshooting steps for common issues (e.g., no traces, connection errors).
- Ensure all developers are aware of how to add custom spans and tags.

---

## 9. References
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Grafana Tempo Documentation](https://grafana.com/docs/tempo/latest/)
- [OTLP Protocol Spec](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/otlp.md)
