# Dashboards ObsBank v3.3

Estos JSON están alineados con el SQL poller actual de `Jhuly1215/obs-bank-v2`.

## Incluyen
- Operación ejecutiva
- Estados, backlog y aging
- Interbancario por banco destino
- Infra SQL y calidad de dato

## Notas
- Datasource Prometheus asumido: `PBFA97CFB590B2093`
- Datasource Loki asumido: `P8E80F9AEF21F6940`
- Si tus UIDs cambian, debes reemplazarlos antes de importar.
- La parte de infraestructura SQL seguirá vacía si el usuario SQL no tiene `VIEW SERVER STATE`.
- Estos dashboards usan métricas ya presentes o ya aliaseadas por el SQL poller actual:
  `tx_success_count_24h`, `tx_review_current_count`, `tx_programmed_current_count`,
  `tx_pending_avg_age_seconds`, `tx_pending_max_age_seconds`,
  `tx_quality_missing_mod_count_24h`, `tx_quality_zero_duration_count_24h`,
  `tx_quality_negative_duration_count_24h`, `tx_closed_count_24h`,
  `tx_other_state_count_24h`, `tx_compensated_current_count`,
  y la familia `sql_server_*` / `sql_database_size_mb`.
