# Threat Detection System

A real-time network threat detection system that collects alerts from **Suricata IDS** via **Elasticsearch**, processes them through an **ASP.NET Core** backend, and broadcasts them live to a **React** frontend using **SignalR**.

---

## Architecture

```
Suricata IDS
     ↓
Elasticsearch (ELK Stack)
     ↓
ASP.NET Core Backend
  ├── REST API  →  React (polling)
  └── SignalR   →  React (real-time push)
```

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| IDS | Suricata |
| Log Storage | Elasticsearch 8.12 |
| Log Visualization | Kibana 8.12 |
| Backend | ASP.NET Core 8 |
| Real-Time | SignalR |
| Container | Docker |
| Frontend | React |

---

## Project Structure

```
ThreatDetectionSystem/
├── Domain_Layer/                   # Models
│   └── Models/
│       ├── Alert.cs
│       └── AlertStats.cs
│
├── ServiceAbstraction_Layer/       # Interfaces
│   ├── IElkService.cs
│   └── IAlertHubNotifier.cs
│
├── Services_Layer/                 # Business Logic
│   ├── ElkService.cs
│   └── AlertBroadcastService.cs
│
└── ThreatDetectionSystem/          # Presentation Layer
    ├── Controllers/
    │   └── AlertController.cs
    ├── Hubs/
    │   ├── AlertHub.cs
    │   └── AlertHubNotifier.cs
    └── Program.cs
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)

---

## Getting Started

### 1 — Start Docker containers

```bash
# Elasticsearch
docker run -d --name elasticsearch \
  -p 9200:9200 \
  -e "discovery.type=single-node" \
  -e "xpack.security.enabled=false" \
  docker.elastic.co/elasticsearch/elasticsearch:8.12.0

# Kibana
docker run -d --name kibana \
  -p 5601:5601 \
  --link elasticsearch:elasticsearch \
  docker.elastic.co/kibana/kibana:8.12.0
```

### 2 — Configure `appsettings.json`

```json
{
  "Elk": {
    "BaseUrl": "http://localhost:9200",
    "Index":   "suricata-*"
  }
}
```

### 3 — Run the backend

```bash
dotnet run
```

Backend will start on `http://localhost:5045`

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Alert` | Get all alerts |
| GET | `/api/Alert?range=30s` | Last 30 seconds |
| GET | `/api/Alert?range=1m` | Last 1 minute |
| GET | `/api/Alert?range=1h` | Last 1 hour |
| GET | `/api/Alert?range=24h` | Last 24 hours |
| GET | `/api/Alert/stats` | Alert statistics |
| GET | `/api/Alert/stats?range=1h` | Stats for time range |
| GET | `/api/Alert/health` | Elasticsearch health check |
| WS | `/alertHub` | SignalR real-time hub |

---

## Alert Model

```json
{
  "id":             "R9lRX50BnpeQ-02XIMqy",
  "source_IP":      "192.168.1.50",
  "destination_IP": "8.8.8.8",
  "severity":       "Critical",
  "message":        "ET SCAN Nmap SYN Scan",
  "timestamp":      "2026-04-13T10:00:00Z"
}
```

---

## Alert Severity Mapping

| Suricata Level | Label |
|---------------|-------|
| 1 | Critical |
| 2 | High |
| 3 | Medium |
| 4 | Low |
| 5 | Info |

---

## Stats Response

```json
{
  "critical": 2,
  "high":     5,
  "medium":   8,
  "low":      3,
  "info":     1,
  "total":    19
}
```

---

## Real-Time with SignalR

`AlertBroadcastService` polls Elasticsearch every **5 seconds** and pushes new alerts to all connected clients via SignalR.

### Connect from React

```bash
npm install @microsoft/signalr
```

```jsx
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5045/alertHub")
  .withAutomaticReconnect()
  .build();

connection.on("ReceiveAlerts", (alerts) => {
  console.log("New alerts:", alerts);
});

connection.start();
```

---

## Insert Test Alerts

```bash
curl -X POST "http://localhost:9200/suricata-$(date +%Y.%m.%d)/_doc" \
  -H "Content-Type: application/json" \
  -d "{
    \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
    \"src_ip\":    \"192.168.1.50\",
    \"dest_ip\":   \"8.8.8.8\",
    \"alert\": {
      \"severity\":  1,
      \"signature\": \"ET SCAN Nmap SYN Scan\"
    }
  }"
```

---

## Swagger UI

Available at `http://localhost:5045/swagger` in Development mode.

---

## License

MIT
