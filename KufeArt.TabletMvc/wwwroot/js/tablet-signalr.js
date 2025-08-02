
class TabletSignalR {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.department = this.getDepartmentFromUser();
    }

    getDepartmentFromUser() {
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
    async connect() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/tabletHub") 
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            this.bindEvents();

            await this.connection.start();

            if (this.department === 'Kitchen') {
                await this.connection.invoke("JoinKitchenGroup");
            } else if (this.department === 'Bar') {
                await this.connection.invoke("JoinBarGroup");
            }

            this.isConnected = true;
            this.updateConnectionStatus(true);
            this.reconnectAttempts = 0;

        } catch (error) {
            this.isConnected = false;
            this.updateConnectionStatus(false);
            this.handleConnectionError();
        }
    }

    bindEvents() {
        if (!this.connection) return;

        this.connection.on("NewOrderReceived", (orderData) => {
            this.handleNewOrder(orderData);
        });

        this.connection.on("OrderStatusChanged", (statusData) => {
            this.handleOrderStatusChange(statusData);
        });

        this.connection.onclose((error) => {
            this.isConnected = false;
            this.updateConnectionStatus(false);
        });

        this.connection.onreconnected((connectionId) => {
            this.isConnected = true;
            this.updateConnectionStatus(true);
            this.reconnectAttempts = 0;
        });

        this.connection.onreconnecting((error) => {
            this.updateConnectionStatus(false);
        });
    }

    handleNewOrder(orderData) {

        if (window.TabletDashboard) {
            window.TabletDashboard.loadOrders().then(() => {
 
                this.playNotificationSound();
                TabletUtils.vibrate([300, 100, 300, 100, 300]);

                this.triggerVisualEffects(orderData);

                if (window.TabletDashboard) {
                    console.log('🔄 Dashboard yenileniyor...');
                    window.TabletDashboard.loadOrders();
                }
            });
        }
    }
    triggerVisualEffects(orderData) {
        document.body.classList.add('shake-screen');
        setTimeout(() => document.body.classList.remove('shake-screen'), 600);

        this.createConfetti();

        this.showNewOrderAlert(orderData);

        this.highlightStats();
    }

    createConfetti() {
        const container = document.createElement('div');
        container.className = 'confetti-container';
        document.body.appendChild(container);

        const colors = ['red', 'yellow', 'green', 'blue', 'purple'];

        for (let i = 0; i < 30; i++) {
            setTimeout(() => {
                const confetti = document.createElement('div');
                confetti.className = `confetti confetti-${colors[Math.floor(Math.random() * colors.length)]} confetti-fall`;
                confetti.style.left = Math.random() * 100 + '%';
                confetti.style.animationDelay = Math.random() * 0.3 + 's';
                confetti.style.animationDuration = (Math.random() * 2 + 2) + 's';
                container.appendChild(confetti);

                setTimeout(() => confetti.remove(), 3000);
            }, i * 50);
        }

        setTimeout(() => container.remove(), 5000);
    }

    showNewOrderAlert(orderData) {
        const alert = document.createElement('div');
        alert.className = 'new-order-alert';
        alert.innerHTML = `
        <div class="alert-content">
            <div class="alert-icon">
                <i class="fas fa-bell"></i>
            </div>
            <div class="alert-text">
                <h4>YENİ SİPARİŞ!</h4>
                <p>${orderData.tableName || orderData.TableName} - ${orderData.totalAmount || orderData.TotalAmount}₺</p>
            </div>
        </div>
    `;

        document.body.appendChild(alert);

        setTimeout(() => {
            alert.classList.add('slide-out');
            setTimeout(() => alert.remove(), 500);
        }, 4000);
    }

    highlightStats() {
        document.querySelectorAll('.stat-card').forEach((card, index) => {
            setTimeout(() => {
                card.classList.add('highlight');
                setTimeout(() => card.classList.remove('highlight'), 2000);
            }, index * 200);
        });
    }

    playNotificationSound() {
        try {
            console.log('🔊 Bildirim sesi çalınıyor...');

            const audio = new Audio('/sounds/notification.mp3');
            audio.volume = 0.8; 
            audio.preload = 'auto';

            const playPromise = audio.play();

            if (playPromise !== undefined) {
                playPromise
                    .then(() => {

                        setTimeout(() => {
                            const audio2 = new Audio('/sounds/notification.mp3');
                            audio2.volume = 0.7;
                            audio2.play().catch(e => console.log('İkinci ses çalınamadı:', e));
                        }, 1500);
                    })
                    .catch(error => {
                        this.fallbackBeep();
                    });
            }

            TabletUtils.playNotificationSound();

        } catch (error) {
            this.fallbackBeep();
        }
    }

    fallbackBeep() {
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            oscillator.frequency.value = 800; // Yüksek frekans
            gainNode.gain.value = 0.3;

            oscillator.start();
            oscillator.stop(audioContext.currentTime + 0.3);

        } catch (error) {
        }
    }

    handleOrderStatusChange(statusData) {
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

    isConnectionActive() {
        return this.isConnected && this.connection?.state === signalR.HubConnectionState.Connected;
    }

    async sendMessage(method, ...args) {
        if (this.isConnectionActive()) {
            try {
                return await this.connection.invoke(method, ...args);
            } catch (error) {
                throw error;
            }
        } else {
            throw new Error('SignalR bağlantısı aktif değil');
        }
    }
}
window.TabletSignalR = TabletSignalR;