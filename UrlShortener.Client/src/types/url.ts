export interface UrlRecord {
    id: number;
    originalUrl: string;
    shortCode: string;
    clickCount: number;
    createdAt: string;
    lastAccessedAt: string | null;
}

export interface CreateUrlPayload {
    originalUrl: string;
}

export interface UpdateUrlPayload {
    originalUrl: string;
}

export interface ApiErrorResponse {
    error?: string;
    title?: string;
    status?: number;
}