﻿class TabletUtils {
    static formatCurrency(amount) {
        if (typeof amount !== 'number') {
            amount = parseFloat(amount) || 0;
        }
        return new Intl.NumberFormat('tr-TR', {
            style: 'currency',
            currency: 'TRY',
            minimumFractionDigits: 2
        }).format(amount);
    }

    static formatTime(date) {
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        return date.toLocaleTimeString('tr-TR', {
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    static formatDateTime(date) {
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        return date.toLocaleString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    static getTimeElapsed(startTime) {
        const now = new Date();
        const start = new Date(startTime);
        const diffMs = now - start;
        const diffMins = Math.floor(diffMs / (1000 * 60));

        if (diffMins < 1) return 'Az önce';
        if (diffMins < 60) return `${diffMins} dk önce`;

        const hours = Math.floor(diffMins / 60);
        const remainingMins = diffMins % 60;
        return `${hours}s ${remainingMins}dk önce`;
    }

    static showToast(message, type = 'info', duration = 4000) {
        let toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toastContainer';
            toastContainer.className = 'toast-container';
            document.body.appendChild(toastContainer);
        }

        const toastId = 'toast_' + Date.now();
        const toast = document.createElement('div');
        toast.id = toastId;
        toast.className = `toast-notification toast-${type}`;

        const icons = {
            'success': 'fas fa-check-circle',
            'error': 'fas fa-exclamation-circle',
            'warning': 'fas fa-exclamation-triangle',
            'info': 'fas fa-info-circle'
        };

        toast.innerHTML = `
            <div class="toast-content">
                <i class="${icons[type] || icons.info}"></i>
                <span class="toast-message">${message}</span>
                <button class="toast-close" onclick="TabletUtils.closeToast('${toastId}')">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        `;

        toastContainer.appendChild(toast);

        requestAnimationFrame(() => {
            toast.classList.add('toast-show');
        });

        setTimeout(() => {
            TabletUtils.closeToast(toastId);
        }, duration);

        return toastId;
    }

    static closeToast(toastId) {
        const toast = document.getElementById(toastId);
        if (toast) {
            toast.classList.remove('toast-show');
            toast.classList.add('toast-hide');

            setTimeout(() => {
                toast.remove();
            }, 300);
        }
    }

    static playNotificationSound() {
        try {

            const audio = document.getElementById('notificationSound');
            if (audio) {
                audio.volume = 0.9; 
                audio.currentTime = 0; 

                const playPromise = audio.play();

                if (playPromise !== undefined) {
                    playPromise.catch(error => {
                    });
                }
            } else {
            }
        } catch (error) {
        }
    }

    static vibrate(pattern = [200, 100, 200]) {
        if ('vibrate' in navigator) {
            try {
                navigator.vibrate(pattern);
            } catch (error) {
            }
        }
    }

    static createStatusBadge(status) {
        const statusConfig = {
            'New': { class: 'new', icon: 'clock', text: 'Yeni' },
            'Preparing': { class: 'preparing', icon: 'fire', text: 'Hazırlanıyor' },
            'Ready': { class: 'ready', icon: 'check-circle', text: 'Hazır' }
        };

        const config = statusConfig[status] || statusConfig['New'];

        return `
            <span class="status-badge ${config.class}">
                <i class="fas fa-${config.icon}"></i>
                ${config.text}
            </span>
        `;
    }

    static getDeviceInfo() {
        return {
            isMobile: /Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent),
            isTablet: /iPad|Android/i.test(navigator.userAgent) && !/Mobile/i.test(navigator.userAgent),
            isIOS: /iPad|iPhone|iPod/.test(navigator.userAgent),
            isAndroid: /Android/.test(navigator.userAgent),
            screenWidth: window.screen.width,
            screenHeight: window.screen.height,
            orientation: window.screen.orientation?.type || 'unknown'
        };
    }

    static checkNetworkStatus() {
        if ('onLine' in navigator) {
            return navigator.onLine;
        }
        return true; 
    }

    static onNetworkChange(callback) {
        window.addEventListener('online', () => callback(true));
        window.addEventListener('offline', () => callback(false));
    }

    static setStorageItem(key, value) {
        try {
            const data = {
                value: value,
                timestamp: Date.now(),
                expires: Date.now() + (24 * 60 * 60 * 1000) 
            };
            localStorage.setItem(`tablet_${key}`, JSON.stringify(data));
            return true;
        } catch (error) {
            console.error('Storage error:', error);
            return false;
        }
    }

    static getStorageItem(key) {
        try {
            const item = localStorage.getItem(`tablet_${key}`);
            if (!item) return null;

            const data = JSON.parse(item);

            if (data.expires && Date.now() > data.expires) {
                localStorage.removeItem(`tablet_${key}`);
                return null;
            }

            return data.value;
        } catch (error) {
            console.error('Storage read error:', error);
            return null;
        }
    }

    static removeStorageItem(key) {
        try {
            localStorage.removeItem(`tablet_${key}`);
            return true;
        } catch (error) {
            console.error('Storage remove error:', error);
            return false;
        }
    }

    static debounce(func, wait, immediate = false) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                timeout = null;
                if (!immediate) func(...args);
            };
            const callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func(...args);
        };
    }

    static measurePerformance(name, fn) {
        const start = performance.now();
        const result = fn();
        const end = performance.now();
        return result;
    }

    static setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        TabletUtils.setStorageItem('theme', theme);
    }

    static getTheme() {
        return TabletUtils.getStorageItem('theme') || 'light';
    }

    static $(selector) {
        return document.querySelector(selector);
    }

    static $$(selector) {
        return document.querySelectorAll(selector);
    }

    static createElement(tag, className, innerHTML) {
        const element = document.createElement(tag);
        if (className) element.className = className;
        if (innerHTML) element.innerHTML = innerHTML;
        return element;
    }

    static async requestFullscreen() {
        try {
            const element = document.documentElement;
            if (element.requestFullscreen) {
                await element.requestFullscreen();
            } else if (element.webkitRequestFullscreen) {
                await element.webkitRequestFullscreen();
            } else if (element.msRequestFullscreen) {
                await element.msRequestFullscreen();
            }
            return true;
        } catch (error) {
            console.error('Fullscreen error:', error);
            return false;
        }
    }

    static async exitFullscreen() {
        try {
            if (document.exitFullscreen) {
                await document.exitFullscreen();
            } else if (document.webkitExitFullscreen) {
                await document.webkitExitFullscreen();
            } else if (document.msExitFullscreen) {
                await document.msExitFullscreen();
            }
            return true;
        } catch (error) {
            console.error('Exit fullscreen error:', error);
            return false;
        }
    }

    static sanitizeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    static escapeHtml(str) {
        const div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    static serializeForm(form) {
        const formData = new FormData(form);
        const data = {};
        for (let [key, value] of formData.entries()) {
            if (data[key]) {
                if (!Array.isArray(data[key])) {
                    data[key] = [data[key]];
                }
                data[key].push(value);
            } else {
                data[key] = value;
            }
        }
        return data;
    }

    static initializeTablet() {

        TabletUtils.onNetworkChange((isOnline) => {
            const statusElement = document.getElementById('connectionStatus');
            if (statusElement) {
                statusElement.className = `connection-status ${isOnline ? '' : 'disconnected'}`;
                statusElement.title = isOnline ? 'Bağlantı aktif' : 'Bağlantı kopuk';
            }

            if (!isOnline) {
                TabletUtils.showToast('İnternet bağlantısı kesildi!', 'warning');
            } else {
                TabletUtils.showToast('İnternet bağlantısı yeniden kuruldu', 'success', 2000);
            }
        });

        TabletUtils.updateClock();
        setInterval(TabletUtils.updateClock, 1000);

        const savedTheme = TabletUtils.getTheme();
        TabletUtils.setTheme(savedTheme);

    }

    static updateClock() {
        const clockElement = document.getElementById('currentTime');
        if (clockElement) {
            const now = new Date();
            clockElement.textContent = TabletUtils.formatTime(now);
        }
    }
}

