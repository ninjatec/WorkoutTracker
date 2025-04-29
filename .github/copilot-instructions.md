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

## Development Practices
- ALWAYS reference SoW, WiP, README, Inventory and Models to build context
- ALWAYS ensure the codebase compiles without errors before considering a task complete
- ALWAYS use --context flag on dotnet ef commands
- AVOID repeating work already complete
- Use the existing output cache for new pages
- Make NO assumptions - always check the code
- Leave NO TODOs, placeholders, or missing pieces
- Focus on readability over performance
- Fully implement all requested functionality

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
