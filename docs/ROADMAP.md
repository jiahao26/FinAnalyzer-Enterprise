---
tags: [plan, project-management, dotnet, ai, rag]
---

# Project Roadmap: Secure Enterprise RAG Workstation (FinAnalyzer)

**Objective:** Build a production-ready, local-first C# WPF application for analyzing financial documents using LLMs.
**Constraint:** Zero data leakage (Air-gapped compatible). All AI runs locally or on trusted internal containers.

---

## **Phase 1: Infrastructure & "Hello World"**

**Focus:** Connectivity & Environment Setup.

### 1.1 Docker Foundation

- [x] **Docker Compose**: Create `docker-compose.yml` with service definitions.
- [x] **Qdrant Container**: Configure vector database on port 6333/6334.
- [x] **Ollama Container**: Configure LLM backend with GPU passthrough.
- [x] **TEI Container**: Configure Text Embeddings Inference for reranking.
- [x] **Volume Mounts**: Persist Qdrant data and Ollama models.

### 1.2 Solution Scaffold

- [x] **Solution File**: Create `FinAnalyzer_Enterprise.slnx` (modern .NET format).
- [x] **FinAnalyzer.Core**: Domain layer with interfaces and models.
- [x] **FinAnalyzer.Engine**: Application layer with services.
- [x] **FinAnalyzer.UI**: Presentation layer with WPF.
- [x] **FinAnalyzer.Test**: Testing layer with xUnit.
- [x] **Directory Structure**: Establish `docs/`, `build/`, `.agent/` folders.

### 1.3 Service Verification

- [x] **InfrastructureTests.cs**: Create test class for connectivity.
- [x] **Qdrant Connection Test**: Verify gRPC connection to Qdrant.
- [x] **Ollama Connection Test**: Verify HTTP connection to Ollama API.
- [x] **TEI Connection Test**: Verify reranker endpoint responds.

---

## **Phase 2: The Data Pipeline (Ingestion)**

**Focus:** Converting raw PDF data into searchable vectors.

### 2.1 Core Domain Definition

- [x] **DocumentChunk Model**: Define chunk with ID, text, vector, metadata.
- [x] **SearchResult Model**: Define search result with score and metadata.
- [x] **IFileLoader Interface**: Contract for loading documents.
- [x] **IVectorDbService Interface**: Contract for vector operations.
- [x] **IRerankerService Interface**: Contract for reranking.
- [x] **IEmbeddingService Interface**: Contract for embedding generation.
- [x] **IRagService Interface**: Contract for RAG orchestration.

### 2.2 PDF Processing

- [x] **PdfPigLoader**: Implement PDF text extraction using PdfPig.
- [x] **Multi-Column Support**: Handle complex PDF layouts.
- [x] **Page Number Tracking**: Preserve page metadata for citations.
- [x] **Error Handling**: Graceful handling of corrupted PDFs.

### 2.3 Chunking Engine

- [x] **TextChunker**: Implement token-based chunking.
- [x] **Sliding Window**: Configure 500 tokens with 100 overlap.
- [x] **Semantic Boundaries**: Split on sentence/paragraph boundaries.
- [x] **Chunk ID Generation**: Deterministic IDs for idempotent ingestion.

### 2.4 Embedding Generation

- [x] **GenericEmbeddingService**: Implement Ollama embedding client.
- [x] **nomic-embed-text**: Configure default embedding model.
- [x] **Batch Processing**: Support embedding multiple chunks.
- [x] **WarmUp Method**: Pre-load model on startup.

### 2.5 Vector Storage

- [x] **QdrantVectorService.UpsertAsync**: Push chunks to Qdrant.
- [x] **Collection Management**: Auto-create collections as needed.
- [x] **Payload Storage**: Store text, source, page in point payload.
- [x] **Vector Validation**: Ensure embeddings match expected dimensions.

### 2.6 Integration Testing

