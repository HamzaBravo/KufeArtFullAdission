// KufeArt.TabletMvc/wwwroot/js/tablet-signalr.js

class TabletSignalR {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.department = window.tabletSession?.department || '';
    }

    static init() {
        if (!window.tabletSignalR) {
            window.tabletSignalR = new TabletSignalR();
            window.tabletSignalR.connect();
        }
        return window.tabletSignalR;
    }

    async connect() {
        try {
            console.log('🔄 SignalR bağlantısı kuruluyor...');

            // SignalR connection oluştur
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/orderHub") // Ana projenizdeki OrderHub
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            // Event listeners
            this.bindEvents();

            // Bağlantıyı başlat
            await this.connection.start();
            console.log('✅ SignalR bağlandı');

            // Departmana göre gruba katıl
            if (this.department === 'Kitchen') {
                await this.connection.invoke("JoinKitchenGroup");
            } else if (this.department === 'Bar') {
                await this.connection.invoke("JoinBarGroup");
            }

            this.isConnected = true;
            this.updateConnectionStatus(true);
            this.reconnectAttempts = 0;

        } catch (error) {
            console.error('❌ SignalR bağlantı hatası:', error);
            this.isConnected = false;
            this.updateConnectionStatus(false);
            this.handleConnectionError();
        }
    }

    bindEvents() {
        if (!this.connection) return;

        // Yeni sipariş bildirimi
        this.connection.on("NewOrderReceived", (orderData) => {
            console.log('🔔 Yeni sipariş:', orderData);
            this.handleNewOrder(orderData);
        });

        // Sipariş durumu değişikliği
        this.connection.on("OrderStatusChanged", (statusData) => {
            console.log('🔄 Sipariş durumu değişti:', statusData);
            this.handleOrderStatusChange(statusData);
        });

        // Bağlantı events
        this.connection.onclose((error) => {
            console.log('❌ SignalR bağlantısı koptu:', error);
            this.isConnected = false;
            this.updateConnectionStatus(false);
        });

        this.connection.onreconnected((connectionId) => {
            console.log('✅ SignalR yeniden bağlandı:', connectionId);
            this.isConnected = true;
            this.updateConnectionStatus(true);
            this.reconnectAttempts = 0;
        });

        this.connection.onreconnecting((error) => {
            console.log('🔄 SignalR yeniden bağlanıyor...', error);
            this.updateConnectionStatus(false);
        });
    }

    handleNewOrder(orderData) {
        // Sadece kendi departmanımızla ilgili siparişleri dinle
        const orderItems = orderData.Items || [];
        const relevantItems = orderItems.filter(item => {
            if (this.department === 'Kitchen') {
                return item.ProductType === 'Kitchen' || item.ProductType === 'Mutfak';
            } else if (this.department === 'Bar') {
                return item.ProductType === 'Bar';
            }
            return false;
        });

        if (relevantItems.length > 0) {
            // Ses çal ve bildirim göster
            TabletUtils.playNotificationSound();
            TabletUtils.vibrate();

            // Toast bildirim
            const message = `${orderData.TableName} - ${relevantItems.length} ürün`;
            TabletUtils.showToast(message, 'info', 8000);

            // Dashboard'ı güncelle
            if (window.TabletDashboard) {
                window.TabletDashboard.refreshOrders();
            }
        }
    }

    handleOrderStatusChange(statusData) {
        // Sipariş durumu değişikliği
        if (window.TabletDashboard) {
            window.TabletDashboard.updateOrderStatus(statusData);
        }
    }

    updateConnectionStatus(isConnected) {
        const statusElement = document.getElementById('connectionStatus');
        if (statusElement) {
            statusElement.className = `connection-status ${isConnected ? '' : 'disconnected'}`;
            statusElement.title = isConnected ? 'Bağlantı aktif' : 'Bağlantı kopuk';
        }
    }

    handleConnectionError() {
        this.reconnectAttempts++;

        if (this.reconnectAttempts <= this.maxReconnectAttempts) {
            console.log(`🔄 Yeniden bağlanma denemesi: ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
            setTimeout(() => {
                this.connect();
            }, 5000 * this.reconnectAttempts);
        } else {
            console.error('❌ Maksimum bağlantı denemesi aşıldı');
            TabletUtils.showToast('Bağlantı kurulamadı. Sayfa yenileyin.', 'error');
        }
    }

    // Public methods
    isConnectionActive() {
        return this.isConnected && this.connection?.state === signalR.HubConnectionState.Connected;
    }

    async sendMessage(method, ...args) {
        if (this.isConnectionActive()) {
            try {
                return await this.connection.invoke(method, ...args);
            } catch (error) {
                console.error(`SignalR ${method} hatası:`, error);
                throw error;
            }
        } else {
            throw new Error('SignalR bağlantısı aktif değil');
        }
    }
}

// Global access
window.TabletSignalR = TabletSignalR;