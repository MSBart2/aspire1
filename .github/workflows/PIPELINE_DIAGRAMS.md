# Pipeline Architecture Visualization

## High-Level Pipeline Flow

```mermaid
graph TB
    subgraph "Triggers"
        T1[Push to main] --> Decision{Which stage?}
        T2[Tag v*] --> Decision
        T3[Manual Dispatch] --> Decision
    end
    
    subgraph "Stage 1: Build & Version"
        Decision --> B1[Checkout Code]
        B1 --> B2[Setup .NET]
        B2 --> B3[Cache NuGet]
        B3 --> B4[Restore Dependencies]
        B4 --> B5[Extract Version with MinVer]
        B5 --> B6[Build Solution]
        B6 --> B7[Upload Artifacts]
    end
    
    subgraph "Stage 2: Parallel Testing"
        B7 --> TW1[Test Web Job]
        B7 --> TA1[Test API Job]
        
        TW1 --> TW2[Restore & Test Web]
        TW2 --> TW3[Publish Results]
        
        TA1 --> TA2[Restore & Test API]
        TA2 --> TA3[Publish Results]
    end
    
    subgraph "Stage 3: Dev Deployment"
        TW3 --> D1{Tests Pass?}
        TA3 --> D1
        D1 -->|Yes| D2[Azure Login OIDC]
        D2 --> D3[Setup azd]
        D3 --> D4[Configure Environment]
        D4 --> D5[azd up]
        D5 --> D6[Verify Deployment]
        D6 --> D7[Health Check]
    end
    
    subgraph "Stage 4: Stage Deployment"
        D7 --> S1{Approved?}
        S1 -->|Manual Approval| S2[Deploy to Stage]
        S2 --> S3[Verify Stage]
    end
    
    subgraph "Stage 5: Prod Deployment"
        S3 --> P1{Approved?}
        P1 -->|Manual Approval| P2[Deploy to Prod]
        P2 --> P3[Verify Prod]
        P3 --> P4[Post-Deployment Checklist]
    end
    
    style B1 fill:#0078d4,color:#fff
    style B6 fill:#0078d4,color:#fff
    style TW2 fill:#50e6ff,color:#000
    style TA2 fill:#50e6ff,color:#000
    style D5 fill:#107c10,color:#fff
    style S2 fill:#ff8c00,color:#fff
    style P2 fill:#d83b01,color:#fff
```

## Parallel Execution Model

```mermaid
gantt
    title Pipeline Execution Timeline
    dateFormat X
    axisFormat %M:%S

    section Build
    Restore & Build    :0, 180s

    section Test (Parallel)
    Web Tests          :180, 120s
    API Tests          :180, 120s

    section Deploy Dev
    Provision & Deploy :300, 240s
    Verify            :540, 60s

    section Deploy Stage
    Wait for Approval :600, 120s
    Provision & Deploy:720, 240s
    Verify            :960, 60s

    section Deploy Prod
    Wait for Approval :1020, 300s
    Provision & Deploy:1320, 240s
    Verify            :1560, 60s
```

## Environment Flow

```mermaid
flowchart LR
    subgraph "Development"
        A[Developer Push] --> B[main branch]
        B --> C[Build & Test]
        C --> D[Auto Deploy to Dev]
    end
    
    subgraph "Release Process"
        E[Create Tag v*] --> F[Build & Test]
        F --> G[Deploy Dev]
        G --> H{Stage Approval}
        H -->|Approved| I[Deploy Stage]
        I --> J{Prod Approval}
        J -->|Approved| K[Deploy Prod]
    end
    
    subgraph "Emergency"
        L[Manual Trigger] --> M[Select Environment]
        M --> N[Skip Tests Option]
        N --> O[Deploy Directly]
    end
    
    style D fill:#107c10,color:#fff
    style I fill:#ff8c00,color:#fff
    style K fill:#d83b01,color:#fff
```

## Deployment Strategy by Environment

```mermaid
graph TD
    subgraph "Dev Environment"
        Dev1[Trigger: Push to main]
        Dev2[Auto-deploy: Yes]
        Dev3[Approval: None]
        Dev4[Rollback: Manual]
        Dev5[Purpose: Continuous Integration]
    end
    
    subgraph "Stage Environment"
        Stage1[Trigger: Tag v* or Manual]
        Stage2[Auto-deploy: After Dev]
        Stage3[Approval: 1-2 reviewers]
        Stage4[Rollback: azd down + redeploy]
        Stage5[Purpose: Pre-prod Testing]
    end
    
    subgraph "Prod Environment"
        Prod1[Trigger: Tag v* or Manual]
        Prod2[Auto-deploy: After Stage]
        Prod3[Approval: 2+ reviewers + 5min wait]
        Prod4[Rollback: Previous version tag]
        Prod5[Purpose: Production]
    end
    
    style Dev1 fill:#107c10,color:#fff
    style Stage1 fill:#ff8c00,color:#fff
    style Prod1 fill:#d83b01,color:#fff
```

