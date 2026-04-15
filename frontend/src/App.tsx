import { useEffect, useState } from 'react';
import axios from 'axios';

interface HealthResult {
  isHealthy: boolean;
  message: string;
  checkedAtUtc: string;
}

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:8080',
});

function App() {
  const [health, setHealth] = useState<HealthResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiClient
      .get<HealthResult>('/api/health')
      .then((res) => setHealth(res.data))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center p-6">
      <div className="bg-gray-900 rounded-2xl shadow-2xl p-10 max-w-md w-full text-center">
        {/* Logo / Brand */}
        <h1 className="text-4xl font-extrabold text-indigo-400 mb-2 tracking-tight">
          🔑 Borro
        </h1>
        <p className="text-gray-400 text-sm mb-8">Universal P2P Rental Marketplace</p>

        {/* Status Card */}
        <div className="rounded-xl border border-gray-700 bg-gray-800 p-6">
          <h2 className="text-lg font-semibold text-gray-200 mb-4">Backend Health Check</h2>

          {loading && (
            <p className="text-yellow-400 animate-pulse">Connecting to backend…</p>
          )}

          {error && (
            <div className="bg-red-900/40 border border-red-600 rounded-lg p-4">
              <p className="text-red-400 font-medium">❌ Connection Failed</p>
              <p className="text-red-300 text-sm mt-1 break-all">{error}</p>
            </div>
          )}

          {health && (
            <div className="space-y-3 text-left">
              <div className="flex items-center gap-2">
                <span className={`w-3 h-3 rounded-full ${health.isHealthy ? 'bg-green-400' : 'bg-red-500'}`} />
                <span className="text-gray-200 font-medium">
                  {health.isHealthy ? 'Healthy' : 'Unhealthy'}
                </span>
              </div>
              <p className="text-gray-300 text-sm">{health.message}</p>
              <p className="text-gray-500 text-xs">
                Checked at: {new Date(health.checkedAtUtc).toLocaleString()}
              </p>
            </div>
          )}
        </div>

        <p className="text-gray-600 text-xs mt-6">
          API: {import.meta.env.VITE_API_URL ?? 'http://localhost:8080'}
        </p>
      </div>
    </div>
  );
}

export default App;
