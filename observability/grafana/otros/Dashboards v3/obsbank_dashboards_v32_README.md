# Dashboards v3.2 para ObsBank

Estos 4 dashboards se generaron a partir del dashboard `Monitoreo ObsBank — Operación v3.1`
y de la nueva taxonomía de estados/queries.

## Dashboards
1. `obsbank-operacion-v32.json`
   - Vista ejecutiva y de NOC.
   - KPIs 24h, review/100, salud del poller y comparativos.

2. `obsbank-estados-aging-v32.json`
   - Diagnóstico por estados, backlog operativo/programado, aging y calidad de dato.

3. `obsbank-interbancario-v32.json`
   - Enfoque específico para bancos destino, degradación y riesgo por contraparte.

4. `obsbank-sql-infra-calidad-v32.json`
   - Infra SQL, I/O, tamaño/uso de archivos, calidad de dato y logs.

## Métricas nuevas asumidas
Estos JSON ya incluyen métricas nuevas que no estaban explícitas en el dashboard v3.1.
Para que rendericen completamente, el poller debe exponer al menos:
- `tx_success_count_24h`
- `tx_compensated_count_24h`
- `tx_review_current_count`
- `tx_review_avg_age_seconds`
- `tx_review_max_age_seconds`
- `tx_review_dead_count`
- `tx_programmed_current_count`
- `tx_quality_missing_mod_count_24h`
- `tx_quality_zero_duration_count_24h`
- `tx_quality_negative_duration_count_24h`
- `db_sessions_count`
- `db_file_io_read_stall_ms`
- `db_file_io_write_stall_ms`
- `db_file_io_bytes_read`
- `db_file_io_bytes_written`
- `db_file_size_mb`
- `db_file_used_mb`

## Compatibilidad
Los dashboards siguen reutilizando, cuando es posible, métricas ya presentes en v3.1:
- `tx_created_total_*`
- `tx_pending_count_*`
- `tx_pending_current_count`
- `tx_pending_aging_bucket_count`
- `tx_pending_oldest_seconds`
- `tx_error_count_24h`
- `tx_rejected_count_24h`
- `tx_failed_technical_count_24h`
- `tx_success_avg_seconds`
- `tx_success_p95_seconds`
- `tx_type_count_24h`
- `tx_amount_total_24h`
- `tx_amount_by_type_24h`
- `tx_interbank_bank_count_24h`
- `tx_interbank_bank_state_count_24h`
- `tx_interbank_bank_amount_total_24h`

Si quieres, en la siguiente iteración puedo generarte también una variante estricta de compatibilidad,
sin métricas nuevas, usando solamente lo que ya está hoy exportado.
