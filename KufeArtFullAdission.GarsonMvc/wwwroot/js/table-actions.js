// KufeArtFullAdission.GarsonMvc/wwwroot/js/table-actions.js
class TableActions {
    static currentSourceTable = null;
    static selectedTargetTable = null;
    static currentAction = null; // 'move', 'merge', 'cancel'

    // 🔄 MASA TAŞIMA MODAL
    static async showMoveModal(sourceTableId, sourceTableName) {
        console.log('🔄 Masa taşıma başlatılıyor:', sourceTableName);

        this.currentSourceTable = { id: sourceTableId, name: sourceTableName };
        this.currentAction = 'move';
        this.selectedTargetTable = null;

        try {
            // Boş masaları al
            const response = await fetch('/Home/GetTables');
            const result = await response.json();

            if (!result.success) {
                this.showError('Masa listesi alınamadı!');
                return;
            }

            // Boş masaları filtrele
            const emptyTables = [];
            Object.values(result.data).forEach(categoryTables => {
                categoryTables.forEach(table => {
                    if (!table.isOccupied && table.id !== sourceTableId) {
                        emptyTables.push(table);
                    }
                });
            });

            if (emptyTables.length === 0) {
                this.showError('Taşıma için uygun boş masa bulunamadı!');
                return;
            }

            this.renderTableSelectionModal({
                title: '🔄 Masa Taşıma',
                subtitle: `<strong>${sourceTableName}</strong> hangi masaya taşınacak?`,
                icon: 'fas fa-arrows-alt',
                tables: emptyTables,
                confirmText: 'Masayı Taşı',
                confirmIcon: 'fas fa-arrows-alt'
            });

        } catch (error) {
            console.error('Masa taşıma hatası:', error);
            this.showError('Masa listesi yüklenirken hata oluştu!');
        }
    }

    // 🔗 MASA BİRLEŞTİRME MODAL
    static async showMergeModal(sourceTableId, sourceTableName) {
        console.log('🔗 Masa birleştirme başlatılıyor:', sourceTableName);

        this.currentSourceTable = { id: sourceTableId, name: sourceTableName };
        this.currentAction = 'merge';
        this.selectedTargetTable = null;

        try {
            // Dolu masaları al (kendisi hariç)
            const response = await fetch('/Home/GetTables');
            const result = await response.json();

            if (!result.success) {
                this.showError('Masa listesi alınamadı!');
                return;
            }

            // Dolu masaları filtrele (kendisi hariç)
            const occupiedTables = [];
            Object.values(result.data).forEach(categoryTables => {
                categoryTables.forEach(table => {
                    if (table.isOccupied && table.id !== sourceTableId) {
                        occupiedTables.push(table);
                    }
                });
            });

            if (occupiedTables.length === 0) {
                this.showError('Birleştirme için uygun dolu masa bulunamadı!');
                return;
            }

            this.renderTableSelectionModal({
                title: '🔗 Masa Birleştirme',
                subtitle: `<strong>${sourceTableName}</strong> hangi masa ile birleştirilecek?`,
                icon: 'fas fa-link',
                tables: occupiedTables,
                confirmText: 'Masaları Birleştir',
                confirmIcon: 'fas fa-link'
            });

        } catch (error) {
            console.error('Masa birleştirme hatası:', error);
            this.showError('Masa listesi yüklenirken hata oluştu!');
        }
    }

