# ?? obs-bank-v2 — Demo de Observabilidad LGTM

Stack completo de observabilidad basado en **Grafana LGTM**:

- **Loki** ? Logs  
- **Grafana** ? Visualización  
- **Tempo** ? Traces  
- **Prometheus** ? Métricas  
- **OpenTelemetry Collector** ? Ingesta OTLP  
- **Grafana Alloy** ? Recolección de logs desde archivos  
- Servicios demo (.NET + SQL poller)

---

## Arquitectura (flujo de datos)

Aplicaciones ? OTLP ? OTel Collector ? Prometheus / Loki / Tempo
Logs de archivos ? Alloy ? Loki
Grafana ? Consulta Prometheus + Loki + Tempo

---

## ?? Requisitos

- Docker ? 20.x
- Docker Compose ? v2
- Puertos libres:

| Servicio | Puerto |
|----------|---------|
| Grafana | 3000 |
| Prometheus | 9090 |
| Loki | 3100 |
| Tempo | 3200 |
| OTLP gRPC | 4317 |
| OTLP HTTP | 4318 |
| Alloy UI | 12345 |

---

## ?? Cómo levantar el stack

Desde la raíz del repo:

```bash
docker compose up -d --build
```
Para detener y limpiar volúmenes:
```bash
docker compose down -v
```