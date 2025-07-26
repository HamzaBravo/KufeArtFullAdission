// KufeArtFullAdission.GarsonMvc/wwwroot/js/garson-signalr-client.js
class WaiterSignalRClient {
    constructor() {
        this.connection = null;
        this.notifications = [];
        this.isConnected = false;
        this.waiterName = null;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;

        this.init();
    }

    async init() {
        try {
            // Kendi hub'ına bağlan
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/waiterHub")  // Kendi hub'ı
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            this.bindSignalREvents();
            await this.connection.start();

            this.waiterName = this.getWaiterName();
            await this.connection.invoke("JoinWaiterGroup", this.waiterName);

            this.isConnected = true;
            console.log("✅ Garson Hub'ına bağlandı");

        } catch (error) {
            console.error("❌ SignalR bağlantı hatası:", error);
        }
    }

    // 🆕 Basit uyarı handler
    handleInactiveTableAlert(alertData) {
        // Bildirim objesi oluştur
        const notification = {
            id: Date.now(),
            type: 'InactiveTable',
            title: '⏰ Masa Takip',
            message: alertData.Message,
            icon: 'fas fa-clock',
            color: 'warning',
            timestamp: new Date(alertData.Timestamp),
            data: alertData,
            isRead: false,
            priority: 'normal'
        };

        // Bildirim listesine ekle
        this.addNotification(notification);

        // Uyarı sesi çal
        this.playWarningSound();

        // Badge güncelle
        this.updateNotificationBadge();

        console.log(`🔔 ${alertData.TableName} için uyarı eklendi`);
    }


    playWarningSound() {
        try {
            // Basit bir uyarı sesi
            const audio = new Audio('/sounds/table-warning.mp3');
            audio.volume = 0.5;
            audio.play().catch(e => console.log("Uyarı sesi çalınamadı:", e));
        } catch (error) {
            console.log("Ses çalma hatası:", error);
        }
    }

    bindSignalREvents() {

        // 🆕 İnaktif masa uyarısı - sadece ses + bildirim
        this.connection.on("InactiveTableAlert", (alertData) => {
            console.log("⏰ Masa takip uyarısı:", alertData);
            this.handleInactiveTableAlert(alertData);
        });

        // Admin bildirimi
        this.connection.on("AdminNotification", (data) => {
            console.log("📢 Admin bildirimi:", data);

            if (data.Type === "TableUpdate") {
                // Masa listesini yenile
                this.refreshPageData();
            }

            this.showToast(data.Message, 'info');
        });

        // Sipariş tamamlandı bildirimi
        this.connection.on("OrderCompleted", (orderData) => {
            console.log("🔔 Sipariş hazır bildirimi:", orderData);
            this.handleOrderCompleteNotification(orderData);
        });

        // Masa durumu değişikliği
        this.connection.on("TableStatusChanged", (tableData) => {
            console.log("🔄 Masa durumu değişti:", tableData);
            this.handleTableStatusChange(tableData);
        });

        // Admin'den genel bildirimler
        this.connection.on("AdminNotification", (notificationData) => {
            console.log("📢 Admin bildirimi:", notificationData);
            this.handleAdminNotification(notificationData);
        });

        // Bağlantı durumu
        this.connection.on("Connected", (connectionId) => {
            console.log("🔗 Garson SignalR bağlandı:", connectionId);
        });

        this.connection.on("JoinedWaiterGroup", (message) => {
            console.log("👥 Garson grubuna katıldı:", message);
        });

        // Bağlantı kopması
        this.connection.onclose((error) => {
            console.log("❌ SignalR bağlantısı koptu:", error);
            this.isConnected = false;
            this.updateConnectionStatus(false);
            this.handleConnectionError();
        });

        // Yeniden bağlanma
        this.connection.onreconnected((connectionId) => {
            console.log("✅ SignalR yeniden bağlandı:", connectionId);
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.updateConnectionStatus(true);
            this.connection.invoke("JoinWaiterGroup", this.waiterName);
        });

        // Bağlantı yeniden deneme
        this.connection.onreconnecting((error) => {
            console.log("🔄 SignalR yeniden bağlanıyor:", error);
            this.updateConnectionStatus(false);
        });
    }


    refreshPageData() {
        // Mevcut sayfa Dashboard ise masaları yenile
        if (window.location.pathname === '/' && window.GarsonDashboard) {
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        }
    }


    handleOrderCompleteNotification(orderData) {
        const notification = {
            id: Date.now(),
            type: 'OrderComplete',
            title: 'Sipariş Hazır! 🍽️',
            message: orderData.Message,
            icon: orderData.Icon || 'fas fa-check-circle',
            color: orderData.Color || 'success',
            timestamp: new Date(orderData.Timestamp),
            data: orderData,
            isRead: false,
            priority: 'high'
        };

        this.addNotification(notification);

        // Özel işlemler
        this.showMobileNotification(notification);
        this.playNotificationSound();
        this.showToast(notification.message, 'success');

        // Vibration (mobile)
        if ('vibrate' in navigator) {
            navigator.vibrate([200, 100, 200, 100, 200]);
        }

        // Update UI
        this.updateNotificationBadge();
    }

