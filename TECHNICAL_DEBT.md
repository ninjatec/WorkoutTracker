# Technical Debt Report: WorkoutTracker

_Last updated: 2025-05-09_

## Overview
This document summarizes the current technical debt in the WorkoutTracker application, based on analysis of the codebase, documentation, and inventory. It highlights known issues, areas for improvement, and recommendations for future refactoring or cleanup.

---

## 1. **Entity Framework & Database Schema**

### 1.1. **Model Relationship Issues**
- **Status:** _Most previously reported issues are marked as FIXED in `inventory.md`._
- **Current Issues:**
  - [ ] **Database Schema Migration Issues** (see `inventory.md`):
    - Some columns referenced in code do not exist in the database schema:
      - `Notes` column in `WorkoutSessions` table
      - `Name` column in `WorkoutExercises` table
    - `ExerciseTypes` table is referenced in code but missing in the database.
  - [ ] **Obsolete Session Model**: Legacy `Session` model and related code still exist in some places (see `SessionMigration/code-changes.md`).
    - Razor Pages, controllers, and services referencing `Session` need to be fully migrated to `WorkoutSession`.
    - Remove obsolete `Session`-related code and update documentation accordingly.

### 1.2. **Shadow Property Conflicts**
- **Status:** _Middleware and utilities exist to detect shadow property issues._
- **Current Issues:**
  - [ ] **Manual Review Required**: Continue to monitor for shadow property conflicts during model changes. Ensure all relationships are explicitly configured to avoid EF Core shadow property creation.

---

## 2. **Codebase Cleanup**

### 2.1. **Obsolete Code & Dead Files**
- [ ] Remove any unused or obsolete files, especially after major migrations (see `dbmerge.md`, `SessionMigration/migration-plan.md`).
- [ ] Remove or archive `ApplicationDbContext.cs` and old migration files if no longer needed.

### 2.2. **Documentation Consistency**
- [ ] Ensure all documentation (`README.md`, `inventory.md`, `SoW.md`) is updated after major refactors or migrations.
- [ ] Remove references to deprecated models or features.

---

## 3. **Testing & Coverage**
- [ ] Comprehensive unit and integration tests are required for all new and refactored services, especially after the Session → WorkoutSession migration.
- [ ] UI and workflow integration tests should be expanded for new features (see `SessionMigration/migration-plan.md`).

---

## 4. **Configuration & Containerization**
- [ ] Validate all configuration files for Linux container compatibility (case sensitivity, pathing, etc.).
- [ ] Ensure all connection strings and secrets are managed securely (see `README.md` on secrets management).
- [ ] Remove any hardcoded or environment-specific configuration from source code.

---

## 5. **Performance & Resilience**
- [ ] Continue to monitor and optimize database queries after model changes.
- [ ] Review and tune connection resilience patterns (circuit breaker, pooling) for multi-pod Kubernetes deployments.
- [ ] Benchmark application performance after major migrations (see `SessionMigration/migration-plan.md`).

---

## 6. **Feature Parity & UI Consistency**
- [ ] Ensure all features available in the legacy Session model are present in the new WorkoutSession implementation.
- [ ] Review all Razor Pages for consistency in using Bootstrap 5 and output caching.
- [ ] Complete dark mode/theming toggle and ensure cache invalidation works as intended (see `SoW.md`).

---

## 7. **General Recommendations**
- [ ] Regularly review and update the technical debt report after each major release.
- [ ] Use the existing logging and health check frameworks to proactively detect issues.
- [ ] Continue to follow best practices for EF Core relationship configuration (see `/Documentation/migrations/RelationshipBestPractices.md`).

---

## References
- [`inventory.md`](inventory.md)
- [`README.md`](README.md)
- [`SoW.md`](SoW.md)
- [`SessionMigration/code-changes.md`](SessionMigration/code-changes.md)
- [`dbmerge.md`](dbmerge.md)
- [`Documentation/migrations/RelationshipBestPractices.md`](Documentation/migrations/RelationshipBestPractices.md)

---

**This report should be reviewed and updated regularly as technical debt is addressed or new issues are discovered.**
