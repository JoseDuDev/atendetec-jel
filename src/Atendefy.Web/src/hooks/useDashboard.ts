import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/api/client';
import type { DashboardStats } from '@/types/api';

export function useDashboardStats() {
  return useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: () =>
      apiClient.get<DashboardStats>('/dashboard/stats').then((r) => r.data),
    refetchInterval: 60_000,
  });
}
