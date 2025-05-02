# Session to WorkoutSession Migration - Migration Plan

## Overview

This document outlines the detailed implementation plan for migrating from the legacy Session model to the WorkoutSession model.

## Timeline and Milestones

| Phase | Description | Estimated Duration | Deliverables |
|-------|-------------|-------------------|--------------|
| 1 | Preparation and Documentation | 2-3 days | Analysis docs, Extended models, DB views |
| 2 | Service Layer Updates | 2-3 days | Updated Services |
| 3 | Controller and Page Updates | 3-4 days | Updated UI components |
| 4 | Data Migration | 1-2 days | Migration scripts, Data verification |
| 5 | Cleanup and Testing | 2-3 days | Test reports, Final code refinements |
| 6 | Deployment | 1 day | Production deployment |

Total estimated time: 2.5 weeks

## Phase 1: Preparation and Documentation

### Tasks

1. **Environment Setup**
   - [x] Create git branch: `feature/session-to-workoutsession-migration`
   - [x] Create `SessionMigration` folder for documentation
   - [x] Document Session and WorkoutSession model comparison
   - [x] Identify all code references to Session model

2. **Extend WorkoutSession Model**
   - [x] Add SessionId foreign key to WorkoutSession to track original Session
   ```csharp
   [ForeignKey("Session")]
   public int? SessionId { get; set; }
   public Session Session { get; set; }
   ```
   - [x] Create EF Core migration for this change:
   ```
   dotnet ef migrations add AddSessionIdToWorkoutSession --context WorkoutTrackerWebContext
   ```
   - [x] Apply migration to development database

3. **Create Database Views**
   - [x] Create a database view that maps WorkoutSession data to Session format
   ```sql
   CREATE OR ALTER VIEW vw_LegacySession AS
   SELECT 
       ws.WorkoutSessionId AS SessionId,
       ws.Name,
       ws.StartDateTime AS datetime,
       ws.StartDateTime,
       ws.EndDateTime AS endtime,
       ws.UserId,
       ws.Description AS Notes
   FROM WorkoutSessions ws;
   ```

4. **Create Data Migration Scripts**
   - [x] Create script to migrate Session data to WorkoutSession

### Deliverables

- [x] Model comparison document 
- [x] Code changes analysis document
- [x] Migration plan document (this document)
- [x] Entity Framework migration for model extension
- [x] Database views SQL script
- [x] Initial data migration script

## Phase 2: Service Layer Updates

### Tasks

1. **Update UserService**
   - [ ] Modify `GetUserSessionsAsync` to use WorkoutSession instead of Session:
   ```csharp
   public async Task<List<Models.WorkoutSession>> GetUserSessionsAsync(int userId, int limit = 20)
   {
       return await _context.WorkoutSessions
           .Where(s => s.UserId == userId)
           .OrderByDescending(s => s.StartDateTime)
           .Take(limit)
           .AsNoTracking()
           .ToListAsync();
   }
   ```

2. **Update VolumeCalculationService**
   - [ ] Refactor volume calculation to use WorkoutExercise and WorkoutSet:
   ```csharp
   public async Task<double> CalculateWorkoutSessionVolumeAsync(int workoutSessionId)
   {
       var workoutExercises = await _context.WorkoutExercises
           .Include(we => we.WorkoutSets)
           .Where(we => we.WorkoutSessionId == workoutSessionId)
           .ToListAsync();
       
       double totalVolume = 0;
       
       foreach (var exercise in workoutExercises)
       {
           foreach (var set in exercise.WorkoutSets)
           {
               if (set.Reps.HasValue && set.Weight.HasValue)
               {
                   totalVolume += set.Reps.Value * set.Weight.Value;
               }
           }
       }
       
       return totalVolume;
   }
   ```

3. **Update CalorieCalculationService**
   - [ ] Modify calorie calculations to use WorkoutSession data

4. **Create Session-WorkoutSession Bridge Service**
   - [ ] Create a service that can provide Session data from WorkoutSession
   - [ ] Implement WorkoutSession data from Session for backward compatibility

### Deliverables

- [ ] Updated UserService
- [ ] Updated VolumeCalculationService
- [ ] Updated CalorieCalculationService
- [ ] Session-WorkoutSession Bridge Service
- [ ] Unit tests for all service changes

## Phase 3: Controller and Page Updates

### Tasks

1. **Update Session Controllers**
   - [ ] Modify SharedController to use WorkoutSession data
   - [ ] Update SharedWorkoutController API endpoints to use WorkoutSession

