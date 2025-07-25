// KufeArtFullAdission.Mvc/wwwroot/js/signalr-client.js
class AdminSignalRClient {
    constructor() {
        this.connection = null;
        this.notifications = [];
        this.isConnected = false;
        this.init();
    }

    async init() {
        try {
            // SignalR connection oluştur
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/orderHub")
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            // Event listeners
            this.bindSignalREvents();

            // Bağlantıyı başlat
            await this.connection.start();
            console.log("✅ SignalR Admin paneline bağlandı");

            // Admin grubuna katıl
            await this.connection.invoke("JoinAdminGroup");
            this.isConnected = true;
            this.updateConnectionStatus(true);

        } catch (error) {
            console.error("❌ SignalR bağlantı hatası:", error);
            this.updateConnectionStatus(false);
        }
    }

    bindSignalREvents() {
        // Yeni sipariş bildirimi
        this.connection.on("NewOrderReceived", (orderData) => {
            console.log("🔔 Yeni sipariş bildirimi:", orderData);
            this.handleNewOrderNotification(orderData);
        });

        // Masa durumu değişikliği
        this.connection.on("TableStatusChanged", (tableData) => {
            console.log("🔄 Masa durumu değişti:", tableData);
            this.handleTableStatusChange(tableData);
        });

        // Bağlantı durumu
        this.connection.on("Connected", (connectionId) => {
            console.log("🔗 SignalR bağlandı:", connectionId);
        });

        this.connection.on("JoinedAdminGroup", (message) => {
            console.log("👥 Admin grubuna katıldı:", message);
        });

        // Bağlantı kopması
        this.connection.onclose((error) => {
            console.log("❌ SignalR bağlantısı koptu:", error);
            this.isConnected = false;
            this.updateConnectionStatus(false);
        });

        // Yeniden bağlanma
        this.connection.onreconnected((connectionId) => {
            console.log("✅ SignalR yeniden bağlandı:", connectionId);
            this.isConnected = true;
            this.updateConnectionStatus(true);
            this.connection.invoke("JoinAdminGroup");
        });
    }

    handleNewOrderNotification(orderData) {
        // Notification objesi oluştur
        const notification = {
            id: Date.now(),
            type: 'NewOrder',
            title: 'Yeni Sipariş!',
            message: orderData.Message,
            icon: orderData.Icon,
            color: orderData.Color,
            timestamp: new Date(orderData.Timestamp),
            data: orderData,
            isRead: false
        };

        // Notifications array'e ekle
        this.notifications.unshift(notification);

        // Browser notification
        this.showBrowserNotification(notification);

        // UI güncellemesi
        this.updateNotificationUI();

        // Sound notification
        this.playNotificationSound();

        // Masa listesini güncelle
        if (window.TableManager) {
            window.TableManager.loadTables();
        }

        // Toast notification
        if (window.ToastHelper) {
            window.ToastHelper.success(orderData.Message);
        }
    }

    handleTableStatusChange(tableData) {
        // Masa durumu değişikliği için UI güncellemesi
        if (window.TableManager) {
            window.TableManager.loadTables();
        }

        console.log("Masa durumu güncellendi:", tableData.TableName);
    }

    showBrowserNotification(notification) {
        // Browser notification permission kontrolü
        if ("Notification" in window) {
            if (Notification.permission === "granted") {
                new Notification(notification.title, {
                    body: notification.message,
                    icon: "/images/logo-notification.png",
                    badge: "/images/badge.png",
                    tag: notification.type,
                    renotify: true
                });
            } else if (Notification.permission !== "denied") {
                Notification.requestPermission().then((permission) => {
                    if (permission === "granted") {
                        this.showBrowserNotification(notification);
                    }
                });
            }
        }
    }

    updateNotificationUI() {
        // Notification badge güncellemesi
        const badge = document.getElementById('notificationBadge');
        const unreadCount = this.notifications.filter(n => !n.isRead).length;

        if (badge) {
            if (unreadCount > 0) {
                badge.style.display = 'flex';
                badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
            } else {
                badge.style.display = 'none';
            }
        }

        // Notification list güncellemesi
        this.renderNotificationList();
    }

