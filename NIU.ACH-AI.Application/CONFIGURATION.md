# Configuration Guide

This project uses a layered configuration approach to separate sensitive information from version-controlled settings.

## Configuration Files

### 1. `appsettings.json` (Committed to Git)
- Contains non-sensitive default configuration
- Provides the configuration structure
- Safe to commit to version control
- Should contain empty strings or null for sensitive values

### 2. `appsettings.secrets.json` (NOT Committed)
- Contains sensitive information (API keys, endpoints, etc.)
- **This file is ignored by Git and will NOT be committed**
- Overrides values from `appsettings.json`
- Each developer must create their own local copy

### 3. `appsettings.{Environment}.json` (Optional)
- Environment-specific overrides (Development, Production, etc.)
- Can be committed or kept local depending on content

## Setup Instructions

### First-Time Setup

1. **Copy the example secrets file:**
   ```bash
   cp appsettings.secrets.json.example appsettings.secrets.json
   ```

2. **Edit `appsettings.secrets.json`** and replace placeholder values with your actual credentials:
   - Azure OpenAI API keys and endpoints
   - OpenAI API keys
   - Any other sensitive configuration

3. **Verify the file is ignored by Git:**
   ```bash
   git status
   ```
   You should NOT see `appsettings.secrets.json` in the list of changes.

## Configuration Loading Order

The application loads configuration in this order (later sources override earlier ones):

1. `appsettings.json` - Base configuration
2. `appsettings.{Environment}.json` - Environment-specific settings
3. `appsettings.secrets.json` - Local secrets (highest priority)
4. Environment variables - Can override everything

## AI Service Configuration

The application supports three AI service providers:

### Azure OpenAI
```json
"AzureOpenAI": {
  "ApiKey": "your-key",
  "Endpoint": "https://your-resource.openai.azure.com/",
  "DeploymentName": "your-deployment",
  "ModelId": "gpt-4",
  "ServiceId": "azure-openai-service"
}
```

### OpenAI
```json
"OpenAI": {
  "ApiKey": "sk-your-key",
  "ModelId": "gpt-4o",
  "OrganizationId": "org-your-org",
  "ServiceId": "openai-service"
}
```

### Ollama (Local)
```json
"Ollama": {
  "Endpoint": "http://localhost:11434",
  "ModelId": "llama2",
  "ServiceId": "ollama-service"
}
```

## Security Best Practices

- ✅ **DO** commit `appsettings.json` with empty/null sensitive values
- ✅ **DO** commit `appsettings.secrets.json.example` as a template
- ✅ **DO** document required configuration values
- ❌ **DO NOT** commit `appsettings.secrets.json`
- ❌ **DO NOT** commit API keys or sensitive data
- ❌ **DO NOT** share your `appsettings.secrets.json` file

## Troubleshooting

### "Failed to retrieve experiment settings"
- Ensure `appsettings.secrets.json` exists and contains valid Experiments array
- Check that all required fields are populated

### Configuration not loading
- Verify file names are exactly `appsettings.secrets.json` (case-sensitive on Linux/Mac)
- Ensure the file is in the `SemanticKernelPractice` directory
- Check JSON syntax is valid (use a JSON validator)

## Example Experiment Configuration

See `appsettings.secrets.json.example` for a complete example of experiment configuration structure.
