// KufeArt.TabletMvc/wwwroot/js/tablet-common.js

// Global utility functions
window.TabletUtils = {
    // Format tarih/saat
    formatTime: function (dateString) {
        const date = new Date(dateString);
        return date.toLocaleTimeString('tr-TR', {
            hour: '2-digit',
            minute: '2-digit'
        });
    },

    // Geçen süre hesaplama
    getElapsedTime: function (dateString) {
        const now = new Date();
        const orderTime = new Date(dateString);
        const diffMinutes = Math.floor((now - orderTime) / (1000 * 60));

        if (diffMinutes < 1) return 'Şimdi';
        if (diffMinutes < 60) return `${diffMinutes} dk önce`;

        const hours = Math.floor(diffMinutes / 60);
        const minutes = diffMinutes % 60;
        return `${hours}s ${minutes}dk önce`;
    },

    // Para formatı
    formatCurrency: function (amount) {
        return new Intl.NumberFormat('tr-TR', {
            style: 'currency',
            currency: 'TRY'
        }).format(amount);
    },

    // Toast notification göster
    showToast: function (message, type = 'info', duration = 5000) {
        const toastContainer = document.querySelector('.toast-container');
        if (!toastContainer) return;

        const toastId = 'toast_' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast show" role="alert">
                <div class="toast-header">
                    <i class="fas ${this.getToastIcon(type)} text-${type} me-2"></i>
                    <strong class="me-auto">${this.getToastTitle(type)}</strong>
                    <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);

        // Auto remove
        setTimeout(() => {
            const toast = document.getElementById(toastId);
            if (toast) {
                toast.remove();
            }
        }, duration);
    },

    getToastIcon: function (type) {
        const icons = {
            'success': 'fa-check-circle',
            'error': 'fa-exclamation-circle',
            'warning': 'fa-exclamation-triangle',
            'info': 'fa-info-circle'
        };
        return icons[type] || 'fa-bell';
    },

    getToastTitle: function (type) {
        const titles = {
            'success': 'Başarılı',
            'error': 'Hata',
            'warning': 'Uyarı',
            'info': 'Bilgi'
        };
        return titles[type] || 'Bildirim';
    },

    // Ses çal
    playNotificationSound: function () {
        const audio = document.getElementById('notificationSound');
        if (audio) {
            audio.currentTime = 0;
            audio.play().catch(e => {
                console.log('Ses çalınamadı:', e);
            });
        }
    },

    // Vibration (mobil cihazlarda)
    vibrate: function (pattern = [200, 100, 200]) {
        if ('vibrate' in navigator) {
            navigator.vibrate(pattern);
        }
    },

    // Local storage helpers
    setStorage: function (key, value) {
        try {
            localStorage.setItem(key, JSON.stringify(value));
        } catch (e) {
            console.error('Storage error:', e);
        }
    },

    getStorage: function (key, defaultValue = null) {
        try {
            const value = localStorage.getItem(key);
            return value ? JSON.parse(value) : defaultValue;
        } catch (e) {
            console.error('Storage error:', e);
            return defaultValue;
        }
    },

    // AJAX helper
    ajax: function (url, options = {}) {
        const defaultOptions = {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        };

        const finalOptions = { ...defaultOptions, ...options };

        if (finalOptions.body && typeof finalOptions.body === 'object') {
            finalOptions.body = JSON.stringify(finalOptions.body);
        }

        return fetch(url, finalOptions)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }
                return response.json();
            })
            .catch(error => {
                console.error('AJAX Error:', error);
                throw error;
            });
    }
};

// Logout function
window.logout = function () {
    if (confirm('Tablet oturumunu sonlandırmak istediğinizden emin misiniz?')) {
        window.location.href = '/Home/Logout';
    }
};

// Sayfa yüklendiğinde çalışacak genel ayarlar
document.addEventListener('DOMContentLoaded', function () {
    // Gerçek zamanlı saat gösterimi
    function updateTime() {
        const timeElement = document.getElementById('currentTime');
        if (timeElement) {
            const now = new Date();
            timeElement.textContent = now.toLocaleTimeString('tr-TR', {
                hour: '2-digit',
                minute: '2-digit'
            });
        }
    }

    // Saati başlat ve her dakika güncelle
    updateTime();
    setInterval(updateTime, 60000);

    // Touch events için optimizasyon
    document.addEventListener('touchstart', function () { }, { passive: true });

    // Scroll restoration
    if ('scrollRestoration' in history) {
        history.scrollRestoration = 'manual';
    }

    // Connection status checker
    function checkConnection() {
        const statusElement = document.getElementById('connectionStatus');
        if (statusElement) {
            if (navigator.onLine) {
                statusElement.classList.remove('disconnected');
                statusElement.title = 'Bağlantı aktif';
            } else {
                statusElement.classList.add('disconnected');
                statusElement.title = 'Bağlantı kopuk';
            }
        }
    }

    window.addEventListener('online', checkConnection);
    window.addEventListener('offline', checkConnection);
    checkConnection();

    // Prevent zoom on double tap (iOS Safari)
    let lastTouchEnd = 0;
    document.addEventListener('touchend', function (event) {
        const now = (new Date()).getTime();
        if (now - lastTouchEnd <= 300) {
            event.preventDefault();
        }
        lastTouchEnd = now;
    }, false);
});

// Performance monitoring
window.addEventListener('load', function () {
    // Page load performance
    if ('performance' in window) {
        const loadTime = performance.timing.loadEventEnd - performance.timing.navigationStart;
        console.log(`Sayfa yükleme süresi: ${loadTime}ms`);
    }
});

// Error handling
window.addEventListener('error', function (event) {
    console.error('Global error:', event.error);
    TabletUtils.showToast('Beklenmeyen bir hata oluştu', 'error');
});

// Unhandled promise rejections
window.addEventListener('unhandledrejection', function (event) {
    console.error('Unhandled promise rejection:', event.reason);
    event.preventDefault();
});