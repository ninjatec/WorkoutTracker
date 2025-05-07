# WorkoutTracker Project Copilot Instructions

## Technical Requirements
- Use .NET Core 9
- Use Razor Pages
- DO NOT use MVC
- Use Bootstrap 5
- Use Microsoft.Data.SqlClient (not System.Data.SqlClient)
- Only use free, open-source NuGet packages
- Make all code and configuration compatible with Linux containers
- Remenber User details are stored accross 2 db contexts
- We run the site on Linux containers. case sensitivity is very important
- The containers a created with dotnet publish and not dockerfile
- Tech Debt should be avoided, but where unavoidable document via techdebt.md file

## Development Practices
- The application is currently in development so dataloss and downtime are acceptable
- ALWAYS reference SoW, WiP, README, Inventory and Models to build context
- ALWAYS ensure the codebase compiles without errors before considering a task complete
- ALWAYS use --context flag on dotnet ef commands
- AVOID repeating work already complete
- Use the existing output cache for new pages
- Make NO assumptions - always check the code
- Leave NO TODOs 
- Leae NO placeholders
- Leave NO Missing pieces
- Focus on readability over performance
- Fully implement all requested functionality
- Use the logs directory files to help with debug
- Use the existing logging framework
- Use the existing model sctructure where possible
- Use the existing data access layer where possible
- Use the existing repository pattern where possible
- Use the existing service layer where possible
- Use the existing view model structure where possible
- Use the existing view structure where possible
- Use the existing controller structure where possible
- Use the existing view component structure where possible
- Use the existing middleware structure where possible
- Use the existing configuration structure where possible
- Use the existing dependency injection structure where possible
- Use the existing authentication structure where possible
- Use the existing authorization structure where possible
- Use the existing caching structure where possible
- Use the existing localization structure where possible


## Architecture Guidelines
- Design for multi-pod Kubernetes deployments
- Use proper connection resilience patterns
- Update README.md after tasks are completed
- Update inventory.md after tasks are completed
- Update SoW.md after tasks are completed

## Code Style
- Please respect prettier preferences when providing code
- Prioritize code readability and maintainability

## Response Approach
- Do not apologize
- NEVER think demo data is the answer
- Be accurate and thorough
- Suggest solutions that might not be explicitly requested—anticipate needs
- Value good arguments over authorities—the source is irrelevant
- Consider new technologies and contrarian ideas, not just conventional wisdom
- Discuss safety only when it's crucial and non-obvious
- If content policy is an issue, provide the closest acceptable response and explain afterward
- Cite sources at the end, not inline
- Split into multiple responses if one response isn't enough to answer the question
- Examine all existing models before creating new ones to avoid duplication
- Prefer extending existing models over creating new ones
