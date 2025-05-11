# Interactive Progress Dashboard Implementation Plan

## 1. Data Layer Analysis

### 1.1 Available Data Sources
- **WorkoutSession**: Primary data source
  - Start/End times for duration calculations
  - Calorie data (already calculated)
  - Completion status
  - Links to exercises and sets

- **WorkoutExercise**: Exercise level data
  - Exercise type information
  - Start/End times per exercise
  - Set collections

- **WorkoutSet**: Detailed set data
  - Weight and rep data for volume calculations
  - RPE data for intensity tracking
  - Completion status

### 1.2 Existing Services
- **VolumeCalculationService**
  - Can be used directly for volume metrics
  - Already includes exercise-specific calculations
  - Handles total session volume

- **CalorieCalculationService**
  - Provides calorie calculations
  - Includes intensity multipliers
  - Can break down calories by exercise

### 1.3 Required New Components

#### Repository
```csharp
public interface IDashboardRepository
{
    Task<IEnumerable<WorkoutSession>> GetUserSessionsAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Dictionary<string, decimal>> GetVolumeByExerciseTypeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Dictionary<DateTime, int>> GetWorkoutCountByDateAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Dictionary<string, List<WorkoutSet>>> GetPersonalBestsAsync(int userId, DateTime startDate, DateTime endDate);
}
```

#### Service
```csharp
public interface IDashboardService
{
    Task<DashboardMetrics> GetDashboardMetricsAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<ChartData>> GetVolumeProgressChartDataAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<ChartData>> GetWorkoutFrequencyChartDataAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<PersonalBest>> GetPersonalBestsAsync(int userId, DateTime startDate, DateTime endDate);
}
```

#### View Models
```csharp
public class DashboardMetrics
{
    public int TotalWorkouts { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalCalories { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public Dictionary<string, decimal> VolumeByExercise { get; set; }
    public List<PersonalBest> PersonalBests { get; set; }
}

public class ChartData
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; }
}

public class PersonalBest
{
    public string ExerciseName { get; set; }
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public DateTime AchievedDate { get; set; }
}
```

## 2. Required Database Queries

### 2.1 Workout Metrics
```sql
SELECT 
    COUNT(*) as TotalWorkouts,
    SUM(DATEDIFF(MINUTE, StartDateTime, EndDateTime)) as TotalMinutes,
    SUM(CaloriesBurned) as TotalCalories
FROM WorkoutSessions
WHERE UserId = @userId 
AND StartDateTime BETWEEN @startDate AND @endDate
AND IsCompleted = 1
```

### 2.2 Volume Progress
```sql
SELECT 
    CAST(ws.StartDateTime AS DATE) as [Date],
    SUM(wset.Weight * wset.Reps) as DailyVolume
FROM WorkoutSessions ws
JOIN WorkoutExercises we ON ws.WorkoutSessionId = we.WorkoutSessionId
JOIN WorkoutSets wset ON we.WorkoutExerciseId = wset.WorkoutExerciseId
WHERE ws.UserId = @userId 
AND ws.StartDateTime BETWEEN @startDate AND @endDate
GROUP BY CAST(ws.StartDateTime AS DATE)
ORDER BY [Date]
```

### 2.3 Personal Bests
```sql
SELECT 
    et.Name as ExerciseName,
    MAX(ws.Weight) as MaxWeight,
    ws.Reps
FROM WorkoutSets ws
JOIN WorkoutExercises we ON ws.WorkoutExerciseId = we.WorkoutExerciseId
JOIN ExerciseTypes et ON we.ExerciseTypeId = et.ExerciseTypeId
JOIN WorkoutSessions wses ON we.WorkoutSessionId = wses.WorkoutSessionId
WHERE wses.UserId = @userId
AND wses.StartDateTime BETWEEN @startDate AND @endDate
GROUP BY et.Name, ws.Reps
```

## 3. Cache Strategy

### 3.1 Output Cache Policy
```csharp
public static class DashboardCachePolicy
{
    public const string PolicyName = "DashboardCache";
    public const int CacheDurationMinutes = 5;
    
    public static void Configure(OutputCacheOptions options)
    {
        options.AddPolicy(PolicyName, builder =>
        {
            builder.Expire(TimeSpan.FromMinutes(CacheDurationMinutes))
                   .SetVaryByQuery("startDate", "endDate")
                   .Tag("dashboard")
                   .Tag("user");
        });
    }
}
```

### 3.2 Cache Invalidation Rules
- Invalidate on new workout completion
- Invalidate on workout deletion
- Invalidate on workout update
- Per-user cache segregation

## 4. UI Components Required

### 4.1 Chart Components
- Line chart for volume progress
- Bar chart for workout frequency
- Pie chart for exercise distribution
- Progress indicators for goals

### 4.2 Filter Controls
- Date range picker
- Exercise type filter
- Metric toggles

### 4.3 Data Display Components
- Metric cards for key statistics
- Personal best cards
- Recent activity list
- Export controls

## 5. Mobile Optimization Strategy

### 5.1 Responsive Breakpoints
```css
/* Mobile First Breakpoints */
@media (min-width: 576px) { /* Small devices */ }
@media (min-width: 768px) { /* Medium devices */ }
@media (min-width: 992px) { /* Large devices */ }
@media (min-width: 1200px) { /* Extra large devices */ }
```

### 5.2 Mobile Considerations
- Stack charts vertically on mobile
- Collapsible sections for better space usage
- Touch-friendly controls
- Simplified filters for mobile
- Optimized data loading for mobile networks

## 6. Performance Optimization

### 6.1 Data Loading
- Implement lazy loading for historical data
- Use aggregated data for long date ranges
- Cache frequently accessed metrics
- Implement data pagination where appropriate

### 6.2 UI Performance
- Debounce filter changes
- Progressive loading of charts
- Optimize chart redraws
- Minimize DOM updates

## 7. Error Handling

### 7.1 Data Validation
- Validate date ranges
- Handle missing or incomplete data
- Provide fallback values for calculations

### 7.2 Error States
- Loading states for async operations
- Error messages for failed data retrieval
- Fallback UI for missing data
- Retry mechanisms for failed requests

## Next Steps
1. Create DashboardRepository and DashboardService implementations
2. Set up frontend Razor Pages and layouts
3. Implement Chart.js integration
4. Add filter controls and event handlers
5. Implement mobile-responsive design
6. Add export functionality
7. Set up caching and performance monitoring
