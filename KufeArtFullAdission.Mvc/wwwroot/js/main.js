// wwwroot/js/main.js
$(document).ready(function () {
    // İlk yükleme
    TableManager.loadTables();

    // Otomatik güncellemeler
    setInterval(TableManager.updateTableTimes, 60000); // Her dakika zamanları güncelle

    // Global event delegation
    bindGlobalEvents();
});

function bindGlobalEvents() {
    // Masa kartına tıklama
    $(document).on('click', '.table-card', function () {
        const tableId = $(this).data('table-id');
        const tableName = $(this).find('.card-title').text();
        const isOccupied = $(this).hasClass('border-warning');
        TableManager.openTableModal(tableId, tableName, isOccupied);
    });

    // Parçalı ödeme butonları
    $(document).on('click', '#payWithCashBtn', function () {
        PaymentManager.processPartialPayment(0); // 0 = Nakit
    });

    $(document).on('click', '#payWithCardBtn', function () {
        PaymentManager.processPartialPayment(1); // 1 = Kart
    });

    // Sipariş gönder butonu
    $(document).on('click', '#submitOrderBtn', function () {
        OrderManager.submitOrder();
    });

    // Ürün arama
    $('#productSearch').on('input', OrderManager.handleProductSearch);

    // Modal temizleme event'leri
    $('#productSelectionModal').on('hidden.bs.modal', function () {
        OrderManager.clearCart();
        App.currentTableId = null; // Sadece modal kapanırken temizle
    });

    // Enter tuşu ile sipariş gönderme
    $('#waiterNote').on('keypress', function (e) {
        if (e.which === 13 && e.ctrlKey) { // Ctrl+Enter
            e.preventDefault();
            if (App.cartItems.length > 0) {
                OrderManager.submitOrder();
            }
        }
    });

    // Keyboard shortcuts
    $(document).on('keydown', function (e) {
        // Modal açıkken çalışsın
        if ($('#productSelectionModal').hasClass('show')) {
            // Esc = Modal kapat
            if (e.key === 'Escape' && !e.ctrlKey && !e.altKey) {
                $('#productSelectionModal').modal('hide');
            }

            // Ctrl+Enter = Sipariş gönder
            if (e.key === 'Enter' && e.ctrlKey && App.cartItems.length > 0) {
                e.preventDefault();
                OrderManager.submitOrder();
            }
        }
    });
}