namespace Bank.Obs.SqlPoller.State;

public sealed class MetricState
{
    private readonly object _gate = new();

    // =========================
    // VOLUMEN
    // =========================
    private int _intraTxLast15m;
    private int _interTxLast15m;

    private int _intraTxLast24h;
    private int _interTxLast24h;

    private int _intraTxLast30d;
    private int _interTxLast30d;

    // =========================
    // BACKLOG / PENDIENTES
    // =========================
    private int _intraPendingCount24h;
    private int _interPendingCount24h;

    private int _intraPendingLast7d;
    private int _interPendingLast7d;

    private double _intraPendingMaxAgeMin; // 7d
    private double _interPendingMaxAgeMin; // 7d

    // =========================
    // CALIDAD / ERRORES
    // =========================
    private double _intraFailTechRate24h;
    private double _interFailTechRate24h;

    private double _intraFailTechRate30d;
    private double _interFailTechRate30d;

    private double _interErrorState9Share24h;
    private double _interErrorState9Share30d;

    private int _intraErrorCount24h;
    private int _interErrorCount24h;

    private double _intraErrorMaxAgeMin7d;
    private double _interErrorMaxAgeMin7d;

    // =========================
    // Getters usados por ObservableGauge (lecturas consistentes)
    // =========================

    // Volumen
    public int IntraTxLast15m { get { lock (_gate) return _intraTxLast15m; } }
    public int InterTxLast15m { get { lock (_gate) return _interTxLast15m; } }

    public int IntraTxLast24h { get { lock (_gate) return _intraTxLast24h; } }
    public int InterTxLast24h { get { lock (_gate) return _interTxLast24h; } }

    public int IntraTxLast30d { get { lock (_gate) return _intraTxLast30d; } }
    public int InterTxLast30d { get { lock (_gate) return _interTxLast30d; } }

    // Pendientes / backlog
    public int IntraPendingCount24h { get { lock (_gate) return _intraPendingCount24h; } }
    public int InterPendingCount24h { get { lock (_gate) return _interPendingCount24h; } }

    public int IntraPendingLast7d { get { lock (_gate) return _intraPendingLast7d; } }
    public int InterPendingLast7d { get { lock (_gate) return _interPendingLast7d; } }

    public double IntraPendingMaxAgeMin { get { lock (_gate) return _intraPendingMaxAgeMin; } }
    public double InterPendingMaxAgeMin { get { lock (_gate) return _interPendingMaxAgeMin; } }

    // Calidad / errores
    public double IntraFailTechRate24h { get { lock (_gate) return _intraFailTechRate24h; } }
    public double InterFailTechRate24h { get { lock (_gate) return _interFailTechRate24h; } }

    public double IntraFailTechRate30d { get { lock (_gate) return _intraFailTechRate30d; } }
    public double InterFailTechRate30d { get { lock (_gate) return _interFailTechRate30d; } }

    public double InterErrorState9Share24h { get { lock (_gate) return _interErrorState9Share24h; } }
    public double InterErrorState9Share30d { get { lock (_gate) return _interErrorState9Share30d; } }

    public int IntraErrorCount24h { get { lock (_gate) return _intraErrorCount24h; } }
    public int InterErrorCount24h { get { lock (_gate) return _interErrorCount24h; } }

    public double IntraErrorMaxAgeMin7d { get { lock (_gate) return _intraErrorMaxAgeMin7d; } }
    public double InterErrorMaxAgeMin7d { get { lock (_gate) return _interErrorMaxAgeMin7d; } }

    // =========================
    // Update (1 lock): actualiza todo junto
    // =========================
    public void Update(
        // Volumen
        int intraTxLast15m,
        int interTxLast15m,
        int intraTxLast24h,
        int interTxLast24h,
        int intraTxLast30d,
        int interTxLast30d,

        // Pendientes
        int intraPendingCount24h,
        int interPendingCount24h,
        int intraPendingLast7d,
        int interPendingLast7d,
        double intraPendingMaxAgeMin,
        double interPendingMaxAgeMin,

        // Calidad / errores
        double intraFailTechRate24h,
        double interFailTechRate24h,
        double intraFailTechRate30d,
        double interFailTechRate30d,
        double interErrorState9Share24h,
        double interErrorState9Share30d,
        int intraErrorCount24h,
        int interErrorCount24h,
        double intraErrorMaxAgeMin7d,
        double interErrorMaxAgeMin7d)
    {
        lock (_gate)
        {
            // Volumen
            _intraTxLast15m = intraTxLast15m;
            _interTxLast15m = interTxLast15m;
            _intraTxLast24h = intraTxLast24h;
            _interTxLast24h = interTxLast24h;
            _intraTxLast30d = intraTxLast30d;
            _interTxLast30d = interTxLast30d;

            // Pendientes
            _intraPendingCount24h = intraPendingCount24h;
            _interPendingCount24h = interPendingCount24h;
            _intraPendingLast7d = intraPendingLast7d;
            _interPendingLast7d = interPendingLast7d;
            _intraPendingMaxAgeMin = intraPendingMaxAgeMin;
            _interPendingMaxAgeMin = interPendingMaxAgeMin;

            // Calidad / errores
            _intraFailTechRate24h = intraFailTechRate24h;
            _interFailTechRate24h = interFailTechRate24h;
            _intraFailTechRate30d = intraFailTechRate30d;
            _interFailTechRate30d = interFailTechRate30d;
            _interErrorState9Share24h = interErrorState9Share24h;
            _interErrorState9Share30d = interErrorState9Share30d;
            _intraErrorCount24h = intraErrorCount24h;
            _interErrorCount24h = interErrorCount24h;
            _intraErrorMaxAgeMin7d = intraErrorMaxAgeMin7d;
            _interErrorMaxAgeMin7d = interErrorMaxAgeMin7d;
        }
    }
}