    renderNotificationList() {
        const container = document.getElementById('notificationList');
        if (!container) return;

        if (this.notifications.length === 0) {
            container.innerHTML = `
                <div class="no-notifications">
                    <i class="fas fa-bell-slash"></i>
                    <p>Henüz bildirim yok</p>
                </div>
            `;
            return;
        }

        let html = '';
        this.notifications.slice(0, 20).forEach(notification => { // Son 20 bildirim
            html += this.renderNotificationItem(notification);
        });

        container.innerHTML = html;
        this.bindNotificationEvents();
    }

    renderNotificationItem(notification) {
        const timeAgo = this.getTimeAgo(notification.timestamp);

        return `
            <div class="notification-item ${!notification.isRead ? 'unread' : ''}" 
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
                    ${!notification.isRead ? '<button class="mark-read-btn" title="Okundu işaretle"><i class="fas fa-check"></i></button>' : ''}
                    <button class="delete-notification-btn" title="Sil"><i class="fas fa-trash"></i></button>
                </div>
            </div>
        `;
    }

    bindNotificationEvents() {
        // Mark as read
        document.querySelectorAll('.mark-read-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const notificationId = parseInt(btn.closest('.notification-item').dataset.notificationId);
                this.markAsRead(notificationId);
            });
        });

        // Delete notification
        document.querySelectorAll('.delete-notification-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const notificationId = parseInt(btn.closest('.notification-item').dataset.notificationId);
                this.deleteNotification(notificationId);
            });
        });

        // Click notification (navigate to table)
        document.querySelectorAll('.notification-item').forEach(item => {
            item.addEventListener('click', () => {
                const notificationId = parseInt(item.dataset.notificationId);
                const notification = this.notifications.find(n => n.id === notificationId);
                if (notification && notification.data.TableId) {
                    // Masaya git
                    this.navigateToTable(notification.data.TableId);
                    this.markAsRead(notificationId);
                }
            });
        });
    }

    markAsRead(notificationId) {
        const notification = this.notifications.find(n => n.id === notificationId);
        if (notification) {
            notification.isRead = true;
            this.updateNotificationUI();
        }
    }

    deleteNotification(notificationId) {
        this.notifications = this.notifications.filter(n => n.id !== notificationId);
        this.updateNotificationUI();
    }

    markAllAsRead() {
        this.notifications.forEach(n => n.isRead = true);
        this.updateNotificationUI();
    }

    clearAllNotifications() {
        this.notifications = [];
        this.updateNotificationUI();
    }

    navigateToTable(tableId) {
        // Masa detaylarını aç (mevcut TableManager kullanarak)
        if (window.TableManager && window.TableManager.openTableModal) {
            // Masa modalını aç
            console.log("Masaya yönlendiriliyor:", tableId);
            // Burada TableManager'ın openTableModal methodunu çağırabilirsiniz
        }
    }

    updateConnectionStatus(isConnected) {
        const statusIndicator = document.getElementById('signalrStatus');
        if (statusIndicator) {
            statusIndicator.className = isConnected ? 'status-connected' : 'status-disconnected';
            statusIndicator.title = isConnected ? 'Canlı bağlantı aktif' : 'Bağlantı kopuk';
        }
    }

    playNotificationSound() {
        // Notification sound çalma
        try {
            const audio = new Audio('/sounds/notification.mp3');
            audio.volume = 0.3;
            audio.play().catch(e => console.log("Ses çalınamadı:", e));
        } catch (error) {
            console.log("Notification sound error:", error);
        }
    }

    getTimeAgo(date) {
        const seconds = Math.floor((new Date() - date) / 1000);

        let interval = seconds / 31536000;
        if (interval > 1) return Math.floor(interval) + " yıl önce";

        interval = seconds / 2592000;
        if (interval > 1) return Math.floor(interval) + " ay önce";

        interval = seconds / 86400;
        if (interval > 1) return Math.floor(interval) + " gün önce";

        interval = seconds / 3600;
        if (interval > 1) return Math.floor(interval) + " saat önce";

        interval = seconds / 60;
        if (interval > 1) return Math.floor(interval) + " dakika önce";

        return "Az önce";
    }

    // Public methods
    getNotifications() {
        return this.notifications;
    }

    getUnreadCount() {
        return this.notifications.filter(n => !n.isRead).length;
    }

    isSignalRConnected() {
        return this.isConnected;
    }
}

// Global SignalR client
let adminSignalR = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    adminSignalR = new AdminSignalRClient();
    window.adminSignalR = adminSignalR; // Global access
});