const toastStyles = `
<style>
.toast-container {
    position: fixed;
    top: 80px;
    right: 20px;
    z-index: 9999;
    max-width: 400px;
}

.toast-notification {
    background: white;
    border-radius: 8px;
    box-shadow: 0 8px 32px rgba(0,0,0,0.15);
    margin-bottom: 10px;
    transform: translateX(100%);
    transition: all 0.3s cubic-bezier(0.4, 0.0, 0.2, 1);
    border-left: 4px solid #007bff;
    overflow: hidden;
}

.toast-notification.toast-show {
    transform: translateX(0);
}

.toast-notification.toast-hide {
    transform: translateX(100%);
    opacity: 0;
}

.toast-notification.toast-success { border-left-color: #28a745; }
.toast-notification.toast-error { border-left-color: #dc3545; }
.toast-notification.toast-warning { border-left-color: #ffc107; }
.toast-notification.toast-info { border-left-color: #17a2b8; }

.toast-content {
    display: flex;
    align-items: center;
    padding: 16px 20px;
    gap: 12px;
}

.toast-content i:first-child {
    font-size: 18px;
    flex-shrink: 0;
}

.toast-success .toast-content i:first-child { color: #28a745; }
.toast-error .toast-content i:first-child { color: #dc3545; }
.toast-warning .toast-content i:first-child { color: #ffc107; }
.toast-info .toast-content i:first-child { color: #17a2b8; }

.toast-message {
    flex: 1;
    font-size: 14px;
    font-weight: 500;
    color: #333;
    line-height: 1.4;
}

.toast-close {
    background: none;
    border: none;
    color: #666;
    cursor: pointer;
    padding: 4px;
    border-radius: 4px;
    transition: all 0.2s;
    flex-shrink: 0;
}

.toast-close:hover {
    background: #f8f9fa;
    color: #333;
}

/* Mobile responsive */
@media (max-width: 480px) {
    .toast-container {
        left: 20px;
        right: 20px;
        top: 70px;
        max-width: none;
    }
    
    .toast-content {
        padding: 12px 16px;
    }
    
    .toast-message {
        font-size: 13px;
    }
}
</style>
`;

