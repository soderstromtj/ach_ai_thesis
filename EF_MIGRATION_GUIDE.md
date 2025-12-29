# Entity Framework Database-First Migration Guide

To update the Entity Framework models based on the database schema changes, follow these steps:

## Prerequisites

Before running the scaffolding command, ensure you have the Entity Framework Core tools installed.

1.  **Install dotnet-ef tool**:
    Run the following command to install the tool globally:
    ```powershell
    dotnet tool install --global dotnet-ef
    ```

    If you already have it installed but receive errors about versions, try updating it:
    ```powershell
    dotnet tool update --global dotnet-ef
    ```

    *Note: You may need to restart your terminal after installing the tool for the command to be recognized.*

## Migration Steps

1.  **Apply SQL Migration**:
    Ensure the database has been updated using the provided `migration.sql` script.

2.  **Navigate to Infrastructure Persistence Project**:
    Open your terminal and navigate to the persistence project directory:
    ```powershell
    cd NIU.ACH-AI.Infrastructure.Persistence
    ```

3.  **Run Scaffolding Command**:
    Execute the following command to reverse-engineer the database and update the context and entities.
    *Note: Replace `[CONNECTION_STRING]` with your actual database connection string.*

    **PowerShell (Windows Terminal):**
    ```powershell
    dotnet ef dbcontext scaffold "[CONNECTION_STRING]" Microsoft.EntityFrameworkCore.SqlServer `
      --output-dir Models `
      --context-dir Models `
      --context AchAIDbContext `
      --force `
      --no-onconfiguring
    ```

    **Single Line (Command Prompt / Bash):**
    ```bash
    dotnet ef dbcontext scaffold "[CONNECTION_STRING]" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context-dir Models --context AchAIDbContext --force --no-onconfiguring
    ```

    **Parameters Explanation:**
    *   `--output-dir Models`: Puts the entity classes in the `Models` folder.
    *   `--context-dir Models`: Puts the DbContext class in the `Models` folder (matching existing structure).
    *   `--context AchAIDbContext`: Specifies the name of the DbContext class.
    *   `--force`: Overwrites existing files (necessary to update AgentResponse.cs and AchAIDbContext.cs).
    *   `--no-onconfiguring`: Suppresses generation of the `OnConfiguring` method with the connection string (to keep it secure/configurable via DI).

4.  **Verify Changes**:
    Check `Models/AgentResponse.cs` and `Models/AchAIDbContext.cs` to ensure the new fields and mappings have been generated correctly.
