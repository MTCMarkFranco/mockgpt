# Semantic Kernel - MOCKGPT

The `MOCKGPT` is an azure function to simulate a GPT API, Using Azure OpenAI


## Configuring the solution

Ensure you create the following Environment variables in your azure function
```
# OPEN AI Settings
AZURE_OPENAI_URI=https://<YOUR_INSTANCE_NAME>.openai.azure.com
AZURE_OPENAI_API_KEY=abcdefgh123456789
AZURE_OPENAI_MODEL=GPT4

# API Checks
SCOPE_CLAIM=scope123.read

```

To run the application within Visual Studio Code, start a terminal and type in: `func start`.
