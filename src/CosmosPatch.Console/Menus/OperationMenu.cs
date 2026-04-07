using CosmosPatch.Domain.Enums;

namespace CosmosPatch.Console.Menus;

/// <summary>
/// Presents the patch operation type menu and returns the user's selection.
/// </summary>
public static class OperationMenu
{
    private static readonly (OperationType Type, string Label)[] Options = new[]
    {
        (OperationType.PatchProperty,       "Patch Property(s) — set one or more fields from Excel columns"),
        (OperationType.UpdatePartitionKey,  "Update Partition Key — delete + re-insert with new partition key"),
        (OperationType.RemoveChildArrayItem,"Remove Child Array Item — remove array element matching WHERE columns"),
        (OperationType.PatchChildArrayItem, "Patch Child Array Item — update array element matching WHERE columns"),
        (OperationType.UpdateId,            "Update Id — delete + re-insert with new id")
    };

    public static OperationType SelectOperation()
    {
        while (true)
        {
            System.Console.WriteLine("\n=== Select Operation ===");
            for (int i = 0; i < Options.Length; i++)
            {
                System.Console.WriteLine($"  {i + 1}. {Options[i].Label}");
            }
            System.Console.Write("\nEnter option number: ");
            string? input = System.Console.ReadLine();

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= Options.Length)
            {
                return Options[choice - 1].Type;
            }

            System.Console.WriteLine("Invalid selection. Please enter a number between 1 and 5.");
        }
    }
}
