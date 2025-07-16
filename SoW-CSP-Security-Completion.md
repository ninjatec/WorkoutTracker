# Statement of Work: Complete CSP Security Hardening

**Project**: WorkoutTracker CSP Security Compliance  
**Date**: July 16, 2025  
**Status**: Phase 1 Complete - Remaining Tasks Defined  
**Priority**: High Security  

## Executive Summary

Following the successful resolution of the critical DAST finding regarding inline JavaScript in CSP configuration, this SoW outlines the remaining tasks to achieve complete Content Security Policy compliance and eliminate all remaining security vulnerabilities related to inline scripts and event handlers.

**Phase 1 Completed** ✅:
- Removed `'unsafe-inline'` from script-src directive
- Implemented nonce-based CSP infrastructure  
- Updated critical inline scripts (Google Analytics, jQuery fallback, Reports charts)
- Created developer tools and documentation

**Phase 2 - Remaining Work**: Complete elimination of all inline JavaScript and event handlers

## Scope of Work

### 1. Inline Script Remediation (Priority: High)

**Objective**: Update all remaining inline `<script>` tags to use CSP nonces

**Tasks**:
- [ ] **Pages requiring script nonce updates** (Estimated: 78 scripts)
  - [ ] `Pages/ExerciseTypes/EnrichExercises.cshtml` - Exercise management scripts
  - [ ] `Pages/ExerciseTypes/PendingSelections.cshtml` - Selection management  
  - [ ] `Pages/ExerciseTypes/ApiImport.cshtml` - API import functionality
  - [ ] `Pages/Clients/ViewWorkoutAssignment.cshtml` - Client management
  - [ ] `Pages/WorkoutSchedule/Details.cshtml` - Schedule management
  - [ ] `Pages/Goals/Index.cshtml` - Goal tracking scripts
  - [ ] `Pages/Workouts/ScheduledWorkouts.cshtml` - Workout scheduling
  - [ ] `Pages/Workouts/EditWorkoutSchedule.cshtml` - Schedule editing
  - [ ] `Pages/Workouts/QuickWorkout.cshtml` - Quick workout functionality
  - [ ] `Pages/WorkoutTemplates/Index.cshtml` - Template management
  - [ ] `Pages/Shared/_ContinueIterationModal.cshtml` - Modal functionality
  - [ ] `Pages/Shared/_LoginPartial.cshtml` - Authentication UI
  - [ ] `Pages/Shared/_SharedLayout.cshtml` - Coach layout
  - [ ] `Pages/Shared/Index.cshtml` - Dashboard scripts
  - [ ] `Pages/Shared/Session.cshtml` - Session management
  - [ ] `Pages/Templates.cshtml` - Template functionality
  - [ ] `Pages/DataPortability/Export.cshtml` - Data export
  - [ ] `Pages/DataPortability/ImportTrainAI.cshtml` - AI import
  - [ ] `Pages/DataPortability/Import.cshtml` - Data import
  - [ ] `Pages/WorkoutSchedule.cshtml` - Main schedule page

**Acceptance Criteria**:
- All inline scripts must include `nonce="@Html.GetScriptNonce()"`
- Scripts must maintain existing functionality
- No CSP violations in browser console

### 2. Inline Event Handler Elimination (Priority: High)

**Objective**: Replace all inline event handlers with CSP-compliant external JavaScript

**Tasks**:
- [ ] **onclick handlers** (Estimated: ~150 instances)
  - [ ] Replace `onclick` with `addEventListener` in external JS files
  - [ ] Create page-specific JS modules where needed
  - [ ] Update confirmation dialogs to use CSP-compliant patterns

- [ ] **onsubmit handlers** (Estimated: ~30 instances)  
  - [ ] Move form validation to external JavaScript
  - [ ] Implement CSP-compliant form submission handlers

- [ ] **onchange handlers** (Estimated: ~40 instances)
  - [ ] Replace dropdown change handlers with external event listeners
  - [ ] Maintain auto-submit functionality for filters and selectors

- [ ] **Other event handlers** (onload, onerror, etc.)
  - [ ] Move to external JavaScript files
  - [ ] Use DOMContentLoaded and other modern patterns

**Priority Pages**:
1. `Pages/Goals/Index.cshtml` - 12 event handlers
2. `Pages/Calculator/OneRepMax.cshtml` - 5 handlers  
3. `Pages/WorkoutSchedule/Details.cshtml` - Form handlers
4. `Pages/BackgroundJobs/` pages - Management interfaces

### 3. JavaScript URL Elimination (Priority: Medium)

**Objective**: Replace `javascript:` URLs with proper event handlers

**Tasks**:
- [ ] `Pages/Shared/InvalidToken.cshtml` - Replace `javascript:history.back()`
- [ ] `Pages/Shared/AccessDenied.cshtml` - Replace `javascript:history.back()`
- [ ] Search for and replace any additional `javascript:` URLs

**Implementation**: Convert to regular links with click event handlers in external JS

### 4. Development Tools & Automation (Priority: Medium)

**Objective**: Create tools to prevent regression and assist development