2. **Update Session Pages**
   - [ ] Modify Pages/Sessions/Index.cshtml and Index.cshtml.cs
   - [ ] Update Pages/Sessions/Details.cshtml and Details.cshtml.cs
   - [ ] Update Edit, Create and Delete pages
   - [ ] Update Pages/Sessions/Reschedule.cshtml and Reschedule.cshtml.cs

3. **Update Shared Session Pages**
   - [ ] Modify Pages/Shared/Session.cshtml and Session.cshtml.cs
   - [ ] Update Pages/Shared/Index.cshtml and Index.cshtml.cs

4. **Update Coach Area Pages**
   - [ ] Update Areas/Coach/Pages/ClientDetail.cshtml.cs to use WorkoutSession

### Deliverables

- [ ] Updated controllers that use WorkoutSession instead of Session
- [ ] Updated Razor Pages to display WorkoutSession data
- [ ] Updated forms that create/edit WorkoutSession records

## Phase 4: Data Migration

### Tasks

1. **Create Script for Missing Records**
   - [ ] Create data migration script to ensure every Session has a WorkoutSession:
   ```sql
   INSERT INTO WorkoutSessions (Name, Description, UserId, StartDateTime, Status, SessionId)
   SELECT s.Name, s.Notes, s.UserId, s.datetime, 'Migrated', s.SessionId
   FROM Session s
   LEFT JOIN WorkoutSessions ws ON ws.SessionId = s.SessionId
   WHERE ws.WorkoutSessionId IS NULL;
   ```

2. **Map Exercises and Sets**
   - [ ] Create script to map Session Sets to WorkoutExercises
   - [ ] Create script to map Set Reps to WorkoutSets

3. **Update Foreign Keys in Related Tables**
   - [ ] Create script to update ShareToken table:
   ```sql
   -- Add WorkoutSessionId column to ShareToken
   ALTER TABLE ShareToken ADD WorkoutSessionId int NULL;
   
   -- Update ShareToken table where SessionId is not null
   UPDATE st
   SET st.WorkoutSessionId = ws.WorkoutSessionId
   FROM ShareToken st
   JOIN WorkoutSessions ws ON ws.SessionId = st.SessionId
   WHERE st.SessionId IS NOT NULL;
   
   -- After verification, remove SessionId column from ShareToken
   -- ALTER TABLE ShareToken DROP COLUMN SessionId;
   ```

4. **Verify Data Consistency**
   - [ ] Create data validation scripts
   - [ ] Run verification to ensure all data has been properly migrated

### Deliverables

- [ ] Data migration scripts for all related tables
- [ ] Data verification scripts
- [ ] Migration execution plan
- [ ] Migration test results

## Phase 5: Cleanup and Testing

### Tasks

1. **Remove Obsolete Code**
   - [ ] Create list of obsolete methods and classes
   - [ ] Remove Session-specific code that's no longer needed
   - [ ] Update documentation to reflect the new model

2. **Comprehensive Testing**
   - [ ] Execute unit tests for all modified services
   - [ ] Perform integration testing of user workflows
   - [ ] Test all UI components for proper data display

3. **Performance Testing**
   - [ ] Benchmark application performance before and after changes
   - [ ] Optimize any identified performance issues

### Deliverables

- [ ] List of removed obsolete code
- [ ] Test reports
- [ ] Performance optimization report

## Phase 6: Deployment

### Tasks

1. **Create Deployment Plan**
   - [ ] Schedule maintenance window
   - [ ] Create detailed deployment steps
   - [ ] Document rollback procedures

2. **Deploy Migration**
   - [ ] Deploy code changes
   - [ ] Run final data migration scripts
   - [ ] Verify all functionality post-deployment

3. **Post-Deployment Tasks**
   - [ ] Monitor application for issues
   - [ ] Create database schema cleanup migration:
   ```
   dotnet ef migrations add RemoveSessionRelatedTables --context WorkoutTrackerWebContext
   ```
   - [ ] After successful verification, apply the cleanup migration

### Deliverables

- [ ] Deployment plan
- [ ] Rollback procedures
- [ ] Post-deployment report

## Rollback Plan

In case of critical issues, the following rollback steps will be executed:

1. **Code Rollback**
   - Revert the feature branch merge
   - Deploy the previous version of the application

2. **Data Preservation**
   - Keep the WorkoutSession records created during migration
   - No rollback of data migration scripts needed as they only add data

3. **Monitoring**
   - Monitor application stability after rollback
   - Analyze issues that caused the rollback

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Data loss during migration | High | Low | Full database backup before migration |
| Feature regression | Medium | Medium | Comprehensive testing before deployment |
| Performance issues | Medium | Low | Performance testing during Phase 5 |
| Incomplete code updates | High | Medium | Code review and thorough regression testing |
| API compatibility issues | Medium | Low | Maintain backward compatible endpoints temporarily |