- [x] **IngestionTests.cs**: End-to-end PDF → Qdrant tests.
- [x] **Sample PDF**: Include test financial document.

---

## **Phase 3: The RAG Core (The Brain)**

**Focus:** Retrieval logic and Answer Generation.

### 3.1 Retrieval Logic

- [x] **QdrantVectorService.SearchAsync**: Implement vector similarity search.
- [x] **Top-K Selection**: Return top 50 candidates for reranking.
- [x] **Score Threshold**: Filter low-relevance results.

### 3.2 Reranking Logic

- [x] **TeiRerankerService**: Implement TEI reranker client.
- [x] **RerankAsync Method**: Reorder candidates by relevance.
- [x] **Top-N Selection**: Return top 5 after reranking.
- [x] **WarmUp Method**: Pre-load reranker model.

### 3.3 Model Lifecycle

- [x] **IModelLifecycle Interface**: Define warmup contract.
- [x] **Startup Warmup**: Load models into memory on app start.
- [x] **Graceful Degradation**: Handle offline services.

### 3.4 Hybrid AI Backend

- [x] **AISettings Configuration**: Support multiple backend types.
- [x] **Ollama Backend**: Local LLM via OpenAI-compatible API.
- [x] **OpenAI_Compatible Backend**: Support vLLM, LM Studio, etc.
- [x] **appsettings.json**: Externalize all configuration.

### 3.5 Orchestration

- [x] **SemanticKernelService**: Implement using Microsoft Semantic Kernel.
- [x] **QueryAsync Method**: Full RAG pipeline with streaming.
- [x] **Context Stuffing**: Build prompt with relevant chunks.
- [x] **Streaming Response**: Use `IAsyncEnumerable<string>` for tokens.

### 3.6 Testing

- [x] **Orchestration Unit Tests**: Verify pipeline flow with mocks.
- [x] **Mock Services**: NSubstitute for isolation.

---

## **Phase 4: The UI Foundation (The Face)**

**Focus:** Structure, MVVM scaffolding, and visual design.

### 4.1 Theme & Design System

- [x] **Dark Theme**: Create `DarkTheme.xaml` with enterprise color palette.
- [x] **Control Styles**: Define reusable styles for Card, NavButton, PrimaryButton, IconButton, ModernTextBox, StatusBadge.
- [x] **Value Converters**: Implement BoolToVisibilityConverter, StringToVisibilityConverter, CollectionToVisibilityConverter.

### 4.2 Shell Layout

- [x] **Main Window Structure**: Design `MainWindow.xaml` with sidebar navigation and content area.
- [x] **Navigation System**: Implement navigation commands in `MainViewModel`.
- [x] **Header Bar**: Add application branding and user context.

### 4.3 MVVM Architecture

- [x] **MVVM Framework**: Initialize `CommunityToolkit.Mvvm` with source generators.
- [x] **Dependency Injection**: Configure `Microsoft.Extensions.DependencyInjection` in `App.xaml.cs`.
- [x] **ViewModelBase**: Create base class extending `ObservableObject`.
- [x] **View-ViewModel Binding**: Wire DataContext via DI for all views.
- [x] **Configuration Loading**: Load `appsettings.json` via `IConfiguration`.

### 4.4 Dashboard View

- [x] **System Health Cards**: Display Qdrant and Ollama status indicators.
- [x] **Pipeline Status**: Show ingestion pipeline steps with icons.
- [x] **Recent Reports List**: Display recently processed documents.
- [x] **Empty States**: Show placeholder UI when no data available.
- [ ] **Health Polling**: Periodic service status updates via timer.
- [ ] **Live Pipeline Status**: Update steps based on active ingestion.

### 4.5 Documents View

