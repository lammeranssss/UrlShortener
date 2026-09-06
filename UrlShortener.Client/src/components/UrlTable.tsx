import React, { useState } from 'react';
import { UrlRecord } from '@/types/url';
import { API_BASE_URL } from '@/api/urlsApi';

interface UrlTableProps {
    urls: UrlRecord[];
    onEdit: (record: UrlRecord) => void;
    onDelete: (id: number) => Promise<void>;
}

export const UrlTable: React.FC<UrlTableProps> = ({ urls, onEdit, onDelete }) => {
    const [copiedCode, setCopiedCode] = useState<string | null>(null);

    const handleCopy = (shortCode: string) => {
        const redirectUrl = `${API_BASE_URL}/${shortCode}`;
        navigator.clipboard.writeText(redirectUrl);
        setCopiedCode(shortCode);
        setTimeout(() => setCopiedCode(null), 2000);
    };

    return (
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-100 bg-gray-50/50 flex justify-between items-center">
                <h2 className="text-base font-semibold text-gray-800">Список активных ссылок</h2>
                <span className="text-xs text-gray-500">
          Аналитика кликов обновляется каждые 3 сек.
        </span>
            </div>

            <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                    <thead className="bg-gray-50 text-gray-500 uppercase text-[11px] tracking-wider border-b border-gray-100">
                    <tr>
                        <th className="px-6 py-3 font-semibold">Короткий код</th>
                        <th className="px-6 py-3 font-semibold">Оригинальный URL</th>
                        <th className="px-6 py-3 font-semibold text-center">Клики</th>
                        <th className="px-6 py-3 font-semibold">Дата создания</th>
                        <th className="px-6 py-3 font-semibold text-right">Управление</th>
                    </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                    {urls.length === 0 ? (
                        <tr>
                            <td colSpan={5} className="px-6 py-8 text-center text-gray-400">
                                Нет созданных ссылок. Введите первый URL выше!
                            </td>
                        </tr>
                    ) : (
                        urls.map((record) => (
                            <tr key={record.id} className="hover:bg-gray-50/80 transition-colors">
                                <td className="px-6 py-4 font-mono text-blue-600 font-medium">
                                    <div className="flex items-center gap-2">
                                        {/* ПОЧЕМУ: rel="noopener noreferrer" изолирует window.opener, исключая Tabnabbing/Phishing */}
                                        <a
                                            href={`${API_BASE_URL}/${record.shortCode}`}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="hover:underline"
                                        >
                                            /{record.shortCode}
                                        </a>
                                        <button
                                            onClick={() => handleCopy(record.shortCode)}
                                            className="text-xs bg-gray-100 hover:bg-gray-200 text-gray-600 px-2 py-0.5 rounded transition"
                                            title="Скопировать ссылку"
                                        >
                                            {copiedCode === record.shortCode ? '✓ Скопировано' : 'Копия'}
                                        </button>
                                    </div>
                                </td>
                                <td className="px-6 py-4 max-w-xs truncate text-gray-600" title={record.originalUrl}>
                                    {record.originalUrl}
                                </td>
                                <td className="px-6 py-4 text-center">
                    <span className="bg-blue-50 text-blue-700 font-semibold px-2.5 py-1 rounded-full text-xs">
                      {record.clickCount.toLocaleString()}
                    </span>
                                </td>
                                <td className="px-6 py-4 text-gray-400 text-xs">
                                    {new Date(record.createdAt).toLocaleString('ru-RU')}
                                </td>
                                <td className="px-6 py-4 text-right space-x-2">
                                    <button
                                        onClick={() => onEdit(record)}
                                        className="text-gray-600 hover:text-blue-600 font-medium text-xs px-2 py-1 rounded hover:bg-blue-50 transition"
                                    >
                                        Редактировать
                                    </button>
                                    <button
                                        onClick={() => {
                                            if (confirm('Вы действительно хотите удалить эту ссылку?')) {
                                                onDelete(record.id);
                                            }
                                        }}
                                        className="text-red-600 hover:text-red-800 font-medium text-xs px-2 py-1 rounded hover:bg-red-50 transition"
                                    >
                                        Удалить
                                    </button>
                                </td>
                            </tr>
                        ))
                    )}
                    </tbody>
                </table>
            </div>
        </div>
    );
};