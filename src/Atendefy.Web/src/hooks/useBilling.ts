import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/api/client';
import type {
  CreateSubscriptionRequest,
  InvoiceResult,
  Plan,
  SubscriptionResponse,
} from '@/types/api';

export function usePlans() {
  return useQuery({
    queryKey: ['billing-plans'],
    queryFn: () => apiClient.get<Plan[]>('/billing/plans').then((r) => r.data),
  });
}

export function useSubscription() {
  return useQuery({
    queryKey: ['billing-subscription'],
    queryFn: () =>
      apiClient
        .get<SubscriptionResponse>('/billing/subscription')
        .then((r) => r.data)
        .catch((err: { response?: { status?: number } }) => {
          if (err?.response?.status === 404) return null;
          throw err;
        }),
  });
}

export function useSubscribe() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateSubscriptionRequest) =>
      apiClient.post<InvoiceResult>('/billing/subscribe', req).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['billing-subscription'] }),
  });
}

export function useCancelSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiClient.delete('/billing/subscription').then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['billing-subscription'] }),
  });
}
