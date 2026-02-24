namespace Bank.Obs.SqlPoller.State;

public sealed class MetricState
{
    private readonly object _gate = new();

    private int _intraTxLast30d;
    private int _interTxLast30d;
    private int _intraPendingLast7d;
    private int _interPendingLast7d;

    private double _intraFailTechRate30d;
    private double _interFailTechRate30d;
    private double _intraState9InterShare30d;
    private double _intraPendingMaxAgeMin;
    private double _interPendingMaxAgeMin;

    // Getters usados por ObservableGauge (lecturas consistentes)
    public int IntraTxLast30d { get { lock (_gate) return _intraTxLast30d; } }
    public int InterTxLast30d { get { lock (_gate) return _interTxLast30d; } }
    public int IntraPendingLast7d { get { lock (_gate) return _intraPendingLast7d; } }
    public int InterPendingLast7d { get { lock (_gate) return _interPendingLast7d; } }

    public double IntraFailTechRate30d { get { lock (_gate) return _intraFailTechRate30d; } }
    public double InterFailTechRate30d { get { lock (_gate) return _interFailTechRate30d; } }
    public double IntraState9InterShare30d { get { lock (_gate) return _intraState9InterShare30d; } }
    public double IntraPendingMaxAgeMin { get { lock (_gate) return _intraPendingMaxAgeMin; } }
    public double InterPendingMaxAgeMin { get { lock (_gate) return _interPendingMaxAgeMin; } }

    // Método único para actualizar todo junto (1 lock)
    public void Update(
        int intraTxLast30d,
        int interTxLast30d,
        int intraPendingLast7d,
        int interPendingLast7d,
        double intraFailTechRate30d,
        double interFailTechRate30d,
        double intraState9InterShare30d,
        double intraPendingMaxAgeMin,
        double interPendingMaxAgeMin)
    {
        lock (_gate)
        {
            _intraTxLast30d = intraTxLast30d;
            _interTxLast30d = interTxLast30d;
            _intraPendingLast7d = intraPendingLast7d;
            _interPendingLast7d = interPendingLast7d;
            _intraFailTechRate30d = intraFailTechRate30d;
            _interFailTechRate30d = interFailTechRate30d;
            _intraState9InterShare30d = intraState9InterShare30d;
            _intraPendingMaxAgeMin = intraPendingMaxAgeMin;
            _interPendingMaxAgeMin = interPendingMaxAgeMin;
        }
    }
}