    // ❌ SİPARİŞ İPTAL MODAL
    static showCancelModal(tableId, tableName) {
        console.log('❌ Sipariş iptal başlatılıyor:', tableName);

        // Basit onay modalı
        const modalHTML = `
            <div class="modal fade" id="cancelOrderModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content table-selection-modal">
                        <div class="table-selection-header">
                            <h5 class="modal-title">
                                <i class="fas fa-exclamation-triangle me-2"></i>
                                Sipariş İptal Onayı
                            </h5>
                        </div>
                        <div class="modal-body text-center py-4">
                            <div class="mb-3">
                                <i class="fas fa-exclamation-triangle text-warning" style="font-size: 3rem;"></i>
                            </div>
                            <h6><strong>${tableName}</strong> masasındaki siparişi iptal etmek istediğinizden emin misiniz?</h6>
                            <p class="text-muted mt-2">Bu işlem geri alınamaz!</p>
                        </div>
                        <div class="modal-footer-actions">
                            <button type="button" class="btn-cancel-action" data-bs-dismiss="modal">
                                <i class="fas fa-times"></i>
                                Vazgeç
                            </button>
                            <button type="button" class="btn-confirm-action" onclick="TableActions.confirmCancelOrder('${tableId}', '${tableName}')">
                                <i class="fas fa-trash"></i>
                                Siparişi İptal Et
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Modal'ı sayfaya ekle ve göster
        document.body.insertAdjacentHTML('beforeend', modalHTML);
        const modal = new bootstrap.Modal(document.getElementById('cancelOrderModal'));
        modal.show();

        // Modal kapandığında temizle
        document.getElementById('cancelOrderModal').addEventListener('hidden.bs.modal', () => {
            document.getElementById('cancelOrderModal').remove();
        });
    }

    // 🎨 MASA SEÇİM MODAL RENDER
    static renderTableSelectionModal(config) {
        const modalHTML = `
            <div class="modal fade" id="tableSelectionModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content table-selection-modal">
                        <div class="table-selection-header">
                            <h5 class="modal-title">
                                <i class="${config.icon} me-2"></i>
                                ${config.title}
                            </h5>
                            <p class="mb-0 mt-2 opacity-90">${config.subtitle}</p>
                        </div>
                        <div class="table-selection-body">
                            <div id="targetTablesList">
                                ${config.tables.map(table => this.renderTargetTableCard(table)).join('')}
                            </div>
                        </div>
                        <div class="modal-footer-actions">
                            <button type="button" class="btn-cancel-action" data-bs-dismiss="modal">
                                <i class="fas fa-times"></i>
                                İptal
                            </button>
                            <button type="button" class="btn-confirm-action" id="confirmActionBtn" disabled 
                                    onclick="TableActions.confirmAction()">
                                <i class="${config.confirmIcon}"></i>
                                ${config.confirmText}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Modal'ı sayfaya ekle ve göster
        document.body.insertAdjacentHTML('beforeend', modalHTML);
        const modal = new bootstrap.Modal(document.getElementById('tableSelectionModal'));
        modal.show();

        // Masa seçim eventlerini bağla
        this.bindTableSelectionEvents();

        // Modal kapandığında temizle
        document.getElementById('tableSelectionModal').addEventListener('hidden.bs.modal', () => {
            document.getElementById('tableSelectionModal').remove();
            this.selectedTargetTable = null;
        });
    }

    // 🎨 HEDEF MASA KARTI RENDER
    static renderTargetTableCard(table) {
        const status = table.isOccupied ? 'occupied' : 'empty';
        const statusText = table.isOccupied ?
            `Dolu - ${this.formatCurrency(table.remainingAmount || 0)}` :
            'Boş Masa';
        const icon = table.isOccupied ? 'fas fa-chair' : 'fas fa-plus-circle';

        return `
            <div class="target-table-card ${status}" data-table-id="${table.id}" data-table-name="${table.name}">
                <div class="target-table-icon">
                    <i class="${icon}"></i>
                </div>
                <div class="target-table-info">
                    <div class="target-table-name">${table.name}</div>
                    <div class="target-table-status">${statusText}</div>
                </div>
            </div>
        `;
    }

