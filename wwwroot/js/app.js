/**
 * IGD Support - Core JavaScript Library
 * Shared utilities and components
 */

const App = (function () {
    'use strict';

    // Configuration
    const config = {
        animationDuration: 300,
        debounceDelay: 250,
        toastDuration: 5000
    };

    // ===== UTILITY FUNCTIONS =====
    
    /**
     * Debounce function to limit execution rate
     */
    function debounce(func, wait = config.debounceDelay) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    /**
     * Throttle function to limit execution frequency
     */
    function throttle(func, limit) {
        let inThrottle;
        return function (...args) {
            if (!inThrottle) {
                func.apply(this, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }

    /**
     * Format date to locale string
     */
    function formatDate(date, options = {}) {
        const defaults = { year: 'numeric', month: 'short', day: 'numeric' };
        return new Date(date).toLocaleDateString(undefined, { ...defaults, ...options });
    }

    /**
     * Generate unique ID
     */
    function generateId(prefix = 'id') {
        return `${prefix}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    // ===== DOM UTILITIES =====
    
    /**
     * Query selector shorthand
     */
    function $(selector, context = document) {
        return context.querySelector(selector);
    }

    /**
     * Query selector all shorthand
     */
    function $$(selector, context = document) {
        return [...context.querySelectorAll(selector)];
    }

    /**
     * Add event listener with delegation support
     */
    function on(element, event, selector, handler) {
        if (typeof selector === 'function') {
            handler = selector;
            element.addEventListener(event, handler);
        } else {
            element.addEventListener(event, (e) => {
                const target = e.target.closest(selector);
                if (target) {
                    handler.call(target, e);
                }
            });
        }
    }

    /**
     * Create element with attributes and children
     */
    function createElement(tag, attributes = {}, children = []) {
        const element = document.createElement(tag);
        
        Object.entries(attributes).forEach(([key, value]) => {
            if (key === 'className') {
                element.className = value;
            } else if (key === 'dataset') {
                Object.entries(value).forEach(([dataKey, dataValue]) => {
                    element.dataset[dataKey] = dataValue;
                });
            } else if (key.startsWith('on') && typeof value === 'function') {
                element.addEventListener(key.slice(2).toLowerCase(), value);
            } else {
                element.setAttribute(key, value);
            }
        });
        
        children.forEach(child => {
            if (typeof child === 'string') {
                element.appendChild(document.createTextNode(child));
            } else if (child instanceof Node) {
                element.appendChild(child);
            }
        });
        
        return element;
    }

    // ===== NAVIGATION =====
    
    function initNavigation() {
        const navToggle = $('.nav-toggle');
        const navLinks = $('.nav-links');
        
        if (navToggle && navLinks) {
            on(navToggle, 'click', () => {
                navLinks.classList.toggle('active');
                const isExpanded = navLinks.classList.contains('active');
                navToggle.setAttribute('aria-expanded', isExpanded);
            });

            // Close nav when clicking outside
            on(document, 'click', (e) => {
                if (!e.target.closest('.navbar') && navLinks.classList.contains('active')) {
                    navLinks.classList.remove('active');
                    navToggle.setAttribute('aria-expanded', 'false');
                }
            });
        }

        // Highlight current page in nav
        const currentPath = window.location.pathname;
        $$('.nav-links a').forEach(link => {
            if (link.getAttribute('href') === currentPath) {
                link.classList.add('active');
            }
        });
    }

    // ===== TOAST NOTIFICATIONS =====
    
    let toastContainer;

    function initToastContainer() {
        if (!toastContainer) {
            toastContainer = createElement('div', { 
                className: 'toast-container',
                id: 'toast-container'
            });
            document.body.appendChild(toastContainer);
            
            // Add toast styles if not present
            if (!$('#toast-styles')) {
                const styles = createElement('style', { id: 'toast-styles' }, [`
                    .toast-container {
                        position: fixed;
                        top: 20px;
                        right: 20px;
                        z-index: 9999;
                        display: flex;
                        flex-direction: column;
                        gap: 10px;
                    }
                    .toast {
                        padding: 16px 24px;
                        border-radius: 8px;
                        color: white;
                        font-weight: 500;
                        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                        transform: translateX(120%);
                        transition: transform 0.3s ease;
                        max-width: 350px;
                    }
                    .toast.show { transform: translateX(0); }
                    .toast-success { background-color: #10B981; }
                    .toast-error { background-color: #EF4444; }
                    .toast-warning { background-color: #F59E0B; }
                    .toast-info { background-color: #3B82F6; }
                `]);
                document.head.appendChild(styles);
            }
        }
    }

    function showToast(message, type = 'info', duration = config.toastDuration) {
        initToastContainer();
        
        const toast = createElement('div', {
            className: `toast toast-${type}`,
            role: 'alert'
        }, [message]);
        
        toastContainer.appendChild(toast);
        
        // Trigger animation
        requestAnimationFrame(() => {
            toast.classList.add('show');
        });
        
        // Auto remove
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, duration);
    }

    // ===== FORM UTILITIES =====
    
    function initFormValidation() {
        $$('form[data-validate]').forEach(form => {
            on(form, 'submit', (e) => {
                if (!form.checkValidity()) {
                    e.preventDefault();
                    e.stopPropagation();
                    
                    // Focus first invalid field
                    const firstInvalid = form.querySelector(':invalid');
                    if (firstInvalid) {
                        firstInvalid.focus();
                    }
                }
                form.classList.add('was-validated');
            });

            // Real-time validation
            $$('input, textarea, select', form).forEach(field => {
                on(field, 'blur', () => {
                    validateField(field);
                });
            });
        });
    }

    function validateField(field) {
        const isValid = field.checkValidity();
        field.classList.toggle('is-invalid', !isValid);
        field.classList.toggle('is-valid', isValid);
        
        // Update error message
        const errorElement = field.parentElement.querySelector('.form-error');
        if (errorElement) {
            errorElement.textContent = isValid ? '' : field.validationMessage;
        }
        
        return isValid;
    }

    // ===== LOADING STATES =====
    
    function showLoading(element, text = 'Loading...') {
        if (!element) return;
        
        element.dataset.originalContent = element.innerHTML;
        element.disabled = true;
        element.innerHTML = `<span class="spinner"></span> ${text}`;
        element.classList.add('loading');
    }

    function hideLoading(element) {
        if (!element || !element.dataset.originalContent) return;
        
        element.innerHTML = element.dataset.originalContent;
        element.disabled = false;
        element.classList.remove('loading');
        delete element.dataset.originalContent;
    }

    // ===== HTTP CLIENT =====
    
    async function fetchJson(url, options = {}) {
        const defaults = {
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        };
        
        const config = { ...defaults, ...options };
        
        if (config.body && typeof config.body === 'object') {
            config.body = JSON.stringify(config.body);
        }
        
        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('Fetch error:', error);
            throw error;
        }
    }

    // ===== INITIALIZATION =====
    
    function init() {
        initNavigation();
        initFormValidation();
        
        console.log('IGD Support initialized');
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // ===== PUBLIC API =====
    return {
        // Utilities
        debounce,
        throttle,
        formatDate,
        generateId,
        
        // DOM
        $,
        $$,
        on,
        createElement,
        
        // UI Components
        showToast,
        showLoading,
        hideLoading,
        
        // HTTP
        fetchJson,
        
        // Validation
        validateField,
        
        // Config
        config
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = App;
}
