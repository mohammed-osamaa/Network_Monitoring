import type { AlertStats } from '../../types';
import { AlertTriangle, AlertCircle, ShieldAlert, Shield, Info } from 'lucide-react';

const severityConfig = {
  Critical: { label: 'Critical', color: 'text-red-500', bg: 'bg-red-500/10', icon: AlertTriangle },
  High:     { label: 'High',     color: 'text-orange-500', bg: 'bg-orange-500/10', icon: AlertCircle },
  Medium:   { label: 'Medium',   color: 'text-yellow-500', bg: 'bg-yellow-500/10', icon: ShieldAlert },
  Low:      { label: 'Low',      color: 'text-blue-500', bg: 'bg-blue-500/10', icon: Shield },
  Info:     { label: 'Info',     color: 'text-gray-400', bg: 'bg-gray-500/10', icon: Info },
};

export default function StatsCards({ stats }: { stats: AlertStats }) {
  const items = [
    { key: 'critical', value: stats.critical },
    { key: 'high', value: stats.high },
    { key: 'medium', value: stats.medium },
    { key: 'low', value: stats.low },
    { key: 'info', value: stats.info },
  ] as const;

  return (
    <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
      {items.map(({ key, value }) => {
        const config = severityConfig[key.charAt(0).toUpperCase() + key.slice(1) as keyof typeof severityConfig];
        const Icon = config.icon;

        return (
          <div key={key} className="bg-zinc-900 border border-zinc-800 rounded-2xl p-6 hover:border-zinc-700 transition-all group">
            <div className="flex justify-between items-start">
              <div>
                <p className="text-zinc-400 text-sm font-medium">{config.label}</p>
                <p className={`text-4xl font-bold mt-4 ${config.color}`}>{value}</p>
              </div>
              <div className={`p-4 rounded-2xl ${config.bg} group-hover:scale-110 transition-transform`}>
                <Icon className={`w-9 h-9 ${config.color}`} />
              </div>
            </div>
          </div>
        );
      })}

      {/* Total Card */}
      <div className="bg-zinc-900 border border-zinc-700 rounded-2xl p-6 col-span-1 md:col-span-5 lg:col-span-1 flex flex-col justify-center">
        <p className="text-zinc-400 text-sm">Total Alerts</p>
        <p className="text-5xl font-bold mt-2 text-white">{stats.total}</p>
      </div>
    </div>
  );
}