    // 🎯 MASA SEÇİM EVENTLERİ
    static bindTableSelectionEvents() {
        document.querySelectorAll('.target-table-card').forEach(card => {
            card.addEventListener('click', () => {
                // Önceki seçimi kaldır
                document.querySelectorAll('.target-table-card').forEach(c => c.classList.remove('selected'));

                // Yeni seçimi yap
                card.classList.add('selected');
                this.selectedTargetTable = {
                    id: card.dataset.tableId,
                    name: card.dataset.tableName
                };

                // Confirm butonunu aktif et
                document.getElementById('confirmActionBtn').disabled = false;

                console.log('Hedef masa seçildi:', this.selectedTargetTable.name);
            });
        });
    }

    // ✅ İŞLEM ONAYI
    static async confirmAction() {
        if (!this.selectedTargetTable) {
            this.showError('Lütfen bir masa seçin!');
            return;
        }

        const confirmBtn = document.getElementById('confirmActionBtn');
        const originalText = confirmBtn.innerHTML;
        confirmBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> İşleniyor...';
        confirmBtn.disabled = true;

        try {
            let result;

            if (this.currentAction === 'move') {
                result = await this.executeMoveTable();
            } else if (this.currentAction === 'merge') {
                result = await this.executeMergeTable();
            }

            if (result && result.success) {
                this.showSuccess(result.message);

                // Modal'ı kapat
                const modal = bootstrap.Modal.getInstance(document.getElementById('tableSelectionModal'));
                modal.hide();

                // Dashboard'ı yenile
                setTimeout(() => {
                    if (window.GarsonDashboard) {
                        window.GarsonDashboard.loadTables();
                    }
                }, 500);
            } else {
                throw new Error(result?.message || 'İşlem başarısız!');
            }

        } catch (error) {
            console.error('İşlem hatası:', error);
            this.showError(error.message);
            confirmBtn.innerHTML = originalText;
            confirmBtn.disabled = false;
        }
    }

    // 🔄 MASA TAŞIMA İŞLEMİ
    static async executeMoveTable() {
        const response = await fetch('/Home/MoveTable', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                sourceTableId: this.currentSourceTable.id,
                targetTableId: this.selectedTargetTable.id
            })
        });

        return await response.json();
    }

    // 🔗 MASA BİRLEŞTİRME İŞLEMİ
    static async executeMergeTable() {
        const response = await fetch('/Home/MergeTables', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                sourceTableId: this.currentSourceTable.id,
                targetTableId: this.selectedTargetTable.id
            })
        });

        return await response.json();
    }

    // ❌ SİPARİŞ İPTAL İŞLEMİ
    static async confirmCancelOrder(tableId, tableName) {
        const confirmBtn = document.querySelector('#cancelOrderModal .btn-confirm-action');
        const originalText = confirmBtn.innerHTML;
        confirmBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> İptal Ediliyor...';
        confirmBtn.disabled = true;

        try {
            const response = await fetch('/Home/CancelTableOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ tableId: tableId })
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(`${tableName} masasının siparişi iptal edildi!`);

                // Modal'ı kapat
                const modal = bootstrap.Modal.getInstance(document.getElementById('cancelOrderModal'));
                modal.hide();

                // Dashboard'ı yenile
                setTimeout(() => {
                    if (window.GarsonDashboard) {
                        window.GarsonDashboard.loadTables();
                    }
                }, 500);
            } else {
                throw new Error(result.message);
            }

        } catch (error) {
            console.error('Sipariş iptal hatası:', error);
            this.showError(error.message);
            confirmBtn.innerHTML = originalText;
            confirmBtn.disabled = false;
        }
    }

    // 🛠️ YARDIMCI METODLAR
    static formatCurrency(amount) {
        return new Intl.NumberFormat('tr-TR', {
            style: 'currency',
            currency: 'TRY'
        }).format(amount || 0);
    }

    static showSuccess(message) {
        // Toast notification veya başka bir success gösterimi
        if (window.showToast) {
            window.showToast(message, 'success');
        } else {
            alert(message);
        }
    }

    static showError(message) {
        // Toast notification veya başka bir error gösterimi
        if (window.showToast) {
            window.showToast(message, 'error');
        } else {
            alert(message);
        }
    }
}

// Global erişim için
window.TableActions = TableActions;