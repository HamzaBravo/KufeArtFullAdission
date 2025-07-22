// wwwroot/js/utils.js
window.Utils = {

    getTimeAgo: function (dateString) {
        if (!dateString || dateString === 'undefined' || dateString === 'null') {
            return 'Az önce';
        }

        try {
            const now = new Date();
            const openedAt = new Date(dateString);

            // Geçersiz tarih kontrolü
            if (isNaN(openedAt.getTime())) {
                console.warn('⚠️ Invalid date:', dateString);
                return 'Az önce';
            }

            const diffMs = now - openedAt;

            // Negatif değer kontrolü (gelecek tarih)
            if (diffMs < 0) {
                console.warn('⚠️ Future date detected:', dateString);
                return 'Az önce';
            }

            const diffMins = Math.floor(diffMs / 60000);
            const diffHours = Math.floor(diffMins / 60);

            // Detaylı hesaplama
            if (diffMins < 1) return 'Az önce';
            if (diffMins < 60) return `${diffMins} dakika önce`;
            if (diffHours < 24) {
                const remainingMins = diffMins % 60;
                return remainingMins > 0 ? `${diffHours} saat ${remainingMins} dakika önce` : `${diffHours} saat önce`;
            }

            const diffDays = Math.floor(diffHours / 24);
            return `${diffDays} gün önce`;

        } catch (error) {
            console.error('❌ Date calculation error:', error, dateString);
            return 'Tarih hatası';
        }
    },

    openTableAccount: function (tableId) {
        if (!confirm('Bu masaya yeni hesap açmak istediğinizden emin misiniz?')) {
            return;
        }

        LoaderHelper.show('Hesap açılıyor...');

        $.ajax({
            url: App.endpoints.openTableAccount,
            method: 'POST',
            data: { tableId: tableId },
            success: function (response) {
                LoaderHelper.hide();

                if (response.success) {
                    ToastHelper.success(response.message);
                    $('#tableModal').modal('hide');
                    TableManager.loadTables();

                    setTimeout(() => {
                        const tableName = $('#modalTableName').text();
                        TableManager.openTableModal(tableId, tableName, true);
                    }, 1000);

                } else {
                    ToastHelper.error(response.message);
                }
            },
            error: function () {
                LoaderHelper.hide();
                ToastHelper.error('Bağlantı hatası! Lütfen tekrar deneyin.');
            }
        });
    },

    closeTableAccount: function (tableId) {
        if (!confirm('Bu hesabı kapatmak istediğinizden emin misiniz?\n\n⚠️ DİKKAT: Ödeme alınmadan hesap kapatılacak!')) {
            return;
        }

        LoaderHelper.show('Hesap kapatılıyor...');

        $.ajax({
            url: App.endpoints.closeTableAccount,
            method: 'POST',
            data: { tableId: tableId },
            success: function (response) {
                LoaderHelper.hide();

                if (response.success) {
                    ToastHelper.success(response.message);
                    $('#tableModal').modal('hide');
                    TableManager.loadTables();
                } else {
                    ToastHelper.error(response.message);
                }
            },
            error: function () {
                LoaderHelper.hide();
                ToastHelper.error('Bağlantı hatası!');
            }
        });
    }
};