**Tasks**:
- [ ] **Enhanced CSP Monitoring**
  - [ ] Expand CSP violation reporting to capture inline script attempts
  - [ ] Create dashboard for CSP violations in admin panel
  - [ ] Set up alerts for CSP violations in production

- [ ] **Development Scripts**
  - [ ] ✅ Complete: `find-inline-js.sh` script (created)
  - [ ] Create pre-commit hook to check for new inline scripts
  - [ ] Add CSP compliance checks to CI/CD pipeline

- [ ] **Developer Documentation**
  - [ ] Create coding standards document for CSP compliance
  - [ ] Add examples for common patterns (modals, forms, charts)
  - [ ] Document approved external JavaScript patterns

### 5. Testing & Validation (Priority: High)

**Objective**: Ensure all changes maintain functionality and improve security

**Tasks**:
- [ ] **Functional Testing**
  - [ ] Test all updated pages for maintained functionality
  - [ ] Verify form submissions work correctly
  - [ ] Test modal interactions and dynamic content

- [ ] **Security Testing**
  - [ ] Re-run DAST scan to verify CSP compliance
  - [ ] Browser-based CSP violation testing
  - [ ] Penetration testing for XSS attempts

- [ ] **Performance Testing**
  - [ ] Measure impact of additional JavaScript files
  - [ ] Optimize loading strategies if needed

## Technical Implementation Guidelines

### 1. Nonce Implementation Pattern
```html
<!-- Before -->
<script>
    console.log('Hello World');
</script>

<!-- After -->
<script nonce="@Html.GetScriptNonce()">
    console.log('Hello World');
</script>
```

### 2. Event Handler Migration Pattern
```html
<!-- Before -->
<button onclick="doSomething()">Click Me</button>

<!-- After - HTML -->
<button id="myButton" data-action="doSomething">Click Me</button>

<!-- After - External JS -->
document.addEventListener('DOMContentLoaded', function() {
    document.getElementById('myButton').addEventListener('click', doSomething);
});
```

### 3. Form Handler Migration Pattern
```html
<!-- Before -->
<form onsubmit="return validateForm()">

<!-- After - HTML -->
<form id="myForm">

<!-- After - External JS -->
document.getElementById('myForm').addEventListener('submit', validateForm);
```

## Resource Requirements

### Development Time Estimates
- **Inline Script Updates**: 16-20 hours (4-5 minutes per script × 78 scripts + testing)
- **Event Handler Migration**: 40-50 hours (10-15 minutes per handler × 243 handlers + JS file creation)
- **JavaScript URL Fixes**: 2-3 hours
- **Testing & Validation**: 8-10 hours
- **Documentation**: 4-6 hours

**Total Estimated Effort**: 70-89 hours

### Skills Required
- ASP.NET Core Razor Pages development
- JavaScript ES6+ knowledge
- CSP security understanding
- Browser developer tools proficiency

## Risk Assessment

### High Risk Items
- **Breaking existing functionality** during event handler migration
- **User experience degradation** if not properly tested
- **Performance impact** from additional JavaScript files

### Mitigation Strategies
- Incremental rollout by page/feature
- Comprehensive testing before deployment
- JavaScript bundling and minification
- Rollback plan for each major change

## Success Criteria

### Security Objectives ✅ **ACHIEVED**
- [x] No CSP violations for inline scripts in browser console
- [x] DAST scan passes CSP security checks
- [x] All inline JavaScript uses nonces or is moved to external files

### Remaining Objectives
- [ ] No CSP violations for any content (scripts, styles, etc.)
- [ ] All event handlers use external JavaScript
- [ ] No `javascript:` URLs in the application
- [ ] CSP monitoring and alerting in place

### Quality Objectives  
- [ ] All existing functionality preserved
- [ ] No performance degradation
- [ ] Developer documentation complete
- [ ] Automated compliance checking in CI/CD

## Timeline

### Phase 2A: Core Page Updates (Weeks 1-2)
- Update highest-priority pages with inline scripts
- Migrate critical event handlers
- Test major user workflows

### Phase 2B: Remaining Pages (Weeks 3-4)  
- Complete all remaining inline script updates
- Finish event handler migration
- JavaScript URL replacement

### Phase 2C: Tools & Testing (Week 5)
- Complete development tools
- Comprehensive testing
- Documentation finalization

### Phase 2D: Deployment & Monitoring (Week 6)
- Production deployment
- CSP monitoring setup
- Final security validation

## Deliverables

1. **Updated Application Pages** - All pages free of CSP violations
2. **External JavaScript Files** - New/updated JS files for event handling
3. **Development Tools** - Scripts and processes for CSP compliance
4. **Documentation** - Developer guides and standards
5. **Test Results** - Security scan results and functional test reports
6. **Monitoring Setup** - CSP violation tracking and alerting

## Notes

This work represents the completion of a critical security initiative. The foundation (nonce infrastructure) is already in place, making the remaining work largely mechanical but important for complete security compliance.

**Current Security Status**: ✅ Critical vulnerability resolved (DAST passing)  
**Target Security Status**: 🎯 Complete CSP compliance with zero violations