window.TabletUtils = TabletUtils;

window.logout = function () {
    if (confirm('Çıkış yapmak istediğinizden emin misiniz?')) {
        window.location.href = '/Home/Logout';
    }
};

document.addEventListener('DOMContentLoaded', function () {

    document.head.insertAdjacentHTML('beforeend', toastStyles);

    TabletUtils.initializeTablet();
    TabletSignalR.init();

    if (document.getElementById('ordersContainer')) {
        TabletDashboard.init();

        requestAudioPermission();
    }
});

function requestAudioPermission() {
    const audioPermissionDiv = document.createElement('div');
    audioPermissionDiv.innerHTML = `
        <div style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; 
                    background: rgba(0,0,0,0.8); z-index: 9999; display: flex; 
                    align-items: center; justify-content: center;">
            <div style="background: white; padding: 30px; border-radius: 10px; text-align: center;">
                <h3>🔊 Ses Bildirimleri</h3>
                <p>Yeni sipariş bildirimlerini duyabilmek için tıklayın</p>
                <button id="enableAudioBtn" style="padding: 15px 30px; font-size: 16px; 
                                                  background: #2c5530; color: white; border: none; 
                                                  border-radius: 5px; cursor: pointer;">
                    Ses Bildirimlerini Etkinleştir
                </button>
            </div>
        </div>
    `;

    document.body.appendChild(audioPermissionDiv);

    document.getElementById('enableAudioBtn').addEventListener('click', () => {
        const testAudio = new Audio('/sounds/notification.mp3');
        testAudio.volume = 0.3;
        testAudio.play().then(() => {
            audioPermissionDiv.remove();
        }).catch(e => {
            audioPermissionDiv.remove();
        });
    });
}