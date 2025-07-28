// KufeArt.TabletMvc/wwwroot/js/tablet-signalr.js

class TabletSignalR {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.department = this.getDepartmentFromUser();
    }

    getDepartmentFromUser() {
        // HTML'den department bilgisini çek
        const departmentElement = document.querySelector('.tablet-details h3');
        if (departmentElement) {
            return departmentElement.textContent.trim();
        }
        return '';
    }

    static init() {
        if (!window.tabletSignalR) {
            window.tabletSignalR = new TabletSignalR();
            window.tabletSignalR.connect();
        }
        return window.tabletSignalR;
    }

    // KufeArt.TabletMvc/wwwroot/js/tablet-signalr.js
    // KufeArt.TabletMvc/wwwroot/js/tablet-signalr.js
    async connect() {
        try {
            console.log('🔄 SignalR bağlantısı kuruluyor...');

            // ✅ Kendi TabletHub'ını kullan
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/tabletHub") // Kendi hub'ı
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
        console.log('🔔 Yeni sipariş geldi:', orderData);

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
            console.log('🔔 Bu departmana ait sipariş var, bildirim gösteriliyor...');

            // ✅ 1. SES ÇAL (En önemli!)
            this.playNotificationSound();

            // ✅ 2. VİBRASYON
            TabletUtils.vibrate([300, 100, 300, 100, 300]);

            // ✅ 3. TOAST BİLDİRİM
            const message = `🔔 YENİ SİPARİŞ: ${orderData.TableName} - ${relevantItems.length} ürün`;
            TabletUtils.showToast(message, 'info', 8000);

            // ✅ 4. DASHBOARD'I YENİLE
            if (window.TabletDashboard) {
                console.log('🔄 Dashboard yenileniyor...');
                window.TabletDashboard.loadOrders();
            }
        }
    }


    // ✅ YENİ: Ses çalma method'u ekle
    playNotificationSound() {
        try {
            console.log('🔊 Bildirim sesi çalınıyor...');

            // Tablet için daha güçlü ses sistemi
            const audio = new Audio('/sounds/notification.mp3');
            audio.volume = 0.8; // Yüksek ses
            audio.preload = 'auto';

            // Multiple ses çal (tablet'te daha etkili)
            const playPromise = audio.play();

            if (playPromise !== undefined) {
                playPromise
                    .then(() => {
                        console.log('✅ Ses başarıyla çalındı');

                        // 2 saniye sonra tekrar çal (urgent için)
                        setTimeout(() => {
                            const audio2 = new Audio('/sounds/notification.mp3');
                            audio2.volume = 0.7;
                            audio2.play().catch(e => console.log('İkinci ses çalınamadı:', e));
                        }, 1500);
                    })
                    .catch(error => {
                        console.error('❌ Ses çalınamadı:', error);
                        // Fallback: System beep
                        this.fallbackBeep();
                    });
            }

            // TabletUtils ses sistemi de çalıştır
            TabletUtils.playNotificationSound();

        } catch (error) {
            console.error('❌ Ses sistemi hatası:', error);
            this.fallbackBeep();
        }
    }

    // ✅ Fallback beep sistemi
    fallbackBeep() {
        try {
            // Web Audio API ile beep
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            oscillator.frequency.value = 800; // Yüksek frekans
            gainNode.gain.value = 0.3;

            oscillator.start();
            oscillator.stop(audioContext.currentTime + 0.3);

            console.log('🔔 Fallback beep çalındı');
        } catch (error) {
            console.log('Fallback beep de çalınamadı:', error);
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