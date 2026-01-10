# Secure Enterprise RAG Workstation (FinAnalyzer)

![License](https://img.shields.io/badge/license-MIT-blue) ![.NET](https://img.shields.io/badge/.NET-8.0-purple) ![Architecture](https://img.shields.io/badge/Architecture-Clean-green) ![Status](https://img.shields.io/badge/Status-Phase%202%20Complete-green)

**FinAnalyzer Enterprise** is an on-premise RAG workstation built for financial compliance teams. It automates analysis of sensitive 10-K reports using local vector retrieval and quantized LLMs, ensuring zero data egress.

## 📊 Progress Status

| Phase       | Description                                                  | Status          |
| :---------- | :----------------------------------------------------------- | :-------------- |
| **Phase 1** | **Infrastructure & "Hello World"** (Docker, Scaffold, Tests) | ✅ **Complete** |
| **Phase 2** | **The Data Pipeline** (PDF Parsing, Chunking, Vectors)       | ✅ **Complete** |
| **Phase 3** | **The RAG Core** (Retrieval, Reranking, Orchestration)       | ⏳ In Progress  |
| **Phase 4** | **The UI Foundation** (WPF Shell, MVVM, Settings)            | ⬜ Pending      |
| **Phase 5** | **Integration & Threading** (Async loading, Live Chat)       | ⬜ Pending      |
| **Phase 6** | **Advanced Features** (Citations, Chat History)              | ⬜ Pending      |
| **Phase 7** | **Enterprise Hardening** (Logging, Release Packaging)        | ⬜ Pending      |

## 📚 Documentation

| Document                                    | Description                                             |
| :------------------------------------------ | :------------------------------------------------------ |
| [**🚀 QUICK START**](docs/QUICK_START.md)   | How to set up Docker, models, and run the app.          |
| [**🌟 PHILOSOPHY**](docs/PHILOSOPHY.md)     | The strategic intent and "Secure by Design" principles. |
| [**🛤️ ROADMAP**](docs/ROADMAP.md)           | Detailed feature timeline and phasing.                  |
| [**🏗️ ARCHITECTURE**](docs/ARCHITECTURE.md) | Structure, Clean Architecture layers, and File Map.     |

---

## 🌟 Project Intent

This project serves as a proof-of-concept for **"Secure, Local-First Enterprise AI"**. Unlike cloud-native wrappers, FinAnalyzer demonstrates how to build production-grade RAG systems that function entirely air-gapped using local containers and quantized models.

_Read more in [PHILOSOPHY.md](docs/PHILOSOPHY.md)._

## 🚀 Key Features

| Feature                    | Description                                                                                     |
| :------------------------- | :---------------------------------------------------------------------------------------------- |
| **Air-Gapped by Design**   | Runs entirely offline with no dependency on public cloud APIs (OpenAI/Azure).                   |
| **Clean Architecture**     | Strict separation of concerns (Core, Engine, UI) for maintainability.                           |
| **Hybrid Search Pipeline** | Multistage retrieval using Qdrant (Vector), bge-reranker (Precision), and Llama-3 (Generation). |
| **Auditable Grounding**    | Tailored citations for every claim with direct PDF links.                                       |
| **Compliance Logging**     | Full audit trail of all queries and token usage.                                                |
| **Financial Dashboard**    | High-density WPF interface optimized for analyst workflows.                                     |

## 🛠️ Technology Stack

| Component        | Technology                    | Descriptions                       |
| :--------------- | :---------------------------- | :--------------------------------- |
| **UI Framework** | **WPF (.NET 8)**              | Modern Desktop (Material Design)   |
| **Orchestrator** | **Microsoft Semantic Kernel** | AI Agent Orchestration             |
| **Vector DB**    | **Qdrant**                    | Local Docker Container             |
| **Local LLM**    | **Llama-3-8B-Instruct**       | Q8 Quantization (via Ollama)       |
| **Reranker**     | **bge-reranker-v2-m3**        | Text Embeddings Inference (Docker) |
| **Embeddings**   | **nomic-embed-text-v1.5**     | Matryoshka Enabled (via Ollama)    |
| **PDF Engine**   | **PdfPig**                    | Apache 2.0 License (No iText AGPL) |

## 📂 Structure

The solution follows a strict Clean Architecture pattern, divided into four core projects:

| Project                | Role                                     | Key Dependencies                                        |
| :--------------------- | :--------------------------------------- | :------------------------------------------------------ |
| **FinAnalyzer.Core**   | **Contracts & Models** (Domain Layer)    | `None` (Pure .NET)                                      |
| **FinAnalyzer.Engine** | **Logic & Services** (Application Layer) | `Microsoft.SemanticKernel`, `Qdrant.Client`, `PdfPig`   |
| **FinAnalyzer.UI**     | **Presentation** (Presentation Layer)    | `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting` |
| **FinAnalyzer.Test**   | **Verification** (Testing Layer)         | `xUnit`, `NSubstitute`, `FluentAssertions`              |

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
