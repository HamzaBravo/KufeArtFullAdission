
class GarsonLayout {
    constructor() {
        this.init();
    }

    init() {
        this.bindEvents();
        this.updateActiveNav();
    }

    bindEvents() {
        document.getElementById('notificationBtn')?.addEventListener('click', () => {
            this.toggleNotifications();
        });

        document.getElementById('closeNotifications')?.addEventListener('click', () => {
            this.closeNotifications();
        });

        document.getElementById('panelOverlay')?.addEventListener('click', () => {
            this.closeNotifications();
        });

        document.getElementById('profileBtn')?.addEventListener('click', () => {
            this.toggleProfile();
        });

        document.getElementById('closeProfile')?.addEventListener('click', () => {
            this.closeProfile();
        });

        document.addEventListener('click', (e) => {
            if (e.target.id === 'closeCartBtn' || e.target.closest('#closeCartBtn')) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                this.closeCartModal();
                return false;
            }

            if (e.target.id === 'cartOverlay') {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                this.closeCartModal();
                return false;
            }
        }, true);

        window.addEventListener('popstate', () => {
            this.updateActiveNav();
        });
    }

    toggleNotifications() {
        const panel = document.getElementById('notificationPanel');
        const overlay = document.getElementById('panelOverlay');


        if (panel && overlay) {
            const isOpen = panel.classList.contains('open');

            if (isOpen) {
                this.closeNotifications();
            } else {
                panel.classList.add('open');
                overlay.classList.add('active');
                document.body.style.overflow = 'hidden';

                if (window.waiterSignalR) {
                    window.waiterSignalR.renderNotificationPanel();
                }
            }
        } else {
        }
    }

    closeNotifications() {
        const panel = document.getElementById('notificationPanel');
        const overlay = document.getElementById('panelOverlay');

        if (panel && overlay) {
            panel.classList.remove('open');
            overlay.classList.remove('active');
            document.body.style.overflow = '';
        }
    }

    toggleProfile() {
        const panel = document.getElementById('profilePanel');
        const overlay = document.getElementById('panelOverlay');

        this.closeNotifications();

        if (panel && panel.classList.contains('open')) {
            this.closeProfile();
        } else if (panel) {
            panel.classList.add('open');
            overlay.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    }

    closeProfile() {
        const panel = document.getElementById('profilePanel');
        const overlay = document.getElementById('panelOverlay');

        if (panel && overlay) {
            panel.classList.remove('open');
            overlay.classList.remove('active');
            document.body.style.overflow = '';
        }
    }

    closeCartModal() {
       
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

function searchCustomer() {
    alert('Müşteri arama özelliği yakında...');
}

function changePassword() {
    alert('Şifre değiştirme özelliği yakında...');
}

function viewProfile() {
    window.location.href = '/Profile';
}

function logout() {
    if (confirm('Çıkış yapmak istediğinizden emin misiniz?')) {
        window.location.href = '/Auth/Logout';
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new GarsonLayout();
});