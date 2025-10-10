import React from 'react';
import Modal from './Modal';
import { ExclamationTriangleIcon } from '@heroicons/react/24/outline';

interface ConfirmDeleteModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  resourceLabel?: string; // 如：Git仓库、LLM配置
  itemName?: string;      // 展示名称
  warningText?: string;   // 额外警告文案
  isLoading?: boolean;
}

const ConfirmDeleteModal: React.FC<ConfirmDeleteModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
  resourceLabel = '资源',
  itemName,
  warningText,
  isLoading = false,
}) => {
  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="确认删除" size="sm">
      <div className="text-center">
        <ExclamationTriangleIcon className="mx-auto h-12 w-12 text-red-600" aria-hidden="true" />
        <div className="mt-3">
          <h3 className="text-lg font-medium text-gray-900">删除{resourceLabel}</h3>
          <div className="mt-2">
            <p className="text-sm text-gray-500">
              您确定要删除{resourceLabel}
              {itemName ? (
                <>
                  {' '}<strong>"{itemName}"</strong>
                </>
              ) : null}
              吗？
            </p>
            {warningText && (
              <p className="mt-2 text-sm text-red-600 font-medium">{warningText}</p>
            )}
            <p className="mt-2 text-sm text-gray-500">此操作无法撤销。</p>
          </div>
        </div>
      </div>

      <div className="mt-5 sm:mt-6 sm:flex sm:flex-row-reverse">
        <button
          type="button"
          onClick={onConfirm}
          disabled={isLoading}
          className="inline-flex w-full justify-center rounded-md border border-transparent bg-red-600 px-4 py-2 text-base font-medium text-white shadow-sm hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed sm:ml-3 sm:w-auto sm:text-sm"
        >
          {isLoading ? '删除中...' : '删除'}
        </button>
        <button
          type="button"
          onClick={onClose}
          disabled={isLoading}
          className="mt-3 inline-flex w-full justify-center rounded-md border border-gray-300 bg-white px-4 py-2 text-base font-medium text-gray-700 shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 sm:mt-0 sm:w-auto sm:text-sm"
        >
          取消
        </button>
      </div>
    </Modal>
  );
};

export default ConfirmDeleteModal;
