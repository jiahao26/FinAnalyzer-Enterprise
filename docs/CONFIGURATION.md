# ⚙️ Configuration Guide

FinAnalyzer Enterprise uses a centralized configuration system (`FinAnalyzer.Engine/appsettings.json`) to manage connections to local AI services. This design allows you to change models, endpoints, or vector parameters **without recompiling** the application.

## 1. appsettings.json Structure

The configuration file is divided into sections for each major service.

```json
{
  "Qdrant": {
    "Host": "localhost",
    "Port": 6334,
    "HttpPort": 6333,
    "VectorSize": 768
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "nomic-embed-text"
  },
  "Tei": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

## 2. Parameter Details

### 🟢 Qdrant (Vector Database)

| Key          | Default     | Description                                                                |
| :----------- | :---------- | :------------------------------------------------------------------------- |
| `Host`       | `localhost` | Hostname of the Qdrant container.                                          |
| `Port`       | `6334`      | **gRPC Port**. Used for high-performance vector upsert/search.             |
| `HttpPort`   | `6333`      | **REST API Port**. Used for health checks, debugging, and collection info. |
| `VectorSize` | `768`       | Dimensionality of embeddings. MUST match the model used (e.g., Nomic=768). |

### 🦙 Ollama (LLM & Embeddings)

| Key         | Default                  | Description                                              |
| :---------- | :----------------------- | :------------------------------------------------------- |
| `BaseUrl`   | `http://localhost:11434` | Endpoint for the local Ollama instance.                  |
| `ModelName` | `nomic-embed-text`       | The specific model tag to use for generating embeddings. |

### 🔁 TEI (Reranker)

| Key       | Default                 | Description                                                          |
| :-------- | :---------------------- | :------------------------------------------------------------------- |
| `BaseUrl` | `http://localhost:8080` | Endpoint for the Text Embeddings Inference (TEI) reranker container. |

## 3. How to Change Configuration

1.  Navigate to the build output directory (e.g., `FinAnalyzer_Enterprise/FinAnalyzer.UI/bin/Debug/net8.0`).
2.  Open `appsettings.json` in any text editor.
3.  Modify values (e.g., change `VectorSize` to 1024 or `Host` to a LAN IP).
4.  Restart the application.

## 4. Prompt Engineering

Prompts are also externalized. You can find them in the `Prompts/` folder within the build directory.

- **FinancialAnalysis.txt**: The master instruction prompt for the Semantic Kernel agent.
