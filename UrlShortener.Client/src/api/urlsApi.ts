import { UrlRecord, CreateUrlPayload, UpdateUrlPayload, ApiErrorResponse } from '@/types/url';

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || window.location.origin;

async function handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
        const errorData: ApiErrorResponse = await response.json().catch(() => ({}));
        throw new Error(errorData.error || errorData.title || `Ошибка сервера: ${response.status}`);
    }

    if (response.status === 204) {
        return {} as T;
    }

    return response.json();
}

export const urlsApi = {
    async fetchUrls(signal?: AbortSignal): Promise<UrlRecord[]> {
        try {
            const response = await fetch(`${API_BASE_URL}/api/urls`, { signal });
            return await handleResponse<UrlRecord[]>(response);
        } catch (error) {
            // Трансформируем необработанный сетевой сбой браузерного fetch ('TypeError: Load failed')
            // в понятное пользователю сообщение с инструкцией по устранению.
            if (error instanceof TypeError && (error.message.includes('fetch') || error.message.includes('Load failed'))) {
                throw new Error(`Не удалось соединиться с API бэкенда (${API_BASE_URL}). Проверьте запуск сервера и настройки CORS.`);
            }
            throw error;
        }
    },

    async createUrl(payload: CreateUrlPayload, signal?: AbortSignal): Promise<UrlRecord> {
        try {
            const response = await fetch(`${API_BASE_URL}/api/urls`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
                signal,
            });
            return await handleResponse<UrlRecord>(response);
        } catch (error) {
            if (error instanceof TypeError && (error.message.includes('fetch') || error.message.includes('Load failed'))) {
                throw new Error('Не удалось отправить запрос. Сервер недоступен.');
            }
            throw error;
        }
    },

    async updateUrl(id: number, payload: UpdateUrlPayload, signal?: AbortSignal): Promise<UrlRecord> {
        const response = await fetch(`${API_BASE_URL}/api/urls/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
            signal,
        });
        return handleResponse<UrlRecord>(response);
    },

    async deleteUrl(id: number, signal?: AbortSignal): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/api/urls/${id}`, {
            method: 'DELETE',
            signal,
        });
        return handleResponse<void>(response);
    },
};