- [x] **Document List**: Display documents with status indicators (Ingested, Processing, Error).
- [x] **Search Bar**: Add search input for filtering documents.
- [x] **Statistics Cards**: Show ingested count and pending count.
- [x] **Upload FAB Button**: Floating action button to trigger file picker.
- [x] **Shared Document Store**: Create `DocumentStore` singleton for cross-view state.
- [x] **Progress Bar**: Show ingestion progress (0-100%) on each document.
- [x] **Drag & Drop**: Enable PDF drop zone with file validation.
- [x] **Cancel Ingestion**: Button to abort ongoing processing.

### 4.6 Chat View

- [x] **Message Bubbles**: User messages (right-aligned) and AI messages (left-aligned).
- [x] **Analysis Parameters Bar**: Display selected model and corpus context.
- [x] **Input Area**: Text input with send button and Enter key support.
- [x] **Document Picker**: Button to select documents for context.
- [x] **Processing Indicator**: Show "thinking" state while awaiting response.
- [x] **Security Footer**: Display "Zero Egress" and encryption status.

### 4.7 Settings View

- [x] **Configuration Forms**: Input fields for Qdrant URL, Ollama endpoint, TEI endpoint, model names.
- [x] **Service Status Indicators**: Visual dots showing online/offline state.
- [x] **Test Connection Button**: Real HTTP health checks to verify Docker services.
- [x] **Start Docker Button**: Execute `docker-compose up -d` from UI.
- [x] **ServiceHealthChecker**: Utility class for health check logic.

### 4.8 UI Models

- [x] **ChatMessage**: Observable model with streaming content support.
- [x] **Citation**: Model for source references (filename, section, page).
- [x] **DocumentItem**: Observable model for documents with status updates.
- [x] **ServiceHealthInfo**: Model for system health display.
- [x] **PipelineStep**: Model for pipeline status visualization.

---

## **Phase 5: Integration & Threading**

**Focus:** Connecting the Brain to the Face without freezing.

### 5.1 Document Ingestion Pipeline

- [x] **Wire PdfPigLoader**: Connect PDF loader to upload flow.
- [x] **Wire TextChunker**: Chunk uploaded documents.
- [x] **Wire EmbeddingService**: Generate embeddings for chunks.
- [x] **Wire QdrantVectorService**: Store chunks in Qdrant.
- [x] **Progress Tracking**: Update DocumentItem.Progress during ingestion.

### 5.2 Drag & Drop

- [x] **AllowDrop**: Enable drag-drop on Documents view.
- [x] **File Validation**: Filter for PDF files only.
- [x] **Multi-File Support**: Handle multiple dropped files.

### 5.3 Async Loading

- [x] **Background Ingestion**: Use `Task.Run` for CPU-intensive work.
- [x] **Cancellation Support**: Allow canceling ongoing ingestion.
- [x] **Progress Reporting**: Use `IProgress<T>` for UI updates.

### 5.4 Live Chat Integration

- [x] **Wire IRagService**: Connect ChatViewModel to SemanticKernelService.
- [x] **Collection Selection**: Query specific document collection.
- [x] **Streaming Tokens**: Display characters as they arrive (typing effect).
- [x] **Error Handling**: Show user-friendly error messages.

### 5.5 State Management

- [x] **Loading States**: "Thinking...", "Indexing...", "Ready" indicators.
- [x] **Service Status Polling**: Periodic health checks on Dashboard.
- [ ] **Offline Mode**: Graceful degradation when services unavailable.

---

## **Phase 6: Advanced RAG Features**

**Focus:** Usability and Trust.

### 6.1 Citations

- [ ] **Citation Parsing**: Extract `[1]`, `[2]` markers from AI response.
- [ ] **Citation Linking**: Map citations to source chunks.
- [ ] **Clickable Citations**: Open PDF viewer at specific page.
- [ ] **Citation Panel**: Display sources in collapsible sidebar.

### 6.2 Chat History

- [ ] **Session Persistence**: Save chat sessions to JSON/SQLite.
- [ ] **Session List**: Display past conversations in sidebar.
- [ ] **Session Restore**: Load previous conversation context.
- [ ] **Export Chat**: Save chat as markdown or PDF.

