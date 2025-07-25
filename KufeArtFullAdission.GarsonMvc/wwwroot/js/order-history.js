// KufeArtFullAdission.GarsonMvc/wwwroot/js/order-history.js
class OrderHistory {
    constructor() {
        this.isLoading = false;
        this.currentStartDate = document.getElementById('startDate').value;
        this.currentEndDate = document.getElementById('endDate').value;

        this.bindEvents();
        this.setupPullToRefresh();
    }

    bindEvents() {
        // Date input changes
        document.getElementById('startDate').addEventListener('change', () => this.validateDates());
        document.getElementById('endDate').addEventListener('change', () => this.validateDates());

        // Enter key on date inputs
        document.getElementById('startDate').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.applyFilter();
        });
        document.getElementById('endDate').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.applyFilter();
        });
    }

    validateDates() {
        const startDate = new Date(document.getElementById('startDate').value);
        const endDate = new Date(document.getElementById('endDate').value);
        const today = new Date();

        // End date bugün'den büyük olamaz
        if (endDate > today) {
            document.getElementById('endDate').value = today.toISOString().split('T')[0];
        }

        // Start date end date'den büyük olamaz
        if (startDate > endDate) {
            document.getElementById('startDate').value = document.getElementById('endDate').value;
        }

        // Maksimum 30 gün aralık
        const diffTime = Math.abs(endDate - startDate);
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

        if (diffDays > 30) {
            this.showToast('Maksimum 30 günlük aralık seçebilirsiniz', 'warning');
            const newStartDate = new Date(endDate);
            newStartDate.setDate(newStartDate.getDate() - 30);
            document.getElementById('startDate').value = newStartDate.toISOString().split('T')[0];
        }
    }

    async applyFilter() {
        if (this.isLoading) return;

        const startDate = document.getElementById('startDate').value;
        const endDate = document.getElementById('endDate').value;

        if (!startDate || !endDate) {
            this.showToast('Lütfen tarih aralığını seçin', 'error');
            return;
        }

        await this.loadHistory(startDate, endDate);
    }

    async loadHistory(startDate, endDate) {
        this.setLoading(true);

        try {
            // ❌ Yanlış: /Order/HistoryData
            // ✅ Doğru: /Order/GetHistoryData
            const response = await fetch(`/Order/GetHistoryData?startDate=${startDate}&endDate=${endDate}`);

            const result = await response.json();

            if (result.success) {
                this.renderHistory(result.data);
                this.updateSummary(result.data);
                this.currentStartDate = startDate;
                this.currentEndDate = endDate;
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Veri yüklenirken hata oluştu');
            console.error('Load history error:', error);
        } finally {
            this.setLoading(false);
        }
    }

    renderHistory(orders) {
        const container = document.getElementById('historyContent');
        const noHistory = document.getElementById('noHistory');

        if (orders.length === 0) {
            container.style.display = 'none';
            noHistory.style.display = 'block';
            return;
        }

        container.style.display = 'block';
        noHistory.style.display = 'none';

        // Günlere göre grupla
        const groupedByDate = this.groupOrdersByDate(orders);

        let html = '';
        groupedByDate.forEach(dayGroup => {
            html += this.renderDayGroup(dayGroup);
        });

        container.innerHTML = html;

        // Animasyon ekle
        container.querySelectorAll('.history-day-group').forEach((group, index) => {
            group.style.animationDelay = `${index * 0.1}s`;
            group.classList.add('fade-in');
        });
    }

    groupOrdersByDate(orders) {
        const grouped = {};

        orders.forEach(order => {
            const date = order.formattedDate;
            if (!grouped[date]) {
                grouped[date] = {
                    date: date,
                    orders: [],
                    totalAmount: 0,
                    orderCount: 0
                };
            }
            grouped[date].orders.push(order);
            grouped[date].totalAmount += order.totalPrice;
            grouped[date].orderCount++;
        });

        return Object.values(grouped).sort((a, b) => new Date(b.date) - new Date(a.date));
    }

    renderDayGroup(dayGroup) {
        const date = new Date(dayGroup.date);
        const formattedDate = date.toLocaleDateString('tr-TR', {
            day: 'numeric',
            month: 'long',
            year: 'numeric'
        });

        let itemsHtml = '';
        dayGroup.orders.forEach(order => {
            itemsHtml += `
                <div class="history-item">
                    <div class="item-header">
                        <div class="item-time">
                            <i class="fas fa-clock me-1"></i>
                            ${order.formattedTime}
                        </div>
                        <div class="item-table">
                            <i class="fas fa-utensils me-1"></i>
                            ${order.tableName || 'Masa'}
                        </div>
                    </div>
                    
                    <div class="item-product">
                        <div class="product-info">
                            <h5 class="product-name">${order.productName}</h5>
                            <div class="product-details">
                                ${order.productQuantity} x ₺${order.productPrice.toFixed(2)} = 
                                <strong>₺${order.totalPrice.toFixed(2)}</strong>
                            </div>
                        </div>
                    </div>
                    
                    ${order.shorLabel ? `
                        <div class="item-note">
                            <i class="fas fa-sticky-note me-1"></i>
                            ${order.shorLabel}
                        </div>
                    ` : ''}
                </div>
            `;
        });

        return `
            <div class="history-day-group" data-date="${dayGroup.date}">
                <div class="day-header">
                    <div class="day-info">
                        <h4 class="day-title">${formattedDate}</h4>
                        <small class="day-stats">
                            ${dayGroup.orderCount} sipariş • ₺${dayGroup.totalAmount.toFixed(2)}
                        </small>
                    </div>
                    <div class="day-total">
                        <span class="total-badge">₺${dayGroup.totalAmount.toFixed(2)}</span>
                    </div>
                </div>
                
                <div class="history-items">
                    ${itemsHtml}
                </div>
            </div>
        `;
    }

    updateSummary(orders) {
        const totalOrders = orders.length;
        const totalAmount = orders.reduce((sum, order) => sum + order.totalPrice, 0);

        document.getElementById('totalOrders').textContent = totalOrders;
        document.getElementById('totalAmount').textContent = `₺${totalAmount.toFixed(2)}`;
    }

    resetFilter() {
        const today = new Date();
        const weekAgo = new Date();
        weekAgo.setDate(today.getDate() - 7);

        document.getElementById('startDate').value = weekAgo.toISOString().split('T')[0];
        document.getElementById('endDate').value = today.toISOString().split('T')[0];

        this.applyFilter();
    }

    setLoading(loading) {
        this.isLoading = loading;
        const loadingElement = document.getElementById('historyLoading');
        const content = document.getElementById('historyContent');
        const noHistory = document.getElementById('noHistory');

        if (loading) {
            loadingElement.style.display = 'block';
            content.style.display = 'none';
            noHistory.style.display = 'none';
        } else {
            loadingElement.style.display = 'none';
        }
    }

    setupPullToRefresh() {
        let startY = 0;
        let isDragging = false;
        const pullRefresh = document.getElementById('pullRefresh');

        document.addEventListener('touchstart', (e) => {
            startY = e.touches[0].clientY;
            isDragging = true;
        });

        document.addEventListener('touchmove', (e) => {
            if (!isDragging || window.scrollY > 0) return;

            const currentY = e.touches[0].clientY;
            const diff = currentY - startY;

            if (diff > 50) {
                pullRefresh.style.display = 'block';
                pullRefresh.style.transform = `translateY(${Math.min(diff - 50, 50)}px)`;
            }

            if (diff > 100) {
                this.refreshData();
                isDragging = false;
                pullRefresh.style.display = 'none';
                pullRefresh.style.transform = '';
            }
        });

        document.addEventListener('touchend', () => {
            isDragging = false;
            pullRefresh.style.display = 'none';
            pullRefresh.style.transform = '';
        });
    }

    async refreshData() {
        await this.loadHistory(this.currentStartDate, this.currentEndDate);
        this.showToast('Veriler güncellendi', 'success');
    }

    showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <i class="fas fa-${this.getToastIcon(type)}"></i>
            ${message}
        `;

        document.body.appendChild(toast);

        setTimeout(() => toast.classList.add('show'), 100);
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    getToastIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        return icons[type] || icons.info;
    }

    showError(message) {
        this.showToast(message, 'error');
    }
}

// Global functions
function applyFilter() {
    window.orderHistory.applyFilter();
}

function resetFilter() {
    window.orderHistory.resetFilter();
}

function openFilterModal() {
    // Gelecekte detaylı filtre modal'ı için
    document.getElementById('startDate').focus();
}

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    window.orderHistory = new OrderHistory();
});