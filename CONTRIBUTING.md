# Contributing to CosmosPatch

Thank you for considering a contribution!

## How to contribute

1. **Fork** the repository and create a feature branch from `main`.
2. Make your changes. Keep commits focused and atomic.
3. Add or update **unit tests** for any logic you change.
4. Run `dotnet build` and `dotnet test` — both must succeed with no errors.
5. Open a **Pull Request** against `main` with a clear description of the change.

## Code style

- Follow the rules in [.editorconfig](.editorconfig).
- Match the existing Clean Architecture layering: Domain ← Application ← Infrastructure / Console.
- Do not add references from inner layers (Domain, Application) to outer layers (Infrastructure, Console).
- Use `async`/`await` throughout; avoid `.Result` or `.Wait()`.
- Handle `CosmosException` with status-code checks, not bare `Exception`.

## Reporting issues

Open a GitHub Issue with:
- Steps to reproduce
- Expected vs. actual behavior
- .NET SDK version (`dotnet --version`)
- Anonymized Excel sample (if applicable)

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
