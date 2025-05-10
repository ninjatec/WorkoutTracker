# Implementation Plan: Dark Mode / Theming Toggle

## Overview
This document outlines the steps required to implement the Dark Mode / Theming Toggle feature as described in the SoW.md file. The implementation will follow the technical requirements and development practices specified for the WorkoutTracker project.

---

## Steps

### 1. Data Model & Persistence
1. Extend the `ApplicationUser` model to include a `ThemePreference` property:
   - Type: `string` or `enum` (values: "light", "dark").
2. Create an EF Core migration to add the `ThemePreference` column to the `AspNetUsers` table:
   ```bash
   dotnet ef migrations add AddThemePreferenceToUser --context WorkoutTrackerWeb.Data.WorkoutTrackerWebContext
   dotnet ef database update --context WorkoutTrackerWeb.Data.WorkoutTrackerWebContext
   ```
3. Update the `ApplicationDbContext` to include the new property.

---

### 2. Service Layer
1. Extend or create a `UserPreferenceService`:
   - Methods:
     - `Task<string> GetThemePreferenceAsync(Guid userId)`
     - `Task SetThemePreferenceAsync(Guid userId, string theme)`
2. Ensure the service uses the repository pattern and is registered in `Program.cs` for dependency injection.
3. Implement logging for service operations.

---

### 3. UI & Razor Layout
1. Modify `Pages/Shared/_Layout.cshtml`:
   - Add a theme toggle control (e.g., a switch or icon button) in the navbar.
   - Bind the `data-theme` attribute on the `<body>` tag to the user's theme preference.
2. Ensure the toggle control triggers a JavaScript function to update the theme dynamically.

---

### 4. JavaScript & UI Behavior
1. Create a JavaScript module in `wwwroot/js/theme-toggle.js`:
   - Handle toggle clicks to switch the `data-theme` attribute on `<body>`.
   - Update Bootstrap utility classes as needed.
   - Make an AJAX call to a Razor Page handler to persist the new preference.
2. Include the script in `_Layout.cshtml`.

---

### 5. Styling & Bootstrap Integration
1. Define CSS custom properties in `wwwroot/css/site.css` under the `[data-theme="dark"]` selector to override colors.
2. Use Bootstrap 5 utility classes (e.g., `.bg-dark`, `.text-light`) conditionally based on the `data-theme` attribute.

---

### 6. Razor Page Handlers
1. Add a handler method in an appropriate Razor Page (e.g., `SharedController` or a dedicated `ThemeController`):
   - `OnPostSetThemeAsync(string theme)`:
     - Validate the input.
     - Call `UserPreferenceService.SetThemePreferenceAsync` to persist the preference.
     - Return a success response.

---

### 7. Caching & Performance
1. Ensure the existing output cache is invalidated when the theme changes.
2. Use cache headers to optimize performance for theme-related assets.

---

### 8. Testing
1. Unit Tests:
   - Test `UserPreferenceService` methods.
   - Test Razor Page handler logic.
2. Integration Tests:
   - Verify end-to-end functionality of the theme toggle.
3. Manual Testing:
   - Test the UI on different devices and browsers.
   - Ensure the theme persists across sessions.

---

### 9. Documentation Updates
1. Update `README.md` to include instructions for using the dark mode feature.
2. Update `inventory.md` with details of the new `ThemePreference` property and related changes.
3. Update `SoW.md` to mark the feature as complete.

---

## Notes
- Follow the coding standards and practices outlined in the project instructions.
- Ensure all changes are compatible with Linux containers.
- Validate that the codebase compiles without errors after implementing the feature.
