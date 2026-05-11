import { useState, useEffect, useRef } from 'react';
import StatsCards from './components/dashboard/StatsCards';
import AlertsTable from './components/alerts/AlertsTable';
import { alertApi } from './services/api';
import { initSignalR } from './services/signalR';
import type { Alert, AlertStats } from './types';

function App() {
  const [liveAlerts, setLiveAlerts] = useState<Alert[]>([]);
  const [filteredAlerts, setFilteredAlerts] = useState<Alert[]>([]);
  const [stats, setStats] = useState<AlertStats | null>(null);
  const [selectedRange, setSelectedRange] = useState('15m');
  const [loading, setLoading] = useState(false);

  const ranges = ['30s', '1m', '5m', '15m', '1h', '24h'];
  const highlightIds = useRef<Set<string>>(new Set());

  // Fetch filtered alerts when range changes
  const fetchFilteredAlerts = async (range: string) => {
    try {
      setLoading(true);
      const [alertsRes, statsRes] = await Promise.all([
        alertApi.getAlerts(range),
        alertApi.getStats(range)
      ]);
      setFilteredAlerts(alertsRes.data);
      setStats(statsRes.data);
    } catch (error) {
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFilteredAlerts(selectedRange);
  }, [selectedRange]);

  // Real-time Live Alerts
  useEffect(() => {
    const connection = initSignalR((newAlerts: Alert[]) => {
      setLiveAlerts(prev => {
        const combined = [...newAlerts, ...prev];
        return Array.from(new Map(combined.map(a => [a.id, a])).values())
          .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
          .slice(0, 50); // آخر 50 alert فقط للـ Live
      });

      newAlerts.forEach(alert => highlightIds.current.add(alert.id));
      setTimeout(() => highlightIds.current.clear(), 6000);
    });

    // Refresh stats
    const interval = setInterval(() => {
      alertApi.getStats(selectedRange).then(res => setStats(res.data)).catch(console.error);
    }, 12000);

    return () => {
      connection?.stop();
      clearInterval(interval);
    };
  }, [selectedRange]);

  return (
    <div className="min-h-screen bg-zinc-950 text-white">
      <div className="max-w-7xl mx-auto p-6">
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-4xl font-bold">Threat Detection Dashboard</h1>
          <div className="text-emerald-400 flex items-center gap-2">
            <div className="w-3 h-3 bg-emerald-500 rounded-full animate-pulse" />
            LIVE
          </div>
        </div>

        {stats && <StatsCards stats={stats} />}

        <div className="mt-10 grid grid-cols-1 lg:grid-cols-2 gap-8">
          
          {/* ==================== LIVE ALERTS ==================== */}
          <div>
            <div className="flex items-center gap-3 mb-4">
              <div className="w-4 h-4 bg-emerald-500 rounded-full animate-pulse" />
              <h2 className="text-2xl font-semibold">Live Alerts (Real-time)</h2>
            </div>
            <AlertsTable 
              alerts={liveAlerts} 
              highlightIds={highlightIds.current} 
              title="Live Feed"
              showTimeRange={false}
            />
          </div>

          {/* ==================== FILTERED ALERTS ==================== */}
          <div>
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-2xl font-semibold">Filtered Alerts</h2>
              
              <div className="flex gap-2 bg-zinc-900 p-1 rounded-xl border border-zinc-800">
                {ranges.map(range => (
                  <button
                    key={range}
                    onClick={() => setSelectedRange(range)}
                    className={`px-5 py-2 rounded-lg text-sm font-medium transition-all ${
                      selectedRange === range 
                        ? 'bg-white text-black' 
                        : 'hover:bg-zinc-800 text-zinc-400'
                    }`}
                  >
                    {range}
                  </button>
                ))}
              </div>
            </div>
            
            <AlertsTable 
              alerts={filteredAlerts} 
              highlightIds={new Set()} 
              title="Time Range View"
              loading={loading}
              showTimeRange={true}
            />
          </div>

        </div>
      </div>
    </div>
  );
}

export default App;