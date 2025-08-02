
class GarsonDashboard {
    constructor() {
        this.tables = [];
        this.currentCategory = 'all';
        this.searchQuery = '';
        this.refreshInterval = null;

        this.init();
    }

    init() {
        this.loadTables();
        this.bindEvents();
        this.startAutoRefresh();
    }

    bindEvents() {
        document.getElementById('refreshBtn').addEventListener('click', () => {
            this.loadTables();
            this.animateRefreshButton();
        });

        document.getElementById('tableSearch').addEventListener('input', (e) => {
            this.searchQuery = e.target.value.toLowerCase();
            this.filterTables();
        });

        this.bindTouchEvents();
    }

    async loadTables() {
        try {
            const response = await fetch('/Home/GetTables');
            const result = await response.json();

            if (result.success) {
                this.tables = result.data;
                this.renderCategoryTabs();
                this.renderTables();
                this.updateStats();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Bağlantı hatası! Masalar yüklenemedi.');
        }
    }

    renderCategoryTabs() {
        const tabsContainer = document.getElementById('categoryTabs');
        const categories = Object.keys(this.tables);

        let tabsHTML = `
            <button class="filter-tab ${this.currentCategory === 'all' ? 'active' : ''}" 
                    data-category="all">
                <i class="fas fa-th-large"></i>
                Tümü
            </button>
        `;

        categories.forEach(category => {
            const tableCount = this.tables[category].length;
            const occupiedCount = this.tables[category].filter(t => t.isOccupied).length;

            tabsHTML += `
                <button class="filter-tab ${this.currentCategory === category ? 'active' : ''}" 
                        data-category="${category}">
                    <i class="fas fa-utensils"></i>
                    ${category} (${occupiedCount}/${tableCount})
                </button>
            `;
        });

        tabsContainer.innerHTML = tabsHTML;

        tabsContainer.addEventListener('click', (e) => {
            if (e.target.classList.contains('filter-tab') || e.target.closest('.filter-tab')) {
                const tab = e.target.closest('.filter-tab');
                const category = tab.dataset.category;
                this.switchCategory(category);
            }
        });
    }

    renderTables() {
        const container = document.getElementById('tablesContainer');
        let html = '';

        if (this.currentCategory === 'all') {
            Object.keys(this.tables).forEach(category => {
                const filteredTables = this.filterTablesBySearch(this.tables[category]);
                if (filteredTables.length > 0) {
                    html += this.renderCategorySection(category, filteredTables);
                }
            });
        } else {
            const filteredTables = this.filterTablesBySearch(this.tables[this.currentCategory] || []);
            if (filteredTables.length > 0) {
                html += this.renderCategorySection(this.currentCategory, filteredTables);
            }
        }

        if (html === '') {
            html = `
                <div class="no-tables">
                    <i class="fas fa-search fa-3x"></i>
                    <p>Aradığınız kriterde masa bulunamadı</p>
                </div>
            `;
        }

        container.innerHTML = html;
        this.bindTableEvents();
    }

    renderCategorySection(category, tables) {
        return `
            <div class="table-category">
                <h4 class="category-title">
                    <i class="fas fa-utensils me-2"></i>
                    ${category} (${tables.length})
                </h4>
                <div class="tables-grid">
                    ${tables.map(table => this.renderTableCard(table)).join('')}
                </div>
            </div>
        `;
    }

    renderTableCard(table) {
        const status = table.isOccupied ? 'occupied' : 'empty';
        const statusIcon = table.isOccupied ? 'fas fa-chair' : 'fas fa-plus-circle';
        const duration = table.isOccupied ? this.formatDuration(table.duration) : '';
        const amount = table.isOccupied ? this.formatCurrency(table.remainingAmount) : 'Boş Masa';

        return `
        <div class="table-card ${status}" 
             data-table-id="${table.id}" 
             data-table-name="${table.name}"
             data-is-occupied="${table.isOccupied}">
            
            ${table.isOccupied ? `
                <!-- ✅ DOLU MASA: 3 İKON - Geçmiş, Taşıma, Birleştirme -->
                <div class="table-actions">
                    <button class="action-btn history-btn" 
                            onclick="event.stopPropagation(); Dashboard.showTableHistory('${table.id}', '${table.name}')"
                            title="Sipariş Geçmişi">
                        <i class="fas fa-history"></i>
                    </button>
                    <button class="action-btn move-btn" 
                            onclick="event.stopPropagation(); TableActions.showMoveModal('${table.id}', '${table.name}')"
                            title="Masa Taşı">
                        <i class="fas fa-arrows-alt"></i>
                    </button>
                    <button class="action-btn merge-btn" 
                            onclick="event.stopPropagation(); TableActions.showMergeModal('${table.id}', '${table.name}')"
                            title="Masa Birleştir">
                        <i class="fas fa-link"></i>
                    </button>
                </div>
            ` : ''}
            
            <div class="table-icon">
                <i class="${statusIcon}"></i>
            </div>
            <div class="table-name">${table.name}</div>
            <div class="table-amount">${amount}</div>
            ${duration ? `<div class="table-duration">${duration}</div>` : ''}
        </div>
    `;
    }

    static showTableHistory(tableId, tableName) {
        Dashboard.openTableHistoryModal(tableId, tableName);
    }

    static async openTableHistoryModal(tableId, tableName) {
      
        const modalHTML = `
        <div class="modal fade" id="tableHistoryModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">
                            <i class="fas fa-history me-2"></i>
                            ${tableName} - Sipariş Geçmişi
                        </h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body" id="tableHistoryContent">
                        <div class="history-loading">
                            <i class="fas fa-spinner fa-spin"></i>
                            <p>Geçmiş yükleniyor...</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);
        const modal = new bootstrap.Modal(document.getElementById('tableHistoryModal'));
        modal.show();

        try {
            const response = await fetch(`/Order/GetOrderHistory?tableId=${tableId}`);
            const result = await response.json();

            const content = document.getElementById('tableHistoryContent');

            if (result.success && result.data.length > 0) {
                let html = '<div class="history-items">';
                result.data.forEach(order => {
                    html += `
                    <div class="history-item">
                        <div class="history-header">
                            <span class="order-time">${order.formattedTime}</span>
                            <span class="order-waiter">${order.personFullName}</span>
                            <button class="btn-cancel-order" 
                                    onclick="Dashboard.cancelOrderItem('${order.id}', '${order.productName}', '${tableId}')">
                                <i class="fas fa-times"></i> İptal Et
                            </button>
                        </div>
                        <div class="history-details">
                            <div class="product-name">${order.productName}</div>
                            <div class="order-info">
                                ${order.productQuantity} x ₺${order.productPrice.toFixed(2)} = 
                                <strong>₺${order.totalPrice.toFixed(2)}</strong>
                            </div>
                            ${order.shorLabel ? `<div class="order-note">${order.shorLabel}</div>` : ''}
                        </div>
                    </div>
                `;
                });
                html += '</div>';
                content.innerHTML = html;
            } else {
                content.innerHTML = `
                <div class="no-history">
                    <i class="fas fa-history fa-3x"></i>
                    <p>Bu masada henüz sipariş geçmişi yok</p>
                </div>
            `;
            }
        } catch (error) {
            const content = document.getElementById('tableHistoryContent');
            content.innerHTML = `
            <div class="history-error">
                <i class="fas fa-wifi"></i>
                <p>Bağlantı hatası!</p>
            </div>
        `;
        }

        document.getElementById('tableHistoryModal').addEventListener('hidden.bs.modal', () => {
            document.getElementById('tableHistoryModal').remove();
        });
    }

    bindTableEvents() {
        const tableCards = document.querySelectorAll('.table-card');

        tableCards.forEach(card => {
        
            card.addEventListener('click', () => {
                const tableId = card.dataset.tableId;
                const tableName = card.dataset.tableName;
                const isOccupied = card.dataset.isOccupied === 'true';

                this.selectTable(tableId, tableName, isOccupied);
            });

       
            let pressTimer;
            card.addEventListener('touchstart', (e) => {
                pressTimer = window.setTimeout(() => {
                    this.showTableDetails(card.dataset.tableId);
                }, 800);
            });

            card.addEventListener('touchend', () => {
                clearTimeout(pressTimer);
            });
        });
    }

    selectTable(tableId, tableName, isOccupied) {

        if ('vibrate' in navigator) {
            navigator.vibrate(50);
        }

        const card = document.querySelector(`[data-table-id="${tableId}"]`);
        card.style.transform = 'scale(0.95)';
        setTimeout(() => {
            card.style.transform = '';
        }, 150);

        setTimeout(() => {
            window.location.href = `/Order/Index?tableId=${tableId}&tableName=${encodeURIComponent(tableName)}&isOccupied=${isOccupied}`;
        }, 200);
    }

    switchCategory(category) {
        this.currentCategory = category;

        document.querySelectorAll('.filter-tab').forEach(tab => {
            tab.classList.remove('active');
        });
        document.querySelector(`[data-category="${category}"]`).classList.add('active');

        this.renderTables();
    }

    filterTables() {
        this.renderTables();
    }

    filterTablesBySearch(tables) {
        if (!this.searchQuery) return tables;

        return tables.filter(table =>
            table.name.toLowerCase().includes(this.searchQuery)
        );
    }

    updateStats() {
        const allTables = Object.values(this.tables).flat();
        const activeCount = allTables.filter(t => t.isOccupied).length;

        const activeTableCountElement = document.getElementById('activeTableCount');
        if (activeTableCountElement) {
            activeTableCountElement.textContent = activeCount;
        }

        const todayOrderCountElement = document.getElementById('todayOrderCount');
        if (todayOrderCountElement && this.todayOrderCount !== undefined) {
            todayOrderCountElement.textContent = this.todayOrderCount;
        }
    }

    startAutoRefresh() {
        this.refreshInterval = setInterval(() => {
            this.loadTables();
        }, 30000);
    }

    animateRefreshButton() {
        const btn = document.getElementById('refreshBtn');
        btn.style.transform = 'rotate(360deg)';
        setTimeout(() => {
            btn.style.transform = '';
        }, 500);
    }

    formatDuration(minutes) {
        if (minutes < 60) {
            return `${Math.floor(minutes)}dk`;
        } else {
            const hours = Math.floor(minutes / 60);
            const mins = Math.floor(minutes % 60);
            return `${hours}s ${mins}dk`;
        }
    }

    formatCurrency(amount) {
        return new Intl.NumberFormat('tr-TR', {
            style: 'currency',
            currency: 'TRY',
            minimumFractionDigits: 0,
            maximumFractionDigits: 2
        }).format(amount);
    }

    showError(message) {
        const toast = document.createElement('div');
        toast.className = 'toast-error';
        toast.innerHTML = `
            <i class="fas fa-exclamation-triangle"></i>
            ${message}
        `;

        document.body.appendChild(toast);

        setTimeout(() => {
            toast.remove();
        }, 4000);
    }

    bindTouchEvents() {
        let lastTouchEnd = 0;
        document.addEventListener('touchend', (event) => {
            const now = (new Date()).getTime();
            if (now - lastTouchEnd <= 300) {
                event.preventDefault();
            }
            lastTouchEnd = now;
        }, false);

        let startY = 0;
        let isDragging = false;

        document.addEventListener('touchstart', (e) => {
            startY = e.touches[0].clientY;
            isDragging = true;
        });

        document.addEventListener('touchmove', (e) => {
            if (!isDragging || window.scrollY > 0) return;

            const currentY = e.touches[0].clientY;
            const diff = currentY - startY;

            if (diff > 100) {
                this.loadTables();
                isDragging = false;
            }
        });

        document.addEventListener('touchend', () => {
            isDragging = false;
        });
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new GarsonDashboard();
});