### 6.3 Prompt Engineering

- [ ] **System Prompt**: Craft anti-hallucination prompt template.
- [ ] **Few-Shot Examples**: Include example Q&A pairs.
- [ ] **Retrieval Prompt**: Optimize context formatting.
- [ ] **Temperature Control**: Configure creativity vs accuracy.

---

## **Phase 7: Enterprise Hardening**

**Focus:** Reliability and Compliance.

### 7.1 Audit Logging

- [ ] **Serilog Integration**: Configure structured logging.
- [ ] **Query Logging**: Log every user prompt with timestamp.
- [ ] **Response Logging**: Log all AI responses.
- [ ] **Token Usage**: Track and log token consumption.
- [ ] **Log Rotation**: Configure file rotation policy.

### 7.2 Error Handling

- [ ] **Global Exception Handler**: Catch unhandled exceptions.
- [ ] **Service Unreachable Toast**: User-friendly notifications.
- [ ] **Retry Logic**: Automatic retry for transient failures.
- [ ] **Circuit Breaker**: Prevent cascade failures.

### 7.3 Testing

- [ ] **Integration Tests**: Test against real Docker services.
- [ ] **E2E Tests**: Full workflow tests with sample PDFs.
- [ ] **Performance Tests**: Measure ingestion and query times.
- [ ] **Load Tests**: Test with large document sets.

### 7.4 Release Packaging

- [ ] **Self-Contained Build**: Single executable with runtime.
- [ ] **MSI Installer**: Windows installer package.
- [ ] **Version Stamping**: Embed version info in executable.
- [ ] **Code Signing**: Sign executable for trust.

---

## **Definition of Done**

The project achieves **Production Ready** status when all acceptance criteria below are satisfied:

### Functional Requirements

| Criterion                                                                             | Verification Method                             |
| ------------------------------------------------------------------------------------- | ----------------------------------------------- |
| The system shall accept PDF documents via drag-and-drop or file picker                | Manual testing with SEC filing                  |
| The system shall extract, chunk, and embed document content into the vector database  | Integration test: verify chunk count in Qdrant  |
| The system shall retrieve semantically relevant passages for natural language queries | Integration test: query returns relevant chunks |
| The system shall generate accurate, context-grounded answers using the LLM            | Manual verification against source document     |
| The system shall provide traceable citations linking answers to source pages          | UI test: citation click navigates to PDF page   |
| The system shall persist chat sessions for audit and review purposes                  | Verify session recovery after app restart       |

### Non-Functional Requirements

| Criterion                                                                                      | Verification Method                   |
| ---------------------------------------------------------------------------------------------- | ------------------------------------- |
| **Zero Data Egress**: No network traffic shall leave the local machine during normal operation | Network monitor during full workflow  |
| **Air-Gap Compatibility**: The system shall function without internet connectivity             | Disconnect network, run full workflow |
| **Response Latency**: Query-to-first-token shall complete in <3 seconds (8GB GPU)              | Performance benchmark                 |
| **Ingestion Throughput**: 100-page PDF shall ingest in <60 seconds                             | Timed ingestion test                  |
| **UI Responsiveness**: No UI freeze during document ingestion or LLM generation                | Manual testing with large documents   |
| **Audit Compliance**: All queries and responses shall be logged with timestamps                | Log file inspection                   |

### Quality Gates

- [ ] All unit tests pass (`dotnet test`)
- [ ] All integration tests pass against live Docker services
- [ ] No critical or high-severity security vulnerabilities (dependency scan)
- [ ] Code coverage ≥ 70% on Core and Engine projects
- [ ] Application builds as single self-contained executable
- [ ] Documentation complete (README, ARCHITECTURE, CONFIGURATION, QUICK_START)

### Final Validation

- [ ] All phases 1-7 complete
- [ ] Acceptance tests passed
- [ ] Air-gap and audit requirements verified
- [ ] End-to-end demo successful
