﻿
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
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/waiterHub")
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            this.bindSignalREvents();
            await this.connection.start();
            console.log("🔗 SignalR bağlantısı kuruldu");

            this.waiterName = this.getWaiterName();
            await this.connection.invoke("JoinWaiterGroup", this.waiterName);
            console.log("👥 Garson grubuna katıldı:", this.waiterName);

            this.isConnected = true;
            console.log("✅ Garson Hub'ına bağlandı");

        } catch (error) {
            console.error("❌ SignalR bağlantı hatası:", error);
        }
    }


    deleteNotification(notificationId) {
        this.notifications = this.notifications.filter(n => n.id !== notificationId);
        this.updateNotificationBadge();
        this.renderNotificationPanel();
        this.saveNotificationsToStorage();
    }

    bindSignalREvents() {
        console.log("🔧 SignalR events bağlanıyor...");

        this.connection.on("InactiveTableAlert", (alertData) => {
            console.log("⏰ Masa takip uyarısı:", alertData);
            this.handleInactiveTableAlert(alertData);
        });

        // Admin bildirimi
        this.connection.on("AdminNotification", (data) => {
            console.log("📢 Admin bildirimi:", data);
            if (data.Type === "TableUpdate") {
                this.refreshPageData();
            }
            this.showToast(data.Message, 'info');
        });

        this.connection.on("TableOperationCompleted", (data) => {
            console.log("✅ Masa işlemi tamamlandı:", data);
            this.handleTableOperationCompleted(data);
        });

        // ✅ YENİ: Sipariş item iptal edildi
        this.connection.on("OrderItemCancelled", (data) => {
            console.log("❌ Sipariş item iptal edildi:", data);
            this.handleOrderItemCancelled(data);
        });

        // ✅ YENİ: Tablet'den sipariş tamamlama bildirimi
        this.connection.on("OrderCompletedFromTablet", (orderData) => {
            console.log("🍽️ Tablet'den sipariş hazır bildirimi:", orderData);
            this.handleOrderCompletedFromTablet(orderData);
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

        // Bağlantı durumu events
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

    handleOrderItemCancelled(data) {
        // Toast göster
        this.showToast(data.Message, 'info');

        // Dashboard'ı yenile
        this.refreshPageData();

        // Notification ekle
        const notification = {
            id: Date.now(),
            type: 'OrderCancelled',
            title: '❌ Sipariş İptal',
            message: data.Message,
            timestamp: new Date(),
            isRead: false
        };

        this.notifications.unshift(notification);
        this.updateNotificationBadge();
        this.saveNotificationsToStorage();
    }

    handleTableOperationCompleted(data) {
        // Toast göster
        this.showToast(data.Message, 'success');

        // Dashboard'ı yenile
        this.refreshPageData();

        // Notification ekle
        const notification = {
            id: Date.now(),
            type: 'TableOperation',
            title: '✅ İşlem Tamamlandı',
            message: data.Message,
            timestamp: new Date(),
            isRead: false
        };

        this.notifications.unshift(notification);
        this.updateNotificationBadge();
        this.saveNotificationsToStorage();
    }

    // ✅ YENİ: Tablet'den gelen sipariş tamamlama bildirimini işle
    handleOrderCompletedFromTablet(orderData) {
        const notification = {
            id: Date.now(),
            type: 'OrderCompletedFromTablet',
            title: '🍽️ Sipariş Hazır!',
            message: orderData.Message,
            icon: orderData.Icon,
            color: orderData.Color,
            timestamp: new Date(orderData.Timestamp),
            data: orderData,
            isRead: false,
            priority: 'high'
        };

        this.addNotification(notification);
        this.showMobileNotification(notification);
        this.playNotificationSound();
        this.showToast(notification.message, 'success');

        // Vibration for mobile
        if ('vibrate' in navigator) {
            navigator.vibrate([200, 100, 200, 100, 200]);
        }

        this.updateNotificationBadge();

        console.log("✅ Tablet bildirimi işlendi:", notification);
    }

    handleInactiveTableAlert(alertData) {
        console.log("🚨 MASA UYARISI GELDİ:", alertData);

        const notification = {
            id: Date.now(),
            type: 'InactiveTable',
            title: '⏰ Masa Takip',
            message: `${alertData.tableName} - ${alertData.inactiveMinutes} dakikadır sipariş yok`, // 🔥 Küçük harflerle
            icon: 'fas fa-clock',
            color: 'warning',
            timestamp: new Date(),
            data: alertData,
            isRead: false,
            priority: 'normal'
        };

        console.log("🔔 Bildirim oluşturuldu:", notification);
        this.addNotification(notification);
        this.playWarningSound();
        this.updateNotificationBadge();
    }

    // Uyarı sesi çalma
    playWarningSound() {
        try {
            const audio = new Audio('/sounds/table-warning.mp3');
            audio.volume = 0.5;
            audio.play().catch(e => console.log("Uyarı sesi çalınamadı:", e));
        } catch (error) {
            console.log("Ses çalma hatası:", error);
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
        this.showMobileNotification(notification);
        this.playNotificationSound();
        this.showToast(notification.message, 'success');

        if ('vibrate' in navigator) {
            navigator.vibrate([200, 100, 200, 100, 200]);
        }

        this.updateNotificationBadge();
    }

    handleTableStatusChange(tableData) {
        console.log("Masa durumu güncellendi:", tableData.TableName);

        if (window.location.pathname === '/' && window.GarsonDashboard) {
            setTimeout(() => {
                window.location.reload();
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

        if (this.notifications.length > 50) {
            this.notifications = this.notifications.slice(0, 50);
        }

        this.saveNotificationsToStorage();
    }

    showMobileNotification(notification) {
        if ("Notification" in window && Notification.permission === "granted") {
            const browserNotification = new Notification(notification.title, {
                body: notification.message,
                icon: '/images/garson-icon.png',
                badge: '/images/notification-badge.png',
                tag: notification.type,
                renotify: true,
                requireInteraction: notification.priority === 'high'
            });

            browserNotification.onclick = () => {
                window.focus();
                if (notification.data.TableId) {
                    this.navigateToTable(notification.data.TableId, notification.data.TableName);
                }
                browserNotification.close();
            };
        }
    }

    refreshPageData() {
        // Ana sayfadaysak masaları yenile
        if (window.GarsonDashboard) {
            window.GarsonDashboard.loadTables();
        }

        // Sipariş sayfasındaysak masa detayını yenile
        if (window.orderPageInstance) {
            window.orderPageInstance.loadTableDetails();
        }
    }

    // Notification Management
    updateNotificationBadge() {
        const unreadCount = this.notifications.filter(n => !n.isRead).length;
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
                ${!notification.isRead ? '<button class="mark-read-btn" title="Okundu işaretle"><i class="fas fa-check"></i></button>' : ''}
                <button class="delete-notification-btn" title="Sil"><i class="fas fa-trash"></i></button>
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

        // 🔥 YENİ: Delete notification
        document.querySelectorAll('.delete-notification-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const notificationId = parseInt(btn.closest('.notification-item').dataset.notificationId);
                this.deleteNotification(notificationId);
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
            }, 5000 * this.reconnectAttempts);
        } else {
            console.error("❌ SignalR maksimum bağlantı denemesi aşıldı");
            this.showToast("Canlı bildirimler devre dışı. Sayfa yenileyin.", "warning");
        }
    }

    updateConnectionStatus(isConnected) {
        const indicator = document.querySelector('.connection-status');
        if (indicator) {
            indicator.className = `connection-status ${isConnected ? 'connected' : 'disconnected'}`;
            indicator.title = isConnected ? 'Canlı bağlantı aktif' : 'Bağlantı kopuk';
        }

        document.body.classList.toggle('signalr-offline', !isConnected);
    }

    // Utility Methods
    getAdminPanelUrl() {
        return window.location.hostname === 'localhost'
            ? 'https://localhost:7164'
            : 'https://adisyon.kufeart.com';
    }

    getWaiterName() {
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
        if (window.showToast) {
            window.showToast(message, type);
        } else {
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
    if ("Notification" in window && Notification.permission === "default") {
        Notification.requestPermission();
    }

    waiterSignalR = new WaiterSignalRClient();
    window.waiterSignalR = waiterSignalR;
    waiterSignalR.loadNotificationsFromStorage();
});