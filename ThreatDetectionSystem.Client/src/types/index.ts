export interface Alert {
  id: string;
  source_IP: string;
  destination_IP: string;
  severity: string;           // Critical, High, ...
  message: string;
  timestamp: string;          // ISO string
}

export interface AlertStats {
  critical: number;
  high: number;
  medium: number;
  low: number;
  info: number;
  total: number;
}