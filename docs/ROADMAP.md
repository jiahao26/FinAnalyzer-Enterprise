---
tags: [plan, project-management, dotnet, ai, rag]
---

# Project Roadmap: Secure Enterprise RAG Workstation (FinAnalyzer)

**Objective:** Build a production-ready, local-first C# WPF application for analyzing financial documents using LLMs.
**Constraint:** Zero data leakage (Air-gapped compatible). All AI runs locally or on trusted internal containers.

---

## **Phase 1: Infrastructure & "Hello World"**

**Focus:** Connectivity & Environment Setup.

- [x] **Docker Foundation**: Create `docker-compose.yml` with Qdrant, Ollama, and TEI (Reranker) services.
- [x] **Solution Scaffold**: Create `FinAnalyzer_Enterprise.slnx` with `Core`, `Engine`, `UI`, and `Test` projects.
- [x] **Service Verification**: Implement `InfrastructureTests` to verify:
  - [x] Connect to Qdrant.
  - [x] Connect to Ollama.
  - [x] Connect to Reranker.

## **Phase 2: The Data Pipeline (Ingestion)**

**Focus:** Converting raw PDF data into searchable vectors.

- [x] **Core Domain Definition**: Clean Architecture setup in `FinAnalyzer.Core`.
  - [x] **Models**: Create `DocumentChunk.cs` and `SearchResult.cs`.
  - [x] **Interfaces**: Define `IFileLoader`, `IVectorDbService`, `IRerankerService`, and `IRagService`.
  - [x] **Embedding Interface**: Define `IEmbeddingService` for vector generation.
- [x] **PDF Reader**: Implement `PdfPigLoader` to extract text from multi-column PDFs properly.
- [x] **Chunking Engine**: Implement `TextChunker` with sliding windows (e.g., 500 tokens, 100 overlap).
- [x] **Embedding Generation**: Implement `OllamaEmbeddingService` to convert text chunks to vectors using `nomic-embed-text-v1.5`.
- [x] **Vector Upsert**: Implement `QdrantVectorService.UpsertAsync` to push chunks + metadata (page number, filename) to the DB.
- [x] **Integration Tests**: Verify the full ingestion pipeline (PDF -> Chunk -> Qdrant) using `IngestionTests.cs`.

## **Phase 3: The RAG Core (The Brain)**

**Focus:** Retrieval logic and Answer Generation.

- [ ] **Retrieval Logic**: Implement `QdrantVectorService.SearchAsync` (Vector Search).
- [ ] **Reranking Logic**: Implement `TeiRerankerService.RerankAsync` to filter top 50 -> top 5 relevant chunks.
- [ ] **Orchestration**: Implement `SemanticKernelService` to bind it all together:
  - `Input -> Vector Search -> Rerank -> Prompt Engineering (Context Stuffing) -> LLM -> Answer`.
- [ ] **Orchestration Tests**: Verify the pipeline flows correctly using Mocks.

## **Phase 4: The UI Foundation (The Face)**

**Focus:** Structure and MVVM scaffolding.

- [ ] **Shell Layout**: Design the Main Window with a Sidebar (History) and Main Content (Chat).
- [ ] **MVVM Setup**: Initialize `CommunityToolkit.Mvvm` and Dependency Injection container in `App.xaml.cs`.
- [ ] **Settings Page**: Create a view to configure Docker URLs (e.g., "Qdrant URL", "Ollama Model Name").
- [ ] **Mock UI**: Create a "Chat View" that displays static fake messages to test the layout.

## **Phase 5: Integration & Threading**

**Focus:** Connecting the Brain to the Face without freezing.

- [ ] **File Drag & Drop**: Allow users to drop 10-K PDFs onto the UI.
- [ ] **Async Loading**: Implement "Background Ingestion" so the UI remains responsive while parsing PDFs.
- [ ] **Live Chat**: Connect the Chat ViewModel to the `SemanticKernelService`. Use `IAsyncEnumerable` to stream tokens (typing effect).
- [ ] **State Management**: Handle "Thinking...", "Indexing...", and "Ready" states visually.

## **Phase 6: Advanced RAG Features**

**Focus:** Usability and Trust.

- [ ] **Citations**: Parse the answer to link back to source chunks.
  - _Feature_: When AI says "[1]", user clicks it -> UI opens the specific PDF page.
- [ ] **Chat History**: Persist chat sessions to a local SQLite or JSON file.
- [ ] **Prompt Tuning**: Refine the System Prompt to prevent hallucinations ("If you don't know, say 'I don't know'").

## **Phase 7: Enterprise Hardening**

**Focus:** Reliability and Compliance.

- [ ] **Audit Logging**: Implement `Serilog` to log every prompt and response to a text file (Compliance requirement).
- [ ] **Global Error Handling**: Catch Docker connection failures gracefully (e.g., "AI Service Unreachable" toast notification).
- [ ] **Integration Tests**: Write `FinAnalyzer.Test` integration tests against a real Docker instance.
- [ ] **Release Packaging**: Ensure the app builds as a single self-contained executable (or clear MSI installer).

---

## **Definition of Done**

The project is considered complete when:

1.  A user can drag a fresh PDF into the app.
2.  Ask a question about a specific financial figure.
3.  Get an accurate answer with a citation link.
4.  Clicking the citation opens the source document.
5.  No data leaves the local network.
