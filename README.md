# Secure Enterprise RAG Workstation (FinAnalyzer)

![License](https://img.shields.io/badge/license-MIT-blue) ![.NET](https://img.shields.io/badge/.NET-10.0-purple) ![Architecture](https://img.shields.io/badge/Architecture-Clean-green) ![Status](https://img.shields.io/badge/Status-Phase%205%20In%20Progress-blue)

**FinAnalyzer Enterprise** is a secure, test-driven RAG workstation that solves real business intelligence problems without compromising data sovereignty. Built for financial compliance teams with professional software testing standards, it automates analysis of sensitive 10-K reports using on-premise vector retrieval and air-gapped LLMs—ensuring zero data egress while maintaining enterprise-grade reliability through comprehensive unit and integration testing.

## 📊 Progress Status

| Phase       | Description                                                  | Status             |
| :---------- | :----------------------------------------------------------- | :----------------- |
| **Phase 1** | **Infrastructure & "Hello World"** (Docker, Scaffold, Tests) | ✅ **Complete**    |
| **Phase 2** | **The Data Pipeline** (PDF Parsing, Chunking, Vectors)       | ✅ **Complete**    |
| **Phase 3** | **The RAG Core** (Retrieval, Reranking, Orchestration)       | ✅ **Complete**    |
| **Phase 4** | **The UI Foundation** (WPF Shell, MVVM, Settings)            | 🔄 **In Progress** |
| **Phase 5** | **Integration & Threading** (Async loading, Live Chat)       | 🔄 **In Progress** |
| **Phase 6** | **Advanced Features** (Citations, Chat History)              | ⬜ Pending         |
| **Phase 7** | **Enterprise Hardening** (Logging, Release Packaging)        | ⬜ Pending         |

## 🚀 Quick Start

```bash
# 1. Start Docker services
docker compose up -d

# 2. Build the application
dotnet build

# 3. Run the application
dotnet run --project FinAnalyzer.UI/FinAnalyzer.UI.csproj
```

## 📚 Documentation

| Document                                      | Description                                             |
| :-------------------------------------------- | :------------------------------------------------------ |
| [**🚀 QUICK START**](docs/QUICK_START.md)     | How to set up Docker, models, and run the app.          |
| [**🌟 PHILOSOPHY**](docs/PHILOSOPHY.md)       | The strategic intent and "Secure by Design" principles. |
| [**🛤️ ROADMAP**](docs/ROADMAP.md)             | Detailed feature timeline and phasing.                  |
| [**🏗️ ARCHITECTURE**](docs/ARCHITECTURE.md)   | Structure, Clean Architecture layers, and File Map.     |
| [**⚙️ CONFIGURATION**](docs/CONFIGURATION.md) | Guide to appsettings.json and tuning parameters.        |

---

## 🌟 Project Intent

This project serves as a proof-of-concept for **"Secure, Local-First Enterprise AI"**. Unlike cloud-native wrappers, FinAnalyzer demonstrates how to build production-grade RAG systems that function entirely air-gapped using local containers and quantized models.

_Read more in [PHILOSOPHY.md](docs/PHILOSOPHY.md)._

## 🚀 Key Features

| Feature                    | Description                                                                                 |
| :------------------------- | :------------------------------------------------------------------------------------------ |
| **Air-Gapped by Design**   | Built to run offline (default), with optional support for secure Cloud APIs (Hybrid).       |
| **Clean Architecture**     | Strict separation of concerns (Core, Engine, UI) for maintainability.                       |
| **Hybrid Search Pipeline** | Multistage retrieval using Qdrant (Vector), bge-reranker (Precision), and LLM (Generation). |
| **Auditable Grounding**    | Tailored citations for every claim with direct PDF links.                                   |
| **Compliance Logging**     | Full audit trail of all queries and token usage.                                            |
| **Financial Dashboard**    | High-density WPF interface optimized for analyst workflows.                                 |
| **Token-Based Chunking**   | Smart splitting using GPT-4 compatible tokenizer for optimal LLM context usage.             |
| **Idempotent Ingestion**   | Deterministic chunk IDs ensure specific file versions are never duplicated in Vector DB.    |
| **Streaming Pipeline**     | End-to-end `IAsyncEnumerable` support for instant UI feedback (Typing effect).              |
| **External Config**        | Standardized `appsettings.json` for all environment variables (Hot-swappable).              |

## 🛠️ Technology Stack

| Component        | Technology                    | Descriptions                       |
| :--------------- | :---------------------------- | :--------------------------------- |
| **UI Framework** | **WPF (.NET 10)**             | Modern Desktop (Material Design)   |
| **Orchestrator** | **Microsoft Semantic Kernel** | AI Agent Orchestration             |
| **Vector DB**    | **Qdrant**                    | Local Docker Container             |
| **LLM Backend**  | **Hybrid (Ollama / OpenAI)**  | Configurable (Local Host / cloud)  |
| **Reranker**     | **bge-reranker-v2-m3**        | Text Embeddings Inference (Docker) |
| **Embeddings**   | **Hybrid (Ollama / OpenAI)**  | Configurable (Local Host / cloud)  |
| **PDF Engine**   | **PdfPig**                    | Apache 2.0 License (No iText AGPL) |
| **Tokenizer**    | **Microsoft.ML.Tokenizers**   | GPT-4/Llama-3 Token Counting       |

## 📂 Structure

The solution follows a strict Clean Architecture pattern, divided into four core projects:

| Project                | Role                                     | Key Dependencies                                                                 |
| :--------------------- | :--------------------------------------- | :------------------------------------------------------------------------------- |
| **FinAnalyzer.Core**   | **Contracts & Models** (Domain Layer)    | `None` (Pure .NET)                                                               |
| **FinAnalyzer.Engine** | **Logic & Services** (Application Layer) | `Microsoft.SemanticKernel`, `Qdrant.Client`, `PdfPig`, `Microsoft.ML.Tokenizers` |
| **FinAnalyzer.UI**     | **Presentation** (Presentation Layer)    | `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`                          |
| **FinAnalyzer.Test**   | **Verification** (Testing Layer)         | `xUnit`, `NSubstitute`, `FluentAssertions`                                       |

## 🖥️ Screenshots

The application features a modern dark theme designed for extended analyst workflows:

- **Dashboard**: System health monitoring and pipeline status
- **Documents**: PDF repository with upload and status tracking
- **Chat**: RAG-powered analysis with document context selection
- **Settings**: Docker service configuration and health checks

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
