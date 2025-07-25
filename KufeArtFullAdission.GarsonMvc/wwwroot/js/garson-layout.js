// KufeArtFullAdission.GarsonMvc/wwwroot/js/garson-layout.js
class GarsonLayout {
    constructor() {
        this.init();
    }

    init() {
        this.bindEvents();
        this.updateActiveNav();
        this.checkOrientation();
    }

    bindEvents() {
        // Notification panel
        document.getElementById('notificationBtn').addEventListener('click', () => {
            this.toggleNotifications();
        });

        document.getElementById('closeNotifications').addEventListener('click', () => {
            this.closeNotifications();
        });

        // Profile button
        document.getElementById('profileBtn').addEventListener('click', () => {
            this.showProfileMenu();
        });

        // Back button handling
        window.addEventListener('popstate', () => {
            this.updateActiveNav();
        });

        // Orientation change
        window.addEventListener('orientationchange', () => {
            setTimeout(() => this.checkOrientation(), 100);
        });
    }

    toggleNotifications() {
        const panel = document.getElementById('notificationPanel');
        panel.classList.toggle('open');
    }

    closeNotifications() {
        const panel = document.getElementById('notificationPanel');
        panel.classList.remove('open');
    }

    showProfileMenu() {
        // Simple profile menu
        const options = [
            { text: 'Profil', action: () => window.location.href = '/Profile' },
            { text: 'Ayarlar', action: () => window.location.href = '/Settings' },
            { text: 'Çıkış Yap', action: () => this.logout() }
        ];

        this.showActionSheet(options);
    }

    showActionSheet(options) {
        const overlay = document.createElement('div');
        overlay.className = 'action-sheet-overlay';

        const sheet = document.createElement('div');
        sheet.className = 'action-sheet';

        options.forEach(option => {
            const button = document.createElement('button');
            button.className = 'action-button';
            button.textContent = option.text;
            button.onclick = () => {
                overlay.remove();
                option.action();
            };
            sheet.appendChild(button);
        });

        // Cancel button
        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'action-button cancel';
        cancelBtn.textContent = 'İptal';
        cancelBtn.onclick = () => overlay.remove();
        sheet.appendChild(cancelBtn);

        overlay.appendChild(sheet);
        document.body.appendChild(overlay);

        // Animate in
        setTimeout(() => overlay.classList.add('show'), 10);
    }

    updateActiveNav() {
        const currentPath = window.location.pathname;
        const navItems = document.querySelectorAll('.bottom-nav .nav-item');

        navItems.forEach(item => {
            item.classList.remove('active');
            if (item.getAttribute('href') === currentPath ||
                (currentPath === '/' && item.getAttribute('href') === '/')) {
                item.classList.add('active');
            }
        });
    }

    checkOrientation() {
        const isLandscape = window.innerHeight < window.innerWidth;
        document.body.classList.toggle('landscape', isLandscape);
    }

    showLoading() {
        document.getElementById('loadingOverlay').style.display = 'flex';
    }

    hideLoading() {
        document.getElementById('loadingOverlay').style.display = 'none';
    }

    async logout() {
        if (confirm('Çıkış yapmak istediğinizden emin misiniz?')) {
            this.showLoading();
            try {
                await fetch('/Auth/Logout', { method: 'POST' });
                window.location.href = '/Auth/Login';
            } catch (error) {
                this.hideLoading();
                alert('Çıkış işlemi başarısız!');
            }
        }
    }
}

// Initialize layout
document.addEventListener('DOMContentLoaded', () => {
    window.garsonLayout = new GarsonLayout();
});