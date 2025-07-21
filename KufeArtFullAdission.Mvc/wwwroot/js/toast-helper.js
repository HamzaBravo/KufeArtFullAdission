// Global Toast Helper
window.ToastHelper = {
    show: function (message, type = 'info', duration = 5000) {
        const toastContainer = document.querySelector('.toast-container');
        const template = document.getElementById('toastTemplate');

        // Toast kopyası oluştur
        const toast = template.cloneNode(true);
        toast.id = 'toast-' + Date.now();
        toast.style.display = 'block';
        toast.classList.add(type);

        // İkon ve mesaj ayarla
        const icon = toast.querySelector('.toast-icon');
        const messageEl = toast.querySelector('.toast-message');

        // İkon türüne göre ayarla
        const icons = {
            success: 'fas fa-check-circle',
            error: 'fas fa-exclamation-circle',
            warning: 'fas fa-exclamation-triangle',
            info: 'fas fa-info-circle'
        };

        icon.className = `toast-icon me-2 ${icons[type] || icons.info}`;
        messageEl.textContent = message;

        // Container'a ekle
        toastContainer.appendChild(toast);

        // Bootstrap toast başlat
        const bsToast = new bootstrap.Toast(toast, {
            autohide: true,
            delay: duration
        });

        bsToast.show();

        // Toast kapandığında DOM'dan kaldır
        toast.addEventListener('hidden.bs.toast', function () {
            toast.remove();
        });
    },

    // Kısa metodlar
    success: function (message, duration = 3000) {
        this.show(message, 'success', duration);
    },

    error: function (message, duration = 5000) {
        this.show(message, 'error', duration);
    },

    warning: function (message, duration = 4000) {
        this.show(message, 'warning', duration);
    },

    info: function (message, duration = 3000) {
        this.show(message, 'info', duration);
    }
};