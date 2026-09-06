import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { urlsApi } from '@/api/urlsApi';
import { CreateUrlPayload, UpdateUrlPayload } from '@/types/url';

export const URLS_QUERY_KEY = ['urls'] as const;

export function useUrls() {
    const queryClient = useQueryClient();

    // refetchInterval = 3000ms обеспечивает актуальность аналитики кликов,
    // а signal пробрасывается в urlsApi для корректной отмены сетевых сессий.
    const urlsQuery = useQuery({
        queryKey: URLS_QUERY_KEY,
        queryFn: ({ signal }) => urlsApi.fetchUrls(signal),
        refetchInterval: 3000,
    });

    const createMutation = useMutation({
        mutationFn: (payload: CreateUrlPayload) => urlsApi.createUrl(payload),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: URLS_QUERY_KEY });
        },
    });

    const updateMutation = useMutation({
        mutationFn: ({ id, payload }: { id: number; payload: UpdateUrlPayload }) =>
            urlsApi.updateUrl(id, payload),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: URLS_QUERY_KEY });
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: number) => urlsApi.deleteUrl(id),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: URLS_QUERY_KEY });
        },
    });

    return {
        urls: urlsQuery.data ?? [],
        isLoading: urlsQuery.isLoading,
        isError: urlsQuery.isError,
        error: urlsQuery.error,
        createUrl: createMutation.mutateAsync,
        isCreating: createMutation.isPending,
        updateUrl: updateMutation.mutateAsync,
        isUpdating: updateMutation.isPending,
        deleteUrl: deleteMutation.mutateAsync,
        isDeleting: deleteMutation.isPending,
    };
}