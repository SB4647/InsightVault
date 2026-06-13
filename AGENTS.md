# InsightVault

## Architecture

- Domain
- Application
- Infrastructure
- API

## Dependency Rules

- Domain must not reference Infrastructure.
- Domain must not reference Application.
- Application must not reference Infrastructure.

## Coding Rules

- Never place business logic in controllers.
- Keep controllers thin.
- Use async/await.
- Use dependency injection.
- Follow existing project patterns.

## Testing

- Use xUnit.
- Add tests when changing business logic.

## Workflow

Before making changes:
1. Explain the implementation plan.
2. List files that will change.
3. Mention risks and assumptions.
4. Wait for approval.