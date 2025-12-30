/**
 * 管理后台 AJAX 通用函数库
 * 用于处理表单提交和操作反馈,避免页面刷新
 */

// Toast 提示管理器
const ToastManager = {
    container: null,

    // 初始化 Toast 容器
    init() {
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            this.container.style.zIndex = '9999';
            document.body.appendChild(this.container);
        }
    },

    // 显示 Toast 消息
    show(message, type = 'info', duration = 3000) {
        this.init();

        const toastId = 'toast-' + Date.now();
        const iconMap = {
            success: 'check-circle',
            error: 'x-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        const colorMap = {
            success: 'success',
            error: 'danger',
            warning: 'warning',
            info: 'primary'
        };

        const toastHTML = `
            <div id="${toastId}" class="toast show" role="alert">
                <div class="toast-header bg-${colorMap[type]} text-white">
                    <i class="bi bi-${iconMap[type]} me-2"></i>
                    <strong class="me-auto">操作结果</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;

        this.container.insertAdjacentHTML('beforeend', toastHTML);
        const toastElement = document.getElementById(toastId);

        // 自动隐藏
        setTimeout(() => {
            const bsToast = new bootstrap.Toast(toastElement);
            bsToast.hide();
            setTimeout(() => toastElement.remove(), 500);
        }, duration);
    },

    success(message) {
        this.show(message, 'success');
    },

    error(message) {
        this.show(message, 'error', 5000);
    },

    warning(message) {
        this.show(message, 'warning');
    },

    info(message) {
        this.show(message, 'info');
    }
};

// AJAX 表单提交处理器
const AjaxForm = {
    /**
     * 提交表单数据
     * @param {HTMLFormElement} form - 表单元素
     * @param {Object} options - 配置选项
     * @returns {Promise}
     */
    submit(form, options = {}) {
        const defaults = {
            onSuccess: null,          // 成功回调
            onError: null,            // 错误回调
            successMessage: '操作成功',  // 成功提示
            showLoading: true,        // 是否显示加载状态
            resetForm: false,         // 成功后是否重置表单
            closeModal: false,        // 成功后是否关闭模态框
            modalId: null            // 模态框ID
        };

        const config = { ...defaults, ...options };
        const formData = new FormData(form);
        const action = form.action || window.location.href;
        const button = form.querySelector('button[type="submit"]');

        // 禁用提交按钮,显示加载状态
        if (config.showLoading && button) {
            button.disabled = true;
            const originalText = button.innerHTML;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>处理中...';
            button.dataset.originalText = originalText;
        }

        return fetch(action, {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value,
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json, text/plain, */*'
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return response.json().catch(() => {
                // 如果不是 JSON 响应,尝试解析为文本
                return { success: response.ok, message: '操作已完成' };
            });
        })
        .then(result => {
            // 恢复按钮状态
            if (config.showLoading && button) {
                button.disabled = false;
                button.innerHTML = button.dataset.originalText || button.innerHTML;
            }

            if (result.success) {
                // 显示成功消息
                ToastManager.success(result.message || config.successMessage);

                // 重置表单
                if (config.resetForm) {
                    form.reset();
                }

                // 关闭模态框
                if (config.closeModal && config.modalId) {
                    const modal = bootstrap.Modal.getInstance(document.getElementById(config.modalId));
                    if (modal) modal.hide();
                }

                // 执行成功回调
                if (config.onSuccess) {
                    config.onSuccess(result);
                }
            } else {
                // 显示错误消息
                ToastManager.error(result.message || '操作失败');

                // 执行错误回调
                if (config.onError) {
                    config.onError(result);
                }
            }

            return result;
        })
        .catch(error => {
            console.error('AJAX Error:', error);

            // 恢复按钮状态
            if (config.showLoading && button) {
                button.disabled = false;
                button.innerHTML = button.dataset.originalText || button.innerHTML;
            }

            // 显示错误消息
            ToastManager.error(error.message || '网络请求失败,请重试');

            // 执行错误回调
            if (config.onError) {
                config.onError(error);
            }

            throw error;
        });
    },

    /**
     * 绑定表单自动提交 (用于页面级别的表单绑定)
     * @param {string} selector - 表单选择器
     * @param {Object} options - 配置选项
     */
    bind(selector, options = {}) {
        document.addEventListener('submit', function(e) {
            const form = e.target;
            if (form.matches(selector)) {
                e.preventDefault();
                AjaxForm.submit(form, options);
            }
        });
    },

    /**
     * 绑定单个表单
     * @param {HTMLFormElement} form - 表单元素
     * @param {Object} options - 配置选项
     */
    bindOne(form, options = {}) {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            AjaxForm.submit(form, options);
        });
    }
};

// 确认提示辅助函数
function confirmAction(message, callback) {
    if (confirm(message)) {
        callback();
    }
}

// 修复表格内下拉菜单被遮挡问题
const DropdownFixer = {
    menuData: new WeakMap(),

    init() {
        const self = this;

        // 监听下拉菜单显示事件（使用 show 而不是 shown，更早处理）
        document.addEventListener('show.bs.dropdown', function(e) {
            const toggle = e.target;
            if (!toggle.closest('.table')) return;

            const btnGroup = toggle.closest('.btn-group');
            const menu = btnGroup ? btnGroup.querySelector('.dropdown-menu') : toggle.nextElementSibling;

            if (menu && menu.classList.contains('dropdown-menu')) {
                // 保存原始位置信息到 WeakMap
                self.menuData.set(menu, {
                    parent: menu.parentNode,
                    nextSibling: menu.nextSibling
                });

                // 计算按钮位置
                const rect = toggle.getBoundingClientRect();

                // 移动菜单到 body
                document.body.appendChild(menu);

                // 设置固定定位
                menu.style.cssText = `
                    position: fixed !important;
                    z-index: 9999 !important;
                    top: ${rect.bottom + 2}px !important;
                    right: ${window.innerWidth - rect.right}px !important;
                    left: auto !important;
                    transform: none !important;
                    inset: auto !important;
                `;
            }
        });

        // 监听下拉菜单隐藏事件
        document.addEventListener('hide.bs.dropdown', function(e) {
            const toggle = e.target;
            if (!toggle.closest('.table') && !self.menuData.size) return;

            // 查找 body 中的 dropdown-menu
            const menus = document.body.querySelectorAll(':scope > .dropdown-menu');
            menus.forEach(function(menu) {
                const data = self.menuData.get(menu);
                if (data) {
                    // 重置样式
                    menu.style.cssText = '';

                    // 移回原位置
                    if (data.nextSibling) {
                        data.parent.insertBefore(menu, data.nextSibling);
                    } else {
                        data.parent.appendChild(menu);
                    }

                    self.menuData.delete(menu);
                }
            });
        });
    }
};

// 页面加载完成后自动初始化
document.addEventListener('DOMContentLoaded', function() {
    ToastManager.init();
    DropdownFixer.init();
});
