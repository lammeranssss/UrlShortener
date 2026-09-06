import { useState } from 'react';
import { useUrls } from '@/hooks/useUrls';
import { UrlForm } from '@/components/UrlForm';
import { UrlTable } from '@/components/UrlTable';
import { EditUrlModal } from '@/components/EditUrlModal';
import { UrlRecord } from '@/types/url';

export function App() {
  const { urls, isLoading, isError, error, createUrl, isCreating, updateUrl, isUpdating, deleteUrl } = useUrls();
  const [editingRecord, setEditingRecord] = useState<UrlRecord | null>(null);

  const handleCreate = async (originalUrl: string) => {
    await createUrl({ originalUrl });
  };

  const handleUpdate = async (id: number, newOriginalUrl: string) => {
    await updateUrl({ id, payload: { originalUrl: newOriginalUrl } });
  };

  return (
      <div className="min-h-screen bg-gray-50/50 text-gray-900 antialiased font-sans">
        <div className="max-w-5xl mx-auto px-4 py-10">
          <header className="mb-8">
            <h1 className="text-2xl font-bold text-gray-900">URL Shortener</h1>
            <p className="text-sm text-gray-500 mt-1">High-Performance Minimal Redirect Engine (.NET 9 + React SPA)</p>
          </header>

          <main>
            <UrlForm onSubmit={handleCreate} isLoading={isCreating} />

            {isLoading ? (
                <div className="text-center py-12 text-gray-500 text-sm flex items-center justify-center gap-2">
                  <span className="inline-block animate-spin rounded-full h-4 w-4 border-2 border-gray-400 border-t-transparent" />
                  Загрузка аналитики с сервера...
                </div>
            ) : isError ? (
                <div className="p-4 bg-red-50 border-l-4 border-red-500 text-red-700 rounded-r-lg text-sm shadow-sm">
                  <p className="font-semibold">Ошибка подключения к API</p>
                  <p className="mt-1 text-xs">{error?.message}</p>
                </div>
            ) : (
                <UrlTable
                    urls={urls}
                    onEdit={(record) => setEditingRecord(record)}
                    onDelete={deleteUrl}
                />
            )}
          </main>

          <EditUrlModal
              urlRecord={editingRecord}
              isOpen={!!editingRecord}
              onClose={() => setEditingRecord(null)}
              onSave={handleUpdate}
              isLoading={isUpdating}
          />
        </div>
      </div>
  );
}