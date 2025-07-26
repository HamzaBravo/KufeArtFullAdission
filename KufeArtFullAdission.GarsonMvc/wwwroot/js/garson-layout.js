// KufeArtFullAdission.GarsonMvc/wwwroot/js/garson-layout.js
class GarsonLayout {
    constructor() {
        this.init();
    }

    init() {
        this.bindEvents();
        this.updateActiveNav();
    }

    bindEvents() {
        // bindEvents() metodunda önceliği artırın
        document.addEventListener('click', (e) => {
            // Önce cart modal kontrolü yap
            if (e.target.id === 'closeCartBtn' || e.target.closest('#closeCartBtn')) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation(); // ✅ Bu satırı ekleyin
                console.log('❌ Close button clicked');
                this.closeCartModal();
                return false; // ✅ Bu satırı ekleyin
            }

            if (e.target.id === 'cartOverlay') {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation(); // ✅ Bu satırı ekleyin
                console.log('📱 Overlay clicked');
                this.closeCartModal();
                return false; // ✅ Bu satırı ekleyin
            }
        }, true); // ✅ true parametresi = capture phase'de dinle

        // Notification panel
        document.getElementById('notificationBtn')?.addEventListener('click', () => {
            this.toggleNotifications();
        });

        document.getElementById('closeNotifications')?.addEventListener('click', () => {
            this.closeNotifications();
        });

        // Profile panel
        document.getElementById('profileBtn')?.addEventListener('click', () => {
            this.toggleProfile();
        });

        document.getElementById('closeProfile')?.addEventListener('click', () => {
            this.closeProfile();
        });

        // Panel overlay
        document.getElementById('panelOverlay')?.addEventListener('click', () => {
            this.closeAllPanels();
        });

        // Navigation
        window.addEventListener('popstate', () => {
            this.updateActiveNav();
        });
    }

    toggleNotifications() {
        const panel = document.getElementById('notificationPanel');
        const overlay = document.getElementById('panelOverlay');

        // Diğer panelleri kapat
        this.closeProfile();

        if (panel.classList.contains('open')) {
            this.closeNotifications();
        } else {
            panel.classList.add('open');
            overlay.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    }

    closeNotifications() {
        const panel = document.getElementById('notificationPanel');
        const overlay = document.getElementById('panelOverlay');

        panel.classList.remove('open');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    toggleProfile() {
        const panel = document.getElementById('profilePanel');
        const overlay = document.getElementById('panelOverlay');

        // Diğer panelleri kapat
        this.closeNotifications();

        if (panel.classList.contains('open')) {
            this.closeProfile();
        } else {
            panel.classList.add('open');
            overlay.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    }

    closeProfile() {
        const panel = document.getElementById('profilePanel');
        const overlay = document.getElementById('panelOverlay');

        panel.classList.remove('open');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    closeAllPanels() {
        this.closeNotifications();
        this.closeProfile();
    }

    updateActiveNav() {
        const currentPath = window.location.pathname;
        const navItems = document.querySelectorAll('.nav-item');

        navItems.forEach(item => {
            item.classList.remove('active');
            if (item.getAttribute('href') === currentPath ||
                (currentPath === '/' && item.getAttribute('href') === '/')) {
                item.classList.add('active');
            }
        });
    }
}

// Global functions for profile actions
function searchCustomer() {
    // Modal veya yeni sayfa
    alert('Müşteri arama özelliği yakında...');
}

function changePassword() {
    // Şifre değiştirme modalı
    alert('Şifre değiştirme özelliği yakında...');
}

function viewProfile() {
    // Profil görüntüleme sayfası
    window.location.href = '/Profile';
}

function logout() {
    if (confirm('Çıkış yapmak istediğinizden emin misiniz?')) {
        window.location.href = '/Auth/Logout';
    }
}

// Sayfa yüklendiğinde başlat
document.addEventListener('DOMContentLoaded', () => {
    window.garsonLayout = new GarsonLayout();
});