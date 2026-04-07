using CosmosPatch.Application.Strategies;
using CosmosPatch.Application.Utilities;
using CosmosPatch.Console.IO;
using CosmosPatch.Console.Logging;
using CosmosPatch.Console.Menus;
using CosmosPatch.Console.Progress;
using CosmosPatch.Domain.Enums;
using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Infrastructure.Cosmos;
using CosmosPatch.Infrastructure.Storage.Excel;
using CosmosPatch.Infrastructure.Storage.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CosmosPatch.Console;

internal static class Program
{
    static async Task Main()
    {
        FileLogger logger = new();

        try
        {
            // ── Collect startup inputs ──
            string endpointUrl = ConsoleInputReader.GetCosmosEndpointUrl();
            string accountKey = ConsoleInputReader.GetCosmosAccountKey();
            string excelFilePath = ConsoleInputReader.GetInputExcelFilePath();

            OperationType operationType = OperationMenu.SelectOperation();

            // ── Initialize logger ──
            CosmosPatch.Domain.Enums.AppEnvironment detectedEnv = EnvironmentDetector.Detect(endpointUrl);
            string env = detectedEnv.ToString();
            string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "CosmosPatch";
            logger.CreateLog($"{env} - {appName}", append: true, showBeginHeader: true);

            // ── PROD safety gate ──
            if (detectedEnv == CosmosPatch.Domain.Enums.AppEnvironment.PROD)
            {
                logger.WriteMessageOnConsole($"\n⚠️  WARNING: The endpoint URL indicates a PRODUCTION environment.");
                logger.WriteMessageOnConsole("This operation will mutate live data. Type CONFIRM to proceed, or press Enter to exit:");
                string? confirmation = System.Console.ReadLine();
                if (!string.Equals(confirmation?.Trim(), "CONFIRM", StringComparison.Ordinal))
                    throw new OperationCanceledException("User did not confirm PRODUCTION operation. Exiting safely.");
            }

            // ── Initialize Cosmos client ──
            logger.WriteMessageOnConsole("\nConnecting to Cosmos DB...");
            CosmosClientManager clientManager = new();
            clientManager.Initialize(endpointUrl, accountKey);
            logger.WriteMessageOnConsole("Cosmos DB connected successfully.");

            // ── Discover databases ──
            CosmosDatabaseDiscovery discovery = new(clientManager);
            List<string> databases = await discovery.GetDatabasesAsync();

            string databaseName = databases.Count switch
            {
                0 => throw new InvalidOperationException("No databases found in this Cosmos account."),
                1 => SelectSingle("database", databases[0], logger),
                _ => SelectorMenu.SelectItem("Select Database", databases, logger)
            };

            // ── Discover containers ──
            List<string> containers = await discovery.GetContainersAsync(databaseName);
            if (containers.Count == 0)
                throw new InvalidOperationException($"No containers found in database '{databaseName}'.");

            string containerName = containers.Count == 1
                ? SelectSingle("container", containers[0], logger)
                : SelectorMenu.SelectItem("Select Container", containers, logger);

            logger.WriteMessageOnConsole($"\nDatabase: {databaseName}  |  Container: {containerName}");

            // ── Validate file ──
            ConsoleInputReader.VerifyFilePath(excelFilePath);

            // ── Build dependencies ──
            CosmosRepository repository = new(clientManager, databaseName, containerName);
            ExcelDataStore excelStore = new(excelFilePath);
            JsonBackupWriter backupWriter = new(containerName, append: true);
            ConsoleProgressBar progressBar = new(0);

            // ── Run selected strategy ──
            IPatchStrategy strategy = operationType switch
            {
                OperationType.PatchProperty => new PatchPropertyStrategy(repository, excelStore, backupWriter, progressBar, logger),
                OperationType.UpdatePartitionKey => new UpdatePartitionKeyStrategy(repository, excelStore, backupWriter, progressBar, logger),
                OperationType.RemoveChildArrayItem => new RemoveChildArrayItemStrategy(repository, excelStore, backupWriter, progressBar, logger),
                OperationType.PatchChildArrayItem => new PatchChildArrayItemStrategy(repository, excelStore, backupWriter, progressBar, logger),
                OperationType.UpdateId => new UpdateIdStrategy(repository, excelStore, backupWriter, progressBar, logger),
                _ => throw new NotSupportedException($"Operation type '{operationType}' is not supported.")
            };

            await strategy.PatchAsync();

            logger.WriteMessageOnConsole("\nOperation completed successfully.");
        }
        catch (OperationCanceledException)
        {
            logger.WriteMessageOnConsole("\nOperation was cancelled by the user.");
        }
        catch (Exception ex)
        {
            logger.WriteMessageOnConsole($"\nUnhandled error: {ex.Message}");
            logger.WriteMessageOnConsole($"Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            logger.WriteMessageOnConsole("\nExiting CosmosPatch. Press any key...");
            logger.CloseLog(showEndFooter: true);
            System.Console.ReadKey();
        }
    }

    private static string SelectSingle(string label, string value, FileLogger logger)
    {
        logger.WriteMessageOnConsole($"Auto-selected {label}: {value}");
        return value;
    }
}
