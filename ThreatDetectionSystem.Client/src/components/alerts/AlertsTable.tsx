import type { Alert } from '../../types';
import { format } from 'date-fns';
import { Clock } from 'lucide-react';

const severityStyles: Record<string, string> = {
  Critical: 'bg-red-500/20 text-red-400 border-red-500/30',
  High:     'bg-orange-500/20 text-orange-400 border-orange-500/30',
  Medium:   'bg-yellow-500/20 text-yellow-400 border-yellow-500/30',
  Low:      'bg-blue-500/20 text-blue-400 border-blue-500/30',
  Info:     'bg-gray-500/20 text-gray-400 border-gray-500/30',
};

export default function AlertsTable({ 
  alerts, 
  highlightIds,
  loading = false,
  title = "Alerts",
  showTimeRange = false
}: { 
  alerts: Alert[]; 
  highlightIds: Set<string>;
  loading?: boolean;
  title?: string;
  showTimeRange?: boolean;
}) {
  return (
    <div className="bg-zinc-900 border border-zinc-800 rounded-2xl overflow-hidden">
      {/* Header */}
      <div className="px-6 py-5 border-b border-zinc-800 flex items-center justify-between bg-zinc-950">
        <div className="flex items-center gap-3">
          <Clock className="w-5 h-5 text-emerald-500" />
          <h2 className="text-lg font-semibold">{title}</h2>
          {showTimeRange && (
            <span className="text-xs text-zinc-500">({alerts.length} alerts)</span>
          )}
        </div>
        
        {!showTimeRange && (
          <span className="text-emerald-400 text-sm font-medium flex items-center gap-2">
            <div className="w-2 h-2 bg-emerald-500 rounded-full animate-pulse" />
            LIVE
          </span>
        )}
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-zinc-800 text-xs text-zinc-400 bg-zinc-950">
              <th className="px-6 py-4 text-left">TIME</th>
              <th className="px-6 py-4 text-left">SOURCE IP</th>
              <th className="px-6 py-4 text-left">DESTINATION IP</th>
              <th className="px-6 py-4 text-left">SEVERITY</th>
              <th className="px-6 py-4 text-left">MESSAGE</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-800">
            {alerts.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-6 py-16 text-center text-zinc-500">
                  {loading ? "Loading alerts..." : "No alerts found"}
                </td>
              </tr>
            ) : (
              alerts.map((alert) => (
                <tr 
                  key={alert.id} 
                  className={`transition-all duration-700 hover:bg-zinc-800/70 ${
                    highlightIds.has(alert.id) 
                      ? 'bg-emerald-500/10 border-l-4 border-emerald-500' 
                      : ''
                  }`}
                >
                  <td className="px-6 py-4 font-mono text-sm text-zinc-400">
                    {format(new Date(alert.timestamp), 'HH:mm:ss')}
                  </td>
                  <td className="px-6 py-4 font-mono text-sm">{alert.source_IP}</td>
                  <td className="px-6 py-4 font-mono text-sm">{alert.destination_IP}</td>
                  <td className="px-6 py-4">
                    <span className={`px-4 py-1 text-xs font-semibold rounded-full border ${severityStyles[alert.severity] || severityStyles.Info}`}>
                      {alert.severity}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-zinc-200">
                    {alert.message}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}