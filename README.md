# ACH-AI Overview

This application implements the first three stages of the Analysis of Competing Hypotheses (ACH) process as part of a thesis project at NIU. This application utilizes multiple AI agents (see the appsettings.json file in NIU.ACH-AI.FrontendConsole) orchestrated with the Microsoft Semantic Kernel framework. Additionally, .NET Aspire is used for local container orchestration and RabbitMQ for reliable asynchronous execution of the ACH workflow.

# Architecture

The system consists of the following core components:

1. **AppHost**: The main entry point of the application, responsible for initializing and configuring the RabbitMQ service and FrontendConsole.
2. **Domain**: Contains the core business logic and data models for the ACH process.
3. **Application**: Contains the application data transfer objects, interfaces, and application coordination services.
4. **Infrastructure**: Contains the implementation of the RabbitMQ and SemanticKernel services.
5. **Infrastructure.Database**: Contains the table, views, and stored procedure definitions for the backend SQL Server database.
6. **Infrastructure.Persistence**: Contains the Entity Framework Core DbContext and repository implementations for database access.
7. **ServiceDefaults**: Shared library that centralizes configuration for telemetry, health checks, etc. for .NET Aspire services.

# Requirements

To run this project, you will need the following:

- .NET 8 SDK or later
- Docker Desktop (for running RabbitMQ container)
- SQL Server (for database access)
- An OpenAI API key (for AI agent functionality)

# Running the Application

## 1. Clone the repository and navigate to the project directory.

```bash
git clone https://github.com/soderstromtj/ach_ai_thesis
cd ach_ai_thesis
```
## 2. Configure Secrets

The AppHost project supports an optional appsettings.secrets.json file for setting the RabbitMQ server username and password. This file should be created in the NIU.ACH-AI.AppHost project directory and should contain the following structure:

```json
{
  "RabbitMQ": {
    "User": "admin",
    "Password": "your-secure-password"
  }
}
```
(Note: If you skip this step, the app will default to using guest/guest for RabbitMQ credentials).

Additionally, you will need to set your OpenAI API key in the appsettings.json file of the NIU.ACH-AI.FrontendConsole project:
```json
{
  "AIServiceSettings": {
    "AzureOpenAI": {
      "ApiKey": "<API key here>",
      "Endpoint": "https://<domain>.openai.azure.com/",
      "DeploymentName": "<deployment name>",
      "ApiVersion": "<azure API version>"
    },
    "OpenAI": {
      "ApiKey": "<API key here>",
      "ModelId": "<model id>",
      "OrganizationId": "<org id>"
    },
    "Ollama": {
      "Endpoint": "<endpoint URL>",
      "ModelId": "<model id>"
    },
    "Google": {
      "ApiKey": "<API key here>",
      "ModelId": "<model id>"
    },
    "HttpTimeoutSeconds": 300
  },
  "ConnectionStrings": {
    "AchAiDBConnection": "<database connection string here>",
    "AchAiStateDbConnection": "<database connection string here>"
  }
}
```

## 3. Running the Application

Navigate to the NIU.ACH-AI.AppHost project directory and run the application using the following command:
```bash
cd NIU.ACH-AI.AppHost
dotnet run
```

This will start the AppHost, which will initialize the RabbitMQ service and the FrontendConsole. The application will be ready to process ACH workflows.

## 4. View the Aspire and RabbitMQ Dashboards

Once the application is running, you can access the system dashboards at their dedicated local URLs:

* **.NET Aspire Dashboard:** Open [https://localhost:17271](https://localhost:17271) (or `http://localhost:15091`) in your browser to monitor application logs in real-time, and view distributed traces and metrics.
* **RabbitMQ Management UI:** Open [http://localhost:15672](http://localhost:15672) to access the messaging dashboard.

# Customizing the Experiments

The core ACH workflow is entirely driven by configuration. To modify the scenario, question, or agent behaviors, edit the Experiments section in:
NIU.ACH-AI.FrontendConsole/appsettings.json

Here you can customize:

- KeyQuestion: The primary intelligence question.
- Context: The background intelligence report the agents will parse.
- ACHSteps: The sequence of tasks, agent system prompts, max invocation counts, and LLM assignments (e.g., gpt-5.2, gpt-5-mini, o3).
