/**
 * 主题切换控制器
 * 用于处理深色/浅色主题的切换和持久化
 */

(function() {
    'use strict';

    const ThemeSwitcher = {
        // 配置
        config: {
            storageKey: 'zircon-theme',
            themes: {
                light: 'light',
                dark: 'dark'
            },
            defaultTheme: 'dark',
            transitionDuration: 300
        },

        // DOM 元素引用
        elements: {
            toggleBtn: null,
            html: null
        },

        // 当前状态
        state: {
            currentTheme: null
        },

        /**
         * 初始化主题切换器
         */
        init() {
            // 获取 DOM 元素
            this.elements.html = document.documentElement;
            this.elements.toggleBtn = document.getElementById('themeToggle');

            // 如果主题切换按钮不存在，直接返回
            if (!this.elements.toggleBtn) {
                console.warn('主题切换按钮未找到');
                return;
            }

            // 加载保存的主题或检测系统偏好
            this.loadTheme();

            // 应用主题
            this.applyTheme(this.state.currentTheme);

            // 绑定事件
            this.bindEvents();

            // 触发初始化完成事件
            this.dispatchEvent('init');
        },

        /**
         * 绑定事件监听器
         */
        bindEvents() {
            // 主题切换按钮点击事件
            this.elements.toggleBtn.addEventListener('click', () => {
                this.toggle();
            });

            // 键盘快捷键 (Ctrl/Cmd + Shift + T)
            document.addEventListener('keydown', (e) => {
                if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'T') {
                    e.preventDefault();
                    this.toggle();
                }
            });

            // 监听系统主题变化
            if (window.matchMedia) {
                const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');
                darkModeQuery.addEventListener('change', (e) => {
                    // 只在用户未手动设置主题时跟随系统
                    const savedTheme = this.getSavedTheme();
                    if (savedTheme === null) {
                        const systemTheme = e.matches ? 'dark' : 'light';
                        this.applyTheme(systemTheme);
                    }
                });
            }
        },

        /**
         * 切换主题
         * @param {string} forceTheme - 强制设置主题（可选）
         */
        toggle(forceTheme) {
            const newTheme = forceTheme ||
                (this.state.currentTheme === this.config.themes.dark
                    ? this.config.themes.light
                    : this.config.themes.dark);

            this.applyTheme(newTheme);
            this.saveTheme(newTheme);
        },

        /**
         * 切换到深色主题
         */
        toDark() {
            this.toggle(this.config.themes.dark);
        },

        /**
         * 切换到浅色主题
         */
        toLight() {
            this.toggle(this.config.themes.light);
        },

        /**
         * 应用主题到 DOM
         * @param {string} theme - 主题名称 ('light' 或 'dark')
         */
        applyTheme(theme) {
            if (!this.isValidTheme(theme)) {
                console.warn(`无效的主题: ${theme}`);
                return;
            }

            // 添加切换动画
            this.elements.toggleBtn.classList.add('switching');

            // 设置 data-theme 属性
            if (theme === 'dark') {
                this.elements.html.removeAttribute('data-theme');
            } else {
                this.elements.html.setAttribute('data-theme', theme);
            }

            // 更新状态
            this.state.currentTheme = theme;

            // 更新按钮 aria-label
            const label = theme === 'dark' ? '切换到浅色主题' : '切换到深色主题';
            this.elements.toggleBtn.setAttribute('aria-label', label);
            this.elements.toggleBtn.setAttribute('title', label);

            // 移除切换动画
            setTimeout(() => {
                this.elements.toggleBtn.classList.remove('switching');
            }, this.config.transitionDuration);

            // 触发主题变化事件
            this.dispatchEvent('change', { theme });
        },

        /**
         * 加载主题
         * 优先级: localStorage > 系统偏好 > 默认主题
         */
        loadTheme() {
            const savedTheme = this.getSavedTheme();

            if (savedTheme !== null) {
                // 使用保存的主题
                this.state.currentTheme = savedTheme;
            } else {
                // 检测系统主题偏好
                const systemTheme = this.getSystemTheme();
                this.state.currentTheme = systemTheme;
            }
        },

        /**
         * 获取保存的主题
         * @returns {string|null} 主题名称或 null
         */
        getSavedTheme() {
            try {
                const saved = localStorage.getItem(this.config.storageKey);
                if (saved && this.isValidTheme(saved)) {
                    return saved;
                }
            } catch (e) {
                console.warn('无法读取保存的主题:', e);
            }
            return null;
        },

        /**
         * 保存主题到 localStorage
         * @param {string} theme - 主题名称
         */
        saveTheme(theme) {
            try {
                localStorage.setItem(this.config.storageKey, theme);
            } catch (e) {
                console.warn('无法保存主题:', e);
            }
        },

        /**
         * 检测系统主题偏好
         * @returns {string} 系统主题名称
         */
        getSystemTheme() {
            if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                return this.config.themes.dark;
            }
            return this.config.themes.light;
        },

        /**
         * 验证主题名称是否有效
         * @param {string} theme - 主题名称
         * @returns {boolean} 是否有效
         */
        isValidTheme(theme) {
            return Object.values(this.config.themes).includes(theme);
        },

        /**
         * 触发主题相关事件
         * @param {string} eventType - 事件类型 ('init' 或 'change')
         * @param {Object} detail - 事件详情
         */
        dispatchEvent(eventType, detail = {}) {
            const eventName = eventType === 'init'
                ? 'themeInit'
                : 'themeChange';

            const event = new CustomEvent(eventName, {
                detail: {
                    theme: this.state.currentTheme,
                    ...detail
                }
            });

            window.dispatchEvent(event);
        },

        /**
         * 获取当前主题
         * @returns {string} 当前主题名称
         */
        getCurrentTheme() {
            return this.state.currentTheme;
        },

        /**
         * 重置为系统默认主题
         */
        reset() {
            try {
                localStorage.removeItem(this.config.storageKey);
            } catch (e) {
                console.warn('无法重置主题:', e);
            }

            const systemTheme = this.getSystemTheme();
            this.applyTheme(systemTheme);
        }
    };

    // 将控制器暴露到全局
    window.ThemeSwitcher = ThemeSwitcher;

    // 页面加载完成后初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            ThemeSwitcher.init();
        });
    } else {
        ThemeSwitcher.init();
    }

    // 导出供其他模块使用
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = ThemeSwitcher;
    }

})();
