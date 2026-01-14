# 🚀 Quick Start Guide

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows with WSL2)
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Ollama](https://ollama.com/download) (Local installation for better performance/VRAM)
- Visual Studio 2026

## Installation & Setup

1.  **Clone the Repository**

    ```bash
    git clone https://github.com/jiahao26/FinAnalyzer-Enterprise.git
    cd FinAnalyzer-Enterprise
    ```

2.  **Start Infrastructure Services**
    Run the following command to start Qdrant and the Reranker (Ollama should be running as a local service):

    ```bash
    docker-compose up -d
    ```

3.  **Initialize AI Models**
    Download the specific quantized Llama 3 model (Requires local Ollama to be running):

    ```bash
    ollama run llama3:8b-instruct-q4_K_M
    ```

    _Note: This might take a while depending on your internet connection._

> [!TIP] > **Using a different LLM?** FinAnalyzer supports any OpenAI-compatible endpoint. See [CONFIGURATION.md](CONFIGURATION.md) to configure Azure OpenAI, vLLM, or LM Studio instead of Ollama.

4.  **Run the Application**
    Open `FinAnalyzer_Enterprise.slnx` in Visual Studio and run the **FinAnalyzer.UI** project.
