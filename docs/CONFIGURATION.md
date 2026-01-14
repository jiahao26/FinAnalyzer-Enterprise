# ⚙️ Configuration Guide

FinAnalyzer Enterprise uses a **centralized, single-source configuration** (`FinAnalyzer.Engine/appsettings.json`) to manage connections to local AI services. The UI project links to this file, ensuring there is only one configuration to maintain. This design allows you to change models, endpoints, or vector parameters **without recompiling** the application.

> [!IMPORTANT]
> There is only ONE `appsettings.json` file in the entire solution, located at `FinAnalyzer.Engine/appsettings.json`. The UI project references it via a linked file.

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
  "AIServices": {
    "BackendType": "Ollama",
    "ChatEndpoint": "http://localhost:11434",
    "ChatModelId": "llama3:8b-instruct-q4_K_M",
    "EmbeddingEndpoint": "http://localhost:11434",
    "EmbeddingModelId": "nomic-embed-text",
    "ApiKey": ""
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

### 🤖 AI Services (LLM & Embeddings)

| Key                 | Default                     | Description                                                                                        |
| :------------------ | :-------------------------- | :------------------------------------------------------------------------------------------------- |
| `BackendType`       | `Ollama`                    | Only `Ollama` and `OpenAI_Compatible` are supported.                                               |
| `ChatEndpoint`      | `http://localhost:11434`    | The API endpoint for chat (Local Host).                                                            |
| `ChatModelId`       | `llama3:8b-instruct-q4_K_M` | The model ID for chat. **Must match installed Ollama model** (use `ollama list` to check).         |
| `EmbeddingEndpoint` | `http://localhost:11434`    | The API endpoint for embeddings (can be different from Chat).                                      |
| `EmbeddingModelId`  | `nomic-embed-text`          | The specific model tag. **Supports up to 8192 tokens context.**                                    |
| `ApiKey`            | `null`                      | Optional API Key if using a secured OpenAI-compatible endpoint (e.g., LM Studio, vLLM, or OpenAI). |

### 🔁 TEI (Reranker)

| Key       | Default                 | Description                                                          |
| :-------- | :---------------------- | :------------------------------------------------------------------- |
| `BaseUrl` | `http://localhost:8080` | Endpoint for the Text Embeddings Inference (TEI) reranker container. |

> [!NOTE]
> The Reranker model is configured in `docker-compose.yml`, **not** `appsettings.json`.
> Current Model: `cross-encoder/ms-marco-MiniLM-L-6-v2` (Lightweight, CPU-optimized).

## 3. How to Change Configuration

1.  Navigate to the build output directory (e.g., `FinAnalyzer_Enterprise/FinAnalyzer.UI/bin/Debug/net10.0-windows`).
2.  Open `appsettings.json` in any text editor.
3.  Modify values (e.g., change `VectorSize` to 1024 or `Host` to a LAN IP).
4.  Restart the application.

## 4. Prompt Engineering

Prompts are also externalized. You can find them in the `Prompts/` folder within the build directory.

- **FinancialAnalysis.txt**: The master instruction prompt for the Semantic Kernel agent.
