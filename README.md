# Secure Enterprise RAG Workstation

A local-first .NET 8 application demonstrating secure, enterprise-grade Retrieval-Augmented Generation (RAG) for financial compliance. This application allows credit analysts to automate 10-K report compliance checks using local vector retrieval, ensuring that sensitive data never leaves the secure intranet environment.

## 🚀 Key Features

- **100% Local Execution**: Built for air-gapped security. No data is sent to public cloud APIs (OpenAI/Azure).
- **Enterprise Architecture**: Clean Architecture implementation with a clear separation of concerns (Core, Engine, UI).
- **Advanced RAG Pipeline**: Implements a 3-stage retrieval process:
  1.  **Vector Search (Rough Filter)**: Fast retrieval using Qdrant.
  2.  **Semantic Reranking (Precision Filter)**: Accuracy refinement using `bge-reranker-v2-m3`.
  3.  **LLM Generation**: Context-aware answers using **Llama-3-8B-Instruct**.
- **Citation & Compliance**: Provides traceable citations for every answer, mitigating hallucinations (Grounding).
- **WPF Dashboard**: High-density desktop interface tailored for financial analysts.

## 🛠️ Technology Stack

- **UI Framework**: C# WPF (.NET 8)
- **AI Orchestrator**: Microsoft Semantic Kernel
- **Vector Database**: Qdrant (Docker)
- **Local LLM**: Ollama running Llama-3-8B-Instruct (Q8 Quantization)
- **Reranker**: HuggingFace Text Embeddings Inference (bge-reranker-v2-m3)
- **Embeddings**: nomic-embed-text-v1.5 (via Ollama)
- **PDF Processing**: PdfPig (Apache 2.0 License)

## 🏗️ Architecture

The solution follows a strict Clean Architecture pattern:

- **`FinAnalyzer.Core`**: Shared interfaces, domain models, and contracts.
- **`FinAnalyzer.Engine`**: The RAG logic, encapsulating Semantic Kernel and embedding handling.
- **`FinAnalyzer.UI`**: The WPF Presentation layer (MVVM).

## 🔒 Security & Privacy

This application is designed with **data sovereignty** as the primary requirement.

- **Ingestion**: Documents are embedded locally using `nomic-embed-text` (or configured local model).
- **Storage**: Vectors are stored in a self-hosted Qdrant instance on the local network.
- **Inference**: All processing happens within the `FinAnalyzer.Engine` accessing the local Docker containers.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