    handleTableStatusChange(tableData) {
        // Masa durumu değişikliği
        console.log("Masa durumu güncellendi:", tableData.TableName);

        // Dashboard sayfasındaysak masa listesini güncelle
        if (window.location.pathname === '/' && window.GarsonDashboard) {
            setTimeout(() => {
                window.location.reload(); // Simple refresh for now
            }, 1000);
        }
    }

    handleAdminNotification(notificationData) {
        const notification = {
            id: Date.now(),
            type: 'AdminMessage',
            title: 'Yönetici Mesajı 📢',
            message: notificationData.Message,
            icon: 'fas fa-megaphone',
            color: 'info',
            timestamp: new Date(),
            data: notificationData,
            isRead: false,
            priority: 'normal'
        };

        this.addNotification(notification);
        this.showToast(notification.message, 'info');
        this.updateNotificationBadge();
    }

    addNotification(notification) {
        this.notifications.unshift(notification);

        // En fazla 50 bildirim sakla
        if (this.notifications.length > 50) {
            this.notifications = this.notifications.slice(0, 50);
        }

        // Local storage'a kaydet
        this.saveNotificationsToStorage();
    }

    showMobileNotification(notification) {
        // Browser notification
        if ("Notification" in window && Notification.permission === "granted") {
            const browserNotification = new Notification(notification.title, {
                body: notification.message,
                icon: '/images/garson-icon.png',
                badge: '/images/notification-badge.png',
                tag: notification.type,
                renotify: true,
                requireInteraction: notification.priority === 'high'
            });

            // Tıklandığında ilgili sayfaya git
            browserNotification.onclick = () => {
                window.focus();
                if (notification.data.TableId) {
                    this.navigateToTable(notification.data.TableId, notification.data.TableName);
                }
                browserNotification.close();
            };
        }
    }

