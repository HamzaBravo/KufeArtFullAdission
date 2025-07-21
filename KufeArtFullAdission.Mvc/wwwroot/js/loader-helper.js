// Global Loader Helper
window.LoaderHelper = {
    show: function (message = 'Yükleniyor...') {
        const loader = document.getElementById('globalLoader');
        const text = loader.querySelector('.loader-text');

        text.textContent = message;
        loader.style.display = 'flex';

        // Body scroll'u engelle
        document.body.style.overflow = 'hidden';
    },

    hide: function () {
        const loader = document.getElementById('globalLoader');
        loader.style.display = 'none';

        // Body scroll'u geri aç
        document.body.style.overflow = '';
    },

    // Promise ile otomatik kullanım
    wrap: async function (promise, message = 'İşlem yapılıyor...') {
        this.show(message);
        try {
            const result = await promise;
            this.hide();
            return result;
        } catch (error) {
            this.hide();
            throw error;
        }
    }
};