## Caching Strategy

```mermaid
graph LR
    subgraph "Build Stage"
        B1[NuGet Restore] --> B2{Cache Hit?}
        B2 -->|Yes| B3[Use Cached Packages]
        B2 -->|No| B4[Download Packages]
        B4 --> B5[Cache Packages]
    end
    
    subgraph "Test Stages"
        T1[Test Web] --> T2{Cache Hit?}
        T2 -->|Yes| T3[Use Cached Packages]
        
        T4[Test API] --> T5{Cache Hit?}
        T5 -->|Yes| T6[Use Cached Packages]
    end
    
    B3 --> Build[Fast Build ~2min]
    B5 --> BuildSlow[Slower Build ~3min]
    
    T3 --> TestFast[Fast Tests ~1min]
    T6 --> TestFast2[Fast Tests ~1min]
    
    style B3 fill:#50e6ff,color:#000
    style T3 fill:#50e6ff,color:#000
    style T6 fill:#50e6ff,color:#000
```

## Security & Authentication Flow

```mermaid
sequenceDiagram
    participant GH as GitHub Actions
    participant AAD as Azure AD
    participant Azure as Azure Resources
    
    Note over GH,AAD: OIDC Authentication (No Secrets!)
    
    GH->>AAD: Request OIDC token
    Note right of GH: Token includes:<br/>repo, environment,<br/>workflow details
    
    AAD->>AAD: Validate federated credential
    Note right of AAD: Check subject claim:<br/>repo:org/repo:environment:dev
    
    AAD->>GH: Return access token
    
    GH->>Azure: Deploy with access token
    Note right of GH: Token scoped to:<br/>- Specific subscription<br/>- Contributor role<br/>- Time-limited
    
    Azure->>Azure: Provision resources
    Azure->>Azure: Deploy containers
    
    Azure->>GH: Deployment success
    
    GH->>Azure: Verify endpoints
    Note right of GH: Health checks:<br/>/health<br/>/version
```

## Artifact Flow

```mermaid
graph TB
    subgraph "Build Stage"
        B1[Build Solution] --> B2[Generate Artifacts]
        B2 --> B3[Upload to GitHub]
    end
    
    subgraph "Container Registry"
        D1[azd package] --> D2[Build Docker Images]
        D2 --> D3[Tag with Version]
        D3 --> D4[Push to ACR]
    end
    
    subgraph "Deployment"
        D4 --> E1[Deploy Dev]
        D4 --> E2[Deploy Stage]
        D4 --> E3[Deploy Prod]
        
        E1 --> E4[aspire1-web:1.2.3-dev]
        E1 --> E5[aspire1-weatherservice:1.2.3-dev]
        
        E2 --> E6[aspire1-web:1.2.3-stage]
        E2 --> E7[aspire1-weatherservice:1.2.3-stage]
        
        E3 --> E8[aspire1-web:1.2.3]
        E3 --> E9[aspire1-weatherservice:1.2.3]
    end
    
    style B2 fill:#0078d4,color:#fff
    style D4 fill:#0078d4,color:#fff
    style E4 fill:#107c10,color:#fff
    style E5 fill:#107c10,color:#fff
    style E6 fill:#ff8c00,color:#fff
    style E7 fill:#ff8c00,color:#fff
    style E8 fill:#d83b01,color:#fff
    style E9 fill:#d83b01,color:#fff
```

## Monitoring & Observability

```mermaid
graph TB
    subgraph "Pipeline Execution"
        P1[Workflow Run] --> P2[Job Steps]
        P2 --> P3[Action Logs]
    end
    
    subgraph "Deployment Verification"
        D1[Health Endpoint] --> D2[/health]
        D1 --> D3[/version]
        D1 --> D4[/health/detailed]
    end
    
    subgraph "Application Monitoring"
        A1[App Insights] --> A2[Custom Metrics]
        A1 --> A3[Traces]
        A1 --> A4[Logs]
        A1 --> A5[Exceptions]
    end
    
    subgraph "Alerts"
        AL1[Deployment Failed] --> N1[Notify Team]
        AL2[Tests Failed] --> N1
        AL3[Health Check Failed] --> N1
    end
    
    P3 --> Summary[GitHub Summary]
    D2 --> Summary
    D3 --> Summary
    D4 --> Summary
    
    A2 --> Dashboard[Azure Dashboard]
    A3 --> Dashboard
    A4 --> Dashboard
    
    style Summary fill:#0078d4,color:#fff
    style Dashboard fill:#68217a,color:#fff
    style N1 fill:#d83b01,color:#fff
```