    async sendOrderToAdmin(orderData) {
        try {
            if (!this.isConnected) {
                throw new Error('SignalR bağlantısı yok');
            }

            // Admin paneline HTTP request gönder
            const adminPanelUrl = this.getAdminPanelUrl();
            const response = await fetch(`${adminPanelUrl}/api/notification/new-order`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(orderData)
            });

            if (response.ok) {
                console.log("✅ Sipariş bildirimi admin paneline gönderildi");
                return true;
            } else {
                console.error("❌ Admin panel bildirimi başarısız:", response.status);
                return false;
            }
        } catch (error) {
            console.error("❌ SignalR sipariş bildirimi hatası:", error);
            return false;
        }
    }

    // Notification Management
    updateNotificationBadge() {
        const unreadCount = this.notifications.filter(n => !n.isRead).length;

        // Layout'taki notification badge'i güncelle
        const badge = document.getElementById('notificationCount');
        if (badge) {
            if (unreadCount > 0) {
                badge.style.display = 'flex';
                badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
            } else {
                badge.style.display = 'none';
            }
        }
    }

    renderNotificationPanel() {
        const panel = document.getElementById('notificationList');
        if (!panel) return;

        if (this.notifications.length === 0) {
            panel.innerHTML = `
                <div class="no-notifications">
                    <i class="fas fa-bell-slash"></i>
                    <p>Henüz bildirim yok</p>
                </div>
            `;
            return;
        }

        let html = '';
        this.notifications.slice(0, 20).forEach(notification => {
            html += this.renderNotificationItem(notification);
        });

        panel.innerHTML = html;
        this.bindNotificationPanelEvents();
    }

    renderNotificationItem(notification) {
        const timeAgo = this.getTimeAgo(notification.timestamp);
        const priorityClass = notification.priority === 'high' ? 'high-priority' : '';

        return `
            <div class="notification-item ${!notification.isRead ? 'unread' : ''} ${priorityClass}" 
                 data-notification-id="${notification.id}">
                <div class="notification-icon ${notification.color}">
                    <i class="${notification.icon}"></i>
                </div>
                <div class="notification-content">
                    <div class="notification-title">${notification.title}</div>
                    <div class="notification-message">${notification.message}</div>
                    <div class="notification-time">${timeAgo}</div>
                </div>
                <div class="notification-actions">
                    ${!notification.isRead ? '<button class="mark-read-btn"><i class="fas fa-check"></i></button>' : ''}
                </div>
            </div>
        `;
    }

    bindNotificationPanelEvents() {
        // Mark as read
        document.querySelectorAll('.mark-read-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const notificationId = parseInt(btn.closest('.notification-item').dataset.notificationId);
                this.markAsRead(notificationId);
            });
        });

        // Click notification
        document.querySelectorAll('.notification-item').forEach(item => {
            item.addEventListener('click', () => {
                const notificationId = parseInt(item.dataset.notificationId);
                const notification = this.notifications.find(n => n.id === notificationId);
                if (notification) {
                    this.handleNotificationClick(notification);
                    this.markAsRead(notificationId);
                }
            });
        });
    }

    handleNotificationClick(notification) {
        if (notification.type === 'OrderComplete' && notification.data.TableId) {
            this.navigateToTable(notification.data.TableId, notification.data.TableName);
        }
    }

    navigateToTable(tableId, tableName) {
        // Masa sipariş sayfasına git
        window.location.href = `/Order/Index?tableId=${tableId}&tableName=${encodeURIComponent(tableName)}&isOccupied=true`;
    }

    markAsRead(notificationId) {
        const notification = this.notifications.find(n => n.id === notificationId);
        if (notification) {
            notification.isRead = true;
            this.updateNotificationBadge();
            this.renderNotificationPanel();
            this.saveNotificationsToStorage();
        }
    }

    markAllAsRead() {
        this.notifications.forEach(n => n.isRead = true);
        this.updateNotificationBadge();
        this.renderNotificationPanel();
        this.saveNotificationsToStorage();
    }

    // Connection Management
    handleConnectionError() {
        this.reconnectAttempts++;

        if (this.reconnectAttempts <= this.maxReconnectAttempts) {
            console.log(`🔄 SignalR yeniden bağlanma denemesi: ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
            setTimeout(() => {
                this.init();
            }, 5000 * this.reconnectAttempts); // Exponential backoff
        } else {
            console.error("❌ SignalR maksimum bağlantı denemesi aşıldı");
            this.showToast("Canlı bildirimler devre dışı. Sayfa yenileyin.", "warning");
        }
    }

    updateConnectionStatus(isConnected) {
        // Layout'taki connection indicator'ı güncelle
        const indicator = document.querySelector('.connection-status');
        if (indicator) {
            indicator.className = `connection-status ${isConnected ? 'connected' : 'disconnected'}`;
            indicator.title = isConnected ? 'Canlı bağlantı aktif' : 'Bağlantı kopuk';
        }

        // Nav bar'da online/offline durumu
        document.body.classList.toggle('signalr-offline', !isConnected);
    }

    // Utility Methods
    getAdminPanelUrl() {
        // Development ve production URL'leri
        return window.location.hostname === 'localhost'
            ? 'https://localhost:7164'  // Development
            : 'https://adisyon.kufeart.com'; // Production
    }

    getWaiterName() {
        // Layout'tan garson adını al
        const nameElement = document.querySelector('.waiter-name');
        return nameElement ? nameElement.textContent.trim() : 'Garson';
    }

    playNotificationSound() {
        try {
            const audio = new Audio('/sounds/notification-waiter.mp3');
            audio.volume = 0.4;
            audio.play().catch(e => console.log("Ses çalınamadı:", e));
        } catch (error) {
            console.log("Notification sound error:", error);
        }
    }

    showToast(message, type = 'info') {
        // Mevcut toast sistemini kullan
        if (window.showToast) {
            window.showToast(message, type);
        } else {
            // Fallback toast
            console.log(`Toast [${type}]: ${message}`);
        }
    }

    getTimeAgo(date) {
        const seconds = Math.floor((new Date() - date) / 1000);

        if (seconds < 60) return "Az önce";
        if (seconds < 3600) return Math.floor(seconds / 60) + " dk önce";
        if (seconds < 86400) return Math.floor(seconds / 3600) + " saat önce";
        return Math.floor(seconds / 86400) + " gün önce";
    }

    // Storage Management
    saveNotificationsToStorage() {
        try {
            localStorage.setItem('waiter_notifications', JSON.stringify(this.notifications.slice(0, 20)));
        } catch (error) {
            console.error("Notification storage error:", error);
        }
    }

    loadNotificationsFromStorage() {
        try {
            const stored = localStorage.getItem('waiter_notifications');
            if (stored) {
                this.notifications = JSON.parse(stored).map(n => ({
                    ...n,
                    timestamp: new Date(n.timestamp)
                }));
                this.updateNotificationBadge();
            }
        } catch (error) {
            console.error("Notification load error:", error);
        }
    }

    // Public API
    getNotifications() {
        return this.notifications;
    }

    getUnreadCount() {
        return this.notifications.filter(n => !n.isRead).length;
    }

    isSignalRConnected() {
        return this.isConnected;
    }

    async testConnection() {
        if (this.connection && this.isConnected) {
            try {
                await this.connection.invoke("JoinWaiterGroup", this.waiterName);
                return true;
            } catch (error) {
                console.error("Connection test failed:", error);
                return false;
            }
        }
        return false;
    }
}

// Global waiter SignalR client
let waiterSignalR = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Notification permission iste
    if ("Notification" in window && Notification.permission === "default") {
        Notification.requestPermission();
    }

    // SignalR client'ı başlat
    waiterSignalR = new WaiterSignalRClient();
    window.waiterSignalR = waiterSignalR; // Global access

    // Storage'dan eski bildirimleri yükle
    waiterSignalR.loadNotificationsFromStorage();

    // Layout events
    bindNotificationPanelEvents();
});

function bindNotificationPanelEvents() {
    // Notification panel toggle
    const notificationBtn = document.getElementById('notificationBtn');
    if (notificationBtn) {
        notificationBtn.addEventListener('click', () => {
            waiterSignalR.renderNotificationPanel();
            // Panel açma/kapama layout.js'de hallediliyor
        });
    }
}