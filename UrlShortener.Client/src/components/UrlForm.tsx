import React, { useState } from 'react';

interface UrlFormProps {
    onSubmit: (url: string) => Promise<void>;
    isLoading: boolean;
}

export const UrlForm: React.FC<UrlFormProps> = ({ onSubmit, isLoading }) => {
    const [url, setUrl] = useState('');
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!url.trim()) {
            setError('Пожалуйста, введите URL адрес');
            return;
        }

        try {
            await onSubmit(url.trim());
            setUrl('');
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Неизвестная ошибка');
        }
    };

    return (
        <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 mb-8">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">Сократить новую ссылку</h2>
            <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3">
                <input
                    type="url"
                    value={url}
                    onChange={(e) => setUrl(e.target.value)}
                    placeholder="https://example.com/long-url-path"
                    className="flex-1 px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition text-sm"
                    disabled={isLoading}
                    required
                />
                <button
                    type="submit"
                    disabled={isLoading}
                    className="bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-medium px-6 py-2.5 rounded-lg transition-colors text-sm flex items-center justify-center min-w-[130px]"
                >
                    {isLoading ? (
                        <span className="inline-block animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                    ) : (
                        'Сократить'
                    )}
                </button>
            </form>

            {error && (
                <div className="mt-3 p-3 bg-red-50 border-l-4 border-red-500 text-red-700 text-sm rounded">
                    {error}
                </div>
            )}
        </div>
    );
};