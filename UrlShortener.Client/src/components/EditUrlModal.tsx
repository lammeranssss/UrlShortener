import React, { useState, useEffect } from 'react';
import { UrlRecord } from '@/types/url';

interface EditUrlModalProps {
    urlRecord: UrlRecord | null;
    isOpen: boolean;
    onClose: () => void;
    onSave: (id: number, newOriginalUrl: string) => Promise<void>;
    isLoading: boolean;
}

export const EditUrlModal: React.FC<EditUrlModalProps> = ({
                                                              urlRecord,
                                                              isOpen,
                                                              onClose,
                                                              onSave,
                                                              isLoading,
                                                          }) => {
    const [originalUrl, setOriginalUrl] = useState('');
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (urlRecord) {
            setOriginalUrl(urlRecord.originalUrl);
            setError(null);
        }
    }, [urlRecord]);

    if (!isOpen || !urlRecord) return null;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        try {
            await onSave(urlRecord.id, originalUrl.trim());
            onClose();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Не удалось обновить URL');
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
            <div className="bg-white rounded-xl shadow-xl max-w-lg w-full p-6 m-4 border border-gray-100">
                <h3 className="text-lg font-semibold text-gray-800 mb-2">Редактировать длинную ссылку</h3>
                <p className="text-xs text-gray-500 mb-4 font-mono">Короткий код: /{urlRecord.shortCode}</p>

                <form onSubmit={handleSubmit}>
                    <div className="mb-4">
                        <label className="block text-xs font-medium text-gray-700 mb-1">Оригинальный URL</label>
                        <input
                            type="url"
                            value={originalUrl}
                            onChange={(e) => setOriginalUrl(e.target.value)}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 outline-none"
                            required
                            disabled={isLoading}
                        />
                    </div>

                    {error && (
                        <div className="mb-4 p-2.5 bg-red-50 border-l-4 border-red-500 text-red-700 text-xs rounded">
                            {error}
                        </div>
                    )}

                    <div className="flex justify-end gap-2">
                        <button
                            type="button"
                            onClick={onClose}
                            disabled={isLoading}
                            className="px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 rounded-lg transition"
                        >
                            Отмена
                        </button>
                        <button
                            type="submit"
                            disabled={isLoading}
                            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition disabled:bg-blue-400"
                        >
                            {isLoading ? 'Сохранение...' : 'Сохранить'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};